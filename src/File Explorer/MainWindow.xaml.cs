using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        DriveInfo[] Drives;
        List<string> directorys;
        List<string> files;
        DirectoryInfo current_Directory, View_Directory;
        FileInfo View_File;
        Stack<string> back, front;
        string copydir, path = String.Empty, imagesource = "pack://application:,,,/File Manager;component/Images/Icons/";
        //static string computer_Name, operating_System, CPU = "!", Memory_Size;
        List<string> copyfiles, copydirectory;
        int sub_dirs, sub_files;
        static public string to_go_path, new_Name;
        static public bool go_to_path;
        bool hiddenfiles = false, show_image = false;
        string systemDrive = "", PrimPath;
        string[] nn = new string[14000];
        public MainWindow()
        {
            InitializeComponent();
            copyfiles = new List<string>();
            copydirectory = new List<string>();
            directorys = new List<string>();
            files = new List<string>();
            back = new Stack<string>();
            front = new Stack<string>();
            new_Name = null;
        }

        public MainWindow(string PrimPath)
        {
            InitializeComponent();
            copyfiles = new List<string>();
            copydirectory = new List<string>();
            directorys = new List<string>();
            files = new List<string>();
            back = new Stack<string>();
            front = new Stack<string>();
            new_Name = null;
            this.PrimPath = PrimPath;
            //if (Directory.Exists(PrimPath))
            //{
            //    Navigation(PrimPath);
            //    back.Push(PrimPath);
            //    Path = path;
            //}
        }

        private void Main_Window_Loaded(object sender, RoutedEventArgs e)
        {
            systemDrive = Environment.SystemDirectory.Substring(0, 3).ToLower();
            if (PrimPath == null)
            {
                Navigation("Computer");
                back.Push("Computer");
            }
            else
            {
                Navigation(PrimPath);
                back.Push(PrimPath);
            }
            backMenu.Items.Add(Path);
        }

        private void Traversing(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton.Equals(MouseButton.Left)&&((Explorer.SelectedIndex > -1 && Explorer.SelectedIndex < directorys.Count) || path.Equals(String.Empty)))
            {
                if (path.Equals(String.Empty))
                    if (Drives[Explorer.SelectedIndex].IsReady)
                        path = Drives[Explorer.SelectedIndex].Name;
                    else
                        MessageBox.Show("\"" + Drives[Explorer.SelectedIndex].Name + "\" drive is not Ready", "File Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    path = directorys[Explorer.SelectedIndex];
                Navigation(path);
                back.Push(path);
                backMenu.Items.Add(Path);
            }
            else if (Explorer.SelectedIndex >= directorys.Count && Explorer.SelectedIndex < directorys.Count + files.Count)
            {
                ProcessStartInfo PSI = new ProcessStartInfo(files[Explorer.SelectedIndex - directorys.Count]);
                System.Diagnostics.Process.Start(PSI);
                PSI.Verb = "runas";
                //psi.UseShellExecute = true;
                //files[Explorer.SelectedIndex - privous_Directorys.Count].Open(FileMode.Open);
            }
        }

        private void Go_UP(object sender, RoutedEventArgs e)
        {
            List<string> split_path = path.Split('\\').ToList<string>();
            while (split_path.Remove(""))
            { }
            if (split_path.Count == 1)
            {
                Drives = DriveInfo.GetDrives();
                Explorer.Items.Clear();
                foreach (var i in Drives)
                    if (i.DriveType.ToString().ToLower().Equals("fixed"))
                        Explorer.Items.Add(Make_Grid_For_Explorer(GetImageSource(imagesource, i.DriveType.ToString(), i.IsReady && i.Name.ToLower().Equals(systemDrive)), i.Name));
                    else
                        Explorer.Items.Add(Make_Grid_For_Explorer(GetImageSource(imagesource, i.DriveType.ToString(), i.IsReady), i.Name));
                path = String.Empty;
                back.Push(path);
                Path = path;
                backMenu.Items.Add(Path);
                View_Items_Basic_Info(path, path);
            }
            else if (split_path.Count > 1)
            {
                path = String.Empty;
                for (int i = 0; i < split_path.Count - 1; i++)
                    path = path + split_path[i] + "\\";
                Navigation(path);
                back.Push(path);
                backMenu.Items.Add(Path);
                if (split_path.Count == 2)
                    View_Items_Basic_Info(path, "Drive");
                else
                    View_Items_Basic_Info(path, "Folder");
            }
        }

        private void Traversing_KeyBoard(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                path = Address.Text;
                Navigation(path);
                back.Push(path);
                backMenu.Items.Add(path);
            }
        }

        private void Go_Back(object sender, RoutedEventArgs e)
        {
            path = back.Pop();
            front.Push(path);
            Navigation(path);
            Path = path;
            //View_Items_Basic_Info(path, "Folder");
        }

        private void Navigation(string path)
        {
            string unit;
            if (path != "Wrong")
            {
                Explorer.Items.Clear();
                if (path == "" || path == "Computer")
                {
                    Drives = DriveInfo.GetDrives();
                    foreach (var i in Drives)
                        if (i.DriveType.ToString().ToLower().Equals("fixed"))
                            Explorer.Items.Add(Make_Grid_For_Explorer(GetImageSource(imagesource, i.DriveType.ToString(), i.IsReady && i.Name.ToLower().Equals(systemDrive)), i.Name));
                        else
                            Explorer.Items.Add(Make_Grid_For_Explorer(GetImageSource(imagesource, i.DriveType.ToString(), i.IsReady || i.Name.ToLower().Equals(systemDrive)), i.Name));
                }
                else
                {
                    current_Directory = new DirectoryInfo(path);
                    directorys.Clear();
                    files.Clear();
                    //foreach (var i in Directory.GetDirectories(current_Directory).GetDirectories().ToList<DirectoryInfo>())
                    foreach (var i in Directory.GetDirectories(path))
                        if (hiddenfiles || (!hiddenfiles && !new DirectoryInfo(i).Attributes.HasFlag(FileAttributes.Hidden)))
                        {
                            //sub_Directorys.Add(i);
                            Explorer.Items.Add(Make_Grid_For_Explorer(GetImageSource(imagesource, "Folder", true), Name_of_File(i)));
                            directorys.Add(i);
                        }
                    foreach (var i in Directory.GetFiles(path))
                        if (hiddenfiles || (!hiddenfiles && !File.GetAttributes(i).HasFlag(FileAttributes.Hidden)))
                        {
                            files.Add(i);
                            Explorer.Items.Add(Make_Grid_For_Explorer(GetImageSource(imagesource, new FileInfo(i).Extension, true), Name_of_File(i), "Size: " + File_Size(new FileInfo(i).Length, out unit) + " " + unit, "Type: " + new FileInfo(i).Extension + " File"));
                        }
                    //sub_Directorys = Directory.GetDirectories(path).ToList<string>();
                    //files = Directory.GetFiles(path).ToList<String>();
                    //foreach (var i in current_Directory.GetFiles())
                    //    files.Add(i.FullName);
                    //sub_Directorys = current_Directory.GetDirectories().ToList<DirectoryInfo>();
                    //files = current_Directory.GetFiles().ToList<FileInfo>();
                    //for (int i = 0; i < sub_Directorys.Count; i++)
                    //    if (!(hiddenfiles || (!hiddenfiles && !new DirectoryInfo(sub_Directorys[i]).Attributes.HasFlag(FileAttributes.Hidden))))
                    //    { sub_Directorys.RemoveAt(i); i--; }
                    //for (int i = 0; i < files.Count; i++)
                    //    if (!(hiddenfiles || (!hiddenfiles && !new FileInfo(files[i]).Attributes.HasFlag(FileAttributes.Hidden))))
                    //    { files.RemoveAt(i); i--; }
                    //directorys.Clear();
                    //foreach (var i in sub_Directorys)
                    //{

                    //}
                    //foreach (var i in files)
                    //Explorer.Items.Add(Make_Grid_For_Explorer(GetImageSource(imagesource, new FileInfo(i).Extension, true), i, "Size: " + File_Size(i.Length, out unit) + " " + unit, "Type: " + new FileInfo(i).Extension + " File"));
                    //if (IsImage(i.Extension))
                    //    Explorer.Items.Add(Make_Grid_For_Explorer(i.FullName, i.Name));
                    //else
                    Path = path;
                }
                statebar.Text = Explorer.Items.Count + " Item(s)";
            }
        }

        private void Go_Front(object sender, RoutedEventArgs e)
        {
            path = front.Pop();
            back.Push(path);
            Navigation(path);
            Path = path;
            //View_Items_Basic_Info(path, "Folder");
        }

        private void View_Items_Basic_Info(object sender, SelectionChangedEventArgs e)
        {
            if (!Path.Equals("Computer"))
            {
                if (Explorer.SelectedIndex > -1 && Explorer.SelectedIndex < directorys.Count)
                    View_Items_Basic_Info(directorys[Explorer.SelectedIndex], "Folder");
                else if (Explorer.SelectedIndex >= directorys.Count && Explorer.SelectedIndex < directorys.Count + files.Count)
                    View_Items_Basic_Info(files[Explorer.SelectedIndex - directorys.Count], "File");
            }
            else if (Explorer.SelectedIndex > -1)
                View_Items_Basic_Info(Drives[Explorer.SelectedIndex].Name, "Drive");
        }

        private void Preview_Items(object sender, KeyEventArgs e)
        {
        }

        private string Path
        {
            set
            {
                if (value == String.Empty)
                    this.Address.Text = "Computer";
                else
                    this.Address.Text = value;
            }
            get
            { return this.Address.Text; }
        }

        private ListViewItem Make_Grid_For_Explorer(string image_Source, string name,string Size,string Type)
        {
            ListViewItem Item1 = new ListViewItem();
            if (show_image)
            {
                Grid Grid1 = new Grid();
                Grid1.HorizontalAlignment = HorizontalAlignment.Stretch;
                System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
                img1.Height = img1.Width = 64;
                img1.HorizontalAlignment = HorizontalAlignment.Left;
                img1.VerticalAlignment = VerticalAlignment.Stretch;
                img1.Source = new BitmapImage(new Uri(image_Source)).Clone();
                //img1.Stretch = Stretch.Uniform;
                TextBlock text = new TextBlock();
                text.Text = name;
                text.HorizontalAlignment = HorizontalAlignment.Left;
                text.VerticalAlignment = VerticalAlignment.Stretch;
                text.Margin = new Thickness(70, 25, 0, 0);
                text.TextTrimming = TextTrimming.CharacterEllipsis;

                TextBlock size = new TextBlock();
                size.Text = Size;
                size.HorizontalAlignment = HorizontalAlignment.Left;
                size.VerticalAlignment = VerticalAlignment.Top;
                size.Margin = new Thickness(450, 15, 0, 0);
                size.TextTrimming = TextTrimming.CharacterEllipsis;

                TextBlock type = new TextBlock();
                type.Text = Type;
                type.HorizontalAlignment = HorizontalAlignment.Left;
                type.VerticalAlignment = VerticalAlignment.Top;
                type.Margin = new Thickness(450, 35, 0, 0);
                type.TextTrimming = TextTrimming.CharacterEllipsis;

                Grid1.Children.Add(img1);
                Grid1.Children.Add(text);
                Grid1.Children.Add(type);
                Grid1.Children.Add(size);
                Item1.Content = Grid1;
            }
            else
            {
                Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
                Item1.VerticalAlignment = VerticalAlignment.Stretch;
                Item1.Content = name;
            }
            return Item1;
        }

        private ListViewItem Make_Grid_For_Explorer(string image_Source, string name)
        {
            if (show_image)
            {
                ListViewItem Item1 = new ListViewItem();
                Grid Grid1 = new Grid();
                Grid1.HorizontalAlignment = HorizontalAlignment.Stretch;
                System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
                img1.Height = img1.Width = 64;
                img1.HorizontalAlignment = HorizontalAlignment.Left;
                img1.VerticalAlignment = VerticalAlignment.Stretch;
                img1.Source = new BitmapImage(new Uri(image_Source)).Clone();
                //img1.Stretch = Stretch.Uniform;
                TextBlock text = new TextBlock();
                text.Text = name;
                text.Margin = new Thickness(70, 25, 0, 0);
                text.HorizontalAlignment = HorizontalAlignment.Stretch;
                text.VerticalAlignment = VerticalAlignment.Stretch;
                text.TextTrimming = TextTrimming.CharacterEllipsis;

                Grid1.Children.Add(img1);
                Grid1.Children.Add(text);
                Item1.Content = Grid1;
                return Item1;
            }
            else
            {
                ListViewItem Item1 = new ListViewItem();
                Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
                Item1.VerticalAlignment = VerticalAlignment.Stretch;
                Item1.Content = name;
                return Item1;
            }
        }

        static public string GetImageSource(string imagesource, string type, bool Condition)
        {
            if (type.Length > 0 && type[type.Length - 1] == '_')
                imagesource = imagesource + "archive-cab.png";
            else
                switch (type.ToLower())
                {
                    case "computer":
                        imagesource = imagesource + "computer.png";
                        break;
                    case "fixed":
                        if (Condition)
                            imagesource = imagesource + "systemharddisk.png";
                        else
                            imagesource = imagesource + "harddisk.png";
                        break;
                    case "cdrom":
                        if (Condition)
                            imagesource = imagesource + "dvdmedia.png";
                        else
                            imagesource = imagesource + "opticaldrive.png";
                        break;
                    case "removable":
                        if (Condition)
                            imagesource = imagesource + "removableusb.png";
                        else
                            imagesource = imagesource + "removable-media.png";
                        break;
                    case "folder":
                        imagesource = imagesource + "folder.png";
                        break;
                    case ".txt":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".log":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".sh":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".bsh":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".bat":
                        imagesource = imagesource + "text-x.png";
                        break;
                    case ".cmd":
                        imagesource = imagesource + "text-x.png";
                        break;
                    case ".nt":
                        imagesource = imagesource + "text-x.png";
                        break;
                    case ".c":
                        imagesource = imagesource + "text-c.png";
                        break;
                    case ".cpp":
                        imagesource = imagesource + "text-c++.png";
                        break;
                    case ".cs":
                        imagesource = imagesource + "text-cs.png";
                        break;
                    case ".html":
                        imagesource = imagesource + "text-html.png";
                        break;
                    case ".mht":
                        imagesource = imagesource + "text-html.png";
                        break;
                    case ".htm":
                        imagesource = imagesource + "text-html.png";
                        break;
                    case ".shtml":
                        imagesource = imagesource + "text-html.png";
                        break;
                    case ".shtm":
                        imagesource = imagesource + "text-html.png";
                        break;
                    case ".xhtml":
                        imagesource = imagesource + "text-html.png";
                        break;
                    case ".hta":
                        imagesource = imagesource + "text-html.png";
                        break;
                    case ".ini":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".inf":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".reg":
                        imagesource = imagesource + "text-reg.png";
                        break;
                    case ".url":
                        imagesource = imagesource + "text-html.png";
                        break;
                    case ".java":
                        imagesource = imagesource + "text-java.png";
                        break;
                    case ".js":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".nfo":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".pas":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".inc":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".pl":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".pm":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".plx":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".php":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".php3":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".phtml":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".ps":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".ps1":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".py":
                        imagesource = imagesource + "text-python.png";
                        break;
                    case ".pyw":
                        imagesource = imagesource + "text-python.png";
                        break;
                    case ".sql":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".tex":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".vb":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".vbs":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".v":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".vhdl":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".xml":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".xsml":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".xsl":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".xsd":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".kml":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".wsdl":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".yml":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".docx":
                        imagesource = imagesource + "text-richtext.png";
                        break;
                    case ".doc":
                        imagesource = imagesource + "text-richtext.png";
                        break;
                    case ".odt":
                        imagesource = imagesource + "text-richtext.png";
                        break;
                    case ".rtf":
                        imagesource = imagesource + "text-richtext.png";
                        break;
                    case ".docm":
                        imagesource = imagesource + "text-richtext.png";
                        break;
                    case ".dot":
                        imagesource = imagesource + "text-richtext.png";
                        break;
                    case ".dotx":
                        imagesource = imagesource + "text-richtext.png";
                        break;
                    case ".ppsx":
                        imagesource = imagesource + "text-presentation.png";
                        break;
                    case ".pptx":
                        imagesource = imagesource + "text-presentation.png";
                        break;
                    case ".pps":
                        imagesource = imagesource + "text-presentation.png";
                        break;
                    case ".ppt":
                        imagesource = imagesource + "text-presentation.png";
                        break;
                    case ".pptm":
                        imagesource = imagesource + "text-presentation.png";
                        break;
                    case ".potx":
                        imagesource = imagesource + "text-presentation.png";
                        break;
                    case ".pot":
                        imagesource = imagesource + "text-presentation.png";
                        break;
                    case ".odp":
                        imagesource = imagesource + "text-presentation.png";
                        break;
                    case ".xlsx":
                        imagesource = imagesource + "text-spreadsheet.png";
                        break;
                    case ".xlsm":
                        imagesource = imagesource + "text-spreadsheet.png";
                        break;
                    case ".xlsb":
                        imagesource = imagesource + "text-spreadsheet.png";
                        break;
                    case ".xls":
                        imagesource = imagesource + "text-spreadsheet.png";
                        break;
                    case ".xltx":
                        imagesource = imagesource + "text-spreadsheet.png";
                        break;
                    case ".xltm":
                        imagesource = imagesource + "text-spreadsheet.png";
                        break;
                    case ".xlt":
                        imagesource = imagesource + "text-spreadsheet.png";
                        break;
                    case ".ods":
                        imagesource = imagesource + "text-spreadsheet.png";
                        break;
                    case ".accdb":
                        imagesource = imagesource + "text-database.png";
                        break;
                    case ".mdl":
                        imagesource = imagesource + "text-database.png";
                        break;
                    case ".odbc":
                        imagesource = imagesource + "text-database.png";
                        break;
                    case ".accdt":
                        imagesource = imagesource + "text-database.png";
                        break;
                    case ".mdb":
                        imagesource = imagesource + "text-database.png";
                        break;
                    case ".pdf":
                        imagesource = imagesource + "text-pdf.png";
                        break;
                    case ".xps":
                        imagesource = imagesource + "text-xps.png";
                        break;
                    case ".oxps":
                        imagesource = imagesource + "text-xps.png";
                        break;
                    case ".djvu":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".comic":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".chm":
                        imagesource = imagesource + "text-help.png";
                        break;
                    case ".mobi":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".epub":
                        imagesource = imagesource + "text-file.png";
                        break;
                    case ".vdi":
                        imagesource = imagesource + "disk-vdi.png";
                        break;
                    case ".vhd":
                        imagesource = imagesource + "disk-vhd.png";
                        break;
                    case ".hdd":
                        imagesource = imagesource + "disk-hdd.png";
                        break;
                    case ".iso":
                        imagesource = imagesource + "disk-iso.png";
                        break;
                    case ".bin":
                        imagesource = imagesource + "disk-iso.png";
                        break;
                    case ".isz":
                        imagesource = imagesource + "disk-iso.png";
                        break;
                    case ".img":
                        imagesource = imagesource + "disk-iso.png";
                        break;
                    case ".dmg":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".nrg":
                        imagesource = imagesource + "disk-iso.png";
                        break;
                    case ".mou":
                        imagesource = imagesource + "disk-mou.png";
                        break;
                    case ".zip":
                        imagesource = imagesource + "archive-zip.png";
                        break;
                    case ".cab":
                        imagesource = imagesource + "archive-cab.png";
                        break;
                    case ".7z":
                        imagesource = imagesource + "archive-7z.png";
                        break;
                    case ".rar":
                        imagesource = imagesource + "archive-rar.png";
                        break;
                    case ".tar":
                        imagesource = imagesource + "archive-tar.png";
                        break;
                    case ".deb":
                        imagesource = imagesource + "archive-deb.png";
                        break;
                    case ".xz":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".txz":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".lzma":
                        imagesource = imagesource + "archive-lzma.png";
                        break;
                    case ".cpio":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".bz2":
                        imagesource = imagesource + "archive-bz2.png";
                        break;
                    case ".bzip2":
                        imagesource = imagesource + "archive-bz2.png";
                        break;
                    case ".gz":
                        imagesource = imagesource + "archive-gz.png";
                        break;
                    case ".taz":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".fat":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".ntfs":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".hfs":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".rpm":
                        imagesource = imagesource + "archive-rpm.png";
                        break;
                    case ".wim":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".ace":
                        imagesource = imagesource + "archive-ace.png";
                        break;
                    case ".jar":
                        imagesource = imagesource + "archive-jar.png";
                        break;
                    case ".uha":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".squashfs":
                        imagesource = imagesource + "archive-file.png";
                        break;
                    case ".mp3":
                        imagesource = imagesource + "multimedia-mp3.png";
                        break;
                    case ".3gp":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".3ggp2":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".3gpp":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".acc":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".ac3":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".adt":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".adts":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".amr":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".amv":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".avi":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".flv":
                        imagesource = imagesource + "multimedia-flv.png";
                        break;
                    case ".hdmov":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".mid":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".midi":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".mka":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".mkv":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".mp2":
                        imagesource = imagesource + "multimedia-mp3.png";
                        break;
                    case ".mp4":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".mp4v":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".mpeg":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".mpg":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".ogg":
                        imagesource = imagesource + "multimedia-ogg.png";
                        break;
                    case ".ogm":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".ogv":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".opus":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".ra":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".ram":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".rm":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".wav":
                        imagesource = imagesource + "multimedia-wav.png";
                        break;
                    case ".wma":
                        imagesource = imagesource + "multimedia-wma.png";
                        break;
                    case ".wmv":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".wv":
                        imagesource = imagesource + "multimedia-wav.png";
                        break;
                    case ".vob":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".mov":
                        imagesource = imagesource + "multimedia-video.png";
                        break;
                    case ".swf":
                        imagesource = imagesource + "multimedia-flv.png";
                        break;
                    case ".aac":
                        imagesource = imagesource + "multimedia-audio.png";
                        break;
                    case ".jpg":
                        imagesource = imagesource + "multimedia-jpg.png";
                        break;
                    case ".png":
                        imagesource = imagesource + "multimedia-png.png";
                        break;
                    case ".ico":
                        imagesource = imagesource + "multimedia-ico.png";
                        break;
                    case ".bmp":
                        imagesource = imagesource + "multimedia-bmp.png";
                        break;
                    case ".gif":
                        imagesource = imagesource + "multimedia-gif.png";
                        break;
                    case ".tif":
                        imagesource = imagesource + "multimedia-tif.png";
                        break;
                    case ".tiff":
                        imagesource = imagesource + "multimedia-tif.png";
                        break;
                    case ".dib":
                        imagesource = imagesource + "multimedia-image.png";
                        break;
                    case ".jpe":
                        imagesource = imagesource + "multimedia-jpg.png";
                        break;
                    case ".jpeg":
                        imagesource = imagesource + "multimedia-jpg.png";
                        break;
                    case ".jfif":
                        imagesource = imagesource + "multimedia-image.png";
                        break;
                    case ".svg":
                        imagesource = imagesource + "multimedia-image.png";
                        break;
                    case ".jxr":
                        imagesource = imagesource + "multimedia-image.png";
                        break;
                    case ".wdp":
                        imagesource = imagesource + "multimedia-image.png";
                        break;
                    case ".psd":
                        imagesource = imagesource + "multimedia-psd.png";
                        break;
                    case ".pcx":
                        imagesource = imagesource + "multimedia-image.png";
                        break;
                    case ".tga":
                        imagesource = imagesource + "multimedia-image.png";
                        break;
                    case ".fon":
                        imagesource = imagesource + "x-fon.png";
                        break;
                    case ".ttc":
                        imagesource = imagesource + "x-fon.png";
                        break;
                    case ".ttf":
                        imagesource = imagesource + "x-fon.png";
                        break;
                    case ".dll":
                        imagesource = imagesource + "x-dll.png";
                        break;
                    case ".exe":
                        imagesource = imagesource + "x-exe.png";
                        break;
                    case ".cpl":
                        imagesource = imagesource + "x-dll.png";
                        break;
                    case ".h":
                        imagesource = imagesource + "text-script.png";
                        break;
                    case ".drv":
                        imagesource = imagesource + "x-dll.png";
                        break;
                    case ".msc":
                        imagesource = imagesource + "x-exe.png";
                        break;
                    case ".com":
                        imagesource = imagesource + "text-x.png";
                        break;
                    case ".scr":
                        imagesource = imagesource + "x-scr.png";
                        break;
                    case ".msi":
                        imagesource = imagesource + "x-package.png";
                        break;
                    case ".sys":
                        imagesource = imagesource + "x-sys.png";
                        break;
                    case ".db":
                        imagesource = imagesource + "x-sys.png";
                        break;
                    case ".pnf":
                        imagesource = imagesource + "x-sys.png";
                        break;
                    case ".dat":
                        imagesource = imagesource + "file.png";
                        break;
                    case "":
                        imagesource = imagesource + "file.png";
                        break;
                    default:
                        imagesource = imagesource + "unknownfile.png";
                        break;
                }
            return imagesource;
        }

        static public decimal File_Size(decimal SizeInByte, out string Unit)
        {
            if ((SizeInByte >= 0) && SizeInByte < 1000)
            {
                Unit = "Byte";
                return Math.Round(SizeInByte / 1, 2);
            }
            else if ((SizeInByte > 999) && SizeInByte < 1000000)
            {
                Unit = "KB";
                return Math.Round(SizeInByte / 1024, 2);
            }
            else if (SizeInByte > 999999 && SizeInByte < 1000000000)
            {
                Unit = "MB";
                return Math.Round(SizeInByte / 1048576, 2);
            }
            else if (SizeInByte > 999999999 && SizeInByte < 1000000000000)
            {
                Unit = "GB";
                return Math.Round(SizeInByte / 1073741824, 2);
            }
            else if (SizeInByte > 999999999999 && SizeInByte < 1000000000000000)
            {
                Unit = "TB";
                return Math.Round(SizeInByte / 1099511627776, 2);
            }
            else
            {
                Unit = "PB";
                return Math.Round(SizeInByte / 1125899906842624, 2);
            }
        }

        static public bool IsImage(string extension)
        {
            extension = extension.ToLower();
            if (extension.Equals(".png") || extension.Equals(".jpg") || extension.Equals(".bmp") || extension.Equals(".gif") || extension.Equals(".ico") || extension.Equals(".tif") || extension.Equals(".dib") || extension.Equals(".jfif") || extension.Equals(".jpe") || extension.Equals(".jpeg") || extension.Equals(".jxr") || extension.Equals(".tiff") || extension.Equals(".wdp"))
                return true;
            return false;
        }

        static public bool IsVedio(string extension)
        {
            extension = extension.ToLower();
            if (extension.Equals(".avi") || extension.Equals(".wmv") || extension.Equals(".3gp") || extension.Equals(".mov") || extension.Equals(".mp4") || extension.Equals(".3g2") || extension.Equals(".3gp2") || extension.Equals(".3gpp") || extension.Equals(".bdmv") || extension.Equals(".flv") || extension.Equals(".hdmov") || extension.Equals(".mpe") || extension.Equals(".mpeg") || extension.Equals(".mpg") || extension.Equals(".ogm") || extension.Equals(".ogv") || extension.Equals(".rm") || extension.Equals(".vob"))
                return true;
            return false;
        }

        static public bool IsAudio(string extension)
        {
            extension = extension.ToLower();
            if (extension.Equals(".mp3") || extension.Equals(".wma") || extension.Equals(".acc") || extension.Equals(".ogg") || extension.Equals(".amr") || extension.Equals(".ac3") || extension.Equals(".mp2") || extension.Equals(".oga") || extension.Equals(".ra") || extension.Equals(".ram") || extension.Equals(".wav") || extension.Equals(".wv") || extension.Equals(".mid") || extension.Equals(".midi"))
                return true;
            return false;
        }

        static public bool IsHtml(string extension)
        {
            extension = extension.ToLower();
            if (extension.Equals(".svg") || extension.Equals(".swf") || extension.Equals(".htm") || extension.Equals(".html") || extension.Equals(".hta") || extension.Equals(".mht") || extension.Equals(".shtml") || extension.Equals(".shtm") || extension.Equals(".xhtml") || extension.Equals(".php") || extension.Equals(".php3") || extension.Equals(".phtml") || extension.Equals(".xml") || extension.Equals(".xsml") || extension.Equals(".xsl"))
                return true;
            return false;
        }

        static public bool IsText(string extension)
        {
            extension = extension.ToLower();
            if (extension.Equals(".txt") || extension.Equals(".cs") || extension.Equals(".cpp") || extension.Equals(".log") || extension.Equals(".sh") || extension.Equals(".bsh") || extension.Equals(".bat") || extension.Equals(".cmd") || extension.Equals(".nt") || extension.Equals(".c") || extension.Equals(".ini") || extension.Equals(".inf") || extension.Equals(".reg") || extension.Equals(".url") || extension.Equals(".java") || extension.Equals(".js") || extension.Equals(".nfo") || extension.Equals(".pas") || extension.Equals(".inc") || extension.Equals(".pl") || extension.Equals(".pm") || extension.Equals(".plx") || extension.Equals(".ps") || extension.Equals(".ps1") || extension.Equals(".py") || extension.Equals(".pyw") || extension.Equals(".tex") || extension.Equals(".vb") || extension.Equals(".vbs") || extension.Equals(".v") || extension.Equals(".vhdl") || extension.Equals(".xsd") || extension.Equals(".kml") || extension.Equals(".wsdl") || extension.Equals(".yml"))
                return true;
            return false;
        }

        static public bool IsArchive(string extension)
        {
            extension = extension.ToLower();
            if ((extension.Length > 0 && extension[extension.Length - 1] == '_') || extension.Equals(".7z") || extension.Equals(".zip") || extension.Equals(".rar") || extension.Equals(".001") || extension.Equals(".cab") || extension.Equals(".xz") || extension.Equals(".txz") || extension.Equals(".lzma") || extension.Equals(".tar") || extension.Equals(".cpio") || extension.Equals(".bz2") || extension.Equals(".bzip2") || extension.Equals(".tbz2") || extension.Equals(".tbz") || extension.Equals(".gz") || extension.Equals(".gzip") || extension.Equals(".tgz") || extension.Equals(".tpz") || extension.Equals(".z") || extension.Equals(".taz") || extension.Equals(".lzh") || extension.Equals(".lha") || extension.Equals(".rpm") || extension.Equals(".deb") || extension.Equals(".arj") || extension.Equals(".wim") || extension.Equals(".swm") || extension.Equals(".fat") || extension.Equals(".ntfs") || extension.Equals(".dmg") || extension.Equals(".hfs") || extension.Equals(".xar") || extension.Equals(".squashfs") || extension.Equals(".ace") || extension.Equals(".uha") || extension.Equals(".kgp") || extension.Equals(".kge"))
                return true;
            return false;
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            int index = Explorer.SelectedIndex;
            Navigation(path);
            Explorer.SelectedIndex = index;
        }

        private void Refresh(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
                Refresh();
        }

        private void Surfing_Back(object sender, SelectionChangedEventArgs e)
        {
            Navigation((String)(backMenu.Items[backMenu.SelectedIndex]));
            //Address.FontFamily = new FontFamily(new Uri("D:\\Windows\\Fonts\\consolai.ttf"), "Consolas");
        }

        private void New_Window(object sender, RoutedEventArgs e)
        {
            MainWindow new_Window = new MainWindow("C:\\Windows");
            new_Window.Show();
        }

        private void Copy(object sender, RoutedEventArgs e)
        {
            //if (path != "" && path != "Computer")
            //{
            //    if (Explorer.SelectedIndex > -1 && Explorer.SelectedIndex < directorys.Count)
            //    {
            //        copyfiles.Clear();
            //        copydirectory.Clear();
            //        copydir = Directory.GetParent(directorys[Explorer.SelectedIndex]).FullName;
            //        foreach (var i in new DirectoryInfo(directorys[Explorer.SelectedIndex]).GetFiles())
            //            copyfiles.Add(Subname(i.FullName, copydir));
            //        copydirectory.Add(Subname(directorys[Explorer.SelectedIndex], copydir));
            //        Make_Listof_File(directorys[Explorer.SelectedIndex]);
            //    }
            //    else if (Explorer.SelectedIndex >= directorys.Count && Explorer.SelectedIndex < directorys.Count + files.Count)
            //    {
            //        copyfiles.Clear();
            //        copyfiles.Add(files[Explorer.SelectedIndex - directorys.Count]);
            //    }
            //}
        }

        private void Paste(object sender, RoutedEventArgs e)
        {
            //if (copydirectory.Count > 0)
            //    foreach (var i in copydirectory)
            //        if (!Directory.Exists(path + i))
            //            Directory.CreateDirectory(path + i);
            //foreach (var i in copyfiles)
            //    (new FileInfo(copydir + i)).CopyTo(path + i, false);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("File Manager v0.1.0\nby: mohammad.diab93@outlook.com", "FILE MANAGER", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        private void Make_Listof_File(string DirPath)
        {
            var Dir = (new DirectoryInfo(DirPath)).GetDirectories();
            foreach (var i in Dir)
                copydirectory.Add(Subname(i.FullName, copydir));
            foreach (var i in Dir)
            {
                foreach (var j in i.GetFiles())
                    copyfiles.Add(Subname(j.FullName, copydir));
                Make_Listof_File(i.FullName);
            }
        }

        private string Subname(string name, string toRemove)
        {
            return name.Remove(0, toRemove.Length);
        }

        private void New_Window_Admin(object sender, RoutedEventArgs e)
        {
            //var x = Environment.CurrentDirectory;
            ProcessStartInfo PSI = new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath);
            PSI.Verb = "runas";
            //try

            Process.Start(PSI);
            
            //catch (Exception err)
            //{ MessageBox.Show(err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Show_Hidden_Files(object sender, RoutedEventArgs e)
        {
            hiddenfiles = !hiddenfiles;
            Refresh();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
        }

        private void View_Items_Basic_Info(string FileName, string Type)
        {
            //img.Stretch = Stretch.Uniform;
            string unit;
            switch (Type)
            {
                case "Folder":
                    View_Directory = new DirectoryInfo(FileName);
                    //img.Source = new BitmapImage(new Uri(GetImageSource(imagesource, "Folder", true)));
                    name.Text = "Name: " + View_Directory.Name;
                    type.Text = "Type: Directory";
                    sub_dirs = Directory.GetDirectories(FileName).Length;
                    sub_files = Directory.GetFiles(FileName).Length;
                    size.Text = "Contains " + sub_dirs + " Folders & " + sub_files + " Files";
                    data_Modified.Text = "Data Created: " + View_Directory.CreationTime.ToString();
                    break;
                case "File":
                    View_File = new FileInfo(FileName);
                    //img.Source = new BitmapImage(new Uri(GetImageSource(imagesource, View_File.Extension, true)));
                    name.Text = "Name: " + View_File.Name;
                    type.Text = "Type: " + View_File.Extension + " File";
                    size.Text = "Size: " + File_Size(View_File.Length, out unit) + " " + unit;
                    data_Modified.Text = "Data Modified: " + View_File.LastWriteTime.ToString();
                    break;
                case "Drive":
                    DriveInfo drive = new DriveInfo(FileName);
                    //if (drive.DriveType.ToString().ToLower().Equals("fixed"))
                    //img.Source = new BitmapImage(new Uri(GetImageSource(imagesource, drive.DriveType.ToString(), drive.IsReady && drive.Name.ToLower().Equals(systemDrive))));
                    //else
                    //img.Source = new BitmapImage(new Uri(GetImageSource(imagesource, drive.DriveType.ToString(), drive.IsReady)));
                    if (drive.IsReady)
                    {
                        name.Text = "Name: " + drive.VolumeLabel + " (" + drive.Name + ")";
                        type.Text = "Format: " + drive.DriveFormat + " File System";
                        size.Text = "Disk Size: " + File_Size(drive.TotalSize, out unit) + " " + unit + " (" + File_Size(drive.AvailableFreeSpace, out unit) + " " + unit + " Free)";
                    }
                    else
                    {
                        name.Text = "Name: " + drive.Name;
                        type.Text = "Type: " + drive.DriveType;
                        size.Text = "Drive is not Ready";
                    }
                    data_Modified.Text = "";
                    break;
                default:
                    break;
            }
        }

        private string Name_of_File(string path)
        {
            var i = path.Split('\\');
            return i[i.Length - 1];
        }

        private void HH(object sender, RoutedEventArgs e)
        {
        }
    }
}