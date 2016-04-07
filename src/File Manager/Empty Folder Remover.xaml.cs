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
        int progress_value = 0;
        List<EmptyDirectoriesFilesListView> worker_list = new List<EmptyDirectoriesFilesListView>();
        List<EmptyDirectoriesFilesListView> list_to_view = new List<EmptyDirectoriesFilesListView>();
        bool CloseWindow = false, stop_search = false, searching = false;
        List<string> last_roots = new List<string>(), denied = new List<string>(), unreadable_other = new List<string>();

        bool Stopping
        {
            get { return CloseWindow || stop_search; }
        }
        //ObservableCollection<EmptyDirectoriesFilesListView> list_to_view = new ObservableCollection<EmptyDirectoriesFilesListView>();

        public Empty_Folder_Remover()
        {
            InitializeComponent();
            PrepareMenu();
            UpdateButtons();
        }

        public Empty_Folder_Remover(string path)
        {
            InitializeComponent();
            PrepareMenu();
            BegainSearch(new List<string>() { path });
        }

        public Empty_Folder_Remover(List<string> paths)
        {
            InitializeComponent();
            PrepareMenu();
            BegainSearch(paths);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateButtons();
        }

        void PrepareMenu()
        {
            ContextMenu menu = new ContextMenu();
            MenuItem open = new MenuItem() { Header = "Open", InputGestureText = "Enter" };
            MenuItem open_location = new MenuItem() { Header = "Open location" };
            MenuItem preview = new MenuItem() { Header = "Preview", InputGestureText = "Space" };
            MenuItem remove = new MenuItem() { Header = "Remove from list" };
            MenuItem copy_path = new MenuItem() { Header = "Copy path" };
            MenuItem delete = new MenuItem() { Header = "Delete", InputGestureText = "Del" };
            open.Click += (object sender, RoutedEventArgs e) => { Item_MouseDoubleClick(null, null); };
            open_location.Click += (object sender, RoutedEventArgs e) => { if (SelectedItem() != null) SafetyTools.OpenLocation(SelectedItem().FullPath); };
            preview.Click += (object sender, RoutedEventArgs e) => { if (SelectedItem() != null) SafetyTools.PreviewItem(SelectedItem().FullPath); };
            remove.Click += (object sender, RoutedEventArgs e) =>
            {
                HashSet<EmptyDirectoriesFilesListView> selected = new HashSet<EmptyDirectoriesFilesListView>(items_list.SelectedItems.Cast<EmptyDirectoriesFilesListView>());
                worker_list.RemoveAll(x => selected.Contains(x));
                items_list.Items.Refresh();
                UpdateButtons();
            };
            copy_path.Click += (object sender, RoutedEventArgs e) => { if (items_list.SelectedItems.Count > 0) SafetyTools.CopyPaths(items_list.SelectedItems.Cast<EmptyDirectoriesFilesListView>().Select(x => x.FullPath)); };
            delete.Click += (object sender, RoutedEventArgs e) => { DeleteItems_Click(delete_items, null); };
            menu.Items.Add(open);
            menu.Items.Add(open_location);
            menu.Items.Add(preview);
            menu.Items.Add(new Separator());
            menu.Items.Add(remove);
            menu.Items.Add(copy_path);
            menu.Items.Add(new Separator());
            menu.Items.Add(delete);
            menu.Opened += (object sender, RoutedEventArgs e) =>
            {
                int selected = items_list.SelectedItems.Count;
                string one = selected == 0 ? "Select an item first." : selected > 1 ? "Select only one item." : null;
                SafetyTools.SetState(open, one == null, one);
                SafetyTools.SetState(open_location, one == null, one);
                SafetyTools.SetState(preview, one == null, one);
                SafetyTools.SetState(remove, selected > 0 && !searching, searching ? "Please wait, the search is running." : "Select an item first.");
                SafetyTools.SetState(copy_path, selected > 0, "Select an item first.");
                SafetyTools.SetState(delete, selected > 0 && !searching, searching ? "Please wait, the search is running." : "Select an item first.");
            };
            items_list.ContextMenu = menu;
        }

        EmptyDirectoriesFilesListView SelectedItem()
        {
            return items_list.SelectedItems.Count == 1 ? items_list.SelectedItem as EmptyDirectoriesFilesListView : null;
        }

        void UpdateButtons()
        {
            string wait = searching ? "Please wait, the search is running." : null;
            bool any = worker_list.Count > 0;
            int selected = items_list.SelectedItems.Count;
            SafetyTools.SetState(find_button, !searching, wait);
            SafetyTools.SetState(delete_items, selected > 0 && !searching, wait ?? (any ? "Select the items you want to delete first." : "The list is empty."));
            SafetyTools.SetState(select_all, any && !searching && worker_list.Any(x => x.isDirectory), wait ?? (any ? "There is no empty folder in the list (empty files are not selected by 'Select all')." : "The list is empty."));
            SafetyTools.SetState(select_none, selected > 0 && !searching, wait ?? (any ? "Nothing is selected." : "The list is empty."));
            SafetyTools.SetState(clear_list, any && !searching, wait ?? "The list is empty.");
        }

        void ShowInfoBar()
        {
            int skipped = denied.Count + unreadable_other.Count;
            if (skipped == 0)
            {
                info_bar.Visibility = Visibility.Collapsed;
                items_list.Margin = new Thickness(5, 47, 5, 5);
                return;
            }
            string text;
            if (denied.Count > 0)
                text = string.Format("{0} folder{1} skipped (no permission).", denied.Count, denied.Count > 1 ? "s were" : " was") + (unreadable_other.Count > 0 ? string.Format(" {0} could not be read.", unreadable_other.Count) : "") + (App.IsAdmin ? "" : " Run as administrator to include them.");
            else
                text = string.Format("{0} folder{1} could not be read and {2} skipped.", unreadable_other.Count, unreadable_other.Count > 1 ? "s" : "", unreadable_other.Count > 1 ? "were" : "was");
            info_text.Text = text;
            restart_admin.Visibility = denied.Count > 0 && !App.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            info_bar.Visibility = Visibility.Visible;
            items_list.Margin = new Thickness(5, 47, 5, 34);
        }

        private void ShowSkipped_Click(object sender, RoutedEventArgs e)
        {
            List<string> all = denied.Concat(unreadable_other).ToList();
            Messages.ShowMessage(string.Format("{0} folder{1} skipped, they and the folders that contain them are not counted as empty.", all.Count, all.Count > 1 ? "s were" : " was"), ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, string.Join("\n", all.Take(200).ToArray()) + (all.Count > 200 ? string.Format("\n... and {0} more.", all.Count - 200) : ""));
        }

        private void RestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (SafetyTools.RestartAsAdmin("--empty-folder-remover", last_roots))
                Close();
        }

        private void StopSearch_Click(object sender, RoutedEventArgs e)
        {
            stop_search = true;
            stop_button.IsEnabled = false;
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            items_list.SelectedItems.Clear();
            UpdateButtons();
        }

        private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtons();
        }

        private void FindEmptey_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.MultiSelect = true;
            sfd.DialogTitle = "Searching for empty directories in...";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                BegainSearch(sfd.SelectedItems.Count > 0 ? sfd.SelectedItems.ToList() : new List<string>() { sfd.SelectedItem });
        }

        private void BegainSearch(List<string> paths)
        {
            if (searching)
                return;
            List<string> roots = new List<string>();
            foreach (var i in paths)
                if (i != null && Directory.Exists(i) && !roots.Any(x => string.Equals(x.TrimEnd('\\'), i.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                    roots.Add(i);
            roots.RemoveAll(x => roots.Any(y => !string.Equals(x.TrimEnd('\\'), y.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) && x.TrimEnd('\\').StartsWith(y.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)));
            if (roots.Count > 0)
            {
                list_to_view = new List<EmptyDirectoriesFilesListView>();
                if (worker_list.Count > 0)
                    if (Messages.ShowMessage("Clear list?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.Yes)
                        ClearList_Click(clear_list, null);
                progress_value = 0;
                progressbar.Value = 0;
                progressbar.Visibility = Visibility.Visible;
                last_roots = roots;
                denied.Clear();
                unreadable_other.Clear();
                ShowInfoBar();
                stop_search = false;
                searching = true;
                stop_button.IsEnabled = true;
                stop_button.Visibility = Visibility.Visible;
                UpdateButtons();
                List<EmptyDirectoriesFilesListView> empty_directories = new List<EmptyDirectoriesFilesListView>(), empty_files = new List<EmptyDirectoriesFilesListView>();
                BackgroundWorker find_emptey_BackgroundWorker = new BackgroundWorker();
                find_emptey_BackgroundWorker.WorkerSupportsCancellation = find_emptey_BackgroundWorker.WorkerReportsProgress = true;
                find_emptey_BackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    Thread progress_thread = new Thread(() =>
                    {
                        for (int i = 1; i < 5000 && progress_value < 5100; i++)
                        {
                            if (Stopping) break;
                            int sleep_time = i < 4500 ? i / 500 : i / 200;
                            if (!find_emptey_BackgroundWorker.IsBusy)
                                break;
                            else
                                try
                                { find_emptey_BackgroundWorker.ReportProgress(i); }
                                catch (Exception) { }
                            Thread.Sleep(sleep_time);
                        }
                    });
                    progress_thread.IsBackground = true;
                    progress_thread.Start();
                    foreach (var i in roots)
                        FindEmpty(new DirectoryInfo(i), empty_directories, empty_files, denied, unreadable_other);
                };
                find_emptey_BackgroundWorker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    if (CloseWindow) return;
                    if (progressbar.Value < 5100)
                        progressbar.Value = progress_value = e.ProgressPercentage;
                };
                find_emptey_BackgroundWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    if (!CloseWindow)
                    {
                        bool stopped = stop_search;
                        searching = false;
                        stop_search = false;
                        stop_button.Visibility = Visibility.Collapsed;
                        foreach (var i in empty_directories.Concat(empty_files))
                            if (!worker_list.Any(x => string.Equals(x.FullPath, i.FullPath, StringComparison.OrdinalIgnoreCase)))
                                worker_list.Add(i);
                        worker_list.Sort((x, y) => x.isDirectory == y.isDirectory ? string.Compare(x.FullPath, y.FullPath, StringComparison.OrdinalIgnoreCase) : x.isDirectory ? -1 : 1);
                        list_to_view = worker_list;
                        items_list.DataContext = list_to_view;
                        items_list.Items.Refresh();
                        progressbar.Value = 5100;
                        progress_value = 5100;
                        if (e.Error != null)
                            Messages.ShowMessage("Something went wrong", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, e.Error.Message);
                        else if (stopped)
                            Messages.ShowMessage(string.Format("The search was stopped, {0} empty director{1} and {2} empty file{3} found until then.", empty_directories.Count, empty_directories.Count == 1 ? "y" : "ies", empty_files.Count, empty_files.Count == 1 ? "" : "s"), ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "Only folders that were searched completely are listed.");
                        else if (worker_list.Count == 0)
                            Messages.ShowMessage("No empty files or directories here.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                        progressbar.Visibility = Visibility.Hidden;
                        ShowInfoBar();
                        SelectAll_Click(null, null);
                    }
                };
                find_emptey_BackgroundWorker.RunWorkerAsync();
            }
            else
                Messages.ShowMessage("We can not find directory '" + string.Join("', '", paths.ToArray()) + "'", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
        }

        bool FindEmpty(DirectoryInfo directory, List<EmptyDirectoriesFilesListView> empty_directories, List<EmptyDirectoriesFilesListView> empty_files, List<string> denied, List<string> unreadable)
        {
            if (Stopping || Functions.IsLink(directory))
                return false;
            DirectoryInfo[] dirs;
            FileInfo[] files;
            try
            {
                dirs = directory.GetDirectories();
                files = directory.GetFiles();
            }
            catch (UnauthorizedAccessException ex)
            {
                denied.Add(directory.FullName + "\n    (" + ex.Message + ")");
                return false;
            }
            catch (Exception ex)
            {
                unreadable.Add(directory.FullName + "\n    (" + ex.Message + ")");
                return false;
            }
            bool empty = files.Length == 0;
            if (empty_files != null)
                foreach (var i in files)
                    try
                    {
                        if (i.Length == 0)
                            empty_files.Add(new EmptyDirectoriesFilesListView(i.FullName, false));
                    }
                    catch (Exception) { }
            int first_child = empty_directories.Count;
            foreach (var i in dirs)
                if (!FindEmpty(i, empty_directories, empty_files, denied, unreadable))
                    empty = false;
            if (empty && !Stopping)
            {
                empty_directories.RemoveRange(first_child, empty_directories.Count - first_child);
                empty_directories.Add(new EmptyDirectoriesFilesListView(directory.FullName, true));
            }
            return empty && !Stopping;
        }

        private void DeleteItems_Click(object sender, RoutedEventArgs e)
        {
            bool permanent = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            List<FileSystemInfo> items = new List<FileSystemInfo>();
            List<string> changed = new List<string>();
            foreach (EmptyDirectoriesFilesListView i in items_list.SelectedItems)
            {
                if (i.isDirectory)
                {
                    List<EmptyDirectoriesFilesListView> check = new List<EmptyDirectoriesFilesListView>();
                    if (Directory.Exists(i.FullPath) && FindEmpty(new DirectoryInfo(i.FullPath), check, null, new List<string>(), new List<string>()))
                        items.Add(new DirectoryInfo(i.FullPath));
                    else
                        changed.Add(i.FullPath);
                }
                else
                {
                    FileInfo file = new FileInfo(i.FullPath);
                    if (file.Exists && file.Length == 0)
                        items.Add(file);
                    else
                        changed.Add(i.FullPath);
                }
            }
            if (changed.Count > 0)
                Messages.ShowMessage(string.Format("{0} item{1} not empty any more (or not found) and will not be deleted.", changed.Count, changed.Count > 1 ? "s are" : " is"), ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", changed.ToArray()));
            if (items.Count > 0)
            {
                if (Functions.ConfirmDelete(items, permanent) && Functions.DeleteItems(items, permanent, this))
                {
                    worker_list.RemoveAll(x => (x.isDirectory && !Directory.Exists(x.FullPath)) || (!x.isDirectory && !File.Exists(x.FullPath)));
                    items_list.Items.Refresh();
                    UpdateButtons();
                }
            }
            else if (changed.Count == 0)
                Messages.ShowMessage("You have not selected any file to delete.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            worker_list.Clear();
            items_list.Items.Refresh();
            progress_value = 0;
            progressbar.Value = 0;
            if (sender != null && !searching)
            {
                denied.Clear();
                unreadable_other.Clear();
                ShowInfoBar();
            }
            UpdateButtons();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            items_list.SelectedItems.Clear();
            foreach (var i in worker_list)
                if (i.isDirectory)
                    items_list.SelectedItems.Add(i);
            UpdateButtons();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        private void AboutTool_Click(object sender, RoutedEventArgs e)
        {
            //  version : 1.1
            //  find empty directory and empty file
            //  find empty dirctory which contains just empty directoies or files
            //  1.2:
            //  improve speed
            //  new interface
            //  1.21:
            //  bugs fixed
            //  not relese// 2:
            //  find empty from multi pathes whether files or folders
            //
            AboutTool window = new AboutTool();
            window.tool_vertion.Text = "Version: 2";
            window.tool_date.Text = "";
            window.tool_name.Text = "Empty Folders Remover";
            window.tool_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/empty-directory-remover.png"));
            window.ShowDialog();
        }

        private void Item_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (SelectedItem() != null)
            {
                if (e.Key == Key.Space)
                    SafetyTools.PreviewItem(SelectedItem().FullPath);
                else if (e.Key == Key.Enter)
                    Item_MouseDoubleClick(null, null);
            }
            if (items_list.SelectedItems.Count > 0 && !searching)
                if (e.Key == Key.Delete)
                    DeleteItems_Click(delete_items, null);
        }

        private void Item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            EmptyDirectoriesFilesListView item = SelectedItem();
            if (item != null)
                SafetyTools.OpenItem(item.FullPath);
        }
    }
    class EmptyDirectoriesFilesListView : GeneralPurpose
    {
        public bool isDirectory;

        public EmptyDirectoriesFilesListView(string path, bool is_directory)
        {
            FullPath = path;
            isDirectory = is_directory;
            Image = Functions.GetFileImage24(is_directory ? "directory" : System.IO.Path.GetExtension(path));
            Kind = is_directory ? "empty directory" : "empty file (0 bytes), not selected by 'Select all'";
            Tip = path + "\n" + Kind;
        }

        public string Kind { get; set; }

        public string Tip { get; set; }
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