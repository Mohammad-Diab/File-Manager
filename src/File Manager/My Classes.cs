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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
namespace File_Manager
{
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
            file_bitmap16 = Icons.GetIcons.GetIcon(image_file, extention, 16); //BitmapImage(new Uri(@"C:\Users\pal\Documents\Images\16\file.png"));
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

    public class FileOperationItem : IComparable
    {
        public bool? Over;
        public FileSystemInfo Item;
        public long Size;
        public bool IsCut;
        public bool Done;
        public string short_path;

        public FileOperationItem(FileInfo File, bool IsCut, bool? Over, string short_path)
        {
            Item = File;
            Size = ((FileInfo)File).Length;
            this.IsCut = IsCut;
            Done = false;
            this.short_path = short_path;
            this.Over = Over;
        }

        public FileOperationItem(DirectoryInfo Directory, long DirectorySize, bool IsCut, bool? Over, string short_path)
        {
            Item = Directory;
            Size = DirectorySize;
            this.IsCut = IsCut;
            Done = false;
            this.short_path = short_path;
            this.Over = Over;
        }

        public FileOperationItem(DirectoryInfo directoryInfo, string short_path)
        {
            Item = directoryInfo;
            Over = false;
            Size = -1;
            IsCut = false;
            Done = false;
            this.short_path = short_path;
        }

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

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 0;
            if (((FileOperationItem)obj).Size == this.Size)
                return 0;
            else if (((FileOperationItem)obj).Size > this.Size)
                return -1;
            else
                return 1;
        }
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

