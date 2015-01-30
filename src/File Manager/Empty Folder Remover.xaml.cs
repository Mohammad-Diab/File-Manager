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
    /// Interaction logic for Empty_Folder_Remover.xaml
    /// </summary>
    public partial class Empty_Folder_Remover : Window
    {
        int process_value = 0;
        List<EmptyDirectoriesFilesListView> worker_list;
        BackgroundWorker find_emptey_BackgroundWorker;
        bool CloseWindow = false;
        ObservableCollection<EmptyDirectoriesFilesListView> list_to_view = new ObservableCollection<EmptyDirectoriesFilesListView>();

        public Empty_Folder_Remover()
        {
            InitializeComponent();
            items_list.DataContext = list_to_view;
            if (Functions.IsRunningAsAdmin())
            {
                items_list.Margin = new Thickness(10, 41, 10, 10);
                caution.Visibility = Visibility.Collapsed;
            }
        }

        private void FindEmptey_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                ClearList_Click(clear_list, null);
                progressbar.Visibility = Visibility.Visible;
                find_emptey_BackgroundWorker = new BackgroundWorker();
                find_emptey_BackgroundWorker.WorkerSupportsCancellation = true;
                find_emptey_BackgroundWorker.WorkerReportsProgress = true;
                find_emptey_BackgroundWorker.DoWork += FindEmptey_DoWork;
                find_emptey_BackgroundWorker.ProgressChanged += FindEmptey_ProgressChanged;
                find_emptey_BackgroundWorker.RunWorkerCompleted += FindEmptey_RunWorkerCompleted;
                find_emptey_BackgroundWorker.RunWorkerAsync(path);
            }
        }

        void FindEmptey_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread progress_thread = new Thread(() =>
            {
                for (int i = 1; i < 5000 && process_value < 5100; i++)
                {
                    if (CloseWindow) break;
                    int sleep_time = i < 4500 ? i / 500 : i / 200;
                    if (!find_emptey_BackgroundWorker.IsBusy)
                        break;
                    try
                    { find_emptey_BackgroundWorker.ReportProgress(i); }
                    catch (Exception) { }
                    Thread.Sleep(sleep_time);
                }
            });
            progress_thread.Start();
            worker_list = new List<EmptyDirectoriesFilesListView>();
            try
            { EmptyDirectoriesFilesListView root = new EmptyDirectoriesFilesListView(new DirectoryInfo(e.Argument.ToString()), null, worker_list, ref CloseWindow); }
            catch (Exception ex) { find_emptey_BackgroundWorker.ReportProgress(0, ex); }
        }

        void FindEmptey_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (CloseWindow) return;
            if (e.ProgressPercentage == 0)
            {
                ClearList_Click(clear_list, null);
                new My_MessageBox("File Manager", e.UserState.ToString(), new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            }
            else if (progressbar.Value < 5100)
            {
                process_value = e.ProgressPercentage;
                progressbar.Value = e.ProgressPercentage;
            }
        }

        void FindEmptey_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!CloseWindow)
            {
                if (worker_list != null)
                    foreach (var i in worker_list)
                        list_to_view.Add(i);
                progressbar.Value = 5100;
                process_value = 5100;
                if (list_to_view.Count > 0)
                    delete_items.IsEnabled = select_all.IsEnabled = clear_list.IsEnabled = true;
                else
                    new My_MessageBox("File Manager", "No empty files or directories here.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
                Thread.Sleep(5);
                progressbar.Visibility = Visibility.Hidden;
            }
        }

        private void DeleteItems_Click(object sender, RoutedEventArgs e)
        {
            List<FileOperationItem> files = new List<FileOperationItem>();
            foreach (EmptyDirectoriesFilesListView i in items_list.SelectedItems)
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
                    for (int i = items_list.SelectedItems.Count - 1; i > 0; i--)
                    {
                        EmptyDirectoriesFilesListView current_item = items_list.SelectedItems[i] as EmptyDirectoriesFilesListView;
                        if ((!current_item.isDirectory && !File.Exists(current_item.FullPath)) || (current_item.isDirectory && !Directory.Exists(current_item.FullPath)))
                            list_to_view.Remove(current_item);
                    }
                    new My_MessageBox("File Manager", "Files deleted done.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
                }
            }
            else
                new My_MessageBox("File Manager", "You have not selected any file to been deleted!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            list_to_view.Clear();
            process_value = 0;
            progressbar.Value = 0;
            delete_items.IsEnabled = select_all.IsEnabled = clear_list.IsEnabled = false;
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            items_list.SelectAll();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }
    }

    class EmptyDirectoriesFilesListView : GeneralPurpose
    {
        public EmptyDirectoriesFilesListView parent;
        public long size;
        public bool isDirectory;

        public EmptyDirectoriesFilesListView(FileSystemInfo file, EmptyDirectoriesFilesListView parent, List<EmptyDirectoriesFilesListView> empty_list, ref bool Cancel)
        {
            this.parent = parent;
            FullPath = file.FullName;
            Image = Functions.GetFileImage24(file is FileInfo ? file.Extension : "directory");
            isDirectory = file is FileInfo ? false : true;
            if (file is FileInfo)
                size = ((FileInfo)file).Length;
            else
                size = GetAllChidren((DirectoryInfo)file, empty_list, ref Cancel);
        }

        private long GetAllChidren(DirectoryInfo di, List<EmptyDirectoriesFilesListView> empty_list, ref bool Cancel)
        {
            long size = 0;
            DirectoryInfo[] dirs = null;
            FileInfo[] files = null;
            try
            {
                dirs = di.GetDirectories();
                foreach (var i in dirs)
                {
                    if (Cancel) return 0;
                    EmptyDirectoriesFilesListView sun = new EmptyDirectoriesFilesListView(i, this, empty_list, ref Cancel);
                    if (sun.size != 0)
                        size += sun.size;
                }
            }
            catch (Exception) { }
            try
            {
                files = di.GetFiles();
                foreach (var i in files)
                    try
                    {
                        if (Cancel) return 0;
                        EmptyDirectoriesFilesListView sun = new EmptyDirectoriesFilesListView(i, this, empty_list, ref Cancel);
                        if (sun.size == 0)
                            empty_list.Add(sun);
                        else
                            size += sun.size;
                    }
                    catch (Exception) { }
            }
            catch (Exception) { }
            if (size == 0 && dirs != null && files != null)
            {
                int i = empty_list.Count - 1;
                while (i >= 0 && empty_list[i].parent == this)
                {
                    if (Cancel) return 0;
                    empty_list.RemoveAt(i);
                    i--;
                }
                empty_list.Add(this);
            }
            return size;
        }
    }
}

