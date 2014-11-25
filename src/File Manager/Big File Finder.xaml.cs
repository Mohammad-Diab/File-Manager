using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Big_File_Finder.xaml
    /// </summary>
    public partial class Big_File_Finder : Window
    {
        double process_value;
        bool CloseWindow = false;
        BigFileListView root;
        BigFileListView parent;
        List<BigFileListView> list_to_view;
        BackgroundWorker analyse_disk_BackgroundWorker;

        public Big_File_Finder()
        {
            InitializeComponent();
        }

        public void AnalyseDisk_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                ClearList_Click(null, null);
                Title = "'" + path + "' Analyze";
                progressbar.Visibility = Visibility.Visible;
                analyse_disk_BackgroundWorker = new BackgroundWorker();
                analyse_disk_BackgroundWorker.WorkerSupportsCancellation = true;
                analyse_disk_BackgroundWorker.WorkerReportsProgress = true;
                analyse_disk_BackgroundWorker.ProgressChanged += Anlyzer_ProgressChanged;
                analyse_disk_BackgroundWorker.DoWork += Anlyzer_DoWork;
                analyse_disk_BackgroundWorker.RunWorkerCompleted += Anlyzer_RunWorkerCompleted;
                analyse_disk_BackgroundWorker.RunWorkerAsync(path);
            }
        }

        void Anlyzer_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = new BackgroundWorker();
            try
            {
                Thread progress_thread = new Thread(() =>
                {
                    for (int i = 1; i < 5000 && process_value < 5100; i++)
                    {
                        if (CloseWindow) return;
                        int sleep_time = i < 4500 ? i / 500 : i / 200;
                        if (!bw.IsBusy)
                            break;
                        try
                        { bw.ReportProgress(i); }
                        catch (Exception) { }
                        Thread.Sleep(sleep_time);
                    }
                });
                progress_thread.Start();
                root = new BigFileListView(new DirectoryInfo(e.Argument.ToString()), null, ref CloseWindow);
                root.SetSizeFromAll(root.size_long);
                root.Sort();
            }
            catch (Exception ex) { ((BackgroundWorker)sender).ReportProgress(0, ex.Message); }
        }

        void Anlyzer_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (CloseWindow) return;
            if (e.ProgressPercentage == 0)
            {
                ClearList_Click(null, null);
                new My_MessageBox("FILE MANAGER", e.UserState.ToString(), new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            }
            else if (progressbar.Value < 5100)
            {
                process_value = e.ProgressPercentage;
                progressbar.Value = e.ProgressPercentage;
            }
        }

        void Anlyzer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (CloseWindow) return;
            item_list.DataContext = root.Children;
            list_to_view = root.Children;
            progressbar.Value = 5100;
            process_value = 5100;
            if (item_list.Items.Count > 0)
                select_all.IsEnabled = delete_files.IsEnabled = clear_list.IsEnabled = true;
            else
                new My_MessageBox("File Manager", "This directory is empty.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
            Thread.Sleep(5);
            progressbar.Visibility = Visibility.Hidden;
        }

        private void BigFileFinder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (item_list.SelectedIndex > -1 && list_to_view[item_list.SelectedIndex].isDirectory)
            {
                parent = list_to_view[item_list.SelectedIndex];
                go_up.IsEnabled = go_home.IsEnabled = true;
                current_path.Text += "\\" + parent.FileName;
                parent.Sort();
                list_to_view = parent.Children;
                item_list.DataContext = parent.Children;
                if (item_list.Items.Count > 0)
                    item_list.ScrollIntoView(item_list.Items[0]);
            }
            else if (item_list.SelectedIndex > -1)
                try { System.Diagnostics.Process.Start(list_to_view[item_list.SelectedIndex].FullPath); }
                catch (Exception ex) { new My_MessageBox("FILE MANAGER", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void ItemsList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && go_up.IsEnabled)
                GoUp_Click(go_up, null);
        }

        private void BigFileFinder_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (list_to_view[item_list.SelectedIndex].isDirectory && Directory.Exists(list_to_view[item_list.SelectedIndex].FullPath))
                    new Preview().ShowDialog(new DirectoryInfo(list_to_view[item_list.SelectedIndex].FullPath), ItemType.Directory);
                else if (!list_to_view[item_list.SelectedIndex].isDirectory && File.Exists(list_to_view[item_list.SelectedIndex].FullPath))
                    new Preview().ShowDialog(new FileInfo(list_to_view[item_list.SelectedIndex].FullPath), ItemType.File);
                if (MainWindow.GoToPath)
                {
                    MainWindow.GoToPath = false;
                    new MainWindow(MainWindow.to_go_path).ShowDialog();
                }
            }
            else if (e.Key == Key.Enter)
                BigFileFinder_MouseDoubleClick(null, null);
            else if (e.Key == Key.Delete)
                DeleteItems_Click(delete_files, null);
        }

        private void GoUp_Click(object sender, RoutedEventArgs e)
        {
            if (parent.Parent.Parent == null)
                go_up.IsEnabled = go_home.IsEnabled = false;
            parent = parent.Parent;
            current_path.Text = current_path.Text.Substring(0, current_path.Text.LastIndexOf('\\'));
            parent.Sort();
            list_to_view = parent.Children;
            item_list.DataContext = parent.Children;
            if (item_list.Items.Count > 0)
                item_list.ScrollIntoView(item_list.Items[0]);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            item_list.SelectAll();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            Title = "Directory Analyzer";
            process_value = 0;
            progressbar.Value = 0;
            current_path.Text = "root";
            item_list.DataContext = null;
            go_up.IsEnabled = select_all.IsEnabled = delete_files.IsEnabled = clear_list.IsEnabled = false;
            root = null;
        }

        private void DeleteItems_Click(object sender, RoutedEventArgs e)
        {
            List<FileOperationItem> files = new List<FileOperationItem>();
            foreach (BigFileListView i in item_list.SelectedItems)
                if (i.isDirectory)
                    files.Add(new FileOperationItem(new DirectoryInfo(i.FullPath), false, ""));
                else
                    files.Add(new FileOperationItem(new FileInfo(i.FullPath), false, ""));
            if (files.Count > 0)
            {
                My_MessageBox messagebox;
                if (files.Count > 1)
                    messagebox = new My_MessageBox(files.Count);
                else
                    messagebox = new My_MessageBox(files[0].Item);
                if (messagebox.ShowThisDialog() == MyMessageBoxResult.Yes)
                {
                    new Operation().CopyDialogShow(files, Op.Delete, "", true);
                    foreach (BigFileListView i in item_list.SelectedItems)
                        if ((!i.isDirectory && !File.Exists(i.FullPath)) || (i.isDirectory && !Directory.Exists(i.FullPath)))
                            parent.Children.Remove(i);
                    new My_MessageBox("FILE MANAGER", "Files deleted done.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
                }
            }
            else
                new My_MessageBox("FILE MANAGER", "You have not selected any file to been deleted!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
            item_list.Items.Refresh();
        }

        private void GoHome_Click(object sender, RoutedEventArgs e)
        {
            current_path.Text = "root";
            parent = null;
            go_up.IsEnabled = go_home.IsEnabled = false;
            item_list.DataContext = list_to_view = root.Children;
            if (item_list.Items.Count > 0)
                item_list.ScrollIntoView(item_list.Items[0]);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }
    }

    class BigFileListView : GeneralPurpose, IComparable
    {
        bool is_sorted;
        public BigFileListView Parent;
        List<BigFileListView> children;
        public long size_long;
        int TotalFilesCount, TotalDirectoryCount;

        public void Sort()
        {
            if (!is_sorted)
                Children.Sort();
            is_sorted = true;
        }

        public List<BigFileListView> Children
        {
            get { return children; }
            set { children = value; }
        }

        public BigFileListView(FileInfo file, BigFileListView parent)
        {
            Parent = parent;
            is_sorted = true;
            if (parent != null)
            {
                parent.size_long += file.Length;
                parent.TotalFilesCount++;
            }
            FullPath = file.FullName;
            FileName = file.Name;
            Image = Functions.GetFileImage24(file.Extension);
            FileType = Functions.GetFileType(file.Extension);
            size_long = file.Length;
            FileSize = Functions.FileSize(size_long, 2);
            isDirectory = false;
        }

        public BigFileListView(DirectoryInfo directory, BigFileListView parent, ref bool Cancel)
        {
            Parent = parent;
            is_sorted = false;
            FullPath = directory.FullName;
            FileName = directory.Name;
            Image = Functions.GetFileImage24("directory");
            FileType = "Directory";
            children = new List<BigFileListView>();
            size_long = GetAllChidren(directory, this, ref Cancel);
            if (parent != null)
            {
                parent.TotalDirectoryCount++;
                parent.size_long += size_long;
                parent.TotalDirectoryCount += TotalDirectoryCount;
                parent.TotalFilesCount += TotalFilesCount;
            }
            FileSize = Functions.FileSize(size_long, 2);
            isDirectory = true;
        }

        public void SetSizeFromAll(long total_size)
        {
            SizeFromAll = Math.Round(size_long * 100.0 / total_size, 2) + "%";
            if (isDirectory)
                DirectoriesAndFiles = TotalDirectoryCount + (TotalDirectoryCount > 1 ? " directories & " : " directory & ") + TotalFilesCount + (TotalFilesCount > 1 ? " files" : " file");
            if (Children != null)
                foreach (var i in Children)
                {
                    i.SetSizeFromAll(total_size);
                    DirectoriesAndFiles = TotalDirectoryCount + (TotalDirectoryCount > 1 ? " directories & " : " directory & ") + TotalFilesCount + (TotalFilesCount > 1 ? " files" : " file");
                }
            if (Parent != null)
                percent = Math.Round(size_long * 100.0 / Parent.size_long, 2) + "%";
            else
                percent = Math.Round(size_long * 100.0 / total_size, 2) + "%";
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            return -1 * size_long.CompareTo(((BigFileListView)obj).size_long);
        }

        private long GetAllChidren(DirectoryInfo di, BigFileListView parent, ref bool Cancel)
        {
            long size = 0;
            try
            {
                var dirs = di.GetDirectories();
                foreach (var i in dirs)
                {
                    if (Cancel) return 0;
                    BigFileListView sun = new BigFileListView(i, parent, ref Cancel);
                    parent.Children.Add(sun);
                    size += sun.size_long;
                }
            }
            catch (Exception) { }
            try
            {
                var files = di.GetFiles();
                foreach (var i in files)
                    try
                    {
                        if (Cancel) return 0;
                        parent.Children.Add(new BigFileListView(i, parent));
                        size += i.Length;
                    }
                    catch (Exception) { }
            }
            catch (Exception) { }
            return size;
        }

        private String directories_and_files;
        public string DirectoriesAndFiles
        {
            get { return directories_and_files; }
            set { directories_and_files = value; }
        }

        private String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private String file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }

        private String file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }

        private String percent;
        public string Percent
        {
            get { return percent; }
            set { percent = value; }
        }

        private String size_from_all;
        public string SizeFromAll
        {
            get { return size_from_all; }
            set { size_from_all = value; }
        }

        public bool isDirectory;

        //private Brush background;
        //public Brush Background
        //{
        //    get { return background; }
        //    set { background = value; }
        //}
    }
}

