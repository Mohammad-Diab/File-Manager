using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WpfApplication1
{
    public class FileElement : IComparable
    {
        public bool? over;
        public string extension;
        public string files_full_name;
        public string file_short_name;
        public long files_size;
        public bool remove_file;
        public bool done;
        public FileElement()
        {
            files_full_name = "";
            file_short_name = "";
            extension = "";
            files_size = 0;
            remove_file = false;
            over = false;
            done = false;
        }

        public FileElement(string file_path, string short_name, bool remove)
        {
            FileInfo f = new FileInfo(file_path);
            files_full_name = file_path;
            file_short_name = short_name;
            files_size = f.Length;
            extension = f.Extension;
            remove_file = remove;
            done = false;
            over = false;
        }

        public FileElement(string file_path, long file_Size, string extension, string short_name, bool remove)
        {
            this.extension = extension;
            files_full_name = file_path;
            file_short_name = short_name;
            files_size = file_Size;
            remove_file = remove;
            done = false;
            over = false;
        }

        public FileElement(string file_path, long file_Size, string extension, string short_name, bool remove, bool? over)
        {
            this.extension = extension;
            files_full_name = file_path;
            file_short_name = short_name;
            files_size = file_Size;
            remove_file = remove;
            done = false;
            this.over = over;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 0;
            if (((FileElement)obj).files_size == this.files_size)
                return 0;
            else if (((FileElement)obj).files_size > this.files_size)
                return -1;
            else
                return 1;
        }
    }

    public class UnCompleatOperation : IComparable
    {
        public string files_full_name;
        public string file_short_name;
        public long files_size;
        public string extension;
        public bool remove_file;
        public string error_message;
        public bool IsDirectory;
        public UnCompleatOperation()
        {
            files_full_name = "";
            file_short_name = "";
            extension = "";
            files_size = 0;
            remove_file = false;
            error_message = "";
            IsDirectory = false;
        }

        public UnCompleatOperation(string file_path, string error_message, bool IsDirectory)
        {
            this.extension = "";
            files_full_name = file_path;
            file_short_name = "";
            files_size = 0;
            remove_file = false;
            this.IsDirectory = IsDirectory;
            this.error_message = error_message;
        }

        public UnCompleatOperation(string file_path, string short_name, bool remove, string error_message)
        {
            FileInfo f = new FileInfo(file_path);
            files_full_name = file_path;
            this.extension = f.Extension;
            file_short_name = short_name;
            files_size = f.Length;
            remove_file = remove;
            IsDirectory = false;
            this.error_message = error_message;
        }

        public UnCompleatOperation(string file_path, long file_Size, string extension, string short_name, bool remove, string error_message)
        {
            files_full_name = file_path;
            file_short_name = short_name;
            this.extension = extension;
            files_size = file_Size;
            remove_file = remove;
            IsDirectory = false;
            this.error_message = error_message;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 0;
            if (((UnCompleatOperation)obj).error_message == this.error_message)
                return 0;
            else if (((UnCompleatOperation)obj).error_message.CompareTo(this.error_message) < 0)
                return -1;
            else
                return 1;
        }
    }

    public static class FileInformation
    {
        public static string[] file_extentions = { ".", ".001", ".3ggp2", ".3gp", ".3gpp", ".7z", ".aac", ".ac3", ".acc", ".accdb", ".accdt", ".ace", ".adt", ".adts", ".amr", ".amv", ".arj", ".avi", ".bat", ".bin", ".bmp", ".bsh", ".bz2", ".bzip2", ".c", ".cab", ".chm", ".cmd", ".com", ".comic", ".cpio", ".cpl", ".cpp", ".cs", ".dat", ".db", ".deb", ".dib", ".djvu", ".dll", ".dmg", ".doc", ".docm", ".docx", ".dot", ".dotx", ".drv", ".epub", ".exe", ".fat", ".flv", ".fon", ".gif", ".gz", ".gzip", ".h", ".hdd", ".hdmov", ".hfs", ".hta", ".htm", ".html", ".ico", ".img", ".inc", ".inf", ".ini", ".iso", ".isz", ".jar", ".java", ".jfif", ".jpe", ".jpeg", ".jpg", ".js", ".jxr", ".kge", ".kgp", ".kml", ".lha", ".log", ".lzh", ".lzma", ".mdb", ".mdl", ".mht", ".mid", ".midi", ".mka", ".mkv", ".mobi", ".mou", ".mov", ".mp2", ".mp3", ".mp4", ".mp4v", ".mpeg", ".mpg", ".msc", ".msi", ".nfo", ".nrg", ".nt", ".ntfs", ".odbc", ".odp", ".ods", ".odt", ".ogg", ".ogm", ".ogv", ".opus", ".oxps", ".pas", ".pcx", ".pdf", ".php", ".php3", ".phtml", ".pl", ".plx", ".pm", ".pnf", ".png", ".pot", ".potx", ".pps", ".ppsx", ".ppt", ".pptm", ".pptx", ".ps", ".ps1", ".psd", ".py", ".pyw", ".ra", ".ram", ".rar", ".reg", ".rm", ".rpm", ".rtf", ".scr", ".sh", ".shtm", ".shtml", ".sql", ".squashfs", ".svg", ".swf", ".sys", ".tar", ".taz", ".tbz", ".tbz2", ".tex", ".tga", ".tgz", ".tif", ".tiff", ".tpz", ".ttc", ".ttf", ".txt", ".txz", ".uha", ".url", ".v", ".vb", ".vbs", ".vdi", ".vhd", ".vhdl", ".vob", ".wav", ".wdp", ".wim", ".wma", ".wmv", ".wsdl", ".wv", ".xar", ".xhtml", ".xls", ".xlsb", ".xlsm", ".xlsx", ".xlt", ".xltm", ".xltx", ".xml", ".xps", ".xsd", ".xsl", ".xsml", ".xz", ".yml", ".z", ".zip", "directory", "unknown" };
        public static string[] file_image = { "file.png", "archive-file.png", "multimedia-video.png", "multimedia-video.png", "multimedia-video.png", "archive-7z.png", "multimedia-audio.png", "multimedia-audio.png", "multimedia-audio.png", "text-database.png", "text-database.png", "archive-ace.png", "multimedia-video.png", "multimedia-video.png", "multimedia-audio.png", "multimedia-video.png", "archive-file.png", "multimedia-video.png", "text-x.png", "disk-iso.png", "multimedia-bmp.png", "text-file.png", "archive-bz2.png", "archive-bz2.png", "text-c.png", "archive-cab.png", "text-help.png", "text-x.png", "text-x.png", "text-file.png", "archive-file.png", "x-dll.png", "text-c++.png", "text-cs.png", "file.png", "x-sys.png", "archive-deb.png", "multimedia-image.png", "text-file.png", "x-dll.png", "archive-file.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "x-dll.png", "text-file.png", "x-exe.png", "archive-file.png", "multimedia-flv.png", "x-fon.png", "multimedia-gif.png", "archive-gz.png", "archive-gz.png", "text-script.png", "disk-hdd.png", "multimedia-video.png", "archive-file.png", "text-html.png", "text-html.png", "text-html.png", "multimedia-ico.png", "disk-iso.png", "text-file.png", "text-script.png", "text-script.png", "disk-iso.png", "disk-iso.png", "archive-jar.png", "text-java.png", "multimedia-image.png", "multimedia-jpg.png", "multimedia-jpg.png", "multimedia-jpg.png", "text-script.png", "multimedia-image.png", "archive-file.png", "archive-file.png", "text-script.png", "archive-file.png", "text-file.png", "archive-file.png", "archive-lzma.png", "text-database.png", "text-database.png", "text-html.png", "multimedia-audio.png", "multimedia-audio.png", "multimedia-video.png", "multimedia-audio.png", "text-file.png", "disk-mou.png", "multimedia-video.png", "multimedia-mp3.png", "multimedia-mp3.png", "multimedia-video.png", "multimedia-video.png", "multimedia-video.png", "multimedia-video.png", "x-exe.png", "x-package.png", "text-script.png", "disk-iso.png", "text-x.png", "archive-file.png", "text-database.png", "text-presentation.png", "text-spreadsheet.png", "text-richtext.png", "multimedia-ogg.png", "multimedia-video.png", "multimedia-video.png", "multimedia-audio.png", "text-xps.png", "text-file.png", "multimedia-image.png", "text-pdf.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "x-sys.png", "multimedia-png.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-file.png", "text-file.png", "multimedia-psd.png", "text-python.png", "text-python.png", "multimedia-audio.png", "multimedia-audio.png", "archive-rar.png", "text-reg.png", "multimedia-video.png", "archive-rpm.png", "text-richtext.png", "x-scr.png", "text-file.png", "text-html.png", "text-html.png", "text-file.png", "archive-file.png", "multimedia-image.png", "multimedia-flv.png", "x-sys.png", "archive-tar.png", "archive-file.png", "archive-file.png", "archive-file.png", "text-file.png", "multimedia-image.png", "archive-gz.png", "multimedia-tif.png", "multimedia-tif.png", "archive-file.png", "x-fon.png", "x-fon.png", "text-file.png", "archive-file.png", "archive-file.png", "text-html.png", "text-file.png", "text-script.png", "text-script.png", "disk-vdi.png", "disk-vhd.png", "text-file.png", "multimedia-video.png", "multimedia-wav.png", "multimedia-image.png", "archive-file.png", "multimedia-wma.png", "multimedia-video.png", "text-script.png", "multimedia-wav.png", "archive-file.png", "text-html.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-script.png", "text-xps.png", "text-script.png", "text-script.png", "text-script.png", "archive-file.png", "text-file.png", "archive-file.png", "archive-zip.png", "directory.png", "unknown-file.png" };
        public static string[] file_description = { "File", "7z Split Archive File", "3GPP2 Audio/Video", "3GPP Audio/Video", "3GPP Audio/Video", "7Z Archive File", "ADTS Audio File", "AC3 Audio File", "ACC File", "Microsoft Access Database", "Microsoft Access Template", "Winace Archive File", "ADTS Audio File", "ADTS Audio File", "AMR Audio File", "AMV Video File", "ARJ Archive File", "Video Clip File", "Batch File", "BIN File", "Bitmap Image File", "Unix Script File", "BZ2 Archive File", "BZIP2 Archive File", "'C' Source File", "Cabinet File", "Compiled HTML Help File", "Command Script", "MS-DOS Application", "Comic Document", "CPIO Archive File", "Windows Control Panel Item", "'C++' Source File", "'C#' Source File", "Data File", "Data Base File", "Debian Application Package", "Bitmap Image File", "Djvu Document", "Application Extension", "Mac Application Package", "Microsoft Word 97 - 2003 Document", "Microsoft Word Macro-Enabled Document", "Microsoft Word Document", "Microsoft Word 97 - 2003 Template", "Microsoft Word Template", "Device Driver", ".epub", "Windows Application", "Fat Archive File", "Flash Video File", "Font File", "GIF Image File", "GZ Archive File", "GZ Archive File", "C++ Library Source File", "Virtual Hard Disk", "HDMOV Video File", "HFS Archive File", "HTML Application", "HTML (Hyper Text Markup Language) Document", "HTML (Hyper Text Markup Language) Document", "Icon File", "Disc Image File", "Pascal Source File", "Setup Information", "Configuration Settings", "Disc Image File", "Compressed Disc Image File", "Executable Jar File", "java Source File", "JPEG Image File", "JPEG Image File", "JPEG Image File", "JPEG Image File", "java Script File", "Windows Media Photo File", "KGE Archive Files", "KGP Archive File", "Extensible Markup Language File", "LHA Archive File", "Log File", "LZH Archive File", "LZMA Archive File", "Microsoft Access Database", ".mdl", "HTML Archive File", "MIDI Sequence File", "MIDI Sequence File", "Matroska Audio File", "Matroska Video File", ".mobi", "WinMount archiver", "QuickTime Movie", "MP3 Format Sound", "MP3 Format Sound", "MP4 Video File", "MP4 Video File", "Movie Clip File", "Movie Clip File", "Microsoft Common Console Document", "Windows Installer Package", "System Information File", "Nero Burning ROM", ".nt", "NTFS Archive File", ".odbc", "Open Document Presentation", "Open Document Spreadsheet", "Open Document Text", ".ogg", "Ogg Video File", "Ogg Video File", "Opus Audio File", "Open XPS Document", "Pascal Source File", ".pcx", "Portable Document Format File", "PHP Hypertext Preprocessor File", "PHP Hypertext Preprocessor File", "PHP Hypertext Preprocessor File", "Perl Source File", "Perl Source File", "Perl Source File", "Precompiled Setup Information", "PNG Image File", "Microsoft PowerPoint 97-2003 Template", "Microsoft PowerPoint Template", "Microsoft PowerPoint 97-2003 Slide Show", "Microsoft PowerPoint Slide Show", "Microsoft PowerPoint 97-2003 Presentation", "Microsoft PowerPoint Macro-Enabled Presentation", "Microsoft PowerPoint Presentation", "Post Script File", "Windows PowerShell Script", ".psd", "Python File", "Python File", "RealMedia Audio File", "RealMedia File", "Winrar Archive File", "Registration Entries", "RealMedia Video File", "Red Hat Application Package", "Rich Text Format", "Screen Saver File", "Unix Script File", "HTML Document", "HTML Document", "Structured Query Language File", "Squashfs Image File", "Standard Vector Graphics", "Shockwave Flash Object", "System File", "TAR Archive File", "TAZ Archive File", "TPZ Archive File", "TBZ2 Archive File", "TEX File", ".tga", "TGZ Archive File", "TIFF Image File", "TIFF Image File", "TPZ Archive File", "TrueType Collection Font File", "TrueType Font File", "Plan Text File", "TXZ Archive File", "Uha Archive File", "Internet Shortcut", "Verilog file", "Visual Basic File", "Visual Basic File", "Virtual Disk Image", "Virtual Hard Disk", "VHSIC Hardware Description Language File", ".DVD VOB File", "Wave Sound File", "Windows Media Photo File", "Wim Image File", "Windows Media Audio File", "Windows Media Audio/Video File", "Extensible Markup Language File", "WavPack Audio File", "XAR Archive File", "XHTML Document", "Microsoft Excel 97-2003 Worksheet", "Microsoft Excel Binary Worksheet", "Microsoft Excel Macro-Enabled Worksheet", "Microsoft Excel Worksheet", ".Microsoft Excel Template", "Microsoft Excel Macro-Enabled Template", "Microsoft Excel Template", "Extensible Markup Language File", "XPS Document", "Extensible Markup Language File", "Extensible Markup Language File", "Extensible Markup Language File", "XZ Archive File", "YAML Ain't Markup Language", "Z Archive File", "Compressed Zipped File", "Files Directory", "Unknown File Type" };
        public static FileCategory[] file_type = { FileCategory.Unknown, FileCategory.Archive, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Archive, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Document, FileCategory.Document, FileCategory.Archive, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Archive, FileCategory.Vedio, FileCategory.TextCanRunAsApplication, FileCategory.DiskImage, FileCategory.Image, FileCategory.Text, FileCategory.Archive, FileCategory.Archive, FileCategory.Text, FileCategory.Archive, FileCategory.Document, FileCategory.TextCanRunAsApplication, FileCategory.Application, FileCategory.Document, FileCategory.Archive, FileCategory.Application, FileCategory.Text, FileCategory.Text, FileCategory.CannotOpen, FileCategory.CannotOpen, FileCategory.Archive, FileCategory.Image, FileCategory.Document, FileCategory.CannotOpen, FileCategory.Archive, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.CannotOpen, FileCategory.Unknown, FileCategory.Application, FileCategory.Archive, FileCategory.Vedio, FileCategory.Font, FileCategory.Image, FileCategory.Archive, FileCategory.Archive, FileCategory.Text, FileCategory.DiskImage, FileCategory.Vedio, FileCategory.Archive, FileCategory.Document, FileCategory.Html, FileCategory.Html, FileCategory.Image, FileCategory.DiskImage, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.DiskImage, FileCategory.DiskImage, FileCategory.Archive, FileCategory.Text, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.TextCanRunAsApplication, FileCategory.Image, FileCategory.Archive, FileCategory.Archive, FileCategory.Text, FileCategory.Archive, FileCategory.Text, FileCategory.Archive, FileCategory.Archive, FileCategory.Document, FileCategory.Unknown, FileCategory.Html, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Unknown, FileCategory.DiskImage, FileCategory.Vedio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Application, FileCategory.Application, FileCategory.TextCanRunAsApplication, FileCategory.DiskImage, FileCategory.Text, FileCategory.Archive, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Audio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Audio, FileCategory.Document, FileCategory.Text, FileCategory.Unknown, FileCategory.Document, FileCategory.Html, FileCategory.Html, FileCategory.Html, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.CannotOpen, FileCategory.Image, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Text, FileCategory.Text, FileCategory.Unknown, FileCategory.Text, FileCategory.Text, FileCategory.Audio, FileCategory.Audio, FileCategory.Archive, FileCategory.TextCanRunAsApplication, FileCategory.Vedio, FileCategory.Archive, FileCategory.Document, FileCategory.Application, FileCategory.Text, FileCategory.Html, FileCategory.Html, FileCategory.Text, FileCategory.Archive, FileCategory.Html, FileCategory.Html, FileCategory.CannotOpen, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Text, FileCategory.Unknown, FileCategory.Archive, FileCategory.Image, FileCategory.Image, FileCategory.Archive, FileCategory.Font, FileCategory.Font, FileCategory.Text, FileCategory.Archive, FileCategory.Archive, FileCategory.TextCanRunAsApplication, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.DiskImage, FileCategory.DiskImage, FileCategory.Text, FileCategory.Vedio, FileCategory.Audio, FileCategory.Image, FileCategory.Archive, FileCategory.Audio, FileCategory.Vedio, FileCategory.Text, FileCategory.Audio, FileCategory.Archive, FileCategory.Html, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Html, FileCategory.Document, FileCategory.Text, FileCategory.Html, FileCategory.Html, FileCategory.Archive, FileCategory.Text, FileCategory.Archive, FileCategory.Archive, FileCategory.Directory, FileCategory.Unknown };
        public static List<BitmapImage> file_bitmap = new List<BitmapImage>();
        public static bool[] May_Danger;
    }

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
        Abort = 7
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

    static class Functions
    {
        public static string[] spliter = { "\\" };

        internal static string File_Size(decimal SizeInByte)
        {
            byte i = 0;
            while (SizeInByte / 1024 > 1)
            {
                SizeInByte = SizeInByte / 1024;
                i++;
            }
            return Math.Round(SizeInByte, 2) + " " + FileSizeUnit(i);
        }

        private static string FileSizeUnit(byte num)
        {
            if (num == 0)
                return "Byte";
            else if (num == 1)
                return "KB";
            else if (num == 2)
                return "MB";
            else if (num == 3)
                return "GB";
            else if (num == 4)
                return "TB";
            else if (num == 5)
                return "PB";
            else
                return null;
        }

        public static string GetDriveImage(string path, string StandardImageSource)
        {
            DriveInfo d = new DriveInfo(path);
            if (d.DriveType == DriveType.Fixed)
                if (d.IsReady)
                    if (path == MainWindow.systemDrive)
                        return StandardImageSource + "system-hard-disk.png";
                    else
                        return StandardImageSource + "hard-disk.png";
                else
                    return StandardImageSource + "";
            else if (d.DriveType == DriveType.CDRom)
                if (d.IsReady)
                    return StandardImageSource + "dvd-media.png";
                else
                    return StandardImageSource + "optical-drive.png";
            else if (d.DriveType == DriveType.Removable)
                if (d.IsReady)
                    return StandardImageSource + "removable-usb.png";
                else
                    return StandardImageSource + "removable-media.png";
            else if (d.DriveType == DriveType.Ram)
                return StandardImageSource + "";
            else
                return StandardImageSource + "";
        }

        public static string GetFileType(int file_index, string extension)
        {
            if (file_index != (FileInformation.file_extentions.Length - 1))
                return FileInformation.file_description[file_index];
            else
                if (extension.Length > 2)
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

        public static void AddAllFiles(string directory, string parent_path, bool remove, List<FileElement> list)
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
                list.Add(new FileElement(i.FullName, i.Length, i.Extension, file_short_name, remove));
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

        public static string ToString(long num)
        {
            if (num < 10)
                return "0" + num;
            else
                return num.ToString();
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
    }

    public class BigItemElement
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
}