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

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Renamer.xaml
    /// </summary>
    public partial class Renamer : Window
    {
        //Thickness[] margins = { new Thickness(10, 10, 0, 0), new Thickness(31, 3, 0, 0), new Thickness(110, 10, 10, 0), new Thickness(110, 34, 10, 0), new Thickness(110, 58, 11, 0), new Thickness(10, 0, 10, 3) };
        Thickness[] margins = { new Thickness(10, 3, 0, 0), new Thickness(40, 5, 10, 0) };
        public Renamer()
        { InitializeComponent(); }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog OFD = new Microsoft.Win32.OpenFileDialog();
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == true)
                foreach (string i in OFD.FileNames)
                    FilesList.Items.Add(MakeItemFile(new FileInfo(i)));
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
                GetAllFiles(path);
        }

        private void GetAllFiles(string dir)
        {
            var sub_dirs = Directory.GetDirectories(dir);
            DirectoryInfo directory = new DirectoryInfo(dir);
            foreach (var i in directory.GetFiles())
                FilesList.Items.Add(MakeItemFile(i));
            if (sub_dirs.Length == 0)
                return;
            else
                foreach (var i in sub_dirs)
                    GetAllFiles(i);
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            FilesList.Items.Clear();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (new My_MessageBox("Confirmation", "Are you sure you want to rename all files in list?", "Caution: you can not reverse this operation or cancel it!", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
            {
                bool show_message_again = true;
                List<FileInfo> Undone = new List<FileInfo>();
                foreach (ListViewItem i in FilesList.Items)
                    try
                    {
                        string file_path;
                        if (remove_extension.IsChecked == false)
                            file_path = i.ContentStringFormat;
                        else
                            file_path = i.ContentStringFormat.Substring(0, i.ContentStringFormat.Length - ((FileInfo)i.Tag).Extension.Length);
                        if (!File.Exists(file_path + "." + extension.Text))
                            File.Move(i.ContentStringFormat, file_path + "." + extension.Text);
                        else
                        {
                            int k = 1;
                            while (File.Exists(file_path + " (" + k + ")" + "." + extension.Text)) k++;
                            if (!show_message_again || (show_message_again && new My_MessageBox("Rename file", "Do you want to name file '" + file_path + " (" + k + ")" + "." + extension.Text + "' instat of '" + file_path + "." + extension.Text + "'", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-Warning.wav"), true).ShowThisDialog(out show_message_again) == MyMessageBoxResult.Yes))
                                File.Move(i.ContentStringFormat, file_path + " (" + k + ")" + "." + extension.Text);
                            else
                                Undone.Add((FileInfo)i.Tag);
                        }
                    }
                    catch (Exception) { Undone.Add((FileInfo)i.Tag); }
                FilesList.Items.Clear();
                foreach (FileInfo i in Undone)
                    FilesList.Items.Add(MakeItemFile(i));
                new My_MessageBox("Renamer", "Done." + (Undone.Count > 0 ? "\n But some files have not been renamed successfully, this files have been readded to the list" : ""), new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
            }
        }

        private void extension_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (extension.Text.Contains("*") || extension.Text.Contains("/") || extension.Text.Contains("\\") || extension.Text.Contains("\"") || extension.Text.Contains("?") || extension.Text.Contains("<") || extension.Text.Contains(">") || extension.Text.Contains("|") || extension.Text.Contains("."))
            {
                extension.Text = extension.Text.Replace("*", "").Replace("/", "").Replace("\\", "").Replace("\"", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "").Replace(".", "");
                new My_MessageBox("Renamer", "File extension can't contain any of these characters:\n\\ / : * ? \" < > | .", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            }
        }

        private ListViewItem MakeItemFile(FileInfo file)
        {
            int file_index = 0;
            file_index = Functions.GetFileIndex(file.Extension);

            Image image = new Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Width = image.Height = 24;
            image.Margin = margins[0];
            image.Source = FileInformation.file_bitmap_24[file_index];

            TextBlock tx1 = new TextBlock();
            tx1.VerticalAlignment = VerticalAlignment.Top;
            tx1.FontSize = 14;
            tx1.Margin = margins[1];
            tx1.TextTrimming = TextTrimming.CharacterEllipsis;
            tx1.Text = file.FullName;

            //TextBlock tx2 = new TextBlock();
            //tx2.VerticalAlignment = VerticalAlignment.Top;
            //tx2.FontSize = 14;
            //tx2.Margin = margins[3];
            //tx2.TextTrimming = TextTrimming.CharacterEllipsis;
            //tx2.Text = "Old Extension: '" + file.Extension.ToUpper() + "'";

            //TextBlock tx3 = new TextBlock();
            //tx3.VerticalAlignment = VerticalAlignment.Top;
            //tx3.FontSize = 14;
            //tx3.Margin = margins[4];
            //tx3.TextTrimming = TextTrimming.CharacterEllipsis;
            //tx3.Text = "Type: " + Functions.GetFileType(file_index, file.Extension);

            //Rectangle separator = new Rectangle();
            //separator.Fill = Brushes.Silver;
            //separator.Height = 1;
            //separator.Margin = margins[5];
            //separator.VerticalAlignment = VerticalAlignment.Bottom;

            Grid container = new Grid();
            container.Height = 30;
            container.Width = FilesList.ActualWidth - 40;
            container.Children.Add(image);
            container.Children.Add(tx1);
            //container.Children.Add(tx2);
            //container.Children.Add(tx3);
            //container.Children.Add(separator);

            ListViewItem LVI = new ListViewItem();
            LVI.ToolTip = file.FullName;
            LVI.Tag = file;
            LVI.ContentStringFormat = file.FullName;
            LVI.Content = container;

            return LVI;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (ListViewItem i in FilesList.Items)
                ((Grid)i.Content).Width = FilesList.ActualWidth - 40;
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            for (int i = FilesList.SelectedItems.Count - 1; i >= 0; i--)
                FilesList.Items.RemoveAt(FilesList.Items.IndexOf(FilesList.SelectedItems[i]));
        }
    }
}
