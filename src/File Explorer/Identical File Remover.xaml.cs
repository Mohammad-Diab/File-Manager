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
using System.Security.Cryptography;

namespace File_Explorer
{
    /// <summary>
    /// Interaction logic for Identical_File_Remover.xaml
    /// </summary>
    public partial class Identical_File_Remover : Window
    {
        List<string> files_name;
        List<long> files_size;
        List<List<byte[]>> SHA1s;
        System.Windows.Controls.ContextMenu context_menu;
        public Identical_File_Remover()
        {
            context_menu = new System.Windows.Controls.ContextMenu();
            System.Windows.Controls.MenuItem view_file = new System.Windows.Controls.MenuItem();
            view_file.Header = "View File";
            view_file.Click += View_File_Click;
            System.Windows.Controls.MenuItem view_directory = new System.Windows.Controls.MenuItem();
            view_directory.Header = "View Directory";
            view_directory.Click += View_Directory_Click;
            context_menu.Items.Add(view_file);
            context_menu.Items.Add(view_directory);
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            List1.Items.Clear();
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            files_name = new List<string>();
            SHA1s = new List<List<byte[]>>();
            files_size = new List<long>();
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Get_all_files(FBD.SelectedPath);
                List<List<string>> Identical_Files = new List<List<string>>();
                Quick_Sort(files_size, 0, files_size.Count - 1);
                List<string> fileNode = null;
                long current_size = -1;
                for (int i = 0; i < files_size.Count; i++)
                {
                    if (current_size != files_size[i])
                    {
                        current_size = files_size[i]; //create edge
                        if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
                            Identical_Files.Add(fileNode);
                        fileNode = new List<string>();
                    }
                    fileNode.Add(files_name[i]);
                } //create edge for last node
                if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
                    Identical_Files.Add(fileNode);
                SHA1 HashComputer = new SHA1Cng();
                for (int i = 0; i < Identical_Files.Count; i++)
                {
                    List<byte[]> tmp = new List<byte[]>();
                    for (int j = 0; j < Identical_Files[i].Count; j++)
                        tmp.Add(HashComputer.ComputeHash(File.OpenRead(Identical_Files[i][j])));
                    SHA1s.Add(tmp);
                }
                bool equal;
                for (int i = 0; i < Identical_Files.Count; i++)
                {
                    for (int j = 0; j < Identical_Files[i].Count - 1; j++)
                    {
                        TreeViewItem father = new TreeViewItem();
                        father.Header = Identical_Files[i][j];
                        father.ContextMenu = context_menu;
                        for (int k = j + 1; k < Identical_Files[i].Count; k++)
                        {
                            equal = Equals(SHA1s[i][j], SHA1s[i][k]);
                            if (equal)
                            {
                                TreeViewItem sun = new TreeViewItem();
                                sun.Header = Identical_Files[i][k];
                                father.Items.Add(sun);
                                files_name.Remove(files_name[k]);
                                SHA1s[i].Remove(SHA1s[i][k]);
                                Identical_Files[i].Remove(Identical_Files[i][k]);
                                k--;
                            }
                        }
                        if (father.Items.Count > 0)
                            List1.Items.Add(father);
                    }
                }
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
                    Get_all_files(i);
        }

        private bool Equals(byte[] first, byte[] socend)
        {
            bool res = true;
            Parallel.For(0, first.Length, (i) =>
                {
                    if (first[i] != socend[i])
                        res = false;
                });
            return res;
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

        private void Swap(List<long> A, int first, int last)
        {
            long tmp = A[last];
            A[last] = A[first];
            A[first] = tmp;
        }

        private void Swap(List<string> A, int first, int last)
        {
            string tmp = A[last];
            A[last] = A[first];
            A[first] = tmp;
        }

        private void View_File_Click(object sender, RoutedEventArgs e)
        {
            if (List1.SelectedItem != null)
            {
                Viewer window = new Viewer(((TreeViewItem)List1.SelectedItem).Header.ToString(), ItemType.File, 0, 0);
                window.ShowDialog();
            }
        }

        private void View_Directory_Click(object sender, RoutedEventArgs e)
        {
            if (List1.SelectedItem != null)
            {
                MainWindow window = new MainWindow(new FileInfo(((TreeViewItem)List1.SelectedItem).Header.ToString()).DirectoryName, ((TreeViewItem)List1.SelectedItem).Header.ToString());
                window.ShowDialog();
            }
        }
    }
}