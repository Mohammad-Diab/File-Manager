using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for ArchiveOperation.xaml
    /// </summary>
    public partial class ArchiveOperation : Window
    {
        Brush progress_brush;
        double progress_value = 0, old_progress = 0;
        long time, last_time, total_size = 0;
        bool stop_operation = false, operation_finished = false, pause_operation = false, report_stream = false;
        string current_file = "";
        int conflict_for_all = 0, stream_percent = -1, moved_files = 0, replaced_files = 0, skipped_files = 0;
        BackgroundWorker worker;
        List<string> move_errors = new List<string>();
        internal string result_error = null;
        internal List<string> result_notes = new List<string>();
        internal int broken_count = 0;

        public ArchiveOperation()
        {
            InitializeComponent();
        }

        #region Common

        void RunOperation(Action work)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            worker = bw;
            bw.DoWork += (object sender, DoWorkEventArgs e) => { work(); };
            bw.ProgressChanged += Worker_ProgressChanged;
            bw.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (e.Error != null && result_error == null)
                    result_error = e.Error.Message;
                operation_finished = true;
                Close();
            };
            bw.RunWorkerAsync();
            ShowDialog();
        }

        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progress_bar.IsIndeterminate = false;
            progress_bar.Value = e.ProgressPercentage;
            percent.Text = e.ProgressPercentage + "%";
            TaskbarItemInfo.ProgressValue = e.ProgressPercentage / 100.0;
            if (!stop_operation && !pause_operation)
                archive_name.Text = current_file;
            double done = total_size * e.ProgressPercentage / 100.0;
            ChangeRemain(total_size - done, false);
            UpdateSpeed(done);
        }

        void Report(int value)
        {
            try { worker.ReportProgress(Math.Max(0, Math.Min(100, value))); }
            catch (Exception) { }
        }

        void SetPhase(string title, long size, bool indeterminate)
        {
            stream_percent = -1;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                operation_title.Text = title;
                total_size = size;
                last_time = 0;
                speed_text.Text = "";
                time_text.Text = "";
                progress_bar.Value = 0;
                progress_bar.IsIndeterminate = indeterminate;
                percent.Text = indeterminate ? "" : "0%";
                ChangeRemain(size, false);
            }));
        }

        internal void CheckState()
        {
            WaitIfPaused();
            if (stop_operation)
                throw new OperationCanceledException();
        }

        void WaitIfPaused()
        {
            while (pause_operation && !stop_operation)
                Thread.Sleep(100);
        }

        internal void StreamMoved(long position, long length)
        {
            if (!report_stream || length <= 0)
                return;
            int value = (int)(position * 100 / length);
            if (value != stream_percent)
            {
                stream_percent = value;
                Report(value);
            }
        }

        void UpdateSpeed(double done)
        {
            long now = DateTime.UtcNow.Ticks;
            if (last_time == 0)
            {
                last_time = now;
                old_progress = done;
                return;
            }
            time = now - last_time;
            if (time > 10000000)
            {
                double speed = (done - old_progress) / (time / 10000000.0);
                if (speed >= 0)
                {
                    speed_text.Text = Functions.FileSize(speed, 1) + "/s";
                    time_text.Text = speed > 0 ? TimeText((total_size - done) / speed) : "";
                }
                last_time = now;
                old_progress = done;
            }
            progress_value = done;
        }

        static string TimeText(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 8640000)
                return "";
            TimeSpan span = TimeSpan.FromSeconds(Math.Ceiling(seconds));
            if (span.TotalDays >= 1)
                return string.Format("{0} day{1} {2} h", (int)span.TotalDays, (int)span.TotalDays > 1 ? "s" : "", span.Hours);
            if (span.TotalHours >= 1)
                return string.Format("{0} h {1} min", span.Hours, span.Minutes);
            if (span.TotalMinutes >= 1)
                return string.Format("{0} min {1} s", span.Minutes, span.Seconds);
            return span.Seconds + " s";
        }

        void ChangeRemain(double size, bool is_items)
        {
            string remain_number;
            remain_number = Functions.FileSize1000(size, 3);
            remain.Text = remain_number;
            remain_unit.Text = (is_items ? "item" : "byte") + (size > 1 ? "s" : "");
            remain_unit.Margin = new Thickness(100 - (10 * (4 - remain.Text.Length)), 40, 0, 0);
        }

        static bool IsSafeEntry(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Contains(":") || name.StartsWith("\\") || name.StartsWith("/"))
                return false;
            foreach (var i in name.Split('\\', '/'))
                if (i == "..")
                    return false;
            return true;
        }

        internal static string SafeName(string name)
        {
            foreach (char i in Path.GetInvalidFileNameChars())
                name = name.Replace(i, '_');
            return name;
        }

        static string DiskPath(string entry)
        {
            string[] parts = entry.Split('\\');
            for (int i = 0; i < parts.Length; i++)
                parts[i] = SafeName(parts[i]);
            return string.Join("\\", parts);
        }

        static string UniqueName(string path, bool is_file)
        {
            string folder = Path.GetDirectoryName(path), name = is_file ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path), extention = is_file ? Path.GetExtension(path) : "";
            int num = 2;
            string new_name;
            do new_name = Path.Combine(folder, name + " (" + num++ + ")" + extention);
            while (File.Exists(new_name) || Directory.Exists(new_name));
            return new_name;
        }

        static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
            }
            catch (Exception) { }
        }

        static bool TryDeleteDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return true;
                foreach (string i in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(i, FileAttributes.Normal);
                foreach (string i in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(i, FileAttributes.Normal);
                File.SetAttributes(path, FileAttributes.Normal);
                Directory.Delete(path, true);
                return true;
            }
            catch (Exception) { return false; }
        }

        static void ReplaceFile(string source, string target)
        {
            if (!File.Exists(target))
            {
                File.Move(source, target);
                return;
            }
            FileAttributes attributes = File.GetAttributes(target);
            string backup = target + "." + Guid.NewGuid().ToString("N").Substring(0, 8) + ".old";
            File.Move(target, backup);
            try { File.Move(source, target); }
            catch (Exception)
            {
                File.Move(backup, target);
                throw;
            }
            try { File.SetAttributes(target, attributes & ~FileAttributes.ReadOnly); }
            catch (Exception) { }
            TryDeleteFile(backup);
        }

        static bool IsSingleStream(InArchiveFormat format)
        {
            return format == InArchiveFormat.GZip || format == InArchiveFormat.BZip2 || format == InArchiveFormat.XZ || format == InArchiveFormat.Lzma || format == InArchiveFormat.Lzw || format == InArchiveFormat.Split;
        }

        SevenZipExtractor OpenExtractor(string archive, string password, out Stream stream)
        {
            stream = null;
            SevenZipExtractor sze = string.IsNullOrEmpty(password) ? new SevenZipExtractor(archive) : new SevenZipExtractor(archive, password);
            List<string> names = new List<string>();
            bool use_stream;
            try
            {
                foreach (var i in sze.ArchiveFileData)
                    names.Add(i.FileName);
                use_stream = names.Count > 0 && sze.VolumeFileNames.Count <= 1 && !IsSingleStream(sze.Format) && names.All(x => !string.IsNullOrEmpty(x));
            }
            catch (Exception) { use_stream = false; }
            if (!use_stream)
                return sze;
            SevenZipExtractor by_stream = null;
            try
            {
                stream = new ControlledStream(new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.Read), this);
                by_stream = string.IsNullOrEmpty(password) ? new SevenZipExtractor(stream) : new SevenZipExtractor(stream, password);
                var data = by_stream.ArchiveFileData;
                bool same = data.Count == names.Count;
                for (int i = 0; same && i < data.Count; i++)
                    same = data[i].FileName == names[i];
                if (same)
                {
                    sze.Dispose();
                    return by_stream;
                }
            }
            catch (Exception) { }
            if (by_stream != null)
                try { by_stream.Dispose(); }
                catch (Exception) { }
            if (stream != null)
                try { stream.Dispose(); }
                catch (Exception) { }
            stream = null;
            return sze;
        }

        static void CloseExtractor(SevenZipExtractor sze, Stream stream)
        {
            if (sze != null)
                try { sze.Dispose(); }
                catch (Exception) { }
            if (stream != null)
                try { stream.Dispose(); }
                catch (Exception) { }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        SevenZipCompressor NewCompressor(string folder, OutArchiveFormat format)
        {
            SevenZipCompressor szc = new SevenZipCompressor(folder);
            szc.EventSynchronization = EventSynchronizationStrategy.AlwaysSynchronous;
            szc.ArchiveFormat = format;
            szc.CompressionMode = CompressionMode.Create;
            szc.DirectoryStructure = true;
            szc.CompressionLevel = format == OutArchiveFormat.Tar ? CompressionLevel.None : CompressionLevel.Normal;
            szc.Compressing += (object sender, ProgressEventArgs e) =>
            {
                WaitIfPaused();
                Report(e.PercentDone);
            };
            szc.FileCompressionStarted += (object sender, FileNameEventArgs e) =>
            {
                WaitIfPaused();
                current_file = e.FileName;
                if (stop_operation)
                    e.Cancel = true;
            };
            return szc;
        }

        void CopyFile(string source, string target)
        {
            byte[] buffer = new byte[1048576];
            using (FileStream input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FileStream output = new FileStream(target, FileMode.CreateNew, FileAccess.Write))
            {
                long length = input.Length, done = 0;
                int read, last = -1;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    CheckState();
                    output.Write(buffer, 0, read);
                    done += read;
                    int value = length > 0 ? (int)(done * 100 / length) : 100;
                    if (value != last)
                    {
                        last = value;
                        Report(value);
                    }
                }
            }
        }

        List<SourceGroup> GroupSources(IEnumerable<string> sources, ref long size, ref int files_count, int[] skipped)
        {
            List<SourceGroup> groups = new List<SourceGroup>();
            Dictionary<string, SourceGroup> by_parent = new Dictionary<string, SourceGroup>();
            foreach (string i in sources)
            {
                CheckState();
                string parent = Path.GetDirectoryName(i);
                SourceGroup group;
                if (!by_parent.TryGetValue(parent.ToLower(), out group))
                {
                    group = new SourceGroup();
                    group.RootLength = (parent.TrimEnd('\\') + "\\").Length;
                    by_parent.Add(parent.ToLower(), group);
                    groups.Add(group);
                }
                group.Paths.Add(i);
                if (Directory.Exists(i))
                    AddDirectoryContent(i, group.Paths, ref size, ref files_count, skipped);
                else
                {
                    try { size += new FileInfo(i).Length; }
                    catch (Exception) { }
                    files_count++;
                }
            }
            return groups;
        }

        void AddDirectoryContent(string folder, List<string> paths, ref long size, ref int files_count, int[] skipped)
        {
            CheckState();
            DirectoryInfo directory = new DirectoryInfo(folder);
            try
            {
                foreach (var i in directory.GetFiles())
                {
                    paths.Add(i.FullName);
                    size += i.Length;
                    files_count++;
                }
                foreach (var i in directory.GetDirectories())
                {
                    if (Functions.IsLink(i))
                    {
                        skipped[0]++;
                        continue;
                    }
                    paths.Add(i.FullName);
                    AddDirectoryContent(i.FullName, paths, ref size, ref files_count, skipped);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { throw new IOException(string.Format("We can not read the directory '{0}': {1}", folder, ex.Message), ex); }
        }

        #endregion

        #region Compress

        internal void StartCompressing(CompressOptions options, CompressItem files_names)
        {
            progress_brush = Brushes.MediumPurple;
            Title = "Compressing " + Functions.FileSize(files_names.RealSize.HasValue ? files_names.RealSize.Value : 0, 2);
            operation_title.Text = "Creating archive:";
            speed_text.Text = "";
            time_text.Text = "";
            archive_name.ToolTip = archive_name.Text = options.Output;
            List<SourceGroup> groups = new List<SourceGroup>();
            Dictionary<string, SourceGroup> by_parent = new Dictionary<string, SourceGroup>();
            long total = 0;
            int files_count = 0, directories_count = 0;
            foreach (var i in files_names.Children.Values)
            {
                if (i.Item == null)
                    continue;
                string full = i.Item.FullName.TrimEnd('\\'), parent = Path.GetDirectoryName(full);
                if (parent == null)
                    continue;
                SourceGroup group;
                if (!by_parent.TryGetValue(parent.ToLower(), out group))
                {
                    group = new SourceGroup();
                    group.RootLength = (parent.TrimEnd('\\') + "\\").Length;
                    by_parent.Add(parent.ToLower(), group);
                    groups.Add(group);
                }
                AddNodePaths(i, group.Paths, ref total, ref files_count, ref directories_count);
            }
            if (files_count + directories_count == 0)
            {
                Messages.ShowMessage("There are no files to compress.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                return;
            }
            if (!Functions.PrepareSevenZip())
                return;
            string folder = Path.GetDirectoryName(options.Output), temp_path = options.Output + ".partial", tar_path = options.Output + ".tar.partial";
            List<string> volumes = new List<string>();
            bool done = false;
            total_size = total;
            Process process = Process.GetCurrentProcess();
            ProcessPriorityClass old_priority = ProcessPriorityClass.Normal;
            try
            {
                old_priority = process.PriorityClass;
                if (options.Priority != 0)
                    process.PriorityClass = options.Priority < 0 ? ProcessPriorityClass.BelowNormal : ProcessPriorityClass.AboveNormal;
            }
            catch (Exception) { }
            RunOperation(() =>
            {
                Thread.CurrentThread.Priority = options.Priority < 0 ? ThreadPriority.BelowNormal : options.Priority > 0 ? ThreadPriority.AboveNormal : ThreadPriority.Normal;
                try
                {
                    bool single_stream = options.Format == OutArchiveFormat.GZip || options.Format == OutArchiveFormat.BZip2 || options.Format == OutArchiveFormat.XZ;
                    if (single_stream && files_count == 1 && directories_count == 0)
                    {
                        SevenZipCompressor szc = NewCompressor(folder, options.Format);
                        szc.CompressionLevel = (CompressionLevel)options.Level;
                        using (ControlledStream output = new ControlledStream(new FileStream(temp_path, FileMode.Create, FileAccess.ReadWrite), this))
                            szc.CompressFiles(output, groups[0].RootLength, groups[0].Paths.ToArray());
                    }
                    else if (single_stream)
                    {
                        BuildArchive(OutArchiveFormat.Tar, groups, tar_path, CompressionLevel.None, "", false, null);
                        CheckState();
                        SetPhase("Compressing:", new FileInfo(tar_path).Length, false);
                        SevenZipCompressor szc = NewCompressor(folder, options.Format);
                        szc.CompressionLevel = (CompressionLevel)options.Level;
                        string inner = Path.GetFileName(options.Output);
                        if (inner.Contains('.'))
                            inner = inner.Remove(inner.LastIndexOf('.'));
                        if (!inner.EndsWith(".tar", StringComparison.OrdinalIgnoreCase))
                            inner += ".tar";
                        szc.DefaultItemName = inner;
                        using (FileStream tar = new FileStream(tar_path, FileMode.Open, FileAccess.Read))
                        using (ControlledStream output = new ControlledStream(new FileStream(temp_path, FileMode.Create, FileAccess.ReadWrite), this))
                            szc.CompressStream(tar, output);
                    }
                    else
                        BuildArchive(options.Format, groups, temp_path, (CompressionLevel)options.Level, options.Password, options.EncryptNames, options.SolidBlock);
                    CheckState();
                    if (options.VolumeSize > 0)
                    {
                        SetPhase("Splitting:", new FileInfo(temp_path).Length, false);
                        SplitFile(temp_path, options.Output, options.VolumeSize, volumes);
                        CheckState();
                        for (int i = 0; i < volumes.Count; i++)
                        {
                            string final_name = options.Output + "." + (i + 1).ToString("000");
                            TryDeleteFile(final_name);
                            File.Move(volumes[i], final_name);
                            volumes[i] = final_name;
                        }
                        for (int i = volumes.Count + 1; File.Exists(options.Output + "." + i.ToString("000")); i++)
                            TryDeleteFile(options.Output + "." + i.ToString("000"));
                    }
                    else
                    {
                        ReplaceFile(temp_path, options.Output);
                        volumes.Add(options.Output);
                    }
                    done = true;
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (!stop_operation)
                        result_error = ex.Message;
                }
                finally
                {
                    TryDeleteFile(temp_path);
                    TryDeleteFile(tar_path);
                    TryDeleteFile(Path.Combine(folder, Path.GetFileName(temp_path) + ".~"));
                    TryDeleteFile(Path.Combine(folder, Path.GetFileName(tar_path) + ".~"));
                    if (!done)
                        foreach (var i in volumes)
                            if (i.EndsWith(".partial"))
                                TryDeleteFile(i);
                }
            });
            try { process.PriorityClass = old_priority; }
            catch (Exception) { }
            if (result_error != null)
                Messages.ShowMessage("We can not create the archive.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, result_error + "\nNothing was written, an older file with the same name was not changed.");
            else if (!done)
                Messages.ShowMessage("Compressing was stopped, the unfinished archive was deleted.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else
            {
                long archive_size = 0;
                foreach (var i in volumes)
                    try { archive_size += new FileInfo(i).Length; }
                    catch (Exception) { }
                StringBuilder details = new StringBuilder();
                details.AppendLine(volumes.Count > 1 ? string.Format("{0} volumes: {1} ... {2}", volumes.Count, volumes[0], Path.GetFileName(volumes[volumes.Count - 1])) : volumes[0]);
                details.AppendLine(string.Format("{0} file{1}, {2} folder{3}, {4} -> {5}", files_count, files_count == 1 ? "" : "s", directories_count, directories_count == 1 ? "" : "s", Functions.FileSize(total, 2), Functions.FileSize(archive_size, 2)));
                foreach (var i in result_notes)
                    details.AppendLine(i);
                Messages.ShowMessage("Compressing done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, details.ToString());
            }
        }

        static void AddNodePaths(CompressItem node, List<string> paths, ref long size, ref int files_count, ref int directories_count)
        {
            paths.Add(node.Item.FullName.TrimEnd('\\'));
            if (node.IsDirectory)
            {
                directories_count++;
                if (node.Children != null)
                    foreach (var i in node.Children.Values)
                        if (i.Item != null)
                            AddNodePaths(i, paths, ref size, ref files_count, ref directories_count);
            }
            else
            {
                files_count++;
                size += (long)(node.RealSize.HasValue ? node.RealSize.Value : 0);
            }
        }

        void BuildArchive(OutArchiveFormat format, List<SourceGroup> groups, string path, CompressionLevel level, string password, bool encrypt_names, string solid_block_size)
        {
            string folder = Path.GetDirectoryName(path);
            bool has_password = !string.IsNullOrEmpty(password) && (format == OutArchiveFormat.SevenZip || format == OutArchiveFormat.Zip);
            bool use_dictionary = groups.Count > 1 && has_password && encrypt_names && format == OutArchiveFormat.SevenZip;
            for (int i = 0; i < groups.Count; i++)
            {
                CheckState();
                SevenZipCompressor szc = NewCompressor(folder, format);
                szc.CompressionLevel = format == OutArchiveFormat.Tar ? CompressionLevel.None : level;
                if (format == OutArchiveFormat.SevenZip)
                {
                    szc.EncryptHeaders = has_password && encrypt_names;
                    if (level != CompressionLevel.None && solid_block_size != null)
                        szc.CustomParameters.Add("s", solid_block_size);
                }
                if (format == OutArchiveFormat.Zip && has_password)
                    szc.ZipEncryptionMethod = ZipEncryptionMethod.Aes256;
                if (use_dictionary)
                {
                    Dictionary<string, string> entries = new Dictionary<string, string>();
                    foreach (var group in groups)
                        foreach (var j in group.Paths)
                        {
                            string name = j.Substring(group.RootLength);
                            if (!entries.ContainsKey(name))
                                entries.Add(name, Directory.Exists(j) ? null : j);
                        }
                    using (ControlledStream output = new ControlledStream(new FileStream(path, FileMode.Create, FileAccess.ReadWrite), this))
                        szc.CompressFileDictionary(entries, output, password);
                    result_notes.Add("The items come from different folders and the file names are encrypted, so the dates of the files could not be kept (they have the time of compressing).");
                    return;
                }
                string[] paths = groups[i].Paths.ToArray();
                if (i == 0)
                {
                    using (ControlledStream output = new ControlledStream(new FileStream(path, FileMode.Create, FileAccess.ReadWrite), this))
                    {
                        if (has_password)
                            szc.CompressFilesEncrypted(output, groups[i].RootLength, password, paths);
                        else
                            szc.CompressFiles(output, groups[i].RootLength, paths);
                    }
                }
                else
                {
                    SetPhase("Adding:", total_size, false);
                    szc.CompressionMode = CompressionMode.Append;
                    if (has_password)
                        szc.CompressFilesEncrypted(path, groups[i].RootLength, password, paths);
                    else
                        szc.CompressFiles(path, groups[i].RootLength, paths);
                }
            }
        }

        void SplitFile(string source, string target, long volume_size, List<string> volumes)
        {
            byte[] buffer = new byte[1048576];
            using (FileStream input = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                long length = input.Length, done = 0;
                int number = 0, last = -1;
                do
                {
                    number++;
                    string part = target + "." + number.ToString("000") + ".partial";
                    volumes.Add(part);
                    using (FileStream output = new FileStream(part, FileMode.Create, FileAccess.Write))
                    {
                        long written = 0;
                        while (written < volume_size)
                        {
                            CheckState();
                            int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, volume_size - written));
                            if (read <= 0)
                                break;
                            output.Write(buffer, 0, read);
                            written += read;
                            done += read;
                            int value = length > 0 ? (int)(done * 100 / length) : 100;
                            if (value != last)
                            {
                                last = value;
                                Report(value);
                            }
                        }
                    }
                }
                while (done < length);
            }
        }

        #endregion

        #region Extract

        static void CollectIndexes(CompressItem node, Dictionary<int, string> entries, Dictionary<int, string> indexes, Dictionary<int, string> nameless, List<string> unsafe_entries, ref long size)
        {
            if (node.Indexes != null)
                foreach (int i in node.Indexes)
                {
                    string name;
                    if (!entries.TryGetValue(i, out name) || indexes.ContainsKey(i) || nameless.ContainsKey(i))
                        continue;
                    if (entries.Count == 1 && !node.IsDirectory && (name == "" || name == "[no name]"))
                        nameless.Add(i, node.FilePath);
                    else if (!IsSafeEntry(name))
                    {
                        unsafe_entries.Add(name);
                        continue;
                    }
                    else
                        indexes.Add(i, name);
                    if (!node.IsDirectory && node.Info.HasValue)
                        size += (long)node.Info.Value.Size;
                }
            if (node.IsDirectory && node.Children != null)
                foreach (var i in node.Children.Values)
                    CollectIndexes(i, entries, indexes, nameless, unsafe_entries, ref size);
        }

        internal bool StartExtracting(string archive, string password, string extract_path, List<CompressItem> nodes, Dictionary<int, string> entries, bool show_result)
        {
            progress_brush = Brushes.DarkGoldenrod;
            progress_bar.Foreground = progress_brush;
            Title = "Extracting " + Path.GetFileName(archive);
            operation_title.Text = "Extracting:";
            archive_name.ToolTip = archive_name.Text = archive;
            speed_text.Text = "";
            time_text.Text = "";
            if (!Functions.PrepareSevenZip())
                return false;
            Dictionary<int, string> indexes = new Dictionary<int, string>(), nameless = new Dictionary<int, string>();
            List<string> unsafe_entries = new List<string>(), broken = new List<string>();
            long size = 0;
            foreach (var i in nodes)
                CollectIndexes(i, entries, indexes, nameless, unsafe_entries, ref size);
            string staging;
            try
            {
                Directory.CreateDirectory(extract_path);
                staging = Path.Combine(extract_path, ".fm-extract-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                DirectoryInfo staging_info = Directory.CreateDirectory(staging);
                staging_info.Attributes |= FileAttributes.Hidden;
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not extract to this directory.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, extract_path + "\n" + ex.Message);
                return false;
            }
            total_size = size;
            bool staging_left = false;
            RunOperation(() =>
            {
                HashSet<int> started = new HashSet<int>(), finished = new HashSet<int>();
                Stream stream = null;
                SevenZipExtractor sze = null;
                try
                {
                    if (indexes.Count + nameless.Count > 0)
                    {
                        sze = OpenExtractor(archive, password, out stream);
                        sze.EventSynchronization = EventSynchronizationStrategy.AlwaysSynchronous;
                        Dictionary<int, string> current = new Dictionary<int, string>();
                        foreach (var i in sze.ArchiveFileData)
                            current[i.Index] = i.FileName;
                        foreach (var i in indexes)
                        {
                            string name;
                            if (!current.TryGetValue(i.Key, out name) || name != i.Value)
                                throw new InvalidDataException("The archive was changed after it was opened, open it again and retry.");
                        }
                        if (nameless.Keys.Any(x => !current.ContainsKey(x)) || current.Count != entries.Count)
                            throw new InvalidDataException("The archive was changed after it was opened, open it again and retry.");
                        sze.Extracting += (object sender, ProgressEventArgs e) =>
                        {
                            WaitIfPaused();
                            Report(e.PercentDone);
                        };
                        sze.FileExtractionStarted += (object sender, FileInfoEventArgs e) =>
                        {
                            WaitIfPaused();
                            current_file = e.FileInfo.FileName;
                            if (stop_operation)
                                e.Cancel = true;
                            else if (!e.FileInfo.IsDirectory)
                                started.Add(e.FileInfo.Index);
                        };
                        sze.FileExtractionFinished += (object sender, FileInfoEventArgs e) =>
                        {
                            finished.Add(e.FileInfo.Index);
                        };
                        if (indexes.Count > 0)
                            sze.ExtractFiles(staging, indexes.Keys.OrderBy(x => x).ToArray());
                        foreach (var i in nameless)
                        {
                            CheckState();
                            string target = Path.Combine(staging, DiskPath(i.Value));
                            Directory.CreateDirectory(Path.GetDirectoryName(target));
                            using (FileStream output = new FileStream(target, FileMode.Create, FileAccess.Write))
                                sze.ExtractFile(i.Key, output);
                            if (i.Value.Length > 0 && sze.ArchiveFileData.Count == 1)
                                try { File.SetLastWriteTime(target, sze.ArchiveFileData[0].LastWriteTime); }
                                catch (Exception) { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!stop_operation)
                        result_error = ex.Message;
                }
                finally { CloseExtractor(sze, stream); }
                foreach (int i in started)
                    if (!finished.Contains(i))
                    {
                        string name = nameless.ContainsKey(i) ? nameless[i] : indexes.ContainsKey(i) ? indexes[i] : null;
                        if (name == null)
                            continue;
                        broken.Add(name);
                        TryDeleteFile(Path.Combine(staging, DiskPath(name)));
                    }
                if (!stop_operation)
                {
                    SetPhase("Moving files:", 0, true);
                    foreach (var i in nodes)
                        try { MoveNode(i, staging, extract_path); }
                        catch (Exception ex) { move_errors.Add(i.FileName + ": " + ex.Message); }
                }
                staging_left = !TryDeleteDirectory(staging);
            });
            broken_count = broken.Count;
            StringBuilder details = new StringBuilder();
            if (broken.Count > 0)
                details.AppendLine(string.Format("{0} file{1} could not be extracted correctly (wrong password or damaged archive), {2} not written:\n", broken.Count, broken.Count > 1 ? "s" : "", broken.Count > 1 ? "they were" : "it was") + string.Join("\n", broken.Take(20).ToArray()) + (broken.Count > 20 ? "\n..." : ""));
            if (move_errors.Count > 0)
                details.AppendLine(string.Format("{0} item{1} could not be written:\n", move_errors.Count, move_errors.Count > 1 ? "s" : "") + string.Join("\n", move_errors.Take(20).ToArray()) + (move_errors.Count > 20 ? "\n..." : ""));
            if (unsafe_entries.Count > 0)
                details.AppendLine(string.Format("{0} item{1} with unsafe paths (outside the target directory) {2} not extracted:\n", unsafe_entries.Count, unsafe_entries.Count > 1 ? "s" : "", unsafe_entries.Count > 1 ? "were" : "was") + string.Join("\n", unsafe_entries.Take(20).ToArray()));
            if (result_notes.Count > 0)
                details.AppendLine("Files that already existed were kept, the new files got new names:\n" + string.Join("\n", result_notes.Take(20).ToArray()) + (result_notes.Count > 20 ? "\n..." : ""));
            if (staging_left)
                details.AppendLine("The temporary folder could not be deleted: " + staging);
            string counts = string.Format("{0} file{1} extracted", moved_files, moved_files == 1 ? "" : "s") + (replaced_files > 0 ? string.Format(", {0} replaced", replaced_files) : "") + (skipped_files > 0 ? string.Format(", {0} skipped", skipped_files) : "") + ".";
            bool failed = result_error != null || broken.Count > 0 || move_errors.Count > 0;
            if (failed)
                Messages.ShowMessage("We can not extract the archive correctly.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, (result_error != null ? result_error + "\n" : "") + details + counts);
            else if (stop_operation)
                Messages.ShowMessage("Extracting was stopped.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, details + counts);
            else if (show_result || details.Length > 0)
                Messages.ShowMessage("Extracting done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, extract_path + "\n" + counts + "\n" + details);
            return !failed && !stop_operation;
        }

        void MoveNode(CompressItem node, string staging, string destination)
        {
            if (stop_operation)
                return;
            string source = Path.Combine(staging, DiskPath(node.FilePath)), target = Path.Combine(destination, SafeName(node.FileName));
            if (node.IsDirectory)
            {
                if (Directory.Exists(source))
                    MoveDirectory(source, target);
            }
            else if (File.Exists(source))
                MoveFile(source, target);
        }

        void MoveDirectory(string source, string target)
        {
            if (stop_operation)
                return;
            current_file = target;
            if (File.Exists(target))
            {
                string new_target = UniqueName(target, false);
                result_notes.Add(target + " -> " + Path.GetFileName(new_target));
                target = new_target;
            }
            if (!Directory.Exists(target))
            {
                int count = Directory.GetFiles(source, "*", SearchOption.AllDirectories).Length;
                try
                {
                    Directory.Move(source, target);
                    moved_files += count;
                    return;
                }
                catch (Exception) { }
                Directory.CreateDirectory(target);
            }
            foreach (string i in Directory.GetFiles(source))
                MoveFile(i, Path.Combine(target, Path.GetFileName(i)));
            foreach (string i in Directory.GetDirectories(source))
                MoveDirectory(i, Path.Combine(target, Path.GetFileName(i)));
        }

        void MoveFile(string source, string target)
        {
            if (stop_operation)
                return;
            current_file = target;
            try
            {
                if (File.Exists(target) || Directory.Exists(target))
                {
                    int answer = Directory.Exists(target) ? 3 : AskConflict(target, source);
                    if (answer == 0)
                    {
                        stop_operation = true;
                        return;
                    }
                    if (answer == 2)
                    {
                        skipped_files++;
                        return;
                    }
                    if (answer == 3)
                    {
                        string new_target = UniqueName(target, true);
                        result_notes.Add(target + " -> " + Path.GetFileName(new_target));
                        target = new_target;
                    }
                    else
                    {
                        File.SetAttributes(target, FileAttributes.Normal);
                        File.Delete(target);
                        replaced_files++;
                    }
                }
                File.Move(source, target);
                moved_files++;
            }
            catch (Exception ex) { move_errors.Add(target + ": " + ex.Message); }
        }

        int AskConflict(string target, string source)
        {
            if (conflict_for_all != 0)
                return conflict_for_all;
            string details = "";
            try
            {
                FileInfo old_file = new FileInfo(target), new_file = new FileInfo(source);
                details = string.Format("Existing file: {0}, modified {1}\nFile in the archive: {2}, modified {3}", Functions.FileSize(old_file.Length, 2), old_file.LastWriteTime, Functions.FileSize(new_file.Length, 2), new_file.LastWriteTime);
            }
            catch (Exception) { }
            MyMessageBoxResult result = MyMessageBoxResult.None;
            Dispatcher.Invoke(new Action(() =>
            {
                result = Messages.ShowMessage(string.Format("'{0}' already exists in '{1}'.", Path.GetFileName(target), Path.GetDirectoryName(target)), details, new ButtonContent("Overwrite", ModrenMessageBitmapImage.Default, 1), new ButtonContent("Overwrite all", ModrenMessageBitmapImage.Default, 2), new ButtonContent("Skip", ModrenMessageBitmapImage.Skip, 3), new ButtonContent("Skip all", ModrenMessageBitmapImage.Skip, 4), new ButtonContent("Keep both", ModrenMessageBitmapImage.Default, 5), new ButtonContent("Keep both for all", ModrenMessageBitmapImage.Default, 6), new ButtonContent("Stop", ModrenMessageBitmapImage.Cancel, 7));
            }));
            switch (result)
            {
                case MyMessageBoxResult.Button1:
                    return 1;
                case MyMessageBoxResult.Button2:
                    conflict_for_all = 1;
                    return 1;
                case MyMessageBoxResult.Button3:
                    return 2;
                case MyMessageBoxResult.Button4:
                    conflict_for_all = 2;
                    return 2;
                case MyMessageBoxResult.Button5:
                    return 3;
                case MyMessageBoxResult.Button6:
                    conflict_for_all = 3;
                    return 3;
                default:
                    return 0;
            }
        }

        #endregion

        #region Test

        internal bool StartTesting(string archive, string password)
        {
            progress_brush = Brushes.SeaGreen;
            progress_bar.Foreground = progress_brush;
            Title = "Testing " + Path.GetFileName(archive);
            operation_title.Text = "Testing:";
            archive_name.ToolTip = archive_name.Text = archive;
            speed_text.Text = "";
            time_text.Text = "";
            if (!Functions.PrepareSevenZip())
                return false;
            List<string> failed = new List<string>();
            int tested = 0;
            bool check_ok = true;
            long size = 0;
            RunOperation(() =>
            {
                Stream stream = null;
                SevenZipExtractor sze = null;
                HashSet<int> finished = new HashSet<int>();
                List<ArchiveFileInfo> files = new List<ArchiveFileInfo>();
                try
                {
                    sze = OpenExtractor(archive, password, out stream);
                    sze.EventSynchronization = EventSynchronizationStrategy.AlwaysSynchronous;
                    foreach (var i in sze.ArchiveFileData)
                        if (!i.IsDirectory)
                        {
                            files.Add(i);
                            size += (long)i.Size;
                        }
                    SetPhase("Testing:", size, stream == null);
                    report_stream = stream != null;
                    sze.FileExtractionFinished += (object sender, FileInfoEventArgs e) =>
                    {
                        WaitIfPaused();
                        finished.Add(e.FileInfo.Index);
                        current_file = e.FileInfo.FileName;
                    };
                    check_ok = sze.Check();
                }
                catch (Exception ex)
                {
                    if (!stop_operation)
                        result_error = ex.Message;
                }
                finally
                {
                    report_stream = false;
                    CloseExtractor(sze, stream);
                }
                foreach (var i in files)
                    if (finished.Contains(i.Index))
                        tested++;
                    else
                        failed.Add(i.FileName);
                if (!check_ok && failed.Count == 0 && result_error == null && !stop_operation)
                    result_error = files.Count == 0 ? "The archive could not be read." : "7-Zip reported an error while reading the archive, it may be damaged or incomplete.";
            });
            broken_count = stop_operation ? 0 : failed.Count;
            if (stop_operation)
                Messages.ShowMessage("Testing was stopped.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Format("{0} file{1} were tested without errors before stopping.", tested, tested == 1 ? "" : "s"));
            else if (result_error != null || failed.Count > 0)
                Messages.ShowMessage("The archive has errors.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, (result_error != null ? result_error + "\n" : "") + (failed.Count > 0 ? string.Format("{0} of {1} file{2} failed (wrong password or damaged data):\n", failed.Count, failed.Count + tested, failed.Count + tested == 1 ? "" : "s") + string.Join("\n", failed.Take(20).ToArray()) + (failed.Count > 20 ? "\n..." : "") : ""));
            else
                Messages.ShowMessage("There are no errors in the archive.", ModrenMessageButton.OK, ModrenMessageImage.Tick, ModrenMessageSound.Information, true, string.Format("{0}\n{1} file{2}, {3}", archive, tested, tested == 1 ? "" : "s", Functions.FileSize(size, 2)));
            return result_error == null && failed.Count == 0 && !stop_operation;
        }

        #endregion

        #region Update

        internal bool StartUpdating(string archive, string password, OutArchiveFormat format, ArchiveUpdate update)
        {
            progress_brush = Brushes.SteelBlue;
            progress_bar.Foreground = progress_brush;
            Title = "Updating " + Path.GetFileName(archive);
            operation_title.Text = "Preparing:";
            archive_name.ToolTip = archive_name.Text = archive;
            speed_text.Text = "";
            time_text.Text = "";
            if (!Functions.PrepareSevenZip())
                return false;
            string folder = Path.GetDirectoryName(archive), work = Path.Combine(folder, ".fm-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "-" + Path.GetFileName(archive));
            bool done = false;
            int[] skipped = new int[1];
            RunOperation(() =>
            {
                try
                {
                    int before, after, removed = update.Changes.Count(x => x.Value == null);
                    using (SevenZipExtractor sze = string.IsNullOrEmpty(password) ? new SevenZipExtractor(archive) : new SevenZipExtractor(archive, password))
                        before = sze.ArchiveFileData.Count;
                    SetPhase("Preparing:", new FileInfo(archive).Length, false);
                    CopyFile(archive, work);
                    if (update.Changes.Count > 0)
                    {
                        CheckState();
                        SetPhase(removed == update.Changes.Count ? "Removing:" : update.Sources.Count == 0 ? "Renaming:" : "Preparing:", 0, true);
                        NewCompressor(folder, format).ModifyArchive(work, update.Changes);
                    }
                    if (update.Sources.Count > 0)
                    {
                        long size = 0;
                        int files_count = 0;
                        SetPhase("Reading files:", 0, true);
                        List<SourceGroup> groups = GroupSources(update.Sources, ref size, ref files_count, skipped);
                        SetPhase("Adding:", size, false);
                        foreach (var group in groups)
                        {
                            CheckState();
                            SevenZipCompressor szc = NewCompressor(folder, format);
                            szc.CompressionMode = CompressionMode.Append;
                            bool has_password = !string.IsNullOrEmpty(password) && format != OutArchiveFormat.Tar;
                            if (format == OutArchiveFormat.Zip && has_password)
                                szc.ZipEncryptionMethod = ZipEncryptionMethod.Aes256;
                            if (has_password)
                                szc.CompressFilesEncrypted(work, group.RootLength, password, group.Paths.ToArray());
                            else
                                szc.CompressFiles(work, group.RootLength, group.Paths.ToArray());
                        }
                        CheckState();
                        Dictionary<int, string> renames = new Dictionary<int, string>();
                        using (SevenZipExtractor sze = new SevenZipExtractor(work))
                            foreach (var i in sze.ArchiveFileData)
                            {
                                string name = i.FileName ?? "";
                                int split = name.IndexOf('\\');
                                string first = (split < 0 ? name : name.Substring(0, split)).ToLower(), rest = split < 0 ? "" : name.Substring(split), original;
                                if (update.FinalNames.ContainsKey(first))
                                {
                                    string new_name = (update.Target == "" ? "" : update.Target + "\\") + update.FinalNames[first] + rest;
                                    if (new_name != name)
                                        renames.Add(i.Index, new_name);
                                }
                                else if (update.Aside.TryGetValue(first, out original))
                                    renames.Add(i.Index, original + rest);
                            }
                        if (renames.Count > 0)
                        {
                            CheckState();
                            SetPhase("Renaming:", 0, true);
                            NewCompressor(folder, format).ModifyArchive(work, renames);
                        }
                    }
                    CheckState();
                    SetPhase("Checking:", 0, true);
                    using (SevenZipExtractor sze = string.IsNullOrEmpty(password) ? new SevenZipExtractor(work) : new SevenZipExtractor(work, password))
                        after = sze.ArchiveFileData.Count;
                    if ((update.Sources.Count == 0 && after != before - removed) || after < before - removed)
                        throw new InvalidDataException(string.Format("The updated archive has {0} items instead of {1}.", after, before - removed));
                    CheckState();
                    SetPhase("Saving:", 0, true);
                    ReplaceFile(work, archive);
                    done = true;
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    if (!stop_operation)
                        result_error = ex.Message;
                }
                finally
                {
                    TryDeleteFile(work);
                    TryDeleteFile(Path.Combine(folder, Path.GetFileName(work) + ".~"));
                }
            });
            if (result_error != null)
                Messages.ShowMessage("We can not update the archive.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, result_error + "\nThe archive was not changed.");
            else if (!done)
                Messages.ShowMessage("Updating was stopped, the archive was not changed.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else if (skipped[0] > 0 || update.Notes.Count > 0)
            {
                List<string> notes = new List<string>(update.Notes);
                if (skipped[0] > 0)
                    notes.Add(string.Format("{0} folder link{1} (junction or symbolic link) {2} not added.", skipped[0], skipped[0] > 1 ? "s" : "", skipped[0] > 1 ? "were" : "was"));
                Messages.ShowMessage("The archive was updated.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, string.Join("\n", notes.ToArray()));
            }
            return done;
        }

        #endregion

        private void Start_ThumbButtonInfo_Click(object sender, EventArgs e)
        {
            PlayPause_Click(null, null);
        }

        private void Stop_ThumbButtonInfo_Click(object sender, EventArgs e)
        {
            Stop_Click(null, null);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            stop_operation = true;
            archive_name.Text = "Stopping...";
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (operation_finished || stop_operation)
                return;
            pause_operation = !pause_operation;
            BitmapImage image = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/" + (pause_operation ? "copy-start.png" : "copy-pause.png")));
            play_pause_img.Source = image;
            play_pause_taskbar.ImageSource = image;
            play_pause_taskbar.Description = pause_operation ? "Resume this operation" : "Pause this operation";
            pause_button.ToolTip = pause_operation ? "Resume" : "Pause";
            TaskbarItemInfo.ProgressState = pause_operation ? TaskbarItemProgressState.Paused : TaskbarItemProgressState.Normal;
            if (pause_operation)
            {
                archive_name.Text = "Paused";
                speed_text.Text = "";
                time_text.Text = "";
            }
            last_time = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!operation_finished)
            {
                e.Cancel = true;
                if (!stop_operation && Messages.ShowMessage("Stop this operation?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.Yes)
                    Stop_Click(null, null);
            }
        }
    }

    class SourceGroup
    {
        internal int RootLength;
        internal List<string> Paths = new List<string>();
    }

    class ArchiveUpdate
    {
        internal Dictionary<int, string> Changes = new Dictionary<int, string>();
        internal List<string> Sources = new List<string>();
        internal string Target = "";
        internal Dictionary<string, string> FinalNames = new Dictionary<string, string>();
        internal Dictionary<string, string> Aside = new Dictionary<string, string>();
        internal List<string> Notes = new List<string>();
    }

    class ControlledStream : Stream
    {
        Stream stream;
        ArchiveOperation operation;
        long length = 0;

        public ControlledStream(Stream stream, ArchiveOperation operation)
        {
            this.stream = stream;
            this.operation = operation;
            if (stream.CanSeek && !stream.CanWrite)
                length = stream.Length;
        }

        public override bool CanRead
        {
            get { return stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return stream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return stream.CanWrite; }
        }

        public override long Length
        {
            get { return stream.Length; }
        }

        public override long Position
        {
            get { return stream.Position; }
            set { stream.Position = value; }
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            operation.CheckState();
            int read = stream.Read(buffer, offset, count);
            if (length > 0)
                operation.StreamMoved(stream.Position, length);
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            operation.CheckState();
            stream.Write(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
