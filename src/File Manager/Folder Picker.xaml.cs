using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Folder_Picker.xaml
    /// </summary>
    public partial class Folder_Picker : Window
    {
        ObservableCollection<RenamerListView> list_to_rename = new ObservableCollection<RenamerListView>();
        BitmapImage folder_source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Icons/16/folder.png"));
        Thickness icon_Margin = new Thickness(0, 0, 0, 0);
        Thickness header_Margin = new Thickness(20, 0, 0, 0);
        bool writable = false;
        string returnd_path = null;
        public Folder_Picker()
        {
            InitializeComponent();
        }

        public Folder_Picker(string title, bool write)
        {
            InitializeComponent();
            Title = title;
            writable = write;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            //FolderExplorer.DataContext = list_to_rename;
            foreach (var i in DriveInfo.GetDrives())
                if (i.IsReady)
                    FolderExplorer.Items.Add(make_grid(Functions.GetDriveImage(i.DriveType,false,i.IsReady, 16), i.VolumeLabel + " (" + i.Name + ")", new DirectoryInfo(i.Name)));
                else
                    FolderExplorer.Items.Add(make_grid(Functions.GetDriveImage(i.DriveType, false, i.IsReady, 16), i.Name, new DirectoryInfo(i.Name)));
        }

        private TreeViewItem make_grid(BitmapImage icon, string header, DirectoryInfo full_path)
        {
            Image img = new Image();
            img.Width = img.Height = 14;
            img.Source = icon;
            img.Margin = icon_Margin;
            img.HorizontalAlignment = HorizontalAlignment.Left;
            img.VerticalAlignment = VerticalAlignment.Top;
            TextBlock Header = new TextBlock();
            Header.Text = header;
            Header.Margin = header_Margin;
            Grid grid = new Grid();
            grid.HorizontalAlignment = HorizontalAlignment.Left;
            grid.VerticalAlignment = VerticalAlignment.Top;
            grid.ToolTip = header;
            grid.Children.Add(img);
            grid.Children.Add(Header);
            if (grid.ActualWidth < 200)
                grid.Width = 200;
            TreeViewItem TreeItem = new TreeViewItem();
            TreeItem.Header = grid;
            TreeItem.Tag = full_path;
            TreeItem.Items.Add("*");
            //TreeItem.PreviewMouseDoubleClick += AddFolders;
            return TreeItem;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (path.Text != string.Empty && Directory.Exists(path.Text))
            {
                //returnd_path = ((TreeViewItem)(FolderExplorer.SelectedItem)).HeaderStringFormat;
                returnd_path = path.Text;
                this.Close();
            }
            else
            {
                Functions.ShuckWindow(this, !path.Text.Equals(string.Empty) && !Directory.Exists(path.Text), "Directory is not exists!");
                OK.IsEnabled = false;
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += bw_DoWork;
                bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                bw.RunWorkerAsync();
                //left = Left;
                //DoubleAnimation animation = new DoubleAnimation(Left, Left + 10, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
                //BeginAnimation(LeftProperty, animation);
                //bw.WorkerReportsProgress = true;
            }
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(280);
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OK.IsEnabled = true;
        }

        //void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    //DoubleAnimation animation = new DoubleAnimation(Left, Left - 20, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
        //    //BeginAnimation(LeftProperty, animation);
        //    BackgroundWorker bw = new BackgroundWorker();
        //    bw.WorkerReportsProgress = true;
        //    bw.DoWork += bw_DoWork;
        //    bw.RunWorkerCompleted += bw_RunWorkerCompleted;
        //    bw.RunWorkerAsync();
        //}



        //private void bw_ProgressChanged2(object sender, ProgressChangedEventArgs e)
        //{
        //    DoubleAnimation animation = new DoubleAnimation(Left, Left + 20, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
        //    BeginAnimation(LeftProperty, animation);
        //    BackgroundWorker bw = new BackgroundWorker();
        //    bw.WorkerReportsProgress = true;
        //    bw.DoWork += bw_DoWork;
        //    bw.ProgressChanged += bw_ProgressChanged3;
        //    bw.RunWorkerAsync();
        //}

        //private void bw_ProgressChanged3(object sender, ProgressChangedEventArgs e)
        //{
        //    DoubleAnimation animation = new DoubleAnimation(Left, Left - 20, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
        //    BeginAnimation(LeftProperty, animation);
        //    BackgroundWorker bw = new BackgroundWorker();
        //    bw.WorkerReportsProgress = true;
        //    bw.DoWork += bw_DoWork;
        //    bw.ProgressChanged += bw_ProgressChanged4;
        //    bw.RunWorkerAsync();
        //}

        //private void bw_ProgressChanged4(object sender, ProgressChangedEventArgs e)
        //{
        //    DoubleAnimation animation = new DoubleAnimation(Left, Left + 10, new Duration(new TimeSpan(0, 0, 0, 0, 20)));
        //    BeginAnimation(LeftProperty, animation);
        //    BackgroundWorker bw = new BackgroundWorker();
        //    bw.WorkerReportsProgress = true;
        //    bw.DoWork += bw_DoWork;
        //    bw.ProgressChanged += bw_ProgressChanged5;
        //    bw.RunWorkerAsync();
        //}

        //private void bw_ProgressChanged5(object sender, ProgressChangedEventArgs e)
        //{
        //    if (!path.Text.Equals(string.Empty)&&!Directory.Exists(path.Text))
        //        MessageBox.Show("Directory is not exists!");
        //    Left = left;
        //    OK.IsEnabled = true;
        //}

        internal string ShowThisDialog()
        {
            this.ShowDialog();
            return returnd_path;
        }

        private void CreateDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (FolderExplorer.SelectedItem != null)
            {
                string new_name = null;
                new_name = new Rename("new directory").ShowthisDialog();
                string dir_name = null; ;
                if (new_name != null)
                {
                    if (Directory.Exists(((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat + "\\" + new_name))
                    {
                        int i = 1;
                        while (Directory.Exists(((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat + "\\" + new_name + " (" + i + ")")) i++;
                        if (new My_MessageBox("New directory", "Do you want to create directory '" + new_name + " (" + i + ")' instat of '" + new_name + "'", "", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/new-folder.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                            dir_name = ((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat + "\\" + new_name + " (" + i + ")";
                    }
                    else
                        dir_name = ((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat + "\\" + new_name;
                    try { if (dir_name != null) Directory.CreateDirectory(dir_name); }
                    catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
                    Add_Folders(null, null);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            returnd_path = null;
            this.Close();
        }

        private void FolderExplorer_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (FolderExplorer.SelectedItem != null)
                path.Text = ((DirectoryInfo)((TreeViewItem)(FolderExplorer.SelectedItem)).Tag).FullName;
        }

        private void Add_Folders(object sender, RoutedEventArgs e)
        {
            TreeViewItem item;
            if (e == null)
                item = (TreeViewItem)FolderExplorer.SelectedItem;
            else
                item = (TreeViewItem)e.OriginalSource;
            item.Items.Clear();
            DirectoryInfo dir;
            if (item.Tag is DriveInfo)
            {
                DriveInfo drive = (DriveInfo)item.Tag;
                dir = drive.RootDirectory;
            }
            else
                dir = (DirectoryInfo)item.Tag;
            try
            {
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                    item.Items.Add((make_grid(folder_source, subDir.Name, subDir)));
            }
            catch (Exception) { }

            //try
            //{ driver = new DriveInfo(((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat); }
            //catch (Exception) { }
            //if (driver == null || (driver != null && driver.IsReady))
            //    try
            //    {
            //        dir = new DirectoryInfo(((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat);
            //        foreach (var j in dir.GetDirectories())
            //            ((TreeViewItem)FolderExplorer.SelectedItem).Items.Add(make_grid(folder_source, j.Name, j.FullName));
            //    }
            //    catch (Exception) { }
        }
    }
}
