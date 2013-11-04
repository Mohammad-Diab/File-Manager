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
    /// Interaction logic for My_MessageBox.xaml
    /// </summary>
    public partial class My_MessageBox : Window
    {
        bool is_checked = true, have_checkbox = false;
        MyMessageBoxResult to_return = MyMessageBoxResult.No;
        public My_MessageBox(string title, string main_instruction, string caution, string expanded_information, BitmapImage icon, string[] button, MyMessageBoxResult[] message_box_button)
        {
            InitializeComponent();
            Height = 115;
            Title = title;
            main.Text = main_instruction;
            cautions.Text = caution;
            if (caution != "")
                cautions.Visibility = Visibility.Visible;
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

        public My_MessageBox(string title, string main_instruction, BitmapImage icon, Uri media_source, bool checkbox)
        {
            InitializeComponent();
            Helper(title, main_instruction, icon, media_source);
            have_checkbox = checkbox;
            for (int i = 0; i < 5; i++)
                container.Children.RemoveAt(4);
            if (checkbox)
                MakeCheckBox();
        }

        public My_MessageBox(string title, string main_instruction, BitmapImage icon, Uri media_source)
        {
            InitializeComponent();
            for (int i = 0; i < 6; i++)
                container.Children.RemoveAt(3);
            yes.Content = "OK";
            yes.Margin = new Thickness(125, 0, 125, 10);
            yes.HorizontalAlignment = HorizontalAlignment.Stretch;
            Helper(title, main_instruction, icon, media_source);
            Width = 350;
        }

        private void Helper(string title, string main_instruction, BitmapImage icon, Uri media_source)
        {
            Height = 130;
            Title = title;
            main.Text = main_instruction;
            this.Icon = icon;
            icons.Source = icon;
            try
            {
                MediaPlayer media = new MediaPlayer();
                media.Open(media_source);
                media.Play();
            }
            catch (Exception) { }
            yes.Background = null;
            yes.Foreground = Brushes.Black;
        }

        private void MakeCheckBox()
        {
            CheckBox show_again = new CheckBox();
            show_again.Content = "Don't show this message again right now.";
            show_again.HorizontalAlignment = HorizontalAlignment.Left;
            show_again.VerticalAlignment = VerticalAlignment.Bottom;
            show_again.Margin = new Thickness(10, 0, 0, 10);
            additions.Children.Add(show_again);
            additions.Visibility = Visibility.Visible;
        }

        internal MyMessageBoxResult ShowThisDialog()
        {
            ShowDialog();
            return to_return;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            to_return = MyMessageBoxResult.Yes;
            if (have_checkbox)
                is_checked = ((CheckBox)additions.Children[0]).IsChecked == true ? false : true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            to_return = MyMessageBoxResult.No;
            Close();
        }

        internal MyMessageBoxResult ShowThisDialog(out bool show_message_again)
        {
            ShowDialog();
            show_message_again = is_checked;
            return to_return;
        }
    }
}
