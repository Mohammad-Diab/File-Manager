using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        ObservableCollection<CompresserListView> list_items;
        public File_Compresser()
        {
            InitializeComponent();
            list_items = new ObservableCollection<CompresserListView>();
            list.DataContext = list_items;
        }

        private void current_path_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }

        private void GoUp_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (string i in ofd.FileNames)
                {
                    FileInfo file = new FileInfo(i);
                    list_items.Add(new CompresserListView(file, file.Length));
                }
            }
        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                long size = 0;
                Thread th = new Thread(() =>
                {
                    size = GetDirectorySize(dir);
                });
                list_items.Add(new CompresserListView(dir, size));
            }
        }

        private long GetDirectorySize(DirectoryInfo dir)
        {
            long size = 0;
            try
            {
                foreach (var i in dir.GetFiles())
                    size += i.Length;
                try
                {
                    foreach (var i in dir.GetDirectories())
                        try
                        { size += GetDirectorySize(i); }
                        catch (Exception) { }
                }
                catch (Exception) { }
            }
            catch (Exception) { }
            return size;
        }


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
    }
}
