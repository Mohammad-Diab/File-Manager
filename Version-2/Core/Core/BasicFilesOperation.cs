using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Core
{
    //class BasicFilesOperation
    //{
    //    public List<FileInfo> GetFiles(string Path, FileCategory FileType, bool IncludeSubDirectories)
    //    {
    //        List<FileInfo> list = new List<FileInfo>();
    //        try
    //        {
    //            DirectoryInfo first_directory = new DirectoryInfo(Path);
    //            list.AddRange(first_directory.GetFiles().Select((x) => { x.Extension.ToLower().Equals(""); }));
    //            first_directory.GetDirectories();
    //        }
    //        catch(Exception ex){ }
    //        return list;

    //    }
    
    //}

    //class BaseDataSorce
    //{

    //}

    //public enum FileCategory
    //{
    //    Drive = 0,
    //    Directory = 1,
    //    Text = 2,
    //    TextCanRunAsApplication = 3,
    //    Image = 4,
    //    Audio = 5,
    //    Vedio = 6,
    //    Html = 7,
    //    Archive = 8,
    //    DiskImage = 9,
    //    Font = 10,
    //    Document = 11,
    //    Application = 12,
    //    CannotOpen = 13,
    //    GameFile = 14,
    //    Unknown = 100
    //}

    //public static class Functions
    //{
    //    public static string GetFileType(string v)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public static string FileSize(long length, int v)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public static string GetDownloadsPath()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public static BitmapImage GetDirectoryImage(string v)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public static BitmapImage GetDriveImage(DriveInfo driveInfo, int v)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class GeneralPurpose
    //{
    //    internal GeneralPurpose() { }

    //    public GeneralPurpose(FileInfo file, string file_path)
    //    {
    //        FullPath = file_path;//file name or file path;
    //        Image = Functions.GetFileImage24(file.Extension);
    //        File = file == null ? new FileInfo(file_path) : file;
    //        //isDirectory = false;
    //    }

    //    public GeneralPurpose(string directory_path, DirectoryInfo directory)
    //    {
    //        FullPath = directory_path;      //file name or file path;
    //        Image = Functions.GetFileImage24("directory");
    //        Directory = directory == null ? new DirectoryInfo(directory_path) : directory;
    //        //isDirectory = true;
    //    }

    //    private String file_path;
    //    public string FullPath
    //    {
    //        get { return file_path; }
    //        set { file_path = value; }
    //    }

    //    private BitmapImage img_source;
    //    public BitmapImage Image
    //    {
    //        get { return img_source; }
    //        set { img_source = value; }
    //    }

    //    public FileInfo File { get; set; }

    //    public DirectoryInfo Directory { get; set; }

    //    //public bool isDirectory;

    //    //public int CompareTo(object obj)
    //    //{
    //    //    GeneralPurpose x = (GeneralPurpose)obj;
    //    //    if (x.isDirectory)
    //    //        if (this.isDirectory)
    //    //            return (x.Directory.FullName.CompareTo(this.Directory.FullName));
    //    //        else
    //    //            return 1;
    //    //    else
    //    //        if (this.isDirectory)
    //    //            return -1;
    //    //        else
    //    //            return (x.File.FullName.CompareTo(this.File.FullName));
    //    //}

    //    //public override bool Equals(object obj)
    //    //{
    //    //    if (obj != null)
    //    //    {
    //    //        GeneralPurpose x = (GeneralPurpose)obj;
    //    //        if (x.isDirectory)
    //    //            if (this.isDirectory)
    //    //                return (x.Directory.FullName.Equals(this.Directory.FullName));
    //    //            else
    //    //                return false;
    //    //        else
    //    //            if (this.isDirectory)
    //    //                return false;
    //    //            else
    //    //                return (x.File.FullName.Equals(this.File.FullName));
    //    //    }
    //    //    return false;
    //    //}
    //}

    //public class FileOperationItem /*: IComparable*/
    //{
    //    //public bool? Over;
    //    public FileSystemInfo Item;
    //    //public long Size;
    //    public bool IsCut;
    //    //public bool Done;
    //    public object short_path;

    //    public FileOperationItem(FileInfo File, bool IsCut, object short_path)
    //    {
    //        Item = File;
    //        this.IsCut = IsCut;
    //        this.short_path = short_path;
    //    }

    //    public FileOperationItem(DirectoryInfo Directory, /*long DirectorySize,*/ bool IsCut/*, bool? Over*/, object short_path)
    //    {
    //        Item = Directory;
    //        //Size = DirectorySize;
    //        this.IsCut = IsCut;
    //        //Done = false;
    //        this.short_path = short_path;
    //        //this.Over = Over;
    //    }

    //    //public FileOperationItem(DirectoryInfo directoryInfo, string short_path)
    //    //{
    //    //    Item = directoryInfo;
    //    //    //Over = false;
    //    //    //Size = -1;
    //    //    IsCut = false;
    //    //    //Done = false;
    //    //    this.short_path = short_path;
    //    //}

    //    //public FileElement(string file_path, long file_Size, string extension, string short_name, bool remove, bool? over)
    //    //{
    //    //    this.extension = extension;
    //    //    files_full_name = file_path;
    //    //    file_short_name = short_name;
    //    //    files_size = file_Size;
    //    //    remove_file = remove;
    //    //    Done = false;
    //    //    this.over = over;
    //    //}

    //    //public int CompareTo(object obj)
    //    //{
    //    //    if (obj == null)
    //    //        return 1;
    //    //    return ((FileOperationItem)obj).Size.CompareTo(this.Size);
    //    //}
    //}


    //public enum MyMessageBoxResult
    //{
    //    None = 0,
    //    Yes = 1,
    //    No = 2,
    //    OK = 3,
    //    Cancel = 4,
    //    Skip = 5,
    //    Rety = 6,
    //    Abort = 7,
    //    Button1 = 101,
    //    Button2 = 102,
    //    Button3 = 103,
    //    Button4 = 104,
    //    Button5 = 105,
    //    Button6 = 106,
    //    Button7 = 107,
    //    Button8 = 108,
    //    Button9 = 109,
    //    Button10 = 110,
    //    Button11 = 111,
    //}

    //public enum SortItemsBy
    //{
    //    Random = 0,
    //    Name = 1,
    //    Size = 2,
    //    Type = 3,
    //    DateCreation = 4,
    //    DateModified = 5,
    //    DateLastAccess = 6,

    //    //and More
    //    RandomDescending = 100,
    //    NameDescending = 101,
    //    SizeDescending = 102,
    //    TypeDescending = 103,
    //    DateCreationDescending = 104,
    //    DateModifiedDescending = 105,
    //    DateLastAccessDescending = 106

    //}

}
