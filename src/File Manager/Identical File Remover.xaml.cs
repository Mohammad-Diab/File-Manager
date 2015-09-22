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
                    return x.FileType.CompareTo(y.FileType);
                case DublicateSortBy.DecType:
                    return y.FileType.CompareTo(x.FileType);

                default:
                    return 0;
            }
        }

        SHA1 Hasher = new SHA1Cng();
        //int files_count = 0;
        bool sun_bridle = true, bridle_father = true, show_message_again = true, CloseWindow = false, starting_window = true;
        double dublicate_size = 0;
        double deleted_size = 0;
        double total_size = 0;
        //List<BasicFileInfo> file_list;
        //List<string> sha1_string;
        //BackgroundWorker find_duplicate_BackgroundWorker;

        public Identical_File_Remover()
        {
            InitializeComponent();
        }

        public Identical_File_Remover(List<string> items)
        {
            InitializeComponent();
            FindDuplicate(items);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.Width = label1.ActualWidth;
            item_group_viewer.DataContext = dublicate_data;
            starting_window = false;
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
                paths.Add(i);
            ClearList_Click(null, null);
            progressbar.Visibility = Visibility.Visible;
            main_grid.IsEnabled = false;
            TaskbarItemInfo.ProgressValue = 0;

            BackgroundWorker find_duplicate = new BackgroundWorker();
            find_duplicate.WorkerReportsProgress = true;
            find_duplicate.DoWork += (object dw_sender, DoWorkEventArgs dw_e) =>
            {
                bool first_stage = true;
                Thread progress_thread = new Thread(() =>
                {
                    for (int i = 0; i < 126 && first_stage; i++)
                    {
                        if (CloseWindow) return;
                        if (!find_duplicate.IsBusy)
                            break;
                        else
                            try
                            { if (first_stage) find_duplicate.ReportProgress(0, 0.75); }
                            catch (Exception) { }
                        Thread.Sleep(100);
                        int sleep_time = i < 100 ? 100 : i < 120 ? 1000 : i < 126 ? 5000 : 10000;
                    }
                });
                List<DublicateFileInfo> files = new List<DublicateFileInfo>();
                foreach (string i in paths)
                {
                    if (File.Exists(i))
                    {
                        FileInfo tmp_file = new FileInfo(i);
                        files.Add(new DublicateFileInfo() { fullpath = tmp_file.FullName, size = tmp_file.Length, file = tmp_file });
                    }
                    else
                    {
                        try
                        { GetAllFiles(files, new DirectoryInfo(i)); }
                        catch (Exception) { }
                    }
                }
                first_stage = false;
                find_duplicate.ReportProgress(-1, 25);
                files.Sort((x, y) => x.size.CompareTo(y.size));
                int start_i = 0, start_k = 0;

                for (int i = 0; i < files.Count - 1; i++)
                {
                    find_duplicate.ReportProgress(0, 70.0 / files.Count);
                    start_i = i;
                    int same_size = 0;
                    if (files.Count > i + 1 && files[i].size == files[i + 1].size)
                    {
                        ReadPartsOF(files[i]);
                        same_size++;
                    }
                    else
                        continue;
                    int j;
                    for (j = i + 1; j < files.Count; j++)
                    {
                        if (files[i].size == files[j].size)
                        {
                            ReadPartsOF(files[j]);
                            same_size++;
                        }
                        else break;
                    }
                    if (same_size > 1)
                    {
                        List<DublicateFileInfo> small_list = files.GetRange(start_i, same_size);
                        small_list.Sort((x, y) => x.parts.CompareTo(y.parts));
                        for (int k = 0; k < small_list.Count - 1; k++)
                        {
                            start_k = k;
                            int duplicates = 0;
                            if (small_list.Count > k + 1 && small_list[k].parts == small_list[k + 1].parts)
                            {
                                CalcHash(small_list[k]);
                                duplicates++;
                            }
                            else
                                continue;
                            int l;
                            for (l = k + 1; l < small_list.Count; l++)
                            {
                                if (small_list[k].parts.Equals(small_list[l].parts))
                                {
                                    CalcHash(small_list[l]);
                                    duplicates++;
                                }
                                else break;
                            }
                            if (duplicates > 1)
                            {
                                List<DublicateFileInfo> micro_list = small_list.GetRange(start_k, duplicates);
                                micro_list.Sort((x, y) =>/* x.SHA1 == null ? -1 : y.SHA1 == null ? 1 : */x.SHA1.CompareTo(y.SHA1));
                                for (int m = 0; m < micro_list.Count - 1; m++)
                                {
                                    if (micro_list.Count > m + 1 && micro_list[m].SHA1 == micro_list[m + 1].SHA1)
                                    {
                                        find_duplicate.ReportProgress(1);
                                        find_duplicate.ReportProgress(2, micro_list[m].file);
                                    }
                                    else
                                        continue;
                                    int n;
                                    for (n = m + 1; n < micro_list.Count; n++)
                                        if (micro_list[m].SHA1.Equals(micro_list[n].SHA1))
                                            find_duplicate.ReportProgress(2, micro_list[n].file);
                                        else
                                            break;
                                    m = n - 1;
                                    find_duplicate.ReportProgress(3);
                                }
                            }
                            k = l - 1;// will be l in for loop
                            duplicates = 0;
                            //break;
                        }
                    }
                    i = j - 1;
                    same_size = 0;
                    //break;
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
                        dublicate_data[dublicate_data.Count - 1].FullPath = dublicate_data[dublicate_data.Count - 1].Dublicats.Count + (dublicate_data[dublicate_data.Count - 1].Dublicats.Count > 1 ? " dublicats " : " dublicat");
                        dublicate_data[dublicate_data.Count - 1].TotalFileSize = Functions.FileSize(dublicate_data[dublicate_data.Count - 1].Size * dublicate_data[dublicate_data.Count - 1].Dublicats.Count, 2);
                        break;
                }
            };

            find_duplicate.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
            {
                if (!CloseWindow)
                {
                    main_grid.IsEnabled = true;
                    SelectAll_Click(null, null);
                    if (dublicate_data.Count > 0)
                        delete.IsEnabled = select_all.IsEnabled = select_none.IsEnabled = clear_list.IsEnabled = sort_button.IsEnabled = true;
                    find_dublicate.IsEnabled = true;
                    items_label.Text = dublicate_data.Count > 1 ? "items" : "item";
                    number.Text = " " + dublicate_data.Count;
                    dublicate_data.Sort(Sorter);
                    item_group_viewer.Items.Refresh();
                    progressbar.Value = 100;
                    TaskbarItemInfo.ProgressValue = 100;
                    Messages.ShowMessage("Search done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, dublicate_data.Count > 0 ? string.Format("We found {0} item{1}", dublicate_data.Count, dublicate_data.Count > 1 ? "s" : "") : "Congratulations we did not find any dublicates here.");
                    TaskbarItemInfo.ProgressValue = 0;
                    progressbar.Visibility = Visibility.Hidden;
                    flow_shart.Visibility = Visibility.Visible;
                }
            };
            find_duplicate.RunWorkerAsync();
        }

        private void GetAllFiles(List<DublicateFileInfo> list_to_add, DirectoryInfo directory)
        {
            if (CloseWindow)
                return;
            try
            {
                var files = directory.GetFiles();
                foreach (var i in files)
                {
                    list_to_add.Add(new DublicateFileInfo() { fullpath = i.FullName, size = i.Length, file = i });
                    if (CloseWindow)
                        return;
                }
            }
            catch (Exception) { /*return;*/ }
            try
            {
                var sub_dirs = directory.GetDirectories();
                foreach (var i in sub_dirs)
                {
                    GetAllFiles(list_to_add, i);
                    if (CloseWindow)
                        return;
                }
            }
            catch (Exception) { }
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
                    BinaryReader BR = new BinaryReader(item.file.OpenRead());

                    for (int i = 0; i < 32; i++)
                    {
                        BR.BaseStream.Position = BR.BaseStream.Length * i / 32;
                        sb.Append(Encoding.UTF8.GetString(BR.ReadBytes(4)));
                    }
                }
                catch (Exception) { };
                item.parts = sb.ToString();
            }
        }

        private void CalcHash(DublicateFileInfo item)
        {
            try
            { item.SHA1 = System.Text.Encoding.UTF8.GetString(Hasher.ComputeHash(File.OpenRead(item.fullpath))); }
            catch (Exception) { }
        }

        private void Sun_Unchecked(object sender, RoutedEventArgs e)
        {
            if (bridle_father)
                sun_bridle = false;
            CheckAllSun(dublicate_data[item_group_viewer.SelectedIndex], -1);
            sun_bridle = true;
        }

        private void Sun_Checked(object sender, RoutedEventArgs e)
        {
            if (bridle_father)
                sun_bridle = false;
            if (dublicate_data[item_group_viewer.SelectedIndex].cheched_count == dublicate_data[item_group_viewer.SelectedIndex].Dublicats.Count - 1)
                if (show_message_again)
                {
                    if (Messages.ShowMessage("You are about to remove all copies of this file, do you want to do that?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "", out show_message_again) == MyMessageBoxResult.Yes)
                        CheckAllSun(dublicate_data[item_group_viewer.SelectedIndex], 1);
                    else
                    {
                        sun_bridle = true;
                        bridle_father = false;
                        (sender as CheckBox).IsChecked = false;
                        bridle_father = true;
                    }
                }
                else
                    CheckAllSun(dublicate_data[item_group_viewer.SelectedIndex], 1);
            else
                CheckAllSun(dublicate_data[item_group_viewer.SelectedIndex], 1);
            sun_bridle = true;
        }

        private void CheckAllSun(DublicateFiles_ListView d, int dif)
        {
            if (!sun_bridle)
            {
                dublicate_data[item_group_viewer.SelectedIndex].cheched_count += dif;
                deleted_size += dif * dublicate_data[item_group_viewer.SelectedIndex].Size;
                deleted_grid.Width = (deleted_size / total_size) * total.ActualWidth + 4;
                deleted_text.Text = Functions.FileSize(deleted_size, 2) + " will be deleted";
                int count = 0;
                foreach (var i in d.Dublicats)
                    if (i.IsChecked) count++;
                if (count == 0)
                    d.ChangeIsCheakedValue(false);
                else if (count == d.Dublicats.Count)
                    d.ChangeIsCheakedValue(true);
                else
                    d.ChangeIsCheakedValue(null);
                item_group_viewer.Items.Refresh();
                item_viewer.Items.Refresh();
            }
        }

        private void Item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (item_viewer.SelectedIndex > -1)
                try { System.Diagnostics.Process.Start(dublicate_data[item_group_viewer.SelectedIndex].Dublicats[item_viewer.SelectedIndex].FullPath); }
                catch (Exception ex) { Messages.ShowMessage("Some went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void Item_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (File.Exists(dublicate_data[item_group_viewer.SelectedIndex].Dublicats[item_viewer.SelectedIndex].FullPath))
                    new Preview().ShowDialog(new FileInfo(dublicate_data[item_group_viewer.SelectedIndex].Dublicats[item_viewer.SelectedIndex].FullPath), ItemType.File);
                if (MainWindow.GoToPath)
                {
                    MainWindow.GoToPath = false;
                    new MainWindow(MainWindow.to_go_path).ShowDialog();
                }
            }
            else if (e.Key == Key.Enter)
                Item_MouseDoubleClick(null, null);
            else if (e.Key == Key.Delete)
                Delete_Click(delete, null);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            List<FileOperationItem> files = new List<FileOperationItem>();
            StringBuilder items_name = new StringBuilder();
            foreach (var i in dublicate_data)
                foreach (var j in i.Dublicats)
                    if (j.IsChecked)
                    {
                        files.Add(new FileOperationItem(new FileInfo(j.FullPath), false, ""));
                        items_name.AppendLine(j.FullPath);
                    }
            if (files.Count > 0)
            {
                bool delete = false;
                if (files.Count > 1)
                {
                    if (Messages.ShowMessage(string.Format("Are you sure you want to permanently delete these {0} items", files.Count), ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, items_name.ToString()) == MyMessageBoxResult.Yes)
                        delete = true;
                }
                else if (files.Count == 1)
                    if (Messages.ShowMessage("Are you sure you want to permanently delete these item", ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, Messages.GetItemInfo(files[0].Item)) == MyMessageBoxResult.Yes)
                        delete = true;
                if (delete)
                {
                    new Operation().CopyDialogShow(files, OperationType.Delete, "", true);
                    for (int i = dublicate_data.Count - 1; i > -1; i--)
                    {
                        for (int j = dublicate_data[i].Dublicats.Count - 1; j > -1; j--)
                            if (dublicate_data[i].Dublicats[j].IsChecked && !File.Exists(dublicate_data[i].Dublicats[j].FullPath))
                            { dublicate_data[i].Dublicats.RemoveAt(j); dublicate_data[i].cheched_count--; }
                        if (dublicate_data[i].Dublicats.Count < 2)
                            dublicate_data.RemoveAt(i);
                        else
                            dublicate_data[i].FullPath = dublicate_data[i].Dublicats.Count + " dublicats ";
                    }
                    number.Text = dublicate_data.Count + " ";
                    items_label.Text = dublicate_data.Count > 1 ? "items" : "item";
                    Messages.ShowMessage("Files deleted done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                }
            }
            else
                Messages.ShowMessage("You have not selected any file to delete.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            dublicate_size = 0;
            deleted_size = 0;
            total_size = 0;
            foreach (DublicateFiles_ListView i in dublicate_data)
            {
                i.IsChecked = true;
                total_size += (i.Size * i.Dublicats.Count);
                dublicate_size += (i.Size * (i.Dublicats.Count - 1));
            }
            item_viewer.Items.Refresh();
            item_group_viewer.Items.Refresh();
            deleted_size = dublicate_size;
            duplicate.Width = (dublicate_size / total_size) * total.ActualWidth - 2;
            deleted_grid.Width = (deleted_size / total_size) * total.ActualWidth + 4;
            deleted_text.Text = Functions.FileSize(deleted_size, 2) + " will be deleted";
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            progressbar.Value = 0;
            delete.IsEnabled = select_all.IsEnabled = select_none.IsEnabled = clear_list.IsEnabled = sort_button.IsEnabled = false;
            flow_shart.Visibility = Visibility.Hidden;
            dublicate_data.Clear();
            item_viewer.DataContext = null;
        }

        private void SortBy_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).ContextMenu.IsOpen = !((Button)sender).ContextMenu.IsOpen;
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (DublicateFiles_ListView i in dublicate_data)
                i.IsChecked = false;
            item_group_viewer.Items.Refresh();
            item_viewer.Items.Refresh();
            deleted_grid.Width = 4;
            deleted_text.Text = "0 byte will be deleted";
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
                    f_date.Text = "Date: " + string.Format("{0}/{1}/{2}  ", OrginizeDate(file.LastWriteTimeUtc.Day, 2), OrginizeDate(file.LastWriteTimeUtc.Month, 2), OrginizeDate(file.LastWriteTimeUtc.Year, 4)) + string.Format("{0}:{1}", OrginizeDate(file.LastWriteTimeUtc.Hour, 2), OrginizeDate(file.LastWriteTimeUtc.Minute, 2));
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
                    s_date.Text = "Date: " + string.Format("{0}/{1}/{2}  ", OrginizeDate(file.LastWriteTimeUtc.Day, 2), OrginizeDate(file.LastWriteTimeUtc.Month, 2), OrginizeDate(file.LastWriteTimeUtc.Year, 4)) + string.Format("{0}:{1}", OrginizeDate(file.LastWriteTimeUtc.Hour, 2), OrginizeDate(file.LastWriteTimeUtc.Minute, 2));
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
                    if ((int)SortBy < 5)
                        SortBy += 4;
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
                    if ((int)SortBy > 4)
                        SortBy -= 4;
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
        }

        private void FirstFilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
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
            if (File.Exists(f_filepath.Text))
            {
                if (File.Exists(s_filepath.Text))
                {
                    main_grid.IsEnabled = false;
                    DublicateFileInfo f = new DublicateFileInfo(new FileInfo(f_filepath.Text));
                    DublicateFileInfo s = new DublicateFileInfo(new FileInfo(s_filepath.Text));
                    if (f.size == s.size)
                    {
                        ReadPartsOF(f);
                        ReadPartsOF(s);
                        if (f.parts.Equals(s.parts))
                        {
                            CalcHash(f);
                            CalcHash(s);
                            if (f.SHA1.Equals(s.SHA1))
                            {
                                Messages.ShowMessage("Files is equal.", ModrenMessageButton.OK, ModrenMessageImage.Tick, ModrenMessageSound.Information, false, "");
                                main_grid.IsEnabled = true;
                                if (!f.file.Extension.ToLower().Equals(s.file.Extension.ToLower()))
                                    if (Messages.ShowMessage("Something wrong.", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "Files seem have deferent extention, do you want to run 'Super extention changer' to detemenate what is the corect extention?") == MyMessageBoxResult.Yes)
                                    {
                                        List<string> files = new List<string>();
                                        files.Add(f.file.FullName);
                                        files.Add(s.file.FullName);
                                        new Renamer().ShowWindow(files, "", ExtentionChanger.ExtentionSuggester, true);
                                    }
                                main_grid.IsEnabled = true;
                                return;
                            }
                        }
                    }
                    main_grid.IsEnabled = true;
                    Messages.ShowMessage("Files is not equal.", ModrenMessageButton.OK, ModrenMessageImage.Cross, ModrenMessageSound.Information, false, "");
                }
                else if (s_filepath.Text != "-" && s_filepath.Text != "")
                        Messages.ShowMessage("File is not exist", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Format("the file '{0}' is not exist", s_filepath.Text));
            }
            else if (f_filepath.Text != "-" && f_filepath.Text != "")
                Messages.ShowMessage("File is not exist", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Format("the file '{0}' is not exist", f_filepath.Text));
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
        internal int cheched_count = 0;
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

        private bool? is_cheched;
        public Boolean? IsChecked
        {
            get { return is_cheched; }
            set
            {
                if (value.HasValue)
                {
                    foreach (var i in Dublicats)
                        i.IsChecked = value.Value;
                    if (value.Value)
                        Dublicats[0].IsChecked = false;
                    cheched_count = Dublicats.Count - 1;
                }
                if (value.Value)
                    is_cheched = null;
                else
                    is_cheched = value;
                //refresh list view;
            }
        }

        public void ChangeIsCheakedValue(bool? value)
        {
            is_cheched = value;
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