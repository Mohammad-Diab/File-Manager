using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for Big_File_Finder.xaml
    /// </summary>
    public partial class Big_File_Finder : Window
    {
        Point point = new Point(0.5, 0.5);
        RotateTransform angel = new RotateTransform(45);
        Tree big_items;
        long full_size;
        Thickness[] margins = { new Thickness(10, 10, 0, 0), new Thickness(26, 10, 0, 0), new Thickness(125, 10, 10, 0), new Thickness(125, 34, 10, 0), new Thickness(125, 60, 10, 0), new Thickness(125, 84, 10, 0), new Thickness(10, 0, 10, 3), new Thickness(0, 40, 10, 0) };
        Brush[] brushes = { new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), new SolidColorBrush(Color.FromArgb(180, 255, 255, 245)), new SolidColorBrush(Color.FromArgb(180, 255, 250, 235)), new SolidColorBrush(Color.FromArgb(180, 255, 245, 220)), new SolidColorBrush(Color.FromArgb(180, 255, 230, 180)), new SolidColorBrush(Color.FromArgb(180, 255, 220, 140)), new SolidColorBrush(Color.FromArgb(180, 255, 200, 80)), new SolidColorBrush(Color.FromArgb(180, 255, 190, 75)), new SolidColorBrush(Color.FromArgb(180, 255, 175, 50)), new SolidColorBrush(Color.FromArgb(180, 255, 160, 50)), new SolidColorBrush(Color.FromArgb(180, 255, 140, 50)), new SolidColorBrush(Color.FromArgb(180, 255, 120, 30)), new SolidColorBrush(Color.FromArgb(180, 255, 100, 30)), new SolidColorBrush(Color.FromArgb(180, 255, 85, 20)), new SolidColorBrush(Color.FromArgb(180, 255, 75, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 65, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 55, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 45, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 35, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 20, 0)) };
        public Big_File_Finder()
        { InitializeComponent(); }

        double process_value;
        public TimeSpan timespan = new TimeSpan(1);
        ObservableCollection<BigFileListView> list_to_view = new ObservableCollection<BigFileListView>();
        Dictionary<string, List<BigItemElement>> dic;

        public void findBigItems_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                dic = new Dictionary<string, List<BigItemElement>>();
                process_value = 0;
                progressbar.Value = 0;
                progressbar.Visibility = Visibility.Visible;
                DirectoryInfo root_directory = new DirectoryInfo(path);
                current_path.Text = path;
                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerSupportsCancellation = true;
                bw.WorkerReportsProgress = true;
                bw.DoWork += bw_DoWork;
                bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                bw.RunWorkerAsync(root_directory);
                BackgroundWorker process_bar = new BackgroundWorker();
                process_bar.WorkerSupportsCancellation = true;
                process_bar.WorkerReportsProgress = true;
                process_bar.DoWork += processbar_DoWork;
                process_bar.ProgressChanged += process_bar_ProgressChanged;
                process_bar.RunWorkerAsync(root_directory);
                if (ItemsTree.Items.Count > 0)
                {DeleteItems.IsEnabled = true;}
                else
                { DeleteItems.IsEnabled = false; }
            }
        }

        private void process_bar_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            process_value = e.ProgressPercentage;
            progressbar.Value = e.ProgressPercentage;
        }

        private void processbar_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < 5000 && process_value < 5100; i++)
            {
                int sleep_time = i < 4500 ? i / 500 : i / 200;
                ((BackgroundWorker)sender).ReportProgress(i);
                Thread.Sleep(sleep_time);
            }
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                list_to_view.Clear();
                BackgroundWorker BW_Viewer = new BackgroundWorker();
                BW_Viewer.WorkerSupportsCancellation = true;
                BW_Viewer.WorkerReportsProgress = true;
                BW_Viewer.ProgressChanged += bw1_ProgressChanged;
                BW_Viewer.RunWorkerCompleted += BW_Viewer_RunWorkerCompleted;
                BW_Viewer.DoWork += bw1_DoWork;
                BW_Viewer.RunWorkerAsync(current_path.Text);
            }
        }

        private void BW_Viewer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressbar.Value = 5100;
            process_value = 5100;
            Thread.Sleep(1);
            progressbar.Visibility = Visibility.Hidden;
        }

        private void bw1_DoWork(object sender, DoWorkEventArgs e)
        {
            string dir = e.Argument.ToString();
            BackgroundWorker bw = (BackgroundWorker)sender;
            dic[dir].Sort();
            foreach (var i in dic[dir])
                if (i.file_system.Exists)
                {
                    bw.ReportProgress(0, i);
                    Thread.Sleep(timespan);
                }
        }

        private void bw1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BigItemElement i = ((BigItemElement)e.UserState);
            int index = (i.file_system is FileInfo) ? Functions.GetFileIndex(i.file_system.Extension) : FileInformation.file_extentions.Length - 2;
            double perc = Math.Round((i.size * 100.0) / (full_size), 2);
            double brush_index = Math.Round((20.0 * i.size / full_size /*Current Size*/), 0);
            list_to_view.Add(new BigFileListView(i.file_system.FullName, i.file_system.Name, FileInformation.file_bitmap_24[index], FileInformation.file_description[index], Functions.FileSize(i.size), 5.88 + " %", perc + " %", brush_index > 19 ? brushes[19] : brush_index < 0 ? brushes[0] : brushes[(int)brush_index], i.file_system is DirectoryInfo));
            //list_to_view[list_to_view.Count - 1].Tap += Big_File_Finder_MouseDoubleClick;
        }

        private void Big_File_Finder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Hello");
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                DirectoryInfo dir = (DirectoryInfo)e.Argument;
                full_size = getAllFiles(dir);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            item_list.DataContext = list_to_view;
        }

        public long getAllFiles(DirectoryInfo directory)
        {
            long size = 0;
            foreach (var i in directory.GetDirectories())
                try
                {
                    long current_size = getAllFiles(i);
                    size += current_size;
                    if (dic.Keys.Contains(directory.FullName))
                        dic[directory.FullName].Add(new BigItemElement(i, current_size, true));
                    else
                    {
                        List<BigItemElement> list = new List<BigItemElement>();
                        list.Add(new BigItemElement(i, current_size, true));
                        dic.Add(directory.FullName, list);
                    }
                }
                catch (Exception ex) { /*MessageBox.Show(ex.Message);*/ }
            FileInfo[] files = new FileInfo[0];
            try { files = directory.GetFiles(); }
            catch (Exception ex) { /*MessageBox.Show(ex.Message);*/}
            foreach (var i in files)
            {
                size += i.Length;
                if (dic.Keys.Contains(directory.FullName))
                    dic[directory.FullName].Add(new BigItemElement(i, i.Length, true));
                else
                {
                    List<BigItemElement> list = new List<BigItemElement>();
                    list.Add(new BigItemElement(i, i.Length, true));
                    dic.Add(directory.FullName, list);
                }
            }
            return size;
        }

        private void item_list_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (item_list.SelectedItem != null)
            {
                BigFileListView item = (BigFileListView)item_list.SelectedItem;
                if (item.isDirectory)
                {
                    current_path.Text = item.FullPath;
                    list_to_view.Clear();
                    BackgroundWorker BW_Viewer = new BackgroundWorker();
                    BW_Viewer.WorkerSupportsCancellation = true;
                    BW_Viewer.WorkerReportsProgress = true;
                    BW_Viewer.ProgressChanged += bw1_ProgressChanged;
                    BW_Viewer.DoWork += bw1_DoWork;
                    BW_Viewer.RunWorkerAsync(current_path.Text);
                }
            }
        }

        private void GoUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string parent_path = new DirectoryInfo(current_path.Text).Parent.FullName;
                if (dic.Keys.Contains(parent_path))
                {
                    current_path.Text = parent_path;
                    list_to_view.Clear();
                    BackgroundWorker BW_Viewer = new BackgroundWorker();
                    BW_Viewer.WorkerSupportsCancellation = true;
                    BW_Viewer.WorkerReportsProgress = true;
                    BW_Viewer.ProgressChanged += bw1_ProgressChanged;
                    BW_Viewer.DoWork += bw1_DoWork;
                    BW_Viewer.RunWorkerAsync(current_path.Text);
                }
            }
            catch (Exception) { }
        }

        private void current_path_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && dic != null)
                if (dic.Keys.Contains(current_path.Text))
                {
                    list_to_view.Clear();
                    BackgroundWorker BW_Viewer = new BackgroundWorker();
                    BW_Viewer.WorkerSupportsCancellation = true;
                    BW_Viewer.WorkerReportsProgress = true;
                    BW_Viewer.ProgressChanged += bw1_ProgressChanged;
                    BW_Viewer.DoWork += bw1_DoWork;
                    BW_Viewer.RunWorkerAsync(current_path.Text);
                }
                else
                    MessageBox.Show("Invalid Path");
        }



        private long GetAllFiles(DirectoryInfo directory, Tree parent)
        {
            long size = 0;
            foreach (var i in directory.GetDirectories())
                try
                {
                    Tree sun = new Tree(parent, new BigItemElement(i, 0, true));
                    parent.AddChild(sun);
                    long dir_size = GetAllFiles(i, sun);
                    sun.SetSize(dir_size);
                    size += dir_size;
                }
                catch (Exception) { }
            foreach (FileInfo i in directory.GetFiles())
            {
                parent.AddChild(new BigItemElement(i, i.Length, false));
                size += i.Length;
            }
            parent.children.Sort();
            return size;
        }

        private void FindBigItems_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                DirectoryInfo root_directory = new DirectoryInfo(path);
                big_items = new Tree(null, new BigItemElement(root_directory, 0, true));
                full_size = GetAllFiles(root_directory, big_items);
                big_items.SetSize(full_size);
                PrintResult(big_items, ItemsTree);
                if (ItemsTree.Items.Count > 0)
                    DeleteItems.IsEnabled = true;
                else
                    DeleteItems.IsEnabled = false;
            }
        }

        private void PrintResult(Tree big_item, TreeView ItemsTree)
        {
            TreeViewItem root = MakeTreeViewItem(big_item, 40, big_item.content.size);
            ItemsTree.Items.Add(root);
            foreach (var i in big_item.children)
            {
                TreeViewItem sun = MakeTreeViewItem(i, 60, big_item.content.size);
                root.Items.Add(sun);
                PrintResult(i, sun, 60);
            }
        }

        private void PrintResult(Tree big_item, TreeViewItem parent, double level)
        {
            foreach (var i in big_item.children)
            {
                TreeViewItem sun = MakeTreeViewItem(i, level, i.parent.content.size);
                parent.Items.Add(sun);
                PrintResult(i, sun, level + 25);
            }
        }

        private TreeViewItem MakeTreeViewItem(Tree item, double level, long parent_size)
        {
            int file_index = 0;
            if (!item.content.is_directory)
                file_index = Functions.GetFileIndex(item.content.file_system.Extension);

            CheckBox ch = new CheckBox();
            ch.HorizontalAlignment = HorizontalAlignment.Left;
            ch.VerticalAlignment = VerticalAlignment.Top;
            ch.Margin = margins[0];
            ch.Checked += Ch_Checked;
            ch.Unchecked += Ch_Unchecked;

            Image image = new Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Width = image.Height = 90;
            image.Margin = margins[1];
            if (item.content.is_directory)
                image.Source = FileInformation.file_bitmap_64[FileInformation.file_bitmap_64.Count - 2];
            else
                image.Source = FileInformation.file_bitmap_64[file_index];

            TextBlock tx1 = new TextBlock();
            tx1.VerticalAlignment = VerticalAlignment.Top;
            tx1.FontSize = 14;
            tx1.Margin = margins[2];
            tx1.TextTrimming = TextTrimming.CharacterEllipsis;
            tx1.Text = "Path: " + item.content.file_system.FullName;

            TextBlock tx2 = new TextBlock();
            tx2.VerticalAlignment = VerticalAlignment.Top;
            tx2.FontSize = 16;
            tx2.Margin = margins[3];
            tx2.TextTrimming = TextTrimming.CharacterEllipsis;
            tx2.Text = "Size: " + Functions.FileSize(item.content.size) + "\t«" + Math.Round((100.0 * item.content.size / parent_size), 3) + "%» of current directory " + "\t«" + Math.Round((100.0 * item.content.size / full_size), 3) + "%» of total.";

            TextBlock tx3 = new TextBlock();
            tx3.VerticalAlignment = VerticalAlignment.Top;
            tx3.FontSize = 14;
            tx3.Margin = margins[4];
            tx3.TextTrimming = TextTrimming.CharacterEllipsis;
            tx3.Text = "Type: " + (item.content.is_directory ? "Directory" : Functions.GetFileType(file_index, item.content.file_system.Extension));

            TextBlock tx4 = new TextBlock();
            tx4.VerticalAlignment = VerticalAlignment.Top;
            tx4.FontSize = 14;
            tx4.Margin = margins[5];
            tx4.TextTrimming = TextTrimming.CharacterEllipsis;
            tx4.Text = "Date Modified: " + item.content.file_system.LastWriteTimeUtc;

            Rectangle separator = new Rectangle();
            separator.Fill = Brushes.Silver;
            separator.Height = 1;
            separator.Margin = margins[6];
            separator.VerticalAlignment = VerticalAlignment.Bottom;

            Grid container = new Grid();
            container.Height = 115;
            container.Width = ItemsTree.ActualWidth - level;
            container.Children.Add(ch);
            container.Children.Add(image);
            container.Children.Add(tx1);
            container.Children.Add(tx2);
            container.Children.Add(tx3);
            container.Children.Add(tx4);
            container.Children.Add(separator);

            TreeViewItem TVI = new TreeViewItem();
            TVI.ToolTip = item.content.file_system.FullName;
            TVI.Tag = item.content.file_system;
            TVI.HeaderStringFormat = item.content.file_system.FullName;
            TVI.Header = container;

            return TVI;
        }

        private void Ch_Unchecked(object sender, RoutedEventArgs e)
        {
            TreeViewItem checks = ((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent);
            foreach (TreeViewItem i in checks.Items)
                ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = false;
            CheckParent(checks);
        }

        private void Ch_Checked(object sender, RoutedEventArgs e)
        {
            TreeViewItem checks = ((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent);
            foreach (TreeViewItem i in checks.Items)
                ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = true;
            CheckParent(checks);
        }

        private void CheckParent(TreeViewItem checks)
        {
            while (checks.Parent != ItemsTree)
            {
                checks = ((TreeViewItem)checks.Parent);
                int count = 0, nullable = 0;
                foreach (TreeViewItem i in checks.Items)
                    if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true)
                        count++;
                    else if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == null)
                        nullable++;
                if (count == 0 && nullable == 0)
                    ((CheckBox)((Grid)checks.Header).Children[0]).IsChecked = false;
                else if (count == checks.Items.Count)
                    ((CheckBox)((Grid)checks.Header).Children[0]).IsChecked = true;
                else
                    ((CheckBox)((Grid)checks.Header).Children[0]).IsChecked = null;
            }
        }

        private void DeleteItems_Click(object sender, RoutedEventArgs e)
        {
            List<string> files = new List<string>();
            List<string> dires = new List<string>();
            foreach (TreeViewItem i in ItemsTree.Items)
            {
                AddItem(i, files, dires);
                if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && i.IsEnabled == true)
                    if (File.Exists(i.HeaderStringFormat))
                        files.Add(i.HeaderStringFormat);
                    else
                        dires.Add(i.HeaderStringFormat);
            }
            if (files.Count + dires.Count > 0)
            {
                My_MessageBox messagebox;
                if (files.Count + dires.Count > 1)
                    messagebox = new My_MessageBox(files.Count + dires.Count);
                else if (files.Count == 1)
                    messagebox = new My_MessageBox(new FileInfo(files[0]));
                else
                    messagebox = new My_MessageBox(new DirectoryInfo(dires[0]));
                if (messagebox.ShowThisDialog() == MyMessageBoxResult.Yes)
                {
                    File_Operation window = new File_Operation();
                    window.DeleteDialogShow(files, dires);
                }
                foreach (TreeViewItem i in ItemsTree.Items)
                {
                    if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && (!File.Exists(i.HeaderStringFormat) && (!Directory.Exists(i.HeaderStringFormat))))
                    {
                        TextBlock del = new TextBlock();
                        del.Text = "Deleted";
                        del.HorizontalAlignment = HorizontalAlignment.Right;
                        del.Margin = margins[7];
                        del.FontSize = 26;
                        del.RenderTransformOrigin = point;
                        del.FontWeight = FontWeights.Bold;
                        del.Foreground = Brushes.Red;
                        del.VerticalAlignment = VerticalAlignment.Top;
                        del.RenderTransform = angel;
                        ((Grid)i.Header).Children.Add(del);
                        i.IsEnabled = false;
                    }
                    CheckBoxSun(i);
                }
            }
            else
                new My_MessageBox("Delete files", "You have not selected any file to been deleted!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void CheckBoxSun(TreeViewItem item)
        {
            foreach (TreeViewItem i in item.Items)
            {
                if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && (!File.Exists(i.HeaderStringFormat) && (!Directory.Exists(i.HeaderStringFormat))))
                {
                    TextBlock del = new TextBlock();
                    del.Text = "Deleted";
                    del.HorizontalAlignment = HorizontalAlignment.Right;
                    del.Margin = margins[7];
                    del.FontSize = 26;
                    del.RenderTransformOrigin = point;
                    del.FontWeight = FontWeights.Bold;
                    del.Foreground = Brushes.Red;
                    del.VerticalAlignment = VerticalAlignment.Top;
                    del.RenderTransform = angel;
                    ((Grid)i.Header).Children.Add(del);
                    i.IsEnabled = false;
                }
                CheckBoxSun(i);
            }
        }

        private void AddItem(TreeViewItem item, List<string> files, List<string> dires)
        {
            foreach (TreeViewItem i in item.Items)
            {
                AddItem(i, files, dires);
                if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && i.IsEnabled == true)
                {
                    if (File.Exists(i.HeaderStringFormat))
                        files.Add(i.HeaderStringFormat);
                    else
                        dires.Add(i.HeaderStringFormat);
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (TreeViewItem i in ItemsTree.Items)
            {
                ((Grid)i.Header).Width = ItemsTree.ActualWidth - 40;
                ChangeSize(i, 60);
            }
        }

        private void ChangeSize(TreeViewItem big_item, double level)
        {
            foreach (TreeViewItem i in big_item.Items)
            {
                ((Grid)i.Header).Width = ItemsTree.ActualWidth - level;
                ChangeSize(i, level + 25);
            }
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


    }
}
