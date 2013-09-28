using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
    public class ListItem
    {
        public FileInfo File;
        string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        string size;
        public string Size
        {
            get { return size; }
            set { size = value; }
        }

        string type;
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        string date;
        public string Date
        {
            get { return date; }
            set { date = value; }
        }

        public ListItem(string fullname/*, ItemType type*/)
        {
            FileInfo file = new FileInfo(fullname);
            int file_index = Functions.GetFileIndex(file.Extension);
            Name = file.Name;
            Size = Functions.File_Size(file.Length);
            Type = FileInformation.file_description[file_index];
            Date = file.LastWriteTime.ToString();
            File = file;
        }
    }
    /// <summary>
    /// Interaction logic for File_Compresser.xaml
    /// </summary>
    public partial class File_Compresser : Window
    {
        ObservableCollection <ListItem> list_items;
        public File_Compresser()
        {
            InitializeComponent();
            list_items = new ObservableCollection<ListItem>();
            list.DataContext = list_items;
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var FBD = new System.Windows.Forms.FolderBrowserDialog();
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                list_items.Add(new ListItem(FBD.SelectedPath/*, ItemType.Directory*/));
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var OFD = new Microsoft.Win32.OpenFileDialog();
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == true)
                foreach (var i in OFD.FileNames)
                    list_items.Add(new ListItem(i/*, ItemType.File*/));
        }
    }
}
