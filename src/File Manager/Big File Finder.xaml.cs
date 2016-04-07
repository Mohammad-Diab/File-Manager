using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Big_File_Finder.xaml
    /// </summary>
    public partial class Big_File_Finder : Window
    {
        bool CloseWindow = false, starting_window = true;
        BigFileListView root = new BigFileListView();
        BigFileListView parent;
        ObservableCollection<BigFileListView> browse_data = new ObservableCollection<BigFileListView>();
        List<BigFileListView> all_files_data = new List<BigFileListView>();
        //ObservableCollection<BigFileListView> all_files_data = new ObservableCollection<BigFileListView>();
        List<string> analyzing = new List<string>(), skipped = new List<string>(), links = new List<string>(), unreadable = new List<string>();
        StaticalType? files_filter = null;
        DirectoriesAnalizePages current_page = DirectoriesAnalizePages.Browse;
        DirectoriesAnalizePages CurrentPage
        {
            get { return current_page; }
            set
            {
                if (value != current_page)
                {
                    current_page = value;
                    switch (value)
                    {
                        case DirectoriesAnalizePages.Files:
                            RefreshAllFiles();
                            //new StreamWriter(File.OpenWrite("d:\\dd\\dn.txt")).Write(sb.ToString());
                            break;
                        case DirectoriesAnalizePages.Staticals:
                            ViewStatical();
                            break;
                    }
                }
            }
        }

        private void RefreshAllFiles()
        {
            all_files_data.Clear();
            FillAllFiles(root.Children);
            if (files_filter != null)
                all_files_data.RemoveAll(x => StaticalItem.GetStaticalType(System.IO.Path.GetExtension(x.FullPath)) != files_filter.Value);
            all_files_data.Sort(Sorter);
            all_files_list.Items.Refresh();
            filter_bar.Visibility = files_filter == null ? Visibility.Collapsed : Visibility.Visible;
            all_files_list.Margin = new Thickness(10, files_filter == null ? 45 : 74, 20, 10);
            if (files_filter != null)
                filter_text.Text = string.Format("Only {0} files: {1} file{2}, {3}", StaticalItem.TypeName(files_filter.Value).ToLower(), all_files_data.Count, all_files_data.Count == 1 ? "" : "s", Functions.FileSize(all_files_data.Sum(x => x.SizeDouble), 2));
        }

        private void RefreshPages()
        {
            if (current_page == DirectoriesAnalizePages.Files)
                RefreshAllFiles();
            else if (current_page == DirectoriesAnalizePages.Staticals)
                ViewStatical();
            else
                all_files_data.Clear();
        }

        private void ViewStatical()
        {
            List<BigFileListView> files = new List<BigFileListView>();
            CollectFiles(root.Children, files);
            StaticalItem.Reset();
            foreach (var i in files)
                StaticalItem.AddFile(System.IO.Path.GetExtension(i.FullPath), (long)i.SizeDouble);
            List<StaticalItem> tmp_list = new List<StaticalItem>();
            foreach(var i in StaticalItem.statical_list)
            {
                if (i.Value.FilesCount > 0)
                    tmp_list.Add(i.Value);
            }
            tmp_list.Sort((x, y) => { return -1 * x.Size.CompareTo(y.Size); });
            for (int i = 1; i < statical_stack.Children.Count; i++)
                    statical_stack.Children[i].Visibility = Visibility.Collapsed;
            total_row.Visibility = statical_folders.Visibility = Visibility.Visible;
            statical_hint.Visibility = tmp_list.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            for (int i = 0; i < tmp_list.Count; i++)
            {
                Grid row = statical_stack.Children[i + 1] as Grid;
                row.Visibility = Visibility.Visible;
                row.Tag = tmp_list[i].Type;
                (row.Children[0] as TextBlock).Text = tmp_list[i].Header;
                (row.Children[1] as Image).Source = new BitmapImage(new Uri(tmp_list[i].ImagePath));
                (row.Children[2] as TextBlock).Text = tmp_list[i].FilesCount + (tmp_list[i].FilesCount == 1 ? " file" : " files");
                (row.Children[3] as TextBlock).Text = Functions.FileSize(tmp_list[i].Size, 2);
                (row.Children[4] as TextBlock).Text = Math.Round(tmp_list[i].Percent, 2) + "%";
                (row.Children[5] as Rectangle).Width = Math.Max(1, tmp_list[i].Percent * 1.4);
            }
            (total_row.Children[2] as TextBlock).Text = StaticalItem.total_count + (StaticalItem.total_count == 1 ? " file" : " files");
            (total_row.Children[3] as TextBlock).Text = Functions.FileSize(StaticalItem.total_size, 2);
            int directories = 0;
            foreach (var i in root.Children)
                directories += i.DirectoriesCount;
            BigFileListView biggest = files.Count > 0 ? files.OrderByDescending(x => x.SizeDouble).First() : null;
            statical_folders.Text = root.Children.Count == 0 ? "Add a folder or a drive on the BROWSING page to see its statistics." : string.Format("{0} analysed folder{1}, {2} folder{3} inside.", root.Children.Count, root.Children.Count == 1 ? "" : "s", directories, directories == 1 ? "" : "s") + (biggest != null ? "\nBiggest file: " + biggest.FullPath + " (" + biggest.FileSize + ")" : "") + (files.Count > 0 ? "\nAverage file size: " + Functions.FileSize(StaticalItem.total_size / (double)files.Count, 2) : "");
        }

        private void CollectFiles(List<BigFileListView> list, List<BigFileListView> files)
        {
            foreach (var i in list)
                if (i.isDirectory)
                    CollectFiles(i.Children, files);
                else
                    files.Add(i);
        }

        //StringBuilder sb = new StringBuilder();
        private void FillAllFiles(List<BigFileListView> list)
        {
            foreach (var i in list)
            {

                //sb.AppendLine(i.FullPath);

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
            SafetyTools.SetWindowIcon(this, "DirectoriesAnalyzer");
            parent = root;
            string hidden = Functions.ReadSetting("analyzer.hidden");
            include_hidden.IsChecked = hidden != "0";
        }

        public Big_File_Finder(string path)
            : this(new List<string>() { path })
        {
        }

        public Big_File_Finder(List<string> paths)
            : this()
        {
            BegainAnalyze(paths);
        }

        void AnalyzeDisk_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Which folder or drive do you want to analyze?";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                BegainAnalyze(new List<string>() { sfd.SelectedItem });
        }

        static string NormalPath(string path)
        {
            try { path = System.IO.Path.GetFullPath(path); }
            catch (Exception) { }
            return path.Length > 3 ? path.TrimEnd('\\') : path;
        }

        static bool IsInside(string path, string folder)
        {
            string a = path.TrimEnd('\\') + "\\", b = folder.TrimEnd('\\') + "\\";
            return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
        }

        void BegainAnalyze(List<string> paths)
        {
            List<string> list = new List<string>();
            foreach (var p in paths)
            {
                if (string.IsNullOrEmpty(p))
                    continue;
                string path = NormalPath(p);
                if (!Directory.Exists(path))
                {
                    Messages.ShowMessage("We can not find the folder '" + path + "'", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                    continue;
                }
                string busy = analyzing.FirstOrDefault(x => IsInside(path, x) || IsInside(x, path));
                if (busy != null)
                {
                    Messages.ShowMessage("'" + busy + "' is being analysed now.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "Wait until it ends, then analyse '" + path + "' again.");
                    continue;
                }
                BigFileListView outer = root.Children.FirstOrDefault(x => !x.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase) && IsInside(path, x.FullPath));
                if (outer != null)
                {
                    Messages.ShowMessage("'" + path + "' is already part of '" + outer.FullPath + "'.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "Open it from the BROWSING list, or clear the list first to analyse it alone.");
                    continue;
                }
                if (list.Any(x => IsInside(path, x)))
                    continue;
                list.RemoveAll(x => IsInside(x, path));
                list.Add(path);
            }
            if (list.Count == 0)
                return;
            foreach (var i in list)
                foreach (var j in root.Children.Where(x => IsInside(x.FullPath, i)).ToList())
                    RemoveRoot(j);
            analyzing.AddRange(list);
            bool include = include_hidden.IsChecked;
            List<BigFileListView> results = new List<BigFileListView>();
            AnalyzeState state = new AnalyzeState() { IncludeHidden = include };
            progressbar.IsIndeterminate = true;
            progressbar.Visibility = Visibility.Visible;
            progressbar.ToolTip = "Analysing " + string.Join(", ", analyzing.ToArray());
            UpdateButtons();
            BackgroundWorker analyze_disk_BackgroundWorker = new BackgroundWorker();
            analyze_disk_BackgroundWorker.WorkerSupportsCancellation = true;

            analyze_disk_BackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                foreach (var path in list)
                {
                    if (CloseWindow) return;
                    BigFileListView current = new BigFileListView();
                    current.State = state;
                    current.SetInfo(new DirectoryInfo(path), null, current, ref CloseWindow);
                    current.SetSizeFromAll(current.SizeDouble);
                    current.Sort();
                    results.Add(current);
                }
            };

            analyze_disk_BackgroundWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (CloseWindow) return;
                foreach (var i in list)
                    analyzing.Remove(i);
                if (e.Error != null)
                    Messages.ShowMessage("Something went wrong", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, e.Error.Message);
                    //{
                    //    //ClearList_Click(null, null);
                    //}
                List<string> empty = new List<string>();
                foreach (var current in results)
                {
                    current.AttachTo(root);
                    if (parent == root)
                        browse_data.Add(current);
                    if (current.Children.Count == 0)
                        empty.Add(current.FullPath);
                }
                foreach (var i in root.Children)
                    i.SetSizeFromAll(i.SizeDouble);
                item_list.Items.Refresh();
                skipped.AddRange(state.Skipped);
                links.AddRange(state.Links);
                unreadable.AddRange(state.Unreadable);
                UpdateInfoBar();
                if (empty.Count > 0)
                    Messages.ShowMessage(empty.Count == 1 ? "This folder is empty." : "These folders are empty.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, string.Join("\n", empty.ToArray()));
                if (analyzing.Count == 0)
                {
                    progressbar.IsIndeterminate = false;
                    progressbar.Visibility = Visibility.Hidden;
                }
                else
                    progressbar.ToolTip = "Analysing " + string.Join(", ", analyzing.ToArray());
                RefreshPages();
                UpdateButtons();
                //MessageBox.Show((DateTime.UtcNow.Ticks - first).ToString());
            };

            analyze_disk_BackgroundWorker.RunWorkerAsync();
        }

        void RemoveRoot(BigFileListView item)
        {
            item.Detach();
            browse_data.Remove(item);
            if (parent != root && (parent == item || IsInside(parent.FullPath, item.FullPath)))
                GoHome_Click(null, null);
            skipped.RemoveAll(x => IsInside(x, item.FullPath));
            links.RemoveAll(x => IsInside(x, item.FullPath));
            unreadable.RemoveAll(x => IsInside(x, item.FullPath));
            UpdateInfoBar();
        }

        void UpdateInfoBar()
        {
            List<string> parts = new List<string>();
            if (skipped.Count > 0)
                parts.Add(string.Format("{0} folder{1} skipped (no permission), {2} not counted.", skipped.Count, skipped.Count > 1 ? "s were" : " was", skipped.Count > 1 ? "their sizes are" : "its size is") + (App.IsAdmin ? "" : " Run as administrator to include them."));
            if (unreadable.Count > 0)
                parts.Add(string.Format("{0} item{1} could not be read.", unreadable.Count, unreadable.Count > 1 ? "s" : ""));
            if (links.Count > 0)
                parts.Add(string.Format("{0} folder link{1} not followed.", links.Count, links.Count > 1 ? "s were" : " was"));
            info_text.Text = string.Join(" ", parts.ToArray());
            restart_admin.Visibility = skipped.Count > 0 && !App.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            info_bar.Visibility = parts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowSkipped_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder text = new StringBuilder();
            AppendList(text, "Skipped (no permission):", skipped);
            AppendList(text, "Could not be read:", unreadable);
            AppendList(text, "Folder links (not followed):", links);
            Messages.ShowMessage("These items were skipped and are not counted.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, text.ToString().Trim());
        }

        static void AppendList(StringBuilder text, string title, List<string> list)
        {
            if (list.Count == 0)
                return;
            text.AppendLine(title);
            foreach (var i in list.Take(100))
                text.AppendLine(i);
            if (list.Count > 100)
                text.AppendLine(string.Format("... and {0} more.", list.Count - 100));
            text.AppendLine();
        }

        private void RestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (SafetyTools.RestartAsAdmin("--directories-analyzer", root.Children.Select(x => x.FullPath).Concat(analyzing)))
                Close();
        }

        void UpdateButtons()
        {
            SafetyTools.SetState(go_up, parent != root, "You are at the list of analysed folders");
            SafetyTools.SetState(go_home, parent != root, "You are at the list of analysed folders");
            SafetyTools.SetState(select_all, browse_data.Count > 0, "The list is empty");
            SafetyTools.SetState(delete_files, item_list.SelectedItems.Count > 0, browse_data.Count == 0 ? "The list is empty" : "Select the items to delete first");
            SafetyTools.SetState(clear_list, root.Children.Count > 0 && analyzing.Count == 0, analyzing.Count > 0 ? "Wait until the analysis ends" : "The list is empty");
            SafetyTools.SetState(export_button, root.Children.Count > 0 && analyzing.Count == 0, analyzing.Count > 0 ? "Wait until the analysis ends" : "Analyse a folder first");
        }

        private void ItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtons();
        }

        private void BigFileFinder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BigFileListView item = item_list.SelectedItem as BigFileListView;
            if (item != null && item.isDirectory && !item.IsLink)
            {
                parent = item;
                go_up.IsEnabled = go_home.IsEnabled = true;
                current_path.Text = parent.FullPath;
                parent.Sort();
                browse_data.Clear();
                foreach (var i in parent.Children)
                    browse_data.Add(i);
                if (item_list.Items.Count > 0)
                    item_list.ScrollIntoView(item_list.Items[0]);
                UpdateButtons();
            }
            else if (item != null)
                SafetyTools.OpenItem(item.FullPath);
        }

        private void AllFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BigFileListView item = all_files_list.SelectedItem as BigFileListView;
            if (item != null)
                LaunchFile(item.FullPath);
        }

        private void ItemsList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && go_up.IsEnabled && sender == item_list)
                GoUp_Click(go_up, null);
        }

        private void ItemsList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
                PreviewItem(((ListView)sender).SelectedItem as BigFileListView);
            }
        }

        private void PreviewItem(BigFileListView item)
        {
            if (item == null)
                return;
            SafetyTools.PreviewItem(item.FullPath);
        }

        private void BrowseList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                BigFileFinder_MouseDoubleClick(null, null);
            else if (e.Key == Key.Delete)
                DeleteItems_Click(delete_files, null);
        }

        private void AllFilesList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            BigFileListView item = all_files_list.SelectedItem as BigFileListView;
            if (e.Key == Key.Enter && item != null)
                LaunchFile(item.FullPath);
            else if (e.Key == Key.Delete)
                DeleteSelected(all_files_list.SelectedItems);
        }

        void DeleteSelected(System.Collections.IList selected)
        {
            bool permanent = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            List<FileSystemInfo> files = new List<FileSystemInfo>();
            List<BigFileListView> items = new List<BigFileListView>();
            foreach (BigFileListView i in selected)
            {
                if (i.isDirectory)
                    files.Add(new DirectoryInfo(i.FullPath));
                else
                    files.Add(new FileInfo(i.FullPath));
                items.Add(i);
            }
            if (files.Count > 0)
            {
                if (Functions.ConfirmDelete(files, permanent) && Functions.DeleteItems(files, permanent, this))
                {
                    foreach (var i in items)
                        if ((i.isDirectory && !Directory.Exists(i.FullPath)) || (!i.isDirectory && !File.Exists(i.FullPath)))
                        {
                            if (root.Children.Contains(i))
                                RemoveRoot(i);
                            else
                                i.Detach();
                            browse_data.Remove(i);
                            all_files_data.Remove(i);
                        }
                    foreach (var i in root.Children)
                        i.SetSizeFromAll(i.SizeDouble);
                    if (parent != root && !root.Children.Any(x => x == parent || IsInside(parent.FullPath, x.FullPath)))
                        GoHome_Click(null, null);
                    item_list.Items.Refresh();
                    RefreshPages();
                    UpdateButtons();
                }
            }
            else
                Messages.ShowMessage("You have not selected any file to delete.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void LaunchFile(string path)
        {
            if (path != null)
                if (File.Exists(path))
                    try { System.Diagnostics.Process.Start(path); }
                    catch (Exception ex) { Messages.ShowMessage("We can not open '" + path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                else
                    Messages.ShowMessage("We can not find '" + path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
        }

        private void GoUp_Click(object sender, RoutedEventArgs e)
        {
            if (parent == root || parent.Parent == null || parent.Parent == root)
            {
                GoHome_Click(null, null);
                return;
            }
            parent = parent.Parent;
            current_path.Text = parent.FullPath;
            parent.Sort();
            browse_data.Clear();
            foreach (var i in parent.Children)
                browse_data.Add(i);
            if (item_list.Items.Count > 0)
                item_list.ScrollIntoView(item_list.Items[0]);
            UpdateButtons();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            item_list.SelectAll();
            item_list.Focus();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            current_path.Text = "root";
            go_up.IsEnabled = go_home.IsEnabled = false;
            browse_data.Clear();
            root = new BigFileListView();
            all_files_data.Clear();
            all_files_list.Items.Refresh();
            parent = root;
            files_filter = null;
            skipped.Clear();
            links.Clear();
            unreadable.Clear();
            UpdateInfoBar();
            RefreshPages();
            UpdateButtons();
        }

        private void DeleteItems_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelected(item_list.SelectedItems);
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
            UpdateButtons();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            all_files_list.DataContext = all_files_data;
            item_list.DataContext = browse_data;
            rec.Width = label1.ActualWidth;
            starting_window = false;
            item_list.ContextMenu = MakeMenu(item_list);
            all_files_list.ContextMenu = MakeMenu(all_files_list);
            UpdateButtons();
        }

        ContextMenu MakeMenu(ListView list)
        {
            ContextMenu menu = new ContextMenu();
            MenuItem open = new MenuItem() { Header = "Open" }, location = new MenuItem() { Header = "Open in File Manager" }, preview = new MenuItem() { Header = "Preview", InputGestureText = "Space" }, copy = new MenuItem() { Header = "Copy path" }, delete = new MenuItem() { Header = "Delete", InputGestureText = "Del" };
            open.Click += (object sender, RoutedEventArgs e) =>
            {
                if (list == item_list)
                    BigFileFinder_MouseDoubleClick(null, null);
                else
                    AllFiles_MouseDoubleClick(null, null);
            };
            location.Click += (object sender, RoutedEventArgs e) =>
            {
                BigFileListView item = list.SelectedItem as BigFileListView;
                if (item == null)
                    return;
                if (item.isDirectory)
                    SafetyTools.OpenItem(item.FullPath);
                else
                    SafetyTools.OpenLocation(item.FullPath);
            };
            preview.Click += (object sender, RoutedEventArgs e) => { PreviewItem(list.SelectedItem as BigFileListView); };
            copy.Click += (object sender, RoutedEventArgs e) => { SafetyTools.CopyPaths(list.SelectedItems.Cast<BigFileListView>().Select(x => x.FullPath)); };
            delete.Click += (object sender, RoutedEventArgs e) => { DeleteSelected(list.SelectedItems); };
            delete.Tag = "Recycle Bin; hold Shift while clicking to delete permanently";
            menu.Items.Add(open);
            menu.Items.Add(location);
            menu.Items.Add(preview);
            menu.Items.Add(new Separator());
            menu.Items.Add(copy);
            menu.Items.Add(delete);
            menu.Opened += (object sender, RoutedEventArgs e) =>
            {
                BigFileListView item = list.SelectedItem as BigFileListView;
                int count = list.SelectedItems.Count;
                foreach (var i in new MenuItem[] { open, location, preview, copy, delete })
                    SafetyTools.SetState(i, count > 0, "Select an item first");
                if (count > 1)
                {
                    SafetyTools.SetState(open, false, "Select only one item");
                    SafetyTools.SetState(location, false, "Select only one item");
                    SafetyTools.SetState(preview, false, "Select only one item");
                }
                location.Header = item != null && !item.isDirectory ? "Open file location" : "Open in File Manager";
                open.Header = item != null && item.isDirectory && !item.IsLink && list == item_list ? "Open (browse)" : "Open";
            };
            return menu;
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            setting_button.ContextMenu.PlacementTarget = setting_button;
            setting_button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
            setting_button.ContextMenu.IsOpen = true;
        }

        private void IncludeHidden_Click(object sender, RoutedEventArgs e)
        {
            Functions.SaveSetting("analyzer.hidden", include_hidden.IsChecked ? "1" : "0");
        }

        private void StaticalRow_Click(object sender, MouseButtonEventArgs e)
        {
            Grid row = sender as Grid;
            if (row == null || !(row.Tag is StaticalType))
                return;
            files_filter = (StaticalType)row.Tag;
            TabChange_MouseButtonUp(label2, null);
            RefreshAllFiles();
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            files_filter = null;
            RefreshAllFiles();
        }

        static string Csv(string text)
        {
            if (text == null)
                return "";
            return text.IndexOfAny(new char[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SaveFile;
            sfd.CreateFolderEnable = true;
            sfd.DialogTitle = "Export the analysis as CSV";
            sfd.AddExtension = true;
            sfd.MultiSelect = false;
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "CSV File (*.csv)", extentions = SelectFileFilter.SetExtentions(".csv") });
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (sfd.ShowWindow() != MyMessageBoxResult.OK || string.IsNullOrEmpty(sfd.SelectedItem))
                return;
            List<BigFileListView> files = new List<BigFileListView>();
            CollectFiles(root.Children, files);
            files.Sort(Sorter);
            StaticalItem.Reset();
            foreach (var i in files)
                StaticalItem.AddFile(System.IO.Path.GetExtension(i.FullPath), (long)i.SizeDouble);
            StringBuilder text = new StringBuilder();
            text.AppendLine("Analysed folder,Size (bytes),Size,Folders,Files");
            foreach (var i in root.Children)
                text.AppendLine(Csv(i.FullPath) + "," + (long)i.SizeDouble + "," + Csv(i.FileSize) + "," + i.DirectoriesCount + "," + i.FilesCount);
            text.AppendLine();
            text.AppendLine("Type,Files,Size (bytes),Size,Percent");
            foreach (var i in StaticalItem.statical_list.Values.Where(x => x.FilesCount > 0).OrderByDescending(x => x.Size))
                text.AppendLine(Csv(i.Header) + "," + i.FilesCount + "," + i.Size + "," + Csv(Functions.FileSize(i.Size, 2)) + "," + Math.Round(i.Percent, 2).ToString(System.Globalization.CultureInfo.InvariantCulture));
            text.AppendLine("Total," + StaticalItem.total_count + "," + StaticalItem.total_size + "," + Csv(Functions.FileSize(StaticalItem.total_size, 2)) + ",100");
            text.AppendLine();
            text.AppendLine("Path,Type,Size (bytes),Size,Percent of its analysed folder");
            foreach (var i in files)
                text.AppendLine(Csv(i.FullPath) + "," + Csv(i.FileType) + "," + (long)i.SizeDouble + "," + Csv(i.FileSize) + "," + i.SizeFromAllDouble.ToString(System.Globalization.CultureInfo.InvariantCulture));
            string temp = sfd.SelectedItem + ".partial";
            try
            {
                File.WriteAllText(temp, text.ToString(), new UTF8Encoding(true));
                if (File.Exists(sfd.SelectedItem))
                    File.Delete(sfd.SelectedItem);
                File.Move(temp, sfd.SelectedItem);
                Messages.ShowMessage("The analysis was exported.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, sfd.SelectedItem + "\n" + files.Count + " files");
            }
            catch (Exception ex)
            {
                try { File.Delete(temp); }
                catch (Exception) { }
                Messages.ShowMessage("We can not write the file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
            }
            if (current_page == DirectoriesAnalizePages.Staticals)
                ViewStatical();
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

        private void AboutTool_Click(object sender, RoutedEventArgs e)
        {
            //version 1:
            //find big 100 files in a directory
            //1.1:
            //can now find big 100 directory in a directory
            //version 2:
            //directory analizer base
            //explore directory and view there size in tree view
            //2.1:
            //color for each use
            //2.2:
            //imrove algorethm and speed
            //bugs fixed
            //3:
            //redesgin the tool
            //explorer to explore items instate of tree view
            //imrove calculation speed
            //improve tool speed
            //3.1
            //all files list sorted by size
            //can explore more than one item at same time
            //3.2
            //statical info
            //bug fixed (do not show all files in directory)
            AboutTool window = new AboutTool();
            window.tool_vertion.Text = "Version: 3.2";
            window.tool_date.Text = "";
            window.tool_name.Text = "Directories Analyzer";
            window.tool_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/directory-analyzer.png"));
            window.Owner = this;
            window.ShowDialog();
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

    class AnalyzeState
    {
        public bool IncludeHidden = true;
        public List<string> Skipped = new List<string>(), Links = new List<string>(), Unreadable = new List<string>();
    }

    class BigFileListView : GeneralPurpose, IComparable
    {
        bool is_sorted;
        public BigFileListView Parent, Root;
        public AnalyzeState State;
        List<BigFileListView> children;
        int TotalFilesCount, TotalDirectoryCount;
        public bool isDirectory, IsLink;

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
            //if (!Classes.JunctionPoint.Exists(directory.FullName))
                SetInfo(directory, parent, home, ref Cancel);
            //else
            //{ string h = directory.FullName; }
        }

        public int DirectoriesCount
        {
            get { return TotalDirectoryCount; }
        }

        public int FilesCount
        {
            get { return TotalFilesCount; }
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

        internal void AttachTo(BigFileListView parent)
        {
            Parent = parent;
            parent.TotalDirectoryCount += TotalDirectoryCount + 1;
            parent.TotalFilesCount += TotalFilesCount;
            parent.SizeDouble += SizeDouble;
            parent.Children.Add(this);
        }

        internal void Detach()
        {
            double size = SizeDouble;
            int directories = isDirectory ? TotalDirectoryCount + 1 : 0, files = isDirectory ? TotalFilesCount : 1;
            for (BigFileListView i = Parent; i != null; i = i.Parent)
            {
                i.SizeDouble = Math.Max(0, i.SizeDouble - size);
                i.TotalDirectoryCount = Math.Max(0, i.TotalDirectoryCount - directories);
                i.TotalFilesCount = Math.Max(0, i.TotalFilesCount - files);
            }
            if (Parent != null && Parent.Children != null)
                Parent.Children.Remove(this);
        }

        private double GetAllChidren(DirectoryInfo di, BigFileListView parent,BigFileListView home, ref bool Cancel)
        {
            double size = 0;
            AnalyzeState state = home != null ? home.State : null;
            if (Functions.IsLink(di))
            {
                FileType = "Link (not followed)";
                IsLink = true;
                if (state != null)
                    state.Links.Add(di.FullName);
                return double.Epsilon;
            }
            try
            {
                var dirs = di.GetDirectories();
                foreach (var i in dirs)
                {
                    if (Cancel) return 0;
                    if (state != null && !state.IncludeHidden && Hidden(i))
                        continue;
                    BigFileListView sun = new BigFileListView(i, parent, home, ref Cancel);
                    parent.Children.Add(sun);
                    size += sun.SizeDouble;
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (state != null)
                    state.Skipped.Add(di.FullName);
                return double.Epsilon;
            }
            catch (Exception)
            {
                if (state != null)
                    state.Unreadable.Add(di.FullName);
            }
            try
            {
                var files = di.GetFiles();
                foreach (var i in files)
                    try
                    {
                        if (Cancel) return 0;
                        if (state != null && !state.IncludeHidden && Hidden(i))
                            continue;
                        parent.Children.Add(new BigFileListView(i, parent, home));
                        size += i.Length;
                    }
                    catch (Exception)
                    {
                        if (state != null)
                            state.Unreadable.Add(i.FullName);
                    }
            }
            catch (UnauthorizedAccessException)
            {
                if (state != null)
                    state.Skipped.Add(di.FullName);
            }
            catch (Exception)
            {
                if (state != null)
                    state.Unreadable.Add(di.FullName);
            }
            return size + double.Epsilon;
        }

        static bool Hidden(FileSystemInfo item)
        {
            try { return (item.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0; }
            catch (Exception) { return false; }
        }

        public void Sort()
        {
            if (!is_sorted)
                Children.Sort();
            is_sorted = true;
        }

        public void SetSizeFromAll(double total_size)
        {
            SizeFromAllDouble = total_size > 0 ? Math.Round(SizeDouble * 100.0 / total_size, 2) : 0;
            if (isDirectory)
                DirectoriesAndFiles = TotalDirectoryCount + (TotalDirectoryCount == 1 ? " directory & " : " directories & ") + TotalFilesCount + (TotalFilesCount == 1 ? " file" : " files");
            if (Children != null)
                foreach (var i in Children)
                    i.SetSizeFromAll(total_size);
            if (Parent != null)
                PercentDouble = Parent.SizeDouble > 0 ? Math.Round(SizeDouble * 100.0 / Parent.SizeDouble, 2) : 0;
            else
                PercentDouble = total_size > 0 ? Math.Round(SizeDouble * 100.0 / total_size, 2) : 0;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            return -1 * SizeDouble.CompareTo(((BigFileListView)obj).SizeDouble);
        }

        private string directories_and_files;
        public string DirectoriesAndFiles
        {
            get { return directories_and_files; }
            set { directories_and_files = value; }
        }

        private string file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private string file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }

        private string file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }

        private string percent;
        public string Percent
        {
            get { return percent; }
            set { percent = value; }
        }

        private string size_from_all;
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

    public class StaticalItem
    {
        static internal Dictionary<StaticalType, StaticalItem> statical_list = new Dictionary<StaticalType, StaticalItem>();
        static internal long total_size = 0;
        static internal long total_count = 0;
        static Dictionary<string, StaticalType> extra_types;

        long size;
        public long Size
        {
            get { return size; }
        }

        int files_count;
        public int FilesCount
        {
            get { return files_count; }
        }

        double percent = 0;
        public double Percent
        {
            get { return percent; }
        }

        string header;
        public string Header
        {
            get { return header; }
        }

        string image_path;
        public string ImagePath
        {
            get { return image_path; }
        }

        StaticalType type;
        public StaticalType Type
        {
            get { return type; }
        }

        public StaticalItem(StaticalType type, string name, string image_path)
        {
            header = name;
            percent = 0;
            files_count = 0;
            size = 0;
            this.type = type;
            this.image_path = image_path;
            statical_list.Add(type, this);
        }

        static public void Reset()
        {
            statical_list.Clear();
            total_size = 0;
            total_count = 0;
            CreateDictionary();
        }

        static public StaticalType GetStaticalType(string extension)
        {
            FileCategory category = Functions.GetFileCategory(extension);
            switch (category)
            {
                case FileCategory.Application:
                case FileCategory.TextCanRunAsApplication:
                    return StaticalType.Application;
                case FileCategory.Archive:
                case FileCategory.DiskImage:
                    return StaticalType.Archive;
                case FileCategory.Audio:
                    return StaticalType.Music;
                case FileCategory.Vedio:
                    return StaticalType.Video;
                case FileCategory.Document:
                case FileCategory.Text:
                case FileCategory.Html:
                    return StaticalType.Document;
                case FileCategory.Font:
                    return StaticalType.Font;
                case FileCategory.Image:
                    return StaticalType.Image;
            }
            if (extra_types == null)
            {
                Dictionary<string, StaticalType> types = new Dictionary<string, StaticalType>();
                foreach (var i in new string[] { ".ts", ".mts", ".m2ts", ".m2t", ".divx", ".xvid", ".f4v", ".asf", ".dv", ".mxf", ".h264", ".hevc" })
                    types[i] = StaticalType.Video;
                foreach (var i in new string[] { ".webp", ".heic", ".heif", ".avif", ".jxl", ".raw", ".cr2", ".cr3", ".nef", ".arw", ".dng", ".orf", ".rw2", ".svg", ".svgz", ".psd", ".xcf", ".eps", ".emf", ".wmf", ".pcx", ".tga" })
                    types[i] = StaticalType.Image;
                foreach (var i in new string[] { ".aiff", ".aifc", ".alac", ".dsf", ".dff", ".caf", ".weba" })
                    types[i] = StaticalType.Music;
                foreach (var i in new string[] { ".md", ".odg", ".one", ".pub", ".vsd", ".vsdx", ".xps", ".oxps", ".pdf", ".epub", ".mobi", ".azw3", ".djvu", ".rtf", ".tex", ".csv", ".tsv", ".json" })
                    types[i] = StaticalType.Document;
                foreach (var i in new string[] { ".zst", ".lz", ".lz4", ".lzma", ".tlz", ".cab", ".arj", ".vhdx", ".vmdk", ".qcow2", ".wim", ".esd", ".appx", ".msix", ".msixbundle", ".nupkg" })
                    types[i] = StaticalType.Archive;
                foreach (var i in new string[] { ".woff", ".woff2", ".eot", ".pfb", ".pfm" })
                    types[i] = StaticalType.Font;
                foreach (var i in new string[] { ".msp", ".appref-ms", ".jar", ".ps1", ".psm1" })
                    types[i] = StaticalType.Application;
                extra_types = types;
            }
            StaticalType type;
            if (extension != null && extra_types.TryGetValue(extension.ToLower(), out type))
                return type;
            return StaticalType.Other;
        }

        static public string TypeName(StaticalType type)
        {
            switch (type)
            {
                case StaticalType.Image:
                    return "Image";
                case StaticalType.Music:
                    return "Music";
                case StaticalType.Video:
                    return "Video";
                case StaticalType.Document:
                    return "Document";
                case StaticalType.Application:
                    return "Application";
                case StaticalType.Archive:
                    return "Archive";
                case StaticalType.Font:
                    return "Font";
            }
            return "Other";
        }

        static public void AddFile(string extension, long length)
        {
            if (statical_list.Count == 0)
                CreateDictionary();
            statical_list[GetStaticalType(extension)].AddFile(length);
        }

        private static void CreateDictionary()
        {
            new StaticalItem(StaticalType.Image, "Image", Functions.StandardImageSource + "24/image.png");
            new StaticalItem(StaticalType.Music, "Music", Functions.StandardImageSource + "24/music.png");
            new StaticalItem(StaticalType.Video, "Video", Functions.StandardImageSource + "24/video.png");
            new StaticalItem(StaticalType.Document, "Document", Functions.StandardImageSource + "24/document.png");
            new StaticalItem(StaticalType.Application, "Application", Functions.StandardImageSource + "24/application.png");
            new StaticalItem(StaticalType.Archive, "Archive", Functions.StandardImageSource + "24/archive.png");
            new StaticalItem(StaticalType.Font, "Font", Functions.StandardImageSource + "24/font.png");
            new StaticalItem(StaticalType.Other, "Other", Functions.StandardImageSource + "24/other.png");
        }

        private void AddFile(long file_size)
        {
            total_size += file_size;
            size += file_size;
            files_count++;
            total_count++;
            foreach (var i in statical_list)
                i.Value.percent = total_size > 0 ? (i.Value.size * 100.0) / total_size : 0;
        }

    }

    public enum DirectoriesAnalizePages
    {
        Browse = 1,
        Files = 2,
        Staticals = 3
    }

    public enum StaticalType
    {
        Image = 1,
        Music = 2,
        Video = 3,
        Document = 4,
        Application = 5,
        Archive = 6,
        Font = 7,
        Other = 100
    }

}