//Tree directory_tree;
//Point point = new Point(0.5, 0.5);
//RotateTransform angel = new RotateTransform(45);
//Thickness[] old_margins = { new Thickness(10, 10, 0, 0), new Thickness(30, 10, 0, 0), new Thickness(110, 10, 10, 0), new Thickness(110, 34, 10, 0), new Thickness(110, 58, 11, 0), new Thickness(10, 0, 10, 3), new Thickness(10, 0, 10, 3), new Thickness(0, 40, 10, 0) };
//Thickness[] margins = { new Thickness(10, 7, 0, 0), new Thickness(31, 3, 0, 0), new Thickness(60, 5, 10, 0), new Thickness(110, 34, 10, 0), new Thickness(110, 58, 11, 0), new Thickness(10, 0, 10, 3), new Thickness(10, 0, 10, 3), new Thickness(26, 8, 0, 0) };
        




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
//    foreach (TreeViewItem i in big_item.Items)
//    {
//        ((Grid)i.Header).Width = ItemsTree.ActualWidth - level;
//        ChangeSize(i, level + 25);
//    }
//}

//private void CheckBoxSun(TreeViewItem item)
//{
//    foreach (TreeViewItem i in item.Items)
//    {
//        if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && (!File.Exists(i.HeaderStringFormat) && (!Directory.Exists(i.HeaderStringFormat))))
//        {
//            TextBlock del = new TextBlock();
//            del.Text = "Deleted";
//            del.HorizontalAlignment = HorizontalAlignment.Left;
//            del.Margin = margins[7];
//            del.FontSize = 9.5;
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



//private void PrintResult(Tree directory_tree)
//{
//    foreach (Tree i in directory_tree.children)
//        if (i.content.size == 0)
//            ItemsTree.Items.Add(MakeTreeViewItem(i, 40));
//        else
//            PrintResult(i);
//}

