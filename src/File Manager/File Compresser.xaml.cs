using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for File_Compresser.xaml
    /// </summary>
    public partial class File_Compresser : Window
    {
        CompresserListView current_parents, root;
        bool project_saved = false;
        string archive_path;
        ObservableCollection<CompresserListView> list_items;
        public File_Compresser()
        {
            InitializeComponent();
            list_items = new ObservableCollection<CompresserListView>();
            root = new CompresserListView(null, 0, null);
            list.DataContext = list_items;
        }

        private void current_path_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }

        private void GoUp_Click(object sender, RoutedEventArgs e)
        {
            list_items.Clear();
            if (current_parents.Parent == null)
            {
                current_parents = null;
                foreach (var i in root.Children)
                    list_items.Add(i);
            }
            else
            {
                current_parents = current_parents.Parent;
                foreach (var i in current_parents.Children)
                    list_items.Add(i);
            }
            current_path.Text = current_path.Text.Remove(current_path.Text.LastIndexOf('\\'));
            if (current_parents == null)
            {
                go_up.IsEnabled = false;
                go_home.IsEnabled = false;
            }
            if (list.Items.Count > 0)
                list.ScrollIntoView(list.Items[0]);
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            if (current_parents == null)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (string i in ofd.FileNames)
                    {
                        FileInfo file = new FileInfo(i);
                        CompresserListView x = new CompresserListView(file, file.Length, null);
                        list_items.Add(x);
                        root.Children.Add(x);
                    }
                    project_saved = false;
                }
            }
            else
                new My_MessageBox("File Manager", "You can only add items on 'root' directory.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (current_parents == null)
            {
                process_bar.Visibility = Visibility.Visible;
                string path = new Folder_Picker().ShowThisDialog();
                if (path != null)
                {
                    DirectoryInfo dir = new DirectoryInfo(path);
                    CompresserListView x = new CompresserListView(dir, 0, null);
                    root.Children.Add(x);
                    list_items.Add(x);
                    project_saved = false;
                }
                process_bar.Visibility = Visibility.Collapsed;
            }
            else
                new My_MessageBox("File Manager", "You can only add items on 'root' directory.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            go_home.IsEnabled = false;
            go_up.IsEnabled = false;
            current_path.Text = "root";
            current_parents = null;
            root.Children.Clear();
            list_items.Clear();
            project_saved = false;
        }

        private void RemoveSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItems.Count > 0)
                if (current_parents == null)
                {
                    for (int i = list.SelectedItems.Count - 1; i > -1; i--)
                    {
                        root.Children.Remove(list.SelectedItems[i] as CompresserListView);
                        list_items.Remove(list.SelectedItems[i] as CompresserListView);
                    }
                    project_saved = false;
                }
                else
                    new My_MessageBox("File Manager", "You can only remove items from 'root' directory.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void Item_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && list.SelectedIndex > -1 && list_items[list.SelectedIndex].Children != null)
            {
                go_up.IsEnabled = true;
                go_home.IsEnabled = true;
                CompresserListView x = list.SelectedItem as CompresserListView;
                current_parents = x;
                list_items.Clear();
                foreach (var i in x.Children)
                    list_items.Add(i);
                current_path.Text += ("\\" + x.FileName);
                if (list.Items.Count > 0)
                    list.ScrollIntoView(list.Items[0]);
            }
            else if (files_menu.IsEnabled && e.ChangedButton == MouseButton.Left && list.SelectedIndex > -1)
            {
                try { System.Diagnostics.Process.Start((list.SelectedItem as CompresserListView).FileFullName); }
                catch (Exception ex) { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            }
            else if (e.ChangedButton == MouseButton.Left && list.SelectedIndex > -1)
            {
                //C:\Users\pal>"C:\Program Files\7-Zip\7z.exe" x "D:\User\Desktop\Project temp.7z" -o"D:\User\Desktop\New folder (2)" 1398177591_Administrative_Tools.png -r
                Process pr = new Process();
                pr.StartInfo = new ProcessStartInfo("Tools\\compress\\7z\\7z.exe", " x \"" + archive_path + "\" -o" + Environment.GetEnvironmentVariable("tmp") + " \"" + list_items[list.SelectedIndex].FileFullName + "\" -aoa -r");//output dir
                pr.Start();
                pr.WaitForExit();
                string j = Environment.GetEnvironmentVariable("tmp");
                try { System.Diagnostics.Process.Start(Environment.GetEnvironmentVariable("tmp") + "\\" + list_items[list.SelectedIndex].FileFullName); }
                catch (Exception ex) { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            }
        }

        private void GoHome_Click(object sender, RoutedEventArgs e)
        {
            list_items.Clear();
            current_path.Text = "root";
            go_up.IsEnabled = false;
            go_home.IsEnabled = false;
            current_parents = null;
            foreach (var i in root.Children)
                list_items.Add(i);
            if (list.Items.Count > 0)
                list.ScrollIntoView(list.Items[0]);
        }

        private void Compress_Click(object sender, RoutedEventArgs e)
        {
            new Compress_Files().ShowDialog(root);
        }

        private void NewArchive_Click(object sender, RoutedEventArgs e)
        {
            if (!project_saved && list.Items.Count > 0)
            {
                MyMessageBoxResult result = new My_MessageBox("Compressed File Manager", "Do you want to save your project?", "", ""/*To cancel just close this window.*/, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/save-48.png")), null, null).ShowThisDialog();
                if (result == MyMessageBoxResult.Yes)
                {
                    if (SaveProject())
                        ClearList_Click(null, null);
                }
                else if (result == MyMessageBoxResult.No)
                    ClearList_Click(null, null);
            }
        }

        private bool SaveProject()
        {
            BinaryFormatter bf = new BinaryFormatter();
            bool done = false;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "File Manager Archive|*.fma";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    List<BasicCompresserListView> m = new List<BasicCompresserListView>();
                    foreach (var i in root.Children)
                        m.Add(new BasicCompresserListView(i, null));
                    bf.Serialize(sfd.OpenFile(), m);
                    //StreamWriter sr = new StreamWriter(sfd.OpenFile());
                    //sr.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                    //sr.WriteLine("<CompressedFileManager Version=\"1.0.0\">");u
                    //sr.Write("    <CurrentLocation>");
                    //sr.Write(current_path.Text);
                    //sr.WriteLine("</CurrentLocation>");
                    //sr.WriteLine("    <ItemsList>");
                    //foreach (var i in root.Children)
                    //    sr.WriteLine("        <" + (i.IsDirectory ? "Directory " : "File ") + "path=\"" + i.FileFullName + "\"" + " type=\"" + i.FileType + "\"" + " size=\"" + i.FileSize + "\"/>");
                    //sr.WriteLine("    </ItemsList>");
                    //sr.WriteLine("</CompressedFileManager>");
                    //sr.Close();
                    //project_saved = true;
                    //done = true;
                }
                catch (Exception ex)
                { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            }
            return done;
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            if (list.Items.Count > 0)
                SaveProject();
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (!project_saved && list.Items.Count > 0)
            {
                MyMessageBoxResult result = new My_MessageBox("Compressed File Manager", "Do you want to save your project?", "", ""/*To cancel just close this window.*/, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/save-48.png")), null, null).ShowThisDialog();
                if (result == MyMessageBoxResult.Yes)
                {
                    if (SaveProject())
                        ClearList_Click(null, null);
                }
                else if (result == MyMessageBoxResult.No)
                    ClearList_Click(null, null);
            }
            OpenProject();
        }

        private void OpenProject()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "File Manager Archive|*.fma";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    List<BasicCompresserListView> m = (List<BasicCompresserListView>)bf.Deserialize(ofd.OpenFile());
                    root.Children.Clear();
                    list_items.Clear();
                    foreach (var i in m)
                    {
                        CompresserListView tmp = i.ConvertToSatndart(null);
                        root.Children.Add(tmp);
                        list_items.Add(tmp);
                    }
                }
                catch (Exception)
                { new My_MessageBox("File Manager", "Error while opening file.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!project_saved && list.Items.Count > 0)
            {
                MyMessageBoxResult result = new My_MessageBox("Compressed File Manager", "Do you want to save your project?", "", ""/*To cancel just close this window.*/, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/save-48.png")), null, null).ShowThisDialog();
                if (result == MyMessageBoxResult.Yes)
                {
                    if (!SaveProject())
                        e.Cancel = true;
                }
            }
        }

        private void OpenArchive_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Archives File Format|*.7z;*.zip;*.rar;*.001;*.gz;*.tar;*.cab;*.iso;*.xz;*.txz;*.lzma;*.cpio;*.bz2;*.bzip;*.bzip2;*.tbz2;*.tbz;*.gzip;*.tgz;*.tpz;*.z;*.taz;*.lzh;*.rpm;*.deb;*.jar;*.arj;*.vhd;*.wim;*swm;*fat;*ntfs;*dmg;*hfs;*xar;*squashfs|All Files|*.*";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                archive_path = ofd.FileName;
                Process process = new Process();
                ProcessStartInfo psi = new ProcessStartInfo("Tools\\compress\\7z\\7z.exe", " l \"" + ofd.FileName + "\"");
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardInput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                process.StartInfo = psi;
                process.Start();
                List<string> out_list = new List<string>();
                int[] stage = new int[3];
                int tmp_num = 0, k = 0;
                while (!process.StandardOutput.EndOfStream)
                {
                    string x = process.StandardOutput.ReadLine();
                    if (tmp_num < 3 && x.StartsWith("------------------- ----- ------------ ------------  ------------------------"))
                    {
                        stage[tmp_num] = k;
                        tmp_num++;
                    }
                    out_list.Add(x);
                    k++;
                }
                if (process.ExitCode == 0)
                {
                    project_saved = true;
                    SetValue(false);
                    //string type = out_list[stage[0] + 2];
                    for (int i = stage[0] + 1; i < out_list.Count && i < stage[1]; i++)
                    {
                        string date = out_list[i].Substring(0, 19);
                        string atripute = out_list[i].Substring(20, 5);
                        long size, c_size;
                        long.TryParse(out_list[i].Substring(26, 12).Replace(" ", ""), out size);
                        long.TryParse(out_list[i].Substring(39, 12).Replace(" ", ""), out c_size);
                        string name = out_list[i].Substring(53);
                        CompresserListView tmp = new CompresserListView(name, size, c_size, date, atripute, root, root, true);
                    }
                    process.StandardOutput.Close();
                    root.FileSizeLong = SortList(root);
                    list_items.Clear();
                    foreach (var i in root.Children)
                        list_items.Add(i);
                }
                else if (process.ExitCode == 2 || process.ExitCode == 8 || process.ExitCode == 255)
                    new My_MessageBox("File Manager", out_list[3], new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            }
        }

        private long SortList(CompresserListView item)
        {
            List<CompresserListView> dirs = new List<CompresserListView>();
            List<CompresserListView> files = new List<CompresserListView>();
            long size = 0;
            foreach (var i in item.Children)
            {
                if (i.IsDirectory)
                {
                    dirs.Add(i);
                    long sun_size = SortList(i);
                    size += sun_size;
                    i.FileSizeLong = sun_size;
                }
                else
                {
                    files.Add(i);
                    size += i.FileSizeLong;
                }
                if (item == root)
                    i.Parent = null;
            }
            dirs.Sort((CompresserListView x, CompresserListView y) => { return x.FileName.CompareTo(y.FileName); });
            files.Sort((CompresserListView x, CompresserListView y) => { return x.FileName.CompareTo(y.FileName); });
            item.Children.Clear();
            foreach (var i in dirs) item.Children.Add(i);
            foreach (var i in files) item.Children.Add(i);
            return size;
        }

        private void CloseArchive_Click(object sender, RoutedEventArgs e)
        {
            GoHome_Click(null, null);
            root.Children.Clear();
            list_items.Clear();
            SetValue(true);
        }

        void SetValue(bool p)
        {
            save_menu.IsEnabled = p;
            save_project.IsEnabled = p;
            compress_menu.IsEnabled = p;
            compress.IsEnabled = p;
            files_menu.IsEnabled = p;
            add_files.IsEnabled = p;
            add_directory.IsEnabled = p;
            remove_selected.IsEnabled = p;
            clear_list.IsEnabled = p;
            close_archive.IsEnabled = !p;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class CompresserListView
    {
        public CompresserListView Parent;
        string extension;
        long compressed_size;
        public List<CompresserListView> Children;

        public CompresserListView() { file_full_name = ""; }

        public CompresserListView(FileSystemInfo file_system, long file_size, CompresserListView parent)
        {
            IsDirectory = file_system is DirectoryInfo;
            Parent = parent;
            if (file_system is FileInfo)
            {
                FileName = file_system.Name;
                FileFullName = file_system.FullName;
                if (FileInfoCombonents.FilesInformation.ContainsKey(file_system.Extension.ToLower()))
                    Image = FileInfoCombonents.FilesInformation[file_system.Extension.ToLower()].Bitmap24;
                else
                    Image = FileInfoCombonents.FilesInformation["."].Bitmap24;
                FileType = Functions.GetFileType(file_system.Extension);
                file_size_long = file_size;
            }
            else if (file_system is DirectoryInfo)
            {
                if (file_system.Name.Contains(':'))
                {
                    string label = new DriveInfo(file_system.Name).VolumeLabel;
                    FileName = (label == "" ? "DiskDrive" : label) + " (" + file_system.Name[0] + ")";
                }
                else
                    FileName = file_system.Name;
                FileFullName = file_system.FullName;
                Image = FileInfoCombonents.FilesInformation["directory"].Bitmap24;
                FileType = "Directory";
                Children = new List<CompresserListView>();
                file_size_long = GetChildren((DirectoryInfo)file_system, this);
            }
            else
            {
                IsDirectory = true;
                file_full_name = "";
                Image = FileInfoCombonents.FilesInformation["directory"].Bitmap24;
                FileType = "Directory";
                Children = new List<CompresserListView>();
            }
            FileSize = Functions.FileSize(file_size_long, 2);
        }

        public CompresserListView(string full_name, long size, long c_size, string date, string att, CompresserListView parent, CompresserListView root,bool add_parents)
        {
            IsDirectory = att[0].Equals('D');
            Parent = parent;
            FileName = full_name.Substring(full_name.LastIndexOf('\\') > 0 ? full_name.LastIndexOf('\\') + 1 : 0);
            FileFullName = full_name;
            file_size_long = size;
            int x = FileName.LastIndexOf('.');
            extension = x > 1 && x < FileName.Length ? FileName.Substring(x) : ".";
            if (IsDirectory)
            {
                extension = "directory";
                Children = new List<CompresserListView>();
            }
            else
                FileSize = Functions.FileSize(file_size_long, 2);
            Image = Functions.GetFileImage24(extension);
            FileType = Functions.GetFileType(extension);
            if (add_parents)
                AddSun(this, root);
        }

        void AddSun(CompresserListView item, CompresserListView root)
        {
            CompresserListView par = root;
            var fan = item.file_full_name.Split(Functions.spliter, StringSplitOptions.RemoveEmptyEntries);
            int index;
            for (int i = 0; i < fan.Length - 1; i++)
            {
                bool continue_search = true;
                if (continue_search && par.Contains(fan[i], out index))
                    par = par.Children[index];
                else
                {
                    CompresserListView m = new CompresserListView(par.file_full_name.Length > 0 ? (par.file_full_name + "\\") : "" + fan[i], 0, 0, "", "D...", par, root, false);
                    par.Children.Add(m);
                    par = m;
                    continue_search = false;
                }
            }
            //error here
            item.Parent = par;
            try { par.Children.Add(item); }
            catch (Exception) { }
        }

        private bool Contains(string item, out int index)
        {
            int i;
            if (Children != null)
                for (i = 0; i < Children.Count;i++ )
                    if (Children[i].file_name.Equals(item))
                    {
                        index = i;
                        return true;
                    }
            index = -1;
            return false;
        }

        long GetChildren(DirectoryInfo di, CompresserListView parent)
        {
            long size = 0;
            try
            {
                var dirs = di.GetDirectories();
                foreach (var i in dirs)
                {
                    CompresserListView sun = new CompresserListView(i, 0, parent);
                    parent.Children.Add(sun);
                    //long sun_size = GetChildren(i, sun);
                    size += sun.file_size_long;
                }
            }
            catch (Exception) { }
            try
            {
                var files = di.GetFiles();
                foreach (var i in files)
                    try
                    { parent.Children.Add(new CompresserListView(i, i.Length, parent)); size += i.Length; }
                    catch (Exception) { }
            }
            catch (Exception) { }
            return size;
        }

        long file_size_long = 0;
        public long FileSizeLong 
        {
            get { return file_size_long; }
            set { FileSize = Functions.FileSize(value, 2); file_size_long = value; }
        }

        string file_full_name;
        public String FileFullName
        {
            get { return file_full_name; }
            set { file_full_name = value; }
        }

        Boolean is_directory;
        public bool IsDirectory
        {
            get { return is_directory; }
            set { is_directory = value; }
        }

        String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        BitmapImage img_source;
        public BitmapImage Image
        {
            get { return img_source; }
            set { img_source = value; }
        }

        String file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }

        String file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }
    }
}




//private long GetDirectorySize(DirectoryInfo dir)
//{
//    long size = 0;
//    try
//    {
//        foreach (var i in dir.GetFiles())
//            size += i.Length;
//        try
//        {
//            foreach (var i in dir.GetDirectories())
//                try
//                { size += GetDirectorySize(i); }
//                catch (Exception) { }
//        }
//        catch (Exception) { }
//    }
//    catch (Exception) { }
//    return size;
//}

//private void AddFolder_Click(object sender, RoutedEventArgs e)
//{
//    string path = new Folder_Picker().ShowThisDialog();
//    if (path != null)
//        list_items.Add(new ListItem(path/*, ItemType.Directory*/));
//}

//private void AddFiles_Click(object sender, RoutedEventArgs e)
//{
//    var OFD = new Microsoft.Win32.OpenFileDialog();
//    OFD.Multiselect = true;
//    if (OFD.ShowDialog() == true)
//        foreach (var i in OFD.FileNames)
//            list_items.Add(new ListItem(i/*, ItemType.File*/));
//}
