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
            //public Grid name;
            public string Name;
            public string Size;
            public string Type;
            public string Date;
            //private static BitmapImage folder_source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Icons/16 x 16/folder.png"));
            //private static Thickness img_margin = new Thickness(5, 2, 0, 2);
            //private static Thickness header_margin = new Thickness(26, 2, 0, 2);

            public ListItem(string fullname/*, ItemType type*/)
            {
                //name = new Grid();
                //Image img = new Image();
                //img.Margin = img_margin;
                //img.HorizontalAlignment = HorizontalAlignment.Left;
                //img.VerticalAlignment = VerticalAlignment.Stretch;
                //img.Width = 16;
                //TextBlock header = new TextBlock();
                //header.Margin = header_margin;
                //header.TextTrimming = TextTrimming.CharacterEllipsis;
                //if (type == ItemType.Directory)
                //{
                //    DirectoryInfo dir = new DirectoryInfo(fullname);
                //    //img.Source = folder_source;
                //    //header.Text = dir.Name;
                //    Name = dir.Name;
                //    Size = "Contains: " + dir.GetDirectories().Length + " Directories && " + dir.GetFiles().Length + " Files";
                //    this.Type = "Directory";
                //}
                //else
                //{
                    FileInfo file = new FileInfo(fullname);
                    int file_index = Functions.GetFileIndex(file.Extension);
                    //img.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Icons/16 x 16/" + FileInformation.file_image[file_index]));
                    //header.Text = file.Name;
                    Name = file.Name;
                    Size = Functions.File_Size(file.Length);
                    Type = FileInformation.file_description[file_index];
                    Date = file.LastWriteTime.ToString();
                //}
                //name.Height = 20;
                //name.ToolTip = fullname;
                //name.Children.Add(img);
                //name.Children.Add(header);
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