//Point point = new Point(0.5, 0.5);
//RotateTransform angel = new RotateTransform(45);
//Tree big_items;
//long full_size;
//Thickness[] margins = { new Thickness(10, 10, 0, 0), new Thickness(26, 10, 0, 0), new Thickness(125, 10, 10, 0), new Thickness(125, 34, 10, 0), new Thickness(125, 60, 10, 0), new Thickness(125, 84, 10, 0), new Thickness(10, 0, 10, 3), new Thickness(0, 40, 10, 0) };
//Brush[] brushes = { new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)), new SolidColorBrush(Color.FromArgb(180, 255, 255, 245)), new SolidColorBrush(Color.FromArgb(180, 255, 250, 235)), new SolidColorBrush(Color.FromArgb(180, 255, 245, 220)), new SolidColorBrush(Color.FromArgb(180, 255, 230, 180)), new SolidColorBrush(Color.FromArgb(180, 255, 220, 140)), new SolidColorBrush(Color.FromArgb(180, 255, 200, 80)), new SolidColorBrush(Color.FromArgb(180, 255, 190, 75)), new SolidColorBrush(Color.FromArgb(180, 255, 175, 50)), new SolidColorBrush(Color.FromArgb(180, 255, 160, 50)), new SolidColorBrush(Color.FromArgb(180, 255, 140, 50)), new SolidColorBrush(Color.FromArgb(180, 255, 120, 30)), new SolidColorBrush(Color.FromArgb(180, 255, 100, 30)), new SolidColorBrush(Color.FromArgb(180, 255, 85, 20)), new SolidColorBrush(Color.FromArgb(180, 255, 75, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 65, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 55, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 45, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 35, 0)), new SolidColorBrush(Color.FromArgb(180, 255, 20, 0)) };
//public TimeSpan timespan = new TimeSpan(1);
//Dictionary<string, List<BigItemElement>> dic;

 //public long getAllFiles(DirectoryInfo directory)
        //{
        //    long size = 0;
        //    foreach (var i in directory.GetDirectories())
        //        try
        //        {
        //            long current_size = getAllFiles(i);
        //            size += current_size;
        //            if (dic.Keys.Contains(directory.FullName))
        //                dic[directory.FullName].Add(new BigItemElement(i, current_size, true));
        //            else
        //            {
        //                List<BigItemElement> list = new List<BigItemElement>();
        //                list.Add(new BigItemElement(i, current_size, true));
        //                dic.Add(directory.FullName, list);
        //            }
        //        }
        //        catch (Exception ex) { /*MessageBox.Show(ex.Message);*/ }
        //    FileInfo[] files = new FileInfo[0];
        //    try { files = directory.GetFiles(); }
        //    catch (Exception ex) { /*MessageBox.Show(ex.Message);*/}
        //    foreach (var i in files)
        //    {
        //        size += i.Length;
        //        if (dic.Keys.Contains(directory.FullName))
        //            dic[directory.FullName].Add(new BigItemElement(i, i.Length, true));
        //        else
        //        {
        //            List<BigItemElement> list = new List<BigItemElement>();
        //            list.Add(new BigItemElement(i, i.Length, true));
        //            dic.Add(directory.FullName, list);
        //        }
        //    }
        //    return size;
        //}

        //private void item_list_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    //if (item_list.SelectedItem != null)
        //    //{
        //    //    BigFileListView item = (BigFileListView)item_list.SelectedItem;
        //    //    if (item.isDirectory)
        //    //    {
        //    //        current_path.Text = item.FullPath;
        //    //        list_to_view.Clear();
        //    //        BackgroundWorker BW_Viewer = new BackgroundWorker();
        //    //        BW_Viewer.WorkerSupportsCancellation = true;
        //    //        BW_Viewer.WorkerReportsProgress = true;
        //    //        BW_Viewer.ProgressChanged += bw1_ProgressChanged;
        //    //        BW_Viewer.DoWork += bw1_DoWork;
        //    //        BW_Viewer.RunWorkerAsync(current_path.Text);
        //    //    }
        //    //}
        //}

        

        //private void current_path_PreviewKeyUp(object sender, KeyEventArgs e)
        //{
        //    //if (e.Key == Key.Return && dic != null)
        //    //    if (dic.Keys.Contains(current_path.Text))
        //    //    {
        //    //        list_to_view.Clear();
        //    //        BackgroundWorker BW_Viewer = new BackgroundWorker();
        //    //        BW_Viewer.WorkerSupportsCancellation = true;
        //    //        BW_Viewer.WorkerReportsProgress = true;
        //    //        BW_Viewer.ProgressChanged += bw1_ProgressChanged;
        //    //        BW_Viewer.DoWork += bw1_DoWork;
        //    //        BW_Viewer.RunWorkerAsync(current_path.Text);
        //    //    }
        //    //    else
        //    //        MessageBox.Show("Invalid Path");
        //}



        //private long GetAllFiles(DirectoryInfo directory, Tree parent)
        //{
        //    long size = 0;
        //    foreach (var i in directory.GetDirectories())
        //        try
        //        {
        //            Tree sun = new Tree(parent, new BigItemElement(i, 0, true));
        //            parent.AddChild(sun);
        //            long dir_size = GetAllFiles(i, sun);
        //            sun.SetSize(dir_size);
        //            size += dir_size;
        //        }
        //        catch (Exception) { }
        //    foreach (FileInfo i in directory.GetFiles())
        //    {
        //        parent.AddChild(new BigItemElement(i, i.Length, false));
        //        size += i.Length;
        //    }
        //    parent.children.Sort();
        //    return size;
        //}

        //private void FindBigItems_Click(object sender, RoutedEventArgs e)
        //{
        //    string path = new Folder_Picker().ShowThisDialog();
        //    if (path != null)
        //    {
        //        DirectoryInfo root_directory = new DirectoryInfo(path);
        //        big_items = new Tree(null, new BigItemElement(root_directory, 0, true));
        //        full_size = GetAllFiles(root_directory, big_items);
        //        big_items.SetSize(full_size);
        //        //PrintResult(big_items, item_list);
        //        if (item_list.Items.Count > 0)
        //            DeleteItems.IsEnabled = true;
        //        else
        //            DeleteItems.IsEnabled = false;
        //    }
        //}

        //private void PrintResult(Tree big_item, TreeView ItemsTree)
        //{
        //    //TreeViewItem root = MakeTreeViewItem(big_item, 40, big_item.content.size);
        //    //ItemsTree.Items.Add(root);
        //    //foreach (var i in big_item.children)
        //    //{
        //    //    TreeViewItem sun = MakeTreeViewItem(i, 60, big_item.content.size);
        //    //    root.Items.Add(sun);
        //    //    PrintResult(i, sun, 60);
        //    //}
        //}

        //private void PrintResult(Tree big_item, TreeViewItem parent, double level)
        //{
        //    //foreach (var i in big_item.children)
        //    //{
        //    //    TreeViewItem sun = MakeTreeViewItem(i, level, i.parent.content.size);
        //    //    parent.Items.Add(sun);
        //    //    PrintResult(i, sun, level + 25);
        //    //}
        //}

        //private TreeViewItem MakeTreeViewItem(Tree item, double level, long parent_size)
        //{
        //    int file_index = 0;
        //    if (!item.content.is_directory)
        //        file_index = Functions.GetFileIndex(item.content.file_system.Extension);

        //    CheckBox ch = new CheckBox();
        //    ch.HorizontalAlignment = HorizontalAlignment.Left;
        //    ch.VerticalAlignment = VerticalAlignment.Top;
        //    ch.Margin = margins[0];
        //    ch.Checked += Ch_Checked;
        //    ch.Unchecked += Ch_Unchecked;

        //    Image image = new Image();
        //    image.HorizontalAlignment = HorizontalAlignment.Left;
        //    image.VerticalAlignment = VerticalAlignment.Top;
        //    image.Width = image.Height = 90;
        //    image.Margin = margins[1];
        //    if (item.content.is_directory)
        //        image.Source = FileInformation.file_bitmap_64[FileInformation.file_bitmap_64.Count - 2];
        //    else
        //        image.Source = FileInformation.file_bitmap_64[file_index];

        //    TextBlock tx1 = new TextBlock();
        //    tx1.VerticalAlignment = VerticalAlignment.Top;
        //    tx1.FontSize = 14;
        //    tx1.Margin = margins[2];
        //    tx1.TextTrimming = TextTrimming.CharacterEllipsis;
        //    tx1.Text = "Path: " + item.content.file_system.FullName;

        //    TextBlock tx2 = new TextBlock();
        //    tx2.VerticalAlignment = VerticalAlignment.Top;
        //    tx2.FontSize = 16;
        //    tx2.Margin = margins[3];
        //    tx2.TextTrimming = TextTrimming.CharacterEllipsis;
        //    tx2.Text = "Size: " + Functions.FileSize(item.content.size) + "\t«" + Math.Round((100.0 * item.content.size / parent_size), 3) + "%» of current directory " + "\t«" + Math.Round((100.0 * item.content.size / full_size), 3) + "%» of total.";

        //    TextBlock tx3 = new TextBlock();
        //    tx3.VerticalAlignment = VerticalAlignment.Top;
        //    tx3.FontSize = 14;
        //    tx3.Margin = margins[4];
        //    tx3.TextTrimming = TextTrimming.CharacterEllipsis;
        //    tx3.Text = "Type: " + (item.content.is_directory ? "Directory" : Functions.GetFileType(item.content.file_system.Extension));

        //    TextBlock tx4 = new TextBlock();
        //    tx4.VerticalAlignment = VerticalAlignment.Top;
        //    tx4.FontSize = 14;
        //    tx4.Margin = margins[5];
        //    tx4.TextTrimming = TextTrimming.CharacterEllipsis;
        //    tx4.Text = "Date Modified: " + item.content.file_system.LastWriteTimeUtc;

        //    Rectangle separator = new Rectangle();
        //    separator.Fill = Brushes.Silver;
        //    separator.Height = 1;
        //    separator.Margin = margins[6];
        //    separator.VerticalAlignment = VerticalAlignment.Bottom;

        //    Grid container = new Grid();
        //    container.Height = 115;
        //    container.Width = ItemsTree.ActualWidth - level;
        //    container.Children.Add(ch);
        //    container.Children.Add(image);
        //    container.Children.Add(tx1);
        //    container.Children.Add(tx2);
        //    container.Children.Add(tx3);
        //    container.Children.Add(tx4);
        //    container.Children.Add(separator);

        //    TreeViewItem TVI = new TreeViewItem();
        //    TVI.ToolTip = item.content.file_system.FullName;
        //    TVI.Tag = item.content.file_system;
        //    TVI.HeaderStringFormat = item.content.file_system.FullName;
        //    TVI.Header = container;

        //    return TVI;
        //}

        //private void Ch_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    TreeViewItem checks = ((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent);
        //    foreach (TreeViewItem i in checks.Items)
        //        ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = false;
        //    CheckParent(checks);
        //}

        //private void Ch_Checked(object sender, RoutedEventArgs e)
        //{
        //    TreeViewItem checks = ((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent);
        //    foreach (TreeViewItem i in checks.Items)
        //        ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = true;
        //    CheckParent(checks);
        //}

        //private void CheckParent(TreeViewItem checks)
        //{
        //    while (checks.Parent != item_list)
        //    {
        //        checks = ((TreeViewItem)checks.Parent);
        //        int count = 0, nullable = 0;
        //        foreach (TreeViewItem i in checks.Items)
        //            if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true)
        //                count++;
        //            else if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == null)
        //                nullable++;
        //        if (count == 0 && nullable == 0)
        //            ((CheckBox)((Grid)checks.Header).Children[0]).IsChecked = false;
        //        else if (count == checks.Items.Count)
        //            ((CheckBox)((Grid)checks.Header).Children[0]).IsChecked = true;
        //        else
        //            ((CheckBox)((Grid)checks.Header).Children[0]).IsChecked = null;
        //    }
        //}

            //List<string> files = new List<string>();
            //List<string> dires = new List<string>();
            //foreach (TreeViewItem i in ItemsTree.Items)
            //{
            //    AddItem(i, files, dires);
            //    if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && i.IsEnabled == true)
            //        if (File.Exists(i.HeaderStringFormat))
            //            files.Add(i.HeaderStringFormat);
            //        else
            //            dires.Add(i.HeaderStringFormat);
            //}
            //if (files.Count + dires.Count > 0)
            //{
            //    My_MessageBox messagebox;
            //    if (files.Count + dires.Count > 1)
            //        messagebox = new My_MessageBox(files.Count + dires.Count);
            //    else if (files.Count == 1)
            //        messagebox = new My_MessageBox(new FileInfo(files[0]));
            //    else
            //        messagebox = new My_MessageBox(new DirectoryInfo(dires[0]));
            //    if (messagebox.ShowThisDialog() == MyMessageBoxResult.Yes)
            //    {
            //        File_Operation window = new File_Operation();
            //        window.DeleteDialogShow(files, dires);
            //    }
            //    foreach (TreeViewItem i in ItemsTree.Items)
            //    {
            //        if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && (!File.Exists(i.HeaderStringFormat) && (!Directory.Exists(i.HeaderStringFormat))))
            //        {
            //            TextBlock del = new TextBlock();
            //            del.Text = "Deleted";
            //            del.HorizontalAlignment = HorizontalAlignment.Right;
            //            del.Margin = margins[7];
            //            del.FontSize = 26;
            //            del.RenderTransformOrigin = point;
            //            del.FontWeight = FontWeights.Bold;
            //            del.Foreground = Brushes.Red;
            //            del.VerticalAlignment = VerticalAlignment.Top;
            //            del.RenderTransform = angel;
            //            ((Grid)i.Header).Children.Add(del);
            //            i.IsEnabled = false;
            //        }
            //        CheckBoxSun(i);
            //    }
            //}
            //else
            //    new My_MessageBox("Delete files", "You have not selected any file to been deleted!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        //}

        //private void CheckBoxSun(TreeViewItem item)
        //{
        //    foreach (TreeViewItem i in item.Items)
        //    {
        //        if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && (!File.Exists(i.HeaderStringFormat) && (!Directory.Exists(i.HeaderStringFormat))))
        //        {
        //            TextBlock del = new TextBlock();
        //            del.Text = "Deleted";
        //            del.HorizontalAlignment = HorizontalAlignment.Right;
        //            del.Margin = margins[7];
        //            del.FontSize = 26;
        //            del.RenderTransformOrigin = point;
        //            del.FontWeight = FontWeights.Bold;
        //            del.Foreground = Brushes.Red;
        //            del.VerticalAlignment = VerticalAlignment.Top;
        //            del.RenderTransform = angel;
        //            ((Grid)i.Header).Children.Add(del);
        //            i.IsEnabled = false;
        //        }
        //        CheckBoxSun(i);
        //    }
        //}

        //private void AddItem(TreeViewItem item, List<string> files, List<string> dires)
        //{
        //    foreach (TreeViewItem i in item.Items)
        //    {
        //        AddItem(i, files, dires);
        //        if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && i.IsEnabled == true)
        //        {
        //            if (File.Exists(i.HeaderStringFormat))
        //                files.Add(i.HeaderStringFormat);
        //            else
        //                dires.Add(i.HeaderStringFormat);
        //        }
        //    }
        //}

        //private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    foreach (TreeViewItem i in ItemsTree.Items)
        //    {
        //        ((Grid)i.Header).Width = ItemsTree.ActualWidth - 40;
        //        ChangeSize(i, 60);
        //    }
        //}

        //private void ChangeSize(TreeViewItem big_item, double level)
        //{
        //    //foreach (TreeViewItem i in big_item.Items)
        //    //{
        //    //    ((Grid)i.Header).Width = ItemsTree.ActualWidth - level;
        //    //    ChangeSize(i, level + 25);
        //    //}
        //}
