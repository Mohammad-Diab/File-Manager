using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using Microsoft.Win32.SafeHandles;

namespace File_Manager
{
    class SafetyFileShredder
    {
        internal const string Caveat = "Note: on SSDs, USB flash drives and memory cards (wear levelling), in OneDrive or other cloud-synced folders (copies on the server and in the version history), for compressed, encrypted or sparse NTFS files and when Windows keeps shadow copies or restore points, overwriting can not guarantee that every old copy of the data is gone.";
        internal const string StoppedMessage = "stopped, the file was only partly overwritten";

        static RNGCryptoServiceProvider random_generator = new RNGCryptoServiceProvider();
        static Dictionary<string, long> cluster_sizes = new Dictionary<string, long>();
        static char[] name_chars = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        const int block_size = 1048576;

        DestroyMethod method;
        int repeat;
        internal Func<bool> Stopped = () => false;
        internal Func<bool> Paused = () => false;
        internal Action<long> Written = (count) => { };
        internal List<string> notes = new List<string>();

        internal SafetyFileShredder(DestroyMethod method, int repeat)
        {
            this.method = method;
            this.repeat = Math.Max(1, Math.Min(35, repeat));
        }

        internal static string MethodDescription(DestroyMethod method)
        {
            switch (method)
            {
                case DestroyMethod.FastZero:
                    return "Quick destroy (zeros): one pass of 0x00 bytes.";
                case DestroyMethod.FastOne:
                    return "Quick destroy (ones): one pass of 0xFF bytes.";
                case DestroyMethod.FastRandom:
                    return "Quick destroy (random): one pass of cryptographic random bytes.";
                default:
                    return "Secure destroy: 3 passes like DoD 5220.22-M (0x00, then 0xFF, then cryptographic random bytes), the last pass is read back and checked.";
            }
        }

        internal static string MethodHelp(DestroyMethod method)
        {
            return MethodDescription(method) + "\nEvery pass covers the whole file up to the end of its last cluster and is flushed to the disk. Then the file is renamed 3 times, emptied, its dates are reset and it is deleted. Alternate data streams are overwritten too, links are never followed.\n\n" + Caveat;
        }

        internal static string MethodsHelp()
        {
            StringBuilder help = new StringBuilder();
            for (int i = 1; i <= 4; i++)
                help.AppendLine(MethodDescription((DestroyMethod)i));
            help.AppendLine("Every pass covers the whole file up to the end of its last cluster and is flushed to the disk. Then the file is renamed 3 times, emptied, its dates are reset and it is deleted. Alternate data streams are overwritten too, links are never followed.");
            help.AppendLine();
            help.Append(Caveat);
            return help.ToString();
        }

        int[] Patterns()
        {
            switch (method)
            {
                case DestroyMethod.FastZero:
                    return new int[] { 0x00 };
                case DestroyMethod.FastOne:
                    return new int[] { 0xFF };
                case DestroyMethod.FastRandom:
                    return new int[] { -1 };
                default:
                    return new int[] { 0x00, 0xFF, -1 };
            }
        }

        internal int PassCount
        {
            get { return Patterns().Length * repeat; }
        }

        internal bool Verifies
        {
            get { return method == DestroyMethod.Secure; }
        }

        internal long WorkSize(FileInfo file)
        {
            long length = file.Length;
            return length * PassCount + (ClusterRound(file.FullName, length) - length) + (Verifies ? length : 0);
        }

