using Microsoft.Win32;
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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Explorer
{
    /// <summary>
    /// Interaction logic for Renamer.xaml
    /// </summary>
    public partial class Renamer : Window
    {
        public Renamer()
        {
            InitializeComponent();
        }

        private void Add_file_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog OFD = new Microsoft.Win32.OpenFileDialog();
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == true)
                foreach (string i in OFD.FileNames)
                    File_List.Items.Add(i);
        }

        private void Add_folder_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Get_all_files(FBD.SelectedPath);
        }

        private void Get_all_files(string dir)
        {
            var sub_dirs = Directory.GetDirectories(dir);
            DirectoryInfo directory = new DirectoryInfo(dir);
            foreach (var i in directory.GetFiles())
                File_List.Items.Add(i.FullName);
            if (sub_dirs.Length == 0)
                return;
            else
                foreach (var i in sub_dirs)
                    Get_all_files(i);
        }

        private void Clear_List_Click(object sender, RoutedEventArgs e)
        {
            File_List.Items.Clear();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Forms.MessageBox.Show("Are you sure you want to do that?", "Conform", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.Yes)
            {
                List<string> Undone = new List<string>();
                foreach (string i in File_List.Items)
                    try
                    {
                        if (!File.Exists(i + "." + extension.Text))
                            File.Move(i, i + "." + extension.Text);
                        else
                            Undone.Add(i);
                    }
                    catch (Exception)
                    {
                        Undone.Add(i);
                    }
                File_List.Items.Clear();
                foreach (string i in Undone)
                    File_List.Items.Add(i);
                System.Windows.MessageBox.Show("Done", "Renamer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void extension_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (extension.Text.Contains("*") || extension.Text.Contains("/") || extension.Text.Contains("\\") || extension.Text.Contains("\"") || extension.Text.Contains("?") || extension.Text.Contains("<") || extension.Text.Contains(">") || extension.Text.Contains("|") || extension.Text.Contains("."))
            {
                extension.Text = extension.Text.Replace("*", "").Replace("/", "").Replace("\\", "").Replace("\"", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "").Replace(".", "");
                System.Windows.MessageBox.Show("File extension can't contain any of these characters:\n\\ / : * ? \" < > | .", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
