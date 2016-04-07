using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
//using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Identical_File_Remover.xaml
    /// </summary>
    public partial class Identical_File_Remover : Window
    {
        List<DublicateFiles_ListView> dublicate_data = new List<DublicateFiles_ListView>();
        DuplicateFileRemoverPages current_page = DuplicateFileRemoverPages.DuplicateFind;
        DuplicateFileRemoverPages CurrentPage
        {
            get { return current_page; }
            set
            {
                if (value != current_page)
                    switch (value)
                    {
                        case DuplicateFileRemoverPages.DuplicateFind:
                            break;
                    }
                current_page = value;
            }
        }

        DublicateSortBy sort_by = DublicateSortBy.Count;
        DublicateSortBy SortBy
        {
            get { return sort_by; }
            set
            {
                if (sort_by - value == 4 || sort_by - value == -4)
                    dublicate_data.Reverse();
                else
                {
                    sort_by = value;
                    dublicate_data.Sort(Sorter);
                }
                sort_by = value;
                item_group_viewer.Items.Refresh();
            }
        }

        private int Sorter(DublicateFiles_ListView x, DublicateFiles_ListView y)
        {
            switch(SortBy)
            {
                case DublicateSortBy.Count:
                    return x.Dublicats.Count.CompareTo(y.Dublicats.Count);
                case DublicateSortBy.DecCount:
                    return -1 * (x.Dublicats.Count.CompareTo(y.Dublicats.Count));

                case DublicateSortBy.Size:
                    return x.Size.CompareTo(y.Size);
                case DublicateSortBy.DecSize:
                    return y.Size.CompareTo(x.Size);

                case DublicateSortBy.TotalSize:
                    return (x.Size*x.Dublicats.Count).CompareTo(y.Size * y.Dublicats.Count);
                case DublicateSortBy.DecTotalSize:
                    return (y.Size * y.Dublicats.Count).CompareTo(x.Size * x.Dublicats.Count);

                case DublicateSortBy.Type:
                    return string.Compare(x.FileType, y.FileType);
                case DublicateSortBy.DecType:
                    return string.Compare(y.FileType, x.FileType);

                default:
                    return 0;
            }
        }

        SHA256 Hasher = new SHA256Managed();
        int unreadable_count = 0;
        //int files_count = 0;
        bool CloseWindow = false, starting_window = true, stop_search = false, searching = false, comparing = false;
        List<string> last_roots = new List<string>(), skipped_items = new List<string>();
        int denied_count = 0;

        bool Stopping
        {
            get { return CloseWindow || stop_search; }
        }
        double dublicate_size = 0;
        double deleted_size = 0;
        double total_size = 0;
        //List<BasicFileInfo> file_list;
        //List<string> sha1_string;
        //BackgroundWorker find_duplicate_BackgroundWorker;

        public Identical_File_Remover()
        {
            InitializeComponent();
            PrepareMenus();
            UpdateButtons();
        }

        public Identical_File_Remover(List<string> items)
        {
            InitializeComponent();
            PrepareMenus();
            FindDuplicate(items);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.Width = label1.ActualWidth;
            item_group_viewer.DataContext = dublicate_data;
            starting_window = false;
            UpdateCompareButton();
            if (!searching)
                Recompute();
        }

        void PrepareMenus()
        {
            ContextMenu file_menu = new ContextMenu();
            MenuItem open = new MenuItem() { Header = "Open", InputGestureText = "Enter" };
            MenuItem open_location = new MenuItem() { Header = "Open location" };
            MenuItem preview = new MenuItem() { Header = "Preview", InputGestureText = "Space" };
            MenuItem keep_only = new MenuItem() { Header = "Keep only this copy" };
            MenuItem copy_path = new MenuItem() { Header = "Copy path" };
            open.Click += (object sender, RoutedEventArgs e) => { if (SelectedCopy() != null) SafetyTools.OpenItem(SelectedCopy().FullPath); };
            open_location.Click += (object sender, RoutedEventArgs e) => { if (SelectedCopy() != null) SafetyTools.OpenLocation(SelectedCopy().FullPath); };
            preview.Click += (object sender, RoutedEventArgs e) => { if (SelectedCopy() != null) SafetyTools.PreviewItem(SelectedCopy().FullPath); };
            keep_only.Click += (object sender, RoutedEventArgs e) =>
            {
                FilePath_ListView item = SelectedCopy();
                DublicateFiles_ListView group = GroupOf(item);
                if (group == null)
                    return;
                foreach (var i in group.Dublicats)
                    i.IsChecked = i != item;
                Recompute();
            };
            copy_path.Click += (object sender, RoutedEventArgs e) => { if (item_viewer.SelectedItems.Count > 0) SafetyTools.CopyPaths(item_viewer.SelectedItems.Cast<FilePath_ListView>().Select(x => x.FullPath)); };
            file_menu.Items.Add(open);
            file_menu.Items.Add(open_location);
            file_menu.Items.Add(preview);
            file_menu.Items.Add(new Separator());
            file_menu.Items.Add(keep_only);
            file_menu.Items.Add(new Separator());
            file_menu.Items.Add(copy_path);
            file_menu.Opened += (object sender, RoutedEventArgs e) =>
            {
                string one = item_viewer.SelectedItems.Count == 0 ? "Select a file first." : item_viewer.SelectedItems.Count > 1 ? "Select only one file." : null;
                SafetyTools.SetState(open, one == null, one);
                SafetyTools.SetState(open_location, one == null, one);
                SafetyTools.SetState(preview, one == null, one);
                SafetyTools.SetState(keep_only, one == null, one);
                SafetyTools.SetState(copy_path, item_viewer.SelectedItems.Count > 0, "Select a file first.");
            };
            item_viewer.ContextMenu = file_menu;

            ContextMenu group_menu = new ContextMenu();
            MenuItem keep_oldest = new MenuItem() { Header = "Keep oldest only" };
            MenuItem untick = new MenuItem() { Header = "Untick all" };
            keep_oldest.Click += (object sender, RoutedEventArgs e) =>
            {
                DublicateFiles_ListView group = item_group_viewer.SelectedItem as DublicateFiles_ListView;
                if (group != null)
                {
                    group.IsChecked = true;
                    Recompute();
                }
            };
            untick.Click += (object sender, RoutedEventArgs e) =>
            {
                DublicateFiles_ListView group = item_group_viewer.SelectedItem as DublicateFiles_ListView;
                if (group != null)
                {
                    group.IsChecked = false;
                    Recompute();
                }
            };
            group_menu.Items.Add(keep_oldest);
            group_menu.Items.Add(untick);
            group_menu.Opened += (object sender, RoutedEventArgs e) =>
            {
                DublicateFiles_ListView group = item_group_viewer.SelectedItem as DublicateFiles_ListView;
                SafetyTools.SetState(keep_oldest, group != null, "Select a group first.");
                SafetyTools.SetState(untick, group != null && group.Dublicats.Any(x => x.IsChecked), group == null ? "Select a group first." : "No copy of this group is ticked.");
            };
            keep_oldest.Tag = "Tick every copy of this group except the oldest one";
            item_group_viewer.ContextMenu = group_menu;
        }

        FilePath_ListView SelectedCopy()
        {
            return item_viewer.SelectedItems.Count == 1 ? item_viewer.SelectedItem as FilePath_ListView : null;
        }

        DublicateFiles_ListView GroupOf(FilePath_ListView item)
        {
            return item == null ? null : dublicate_data.FirstOrDefault(x => x.Dublicats.Contains(item));
        }

        void UpdateButtons()
        {
            string wait = searching ? "Please wait, the search is running." : null;
            bool any = dublicate_data.Count > 0;
            int ticked = dublicate_data.Sum(x => x.Dublicats.Count(y => y.IsChecked));
            SafetyTools.SetState(find_dublicate, !searching, wait);
            SafetyTools.SetState(delete, ticked > 0 && !searching, wait ?? (any ? "Tick the copies you want to delete first." : "The list is empty."));
            SafetyTools.SetState(select_all, any && !searching && dublicate_data.Any(x => x.IsChecked != true), wait ?? (any ? "Every group already has all copies but one ticked." : "The list is empty."));
            SafetyTools.SetState(select_none, ticked > 0 && !searching, wait ?? (any ? "No copy is ticked." : "The list is empty."));
            SafetyTools.SetState(clear_list, any && !searching, wait ?? "The list is empty.");
            SafetyTools.SetState(sort_button, dublicate_data.Count > 1 && !searching, wait ?? "There is nothing to sort.");
        }

        void Recompute()
        {
            total_size = dublicate_size = deleted_size = 0;
            foreach (var i in dublicate_data)
            {
                int count = i.Dublicats.Count(x => x.IsChecked);
                total_size += (double)i.Size * i.Dublicats.Count;
                dublicate_size += (double)i.Size * (i.Dublicats.Count - 1);
                deleted_size += (double)i.Size * count;
                i.FullPath = i.Dublicats.Count + (i.Dublicats.Count > 1 ? " copies" : " copy");
                i.TotalFileSize = Functions.FileSize(i.Size * i.Dublicats.Count, 2);
            }
            double width = Math.Max(0, total.ActualWidth);
            duplicate.Width = total_size > 0 ? Math.Max(0, dublicate_size / total_size * width - 2) : 0;
            deleted_grid.Width = total_size > 0 ? deleted_size / total_size * width + 4 : 4;
            deleted_text.Text = Functions.FileSize(deleted_size, 2) + " will be deleted";
            deleted_grid.ToolTip = deleted_text.Text + "\nThe pink part is the space used by the extra copies.";
            flow_shart.Visibility = dublicate_data.Count > 0 ? Visibility.Visible : Visibility.Hidden;
            number.Text = dublicate_data.Count.ToString();
            items_label.Text = dublicate_data.Count == 1 ? "item" : "items";
            item_group_viewer.Items.Refresh();
            item_viewer.Items.Refresh();
            UpdateButtons();
        }

        void AddSkipped(string path, Exception ex)
        {
            if (!searching)
                return;
            lock (skipped_items)
            {
                if (ex is UnauthorizedAccessException)
                    denied_count++;
                skipped_items.Add(path + "\n    (" + ex.Message + ")");
            }
        }

        void ShowInfoBar()
        {
            if (skipped_items.Count == 0)
            {
                info_bar.Visibility = Visibility.Collapsed;
                lists_grid.Margin = new Thickness(10, 79, 15, 33);
                return;
            }
            int other = skipped_items.Count - denied_count;
            string text;
            if (denied_count > 0)
                text = string.Format("{0} item{1} skipped (no permission).", denied_count, denied_count > 1 ? "s were" : " was") + (other > 0 ? string.Format(" {0} could not be read.", other) : "") + (App.IsAdmin ? "" : " Run as administrator to include them.");
            else
                text = string.Format("{0} item{1} could not be read and {2} skipped.", other, other > 1 ? "s" : "", other > 1 ? "were" : "was");
            info_text.Text = text;
            restart_admin.Visibility = denied_count > 0 && !App.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            info_bar.Visibility = Visibility.Visible;
            lists_grid.Margin = new Thickness(10, 107, 15, 33);
        }

        private void ShowSkipped_Click(object sender, RoutedEventArgs e)
        {
            Messages.ShowMessage(string.Format("{0} item{1} skipped.", skipped_items.Count, skipped_items.Count > 1 ? "s were" : " was"), ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, string.Join("\n", skipped_items.Take(200).ToArray()) + (skipped_items.Count > 200 ? string.Format("\n... and {0} more.", skipped_items.Count - 200) : ""));
        }

        private void RestartAdmin_Click(object sender, RoutedEventArgs e)
        {
            if (SafetyTools.RestartAsAdmin("--identical-file-remover", last_roots))
                Close();
        }

        private void StopSearch_Click(object sender, RoutedEventArgs e)
        {
            stop_search = true;
            stop_search_button.IsEnabled = false;
        }

        private void FindDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if (dublicate_data.Count == 0 || Messages.ShowMessage("You should clear list before continue, do you want to do that?", ModrenMessageButton.YesNo, ModrenMessageImage.Question,ModrenMessageSound.Question,false,"") == MyMessageBoxResult.Yes)
            {
                SelectFileDialog sfd = new SelectFileDialog();
                sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
                sfd.CreateFolderEnable = sfd.AddExtension = false;
                sfd.DialogTitle = "Add all files in this directory.";
                sfd.MultiSelect = true;
                if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                    FindDuplicate(sfd.SelectedItems);
            }
        }

        private void FindDuplicate(List<string> _paths)
        {
            List<string> paths = new List<string>();
            foreach (string i in _paths)
                try
                {
                    string full = Path.GetFullPath(i).TrimEnd('\\');
                    if (full.EndsWith(":"))
                        full += "\\";
                    if (!paths.Any(x => string.Equals(x, full, StringComparison.OrdinalIgnoreCase)))
                        paths.Add(full);
                }
                catch (Exception) { }
            paths.RemoveAll(x => paths.Any(y => !string.Equals(x, y, StringComparison.OrdinalIgnoreCase) && x.StartsWith(y.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)));
            if (paths.Count == 0)
                return;
            ClearList_Click(null, null);
            unreadable_count = 0;
            denied_count = 0;
            skipped_items.Clear();
            last_roots = paths;
            stop_search = false;
            searching = true;
            ShowInfoBar();
            progressbar.Value = 0;
            progressbar.Visibility = Visibility.Visible;
            mother.IsEnabled = tab_panal.IsEnabled = false;
            stop_search_button.IsEnabled = true;
            stop_search_button.Visibility = Visibility.Visible;
            UpdateButtons();
            TaskbarItemInfo.ProgressValue = 0;

            BackgroundWorker find_duplicate = new BackgroundWorker();
            find_duplicate.WorkerReportsProgress = true;
            find_duplicate.DoWork += (object dw_sender, DoWorkEventArgs dw_e) =>
            {
                List<DublicateFileInfo> files = new List<DublicateFileInfo>();
                HashSet<string> listed = new HashSet<string>();
                foreach (string i in paths)
                {
                    if (File.Exists(i))
                    {
                        FileInfo tmp_file = new FileInfo(i);
                        if (!Functions.IsLink(tmp_file) && tmp_file.Length > 0 && listed.Add(tmp_file.FullName.ToLower()))
                            files.Add(new DublicateFileInfo() { fullpath = tmp_file.FullName, size = tmp_file.Length, file = tmp_file });
                    }
                    else
                    {
                        try
                        { GetAllFiles(files, new DirectoryInfo(i), listed); }
                        catch (Exception) { }
                    }
                }
                find_duplicate.ReportProgress(-1, 25);
                files.Sort((x, y) => x.size.CompareTo(y.size));

                for (int i = 0; i < files.Count; i++)
                {
                    if (Stopping) return;
                    find_duplicate.ReportProgress(0, 70.0 / files.Count);
                    int j = i + 1;
                    while (j < files.Count && files[j].size == files[i].size)
                        j++;
                    if (j - i > 1)
                    {
                        List<DublicateFileInfo> same_size = files.GetRange(i, j - i);
                        foreach (var k in same_size)
                        {
                            if (Stopping) return;
                            ReadPartsOF(k);
                        }
                        foreach (var parts_group in same_size.Where(x => x.parts != null).GroupBy(x => x.parts))
                            if (parts_group.Count() > 1)
                            {
                                foreach (var k in parts_group)
                                {
                                    if (Stopping) return;
                                    CalcHash(k);
                                }
                                foreach (var hash_group in parts_group.Where(x => x.SHA1 != null).GroupBy(x => x.SHA1))
                                {
                                    List<DublicateFileInfo> copies = new List<DublicateFileInfo>();
                                    foreach (var k in hash_group)
                                        if (k.file_id == null || !copies.Any(x => x.file_id == k.file_id))
                                            copies.Add(k);
                                    if (copies.Count > 1)
                                    {
                                        copies.Sort(KeepOrder);
                                        find_duplicate.ReportProgress(1);
                                        foreach (var k in copies)
                                            find_duplicate.ReportProgress(2, k.file);
                                        find_duplicate.ReportProgress(3);
                                    }
                                }
                            }
                        find_duplicate.ReportProgress(0, 70.0 * (j - i - 1) / files.Count);
                    }
                    i = j - 1;
                }
            };

            find_duplicate.ProgressChanged += (object bw_sender, ProgressChangedEventArgs bw_e) =>
            {
                switch (bw_e.ProgressPercentage)
                {
                    case -1:
                        progressbar.Value = Convert.ToDouble(bw_e.UserState);
                        TaskbarItemInfo.ProgressValue = progressbar.Value / 100;
                        break;
                    case 0:
                        progressbar.Value += Convert.ToDouble(bw_e.UserState);
                        TaskbarItemInfo.ProgressValue = progressbar.Value / 100;
                        break;
                    case 1:
                        dublicate_data.Add(new DublicateFiles_ListView());
                        item_group_viewer.Items.Refresh();
                        break;
                    case 2:
                        dublicate_data[dublicate_data.Count - 1].Dublicats.Add(new FilePath_ListView(((FileInfo)bw_e.UserState)));
                        break;
                    case 3:
                        dublicate_data[dublicate_data.Count - 1].Size = dublicate_data[dublicate_data.Count - 1].Dublicats[0].File.Length;
                        dublicate_data[dublicate_data.Count - 1].FileType = Functions.GetFileType(dublicate_data[dublicate_data.Count - 1].Dublicats[0].File.Extension);
                        dublicate_data[dublicate_data.Count - 1].FullPath = dublicate_data[dublicate_data.Count - 1].Dublicats.Count + " copies";
                        dublicate_data[dublicate_data.Count - 1].TotalFileSize = Functions.FileSize(dublicate_data[dublicate_data.Count - 1].Size * dublicate_data[dublicate_data.Count - 1].Dublicats.Count, 2);
                        number.Text = dublicate_data.Count.ToString();
                        break;
                }
            };

            find_duplicate.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
            {
                if (!CloseWindow)
                {
                    bool stopped = stop_search;
                    searching = false;
                    stop_search = false;
                    mother.IsEnabled = tab_panal.IsEnabled = true;
                    stop_search_button.Visibility = Visibility.Collapsed;
                    foreach (var i in dublicate_data)
                        i.IsChecked = true;
                    dublicate_data.Sort(Sorter);
                    progressbar.Value = 100;
                    TaskbarItemInfo.ProgressValue = 100;
                    string result = dublicate_data.Count > 0 ? string.Format("We found {0} item{1}.\nIn each item the oldest copy is kept (not ticked).", dublicate_data.Count, dublicate_data.Count > 1 ? "s" : "") : "Congratulations we did not find any dublicates here.";
                    if (stopped)
                        result = "The search was stopped, only the items found until then are listed.\n" + result;
                    if (bw_e.Error != null)
                        result += "\nThe search stopped because of an error: " + bw_e.Error.Message;
                    if (skipped_items.Count > 0)
                        result += string.Format("\n{0} item{1} could not be read and {2} skipped (see the information bar).", skipped_items.Count, skipped_items.Count > 1 ? "s" : "", skipped_items.Count > 1 ? "were" : "was");
                    Messages.ShowMessage(stopped ? "Search stopped." : "Search done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, result);
                    TaskbarItemInfo.ProgressValue = 0;
                    progressbar.Visibility = Visibility.Hidden;
                    Recompute();
                    ShowInfoBar();
                }
            };
            find_duplicate.RunWorkerAsync();
        }

        int KeepOrder(DublicateFileInfo x, DublicateFileInfo y)
        {
            DateTime x_date = DateTime.MaxValue, y_date = DateTime.MaxValue;
            try { x_date = x.file.CreationTimeUtc; }
            catch (Exception) { }
            try { y_date = y.file.CreationTimeUtc; }
            catch (Exception) { }
            if (x_date != y_date)
                return x_date.CompareTo(y_date);
            if (x.fullpath.Length != y.fullpath.Length)
                return x.fullpath.Length.CompareTo(y.fullpath.Length);
            return string.Compare(x.fullpath, y.fullpath, StringComparison.OrdinalIgnoreCase);
        }

        private void GetAllFiles(List<DublicateFileInfo> list_to_add, DirectoryInfo directory, HashSet<string> listed)
        {
            if (Stopping)
                return;
            FileInfo[] files;
            DirectoryInfo[] sub_dirs;
            try
            {
                files = directory.GetFiles();
                sub_dirs = directory.GetDirectories();
            }
            catch (Exception ex)
            {
                AddSkipped(directory.FullName, ex);
                return;
            }
            foreach (var i in files)
            {
                try
                {
                    if (i.Length > 0 && !Functions.IsLink(i) && listed.Add(i.FullName.ToLower()))
                        list_to_add.Add(new DublicateFileInfo() { fullpath = i.FullName, size = i.Length, file = i });
                }
                catch (Exception ex) { AddSkipped(i.FullName, ex); }
                if (Stopping)
                    return;
            }
            foreach (var i in sub_dirs)
            {
                if (!Functions.IsLink(i))
                    GetAllFiles(list_to_add, i, listed);
                if (Stopping)
                    return;
            }
        }

        private void ReadPartsOF(DublicateFileInfo item)
        {
            if (item.size < 52428799)   //50 MiB
                item.parts = item.size.ToString();
            else
            {
                StringBuilder sb = new StringBuilder();
                try
                {
                    using (BinaryReader BR = new BinaryReader(new FileStream(item.fullpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            BR.BaseStream.Position = BR.BaseStream.Length * i / 32;
                            sb.Append(BitConverter.ToString(BR.ReadBytes(4)));
                        }
                    }
                    item.parts = sb.ToString();
                }
                catch (Exception ex)
                {
                    item.parts = null;
                    unreadable_count++;
                    AddSkipped(item.fullpath, ex);
                }
            }
        }

        private void CalcHash(DublicateFileInfo item)
        {
            try
            {
                item.SHA1 = Functions.HashFile(item.fullpath, Hasher);
                item.file_id = Functions.GetFileId(item.fullpath);
            }
            catch (Exception ex)
            {
                item.SHA1 = null;
                unreadable_count++;
                AddSkipped(item.fullpath, ex);
            }
        }

        private void Sun_Click(object sender, RoutedEventArgs e)
        {
            CheckBox box = (CheckBox)sender;
            FilePath_ListView item = box.DataContext as FilePath_ListView;
            DublicateFiles_ListView group = GroupOf(item);
            if (group == null)
                return;
            if (box.IsChecked == true && group.Dublicats.All(x => x == item || x.IsChecked))
            {
                box.IsChecked = false;
                Messages.ShowMessage("At least one copy of each file must be kept.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Untick another copy first if you want to delete this one.");
                return;
            }
            item.IsChecked = box.IsChecked == true;
            Recompute();
        }

        private void Group_Click(object sender, RoutedEventArgs e)
        {
            DublicateFiles_ListView group = ((CheckBox)sender).DataContext as DublicateFiles_ListView;
            if (group == null)
                return;
            group.IsChecked = !group.Dublicats.Any(x => x.IsChecked);
            Recompute();
        }

        private void Item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedCopy() != null)
                SafetyTools.OpenItem(SelectedCopy().FullPath);
        }

        private void Item_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (SelectedCopy() != null)
                    SafetyTools.PreviewItem(SelectedCopy().FullPath);
            }
            else if (e.Key == Key.Enter)
                Item_MouseDoubleClick(null, null);
            else if (e.Key == Key.Delete && delete.IsEnabled)
                Delete_Click(delete, null);
        }

        static bool SameSize(string path, long size)
        {
            try
            {
                FileInfo file = new FileInfo(path);
                return file.Exists && file.Length == size && !Functions.IsLink(file);
            }
            catch (Exception) { return false; }
        }

        void RemoveMissing()
        {
            for (int i = dublicate_data.Count - 1; i > -1; i--)
            {
                dublicate_data[i].Dublicats.RemoveAll(x => !File.Exists(x.FullPath));
                if (dublicate_data[i].Dublicats.Count < 2)
                    dublicate_data.RemoveAt(i);
            }
            if (!dublicate_data.Any(x => x.Dublicats == item_viewer.DataContext as List<FilePath_ListView>))
                item_viewer.DataContext = null;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            bool permanent = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            List<FileSystemInfo> files = new List<FileSystemInfo>();
            List<string> changed = new List<string>();
            foreach (var i in dublicate_data)
            {
                List<FilePath_ListView> ticked = i.Dublicats.Where(x => x.IsChecked).ToList();
                if (ticked.Count == 0)
                    continue;
                if (!i.Dublicats.Any(x => !x.IsChecked && SameSize(x.FullPath, i.Size)))
                {
                    changed.Add(i.Dublicats[0].FullPath + "\n    (no unticked copy of this file exists any more with the same size, no copy of it is deleted)");
                    continue;
                }
                foreach (var j in ticked)
                    if (SameSize(j.FullPath, i.Size))
                        files.Add(new FileInfo(j.FullPath));
                    else
                        changed.Add(j.FullPath + "\n    (changed or missing since the search, not deleted)");
            }
            if (changed.Count > 0)
                Messages.ShowMessage(string.Format("{0} item{1} not deleted.", changed.Count, changed.Count > 1 ? "s are" : " is"), ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", changed.ToArray()));
            if (files.Count > 0)
            {
                if (Functions.ConfirmDelete(files, permanent))
                    Functions.DeleteItems(files, permanent, this);
            }
            else if (changed.Count == 0)
                Messages.ShowMessage("You have not selected any file to delete.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            RemoveMissing();
            Recompute();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (DublicateFiles_ListView i in dublicate_data)
                i.IsChecked = true;
            Recompute();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            progressbar.Value = 0;
            dublicate_data.Clear();
            item_viewer.DataContext = null;
            if (sender != null)
            {
                skipped_items.Clear();
                denied_count = 0;
                ShowInfoBar();
            }
            Recompute();
        }

        private void SortBy_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).ContextMenu.IsOpen = !((Button)sender).ContextMenu.IsOpen;
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (DublicateFiles_ListView i in dublicate_data)
                i.IsChecked = false;
            Recompute();
        }

        private void ItemGroupViewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (item_group_viewer.SelectedIndex > -1)
                item_viewer.DataContext = dublicate_data[item_group_viewer.SelectedIndex].Dublicats;
            item_viewer.Items.Refresh();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
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
            ((Label)sender).Background = Brushes.White;
            ((Label)sender).Foreground = Brushes.Black;
            CurrentPage = (DuplicateFileRemoverPages)tab_panal.Children.IndexOf((Label)sender) + 1;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!starting_window)
            {
                double t = (-1 * ((int)CurrentPage - 1)) * e.NewSize.Width;
                ThicknessAnimation ta = new ThicknessAnimation(new Thickness(t, 0, 0, 0), new Duration(new TimeSpan(0)));
                mother.BeginAnimation(MarginProperty, ta);
                for (int i = 0; i < mother.Children.Count; i++)
                {
                    ((Grid)mother.Children[i]).Width = ActualWidth - 5;
                    ((Grid)mother.Children[i]).Margin = new Thickness(i * e.NewSize.Width, 0, 0, 0);
                }
            }
        }

        #region Is Same Section

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            string file_name = "-";
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                file_name = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            else if (e.Data.GetDataPresent(DataFormats.Text))
                file_name = (string)e.Data.GetData(DataFormats.Text);
            if (file_name != "-" && File.Exists(file_name))
            {
                if (sender == f_grid)
                    FillFirstGrid(file_name);
                else
                    FillSecondGrid(file_name);
            }
        }

        private void FillFirstGrid(string p)
        {
            if (p != s_filepath.Text)
            {
                try
                {
                    FileInfo file = new FileInfo(p);
                    f_filename.Text = file.Name;
                    f_filepath.Text = file.FullName;
                    f_date.Text = "Date: " + string.Format("{0}/{1}/{2}  ", OrginizeDate(file.LastWriteTime.Day, 2), OrginizeDate(file.LastWriteTime.Month, 2), OrginizeDate(file.LastWriteTime.Year, 4)) + string.Format("{0}:{1}", OrginizeDate(file.LastWriteTime.Hour, 2), OrginizeDate(file.LastWriteTime.Minute, 2));
                    f_image.Source = Functions.GetFileImage256(file.Extension);
                    f_size.Text = "Size: " + Functions.FileSize(file.Length, 2);
                    f_type.Text = "Type: " + Functions.GetFileType(file.Extension) + (file.Extension.Length > 0 ? string.Format(" ({0})", file.Extension) : string.Empty);
                }
                catch (Exception ex)
                {
                    RestValue(f_grid);
                    f_filepath.Text = "-";
                    s_filepath.Text = "-";
                    Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                }
            }
            else
                Messages.ShowMessage("Files cant be same.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void FillSecondGrid(string p)
        {
            if (p != f_filepath.Text)
            {
                try
                {
                    FileInfo file = new FileInfo(p);
                    s_filename.Text = file.Name;
                    s_filepath.Text = file.FullName;
                    s_date.Text = "Date: " + string.Format("{0}/{1}/{2}  ", OrginizeDate(file.LastWriteTime.Day, 2), OrginizeDate(file.LastWriteTime.Month, 2), OrginizeDate(file.LastWriteTime.Year, 4)) + string.Format("{0}:{1}", OrginizeDate(file.LastWriteTime.Hour, 2), OrginizeDate(file.LastWriteTime.Minute, 2));
                    s_image.Source = Functions.GetFileImage256(file.Extension);
                    s_size.Text = "Size: " + Functions.FileSize(file.Length, 2);
                    s_type.Text = "Type: " + Functions.GetFileType(file.Extension) + (file.Extension.Length > 0 ? string.Format(" ({0})", file.Extension) : string.Empty);
                }
                catch (Exception ex)
                {
                    RestValue(s_grid);
                    f_filepath.Text = "-";
                    s_filepath.Text = "-";
                    Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                }
            }
            else
                Messages.ShowMessage("Files cant be same.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        string OrginizeDate(int number, int digits)
        {
            string N = number.ToString();
            while (N.Length < digits)
                N = "0" + N;
            return N;
        }

        private void RestValue(Grid grid)
        {
            if (grid == f_grid)
            {
                f_filename.Text = "First file";
                f_date.Text = "-";
                f_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/warning-grey.png"));
                f_size.Text = "-";
                f_type.Text = "-";
            }
            else if(grid == s_grid)
            {
                s_filename.Text = "Second file";
                s_date.Text = "-";
                s_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/warning-grey.png"));
                s_size.Text = "-";
                s_type.Text = "-";
            }
        }

        private void SortLists_Click(object sender, RoutedEventArgs e)
        {
            if (sender != ascrending_menu_item)
            {
                by_count_main_menu.IsChecked = false;
                by_type_main_menu.IsChecked = false;
                by_size_main_menu.IsChecked = false;
                by_total_size_main_menu.IsChecked = false;
                ((MenuItem)sender).IsChecked = true;
            }
            if (ascrending_menu_item.IsChecked)
            {
                if (sender == by_count_main_menu)
                    SortBy = DublicateSortBy.Count;
                else if (sender == by_type_main_menu)
                    SortBy = DublicateSortBy.Type;
                else if (sender == by_size_main_menu)
                    SortBy = DublicateSortBy.Size;
                else if (sender == by_total_size_main_menu)
                    SortBy = DublicateSortBy.TotalSize;
                else if (sender == ascrending_menu_item)
                    if ((int)SortBy > 4)
                        SortBy -= 4;
            }
            else
            {
                if (sender == by_count_main_menu)
                    SortBy = DublicateSortBy.DecCount;
                else if (sender == by_type_main_menu)
                    SortBy = DublicateSortBy.DecType;
                else if (sender == by_size_main_menu)
                    SortBy = DublicateSortBy.DecSize;
                else if (sender == by_total_size_main_menu)
                    SortBy = DublicateSortBy.DecTotalSize;
                else if (sender == ascrending_menu_item)
                    if ((int)SortBy < 5)
                        SortBy += 4;
            }
        }

        private void AboutTool_Click(object sender, RoutedEventArgs e)
        {
            //version 1:
            //find duplicate base
            //1.1:
            //improve speed and fixed bug
            //2:
            //redisgin the tools
            //imrove the interface
            //2.1:
            //resize list view (splitter in grid) 
            //2.2:
            //imrove tool speed
            //2.3:
            //add "is semiler to" tool
            //2.4:
            //bug fixed
            //
            AboutTool window = new AboutTool();
            window.tool_vertion.Text = "Version: 2.4";
            window.tool_date.Text = "";
            window.tool_name.Text = "Duplicate Files Remover";
            window.tool_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/dublicate-files-remover.png"));
            window.ShowDialog();
        }

        void UpdateCompareButton()
        {
            if (compare_button == null)
                return;
            bool ready = File.Exists(f_filepath.Text) && File.Exists(s_filepath.Text);
            SafetyTools.SetState(compare_button, ready && !comparing, comparing ? "Please wait, the files are being compared." : "Choose two files first (drag them here or use '...').");
        }

        private void FirstFilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCompareButton();
            if (!starting_window)
            {
                go_button_color.Color = Colors.Tomato;
                if (File.Exists(f_filepath.Text))
                {
                    if (f_filepath.Text.Equals(s_filepath.Text))
                    {
                        f_filepath.Text = "-";
                        return;
                    }
                    FillFirstGrid(f_filepath.Text);
                    if (File.Exists(s_filepath.Text))
                        go_button_color.Color = Colors.ForestGreen;
                }
                else
                    RestValue(f_grid);
            }
        }

        private void SecondFilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCompareButton();
            if (!starting_window)
            {
                go_button_color.Color = Colors.Tomato;
                if (File.Exists(s_filepath.Text))
                {
                    if (f_filepath.Text.Equals(s_filepath.Text))
                    {
                        s_filepath.Text = "-";
                        return;
                    }
                    FillSecondGrid(s_filepath.Text);
                    if (File.Exists(f_filepath.Text))
                        go_button_color.Color = Colors.ForestGreen;
                }
                else
                    RestValue(s_grid);
            }
        }

        private void Compare_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(f_filepath.Text) || !File.Exists(s_filepath.Text) || comparing)
            {
                UpdateCompareButton();
                return;
            }
            FileInfo first = new FileInfo(f_filepath.Text), second = new FileInfo(s_filepath.Text);
            if (string.Equals(first.FullName, second.FullName, StringComparison.OrdinalIgnoreCase))
            {
                Messages.ShowMessage("Choose two different files.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                return;
            }
            comparing = true;
            compare_button.Content = "comparing...";
            f_grid.IsEnabled = s_grid.IsEnabled = tab_panal.IsEnabled = false;
            UpdateCompareButton();
            string problem = null;
            bool equal = false, same_file = false;
            BackgroundWorker compare = new BackgroundWorker();
            compare.DoWork += (object dw_sender, DoWorkEventArgs dw_e) =>
            {
                DublicateFileInfo f = new DublicateFileInfo(first);
                DublicateFileInfo s = new DublicateFileInfo(second);
                string f_id = Functions.GetFileId(f.fullpath), s_id = Functions.GetFileId(s.fullpath);
                if (f_id != null && f_id == s_id)
                {
                    same_file = true;
                    return;
                }
                if (f.size != s.size)
                    return;
                ReadPartsOF(f);
                ReadPartsOF(s);
                if (f.parts == null || s.parts == null)
                {
                    problem = "We can not read one of the files.";
                    return;
                }
                if (f.parts != s.parts)
                    return;
                CalcHash(f);
                CalcHash(s);
                if (f.SHA1 == null || s.SHA1 == null)
                {
                    problem = "We can not read one of the files.";
                    return;
                }
                equal = f.SHA1 == s.SHA1;
            };
            compare.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
            {
                comparing = false;
                compare_button.Content = "compare";
                f_grid.IsEnabled = s_grid.IsEnabled = tab_panal.IsEnabled = true;
                UpdateCompareButton();
                if (CloseWindow)
                    return;
                if (bw_e.Error != null)
                    Messages.ShowMessage("We can not compare the files.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, bw_e.Error.Message);
                else if (problem != null)
                    Messages.ShowMessage(problem, ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
                else if (same_file)
                    Messages.ShowMessage("These two paths are the same file.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "One of them is a link or a hard link to the other, they are not two copies.");
                else if (equal)
                {
                    Messages.ShowMessage("The files are identical.", ModrenMessageButton.OK, ModrenMessageImage.Tick, ModrenMessageSound.Information, false, "");
                    if (!first.Extension.ToLower().Equals(second.Extension.ToLower()))
                        if (Messages.ShowMessage("Something wrong.", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "Files seem have deferent extention, do you want to run 'Super extention changer' to detemenate what is the corect extention?") == MyMessageBoxResult.Yes)
                            new Renamer().ShowWindow(new List<string>() { first.FullName, second.FullName }, "", ExtentionChanger.ExtentionSuggester, true);
                }
                else
                    Messages.ShowMessage("The files are not identical.", ModrenMessageButton.OK, ModrenMessageImage.Cross, ModrenMessageSound.Information, false, "");
            };
            compare.RunWorkerAsync();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.DialogTitle = string.Format("Select the {0} file to compare:", sender == f_browse ? "first" : "second");
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.MultiSelect = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                if (File.Exists(sfd.SelectedItem))
                {
                    if (sender == f_browse)
                        FillFirstGrid(sfd.SelectedItem);
                    else
                        FillSecondGrid(sfd.SelectedItem);
                }
            }
        }

        #endregion
    }

    class DublicateFileInfo
    {
        internal string fullpath;
        internal long size;
        internal FileInfo file;

        internal string SHA1;
        internal string parts;
        internal string file_id;

        public DublicateFileInfo() { }

        public DublicateFileInfo(FileInfo item)
        {
            file = item;
            fullpath = file.FullName;
            size = file.Length;
        }

        public int CompateTo(DublicateFileInfo x)
        {
            if (x == null) return -1;
            if (x.parts.SequenceEqual(parts))
                return 0;
            else return -1;
        }
    }

    class BasicFileInfo : IComparable
    {
        public string name;
        public string extention;
        public string fullpath;
        public long size;
        public string SHA1;

        public BasicFileInfo(FileInfo file)
        {
            name = file.Name;
            extention = file.Extension;
            fullpath = file.FullName;
            size = file.Length;
        }

        public int CompareTo(object obj)
        {
            long s = ((BasicFileInfo)obj).size;
            if (s > this.size)
                return 1;
            else if (s < this.size)
                return -1;
            else
                return 0;
        }

        internal void SetHash(byte[] p)
        {
            StringBuilder h = new StringBuilder();
            foreach (byte i in p)
                h.Append(i.ToString());
            SHA1 = h.ToString();
        }
    }

    class DublicateFiles_ListView : GeneralPurpose
    {
        long size = 0;

        public long Size
        {
            get { return size; }
            set
            {
                FileSize = Functions.FileSize(value, 2);
                size = value;
            }
        }


        public DublicateFiles_ListView()
        {
            Dublicats = new List<FilePath_ListView>();
            Image = Functions.GetFileImage24("directory");
        }

        public DublicateFiles_ListView(string file_type, long size, Point Tag)
        {
            Image = Functions.GetFileImage24("directory");
            FileType = file_type;
            this.size = size;
            FileSize = Functions.FileSize(size, 2);
            Dublicats = new List<FilePath_ListView>();
            Point = Tag;
        }

        public Boolean? IsChecked
        {
            get
            {
                int count = Dublicats.Count(x => x.IsChecked);
                if (count == 0)
                    return false;
                return count >= Dublicats.Count - 1 ? (bool?)true : null;
            }
            set
            {
                if (value.HasValue)
                {
                    foreach (var i in Dublicats)
                        i.IsChecked = value.Value;
                    if (value.Value && Dublicats.Count > 0)
                        Dublicats[0].IsChecked = false;
                }
            }
        }

        List<FilePath_ListView> dublicats;
        public List<FilePath_ListView> Dublicats
        {
            get { return dublicats; }
            set { dublicats = value; }
        }

        private String file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }


        private String total_file_size;
        public string TotalFileSize
        {
            get { return total_file_size; }
            set { total_file_size = value; }
        }

        private String file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }

        Point point;
        public Point Point
        {
            get { return point; }
            set { point = value; }
        }
    }

    class FilePath_ListView : GeneralPurpose
    {
        public FilePath_ListView(FileInfo item)
        {
            FullPath = item.FullName;
            File = item;
            IsChecked = false;
            Image = Functions.GetFileImage24(item.Extension);
        }

        bool is_checked;
        public bool IsChecked
        {
            get { return is_checked; }
            set { is_checked = value; }
        }
    }

    enum DuplicateFileRemoverPages
    {
        DuplicateFind = 1,
        IsSimeller = 2,
        WhatsDeferent = 3
    }

    enum DublicateSortBy
    {
        Count = 1,
        Type = 2,
        Size = 3,
        TotalSize = 4,
        DecCount = 5,
        DecType = 6,
        DecSize = 7,
        DecTotalSize = 8
    }
}