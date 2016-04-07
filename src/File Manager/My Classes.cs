using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace File_Manager
{

    static class Functions
    {
        public static string StandardImageSource = "pack://application:,,,/File Manager;component/Images/Icons/", SystemDrive = Path.GetPathRoot(Environment.SystemDirectory).ToLower();

        public static int buffer = 16384 /*32768 / 2*/;

        static Window window;
        static string message;
        static bool cond;
        static double left;
        public static string[] spliter = { "\\" };

        public static string GetDownloadsPath()
        {
            string path = "";
            if (Environment.OSVersion.Version.Major >= 6)
            {
                IntPtr pathPtr;
                int hr = SHGetKnownFolderPath(ref FolderDownloads, 0, IntPtr.Zero, out pathPtr);
                if (hr == 0)
                {
                    path = Marshal.PtrToStringUni(pathPtr);
                    Marshal.FreeCoTaskMem(pathPtr);
                    return path;
                }
            }
            path = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            path = Path.Combine(path, "Downloads");
            return path;
        }

        private static Guid FolderDownloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetKnownFolderPath(ref Guid id, int flags, IntPtr token, out IntPtr path);

        public static string AppPath(string relative_path)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relative_path);
        }

        internal static string LoadFileInfo()
        {
            if (FileInfoCombonents.FilesInformation != null)
                return null;
            string file_path = AppPath("FileInfo.dat");
            if (!File.Exists(file_path))
                return "Can not find 'FileInfo.dat' File.";
            List<BasicFileInfoCombonents> info = null;
            try
            {
                using (Stream filestream = File.OpenRead(file_path))
                {
                    BinaryFormatter BF = new BinaryFormatter();
                    BF.Binder = new SafeBinder("System.Collections.Generic.List`1", "File_Manager.BasicFileInfoCombonents", "File_Manager.FileCategory", "System.Int32", "System.String", "System.Boolean");
                    info = (List<BasicFileInfoCombonents>)BF.Deserialize(filestream);
                }
            }
            catch (Exception)
            { return "Can not load 'FileInfo.dat' File."; }
            if (info == null || !BasicFileInfoCombonents.ConvertToStandard(info))
            {
                FileInfoCombonents.FilesInformation = null;
                return "The file 'FileInfo.dat' is not valid.";
            }
            return null;
        }

        internal static void LogError(Exception ex)
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "File Manager");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "errors.log"), DateTime.Now + "\r\n" + ex + "\r\n\r\n");
            }
            catch (Exception) { }
        }

        static object settings_lock = new object();

        static string SettingsFile()
        {
            return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "File Manager"), "settings.txt");
        }

        internal static string ReadSetting(string key)
        {
            try
            {
                lock (settings_lock)
                    if (File.Exists(SettingsFile()))
                        foreach (var i in File.ReadAllLines(SettingsFile()))
                            if (i.StartsWith(key + "="))
                                return i.Substring(key.Length + 1);
            }
            catch (Exception) { }
            return null;
        }

        internal static void SaveSetting(string key, string value)
        {
            try
            {
                lock (settings_lock)
                {
                    string file = SettingsFile(), temp = file + ".tmp";
                    List<string> lines = new List<string>();
                    try
                    {
                        if (File.Exists(file))
                            lines = File.ReadAllLines(file).Where(x => x.Contains('=') && !x.StartsWith(key + "=")).ToList();
                    }
                    catch (Exception) { }
                    lines.Add(key + "=" + value);
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllLines(temp, lines.ToArray());
                    if (File.Exists(file))
                        File.Replace(temp, file, null);
                    else
                        File.Move(temp, file);
                }
            }
            catch (Exception ex) { LogError(ex); }
        }

        internal static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url)); }
            catch (Exception ex)
            { Messages.ShowMessage("We can not open the link.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, url + "\n" + ex.Message); }
        }

        #region Links and protected paths

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        static extern bool FindClose(IntPtr hFindFile);

        internal static bool IsLink(FileSystemInfo item)
        {
            FileAttributes attributes;
            try { attributes = item.Attributes; }
            catch (Exception) { return false; }
            if ((int)attributes == -1 || !attributes.HasFlag(FileAttributes.ReparsePoint))
                return false;
            WIN32_FIND_DATA data;
            IntPtr handle = FindFirstFile(item.FullName.TrimEnd('\\'), out data);
            if (handle == new IntPtr(-1))
                return true;
            FindClose(handle);
            return (data.dwReserved0 & 0x20000000) != 0;
        }

        static void AddRiskPlace(List<KeyValuePair<string, string>> places, string path, string description)
        {
            if (!string.IsNullOrEmpty(path))
                try { places.Add(new KeyValuePair<string, string>(Path.GetFullPath(path).TrimEnd('\\').ToLower(), description)); }
                catch (Exception) { }
        }

        internal static string GetPathRisk(string path)
        {
            string full;
            try { full = Path.GetFullPath(path).TrimEnd('\\').ToLower(); }
            catch (Exception) { return null; }
            if (full.EndsWith(":"))
                return "the whole drive " + full.ToUpper() + "\\";
            List<KeyValuePair<string, string>> places = new List<KeyValuePair<string, string>>();
            AddRiskPlace(places, Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Windows itself (Windows may stop working or fail to start)");
            AddRiskPlace(places, Environment.GetEnvironmentVariable("ProgramW6432"), "installed programs");
            AddRiskPlace(places, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "installed programs");
            AddRiskPlace(places, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "installed programs");
            AddRiskPlace(places, AppDomain.CurrentDomain.BaseDirectory, "File Manager itself");
            foreach (var i in places)
            {
                if (full == i.Key)
                    return i.Value;
                if (i.Key.StartsWith(full + "\\"))
                    return "it contains " + i.Value;
                if (full.StartsWith(i.Key + "\\"))
                    return "a part of " + i.Value;
            }
            places.Clear();
            AddRiskPlace(places, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "settings and data of installed programs");
            string users = null;
            try { users = Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)); }
            catch (Exception) { }
            AddRiskPlace(places, users, "all user accounts and their files");
            foreach (var i in places)
            {
                if (full == i.Key)
                    return i.Value;
                if (i.Key.StartsWith(full + "\\"))
                    return "it contains " + i.Value;
            }
            if (users != null && string.Equals(Path.GetDirectoryName(full), users.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                return "a user account's folder (documents, desktop, settings)";
            string name = Path.GetFileName(full);
            if ((name == "system volume information" || name == "$recycle.bin") && Path.GetDirectoryName(full) != null && Path.GetDirectoryName(full).Length <= 3)
                return "system restore data or the Recycle Bin of the drive";
            return null;
        }

        internal static bool ConfirmRiskyItems(IEnumerable<string> paths, string action)
        {
            StringBuilder risks = new StringBuilder();
            int count = 0;
            foreach (string i in paths)
            {
                string risk = GetPathRisk(i);
                if (risk != null)
                {
                    count++;
                    if (count <= 10)
                        risks.AppendLine(i + "\n    (" + risk + ")");
                }
            }
            if (count == 0)
                return true;
            if (count > 10)
                risks.AppendLine(string.Format("... and {0} more.", count - 10));
            if (Messages.ShowMessage(string.Format("Warning: you are about to {0} protected system or user items.", action), ModrenMessageButton.YesNo, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, risks.ToString(), "This can break Windows, installed programs or user accounts and may not be undoable. Continue only if you are sure.") != MyMessageBoxResult.Yes)
                return false;
            return Messages.ShowMessage(string.Format("Last confirmation: {0} {1} protected item{2} at your own risk?", action, count, count > 1 ? "s" : ""), "", new ButtonContent("No, keep them (recommended)", ModrenMessageBitmapImage.Cancel, 1), new ButtonContent("Yes, " + action + " at my own risk", ModrenMessageBitmapImage.Default, 2)) == MyMessageBoxResult.Button2;
        }

        #endregion

        #region Delete and Recycle Bin

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        static bool HasRecycleBin(string path)
        {
            try { return new DriveInfo(Path.GetPathRoot(path)).DriveType == DriveType.Fixed; }
            catch (Exception) { return false; }
        }

        static List<string> SendToRecycleBin(List<FileSystemInfo> items, Window owner)
        {
            StringBuilder from = new StringBuilder();
            foreach (var i in items)
            {
                from.Append(i.FullName);
                from.Append('\0');
            }
            SHFILEOPSTRUCT operation = new SHFILEOPSTRUCT();
            operation.hwnd = owner == null ? IntPtr.Zero : new WindowInteropHelper(owner).Handle;
            operation.wFunc = 3;
            operation.pFrom = from.ToString();
            operation.fFlags = 0x40 | 0x10 | 0x4000;
            int result = -1;
            try { result = SHFileOperation(ref operation); }
            catch (Exception) { }
            List<string> failed = new List<string>();
            foreach (var i in items)
            {
                i.Refresh();
                if (i.Exists)
                    failed.Add(i.FullName + (result != 0 ? string.Format("\n    (error code {0})", result) : ""));
            }
            return failed;
        }

        internal static bool DeleteItems(List<FileSystemInfo> items, bool permanent, Window owner)
        {
            List<string> paths = new List<string>();
            foreach (var i in items)
                paths.Add(i.FullName);
            if (!ConfirmRiskyItems(paths, permanent ? "permanently delete" : "delete"))
                return false;
            List<FileSystemInfo> to_delete = new List<FileSystemInfo>(), to_recycle = new List<FileSystemInfo>(), no_bin = new List<FileSystemInfo>();
            List<string> failed = new List<string>();
            foreach (var i in items)
            {
                if (i is DirectoryInfo && IsLink(i))
                {
                    try { Directory.Delete(i.FullName, false); }
                    catch (Exception ex) { failed.Add(i.FullName + "\n    (" + ex.Message + ")"); }
                }
                else if (permanent)
                    to_delete.Add(i);
                else if (HasRecycleBin(i.FullName))
                    to_recycle.Add(i);
                else
                    no_bin.Add(i);
            }
            if (no_bin.Count > 0)
            {
                StringBuilder names = new StringBuilder();
                foreach (var i in no_bin)
                    names.AppendLine(i.FullName);
                if (Messages.ShowMessage(string.Format("{0} item{1} on a drive without a Recycle Bin (removable or network drive), delete permanently?", no_bin.Count, no_bin.Count > 1 ? "s are" : " is"), ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Exclamation, true, names.ToString()) == MyMessageBoxResult.Yes)
                    to_delete.AddRange(no_bin);
            }
            if (to_recycle.Count > 0)
                failed.AddRange(SendToRecycleBin(to_recycle, owner));
            if (to_delete.Count > 0)
            {
                List<FileOperationItem> list = new List<FileOperationItem>();
                foreach (var i in to_delete)
                    if (i is DirectoryInfo)
                        list.Add(new FileOperationItem((DirectoryInfo)i, false, ""));
                    else
                        list.Add(new FileOperationItem((FileInfo)i, false, ""));
                new Operation().CopyDialogShow(list, OperationType.Delete, "", true);
            }
            if (failed.Count > 0)
                Messages.ShowMessage(string.Format("{0} item{1} not deleted.", failed.Count, failed.Count > 1 ? "s were" : " was"), ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", failed.ToArray()));
            return true;
        }

        internal static bool ConfirmDelete(List<FileSystemInfo> items, bool permanent)
        {
            string action = permanent ? "permanently delete" : "move to the Recycle Bin";
            if (items.Count > 1)
            {
                StringBuilder items_name = new StringBuilder();
                for (int i = 0; i < items.Count && i < 30; i++)
                    items_name.AppendLine(items[i].FullName);
                if (items.Count > 30)
                    items_name.AppendLine(string.Format("... and {0} more.", items.Count - 30));
                if (!permanent)
                    items_name.AppendLine("\nHold Shift while deleting to delete permanently.");
                return Messages.ShowMessage(string.Format("Are you sure you want to {0} these {1} items?", action, items.Count), ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, items_name.ToString()) == MyMessageBoxResult.Yes;
            }
            return Messages.ShowMessage(string.Format("Are you sure you want to {0} this item?", action), ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, Messages.GetItemInfo(items[0]) + (permanent ? "" : "\nHold Shift while deleting to delete permanently.")) == MyMessageBoxResult.Yes;
        }

        internal static void CreateNewFile(string path, string extension)
        {
            extension = extension.ToLower();
            if (extension == ".zip")
            {
                File.WriteAllBytes(path, new byte[] { 0x50, 0x4B, 0x05, 0x06, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
                return;
            }
            if (extension == ".txt" || extension == ".htm" || extension == ".html")
            {
                File.WriteAllBytes(path, new byte[0]);
                return;
            }
            using (RegistryKey extension_key = Registry.ClassesRoot.OpenSubKey(extension))
            {
                if (extension_key != null)
                {
                    List<RegistryKey> places = new List<RegistryKey>();
                    object prog_id = extension_key.GetValue("");
                    if (prog_id != null)
                        places.Add(extension_key.OpenSubKey(prog_id + "\\ShellNew"));
                    places.Add(extension_key.OpenSubKey("ShellNew"));
                    foreach (var i in places)
                        if (i != null)
                            using (i)
                            {
                                object data = i.GetValue("Data");
                                object template = i.GetValue("FileName");
                                if (template != null)
                                {
                                    string template_path = Environment.ExpandEnvironmentVariables(template.ToString());
                                    if (!Path.IsPathRooted(template_path))
                                        template_path = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "ShellNew"), template_path);
                                    if (File.Exists(template_path))
                                    {
                                        File.Copy(template_path, path);
                                        return;
                                    }
                                }
                                else if (data is byte[])
                                {
                                    File.WriteAllBytes(path, (byte[])data);
                                    return;
                                }
                                else if (data is string)
                                {
                                    File.WriteAllText(path, (string)data);
                                    return;
                                }
                                else if (i.GetValueNames().Contains("NullFile"))
                                {
                                    File.WriteAllBytes(path, new byte[0]);
                                    return;
                                }
                            }
                }
            }
            throw new IOException("Windows has no template for '" + extension + "' files on this computer, install a program that creates them first.");
        }

        internal static bool PermanentDeleteKey()
        {
            return Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        }

        #endregion

        #region Third party programs

        public static string SevenZipDownloadLink = "https://www.7-zip.org/download.html", NotepadPlusPlusDownloadLink = "https://notepad-plus-plus.org/downloads/", QemuDownloadLink = "https://www.qemu.org/download/#windows", HelpLink = "https://github.com/Mohammad-Diab/File-Manager", ReleasesLink = "https://github.com/Mohammad-Diab/File-Manager/releases", LicenseLink = "https://github.com/Mohammad-Diab/File-Manager/blob/main/LICENSE";
        static string seven_zip_library = null;

        static string ReadRegistryValue(RegistryHive hive, RegistryView view, string key_name, string value_name)
        {
            try
            {
                using (RegistryKey base_key = RegistryKey.OpenBaseKey(hive, view))
                using (RegistryKey key = base_key.OpenSubKey(key_name))
                    if (key != null)
                    {
                        object value = key.GetValue(value_name);
                        if (value != null)
                            return value.ToString().Trim('"');
                    }
            }
            catch (Exception) { }
            return null;
        }

        static string FirstExisting(List<string> places)
        {
            foreach (var i in places)
                try
                {
                    if (!string.IsNullOrEmpty(i) && File.Exists(i))
                        return Path.GetFullPath(i);
                }
                catch (Exception) { }
            return null;
        }

        internal static string FindSevenZipLibrary()
        {
            List<string> places = new List<string>();
            places.Add(AppPath((Environment.Is64BitProcess ? "x64" : "x86") + "\\7z.dll"));
            RegistryView view = Environment.Is64BitProcess ? RegistryView.Registry64 : RegistryView.Registry32;
            string folder = ReadRegistryValue(RegistryHive.LocalMachine, view, @"SOFTWARE\7-Zip", Environment.Is64BitProcess ? "Path64" : "Path");
            if (folder == null)
                folder = ReadRegistryValue(RegistryHive.LocalMachine, view, @"SOFTWARE\7-Zip", "Path");
            if (folder != null)
                places.Add(Path.Combine(folder, "7z.dll"));
            places.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip\\7z.dll"));
            return FirstExisting(places);
        }

        internal static bool PrepareSevenZip()
        {
            if (seven_zip_library != null)
                return true;
            string library = FindSevenZipLibrary();
            if (library == null)
            {
                if (Messages.ShowMessage("'7-Zip' library (7z.dll) is missing.", ModrenMessageButton.YesNo, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Format("File Manager needs 7-Zip ({0}-bit) to open and create archives, install it from the official 7-Zip site then try again.\n\nOpen the download page now?", Environment.Is64BitProcess ? 64 : 32)) == MyMessageBoxResult.Yes)
                    OpenUrl(SevenZipDownloadLink);
                return false;
            }
            try
            {
                SevenZip.SevenZipBase.SetLibraryPath(library);
                seven_zip_library = library;
                return true;
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not load '7-Zip' library.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, library + "\n" + ex.Message);
                return false;
            }
        }

        internal static string FindProgram(string exe_name, string folder_name, string registry_key, string registry_value)
        {
            List<string> places = new List<string>();
            string[] roots = { Environment.GetEnvironmentVariable("ProgramW6432"), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) };
            foreach (var i in roots)
                if (!string.IsNullOrEmpty(i))
                    places.Add(Path.Combine(Path.Combine(i, folder_name), exe_name));
            foreach (RegistryView view in new RegistryView[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                if (registry_key != null)
                {
                    string folder = ReadRegistryValue(RegistryHive.LocalMachine, view, registry_key, registry_value);
                    if (folder != null)
                        places.Add(folder.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? folder : Path.Combine(folder, exe_name));
                }
                places.Add(ReadRegistryValue(RegistryHive.LocalMachine, view, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exe_name, ""));
                places.Add(ReadRegistryValue(RegistryHive.CurrentUser, view, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exe_name, ""));
            }
            string path_variable = Environment.GetEnvironmentVariable("PATH");
            if (path_variable != null)
                foreach (var i in path_variable.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    try { places.Add(Path.Combine(i.Trim().Trim('"'), exe_name)); }
                    catch (Exception) { }
            return FirstExisting(places);
        }

        internal static string FindQemu(string exe_name)
        {
            string path = FindProgram(exe_name, "qemu", @"SOFTWARE\QEMU", "Install_Dir");
            if (path == null)
                path = FindProgram(exe_name, "qemu", @"SOFTWARE\QEMU", "InstallLocation");
            if (path == null && Messages.ShowMessage("'QEMU' is not installed.", ModrenMessageButton.YesNo, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Format("This tool needs QEMU ('{0}'), install QEMU for Windows from the official QEMU site then try again.\n\nOpen the download page now?", exe_name)) == MyMessageBoxResult.Yes)
                OpenUrl(QemuDownloadLink);
            return path;
        }

        #endregion

        #region Files helpers

        internal static string HashFile(string path, HashAlgorithm hasher)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return BitConverter.ToString(hasher.ComputeHash(stream)).Replace("-", "");
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetFileInformationByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        internal static string GetFileId(string path)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    BY_HANDLE_FILE_INFORMATION info;
                    if (GetFileInformationByHandle(stream.SafeFileHandle, out info))
                        return info.VolumeSerialNumber + ":" + info.FileIndexHigh + ":" + info.FileIndexLow;
                }
            }
            catch (Exception) { }
            return null;
        }

        internal static string GetCopyName(string folder, string name, bool is_file)
        {
            string extention = is_file ? Path.GetExtension(name) : "";
            string base_name = is_file ? Path.GetFileNameWithoutExtension(name) : name;
            string new_name = base_name + " - Copy" + extention;
            int k = 2;
            while (File.Exists(Path.Combine(folder, new_name)) || Directory.Exists(Path.Combine(folder, new_name)))
                new_name = base_name + " - Copy (" + k++ + ")" + extention;
            return new_name;
        }

        internal static List<string> MultiRenameNames(List<FileSystemInfo> items, FileSystemInfo reference, string new_name)
        {
            List<string> names = new List<string>();
            bool first_is_file = reference is FileInfo;
            string new_base = first_is_file ? Path.GetFileNameWithoutExtension(new_name) : new_name;
            string new_extention = first_is_file ? Path.GetExtension(new_name) : "";
            long number;
            if (items.Count > 1 && new_base.Length > 0 && new_base.All(char.IsDigit) && long.TryParse(new_base, out number))
            {
                foreach (var i in items)
                {
                    names.Add(number.ToString().PadLeft(new_base.Length, '0') + (i is FileInfo ? i.Extension : ""));
                    number++;
                }
            }
            else if (items.Count > 1 && first_is_file && string.Equals(Path.GetFileNameWithoutExtension(reference.Name), new_base, StringComparison.OrdinalIgnoreCase) && !string.Equals(reference.Extension, new_extention, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var i in items)
                    names.Add(i is FileInfo ? Path.GetFileNameWithoutExtension(i.Name) + new_extention : i.Name);
            }
            else
                foreach (var i in items)
                    names.Add(new_name);
            return names;
        }

        #endregion

        public static void MarkAs(OperationTree root, OperationOption option, bool over_curent_option)
        {
            foreach (var i in root.children)
                if (i.children == null)
                {
                    if (i.user_option == OperationOption.NoValue || over_curent_option)
                        i.user_option = option;
                }
                else
                {
                    if (i.user_option == OperationOption.NoValue || over_curent_option)
                        i.user_option = option;
                    MarkAs(i, option, over_curent_option);
                }
        }

        public static string OrginizeDate(DateTime date)
        {
            return string.Format("{0}/{1}/{2}  ", OrginizeNumber(date.Day, 2), OrginizeNumber(date.Month, 2), OrginizeNumber(date.Year, 4)) + string.Format("{0}:{1}", OrginizeNumber(date.Hour, 2), OrginizeNumber(date.Minute, 2));
        }

        static string OrginizeNumber(int number, int digits)
        {
            string N = number.ToString();
            while (N.Length < digits)
                N = "0" + N;
            return N;
        }

        public static void Rename(FileSystemInfo Item, string new_name, List<UnCompleatedRename> un_done)
        {
            try
            {
                int k = 0;
                if (Item is FileInfo)
                {
                    string extention = Item.Extension;
                    FileInfo source = (FileInfo)Item;
                    if (File.Exists(source.DirectoryName + "\\" + new_name + extention))
                        while (File.Exists(source.DirectoryName + "\\" + new_name + " (" + k + ")" + extention)) k++;
                    source.MoveTo(source.DirectoryName + "\\" + new_name + " (" + k + ")" + extention);
                }
                else
                {
                    DirectoryInfo source = (DirectoryInfo)Item;
                    if (Directory.Exists(source.Parent.FullName + "\\" + new_name))
                        while (File.Exists(source.Parent.FullName + "\\" + new_name + " (" + k + ")")) k++;
                    source.MoveTo(source.Parent.FullName + "\\" + new_name + " (" + k + ")");
                }
            }
            catch (Exception ex) { un_done.Add(new UnCompleatedRename(Item, new_name, ex.Message)); }
        }

        internal static BitmapImage GetDriveImage(DriveSnapshot drive, int width)
        {
            switch (drive.drive_type)
            {
                case DriveType.Fixed:
                    return Icons.GetIcons.GetIcon(IsSystemDrive(drive.drive.Name) ? "hard-disk-system.png" : "hard-disk.png", width);
                case DriveType.CDRom:
                    return Icons.GetIcons.GetIcon(drive.is_ready ? "CD.png" : "optical-drive.png", width);
                case DriveType.Removable:
                    return Icons.GetIcons.GetIcon(drive.is_ready ? "removable-usb.png" : "removable-media.png", width);
                case DriveType.Network:
                    return Icons.GetIcons.GetIcon("network-drive.png", width);
            }
            return Icons.GetIcons.GetIcon("hard-disk.png", width);
        }

        internal static bool IsSystemDrive(string name)
        {
            return string.Equals(name, Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase);
        }

        internal static string DriveTitle(DriveType type, string label, string name)
        {
            if (string.IsNullOrEmpty(label))
                switch (type)
                {
                    case DriveType.CDRom:
                        label = "Optical Disk";
                        break;
                    case DriveType.Fixed:
                        label = "Local Disk";
                        break;
                    case DriveType.Network:
                        label = "Network Drive";
                        break;
                    case DriveType.Ram:
                        label = "RAM Disk";
                        break;
                    case DriveType.Removable:
                        label = "Removable Disk";
                        break;
                    default:
                        label = "Drive";
                        break;
                }
            return label + " (" + name.TrimEnd('\\') + ")";
        }

        public static BitmapImage GetDriveImage(DriveInfo drive, int width)
        {
            switch (drive.DriveType)
            {
                case DriveType.Fixed:
                    if (IsSystemDrive(drive.Name))
                        return Icons.GetIcons.GetIcon("hard-disk-system.png", width);
                    else
                        return Icons.GetIcons.GetIcon("hard-disk.png", width);
                case DriveType.Network:
                    return Icons.GetIcons.GetIcon("network-drive.png", width);
                case DriveType.CDRom:
                    if (drive.IsReady)
                        return Icons.GetIcons.GetIcon("CD.png", width);
                    else
                        return Icons.GetIcons.GetIcon("optical-drive.png", width);
                case DriveType.Removable:
                    if (drive.IsReady)
                        return Icons.GetIcons.GetIcon("removable-usb.png", width);
                    else
                        return Icons.GetIcons.GetIcon("removable-media.png", width);
            }
            return Icons.GetIcons.GetIcon("hard-disk.png", width);
        }

        internal static string FileSize(double SizeInByte, int round)
        {
            if (SizeInByte < 1)
                return "0 byte";
            byte i = 0;
            while (SizeInByte / 1024 > 1)
            {
                SizeInByte = SizeInByte / 1024;
                i++;
            }
            return Math.Round(SizeInByte, round) + " " + FileSizeUnit(i, SizeInByte);
        }

        private static string FileSizeUnit(byte num, double size)
        {
            switch (num)
            {
                case 0:
                    return "byte" + (size == 1 ? "" : "s");
                case 1:
                    return "KiB";
                case 2:
                    return "MiB";
                case 3:
                    return "GiB";
                case 4:
                    return "TiB";
                case 5:
                    return "PiB";
                case 6:
                    return "EiB";
                case 7:
                    return "ZiB";
                case 8:
                    return "YiB";
                default:
                    return "byte";
            }
        }

        internal static string FileSize1000(double SizeInByte, int digits_count)
        {
            string unit;
            if (SizeInByte < 1)
                return "0";
            byte i = 0;
            while (SizeInByte / 1000 > 1)
            {
                SizeInByte /= 1000;
                i++;
            }
            unit = FileSizeUnit1000(i);
            int j = Convert.ToInt32(SizeInByte);
            if ((j + unit).Length > digits_count)
                digits_count = (j + unit).Length;
            if (digits_count > 2)
                return Math.Round(SizeInByte, digits_count - (j + unit).Length).ToString() + unit;
            else
                return Math.Round(SizeInByte, 0).ToString() + unit;
        }

        private static string FileSizeUnit1000(byte num)
        {
            switch (num)
            {
                case 0:
                    return "";
                case 1:
                    return "K";
                case 2:
                    return "M";
                case 3:
                    return "G";
                case 4:
                    return "T";
                case 5:
                    return "P";
                case 6:
                    return "E";
                case 7:
                    return "Z";
                case 8:
                    return "Y";
                default:
                    return "";
            }
        }

        internal static long CalcFileSizeOnDisk(FileInfo info)
        {
            int clusterSize = GetClusterSize(info.Directory.Root.FullName);
            uint hosize;
            uint losize = GetCompressedFileSizeW(info.FullName, out hosize);
            if (losize == 0xFFFFFFFF && Marshal.GetLastWin32Error() != 0)
                return info.Length;
            long size;
            size = (long)hosize << 32 | losize;
            if (clusterSize > 0)
                return ((size + (clusterSize) - 1) / (clusterSize)) * clusterSize;
            else
                return info.Length;
        }

        #region Get Size on Disk

        static Dictionary<string, int> cluster_sizes = new Dictionary<string, int>();

        static int GetClusterSize(string drive)
        {
            drive = drive.ToLower();
            lock (cluster_sizes)
            {
                if (cluster_sizes.ContainsKey(drive))
                    return cluster_sizes[drive];
                int clusterSize = 0;
                try
                {
                    using (var searcher = new ManagementObjectSearcher("select BlockSize,NumberOfBlocks from Win32_Volume WHERE DriveLetter = '" + drive.TrimEnd('\\') + "'"))
                    {
                        foreach (ManagementObject i in searcher.Get())
                        {
                            string bb = (i["BlockSize"]).ToString();
                            int.TryParse(bb, out clusterSize);
                            if (clusterSize == 0)
                                clusterSize = 4096;
                            break;
                        }
                    }
                }
                catch (Exception) { }
                cluster_sizes[drive] = clusterSize;
                return clusterSize;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName, [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        #endregion Get Size on Disk

        internal static string GetDriveDescription(DriveInfo drive)
        {
            string label = "";
            DriveType type = DriveType.Unknown;
            try
            {
                type = drive.DriveType;
                if (drive.IsReady)
                    label = drive.VolumeLabel;
            }
            catch (Exception) { }
            return DriveTitle(type, label, drive.Name);
        }

        static public bool IsRunningAsAdmin()
        {
            WindowsIdentity cur = WindowsIdentity.GetCurrent();
            foreach (IdentityReference role in cur.Groups)
            {
                if (role.IsValidTargetType(typeof(SecurityIdentifier)))
                {
                    SecurityIdentifier sid = (SecurityIdentifier)role.Translate(typeof(SecurityIdentifier));
                    if (sid.IsWellKnown(WellKnownSidType.AccountAdministratorSid) || sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
                        return true;
                }
            }
            return false;
        }



        public static string GetFileType(string extension)
        {
            if (FileInfoCombonents.FilesInformation.ContainsKey(extension.ToLower()))
                return FileInfoCombonents.FilesInformation[extension.ToLower()].Description;
            else if (extension.Length > 2)
                return extension.ToUpper().Remove(0, 1) + " File";
            else return "File";
        }

        public static int GetFileIndex(string extension)
        {
            if (extension.Length == 0)
                return 0;
            else if (extension[extension.Length - 1] == '_')
                extension = ".cab";
            extension = extension.ToLower();
            int first = 0;
            int last = FileInformation.file_extentions.Length - 1;
            int mid = 0;
            bool found = false;
            while (first <= last && !found)
            {
                mid = (first + last) / 2;
                if (String.Compare(FileInformation.file_extentions[mid], extension) == 0)
                    found = true;
                else
                    if (String.Compare(FileInformation.file_extentions[mid], extension) > 0)
                    last = mid - 1;
                else
                    first = mid + 1;
            }
            if (found) return mid;
            else return FileInformation.file_extentions.Length - 1;
        }

        public static FileCategory GetFileCategory(string extention)
        {
            if (FileInfoCombonents.FilesInformation.ContainsKey(extention.ToLower()))
                return FileInfoCombonents.FilesInformation[extention.ToLower()].Category;
            else
                return FileCategory.Unknown;
        }

        public static BitmapImage GetFileImage24(string extension)
        {
            if (FileInfoCombonents.FilesInformation.ContainsKey(extension.ToLower()))
                return FileInfoCombonents.FilesInformation[extension.ToLower()].Bitmap24;
            else
                return FileInfoCombonents.FilesInformation["."].Bitmap24;
        }

        internal static BitmapImage GetDirectoryImage(string path)
        {
            string icon = SpecialDirectoryIcon(path);
            return icon == null ? GetFileImage64("directory") : Icons.GetIcons.GetIcon(icon, 64);
        }

        static Dictionary<string, string> special_directory_icons = null;
        static object special_directory_lock = new object();

        internal static string SpecialDirectoryIcon(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            switch (path.ToLower())
            {
                case "user":
                    return "folder-user.png";
                case "desktop":
                    return "folder-desktop.png";
                case "downloads":
                    return "folder-downloads.png";
                case "documents":
                    return "folder-documents.png";
                case "music":
                    return "folder-music.png";
                case "pictures":
                    return "folder-pictures.png";
                case "videos":
                    return "folder-videos.png";
                case "programfile":
                    return "folder-programs.png";
                case "fonts":
                    return "folder-fonts.png";
                case "local":
                    return "folder-appdata.png";
                case "history":
                    return "folder-history.png";
            }
            lock (special_directory_lock)
            {
                if (special_directory_icons == null)
                {
                    special_directory_icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string[] directories = { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), GetDownloadsPath(),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                        Environment.GetFolderPath(Environment.SpecialFolder.Fonts), Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Environment.GetFolderPath(Environment.SpecialFolder.History) };
                    string[] icons = { "folder-user.png", "folder-desktop.png", "folder-downloads.png", "folder-documents.png", "folder-music.png", "folder-pictures.png", "folder-videos.png",
                        "folder-programs.png", "folder-programs.png", "folder-fonts.png", "folder-appdata.png", "folder-history.png" };
                    for (int i = 0; i < directories.Length; i++)
                        if (!string.IsNullOrEmpty(directories[i]) && !special_directory_icons.ContainsKey(directories[i].TrimEnd('\\')))
                            special_directory_icons[directories[i].TrimEnd('\\')] = icons[i];
                }
                string icon;
                return special_directory_icons.TryGetValue(path.TrimEnd('\\'), out icon) ? icon : null;
            }
        }

        internal static BitmapImage GetFileImage48(string extension)
        {
            if (FileInfoCombonents.FilesInformation.ContainsKey(extension.ToLower()))
                return FileInfoCombonents.FilesInformation[extension.ToLower()].Bitmap64;
            else
                return FileInfoCombonents.FilesInformation["."].Bitmap64;
        }

        internal static BitmapImage GetFileImage64(string extension)
        {
            if (FileInfoCombonents.FilesInformation.ContainsKey(extension.ToLower()))
                return FileInfoCombonents.FilesInformation[extension.ToLower()].Bitmap64;
            else
                return FileInfoCombonents.FilesInformation["."].Bitmap64;
        }

        internal static BitmapSource GetFileImage256(string extension)
        {
            if (FileInfoCombonents.FilesInformation.ContainsKey(extension.ToLower()))
                return FileInfoCombonents.FilesInformation[extension.ToLower()].Bitmap256;
            else
                return FileInfoCombonents.FilesInformation["."].Bitmap256;
        }





        public static void AddAllFiles(string directory, string parent_path, bool remove, List<FileOperationItem> list)
        {
            DirectoryInfo dir = new DirectoryInfo(directory);
            foreach (var i in dir.GetDirectories())
                try
                { AddAllFiles(i.FullName, parent_path, remove, list); }
                catch (Exception) { }
            foreach (var i in dir.GetFiles())
            {
                string file_short_name = i.FullName.Remove(0, parent_path.Length);
                if (parent_path == "")
                { file_short_name = file_short_name.Remove(1, 1); }
                /*list.Add(new FileOperationItem(i, remove, false));*/
            }
        }

        public static void AddAllFiles(string directory, List<string> list)
        {
            DirectoryInfo dir = new DirectoryInfo(directory);
            foreach (var i in dir.GetDirectories())
                AddAllFiles(i.FullName, list);
            foreach (var i in dir.GetFiles())
                list.Add(i.FullName);
        }

        public static void AddAllDirectories(string directory, List<string> list)
        {
            foreach (var i in Directory.GetDirectories(directory))
            {
                try
                {
                    AddAllDirectories(i, list);
                    list.Add(i);
                }
                catch (Exception) { }
            }
        }

        internal static string FormalDigits(int number, int digits)
        {
            string N = number.ToString();
            while (N.Length < digits)
                N = "0" + N;
            return N;
        }

        public static void OpenWith(string path, ItemType file_type)
        {
            if (file_type == ItemType.Directory || (file_type == ItemType.Drive))
                new MainWindow(path).Show();
            else if (file_type == ItemType.File)
            {
                var OFD = new System.Windows.Forms.OpenFileDialog();
                OFD.Filter = "Application (*.exe)|*.exe";
                if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    System.Diagnostics.Process.Start(new ProcessStartInfo(OFD.FileName, "\"" + path + "\""));
            }
        }

        internal static void ShowTool(Window window)
        {
            window.Show();
            window.Dispatcher.BeginInvoke(new Action(() => { ActivateTool(window); }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }

        internal static bool ShowOpenedTool<T>() where T : Window
        {
            T window = Application.Current.Windows.OfType<T>().FirstOrDefault(x => x.IsVisible);
            if (window == null)
                return false;
            window.Dispatcher.BeginInvoke(new Action(() => { ActivateTool(window); }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            return true;
        }

        static void ActivateTool(Window window)
        {
            if (!window.IsVisible)
                return;
            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            window.Activate();
            if (window.IsKeyboardFocusWithin && !(Keyboard.FocusedElement is Window))
                return;
            UIElement input = FirstInput(window);
            if (input != null)
                input.Focus();
            else
                window.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        static UIElement FirstInput(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                UIElement element = child as UIElement;
                if (element != null && !element.IsVisible)
                    continue;
                if (((child is TextBox && !((TextBox)child).IsReadOnly) || child is PasswordBox) && element.IsEnabled && element.Focusable)
                    return element;
                UIElement found = FirstInput(child);
                if (found != null)
                    return found;
            }
            return null;
        }

        public static ItemType GetItemType(string path)
        {
            if (path.Split(spliter, StringSplitOptions.RemoveEmptyEntries).Length == 1)
                return ItemType.Drive;
            if (File.Exists(path))
                return ItemType.File;
            else if (Directory.Exists(path))
                return ItemType.Directory;
            return ItemType.Unknown;
        }

        public static string[] SplitString(string str)
        {
            string[] strs = str.Split(spliter, StringSplitOptions.RemoveEmptyEntries);
            if (strs.Length > 2)
                for (int i = 1; i < strs.Length - 1; i++)
                    strs[i] = strs[i - 1] + "\\" + strs[i];
            return strs;
        }


        #region Shuck Window Functions

        public static void ShuckWindow(Window win, bool show_messege_condition, string msg)
        {
            window = win;
            message = msg;
            cond = show_messege_condition;
            left = window.Left;
            DoubleAnimation animation = new DoubleAnimation(window.Left, window.Left + 10, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
            window.BeginAnimation(Window.LeftProperty, animation);
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += bw_DoWork;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerAsync();
        }

        static void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(window.Left, window.Left - 20, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
            window.BeginAnimation(Window.LeftProperty, animation);
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += bw_DoWork;
            bw.ProgressChanged += bw_ProgressChanged2;
            bw.RunWorkerAsync();
        }

        private static void bw_ProgressChanged2(object sender, ProgressChangedEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(window.Left, window.Left + 20, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
            window.BeginAnimation(Window.LeftProperty, animation);
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += bw_DoWork;
            bw.ProgressChanged += bw_ProgressChanged3;
            bw.RunWorkerAsync();
        }

        private static void bw_ProgressChanged3(object sender, ProgressChangedEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(window.Left, window.Left - 20, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
            window.BeginAnimation(Window.LeftProperty, animation);
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += bw_DoWork;
            bw.ProgressChanged += bw_ProgressChanged4;
            bw.RunWorkerAsync();
        }

        private static void bw_ProgressChanged4(object sender, ProgressChangedEventArgs e)
        {
            DoubleAnimation animation = new DoubleAnimation(window.Left, window.Left + 10, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
            window.BeginAnimation(Window.LeftProperty, animation);
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += bw_DoWork;
            bw.ProgressChanged += bw_ProgressChanged5;
            bw.RunWorkerAsync();
        }

        private static void bw_ProgressChanged5(object sender, ProgressChangedEventArgs e)
        {
            if (cond)
                Messages.ShowMessage(message, ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
            window.Left = left;
            //button.IsEnabled = true;
        }

        static void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(50);
            ((BackgroundWorker)sender).ReportProgress(0);
        }

        #endregion




        internal static void GetAllFiles(DirectoryInfo root,BackgroundWorker adder,ref bool cancel_operation)
        {
            try
            {
                var sub_dirs = root.GetDirectories();
                foreach (var i in sub_dirs)
                {
                    if (cancel_operation) return;
                    if (IsLink(i)) continue;
                    try { GetAllFiles(i, adder, ref cancel_operation); } catch (Exception) { }
                }
            }
            catch (Exception) { }
            FileInfo[] files = null;
            try
            {
                files = root.GetFiles();
                foreach (var i in files)
                {
                    if (cancel_operation) return;
                    try { adder.ReportProgress(0, i); } catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

    }

    class GeneralPurpose
    {
        internal GeneralPurpose() { }

        public GeneralPurpose(FileInfo file, string file_path)
        {
            FullPath = file_path;//file name or file path;
            Image = Functions.GetFileImage24(file.Extension);
            File = file == null ? new FileInfo(file_path) : file;
            //isDirectory = false;
        }

        public GeneralPurpose(string directory_path, DirectoryInfo directory)
        {
            FullPath = directory_path;      //file name or file path;
            Image = Functions.GetFileImage24("directory");
            Directory = directory == null ? new DirectoryInfo(directory_path) : directory;
            //isDirectory = true;
        }

        private String file_path;
        public string FullPath
        {
            get { return file_path; }
            set { file_path = value; }
        }

        private BitmapImage img_source;
        public BitmapImage Image
        {
            get { return img_source; }
            set { img_source = value; }
        }

        public FileInfo File { get; set; }

        public DirectoryInfo Directory { get; set; }

        //public bool isDirectory;

        //public int CompareTo(object obj)
        //{
        //    GeneralPurpose x = (GeneralPurpose)obj;
        //    if (x.isDirectory)
        //        if (this.isDirectory)
        //            return (x.Directory.FullName.CompareTo(this.Directory.FullName));
        //        else
        //            return 1;
        //    else
        //        if (this.isDirectory)
        //            return -1;
        //        else
        //            return (x.File.FullName.CompareTo(this.File.FullName));
        //}

        //public override bool Equals(object obj)
        //{
        //    if (obj != null)
        //    {
        //        GeneralPurpose x = (GeneralPurpose)obj;
        //        if (x.isDirectory)
        //            if (this.isDirectory)
        //                return (x.Directory.FullName.Equals(this.Directory.FullName));
        //            else
        //                return false;
        //        else
        //            if (this.isDirectory)
        //                return false;
        //            else
        //                return (x.File.FullName.Equals(this.File.FullName));
        //    }
        //    return false;
        //}
    }

    enum SortItemsBy
    {
        Random = 0,
        Name = 1,
        Size = 2,
        Type = 3,
        DateCreation = 4,
        DateModified = 5,
        DateLastAccess = 6,

        //and More
        RandomDescending =100,
        NameDescending = 101,
        SizeDescending = 102,
        TypeDescending = 103,
        DateCreationDescending = 104,
        DateModifiedDescending = 105,
        DateLastAccessDescending = 106

    }



    public class FileInfoCombonents
    {
        string extention;
        public String Extention
        {
            get { return extention; }
            set { extention = value; }
        }

        string image_file;
        public String ImageFile
        {
            get { return image_file; }
            set { image_file = value; }
        }

        string description;
        public String Description
        {
            get { return description; }
            set { description = value; }
        }

        bool has_subtext;
        public Boolean HasSubtext
        {
            get { return has_subtext; }
            set { has_subtext = value; }
        }

        string subtext;
        public String Subtext
        {
            get { return subtext; }
            set { subtext = value; }
        }

        FileCategory category;
        public FileCategory Category
        {
            get { return category; }
            set { category = value; }
        }

        string category_s;
        public String CategoryS
        {
            get { return category_s; }
            set { category_s = value; }
        }

        bool may_danger;
        public Boolean MayDanger
        {
            get { return may_danger; }
            set { may_danger = value; }
        }

        List<int[]> ids;
        public List<Int32[]> IDs
        {
            get { return ids; }
            set { ids = value; }
        }

        int offset;
        public Int32 Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        BitmapImage file_bitmap16;
        public BitmapImage Bitmap16
        {
            get { return file_bitmap16; }
            set { file_bitmap16 = value; }
        }

        BitmapImage file_bitmap24;
        public BitmapImage Bitmap24
        {
            get { return file_bitmap24; }
            set { file_bitmap24 = value; }
        }

        BitmapImage file_bitmap64;
        public BitmapImage Bitmap64
        {
            get { return file_bitmap64; }
            set { file_bitmap64 = value; }
        }

        BitmapImage file_bitmap256;
        public BitmapImage Bitmap256
        {
            get { return file_bitmap256; }
            set { file_bitmap256 = value; }
        }

        static public Dictionary<string, FileInfoCombonents> FilesInformation;

        public FileInfoCombonents(string extention, string image_file, string description, bool has_subtext, string subtext, FileCategory category, bool may_danger)
        {
            Extention = extention;
            ImageFile = image_file;
            Description = description;
            HasSubtext = has_subtext;
            if (HasSubtext)
                Subtext = subtext;
            else
                Subtext = "-";
            Category = category;
            MayDanger = may_danger;
            IDs = new List<int[]>();
            //if (File.Exists(@"C:\Users\pal\Documents\Images\16\" + image_file))
            //    file_bitmap16 = new BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\16\" + image_file));
            //else
            file_bitmap16 = Icons.GetIcons.GetIcon(image_file, 16); //BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\16\file.png"));
            file_bitmap24 = Icons.GetIcons.GetIcon(image_file, 24);//new BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\24\" + image_file));
            file_bitmap64 = Icons.GetIcons.GetIcon(image_file, 64);//new BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\64\" + image_file));
            file_bitmap256 = Icons.GetIcons.GetIcon(image_file, 256); //new BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\256\" + image_file));
        }

        public FileInfoCombonents(string extention, string image_file, string description, bool has_subtext, string subtext, FileCategory category, bool may_danger, List<int[]> Ids,int offset)
        {
            Extention = extention;
            ImageFile = image_file;
            Description = description;
            HasSubtext = has_subtext;
            if (HasSubtext)
                Subtext = subtext;
            else
                Subtext = "-";
            Category = category;
            MayDanger = may_danger;
            IDs = Ids;
            Offset = offset;
            //if (File.Exists(@"C:\Users\pal\Documents\Images\16\" + image_file))
            //    file_bitmap16 = new BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\16\" + image_file));
            //else
            file_bitmap16 = Icons.GetIcons.GetIcon(image_file, 16); //BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\16\file.png"));
            file_bitmap24 = Icons.GetIcons.GetIcon(image_file, 24);//new BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\24\" + image_file));
            file_bitmap64 = Icons.GetIcons.GetIcon(image_file, 64);//new BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\64\" + image_file));
            file_bitmap256 = Icons.GetIcons.GetIcon(image_file, 256); //new BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\256\" + image_file));
        }
    }

    [Serializable]
    public class BasicFileInfoCombonents
    {
        public String Extention;
        public String ImageFile;
        public String Description;
        public Boolean HasSubtext;
        public String Subtext;
        public FileCategory Category;
        public Boolean MayDanger;
        public List<Int32[]> IDs;
        public Int32 Offset;

        public BasicFileInfoCombonents(string extention, string image_file, string description, bool has_subtext, string subtext, FileCategory category, bool may_danger)
        {
            Extention = extention;
            ImageFile = image_file;
            Description = description;
            HasSubtext = has_subtext;
            if (HasSubtext)
                Subtext = subtext;
            else
                Subtext = "-";
            Category = category;
            MayDanger = may_danger;
            IDs = new List<int[]>();
        }

        public BasicFileInfoCombonents(string extention, string image_file, string description, bool has_subtext, string subtext, FileCategory category, bool may_danger, List<string> ids, string offset)
        {
            Extention = extention;
            ImageFile = image_file;
            Description = description;
            HasSubtext = has_subtext;
            if (HasSubtext)
                Subtext = subtext;
            else
                Subtext = "-";
            Category = category;
            MayDanger = may_danger;
            IDs = new List<int[]>();
            char[] cc = { ' ' };
            foreach (var i in ids)
            {
                var chars = i.Split(cc, StringSplitOptions.RemoveEmptyEntries);
                int[] ID1 = new int[chars.Length];
                for (int j = 0; j < chars.Length; j++)
                {
                    if (chars[j][0] >= 48 && chars[j][0] <= 57)//number in ascii
                        ID1[j] = 16 * (chars[j][0] - 48);
                    else if (chars[j][0] >= 65 && chars[j][0] <= 70) //Hix value (A -> F) in asici
                        ID1[j] = 16 * (chars[j][0] - 55);
                    if (chars[j][1] >= 48 && chars[j][1] <= 57)//number in ascii
                        ID1[j] += (chars[j][1] - 48);
                    else if (chars[j][1] >= 65 && chars[j][1] <= 70) //Hix value (A -> F) in asici
                        ID1[j] += (chars[j][1] - 55);
                }
                IDs.Add(ID1);
            }
            int tmp = 0;
            if (!offset.Equals("-"))
                int.TryParse(offset, out tmp);
            Offset = tmp;
        }

        static public bool ConvertToStandard(List<BasicFileInfoCombonents> BasicList)
        {
            FileInfoCombonents.FilesInformation = new Dictionary<string, FileInfoCombonents>();
            try
            {
                foreach (var i in BasicList)
                    if (i.IDs.Count > 0)
                        FileInfoCombonents.FilesInformation.Add(i.Extention,new FileInfoCombonents(i.Extention, i.ImageFile, i.Description, i.HasSubtext, i.Subtext, i.Category, i.MayDanger, i.IDs, i.Offset));
                    else
                        FileInfoCombonents.FilesInformation.Add(i.Extention, new FileInfoCombonents(i.Extention, i.ImageFile, i.Description, i.HasSubtext, i.Subtext, i.Category, i.MayDanger));
            }
            catch (Exception) { return false; }
            return true;
        }
    }

    public class FileOperationItem /*: IComparable*/
    {
        //public bool? Over;
        public FileSystemInfo Item;
        //public long Size;
        public bool IsCut;
        //public bool Done;
        public object short_path;

        public FileOperationItem(FileInfo File, bool IsCut, object short_path)
        {
            Item = File;
            this.IsCut = IsCut;
            this.short_path = short_path;
        }

        public FileOperationItem(DirectoryInfo Directory, /*long DirectorySize,*/ bool IsCut/*, bool? Over*/, object short_path)
        {
            Item = Directory;
            //Size = DirectorySize;
            this.IsCut = IsCut;
            //Done = false;
            this.short_path = short_path;
            //this.Over = Over;
        }

        //public FileOperationItem(DirectoryInfo directoryInfo, string short_path)
        //{
        //    Item = directoryInfo;
        //    //Over = false;
        //    //Size = -1;
        //    IsCut = false;
        //    //Done = false;
        //    this.short_path = short_path;
        //}

        //public FileElement(string file_path, long file_Size, string extension, string short_name, bool remove, bool? over)
        //{
        //    this.extension = extension;
        //    files_full_name = file_path;
        //    file_short_name = short_name;
        //    files_size = file_Size;
        //    remove_file = remove;
        //    Done = false;
        //    this.over = over;
        //}

        //public int CompareTo(object obj)
        //{
        //    if (obj == null)
        //        return 1;
        //    return ((FileOperationItem)obj).Size.CompareTo(this.Size);
        //}
    }

    public class UnCompleatedOperation : IComparable
    {
        public FileOperationItem Item;
        public string ErrorMessage;

        public UnCompleatedOperation(FileOperationItem Item, string Message)
        {
            this.Item = Item;
            this.ErrorMessage = Message;
        }

        //public UnCompleatedOperation(DirectoryInfo Directory, long DirectorySize, bool RemoveFile, string Message)
        //{
        //    Item = Directory;
        //    this.Size = DirectorySize;
        //    IsCut = RemoveFile;
        //    this.ErrorMessage = Message;
        //}

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 0;
            if (((UnCompleatedOperation)obj).ErrorMessage == this.ErrorMessage)
                return 0;
            else if (((UnCompleatedOperation)obj).ErrorMessage.CompareTo(this.ErrorMessage) < 0)
                return -1;
            else
                return 1;
        }
    }

    public class UnCompleatedRename: IComparable
    {
        public FileSystemInfo Item;
        public string NewName;
        public string ErrorMessage;

        public UnCompleatedRename(FileSystemInfo File, string new_name, string Message)
        {
            Item = File;
            NewName = new_name;
            ErrorMessage = Message;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 0;
            if (((UnCompleatedOperation)obj).ErrorMessage == this.ErrorMessage)
                return 0;
            else if (((UnCompleatedOperation)obj).ErrorMessage.CompareTo(this.ErrorMessage) < 0)
                return -1;
            else
                return 1;
        }
    }

    

    #region All ListView Data Context Classes



    


    public class SystemMonitorListView
    {
        public SystemMonitorListView(Process p)
        {
            P = p;
            PID = p.Id.ToString();
            string title = "";
            try { title = p.MainWindowTitle; }
            catch (Exception) { }
            HasWindow = !title.Equals("");
            ProcessName = (title.Equals("") ? (p.ProcessName) : (p.ProcessName + " (" + title + ")"));
            try { Memory = Functions.FileSize(p.PrivateMemorySize64, 2); }
            catch (Exception) { Memory = ""; }
            try
            { cpu_d = p.TotalProcessorTime.TotalMilliseconds; }
            catch (Exception) { cpu_d = -1; }
            try { Threads = p.Threads.Count.ToString(); }
            catch (Exception) { Threads = ""; }
        }

        public void SetCPU(double old_milisecond, double total_milisecond)
        {
            if (cpu_d < 0 || total_milisecond <= 0)
                return;
            cpu = Math.Max(0, Math.Round((cpu_d - old_milisecond) * 100 / total_milisecond, 1)) + " %";
        }

        public bool HasWindow;

        string threads;
        public String Threads
        {
            get { return threads; }
            set { threads = value; }
        }

        public Process P;

        private String process_id;
        public string PID
        {
            get { return process_id; }
            set { process_id = value; }
        }

        private String process_name;
        public string ProcessName
        {
            get { return process_name; }
            set { process_name = value; }
        }

        private BitmapImage img_source;
        public BitmapImage Icon
        {
            get { return img_source; }
            set { img_source = value; }
        }

        string memory;
        public String Memory
        {
            get { return memory; }
            set { memory = value; }
        }

        double cpu_d;

        string cpu;
        public String CPU
        {
            get { return cpu; }
            set { cpu = value; }
        }

        ContextMenu menu;
        public ContextMenu Menu
        {
            get { return menu; }
            set { menu = value; }
        }
    }

    #endregion

    #region ENUMs

    public enum FileCategory
    {
        Drive = 0,
        Directory = 1,
        Text = 2,
        TextCanRunAsApplication = 3,
        Image = 4,
        Audio = 5,
        Vedio = 6,
        Html = 7,
        Archive = 8,
        DiskImage = 9,
        Font = 10,
        Document = 11,
        Application = 12,
        CannotOpen = 13,
        GameFile = 14,
        Unknown = 100
    }

    public enum ItemType
    {
        Computer = 0,
        Drive = 1,
        Directory = 2,
        File = 3,
        Unknown = 4
    }

    public enum MyMessageBoxResult
    {
        None = 0,
        Yes = 1,
        No = 2,
        OK = 3,
        Cancel = 4,
        Skip = 5,
        Rety = 6,
        Abort = 7,
        Button1 = 101,
        Button2 = 102,
        Button3 = 103,
        Button4 = 104,
        Button5 = 105,
        Button6 = 106,
        Button7 = 107,
        Button8 = 108,
        Button9 = 109,
        Button10 = 110,
        Button11 = 111,
    }

    #endregion

    public static class FileInformation
    {
        //Dictionary<string, FileInfoCombonents> file_info = new Dictionary<string, FileInfoCombonents>();
        public static string[] file_extentions = { ".", ".001", ".3ggp2", ".3gp", ".3gpp", ".7z", ".aac", ".ac3", ".acc", ".accdb", ".accdt", ".ace", ".adt", ".adts", ".amr", ".amv", ".arj", ".avi", ".bat", ".bin", ".bmp", ".bsh", ".bz2", ".bzip2", ".c", ".cab", ".chm", ".cmd", ".com", ".comic", ".cpio", ".cpl", ".cpp", ".cs", ".dat", ".db", ".deb", ".dib", ".djvu", ".dll", ".dmg", ".doc", ".docm", ".docx", ".dot", ".dotx", ".drv", ".epub", ".exe", ".fat", ".flv", ".fon", ".gif", ".gz", ".gzip", ".h", ".hdd", ".hdmov", ".hfs", ".hta", ".htm", ".html", ".ico", ".img", ".inc", ".inf", ".ini", ".iso", ".isz", ".jar", ".java", ".jfif", ".jpe", ".jpeg", ".jpg", ".js", ".jxr", ".kge", ".kgp", ".kml", ".lha", ".log", ".lzh", ".lzma", ".mdb", ".mdl", ".mht", ".mid", ".midi", ".mka", ".mkv", ".mobi", ".mou", ".mov", ".mp2", ".mp3", ".mp4", ".mp4v", ".mpeg", ".mpg", ".msc", ".msi", ".nfo", ".nrg", ".nt", ".ntfs", ".odbc", ".odp", ".ods", ".odt", ".ogg", ".ogm", ".ogv", ".opus", ".oxps", ".pas", ".pcx", ".pdf", ".php", ".php3", ".phtml", ".pl", ".plx", ".pm", ".pnf", ".png", ".pot", ".potx", ".pps", ".ppsx", ".ppt", ".pptm", ".pptx", ".ps", ".ps1", ".psd", ".py", ".pyw", ".ra", ".ram", ".rar", ".reg", ".rm", ".rpm", ".rtf", ".scr", ".sh", ".shtm", ".shtml", ".sql", ".squashfs", ".svg", ".swf", ".sys", ".tar", ".taz", ".tbz", ".tbz2", ".tex", ".tga", ".tgz", ".tif", ".tiff", ".tpz", ".ttc", ".ttf", ".txt", ".txz", ".uha", ".url", ".v", ".vb", ".vbs", ".vdi", ".vhd", ".vhdl", ".vob", ".wav", ".wdp", ".wim", ".wma", ".wmv", ".wsdl", ".wv", ".xar", ".xhtml", ".xls", ".xlsb", ".xlsm", ".xlsx", ".xlt", ".xltm", ".xltx", ".xml", ".xps", ".xsd", ".xsl", ".xsml", ".xz", ".yml", ".z", ".zip", "directory", "unknown" };
        public static string[] file_image = { "file.png", "archive-file.png", "multimedia-video.png", "multimedia-video.png", "multimedia-video.png", "archive-7z.png", "multimedia-audio.png", "multimedia-audio.png", "multimedia-audio.png", "text-database.png", "text-database.png", "archive-ace.png", "multimedia-video.png", "multimedia-video.png", "multimedia-audio.png", "multimedia-video.png", "archive-file.png", "multimedia-video.png", "text-x.png", "disk-iso.png", "multimedia-bmp.png", "text-file.png", "archive-bz2.png", "archive-bz2.png", "text-c.png", "archive-cab.png", "text-help.png", "text-x.png", "text-x.png", "text-file.png", "archive-file.png", "x-dll.png", "text-c++.png", "text-cs.png", "file.png", "x-sys.png", "archive-deb.png", "multimedia-image.png", "text-file.png", "x-dll.png", "archive-file.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "x-dll.png", "text-file.png", "x-exe.png", "archive-file.png", "multimedia-flv.png", "x-fon.png", "multimedia-gif.png", "archive-gz.png", "archive-gz.png", "text-script.png", "disk-hdd.png", "multimedia-video.png", "archive-file.png", "text-html.png", "text-html.png", "text-html.png", "multimedia-ico.png", "disk-iso.png", "text-file.png", "text-script.png", "text-script.png", "disk-iso.png", "disk-iso.png", "archive-jar.png", "text-java.png", "multimedia-image.png", "multimedia-jpg.png", "multimedia-jpg.png", "multimedia-jpg.png", "text-script.png", "multimedia-image.png", "archive-file.png", "archive-file.png", "text-script.png", "archive-file.png", "text-file.png", "archive-file.png", "archive-lzma.png", "text-database.png", "text-database.png", "text-html.png", "multimedia-audio.png", "multimedia-audio.png", "multimedia-video.png", "multimedia-audio.png", "text-file.png", "disk-mou.png", "multimedia-video.png", "multimedia-mp3.png", "multimedia-mp3.png", "multimedia-video.png", "multimedia-video.png", "multimedia-video.png", "multimedia-video.png", "x-exe.png", "x-package.png", "text-script.png", "disk-iso.png", "text-x.png", "archive-file.png", "text-database.png", "text-presentation.png", "text-spreadsheet.png", "text-richtext.png", "multimedia-ogg.png", "multimedia-video.png", "multimedia-video.png", "multimedia-audio.png", "text-xps.png", "text-file.png", "multimedia-image.png", "text-pdf.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "x-sys.png", "multimedia-png.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-file.png", "text-file.png", "multimedia-psd.png", "text-python.png", "text-python.png", "multimedia-audio.png", "multimedia-audio.png", "archive-rar.png", "text-reg.png", "multimedia-video.png", "archive-rpm.png", "text-richtext.png", "x-scr.png", "text-file.png", "text-html.png", "text-html.png", "text-file.png", "archive-file.png", "multimedia-image.png", "multimedia-flv.png", "x-sys.png", "archive-tar.png", "archive-file.png", "archive-file.png", "archive-file.png", "text-file.png", "multimedia-image.png", "archive-gz.png", "multimedia-tif.png", "multimedia-tif.png", "archive-file.png", "x-fon.png", "x-fon.png", "text-file.png", "archive-file.png", "archive-file.png", "text-html.png", "text-file.png", "text-script.png", "text-script.png", "disk-vdi.png", "disk-vhd.png", "text-file.png", "multimedia-video.png", "multimedia-wav.png", "multimedia-image.png", "archive-file.png", "multimedia-wma.png", "multimedia-video.png", "text-script.png", "multimedia-wav.png", "archive-file.png", "text-html.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-script.png", "text-xps.png", "text-script.png", "text-script.png", "text-script.png", "archive-file.png", "text-file.png", "archive-file.png", "archive-zip.png", "directory.png", "unknown-file.png" };
        public static string[] file_description = { "File", "7z Split Archive File", "3GPP2 Audio/Video", "3GPP Audio/Video", "3GPP Audio/Video", "7Z Archive File", "ADTS Audio File", "AC3 Audio File", "ACC File", "Microsoft Access Database", "Microsoft Access Template", "Winace Archive File", "ADTS Audio File", "ADTS Audio File", "AMR Audio File", "AMV Video File", "ARJ Archive File", "Video Clip File", "Batch File", "BIN File", "Bitmap Image File", "Unix Script File", "BZ2 Archive File", "BZIP2 Archive File", "'C' Source File", "Cabinet File", "Compiled HTML Help File", "Command Script", "MS-DOS Application", "Comic Document", "CPIO Archive File", "Windows Control Panel Item", "'C++' Source File", "'C#' Source File", "Data File", "Data Base File", "Debian Application Package", "Bitmap Image File", "Djvu Document", "Application Extension", "Mac Application Package", "Microsoft Word 97 - 2003 Document", "Microsoft Word Macro-Enabled Document", "Microsoft Word Document", "Microsoft Word 97 - 2003 Template", "Microsoft Word Template", "Device Driver", ".epub", "Windows Application", "Fat Archive File", "Flash Video File", "Font File", "GIF Image File", "GZ Archive File", "GZ Archive File", "C++ Library Source File", "Virtual Hard Disk", "HDMOV Video File", "HFS Archive File", "HTML Application", "HTML (Hyper Text Markup Language) Document", "HTML (Hyper Text Markup Language) Document", "Icon File", "Disc Image File", "Pascal Source File", "Setup Information", "Configuration Settings", "Disc Image File", "Compressed Disc Image File", "Executable Jar File", "java Source File", "JPEG Image File", "JPEG Image File", "JPEG Image File", "JPEG Image File", "java Script File", "Windows Media Photo File", "KGE Archive Files", "KGP Archive File", "Extensible Markup Language File", "LHA Archive File", "Log File", "LZH Archive File", "LZMA Archive File", "Microsoft Access Database", ".mdl", "HTML Archive File", "MIDI Sequence File", "MIDI Sequence File", "Matroska Audio File", "Matroska Video File", ".mobi", "WinMount archiver", "QuickTime Movie", "MP3 Format Sound", "MP3 Format Sound", "MP4 Video File", "MP4 Video File", "Movie Clip File", "Movie Clip File", "Microsoft Common Console Document", "Windows Installer Package", "System Information File", "Nero Burning ROM", ".nt", "NTFS Archive File", ".odbc", "Open Document Presentation", "Open Document Spreadsheet", "Open Document Text", ".ogg", "Ogg Video File", "Ogg Video File", "Opus Audio File", "Open XPS Document", "Pascal Source File", ".pcx", "Portable Document Format File", "PHP Hypertext Preprocessor File", "PHP Hypertext Preprocessor File", "PHP Hypertext Preprocessor File", "Perl Source File", "Perl Source File", "Perl Source File", "Precompiled Setup Information", "PNG Image File", "Microsoft PowerPoint 97-2003 Template", "Microsoft PowerPoint Template", "Microsoft PowerPoint 97-2003 Slide Show", "Microsoft PowerPoint Slide Show", "Microsoft PowerPoint 97-2003 Presentation", "Microsoft PowerPoint Macro-Enabled Presentation", "Microsoft PowerPoint Presentation", "Post Script File", "Windows PowerShell Script", ".psd", "Python File", "Python File", "RealMedia Audio File", "RealMedia File", "Winrar Archive File", "Registration Entries", "RealMedia Video File", "Red Hat Application Package", "Rich Text Format", "Screen Saver File", "Unix Script File", "HTML Document", "HTML Document", "Structured Query Language File", "Squashfs Image File", "Standard Vector Graphics", "Shockwave Flash Object", "System File", "TAR Archive File", "TAZ Archive File", "TPZ Archive File", "TBZ2 Archive File", "TEX File", ".tga", "TGZ Archive File", "TIFF Image File", "TIFF Image File", "TPZ Archive File", "TrueType Collection Font File", "TrueType Font File", "Plan Text File", "TXZ Archive File", "Uha Archive File", "Internet Shortcut", "Verilog file", "Visual Basic File", "Visual Basic File", "Virtual Disk Image", "Virtual Hard Disk", "VHSIC Hardware Description Language File", ".DVD VOB File", "Wave Sound File", "Windows Media Photo File", "Wim Image File", "Windows Media Audio File", "Windows Media Audio/Video File", "Extensible Markup Language File", "WavPack Audio File", "XAR Archive File", "XHTML Document", "Microsoft Excel 97-2003 Worksheet", "Microsoft Excel Binary Worksheet", "Microsoft Excel Macro-Enabled Worksheet", "Microsoft Excel Worksheet", ".Microsoft Excel Template", "Microsoft Excel Macro-Enabled Template", "Microsoft Excel Template", "Extensible Markup Language File", "XPS Document", "Extensible Markup Language File", "Extensible Markup Language File", "Extensible Markup Language File", "XZ Archive File", "YAML Ain't Markup Language", "Z Archive File", "Compressed Zipped File", "Files Directory", "Unknown File Type" };
        public static FileCategory[] file_type = { FileCategory.Unknown, FileCategory.Archive, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Archive, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Document, FileCategory.Document, FileCategory.Archive, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Archive, FileCategory.Vedio, FileCategory.TextCanRunAsApplication, FileCategory.DiskImage, FileCategory.Image, FileCategory.Text, FileCategory.Archive, FileCategory.Archive, FileCategory.Text, FileCategory.Archive, FileCategory.Document, FileCategory.TextCanRunAsApplication, FileCategory.Application, FileCategory.Document, FileCategory.Archive, FileCategory.Application, FileCategory.Text, FileCategory.Text, FileCategory.CannotOpen, FileCategory.CannotOpen, FileCategory.Archive, FileCategory.Image, FileCategory.Document, FileCategory.CannotOpen, FileCategory.Archive, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.CannotOpen, FileCategory.Unknown, FileCategory.Application, FileCategory.Archive, FileCategory.Vedio, FileCategory.Font, FileCategory.Image, FileCategory.Archive, FileCategory.Archive, FileCategory.Text, FileCategory.DiskImage, FileCategory.Vedio, FileCategory.Archive, FileCategory.Document, FileCategory.Html, FileCategory.Html, FileCategory.Image, FileCategory.DiskImage, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.DiskImage, FileCategory.DiskImage, FileCategory.Archive, FileCategory.Text, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.TextCanRunAsApplication, FileCategory.Image, FileCategory.Archive, FileCategory.Archive, FileCategory.Text, FileCategory.Archive, FileCategory.Text, FileCategory.Archive, FileCategory.Archive, FileCategory.Document, FileCategory.Unknown, FileCategory.Html, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Unknown, FileCategory.DiskImage, FileCategory.Vedio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Application, FileCategory.Application, FileCategory.TextCanRunAsApplication, FileCategory.DiskImage, FileCategory.Text, FileCategory.Archive, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Audio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Audio, FileCategory.Document, FileCategory.Text, FileCategory.Unknown, FileCategory.Document, FileCategory.Html, FileCategory.Html, FileCategory.Html, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.CannotOpen, FileCategory.Image, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Text, FileCategory.Text, FileCategory.Unknown, FileCategory.Text, FileCategory.Text, FileCategory.Audio, FileCategory.Audio, FileCategory.Archive, FileCategory.TextCanRunAsApplication, FileCategory.Vedio, FileCategory.Archive, FileCategory.Document, FileCategory.Application, FileCategory.Text, FileCategory.Html, FileCategory.Html, FileCategory.Text, FileCategory.Archive, FileCategory.Html, FileCategory.Html, FileCategory.CannotOpen, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Text, FileCategory.Unknown, FileCategory.Archive, FileCategory.Image, FileCategory.Image, FileCategory.Archive, FileCategory.Font, FileCategory.Font, FileCategory.Text, FileCategory.Archive, FileCategory.Archive, FileCategory.TextCanRunAsApplication, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.DiskImage, FileCategory.DiskImage, FileCategory.Text, FileCategory.Vedio, FileCategory.Audio, FileCategory.Image, FileCategory.Archive, FileCategory.Audio, FileCategory.Vedio, FileCategory.Text, FileCategory.Audio, FileCategory.Archive, FileCategory.Html, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Html, FileCategory.Document, FileCategory.Text, FileCategory.Html, FileCategory.Html, FileCategory.Archive, FileCategory.Text, FileCategory.Archive, FileCategory.Archive, FileCategory.Directory, FileCategory.Unknown };
        public static List<BitmapImage> file_bitmap_64 = new List<BitmapImage>();
        public static List<BitmapImage> file_bitmap_24 = new List<BitmapImage>();
        public static bool[] May_Danger;
    }

    

    

    

    public enum PhotoOrientation
    {
        Unspecified = 0,
        Normal = 1,
        FlipHorizontal = 2,
        Rotate180 = 3,
        FlipVertical = 4,
        Transpose = 5,
        Rotate270 = 6,
        Transverse = 7,
        Rotate90 = 8,
    }

    public class ProcessItem
    {
        string id;
        string name;
        string memory;
        string cpu;

        public ProcessItem(string id, string name, string memory, string cpu)
        {
            this.id = id;
            this.name = name;
            this.memory = memory;
            this.cpu = cpu;
        }
    }



    public class BigItemElement : IComparable
    {
        public FileSystemInfo file_system;
        public long size;
        public bool is_directory;

        public BigItemElement(FileSystemInfo dir, long size, bool is_directory)
        {
            this.file_system = dir;
            this.size = size;
            this.is_directory = is_directory;
        }

        public int CompareTo(object obj)
        {
            BigItemElement socend_item = ((BigItemElement)obj);
            if (obj == null)
                return -1;
            if (socend_item.size == this.size)
                return 0;
            else if (socend_item.size > this.size)
                return 1;
            else
                return -1;
        }
    }

    public class Tree : IComparable
    {
        public List<Tree> children;
        public Tree parent;
        public BigItemElement content;

        internal Tree(Tree parent, BigItemElement content)
        {
            this.parent = parent;
            this.content = content;
            children = new List<Tree>();
        }

        internal void AddChild(BigItemElement content)
        {
            children.Add(new Tree(this, content));
        }

        internal void AddChild(Tree child)
        {
            children.Add(child);
        }

        internal void SetSize(long full_size)
        {
            content.size = full_size;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 0;
            if (((Tree)obj).content.size == this.content.size)
                return 0;
            else if (((Tree)obj).content.size > this.content.size)
                return 1;
            else
                return -1;
        }
    }

    public class DirectoryTreeItem
    {
        public DirectoryTreeItem(FileSystemInfo Item, long Size, string Type, DirectoryTreeItem Parent, List<DirectoryTreeItem> SubItems)
        {
            item = Item;
            size = Size;
            type = Type;
            parent = Parent;
            sub_items = SubItems;
        }

        public DirectoryTreeItem(FileSystemInfo Item, DirectoryTreeItem Parent)
        {
            item = Item;
            size = ((FileInfo)Item).Length;
            type = Functions.GetFileType(item.Extension);
            parent = Parent;
            sub_items = null;
        }

        public DirectoryTreeItem()
        {
            parent = null;
            sub_items = new List<DirectoryTreeItem>();
        }

        FileSystemInfo item;
        long size;
        string type;
        DirectoryTreeItem parent;
        List<DirectoryTreeItem> sub_items;
    }

    class SafeBinder : SerializationBinder
    {
        List<string> allowed_types;

        public SafeBinder(params string[] allowed_types)
        {
            this.allowed_types = new List<string>(allowed_types);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            foreach (Match i in Regex.Matches(typeName, @"[A-Za-z_][\w`.+]*"))
                if (i.Value.Contains('.') && !allowed_types.Contains(i.Value))
                    throw new SerializationException("The file contains an unexpected type '" + i.Value + "'.");
            return null;
        }
    }

    public class FileIDInfo
    {
        public FileIDInfo(string ext, string description, List<int> IDs)
        {
            this.ext = ext;
            this.bytes = IDs;
            this.description = description;
        }
        string ext;
        public String Extention { get { return ext; } }
        string description;
        public String Description { get { return description; } }
        List<int> bytes;
        public List<Int32> IDs
        {
            get { return bytes; }
        }
    }

}