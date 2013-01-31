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
        string copydir, path = string.Empty;
        //static string computer_Name, operating_System, CPU = "!", Memory_Size;
        int sub_dirs, sub_files;
        static public string  new_Name;
        string systemDrive = "", PrimPath;
        public MainWindow()
        {
            InitializeComponent();
            directorys = new List<string>();
            files = new List<string>();
            new_Name = null;
        }

        public MainWindow(string PrimPath)
        {
            InitializeComponent();
            directorys = new List<string>();
            files = new List<string>();
            new_Name = null;
            this.PrimPath = PrimPath;
        }

        private void Main_Window_Loaded(object sender, RoutedEventArgs e)
        {
            systemDrive = Environment.SystemDirectory.Substring(0, 3).ToLower();
            if (PrimPath == null)
                Navigation("Computer");
            else
                Navigation(PrimPath);
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
            }
            else if (Explorer.SelectedIndex >= directorys.Count && Explorer.SelectedIndex < directorys.Count + files.Count)
            {
                ProcessStartInfo PSI = new ProcessStartInfo(files[Explorer.SelectedIndex - directorys.Count]);
                Process.Start(PSI);
                PSI.Verb = "runas";
                //psi.UseShellExecute = true;
                //files[Explorer.SelectedIndex - privous_Directorys.Count].Open(FileMode.Open);
            }
        }

        private void Go_UP(object sender, RoutedEventArgs e)
        {
            List<string> split_path = path.Split('\\').ToList<string>();
            while (split_path.Remove("")) { }
            if (split_path.Count == 1)
            {
                Drives = DriveInfo.GetDrives();
                Explorer.Items.Clear();
                foreach (var i in Drives)
                    if (i.DriveType.ToString().ToLower().Equals("fixed"))
                        Explorer.Items.Add(Make_Grid_For_Explorer(i.Name));
                    else
                        Explorer.Items.Add(Make_Grid_For_Explorer(i.Name));
                path = string.Empty;
                Path = path;
            }
            else if (split_path.Count > 1)
            {
                path = string.Empty;
                for (int i = 0; i < split_path.Count - 1; i++)
                    path = path + split_path[i] + "\\";
                Navigation(path);
            }
        }

        private void Traversing_KeyBoard(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                path = Address.Text;
                Navigation(path);
            }
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
                            Explorer.Items.Add(Make_Grid_For_Explorer(i.Name));
                        else
                            Explorer.Items.Add(Make_Grid_For_Explorer(i.Name));
                }
                else
                {
                    current_Directory = new DirectoryInfo(path);
                    directorys.Clear();
                    files.Clear();
                    //foreach (var i in Directory.GetDirectories(current_Directory).GetDirectories().ToList<DirectoryInfo>())
                    foreach (var i in Directory.GetDirectories(path))
                        {
                            //sub_Directorys.Add(i);
                            Explorer.Items.Add(Make_Grid_For_Explorer(Name_of_File(i)));
                            directorys.Add(i);
                        }
                    foreach (var i in Directory.GetFiles(path))
                    {
                        files.Add(i);
                        Explorer.Items.Add(Make_Grid_For_Explorer(Name_of_File(i), "Type: " + new FileInfo(i).Extension + " File"));
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

        private ListViewItem Make_Grid_For_Explorer(string name, string Type)
        {
            ListViewItem Item1 = new ListViewItem();
            Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
            Item1.VerticalAlignment = VerticalAlignment.Stretch;
            Item1.Content = name;
            return Item1;
        }

        private ListViewItem Make_Grid_For_Explorer(string name)
        {
            ListViewItem Item1 = new ListViewItem();
            Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
            Item1.VerticalAlignment = VerticalAlignment.Stretch;
            Item1.Content = name;
            return Item1;
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

        private void New_Window(object sender, RoutedEventArgs e)
        {
            MainWindow new_Window = new MainWindow("C:\\Windows");
            new_Window.Show();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("File Manager v0.0.1\nby: mohammad.diab93@outlook.com", "FILE MANAGER", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        private void New_Window_Admin(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo PSI = new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath);
            PSI.Verb = "runas";
            Process.Start(PSI);
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private string Name_of_File(string path)
        {
            var i = path.Split('\\');
            return i[i.Length - 1];
        }
    }
}