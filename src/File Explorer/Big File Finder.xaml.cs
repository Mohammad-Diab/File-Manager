using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for Big_File_Finder.xaml
    /// </summary>
    public partial class Big_File_Finder : Window
    {
        List<string> files_name;
        List<long> files_size;
        List<string> dirs_name;
        List<long> dirs_size;
        List<int> files_number;
        List<int> dirs_numbers;
        long dir_size;
        int filesNumber = 0;
        int dirnumbers = 0;

        ObservableCollection<ListItem> list_items;
        public Big_File_Finder()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            list_items = new ObservableCollection<ListItem>();
            ListFile.DataContext = list_items;
            int x = ListFile.Items.Count;
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                files_name = new List<string>();
                files_size = new List<long>();
                Get_all_files(FBD.SelectedPath);
                Quick_Sort(files_size, 0, files_size.Count - 1);
                //ListFile.Items.Clear();
                if (files_name.Count > 100)
                    for (int i = files_name.Count - 1; i > files_name.Count - 101; i--)
                        list_items.Add(new ListItem(files_name[i]));
                else
                    for (int i = files_name.Count - 1; i > -1; i--)
                        list_items.Add(new ListItem(files_name[i]));
                DirectoryInfo directory = new DirectoryInfo(FBD.SelectedPath);
                var directories = directory.GetDirectories();
                dirs_name = new List<string>();
                dirs_size = new List<long>();
                files_number = new List<int>();
                dirs_numbers = new List<int>();
                foreach (var i in directories)
                {
                    dirs_name.Add(i.FullName);
                    filesNumber = 0;
                    dirnumbers = 0;
                    dir_size = 0;
                    Get_all_files_and_dir_info(i.FullName);
                    dirs_size.Add(dir_size);
                    files_number.Add(filesNumber);
                    dirs_numbers.Add(dirnumbers);
                }
                dir_Quick_Sort(dirs_size, 0, dirs_size.Count - 1);
                ListDir.Items.Clear();
                if (dirs_name.Count > 100)
                    for (int i = dirs_name.Count - 1; i > dirs_name.Count - 101; i--)
                        ListDir.Items.Add(dirs_name[i]);
                else
                    for (int i = dirs_name.Count - 1; i > -1; i--)
                        ListDir.Items.Add(dirs_name[i]);
                //TVI.Header = files_name[i];
            }
        }

        private void Get_all_files(string dir)
        {
            var sub_dirs = Directory.GetDirectories(dir);
            DirectoryInfo directory = new DirectoryInfo(dir);
            foreach (var i in directory.GetFiles())
            {
                files_name.Add(i.FullName);
                files_size.Add(i.Length);
            }
            if (sub_dirs.Length == 0)
                return;
            else
                foreach (var i in sub_dirs)
                    try
                    { Get_all_files(i); }
                    catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Get_all_files_and_dir_info(string dir)
        {
            try
            {
                var sub_dirs = Directory.GetDirectories(dir);
                DirectoryInfo directory = new DirectoryInfo(dir);
                var tt = directory.GetFiles();
                foreach (var i in tt)
                {
                    dir_size += i.Length;
                    filesNumber++;
                }
                if (sub_dirs.Length == 0)
                    return;
                else
                    foreach (var i in sub_dirs)
                    {
                        Get_all_files_and_dir_info(i);
                        dirnumbers++;
                    }

            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Quick_Sort(List<long> files_size, int left, int right)
        {
            int pivot;
            if (right > left)
            {
                pivot = Partition(files_size, left, right);
                Quick_Sort(files_size, left, pivot - 1);
                Quick_Sort(files_size, pivot + 1, right);
            }
        }

        private int Partition(List<long> files_size, int left, int right)
        {
            int swapPos;
            int pivotPos = left + (right - left) / 2;
            Swap(files_name, left, pivotPos);
            Swap(files_size, left, pivotPos);
            swapPos = left + 1;
            for (int i = swapPos; i <= right; i++)
                if (files_size[i] < files_size[left])
                {
                    Swap(files_size, i, swapPos);
                    Swap(files_name, i, swapPos);
                    swapPos++;
                }
            Swap(files_name, swapPos - 1, left);
            Swap(files_size, swapPos - 1, left);
            return swapPos - 1;
        }

        private void dir_Quick_Sort(List<long> files_size, int left, int right)
        {
            int pivot;
            if (right > left)
            {
                pivot = dir_Partition(files_size, left, right);
                dir_Quick_Sort(files_size, left, pivot - 1);
                dir_Quick_Sort(files_size, pivot + 1, right);
            }
        }

        private int dir_Partition(List<long> files_size, int left, int right)
        {
            int swapPos;
            int pivotPos = left + (right - left) / 2;
            Swap(files_size, left, pivotPos);
            Swap(dirs_name, left, pivotPos);
            Swap(files_number, left, pivotPos);
            Swap(dirs_numbers, left, pivotPos);
            swapPos = left + 1;
            for (int i = swapPos; i <= right; i++)
                if (files_size[i] < files_size[left])
                {
                    Swap(files_size, i, swapPos);
                    Swap(dirs_name, i, swapPos);
                    Swap(files_number, i, swapPos);
                    Swap(dirs_numbers, i, swapPos);
                    swapPos++;
                }
            Swap(files_size, swapPos - 1, left);
            Swap(dirs_name, swapPos - 1, left);
            Swap(files_number, swapPos - 1, left);
            Swap(dirs_numbers, swapPos - 1, left);
            return swapPos - 1;
        }

        private void Swap(List<long> A, int first, int last)
        {
            long tmp = A[last];
            A[last] = A[first];
            A[first] = tmp;
        }

        private void Swap(List<int> A, int first, int last)
        {
            int tmp = A[last];
            A[last] = A[first];
            A[first] = tmp;
        }

        private void Swap(List<string> A, int first, int last)
        {
            string tmp = A[last];
            A[last] = A[first];
            A[first] = tmp;
        }

        private void ListFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListFile.SelectedIndex > -1)
            {
                name.Text = (ListFile.SelectedItem as ListItem).Name;
                path.Text = (ListFile.SelectedItem as ListItem).File.FullName;
                type.Text = (ListFile.SelectedItem as ListItem).Type;
                size.Text = (ListFile.SelectedItem as ListItem).Size;
                data_Modified.Text = (ListFile.SelectedItem as ListItem).Date;
            }
        }

        private void ListDir_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListDir.SelectedIndex > -1)
            {
                DirectoryInfo file = new DirectoryInfo((String)ListDir.SelectedItem);
                name1.Text = file.Name;
                path1.Text = file.FullName;
                size1.Text = Functions.File_Size(dirs_size[ListDir.Items.Count - 1 - ListDir.SelectedIndex]);
                contain.Text = files_number[ListDir.Items.Count - 1 - ListDir.SelectedIndex] + " Files & " + dirs_numbers[ListDir.Items.Count - 1 - ListDir.SelectedIndex] + " Directories";
                data_Modified1.Text = file.LastWriteTime.ToString();
            }
        }
    }
}
