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
    /// Interaction logic for Empty_Folder_Remover.xaml
    /// </summary>
    public partial class Empty_Folder_Remover : Window
    {
        List<string> dirs_name=new List<string>();
        List<int> files_number = new List<int>();
        List<int> dirs_numbers = new List<int>();
        int filesNumber = 0;
        int dirnumbers = 0;
        public Empty_Folder_Remover()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Get_all_files(FBD.SelectedPath);
                List1.Items.Clear();
                for (int i = 0; i < files_number.Count; i++)
                    if (files_number[i] == 0)
                        List1.Items.Add(files_number[i]);
                dirs_name.Clear();
                files_number.Clear();
                dirs_numbers.Clear();
            }
        }

        private void Get_all_files(string dir)
        {
            var sub_dirs = Directory.GetDirectories(dir);
            DirectoryInfo directory = new DirectoryInfo(dir);
            filesNumber += directory.GetFiles().Length;
            if (sub_dirs.Length == 0)
            {
                files_number.Add(filesNumber);
                dirs_numbers.Add(dirnumbers);
                filesNumber = 0;
                dirnumbers = 0;
            }
            else
                foreach (var i in sub_dirs)
                    try
                    {
                        Get_all_files(i);
                        dirnumbers++;
                    }
                    catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