        internal string Shred(FileInfo file, bool delete)
        {
            string path = file.FullName;
            FileAttributes attributes;
            long length;
            try
            {
                file.Refresh();
                if (!file.Exists)
                    return "the file was not found";
                if (Functions.IsLink(file))
                    return "this is a link, links are never followed or destroyed";
                attributes = file.Attributes;
                length = file.Length;
                File.SetAttributes(path, FileAttributes.Normal);
            }
            catch (Exception ex) { return ex.Message; }
            if ((attributes & (FileAttributes.Compressed | FileAttributes.Encrypted | FileAttributes.SparseFile)) != 0)
                notes.Add(path + "\n    (compressed, encrypted or sparse NTFS file, old data may stay in other places of the disk)");
            string error = null;
            try
            {
                error = ShredStreams(path);
                if (error == null)
                {
                    int[] patterns = Patterns();
                    long allocated = ClusterRound(path, length);
                    using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough))
                    {
                        byte[] hash = null;
                        for (int r = 0; r < repeat && error == null; r++)
                            for (int p = 0; p < patterns.Length && error == null; p++)
                                if (!WritePass(stream, 0, length, patterns[p], Verifies && r == repeat - 1 && p == patterns.Length - 1, out hash))
                                    error = StoppedMessage;
                        if (error == null && allocated > length)
                        {
                            byte[] slack_hash;
                            if (!WritePass(stream, length, allocated - length, patterns[patterns.Length - 1], false, out slack_hash))
                                error = StoppedMessage;
                        }
                        if (error == null && Verifies && !ReadBack(stream, length, hash))
                            error = Stopped() ? StoppedMessage : "the last pass could not be read back correctly, the file was kept";
                        if (stream.Length != length && (error != null || !delete))
                        {
                            stream.SetLength(length);
                            stream.Flush(true);
                        }
                    }
                }
            }
            catch (Exception ex) { error = ex.Message; }
            if (error != null || !delete)
            {
                try
                {
                    if (File.Exists(path))
                        File.SetAttributes(path, attributes & ~(FileAttributes.Compressed | FileAttributes.Encrypted | FileAttributes.SparseFile | FileAttributes.ReparsePoint));
                }
                catch (Exception) { }
                return error;
            }
            string current = path;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    string next = RandomName(Path.GetDirectoryName(current), Path.GetFileName(current).Length);
                    File.Move(current, next);
                    current = next;
                }
            }
            catch (Exception) { }
            try
            {
                using (FileStream stream = new FileStream(current, FileMode.Open, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    stream.SetLength(0);
                    stream.Flush(true);
                }
                DateTime old_date = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                File.SetCreationTimeUtc(current, old_date);
                File.SetLastWriteTimeUtc(current, old_date);
                File.SetLastAccessTimeUtc(current, old_date);
            }
            catch (Exception) { }
            try
            {
                File.Delete(current);
                return null;
            }
            catch (Exception ex)
            {
                return "overwritten, but not deleted: " + ex.Message + (current != path ? " (its name is now '" + Path.GetFileName(current) + "')" : "");
            }
        }

        internal long FillFile(string path, long size, out string error)
        {
            error = null;
            long done = 0;
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.WriteThrough))
                {
                    int[] patterns = Patterns();
                    byte[] buffer = NewBuffer(size, patterns[0]);
                    byte[] hash = null;
                    try
                    {
                        while (done < size)
                        {
                            if (!WaitIfPaused())
                            {
                                error = StoppedMessage;
                                return done;
                            }
                            int count = (int)Math.Min(buffer.Length, size - done);
                            if (patterns[0] < 0)
                                lock (random_generator)
                                    random_generator.GetBytes(buffer);
                            stream.Write(buffer, 0, count);
                            done += count;
                            Written(count);
                        }
                        stream.Flush(true);
                    }
                    catch (IOException ex)
                    {
                        if (!IsDiskFull(ex))
                            throw;
                        try { stream.SetLength(done); }
                        catch (Exception) { }
                    }
                    if (done == 0)
                        return 0;
                    for (int r = 0; r < repeat; r++)
                        for (int p = r == 0 ? 1 : 0; p < patterns.Length; p++)
                            if (!WritePass(stream, 0, done, patterns[p], Verifies && r == repeat - 1 && p == patterns.Length - 1, out hash))
                            {
                                error = StoppedMessage;
                                return done;
                            }
                    if (Verifies && !ReadBack(stream, done, hash) && !Stopped())
                        error = "the last pass could not be read back correctly";
                }
            }
            catch (Exception ex) { error = ex.Message; }
            return done;
        }

        internal void FillSmallFile(string path, int size)
        {
            int[] patterns = Patterns();
            byte[] buffer = NewBuffer(size, patterns[patterns.Length - 1]);
            if (patterns[patterns.Length - 1] < 0)
                lock (random_generator)
                    random_generator.GetBytes(buffer);
            using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                stream.Write(buffer, 0, size);
                stream.Flush(true);
            }
        }

        static bool IsDiskFull(IOException ex)
        {
            int code = Marshal.GetHRForException(ex) & 0xFFFF;
            return code == 112 || code == 39;
        }

        static byte[] NewBuffer(long length, int pattern)
        {
            byte[] buffer = new byte[(int)Math.Max(1, Math.Min(block_size, length))];
            if (pattern > 0)
                for (int k = 0; k < buffer.Length; k++)
                    buffer[k] = (byte)pattern;
            return buffer;
        }

        bool WaitIfPaused()
        {
            while (Paused() && !Stopped())
                Thread.Sleep(50);
            return !Stopped();
        }

        bool WritePass(FileStream stream, long start, long length, int pattern, bool verify, out byte[] hash)
        {
            hash = null;
            byte[] buffer = NewBuffer(length, pattern);
            using (SHA256 hasher = verify ? new SHA256Cng() : null)
            {
                stream.Position = start;
                long written = 0;
                while (written < length)
                {
                    if (!WaitIfPaused())
                        return false;
                    int count = (int)Math.Min(buffer.Length, length - written);
                    if (pattern < 0)
                        lock (random_generator)
                            random_generator.GetBytes(buffer);
                    stream.Write(buffer, 0, count);
                    if (hasher != null)
                        hasher.TransformBlock(buffer, 0, count, null, 0);
                    written += count;
                    Written(count);
                }
                stream.Flush(true);
                if (hasher != null)
                {
                    hasher.TransformFinalBlock(new byte[0], 0, 0);
                    hash = hasher.Hash;
                }
            }
            return true;
        }

        bool ReadBack(FileStream stream, long length, byte[] hash)
        {
            if (hash == null)
                return false;
            using (SHA256 hasher = new SHA256Cng())
            {
                stream.Position = 0;
                byte[] buffer = new byte[(int)Math.Max(1, Math.Min(block_size, length))];
                long total = 0;
                int read;
                while (total < length && (read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, length - total))) > 0)
                {
                    if (!WaitIfPaused())
                        return false;
                    hasher.TransformBlock(buffer, 0, read, null, 0);
                    total += read;
                    Written(read);
                }
                hasher.TransformFinalBlock(new byte[0], 0, 0);
                if (total != length || hasher.Hash.Length != hash.Length)
                    return false;
                for (int i = 0; i < hash.Length; i++)
                    if (hasher.Hash[i] != hash[i])
                        return false;
                return true;
            }
        }

        string ShredStreams(string path)
        {
            List<KeyValuePair<string, long>> streams = AlternateStreams(path);
            int[] patterns = Patterns();
            foreach (var i in streams)
            {
                try
                {
                    using (SafeFileHandle handle = CreateFile(path + i.Key, 0xC0000000, 0, IntPtr.Zero, 3, 0x80000000, IntPtr.Zero))
                    {
                        if (handle.IsInvalid)
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        using (FileStream stream = new FileStream(handle, FileAccess.ReadWrite))
                        {
                            byte[] hash;
                            for (int r = 0; r < repeat; r++)
                                for (int p = 0; p < patterns.Length; p++)
                                    if (!WritePass(stream, 0, i.Value, patterns[p], false, out hash))
                                        return StoppedMessage;
                            stream.SetLength(0);
                            stream.Flush(true);
                        }
                    }
                }
                catch (Exception ex) { return "alternate data stream '" + i.Key + "': " + ex.Message; }
            }
            return null;
        }

        static List<KeyValuePair<string, long>> AlternateStreams(string path)
        {
            List<KeyValuePair<string, long>> streams = new List<KeyValuePair<string, long>>();
            try
            {
                WIN32_FIND_STREAM_DATA data;
                IntPtr handle = FindFirstStreamW(path, 0, out data, 0);
                if (handle == new IntPtr(-1))
                    return streams;
                try
                {
                    do
                    {
                        if (!string.IsNullOrEmpty(data.cStreamName) && !string.Equals(data.cStreamName, "::$DATA", StringComparison.OrdinalIgnoreCase))
                            streams.Add(new KeyValuePair<string, long>(data.cStreamName, data.StreamSize));
                    }
                    while (FindNextStreamW(handle, out data));
                }
                finally { FindClose(handle); }
            }
            catch (Exception) { }
            return streams;
        }

        static string RandomName(string folder, int length)
        {
            length = Math.Max(1, length);
            StringBuilder name = new StringBuilder();
            string path;
            do
            {
                name.Clear();
                byte[] bytes = new byte[length];
                lock (random_generator)
                    random_generator.GetBytes(bytes);
                foreach (byte i in bytes)
                    name.Append(name_chars[i % name_chars.Length]);
                path = Path.Combine(folder, name.ToString());
            }
            while (File.Exists(path) || Directory.Exists(path));
            return path;
        }

        static long ClusterRound(string path, long length)
        {
            long cluster = ClusterSize(path);
            if (cluster <= 0 || length == 0)
                return length;
            return (length + cluster - 1) / cluster * cluster;
        }

        static long ClusterSize(string path)
        {
            string root;
            try { root = Path.GetPathRoot(Path.GetFullPath(path)); }
            catch (Exception) { return 0; }
            if (string.IsNullOrEmpty(root))
                return 0;
            if (!root.EndsWith("\\"))
                root += "\\";
            lock (cluster_sizes)
            {
                if (cluster_sizes.ContainsKey(root.ToLower()))
                    return cluster_sizes[root.ToLower()];
                long size = 0;
                try
                {
                    uint sectors, bytes, free_clusters, total_clusters;
                    if (GetDiskFreeSpace(root, out sectors, out bytes, out free_clusters, out total_clusters))
                        size = (long)sectors * bytes;
                }
                catch (Exception) { }
                cluster_sizes[root.ToLower()] = size;
                return size;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WIN32_FIND_STREAM_DATA
        {
            public long StreamSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)]
            public string cStreamName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr FindFirstStreamW(string lpFileName, int InfoLevel, out WIN32_FIND_STREAM_DATA lpFindStreamData, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool FindNextStreamW(IntPtr hFindStream, out WIN32_FIND_STREAM_DATA lpFindStreamData);

        [DllImport("kernel32.dll")]
        static extern bool FindClose(IntPtr hFindFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool GetDiskFreeSpace(string lpRootPathName, out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters, out uint lpTotalNumberOfClusters);
    }

    public partial class Operation
    {
        internal List<string> done_files = new List<string>();

        SafetyFileShredder SafetyShredder(DestroyMethod method, int repeat)
        {
            SafetyFileShredder shredder = new SafetyFileShredder(method, repeat);
            shredder.Stopped = () => CloseWindow;
            shredder.Paused = () => pause_copy;
            shredder.Written = (count) => { progress_value += count; };
            return shredder;
        }

        void SafetyProgressThread(BackgroundWorker worker)
        {
            Thread progress_thread = new Thread(() =>
            {
                while (worker.IsBusy)
                {
                    try { worker.ReportProgress(1); }
                    catch (Exception) { }
                    Thread.Sleep(100);
                }
            });
            progress_thread.IsBackground = true;
            progress_thread.Start();
        }

        void SafetyShow(BackgroundWorker worker, bool show_dialog)
        {
            worker.ProgressChanged += Destroy_ProgressChanged;
            worker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (e.Error != null)
                    result_errors.Add(e.Error.Message);
                disk_destroy_busy = false;
                operation_finished = true;
                Close();
            };
            disk_destroy_busy = true;
            progress_bar.IsIndeterminate = false;
            worker.RunWorkerAsync();
            if (show_dialog)
                ShowDialog();
            else
                Show();
        }

        internal void StartSafetyDestroyFiles(List<FileInfo> files_list, DestroyMethod method, int repeat, bool delete, bool show_dialog)
        {
            OperationType = OperationType.Destroy;
            Title = operation.Text = string.Format("Destroying {0} {1}", files_list.Count, files_list.Count > 1 ? "items" : "item");
            SafetyFileShredder shredder = SafetyShredder(method, repeat);
            double max_value = 0;
            foreach (FileInfo i in files_list)
                try { max_value += shredder.WorkSize(i); }
                catch (Exception) { }
            progress_bar.Maximum = max_value + 1;
            BackgroundWorker destroy_worker = new BackgroundWorker();
            destroy_worker.WorkerReportsProgress = true;
            destroy_worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                SafetyProgressThread((BackgroundWorker)sender);
                foreach (var i in files_list)
                {
                    if (CloseWindow)
                        break;
                    current_file_name = i.FullName;
                    string error = shredder.Shred(i, delete);
                    if (error == null)
                    {
                        completed_count++;
                        done_files.Add(i.FullName);
                    }
                    else
                        result_errors.Add(i.FullName + "\n    (" + error + ")");
                }
                result_notes.AddRange(shredder.notes);
            };
            SafetyShow(destroy_worker, show_dialog);
        }

        internal void StartSafetyDestroyDisk(DriveInfo drive, DestroyMethod method, bool show_dialog)
        {
            OperationType = OperationType.DiskDestroy;
            operation.Text = Title = string.Format("Destroying {0}", drive.Name);
            SafetyFileShredder shredder = SafetyShredder(method, 1);
            long reserve = drive.Name.ToLower() == Functions.SystemDrive ? 2147483648 : 0;
            string folder = Path.Combine(drive.Name, WipeFolderName);
            long free = 0;
            try { free = drive.AvailableFreeSpace; }
            catch (Exception) { }
            progress_bar.Maximum = Math.Max(1, (free - reserve) * (shredder.PassCount + (shredder.Verifies ? 1 : 0)));
            BackgroundWorker destroy_worker = new BackgroundWorker();
            destroy_worker.WorkerReportsProgress = true;
            destroy_worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                SafetyProgressThread((BackgroundWorker)sender);
                List<string> files_name = new List<string>();
                try
                {
                    Directory.CreateDirectory(folder);
                    int file_creating_error = 0;
                    string last_error = null;
                    long max_file_size = 2147483648;
                    switch (drive.DriveFormat.ToLower())
                    {
                        case "ntfs":
                        case "refs":
                        case "exfat":
                            max_file_size = 17179869184;
                            break;
                        case "fat32":
                            max_file_size = 4294967294;
                            break;
                        case "fat12":
                            max_file_size = 16777216;
                            break;
                    }
                    while (drive.IsReady && drive.AvailableFreeSpace - reserve > 1048576 && file_creating_error < 3)
                    {
                        if (CloseWindow)
                            return;
                        string path = GetFileName(folder);
                        files_name.Add(path);
                        current_file_name = path;
                        string error;
                        long written = shredder.FillFile(path, Math.Min(max_file_size, drive.AvailableFreeSpace - reserve), out error);
                        if (CloseWindow)
                            return;
                        if (error != null || written == 0)
                        {
                            file_creating_error++;
                            if (error != null)
                                last_error = error;
                        }
                    }
                    if (reserve == 0)
                        for (int i = 0; i < 20000; i++)
                        {
                            if (CloseWindow)
                                return;
                            while (pause_copy && !CloseWindow)
                                Thread.Sleep(50);
                            string path = GetFileName(folder);
                            files_name.Add(path);
                            try { shredder.FillSmallFile(path, 512); }
                            catch (Exception) { break; }
                        }
                    if (file_creating_error > 2)
                        result_errors.Add("Some free space on " + drive.Name + " could not be destroyed." + (last_error != null ? "\n    (" + last_error + ")" : ""));
                }
                catch (Exception ex) { result_errors.Add(ex.Message); }
                finally
                {
                    current_file_name = "Cleaning up...";
                    for (int i = files_name.Count - 1; i >= 0; i--)
                        DeleteQuietly(files_name[i]);
                    DeleteWipeFolder(folder);
                    if (Directory.Exists(folder))
                        result_errors.Add("Some temporary files could not be deleted, delete the folder '" + folder + "' manually (or restart File Manager, it deletes it at start).");
                }
            };
            SafetyShow(destroy_worker, show_dialog);
        }

        internal void StartSafetyEncrypt(List<FileInfo> files_list, string password, bool overwrite_files, string save_in, bool encrypt_files_name, bool is_package, bool show_dialog)
        {
            OperationType = OperationType.Encrypt;
            Title = operation.Text = string.Format("Encrypting {0} {1}", files_list.Count, files_list.Count > 1 ? "items" : "item");
            bool shred = overwrite_files;
            SafetyFileShredder shredder = SafetyShredder(DestroyMethod.FastZero, 1);
            double max_value = 0;
            foreach (FileInfo i in files_list)
                try { max_value += i.Length * 2 + (shred ? shredder.WorkSize(i) : 0); }
                catch (Exception) { }
            progress_bar.Maximum = max_value + 1;
            BackgroundWorker encrypt_worker = new BackgroundWorker();
            encrypt_worker.WorkerReportsProgress = true;
            encrypt_worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                SafetyProgressThread((BackgroundWorker)sender);
                if (is_package)
                    EncryptToFile(files_list, password, OutputFileName(save_in, ".enp"), encrypt_files_name, true);
                else
                    foreach (var i in files_list)
                    {
                        if (CloseWindow)
                            break;
                        string folder = overwrite_files ? i.DirectoryName : save_in;
                        string output_path = encrypt_files_name ? OutputFileName(folder, ".enf") : UniqueFilePath(folder, i.Name + ".enf");
                        EncryptToFile(new List<FileInfo>() { i }, password, output_path, encrypt_files_name, false);
                    }
                if (!shred || verified_files.Count == 0)
                    return;
                if (CloseWindow)
                {
                    result_notes.Add(string.Format("The {0} original file{1} with a verified encrypted copy {2} kept because the operation was stopped.", verified_files.Count, verified_files.Count > 1 ? "s" : "", verified_files.Count > 1 ? "were" : "was"));
                    return;
                }
                int kept = 0;
                foreach (var i in verified_files)
                {
                    if (CloseWindow)
                    {
                        kept++;
                        continue;
                    }
                    current_file_name = i.FullName;
                    string error = shredder.Shred(i, true);
                    if (error == SafetyFileShredder.StoppedMessage)
                        result_errors.Add(i.FullName + "\n    (stopped while it was securely deleted, the original was only partly overwritten, its verified encrypted copy is kept)");
                    else if (error != null)
                        result_errors.Add(i.FullName + "\n    (encrypted and verified, but the original could not be securely deleted: " + error + ")");
                }
                if (kept > 0)
                    result_notes.Add(string.Format("{0} original file{1} with a verified encrypted copy {2} kept because the operation was stopped.", kept, kept > 1 ? "s" : "", kept > 1 ? "were" : "was"));
                result_notes.AddRange(shredder.notes);
            };
            SafetyShow(encrypt_worker, show_dialog);
        }
    }
}
