using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Big_File_Finder.xaml
    /// </summary>
    public partial class Big_File_Finder : Window
    {
        double process_value;
        bool CloseWindow = false, starting_window = true;
        BigFileListView root;
        BigFileListView parent;
        ObservableCollection<BigFileListView> browse_data = new ObservableCollection<BigFileListView>();
        List<BigFileListView> all_files_data = new List<BigFileListView>();
        //ObservableCollection<BigFileListView> all_files_data = new ObservableCollection<BigFileListView>();
        DirectoriesAnalizePages current_page = DirectoriesAnalizePages.Browse;
        DirectoriesAnalizePages CurrentPage
        {
            get { return current_page; }
            set
            {
                switch(value)
                {
                    case DirectoriesAnalizePages.Files:
                        if (all_files_data.Count == 0)
                        {
                            FillAllFiles(root.Children);
                            all_files_data.Sort(Sorter);
                            all_files_list.Items.Refresh();
                        }
                        break;
                    case DirectoriesAnalizePages.Staticals:
                        //show big files type or ...
                        break;
                }
                current_page = value;
            }
        }

        private void FillAllFiles(List<BigFileListView> list)
        {
            foreach (var i in list)
            {
                if (i.isDirectory)
                    FillAllFiles(i.Children);
                else
                    all_files_data.Add(i);
            }
        }

        private int Sorter(BigFileListView x, BigFileListView y)
        {
            return -1 * x.SizeDouble.CompareTo(y.SizeDouble);
        }

        public Big_File_Finder()
        {
            InitializeComponent();
        }

        public Big_File_Finder(string path)
        {
            InitializeComponent();
            BegainAnalyze(path);
        }

        void AnalyzeDisk_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Which directory do you want to anlyze?";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                BegainAnalyze(sfd.SelectedItem);
        }

        void BegainAnalyze(string path)
        {
            if (Directory.Exists(path))
            {
                BigFileListView current = null;
                process_value = 0;
                progressbar.Value = 0;
                progressbar.Visibility = Visibility.Visible;
                BackgroundWorker analyze_disk_BackgroundWorker = new BackgroundWorker();
                analyze_disk_BackgroundWorker.WorkerSupportsCancellation = true;
                analyze_disk_BackgroundWorker.WorkerReportsProgress = true;

                analyze_disk_BackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    try
                    {
                        Thread progress_thread = new Thread(() =>
                        {
                            for (int i = 1; i < 5000 && process_value < 5100; i++)
                            {
                                if (CloseWindow) return;
                                int sleep_time = i < 4500 ? i / 500 : i / 200;
                                if (!analyze_disk_BackgroundWorker.IsBusy)
                                    break;
                                else
                                    try
                                    { analyze_disk_BackgroundWorker.ReportProgress(i); }
                                    catch (Exception) { }
                                Thread.Sleep(sleep_time);
                            }
                        });
                        progress_thread.Start();
                        current = new BigFileListView();
                        current.SetInfo(new DirectoryInfo(path), root, current, ref CloseWindow);
                        current.SetSizeFromAll(current.SizeDouble);
                        current.Sort();
                    }
                    catch (Exception ex) { ((BackgroundWorker)sender).ReportProgress(0, ex.Message); }
                };

                analyze_disk_BackgroundWorker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    if (CloseWindow) return;
                    if (e.ProgressPercentage == 0)
                        Messages.ShowMessage("Something went wrong", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, e.UserState.ToString());
                    //{
                    //    //ClearList_Click(null, null);
                    //}
                    else if (progressbar.Value < 5100)
                        process_value = progressbar.Value = e.ProgressPercentage;
                    
                };


                analyze_disk_BackgroundWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    if (CloseWindow) return;
                    browse_data.Add(current);
                    root.Children.Add(current);
                    progressbar.Value = 5100;
                    process_value = 5100;
                    if (browse_data.Count > 0)
                        select_all.IsEnabled = delete_files.IsEnabled = clear_list.IsEnabled = true;
                    else
                        Messages.ShowMessage("This directory is empty.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                    Thread.Sleep(5);
                    progressbar.Visibility = Visibility.Hidden;
                    //MessageBox.Show((DateTime.UtcNow.Ticks - first).ToString());
                };
            
                analyze_disk_BackgroundWorker.RunWorkerAsync();
            }
            else
                Messages.ShowMessage("We can not find directory '" + path + "'", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
        }

        private void BigFileFinder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (item_list.SelectedIndex > -1 && browse_data[item_list.SelectedIndex].isDirectory)
            {
                parent = browse_data[item_list.SelectedIndex];
                go_up.IsEnabled = go_home.IsEnabled = true;
                current_path.Text += "\\" + parent.FileName;
                parent.Sort();
                browse_data.Clear();
                foreach (var i in parent.Children)
                    browse_data.Add(i);
                if (item_list.Items.Count > 0)
                    item_list.ScrollIntoView(item_list.Items[0]);
            }
            else if (item_list.SelectedIndex > -1)
                try { System.Diagnostics.Process.Start(browse_data[item_list.SelectedIndex].FullPath); }
                catch (Exception ex) { Messages.ShowMessage("We can not open '" + browse_data[item_list.SelectedIndex].FullPath + "'.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void ItemsList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && go_up.IsEnabled)
                GoUp_Click(go_up, null);
        }

        private void BrowseList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (browse_data[item_list.SelectedIndex].isDirectory && Directory.Exists(browse_data[item_list.SelectedIndex].FullPath))
                    new Preview().ShowDialog(new DirectoryInfo(browse_data[item_list.SelectedIndex].FullPath), ItemType.Directory);
                else if (!browse_data[item_list.SelectedIndex].isDirectory && File.Exists(browse_data[item_list.SelectedIndex].FullPath))
                    new Preview().ShowDialog(new FileInfo(browse_data[item_list.SelectedIndex].FullPath), ItemType.File);
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
            browse_data.Clear();
            foreach (var i in parent.Children)
                browse_data.Add(i);
            if (item_list.Items.Count > 0)
                item_list.ScrollIntoView(item_list.Items[0]);
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            item_list.SelectAll();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            current_path.Text = "root";
            go_up.IsEnabled = select_all.IsEnabled = delete_files.IsEnabled = clear_list.IsEnabled = false;
            browse_data.Clear();
            root.Children.Clear();
        }

        private void DeleteItems_Click(object sender, RoutedEventArgs e)
        {
            List<FileOperationItem> files = new List<FileOperationItem>();
            StringBuilder items_name = new StringBuilder();
            foreach (BigFileListView i in item_list.SelectedItems)
            {
                if (i.isDirectory)
                    files.Add(new FileOperationItem(new DirectoryInfo(i.FullPath), false, ""));
                else
                    files.Add(new FileOperationItem(new FileInfo(i.FullPath), false, ""));
                items_name.AppendLine(i.FullPath);
            }
            if (files.Count > 0)
            {
                bool delete = false;
                if (files.Count > 1)
                {
                    if (Messages.ShowMessage(string.Format("Are you sure you want to permanently delete these {0} items", files.Count), ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, items_name.ToString()) == MyMessageBoxResult.Yes)
                        delete = true;
                }
                else if (Messages.ShowMessage("Are you sure you want to permanently delete these item", ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, Messages.GetItemInfo(files[0].Item)) == MyMessageBoxResult.Yes)
                    delete = true;
                if (delete)
                {
                    //bool reload = false;
                    new Operation().CopyDialogShow(files, OperationType.Delete, "", true);

                    //if (MessageBox.Show("deleted done.\nReload List", "FILE MANAGER", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    //    reload = true;
                    //if (reload)
                    //    BegainAnalyze(cu);
                    //foreach (BigFileListView i in item_list.SelectedItems)
                    //for (int i = item_list.SelectedItems.Count - 1; i > -1; i--)
                    //{
                    //    BigFileListView item = item_list.SelectedItems[i] as BigFileListView;
                    //    if ((!item.isDirectory && !File.Exists(item.FullPath)) || (item.isDirectory && !Directory.Exists(item.FullPath)))
                    //    {
                    //        parent.Children.Remove(item);
                    //        browse_list.Remove(item);
                    //        if (reload)
                    //            foreach (var j in parent.Children)
                    //                j.PercentDouble += (item.SizeDouble * item.SizeDouble / ((j.Root.SizeDouble - item.SizeDouble) + double.Epsilon));
                    //        ChangePercentOfAll(item.Root.Children, item.SizeDouble);
                    //    }
                    //}


                    Messages.ShowMessage("Files deleted done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");

                    //new My_MessageBox("FILE MANAGER", "Files deleted done,\n Reload list?", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
                }
            }
            else
                Messages.ShowMessage("You have not selected any file to delete.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            item_list.Items.Refresh();
        }

        private void ChangePercentOfAll(List<BigFileListView> list, double size)
        {
            foreach (var i in list)
            {
                i.SizeFromAllDouble += (size * i.SizeDouble / ((i.Root.SizeDouble - size) + double.Epsilon));
                if (i.isDirectory)
                    ChangePercentOfAll(i.Children, size);
            }
        }

        private void GoHome_Click(object sender, RoutedEventArgs e)
        {
            current_path.Text = "root";
            parent = root;
            go_up.IsEnabled = go_home.IsEnabled = false;
            browse_data.Clear();
            foreach (var i in root.Children)
                browse_data.Add(i);
            if (item_list.Items.Count > 0)
                item_list.ScrollIntoView(item_list.Items[0]);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            all_files_list.DataContext = all_files_data;
            root = new BigFileListView();
            item_list.DataContext = browse_data;
            rec.Width = label1.ActualWidth;
            starting_window = false;
        }

        void TabChange_MouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            ThicknessAnimation ta = new ThicknessAnimation(new Thickness((-1 * tab_panal.Children.IndexOf((Label)sender)) * ActualWidth, 0, 0, 0), new Duration(new TimeSpan(2000000)));
            double rec_wid = 0;
            foreach (Label i in tab_panal.Children)
                if (i != sender)
                    rec_wid += i.ActualWidth;
                else
                    break;
            ThicknessAnimation ta2 = new ThicknessAnimation(new Thickness(rec_wid, 32, 0, 0), new Duration(new TimeSpan(2000000)));
            rec.Width = ((Label)sender).ActualWidth;
            rec.BeginAnimation(MarginProperty, ta2);
            mother.BeginAnimation(MarginProperty, ta);
            label1.Background = Brushes.Black; label1.Foreground = Brushes.White;
            label2.Background = Brushes.Black; label2.Foreground = Brushes.White;
            label3.Background = Brushes.Black; label3.Foreground = Brushes.White;
            ((Label)sender).Background = Brushes.White;
            ((Label)sender).Foreground = Brushes.Black;
            CurrentPage = (DirectoriesAnalizePages)tab_panal.Children.IndexOf((Label)sender) + 1;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!starting_window)
            {
                double t = (-1 * ((int)CurrentPage - 1)) * e.NewSize.Width;
                ThicknessAnimation ta = new ThicknessAnimation(new Thickness(t, 0, 0, 0), new Duration(new TimeSpan(10000)));
                mother.BeginAnimation(MarginProperty, ta);
                //while (mother.Margin.Left != t)
                //    mother.Margin = new Thickness(t, 0, 0, 0);
                for (int i = 0; i < mother.Children.Count; i++)
                {
                    ((Grid)mother.Children[i]).Width = ActualWidth;
                    ((Grid)mother.Children[i]).Margin = new Thickness(i * e.NewSize.Width, 0, 0, 0);
                }
            }
        }
    }

    class BigFileListView : GeneralPurpose, IComparable
    {
        bool is_sorted;
        public BigFileListView Parent, Root;
        List<BigFileListView> children;
        int TotalFilesCount, TotalDirectoryCount;
        public bool isDirectory;

        public BigFileListView()
        {
            children = new List<BigFileListView>();
        }

        public BigFileListView(FileInfo file, BigFileListView parent, BigFileListView home)
        {
            Root = home;
            Parent = parent;
            is_sorted = true;
            if (parent != null)
            {
                parent.SizeDouble += file.Length;
                parent.TotalFilesCount++;
            }
            FullPath = file.FullName;
            FileName = file.Name;
            Image = Functions.GetFileImage24(file.Extension);
            FileType = Functions.GetFileType(file.Extension);
            SizeDouble = file.Length;
            isDirectory = false;
        }

        public BigFileListView(DirectoryInfo directory, BigFileListView parent, BigFileListView home, ref bool Cancel)
        {
            SetInfo(directory, parent, home, ref Cancel);
        }

        internal void SetInfo(DirectoryInfo directory, BigFileListView parent, BigFileListView home, ref bool Cancel)
        {
            Root = home;
            Parent = parent;
            is_sorted = false;
            FullPath = directory.FullName;
            FileName = directory.Name;
            Image = Functions.GetFileImage24("directory");
            FileType = "Directory";
            children = new List<BigFileListView>();
            SizeDouble = GetAllChidren(directory, this, Root, ref Cancel);
            if (parent != null)
            {
                parent.TotalDirectoryCount++;
                parent.SizeDouble += SizeDouble;
                parent.TotalDirectoryCount += TotalDirectoryCount;
                parent.TotalFilesCount += TotalFilesCount;
            }
            isDirectory = true;
        }

        private double GetAllChidren(DirectoryInfo di, BigFileListView parent,BigFileListView home, ref bool Cancel)
        {
            double size = 0;
            try
            {
                var dirs = di.GetDirectories();
                foreach (var i in dirs)
                {
                    if (Cancel) return 0;
                    BigFileListView sun = new BigFileListView(i, parent, home, ref Cancel);
                    parent.Children.Add(sun);
                    size += sun.SizeDouble;
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
                        parent.Children.Add(new BigFileListView(i, parent, home));
                        size += i.Length;
                    }
                    catch (Exception) { }
            }
            catch (Exception) { }
            return size + double.Epsilon;
        }

        public void Sort()
        {
            if (!is_sorted)
                Children.Sort();
            is_sorted = true;
        }

        public void SetSizeFromAll(double total_size)
        {
            SizeFromAllDouble = Math.Round(SizeDouble * 100.0 / total_size, 2);
            if (isDirectory)
                DirectoriesAndFiles = TotalDirectoryCount + (TotalDirectoryCount > 1 ? " directories & " : " directory & ") + TotalFilesCount + (TotalFilesCount > 1 ? " files" : " file");
            if (Children != null)
                foreach (var i in Children)
                {
                    i.SetSizeFromAll(total_size);
                    DirectoriesAndFiles = TotalDirectoryCount + (TotalDirectoryCount > 1 ? " directories & " : " directory & ") + TotalFilesCount + (TotalFilesCount > 1 ? " files" : " file");
                }
            if (Parent != null)
                PercentDouble = Math.Round(SizeDouble * 100.0 / Parent.SizeDouble, 2);
            else
                PercentDouble = Math.Round(SizeDouble * 100.0 / total_size, 2);
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            return -1 * SizeDouble.CompareTo(((BigFileListView)obj).SizeDouble);
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

        double size_double;  
        public double SizeDouble
        {
            get { return size_double; }
            set
            {
                size_double = value;
                FileSize = Functions.FileSize(size_double, 2);
            }
        }

        double percent_double;
        public double PercentDouble
        {
            get { return percent_double; }
            set
            {
                percent_double = value;
                Percent = percent_double + "%";
            }
        }

        double size_from_all_double;
        public double SizeFromAllDouble
        {
            get { return size_from_all_double; }
            set
            {
                size_from_all_double = value;
                SizeFromAll = size_from_all_double + "%";
            }
        }

        public List<BigFileListView> Children
        {
            get { return children; }
            set { children = value; }
        }

        //private Brush background;
        //public Brush Background
        //{
        //    get { return background; }
        //    set { background = value; }
        //}
    
    }

    public enum DirectoriesAnalizePages
    {
        Browse = 1,
        Files = 2,
        Staticals = 3
    }
}