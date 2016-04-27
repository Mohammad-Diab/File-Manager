using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Core
{
    public class Class1
    {
        public static List<FileInfo> GetFiles(DirectoryInfo directory, FileType image, bool sub_directories, bool hidden_files)
        {
            List<FileInfo> neighbors = new List<FileInfo>();
            GetFiles(directory, image, hidden_files, sub_directories, neighbors);
            return neighbors;
        }

        private static void GetFiles(DirectoryInfo directory, FileType files_type, bool hidden_files, bool sub_directories, List<FileInfo> neighbors)
        {
            FileInfo[] files = null;
            try
            { files = directory.GetFiles(); }
            catch (Exception) { }
            if (files != null)
            {
                foreach (var i in files)
                {
                    if (!(i.Attributes.HasFlag(FileAttributes.Hidden) && !hidden_files))
                    {
                        if (FileInfoCombonents.FilesInformation.ContainsKey(i.Extension.ToLower()) && FileInfoCombonents.FilesInformation[i.Extension.ToLower()].Category == files_type)
                            neighbors.Add(i);
                    }
                }
            }
            if (sub_directories)
            {
                DirectoryInfo[] directories = null;
                try
                { directories = directory.GetDirectories(); }
                catch (Exception) { }
                foreach (var i in directories)
                {
                    if (!(i.Attributes.HasFlag(FileAttributes.Hidden) && !hidden_files))
                        GetFiles(i, files_type, hidden_files, sub_directories, neighbors);
                }
            }
        }

        public static bool IsAdmin = false;
        private void Application_Startup()
        {
            IsAdmin = IsRunningAsAdmin();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter BF = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            if (File.Exists("FileInfo.dat"))
            {
                List<BasicFileInfoCombonents> info = null;
                Stream filestream = File.Open("FileInfo.dat", FileMode.Open);
                bool err = false;
                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        info = (List<BasicFileInfoCombonents>)BF.Deserialize(filestream);
                        err = false; break;
                    }
                    catch (Exception)
                    {
                        System.Threading.Thread.Sleep(250);
                        err = true;
                    }
                }
                filestream.Close();
                if (err)
                {
                    MessageBox.Show("Something wrong, sorry the app will close now.");
                    //Messages.ShowMessage("Something wrong, sorry the app will close now.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "Can not load 'FileInfo.dat' File.");
                    //Shutdown();
                }

                if (info == null || !BasicFileInfoCombonents.ConvertToStandard(info))
                {
                    MessageBox.Show("Something wrong, sorry the app will close now.");
                    //Messages.ShowMessage("Something wrong, sorry the app will close now.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "The file 'FileInfo.dat' is not valid.");
                    //Shutdown();
                }
            }
            else
            {
                MessageBox.Show("Something wrong, sorry the app will close now.");
                //Messages.ShowMessage("Something wrong, sorry the app will close now.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "Can not find 'FileInfo.dat' File.");
                //Shutdown();
            }
        }

        static public bool IsRunningAsAdmin()
        {
            System.Security.Principal.WindowsIdentity cur = System.Security.Principal.WindowsIdentity.GetCurrent();
            foreach (System.Security.Principal.IdentityReference role in cur.Groups)
            {
                if (role.IsValidTargetType(typeof(System.Security.Principal.SecurityIdentifier)))
                {
                    System.Security.Principal.SecurityIdentifier sid = (System.Security.Principal.SecurityIdentifier)role.Translate(typeof(System.Security.Principal.SecurityIdentifier));
                    if (sid.IsWellKnown(System.Security.Principal.WellKnownSidType.AccountAdministratorSid) || sid.IsWellKnown(System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid))
                        return true;
                }
            }
            return false;
        }
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

        FileType category;
        public FileType Category
        {
            get { return category; }
            set { category = value; }
        }

        string category_string;
        public String CategoryString
        {
            get { return category_string; }
            set { category_string = value; }
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

        public FileInfoCombonents(string extention, string image_file, string description, bool has_subtext, string subtext, FileType category, bool may_danger)
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

        public FileInfoCombonents(string extention, string image_file, string description, bool has_subtext, string subtext, FileType category, bool may_danger, List<int[]> Ids, int offset)
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
        public string Extention;
        public string ImageFile;
        public string Description;
        public bool HasSubtext;
        public string Subtext;
        public FileType Type;
        public bool MayDanger;
        public List<int[]> IDs;
        public int Offset;

        public BasicFileInfoCombonents(string extention, string image_file, string description, bool has_subtext, string subtext, FileType category, bool may_danger)
        {
            Extention = extention;
            ImageFile = image_file;
            Description = description;
            HasSubtext = has_subtext;
            if (HasSubtext)
                Subtext = subtext;
            else
                Subtext = "-";
            Type = category;
            MayDanger = may_danger;
            IDs = new List<int[]>();
        }

        public BasicFileInfoCombonents(string extention, string image_file, string description, bool has_subtext, string subtext, FileType category, bool may_danger, List<string> ids, string offset)
        {
            Extention = extention;
            ImageFile = image_file;
            Description = description;
            HasSubtext = has_subtext;
            if (HasSubtext)
                Subtext = subtext;
            else
                Subtext = "-";
            Type = category;
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
                        FileInfoCombonents.FilesInformation.Add(i.Extention, new FileInfoCombonents(i.Extention, i.ImageFile, i.Description, i.HasSubtext, i.Subtext, i.Type, i.MayDanger, i.IDs, i.Offset));
                    else
                        FileInfoCombonents.FilesInformation.Add(i.Extention, new FileInfoCombonents(i.Extention, i.ImageFile, i.Description, i.HasSubtext, i.Subtext, i.Type, i.MayDanger));
            }
            catch (Exception) { return false; }
            return true;
        }
    }

    public enum FileType
    {
        All = 1,
        Archive = 2,
        Document = 3,
        Application = 4,
        Html = 5,
        TextCanRunAsApplication = 6,
        DiskImage = 7,
        Image = 8,
        Text = 9,
        CannotOpen = 10,
        Font = 11,
        Directory = 12,
        Audio = 13,
        Vedio = 14,
        Unknown = 1000
    }
}
