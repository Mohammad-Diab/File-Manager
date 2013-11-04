using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace File_Explorer
{
    /// <summary>
    /// Interaction logic for Folder_Picker.xaml
    /// </summary>
    public partial class Folder_Picker : Window
    {
        BitmapImage folder_source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Icons/16 x 16/folder.png"));
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
            foreach (var i in DriveInfo.GetDrives())
                if (i.IsReady)
                    FolderExplorer.Items.Add(make_grid(new BitmapImage(new Uri(Functions.GetDriveImage(i.Name, "pack://application:,,,/File Explorer;component/Images/Icons/16 x 16/"))), i.VolumeLabel + " (" + i.Name + ")", i.Name));
                else
                    FolderExplorer.Items.Add(make_grid(new BitmapImage(new Uri(Functions.GetDriveImage(i.Name, "pack://application:,,,/File Explorer;component/Images/Icons/16 x 16/"))), i.Name, i.Name));
        }

        private void AddFolders(object sender, MouseButtonEventArgs e)
        {
            DriveInfo driver = null;
            ((TreeViewItem)(FolderExplorer.SelectedItem)).Items.Clear();
            try
            { driver = new DriveInfo(((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat); }
            catch (Exception) { }
            if (driver == null || (driver != null && driver.IsReady))
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat);
                    foreach (var j in dir.GetDirectories())
                        ((TreeViewItem)FolderExplorer.SelectedItem).Items.Add(make_grid(folder_source, j.Name, j.FullName));
                }
                catch (Exception) { }
        }

        private TreeViewItem make_grid(BitmapImage icon, string header, string full_path)
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
            TreeItem.HeaderStringFormat = full_path;
            TreeItem.PreviewMouseDoubleClick += AddFolders;
            return TreeItem;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (FolderExplorer.SelectedItem != null)
            {
                returnd_path = ((TreeViewItem)(FolderExplorer.SelectedItem)).HeaderStringFormat;
                this.Close();
            }
        }

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
                        if (new My_MessageBox("New directory", "Do you want to create directory '" + new_name + " (" + i + ")' instat of '" + new_name + "'", "", "", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/new-folder.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                            dir_name = ((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat + "\\" + new_name + " (" + i + ")";
                    }
                    else
                        dir_name = ((TreeViewItem)FolderExplorer.SelectedItem).HeaderStringFormat + "\\" + new_name;
                    try { if (dir_name != null) Directory.CreateDirectory(dir_name); }
                    catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
                    AddFolders(null, null);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            returnd_path = null;
            this.Close();
        }
    }
}