    public class MainListView
    {
        public MainListView(FileInfo file)
        {
            File = file;
            FileName = file.Name;
            FullPath = File.FullName;
            if (FileInfoCombonents.FilesInformation.ContainsKey(file.Extension.ToLower()))
            {
                Icon = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Bitmap24;
                FileType = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Description;
                if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].HasSubtext)
                    FileExt = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Subtext;
                else
                    FileExt = "";
            }
            else
            {
                Icon = FileInfoCombonents.FilesInformation["."].Bitmap24;
                FileType = file.Extension.Length > 1 ? file.Extension.ToLower().Substring(1).ToUpper() + " File" : "File";
                if (file.Extension.Length > 1 && file.Extension.Length < 6)
                    FileExt = file.Extension.ToLower().Substring(1);
                else
                    FileExt = "";
            }
            FileSize = Functions.FileSize(file.Length);
            DateModified = file.LastWriteTime.ToString();          
        }

        public MainListView(DirectoryInfo directory)
        {
            File = directory;
            FullPath = directory.FullName;
            FileName = directory.Name;
            Icon = FileInfoCombonents.FilesInformation["directory"].Bitmap24;
            FileType = "Directory";
            FileSize = "";
            FileExt = "";
            DateModified = directory.LastWriteTimeUtc.ToString("dd\\mm\\yyyy hh:mm");
        }

        public MainListView(DriveInfo drive)
        {
            File = null;
            FullPath = drive.Name;
            Drive = drive;
            if (drive.IsReady)
            {
                FileName = drive.VolumeLabel + " (" + drive.Name + ")";
                FileSize = Functions.FileSize(drive.AvailableFreeSpace);
                DateModified = Functions.FileSize(drive.TotalSize);
            }
            else
            {
                FileName = drive.Name;
                FileSize = "Drive is not ready!";
                DateModified = "";
            }
            Icon = new BitmapImage(new Uri(Functions.GetDriveImage(drive.DriveType, drive.Name.ToLower().Equals(Functions.SystemDrive), drive.IsReady, Functions.StandardImageSource + "24\\")));
            FileType = Functions.GetDriveDescription(drive.DriveType);
            FileExt = "";
        }

        ContextMenu menu;
        public ContextMenu Menu
        {
            get { return menu; }
            set { menu = value; }
        }

        FileSystemInfo file;
        public FileSystemInfo File
        {
            get { return file; }
            set { file = value; }
        }

        public string FullPath;

        DriveInfo drive;
        public DriveInfo Drive
        {
            get { return drive; }
            set { drive = value; }
        }

        private String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private String file_extention;
        public string FileExt
        {
            get { return file_extention; }
            set { file_extention = value; }
        }

        private BitmapImage img_source;
        public BitmapImage Icon
        {
            get { return img_source; }
            set { img_source = value; }
        }

        private String file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }

        private String file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }

        private String file_date;
        public string DateModified
        {
            get { return file_date; }
            set { file_date = value; }
        }
    }

    public class ExistsItemsListView
    {
        public ExistsItemsListView(FileInfo source_file, FileInfo target_file)
        {
            FileName = source_file.Name;
            SourceFile = source_file;
            SourceSize = Functions.FileSize(source_file.Length);
            SourceDate = source_file.LastWriteTimeUtc.ToString();
            SourceTooltip = source_file.DirectoryName;
            TargetFile = target_file;
            TargetSize = Functions.FileSize(target_file.Length);
            TargetDate = target_file.LastWriteTimeUtc.ToString();
            TargetTooltip = target_file.DirectoryName;
            if (FileInfoCombonents.FilesInformation.ContainsKey(source_file.Extension.ToLower()))
            {
                TargetIcon = SourceIcon = FileInfoCombonents.FilesInformation[source_file.Extension.ToLower()].Bitmap64;
                SourceType = TargetType = FileInfoCombonents.FilesInformation[source_file.Extension.ToLower()].Description;
            }
            else
            {
                TargetIcon = SourceIcon = FileInfoCombonents.FilesInformation["."].Bitmap64;
                SourceType = TargetType = source_file.Extension.ToLower().Substring(1).ToUpper() + " File";
            }
            target_ischecked = source_ischecked = false;
        }

        private String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        FileInfo source_file;
        public FileInfo SourceFile
        {
            get { return source_file; }
            set { source_file = value; }
        }

        private BitmapImage source_icon;
        public BitmapImage SourceIcon
        {
            get { return source_icon; }
            set { source_icon = value; }
        }

        private String source_type;
        public string SourceType
        {
            get { return source_type; }
            set { source_type = value; }
        }

        private String source_size;
        public string SourceSize
        {
            get { return source_size; }
            set { source_size = value; }
        }

        private String source_date;
        public string SourceDate
        {
            get { return source_date; }
            set { source_date = value; }
        }

        private String source_tooltip;
        public string SourceTooltip
        {
            get { return source_tooltip; }
            set { source_tooltip = value; }
        }

        private bool? source_ischecked;
        public Boolean? SourceIsChecked
        {
            get { return source_ischecked; }
            set { source_ischecked = value; }
        }

        FileInfo target_file;
        public FileInfo TargetFile
        {
            get { return target_file; }
            set { target_file = value; }
        }

        private BitmapImage target_icon;
        public BitmapImage TargetIcon
        {
            get { return target_icon; }
            set { target_icon = value; }
        }

        private String target_type;
        public string TargetType
        {
            get { return target_type; }
            set { target_type = value; }
        }

        private String target_size;
        public string TargetSize
        {
            get { return target_size; }
            set { target_size = value; }
        }

        private String target_date;
        public string TargetDate
        {
            get { return target_date; }
            set { target_date = value; }
        }

        private String target_tooltip;
        public string TargetTooltip
        {
            get { return target_tooltip; }
            set { target_tooltip = value; }
        }

        private bool? target_ischecked;
        public Boolean? TargetIsChecked
        {
            get { return target_ischecked; }
            set { target_ischecked = value; }
        }
    }

    public class OperationErrorListView
    {
        public OperationErrorListView(FileSystemInfo item, string Error)
        {
            FileName = item.Name;
            Item = item;
            ErrorMsg = "Error: " + Error;
            if (item is DirectoryInfo)
            {
                Icon = FileInfoCombonents.FilesInformation["directory"].Bitmap64;
                FileType = "Directory";
            }
            else if (FileInfoCombonents.FilesInformation.ContainsKey(item.Extension.ToLower()))
            {
                Icon = FileInfoCombonents.FilesInformation[item.Extension.ToLower()].Bitmap64;
                FileType = FileInfoCombonents.FilesInformation[item.Extension.ToLower()].Description;
            }
            else
            {
                Icon = FileInfoCombonents.FilesInformation["."].Bitmap64;
                FileType = item.Extension.Length > 0 ? item.Extension.ToLower().Substring(1).ToUpper() + " File" : "File";
            }
            skip_ischecked = retry_ischecked = false;
        }

        private String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        FileSystemInfo item;
        public FileSystemInfo Item
        {
            get { return item; }
            set { item = value; }
        }

        private BitmapImage item_icon;
        public BitmapImage Icon
        {
            get { return item_icon; }
            set { item_icon = value; }
        }

        private String item_type;
        public string FileType
        {
            get { return item_type; }
            set { item_type = value; }
        }

        private String error_msg;
        public string ErrorMsg
        {
            get { return error_msg; }
            set { error_msg = value; }
        }

        private bool? retry_ischecked;
        public Boolean? RetryIsChecked
        {
            get { return retry_ischecked; }
            set
            { 
                retry_ischecked = value;
                if (value == true)
                    skip_ischecked = false;
                else if (value == false)
                    skip_ischecked = true;
            }
        }

        private bool? skip_ischecked;
        public Boolean? SkipIsChecked
        {
            get { return skip_ischecked; }
            set 
            { 
                skip_ischecked = value;
                if (value == true)
                    retry_ischecked = false;
                else if (value == false)
                    retry_ischecked = true;
            }
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
        Abort = 7
    }

    public enum Operation
    {
        Copy = 0,
        Attributes = 1,
        Delete = 2,
        Rename = 3
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

    static class Functions
    {
        public static string StandardImageSource = "pack://application:,,,/File Manager;component/Images/Icons/", SystemDrive = "c:\\";

        public static string GetDriveImage(DriveType type, bool isSystem, bool isReady, string StandardImageSource)
        {
            switch (type)
            {
                case DriveType.Fixed:
                    if (isSystem)
                        return StandardImageSource + "system-hard-disk.png";
                    else
                        return StandardImageSource + "hard-disk.png";
                case DriveType.CDRom:
                    if (isReady)
                        return StandardImageSource + "dvd-media.png";
                    else
                        return StandardImageSource + "optical-drive.png";
                case DriveType.Removable:
                    if (isReady)
                        return StandardImageSource + "removable-usb.png";
                    else
                        return StandardImageSource + "removable-media.png";
            }
            return StandardImageSource + "hard-disk.png";
        }

        internal static string FileSize(double SizeInByte)
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
            switch (num)
            {
                case 0:
                    return "Byte";
                case 1:
                    return "KB";
                case 2:
                    return "MB";
                case 3:
                    return "GB";
                case 4:
                    return "TB";
                case 5:
                    return "PB";
                default:
                    return "Byte";
            }
        }

        internal static string GetDriveDescription(DriveType driveType)
        {
            return "";
        }




        static Window window;
        static string message;
        static bool condition;
        static double left;
        public static string[] spliter = { "\\" };


        

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

        // Shuck Window Functions
        public static void ShuckWindow(Window win, bool cond, string msg)
        {
            window = win;
            message = msg;
            condition = cond;
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
            if (condition)
                MessageBox.Show(message);
            window.Left = left;
            //button.IsEnabled = true;
        }

        static void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(50);
            ((BackgroundWorker)sender).ReportProgress(0);
        }

        //end of Shuck Window Functions


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

    public class BigFileListView
    {
        public BigFileListView(string file_path, string file_name, BitmapImage img_source, string file_type, string file_size, string percent, string size_from_all, Brush background, bool is_directory)
        {
            FullPath = file_path;
            FileName = file_name;
            Image = img_source;
            FileType = file_type;
            FileSize = file_size;
            Percent = percent;
            SizeFromAll = size_from_all;
            Background = background;
            isDirectory = is_directory;
        }

        private String file_path;
        public string FullPath
        {
            get { return file_path; }
            set { file_path = value; }
        }

        private String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private BitmapImage img_source;
        public BitmapImage Image
        {
            get { return img_source; }
            set { img_source = value; }
        }

        private String file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }

        private String file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }

        private String percent;
        public string Percent
        {
            get { return percent; }
            set { percent = value; }
        }

        private String size_from_all;
        public string SizeFromAll
        {
            get { return size_from_all; }
            set { size_from_all = value; }
        }

        private Brush background;
        public Brush Background
        {
            get { return background; }
            set { background = value; }
        }

        public bool isDirectory;

        //public static readonly RoutedEvent MouseDoubleClick = EventManager.RegisterRoutedEvent("MouseDoubleClick", RoutingStrategy.Bubble, typeof(MouseButtonEventHandler), typeof(BigFileListView));


        //public event MouseButtonEventHandler Tap
        //{
        //    add { System.Windows.AddHandler(MouseDoubleClick, value); }
        //    remove { System.Windows.RemoveHandler(MouseDoubleClick, value); }
        //}

        //void RaiseTapEvent()
        //{
        //    RoutedEventArgs newEventArgs = new RoutedEventArgs(BigFileListView.MouseDoubleClick);
        //    RaiseEvent(newEventArgs);
        //}

        //protected void OnDoubleClick()
        //{
        //    RaiseTapEvent();
        //}
    }

    public class DublicateFiles_ListView
    {
        public DublicateFiles_ListView() { Dublicats = new List<string>(); }
        public DublicateFiles_ListView(string file_path, BitmapImage img_source, string file_type, string file_size, List<string>/*ItemCollection*/ dublicats, bool? is_checked, Point Tag)
        {
            FullPath = file_path;
            Image = img_source;
            FileType = file_type;
            FileSize = file_size;
            Dublicats = dublicats;
            Point = Tag;
        }

        private bool? is_cheched;
        public Boolean? IsChecked
        {
            get { return is_cheched; }
            set { is_cheched = value; }
        }

        private List<string> dublicats;
        public List<string> Dublicats
        {
            get { return dublicats; }
            set { dublicats = value; }
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

        private String file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }

        private String file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }

        Point point;
        public Point Point
        {
            get { return point; }
            set { point = value; }
        }
    }

    public class RenamerListView
    {
        public RenamerListView(string file_path, BitmapImage img_source, FileInfo file)
        {
            FullPath = file_path;
            Image = img_source;
            File = file;
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
    }

    public class Renamer2ListView
    {
        public Renamer2ListView(string file_path, BitmapImage img_source, FileInfo file, List<string> possible_ext)
        {
            FullPath = file_path;
            Image = img_source;
            File = file;
            PossibleExt = possible_ext;
            SelectedExt = -1;
        }

        public Renamer2ListView(string file_path, BitmapImage img_source, FileInfo file)
        {
            FullPath = file_path;
            Image = img_source;
            File = file;
            possible_ext = new List<string>();
            SelectedExt = -1;
        }

        List<string> possible_ext;
        public List<string> PossibleExt
        {
            get { return possible_ext; }
            set { possible_ext = value; }
        }

        int selected_ext;
        public int SelectedExt
        {
            get { return selected_ext; }
            set { selected_ext = value; }
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

        bool done;
        public Boolean Done
        {
            get { return done; }
            set
            {
                if (value)
                {
                    status_img = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/tick.png"));
                    status_txt = "Done";
                    status_color = Brushes.Green;
                }
                else
                {
                    status_img = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/error.png"));
                    status_txt = "Error";
                    status_color = Brushes.Red;
                }
                done = value;
            }
        }

        BitmapImage status_img;
        private BitmapImage StatusImg { get { return status_img; } set { status_img = value; } }

        private string status_txt;
        public String StatusTxt { get { return status_txt; } set { status_txt = value; } }

        Brush status_color;
        public Brush StatusColor { get { return status_color; } set { status_color = value; } }

        public FileInfo File { get; set; }
    }

    public class CompresserListView
    {
        public CompresserListView(FileSystemInfo file_system, long file_size)
        {
            int index = Functions.GetFileIndex(file_system.Extension);
            file = file_system;
            FileName = file_system.Name;
            Image = FileInformation.file_bitmap_24[index];
            FileType = file_system is FileInfo ? Functions.GetFileType(index, file_system.Extension) : "Directory";
            file_size1 = file_size;
            FileSize = Functions.FileSize(file_size);
        }

        long file_size1;

        FileSystemInfo file;
        public FileSystemInfo File
        {
            get { return file; }
            set { file = value; }
        }

        private String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private BitmapImage img_source;
        public BitmapImage Image
        {
            get { return img_source; }
            set { img_source = value; }
        }

        private String file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }

        private String file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
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
            type = Functions.GetFileType(Functions.GetFileIndex(item.Extension), item.Extension);
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