//private TreeViewItem MakeTreeViewItem(Tree item, double level)
//{
//    int file_index = 0;
//    if (!item.content.is_directory)
//        file_index = Functions.GetFileIndex(item.content.file_system.Extension);

//    CheckBox ch = new CheckBox();
//    ch.HorizontalAlignment = HorizontalAlignment.Left;
//    ch.VerticalAlignment = VerticalAlignment.Top;
//    ch.Margin = margins[0];
//    ch.IsChecked = true;
//    ch.Checked += Ch_Checked;
//    ch.Unchecked += Ch_Unchecked;

//    Image image = new Image();
//    image.HorizontalAlignment = HorizontalAlignment.Left;
//    image.VerticalAlignment = VerticalAlignment.Top;
//    image.Width = image.Height = 24;
//    image.Margin = margins[1];
//    if (item.content.is_directory)
//        image.Source =  Functions.GetFileImage24("directory");
//    else
//        image.Source = Functions.GetFileImage24(item.content.file_system.Extension);

//    TextBlock tx1 = new TextBlock();
//    tx1.VerticalAlignment = VerticalAlignment.Top;
//    tx1.FontSize = 14;
//    tx1.Margin = margins[2];
//    tx1.TextTrimming = TextTrimming.CharacterEllipsis;
//    tx1.Text = item.content.file_system.FullName;

//    //TextBlock tx2 = new TextBlock();
//    //tx2.VerticalAlignment = VerticalAlignment.Top;
//    //tx2.FontSize = 14;
//    //tx2.Margin = margins[3];
//    //tx2.TextTrimming = TextTrimming.CharacterEllipsis;
//    //tx2.Text = "Type: " + (item.content.is_directory ? "Directory" : Functions.GetFileType(file_index, item.content.file_system.Extension));

//    //TextBlock tx3 = new TextBlock();
//    //tx3.VerticalAlignment = VerticalAlignment.Top;
//    //tx3.FontSize = 14;
//    //tx3.Margin = margins[4];
//    //tx3.TextTrimming = TextTrimming.CharacterEllipsis;
//    //tx3.Text = "Date Modified: " + item.content.file_system.LastWriteTimeUtc;

//    //Rectangle separator = new Rectangle();
//    //separator.Fill = Brushes.Silver;
//    //separator.Height = 1;
//    //separator.Margin = margins[5];
//    //separator.VerticalAlignment = VerticalAlignment.Bottom;

//    Grid container = new Grid();
//    container.Height = 30;
//    container.Width = ItemsTree.ActualWidth - level;
//    container.Children.Add(ch);
//    container.Children.Add(image);
//    container.Children.Add(tx1);
//    //container.Children.Add(tx2);
//    //container.Children.Add(tx3);
//    //container.Children.Add(separator);

//    TreeViewItem TVI = new TreeViewItem();
//    TVI.ToolTip = item.content.file_system.FullName;
//    TVI.Tag = item.content.file_system;
//    TVI.HeaderStringFormat = item.content.file_system.FullName;
//    TVI.Header = container;
//    foreach (Tree i in item.children)
//        TVI.Items.Add(MakeTreeViewItem(i, level + 20));

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
//    while (checks.Parent != ItemsTree)
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

//private long Get_All_Files(DirectoryInfo directory, Tree parent)
//{
//    long size = 0;
//    foreach (var i in directory.GetDirectories())
//        try
//        {
//            Tree sun = new Tree(parent, new BigItemElement(i, 0, true));
//            parent.AddChild(sun);
//            long dir_size = Get_All_Files(i, sun);
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




//Delete

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
//        File_Operation window=new File_Operation();
//        window.DeleteDialogShow(files, dires);
//    }
//    foreach (TreeViewItem i in ItemsTree.Items)
//    {
//        if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && (!File.Exists(i.HeaderStringFormat) && (!Directory.Exists(i.HeaderStringFormat))))
//        {
//            TextBlock del = new TextBlock();
//            del.Text = "Deleted";
//            del.HorizontalAlignment = HorizontalAlignment.Left;
//            del.Margin = margins[7];
//            del.FontSize = 9.5;
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