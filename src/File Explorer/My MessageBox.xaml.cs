using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for My_MessageBox.xaml
    /// </summary>
    public partial class My_MessageBox : Window
    {
        MyMessageBoxResult to_return = MyMessageBoxResult.No;
        public My_MessageBox(string title, string main_instruction, string content, string expanded_information, BitmapImage icon, string[] button, MyMessageBoxResult[] message_box_button)
        {
            InitializeComponent();
            Height = 115;
            Title = title;
            main.Text = main_instruction;
            icons.Source = icon;
            Icon = icon;
            yes.Background = null;
            yes.Foreground = Brushes.Black;
            for (int i = 0; i < 5; i++)
                container.Children.RemoveAt(4);
        }

        public My_MessageBox(FileSystemInfo item)
        {
            InitializeComponent();
            name.Text = item.Name;
            date.Text = item.LastWriteTime.ToString();
            if (File.Exists(item.FullName))
            {
                int index = Functions.GetFileIndex(item.Extension);
                Image.Source = FileInformation.file_bitmap[index];
                type.Text = FileInformation.file_description[index];
                size.Text = size.Text + Functions.File_Size(((FileInfo)item).Length);
            }
            else if (Directory.Exists(item.FullName))
            {
                type.Text = "Directory";
                Image.Source = FileInformation.file_bitmap[FileInformation.file_bitmap.Count - 2];
                size.Text = "Contains: " + ((DirectoryInfo)item).GetDirectories().Length + " Directories & " + ((DirectoryInfo)item).GetFiles().Length + " Files";
            }
        }

        public My_MessageBox(int item_count)
        {
            InitializeComponent();
            Height = 115;
            for (int i = 0; i < 5; i++)
                container.Children.RemoveAt(4);
            main.Text = "Do you really want to permanently delete " + item_count + " items?";
        }

        internal MyMessageBoxResult ShowThisDialog()
        {
            ShowDialog();
            return to_return;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            to_return = MyMessageBoxResult.Yes;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            to_return = MyMessageBoxResult.No;
            Close();
        }
    }
}
