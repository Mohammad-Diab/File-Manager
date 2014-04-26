using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// Interaction logic for Identical_File_Remover.xaml
    /// </summary>
    public partial class Identical_File_Remover : Window
    {
        //Thickness[] margins = { new Thickness(10, 10, 0, 0), new Thickness(35, 10, 10, 0), new Thickness(35, 34, 10, 0), new Thickness(35, 58, 10, 0), new Thickness(10, 0, 10, 2),/*Socend Part*/ new Thickness(5, 10, 0, 0), new Thickness(10, 33, 82, 0), new Thickness(10, 57, 82, 0), new Thickness(0, 10, 10, 0), new Thickness(0, 30, 10, 0) };
        Thickness[] margins = { new Thickness(10, 7, 0, 0), new Thickness(5, 2, 0, 0), new Thickness(35, 34, 10, 0), new Thickness(35, 58, 10, 0), new Thickness(10, 0, 10, 2),/*Socend Part*/ new Thickness(5, 10, 0, 0), new Thickness(10, 33, 82, 0), new Thickness(10, 57, 82, 0), new Thickness(0, 10, 10, 0), new Thickness(26, 8, 10, 0) };
        Brush[] colors = { new SolidColorBrush(Color.FromArgb(125, 135, 206, 235)), new SolidColorBrush(Color.FromArgb(125, 255, 161, 251)), new SolidColorBrush(Color.FromArgb(125, 187, 255, 161)), new SolidColorBrush(Color.FromArgb(125, 255, 161, 161)), new SolidColorBrush(Color.FromArgb(125, 161, 255, 251)), new SolidColorBrush(Color.FromArgb(125, 255, 217, 161)), new SolidColorBrush(Color.FromArgb(125, 182, 161, 255)) };
        List<string> files_name;
        Point point = new Point(0.5, 0.5);
        RotateTransform angel = new RotateTransform(45);
        List<long> file_size;
        long files_size;
        List<string> paths;
        //string path;
        List<List<byte[]>> SHA1s;
        ContextMenu context_menu;
        bool sun_bridle = false, bridle_father = false, show_message_again = true;

        List<List<string>> Identical_Files;

        public Identical_File_Remover()
        {
            context_menu = new ContextMenu();
            MenuItem view_file = new MenuItem();
            view_file.Header = "View file";
            view_file.Click += ViewFile_Click;
            MenuItem view_directory = new MenuItem();
            view_directory.Header = "View in directory";
            view_directory.Click += ViewDirectory_Click;
            context_menu.Items.Add(view_file);
            context_menu.Items.Add(view_directory);
            InitializeComponent();
        }

        private void FindDuplicate_Click(object sender, RoutedEventArgs e)
        {
            paths = new List<string>();
            if (new MultiDirectoriesSelector().ShowDialog(paths) && paths.Count > 0)
            {
                process.Visibility = Visibility.Visible;
                flow_shart.Visibility = Visibility.Hidden;
                delete.IsEnabled = false;
                Identical_Files = new List<List<string>>();
                BackgroundWorker bw = new BackgroundWorker();
                TaskbarItemInfo.ProgressValue = 0;
                bw.WorkerReportsProgress = true;
                bw.WorkerSupportsCancellation = true;
                bw.ProgressChanged += bw_ProgressChanged;
                bw.DoWork += bw_DoWork;
                bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                //ItemsTree.IsHitTestVisible = false;
                bw.RunWorkerAsync();


                
                //TaskbarItemInfo.ProgressValue = 0;
                //files_name = new List<string>();
                //SHA1s = new List<List<byte[]>>();
                //file_size = new List<long>();
                //List<List<string>> Identical_Files = new List<List<string>>();
                //foreach (string i in paths)
                //    try
                //    { Get_all_files(new DirectoryInfo(i)); }
                //    catch (Exception) { }
                //Quick_Sort(file_size, 0, file_size.Count - 1);
                //List<string> fileNode = null;
                //long current_size = -1;
                //for (int i = 0; i < file_size.Count; i++)
                //{
                //    if (current_size != file_size[i])
                //    {
                //        current_size = file_size[i]; //create edge
                //        if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
                //            Identical_Files.Add(fileNode);
                //        fileNode = new List<string>();
                //    }
                //    fileNode.Add(files_name[i]);
                //} //create edge for last node
                //if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
                //    Identical_Files.Add(fileNode);
                //files_size = Identical_Files.Count;
                //SHA1 HashComputer = new SHA1Cng();
                //for (int i = 0; i < Identical_Files.Count; i++)
                //{
                //    List<byte[]> tmp = new List<byte[]>();
                //    for (int j = 0; j < Identical_Files[i].Count; j++)
                //        tmp.Add(HashComputer.ComputeHash(File.OpenRead(Identical_Files[i][j])));
                //    SHA1s.Add(tmp);
                //    TaskbarItemInfo.ProgressValue = (2 * (i + 0.0)) / (3 * files_size);
                //}
                //for (int i = 0; i < Identical_Files.Count; i++)
                //{
                //    for (int j = 0; j < Identical_Files[i].Count - 1; j++)
                //    {
                //        TreeViewItem father = MakeTreeViewItemFather(Identical_Files[i][j], Identical_Files[i].Count);
                //        if (father != null)
                //        {
                //            TreeViewItem sun1 = MakeTreeViewItemSun(Identical_Files[i][j], i);
                //            if (sun1 != null)
                //                father.Items.Add(sun1);
                //            for (int k = j + 1; k < Identical_Files[i].Count; k++)
                //            {
                //                if (Equals(SHA1s[i][j], SHA1s[i][k]))
                //                {
                //                    TreeViewItem sun = MakeTreeViewItemSun(Identical_Files[i][k], i);
                //                    if (sun != null)
                //                        father.Items.Add(sun);
                //                    files_name.Remove(files_name[k]);
                //                    SHA1s[i].Remove(SHA1s[i][k]);
                //                    Identical_Files[i].Remove(Identical_Files[i][k]);
                //                    k--;
                //                }
                //            }
                //            if (father.Items.Count > 1)
                //                ItemsTree.Items.Add(father);
                //        }
                //    }
                //    TaskbarItemInfo.ProgressValue += 1.0 / (3 * files_size);
                //}

            }
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            double dublicate_size = 0;
            double deleted_size = 0;
            double total_size = 0;
            foreach (TreeViewItem i in ItemsTree.Items)
            {
                ((CheckBox)((Grid)((TreeViewItem)i.Items[0]).Header).Children[0]).IsChecked = false;
                Point p = (Point)i.Tag;
                total_size += (p.X * p.Y);
                dublicate_size+=(p.X * (p.Y-1));
                for (int j = 0; j < i.Items.Count; j++)
                    if (((CheckBox)((Grid)((TreeViewItem)i.Items[j]).Header).Children[0]).IsChecked == true)
                        deleted_size += p.X;
            }
            process.Visibility = Visibility.Hidden;
            flow_shart.Visibility = Visibility.Visible;
            duplicate.Width = (dublicate_size / total_size) * total.ActualWidth - 2;
            deleted_grid.Width = (deleted_size / total_size) * total.ActualWidth + 4;
            deleted_text.Text = Functions.FileSize(deleted_size) + " will be deleted";
            TaskbarItemInfo.ProgressValue = 0;
            delete.IsEnabled = true;
            items_label.Text = ItemsTree.Items.Count > 1 ? "items" : "item";
            MessageBox.Show("Done");
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            files_name = new List<string>();
            SHA1s = new List<List<byte[]>>();
            file_size = new List<long>();
            foreach (string i in paths)
                try
                { Get_all_files(new DirectoryInfo(i)); }
                catch (Exception) { }
            Quick_Sort(file_size, 0, file_size.Count - 1);
            List<string> fileNode = null;
            long current_size = -1;
            for (int i = 0; i < file_size.Count; i++)
            {
                if (current_size != file_size[i])
                {
                    current_size = file_size[i]; //create edge
                    if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
                        Identical_Files.Add(fileNode);
                    fileNode = new List<string>();
                }
                fileNode.Add(files_name[i]);
            } //create edge for last node
            if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
                Identical_Files.Add(fileNode);
            files_size = Identical_Files.Count;
            SHA1 HashComputer = new SHA1Cng();
            for (int i = 0; i < Identical_Files.Count; i++)
            {
                List<byte[]> tmp = new List<byte[]>();
                for (int j = 0; j < Identical_Files[i].Count; j++)
                    tmp.Add(HashComputer.ComputeHash(File.OpenRead(Identical_Files[i][j])));
                SHA1s.Add(tmp);
                ((BackgroundWorker)sender).ReportProgress(i);
            }
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int i = e.ProgressPercentage;
            TaskbarItemInfo.ProgressValue = (i - 1.0) / files_size;
            process.Value = TaskbarItemInfo.ProgressValue * 100.0;
            number.Text = ItemsTree.Items.Count.ToString();
            for (int j = 0; j < Identical_Files[i].Count - 1; j++)
            {
                TreeViewItem father = MakeTreeViewItemFather(Identical_Files[i][j], Identical_Files[i].Count);
                if (father != null)
                {
                    TreeViewItem sun1 = MakeTreeViewItemSun(Identical_Files[i][j], i);
                    if (sun1 != null)
                        father.Items.Add(sun1);
                    for (int k = j + 1; k < Identical_Files[i].Count; k++)
                    {
                        if (Equals(SHA1s[i][j], SHA1s[i][k]))
                        {
                            TreeViewItem sun = MakeTreeViewItemSun(Identical_Files[i][k], i);
                            if (sun != null)
                                father.Items.Add(sun);
                            files_name.Remove(files_name[k]);
                            SHA1s[i].Remove(SHA1s[i][k]);
                            Identical_Files[i].Remove(Identical_Files[i][k]);
                            k--;
                        }
                    }
                    if (father.Items.Count > 1)
                        ItemsTree.Items.Add(father);
                }
            }
        }

        //path = new Folder_Picker().ShowThisDialog();
        //    if (path != null)
        //    {
        //        //delete.IsEnabled = false;
        //        //TaskbarItemInfo.ProgressValue = 0;
        //        //files_name = new List<string>();
        //        //SHA1s = new List<List<byte[]>>();
        //        //file_size = new List<long>();
        //        //List<List<string>> Identical_Files = new List<List<string>>();
        //        //Get_all_files(path);
        //        //Quick_Sort(file_size, 0, file_size.Count - 1);
        //        //List<string> fileNode = null;
        //        //long current_size = -1;
        //        for (int i = 0; i < file_size.Count; i++)
        //        {
        //            if (current_size != file_size[i])
        //            {
        //                current_size = file_size[i]; //create edge
        //                if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
        //                    Identical_Files.Add(fileNode);
        //                fileNode = new List<string>();
        //            }
        //            fileNode.Add(files_name[i]);
        //        } //create edge for last node
        //        if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
        //            Identical_Files.Add(fileNode);
        //        files_size = Identical_Files.Count;
        //        SHA1 HashComputer = new SHA1Cng();
        //        for (int i = 0; i < Identical_Files.Count; i++)
        //        {
        //            List<byte[]> tmp = new List<byte[]>();
        //            for (int j = 0; j < Identical_Files[i].Count; j++)
        //                tmp.Add(HashComputer.ComputeHash(File.OpenRead(Identical_Files[i][j])));
        //            SHA1s.Add(tmp);
        //            TaskbarItemInfo.ProgressValue = (2 * (i + 0.0)) / (3 * files_size);
        //        }
        //        bool equal;
        //        for (int i = 0; i < Identical_Files.Count; i++)
        //        {
        //            for (int j = 0; j < Identical_Files[i].Count - 1; j++)
        //            {
        //                TreeViewItem father = MakeTreeViewItemFather(Identical_Files[i][j], Identical_Files[i].Count);
        //                if (father != null)
        //                {
        //                    TreeViewItem sun1 = MakeTreeViewItemSun(Identical_Files[i][j], i);
        //                    if (sun1 != null)
        //                        father.Items.Add(sun1);
        //                    for (int k = j + 1; k < Identical_Files[i].Count; k++)
        //                    {
        //                        equal = Equals(SHA1s[i][j], SHA1s[i][k]);
        //                        if (equal)
        //                        {
        //                            TreeViewItem sun = MakeTreeViewItemSun(Identical_Files[i][k], i);
        //                            if (sun != null)
        //                                father.Items.Add(sun);
        //                            files_name.Remove(files_name[k]);
        //                            SHA1s[i].Remove(SHA1s[i][k]);
        //                            Identical_Files[i].Remove(Identical_Files[i][k]);
        //                            k--;
        //                        }
        //                    }
        //                    if (father.Items.Count > 1)
        //                        ItemsTree.Items.Add(father);
        //                }
        //            }
        //            TaskbarItemInfo.ProgressValue += 1.0 / (3 * files_size);
        //        }
        //        for (int i = 0; i < ItemsTree.Items.Count; i++)
        //            for (int j = 1; j < ((TreeViewItem)ItemsTree.Items[i]).Items.Count; j++)
        //                ((CheckBox)((Grid)((TreeViewItem)((TreeViewItem)ItemsTree.Items[i]).Items[j]).Header).Children[0]).IsChecked = true;
        //        TaskbarItemInfo.ProgressValue = 1;
        //    }
        //    delete.IsEnabled = true;

        private void Sun_Unchecked(object sender, RoutedEventArgs e)
        {
            bridle_father = true;
            CheckAllSun((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent, sender);
            bridle_father = false; ;
        }

        private void Sun_Checked(object sender, RoutedEventArgs e)
        {
            bridle_father = true;
            CheckAllSun((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent, sender);
            bridle_father = false;
        }

        private void Father_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!bridle_father)
                CheckUncheckAll((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent, false);
        }

        private void Father_Checked(object sender, RoutedEventArgs e)
        {
            if (!bridle_father)
                if (show_message_again)
                    if (new My_MessageBox("Remove copies", "You are about to remove all copies of this file?", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-Warning.wav"), true).ShowThisDialog(out show_message_again) == MyMessageBoxResult.Yes)
                        CheckUncheckAll((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent, true);
                    else
                        ((CheckBox)sender).IsChecked = false;
                else
                    CheckUncheckAll((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent, true);

        }

        private void CheckAllSun(TreeViewItem sun, object sender)
        {
            if (!sun_bridle)
            {
                int count = 0;
                foreach (TreeViewItem i in ((TreeViewItem)sun.Parent).Items)
                    if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true)
                        count++;
                if (count == ((TreeViewItem)sun.Parent).Items.Count)
                {
                    if (show_message_again)
                        if (new My_MessageBox("Remove copies", "You are about to remove all copies of this file, do you want to do that?", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-Warning.wav"), true).ShowThisDialog(out show_message_again) == MyMessageBoxResult.Yes)
                            ((CheckBox)((Grid)((TreeViewItem)sun.Parent).Header).Children[0]).IsChecked = true;
                        else
                            ((CheckBox)sender).IsChecked = false;
                    else
                        ((CheckBox)((Grid)((TreeViewItem)sun.Parent).Header).Children[0]).IsChecked = true;
                }
                else if (count == 0)
                    ((CheckBox)((Grid)((TreeViewItem)sun.Parent).Header).Children[0]).IsChecked = false;
                else
                    ((CheckBox)((Grid)((TreeViewItem)sun.Parent).Header).Children[0]).IsChecked = null;
            }
        }

        private void CheckUncheckAll(TreeViewItem father, bool check)
        {
            sun_bridle = true;
            foreach (TreeViewItem i in father.Items)
                ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = check;
            sun_bridle = false;
        }

        private void Get_all_files(DirectoryInfo dir)
        {
            DirectoryInfo[] sub_dirs = dir.GetDirectories();
            foreach (var i in dir.GetFiles())
            {
                try
                {
                    files_name.Add(i.FullName);
                    file_size.Add(i.Length);
                }
                catch (Exception) { }
            }
            foreach (var i in sub_dirs)
                try
                { Get_all_files(i); }
                catch (Exception) { }
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

        private void ViewFile_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsTree.SelectedItem != null && !((TreeViewItem)ItemsTree.SelectedItem).HeaderStringFormat.Equals(""))
            {
                Viewer window = new Viewer(((TreeViewItem)ItemsTree.SelectedItem).HeaderStringFormat, ItemType.File, 0, 0);
                window.ShowDialog();
            }
        }

        private void ViewDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsTree.SelectedItem != null && !((TreeViewItem)ItemsTree.SelectedItem).HeaderStringFormat.Equals(""))
            {
                MainWindow window = new MainWindow(new FileInfo(((TreeViewItem)ItemsTree.SelectedItem).HeaderStringFormat).DirectoryName, ((TreeViewItem)ItemsTree.SelectedItem).HeaderStringFormat);
                window.ShowDialog();
            }
        }

        private TreeViewItem MakeTreeViewItemSun(string file_name, int father_index)
        {
            try
            {
                FileInfo file = new FileInfo(file_name);
                int file_index = Functions.GetFileIndex(file.Extension);

                CheckBox ch1 = new CheckBox();
                ch1.HorizontalAlignment = HorizontalAlignment.Left;
                ch1.VerticalAlignment = VerticalAlignment.Top;
                ch1.Margin = margins[0];
                ch1.FontSize = 14;
                ch1.Content = file_name;
                ch1.IsChecked = true;
                ch1.Checked += Sun_Checked;
                ch1.Unchecked += Sun_Unchecked;

                //TextBlock t1 = new TextBlock();
                //t1.Text = "File Path: " + file_name;
                //t1.TextTrimming = TextTrimming.CharacterEllipsis;
                //t1.Margin = margins[1];
                //t1.VerticalAlignment = VerticalAlignment.Top;
                //t1.FontSize = 14;

                //TextBlock t2 = new TextBlock();
                //t2.Text = "Type: " + Functions.GetFileType(file_index, file.Extension);
                //t2.Margin = margins[2];
                //t2.VerticalAlignment = VerticalAlignment.Top;
                //t2.FontSize = 14;

                //TextBlock t3 = new TextBlock();
                //t3.Text = "Date Modified: " + file.LastWriteTimeUtc;
                //t3.Margin = margins[3];
                //t3.VerticalAlignment = VerticalAlignment.Top;
                //t3.FontSize = 14;

                //Rectangle separator = new Rectangle();
                //separator.Fill = Brushes.Silver;
                //separator.Height = 1;
                //separator.Margin = margins[4];
                //separator.VerticalAlignment = VerticalAlignment.Bottom;

                Grid grid = new Grid();
                grid.Height = 30;
                grid.Width = ItemsTree.ActualWidth - 60;
                //grid.ToolTip = file_name;
                grid.Children.Add(ch1);
                //grid.Children.Add(t1);
                //grid.Children.Add(t2);
                //grid.Children.Add(t3);
                //grid.Children.Add(separator);
                grid.Background = colors[father_index % 7];

                TreeViewItem tree_item = new TreeViewItem();
                tree_item.ToolTip = file_name;
                tree_item.ContextMenu = context_menu;
                tree_item.Header = grid;
                //tree_item.Background = colors[father_index % 7];
                tree_item.HeaderStringFormat = file_name;
                return tree_item;
            }
            catch (Exception)
            { return null; }
        }

        private TreeViewItem MakeTreeViewItemFather(string file_name, int duplicates)
        {
            try
            {
                FileInfo file = new FileInfo(file_name);
                int file_index = Functions.GetFileIndex(file.Extension);

                CheckBox ch1 = new CheckBox();
                ch1.Content = "Check All Copies";
                ch1.HorizontalAlignment = HorizontalAlignment.Left;
                ch1.VerticalAlignment = VerticalAlignment.Top;
                ch1.Margin = margins[5];
                ch1.FontSize = 14;
                ch1.IsChecked = true;
                ch1.Unchecked += Father_Unchecked;
                ch1.Checked += Father_Checked;

                TextBlock t1 = new TextBlock();
                t1.Text = "File size: " + Functions.FileSize(file.Length);
                t1.Margin = margins[6];
                t1.VerticalAlignment = VerticalAlignment.Top;
                t1.FontSize = 14;

                TextBlock t2 = new TextBlock();
                t2.Text = "duplicate's number: " + duplicates + " Copies";
                t2.Margin = margins[7];
                t2.VerticalAlignment = VerticalAlignment.Top;
                t2.FontSize = 14;

                Image image = new Image();
                image.Margin = margins[8];
                image.HorizontalAlignment = HorizontalAlignment.Right;
                image.Width = 67;
                image.Source = FileInformation.file_bitmap_64[file_index];

                Grid grid = new Grid();
                grid.Height = 87;
                grid.Width = ItemsTree.ActualWidth - 45;
                grid.Children.Add(ch1);
                grid.Children.Add(t1);
                grid.Children.Add(t2);
                grid.Children.Add(image);
                if (ItemsTree.Items.Count > 0)
                {
                    Rectangle separator = new Rectangle();
                    separator.Fill = Brushes.YellowGreen;
                    separator.Height = 2;
                    separator.Margin = margins[1];
                    separator.VerticalAlignment = VerticalAlignment.Top;
                    grid.Children.Add(separator);
                }

                TreeViewItem tree_item = new TreeViewItem();
                tree_item.Tag = new Point(file.Length, duplicates);
                //tree_item.HeaderStringFormat = "";
                tree_item.Header = grid;
                return tree_item;
            }
            catch (Exception)
            { return null; }
        }

        private void Window_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            foreach (TreeViewItem i in ItemsTree.Items)
            {
                ((FrameworkElement)i.Header).Width = ItemsTree.ActualWidth - 45;
                foreach (TreeViewItem j in i.Items)
                    ((FrameworkElement)j.Header).Width = ItemsTree.ActualWidth - 60;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            List<string> files = new List<string>();
            foreach (TreeViewItem i in ItemsTree.Items)
                foreach (TreeViewItem j in i.Items)
                    if (((CheckBox)((Grid)j.Header).Children[0]).IsChecked == true && j.IsEnabled == true)
                        files.Add(j.HeaderStringFormat);
            if (files.Count > 0)
            {
                My_MessageBox messagebox;
                if (files.Count > 1)
                    messagebox = new My_MessageBox(files.Count);
                else
                    messagebox = new My_MessageBox(new FileInfo(files[0]));
                if (messagebox.ShowThisDialog() == MyMessageBoxResult.Yes)
                    new File_Operation(files, new List<string>()).ShowDialog();
                foreach (TreeViewItem i in ItemsTree.Items)
                    foreach (TreeViewItem j in i.Items)
                        if (((CheckBox)((Grid)j.Header).Children[0]).IsChecked == true && !File.Exists(j.HeaderStringFormat))
                        {
                            TextBlock del = new TextBlock();
                            del.Text = "Deleted";
                            del.HorizontalAlignment = HorizontalAlignment.Right;
                            del.Margin = margins[9];
                            del.FontSize = 9.5;
                            del.RenderTransformOrigin = point;
                            del.FontWeight = FontWeights.Bold;
                            del.Foreground = Brushes.Red;
                            del.VerticalAlignment = VerticalAlignment.Top;
                            del.RenderTransform = angel;
                            ((Grid)j.Header).Children.Add(del);
                            j.IsEnabled = false;
                        }
            }
            else
                new My_MessageBox("Delete files", "You have not selected any file to been deleted!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (TreeViewItem i in ItemsTree.Items)
                ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = true;
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            ItemsTree.Items.Clear();
        }

        private void SortBy_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).ContextMenu.IsOpen = !((Button)sender).ContextMenu.IsOpen;
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (TreeViewItem i in ItemsTree.Items)
                ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = false;
        }
    }
}