//    }
//}


        //private void BW_Viewer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{

        //}

        //private void bw1_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    string dir = e.Argument.ToString();
        //    BackgroundWorker bw = (BackgroundWorker)sender;
        //    dic[dir].Sort();
        //    foreach (var i in dic[dir])
        //        if (i.file_system.Exists)
        //        {
        //            bw.ReportProgress(0, i);
        //            Thread.Sleep(timespan);
        //        }
        //}

        //private void bw1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    BigItemElement i = ((BigItemElement)e.UserState);
        //    int index = (i.file_system is FileInfo) ? Functions.GetFileIndex(i.file_system.Extension) : FileInformation.file_extentions.Length - 2;
        //    double perc = Math.Round((i.size * 100.0) / (full_size), 2);
        //    double brush_index = Math.Round((20.0 * i.size / full_size /*Current Size*/), 0);
        //    //if (i.file_system is FileInfo)
        //    ////list_to_view.Add(new BigFileListView(i.file_system.FullName, i.file_system.Name, Functions.GetFileImage24(i.file_system.Extension), Functions.GetFileType(i.file_system.Extension), Functions.FileSize(i.size), 5.88 + " %", perc + " %", brush_index > 19 ? brushes[19] : brush_index < 0 ? brushes[0] : brushes[(int)brush_index], i.file_system is DirectoryInfo));
        //    //else
        //        //list_to_view.Add(new BigFileListView(i.file_system.FullName, i.file_system.Name, Functions.GetFileImage24("directory"), "Directory", Functions.FileSize(i.size), 5.88 + " %", perc + " %", brush_index > 19 ? brushes[19] : brush_index < 0 ? brushes[0] : brushes[(int)brush_index], i.file_system is DirectoryInfo));
        //        //list_to_view[list_to_view.Count - 1].Tap += Big_File_Finder_MouseDoubleClick;
        //}

//private void Window_Loaded(object sender, RoutedEventArgs e)
//{
//    item_list.DataContext = list_to_view;
//}