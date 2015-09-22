using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Collections.ObjectModel;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for SelectFileDialog.xaml
    /// </summary>
    public partial class SelectFileDialog : Window
    {//**** save dialog set file name
        internal SelectFileDialogMode dialogMode = SelectFileDialogMode.OpenFile;
        internal bool CreateFolderEnable = true;
        internal bool OpenAsReadOnlyEnable = false;
        internal bool MultiSelect = false;
        internal List<QuickAccessMenuItem> QuickAccessMenuItems = new List<QuickAccessMenuItem>();
        internal List<SelectFileFilter> FilterMenu = new List<SelectFileFilter>();
        internal string DialogTitle;
        internal string InitialDirectory;
        internal object[] OkAndCancelContent;
        static string LastSelectedPath = "root";
        SortItemsBy sort_by = SortItemsBy.Name;
        static SortItemsBy GeneralSortBy;
        internal string SelectedItem;
        internal List<string> SelectedItems = new List<string>();
        internal bool AddExtension = true;
        internal bool CheckFileExists = true;
        Boolean use_multiselection_list = false, Initializing = true, ShowHiddenFiles = true;
        MyMessageBoxResult return_value = MyMessageBoxResult.Cancel;

        List<ExplorerItem> explorer_data = new List<ExplorerItem>();
        ObservableCollection<MultiSelectionItem> multiselection_data = new ObservableCollection<MultiSelectionItem>();
        string current_path = "root";
        string CurrentPath
        {
            get { return current_path; }
            set
            {
                if (value == "root")
                    Up_Button.IsEnabled = false;
                else
                    Up_Button.IsEnabled = true;
                current_path = value;
            }
        }

        public SelectFileDialog()
        {
            InitializeComponent();
            DialogTitle = "Open File";
            InitialDirectory = "root";
        }

        internal MyMessageBoxResult ShowWindow()
        {
            if (OkAndCancelContent == null)
                OK_Button.Content = dialogMode == SelectFileDialogMode.OpenFile ? "Open" : dialogMode == SelectFileDialogMode.SaveFile ? "Save" : "Select";
            else
            {
                OK_Button.Content = OkAndCancelContent[0];
                Cancel_Button.Content = OkAndCancelContent[1];
            }
            CreateFolder_Button.IsEnabled = CreateFolderEnable;
            ReadOnly_CheckBox.IsEnabled = OpenAsReadOnlyEnable && dialogMode == SelectFileDialogMode.OpenFile;
            Explorer.SelectionMode = MultiSelect ? SelectionMode.Extended : SelectionMode.Single;
            ShowSelectedList_Button.Visibility = MultiSelect ? Visibility.Visible : Visibility.Hidden;
            Title = DialogTitle != null && DialogTitle != string.Empty ? DialogTitle : Title;
            if (dialogMode == SelectFileDialogMode.SelectDirectory)
                Filter_ComboBox.Items.Add(new ComboBoxItem() { Content = "Directory", Tag = "directory" });
            
            else if (FilterMenu.Count == 0)
                Filter_ComboBox.Items.Add(new ComboBoxItem() { Content = "All files", Tag = "" });

            else
                foreach (var i in FilterMenu)
                    Filter_ComboBox.Items.Add(new ComboBoxItem() { Content = i.display_name, Tag = i.extentions });
            Filter_ComboBox.SelectedIndex = 0;
            if (InitialDirectory != null && InitialDirectory != string.Empty)
                CurrentPath = InitialDirectory;
            else
                CurrentPath = LastSelectedPath;
            Initializing = false;
            ShowDialog();
            try
            {
                if (MultiSelect)
                    if (dialogMode == SelectFileDialogMode.SelectDirectory)
                        LastSelectedPath = SelectedItems[SelectedItems.Count - 1];
                    else
                        LastSelectedPath = new FileInfo(SelectedItems[SelectedItems.Count - 1]).Directory.FullName;
                else
                    if (dialogMode == SelectFileDialogMode.SelectDirectory)
                        LastSelectedPath = SelectedItem;
                    else
                        LastSelectedPath = new FileInfo(SelectedItem).Directory.FullName;
            }
            catch (Exception) { }
            return return_value;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            #region Startup Movement
            Rotation3d.Angle = 90;
            BackgroundWorker rotate_3d = new BackgroundWorker();
            rotate_3d.WorkerReportsProgress = true;
            rotate_3d.DoWork += (object senderw, DoWorkEventArgs ew) =>
            {
                for (int i = 0; i < 50; i ++)
                {
                    rotate_3d.ReportProgress(i);
                    Thread.Sleep(2);
                }
            };
            rotate_3d.ProgressChanged += (object senderw, ProgressChangedEventArgs ew) => { Rotation3d.Angle -= 1.8; };
            rotate_3d.RunWorkerCompleted += (object senderw, RunWorkerCompletedEventArgs ew) =>
            {
                Rotation3d.Angle = 0;
                FistAnimation_Grid.Visibility = Visibility.Collapsed;
                containt_grid.Visibility = Visibility.Visible;
            };
            rotate_3d.RunWorkerAsync();
            #endregion Startup Movement

            InitializeData();
            MultiSelection_List.DataContext = multiselection_data;
            if (CurrentPath.Equals("root"))
                RootView();
            else
                RootItemBrowse("root", CurrentPath, "");//if failure show root
        }

        void InitializeData()
        {
            bool worker_isbusy = true;
            BackgroundWorker get_drivers = new BackgroundWorker();
            get_drivers.WorkerSupportsCancellation = true;
            DriveInfo[] drivers = new DriveInfo[0];
            get_drivers.DoWork += (object senderw, DoWorkEventArgs ew) => 
            {
                drivers = DriveInfo.GetDrives();
                while (worker_isbusy)
                    Thread.Sleep(50);
            };
            get_drivers.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                for (int i = drivers.Length - 1; i > -1; i--)
                    QuickAccess_List.Items.Insert(0, CreateQuickAccessItem(Functions.GetDriveImage(drivers[i], 24), 24, drivers[i].IsReady ? ((drivers[i].VolumeLabel.Equals(string.Empty) ? "Local disk" : drivers[i].VolumeLabel) + " (" + drivers[i].Name + ")") : drivers[i].Name, drivers[i], drivers[i].Name));
                QuickAccess_List.Items.Insert(0, CreateQuickAccessItem("Local disks"));
            };
            get_drivers.RunWorkerAsync();

            string[] user_folders = { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Functions.GetDownloadsPath(),Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) };
            string[] user_folders_images = { "user", "desktop", "downloads", "documents", "music", "pictures", "videos" };
            QuickAccess_List.Items.Add(CreateQuickAccessItem("User directories"));
            for (int i = 0; i < user_folders.Length; i++)
            {
                if (Directory.Exists(user_folders[i]))
                {
                    DirectoryInfo dir = new DirectoryInfo(user_folders[i]);
                    QuickAccess_List.Items.Add(CreateQuickAccessItem(Functions.GetDirectoryImage(user_folders_images[i]), 24, dir.Name, dir, dir.FullName));
                }
            }

            //if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)))
            //    quick_access_data.Add(new QuickAccessMenuItem() { FullPath = Path.GetFileName(tmp_path), Directory = new DirectoryInfo(tmp_path), Image = Functions.GetSpecialDirectoryImage("desktop"), isDrive = true });
            //if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)))
            //    quick_access_data.Add(new QuickAccessMenuItem() { FullPath = Path.GetFileName(tmp_path), Directory = new DirectoryInfo(tmp_path), Image = Functions.GetSpecialDirectoryImage("document"), isDrive = true });
            //if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic)))
            //    quick_access_data.Add(new QuickAccessMenuItem() { FullPath = Path.GetFileName(tmp_path), Directory = new DirectoryInfo(tmp_path), Image = Functions.GetSpecialDirectoryImage("music"), isDrive = true });
            //if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures)))
            //    quick_access_data.Add(new QuickAccessMenuItem() { FullPath = Path.GetFileName(tmp_path), Directory = new DirectoryInfo(tmp_path), Image = Functions.GetSpecialDirectoryImage("picture"), isDrive = true });
            //if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos)))
            //    quick_access_data.Add(new QuickAccessMenuItem() { FullPath = Path.GetFileName(tmp_path), Directory = new DirectoryInfo(tmp_path), Image = Functions.GetSpecialDirectoryImage("video"), isDrive = true });

            string[] special_folders = { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) , Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.Fonts),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Environment.GetFolderPath(Environment.SpecialFolder.History)};
            string[] special_folders_image = { "programfile", "programfile", "fonts", "directory", "history" };
            QuickAccess_List.Items.Add(CreateQuickAccessItem("System directories"));

            bool k = false;
            for (int i = 0; i < special_folders.Length; i++)
            {
                if (Directory.Exists(special_folders[i]))
                {
                    if (!k && (i != special_folders.Length - 1) && (special_folders[i].ToLower().Equals(special_folders[i + 1].ToLower())))
                    {
                        k = true;
                        continue;
                    }
                    DirectoryInfo dir = new DirectoryInfo(special_folders[i]);
                    QuickAccess_List.Items.Add(CreateQuickAccessItem(Functions.GetDirectoryImage(user_folders_images[i]), 24, dir.Name, dir, dir.FullName));
                }
                worker_isbusy = false;
            }
            //Network
        }

        private ListBoxItem CreateQuickAccessItem(BitmapImage picture_source, double picture_length, string header, object tag, string string_tag)
        {
            Image image = new Image();
            image.Source = picture_source;
            image.Width = image.Height = picture_length;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.HorizontalAlignment = HorizontalAlignment.Left;

            TextBlock text = new TextBlock();
            text.Text = header;
            text.Margin = new Thickness(5, 2, 0, 0);
            text.VerticalAlignment = VerticalAlignment.Top;
            text.HorizontalAlignment = HorizontalAlignment.Left;
            text.FontSize = picture_length / 2 + 2;
            text.Foreground = Brushes.WhiteSmoke;

            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            stack.Children.Add(image);
            stack.Children.Add(text);

            ListBoxItem item = new ListBoxItem();
            item.Tag = tag;
            item.ToolTip = item.ContentStringFormat = string_tag;
            item.Content = stack;
            item.MouseUp += (object sender, MouseButtonEventArgs e) =>
            {
                if (CurrentPath.ToLower() != ((ListBoxItem)sender).ContentStringFormat.ToLower())
                    RootItemBrowse(CurrentPath, ((ListBoxItem)sender).ContentStringFormat, "");
            };
            return item;
        }

        private ListBoxItem CreateQuickAccessItem(string title)
        {
            ListBoxItem item = new ListBoxItem();
            item.FontSize = 12;
            item.Margin = new Thickness(5, 0, 0, 0);
            item.IsEnabled = false;
            item.Content = title;
            return item;
        }

        #region Exploreing

        //cut copy drug and drop preview

        private void RootView()
        {
            RootPage.Visibility = Visibility.Visible;
            CurrentPath = address.Text = "root";
            List<RootItem> items = new List<RootItem>();
            BackgroundWorker get_drivers = new BackgroundWorker();
            get_drivers.WorkerSupportsCancellation = true;
            DriveInfo[] drivers = new DriveInfo[0];
            get_drivers.DoWork += (object senderw, DoWorkEventArgs ew) => { drivers = DriveInfo.GetDrives(); };
            get_drivers.RunWorkerCompleted += (object senderw, RunWorkerCompletedEventArgs ew) =>
            {
                foreach (DriveInfo i in drivers)
                    items.Add(new RootItem(i));
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(RootPage.ItemsSource);
                PropertyGroupDescription groupDescription = new PropertyGroupDescription("Header");
                view.GroupDescriptions.Add(groupDescription);
            };
            string tmp_path;
            RootPage.ItemsSource = items;
            get_drivers.RunWorkerAsync();
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "User directories", "user"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "User directories", "desktop"));
            if (Directory.Exists(tmp_path = Functions.GetDownloadsPath()))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "User directories", "downloads"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "User directories", "documents"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "User directories", "music"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "User directories", "pictures"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "User directories", "videos"));

            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "Public directories", "desktop"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "Public directories", "documents"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonMusic)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "Public directories", "music"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "Public directories", "pictures"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.CommonVideos)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "Public directories", "videos"));


            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "System directories", "programfile"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) && !Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86).Equals(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "System directories", "programfile"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.Fonts)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "System directories", "fonts"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "System directories", "directory"));
            if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.History)))
                items.Add(new RootItem(new DirectoryInfo(tmp_path), "System directories", "history"));
        }

        private void RootItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                RootItemBrowse("root", (RootPage.SelectedItem as RootItem).toolTip, "");
        }

        void RootItemBrowse(string old_path, string new_path, string filter_text)
        {
            auto_changed = true;
            Filter_TextBox.Text = filter_text;
            auto_changed = false;
            selectedItemName_TextBox.Text = "";
            if (!IsSpecialDirictory(new_path))
            {
                if (new_path.Length > 0 && new_path[new_path.Length - 1] != '\\')
                    new_path = new_path + "\\";
                bool WorkerHasFailed = false;
                List<DirectoryInfo> sub_directories = new List<DirectoryInfo>();
                List<FileInfo> files = new List<FileInfo>();
                string error_message = string.Empty, error_message_info = string.Empty;
                BackgroundWorker GetFilesAndDirectory = new BackgroundWorker();
                GetFilesAndDirectory.WorkerReportsProgress = true;
                GetFilesAndDirectory.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    if (e.ProgressPercentage < 0)
                    {
                        Messages.ShowMessage(error_message, ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, error_message_info != string.Empty  , error_message_info);
                    }
                };
                GetFilesAndDirectory.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    if (Directory.Exists(new_path))
                    {
                        DirectoryInfo Current_Directory = new DirectoryInfo(Path.GetFullPath(new_path));
                        try
                        {
                            sub_directories = Current_Directory.GetDirectories().ToList();
                            sub_directories.Sort(DirectorySorter);
                            if (dialogMode != SelectFileDialogMode.SelectDirectory)
                                files = Current_Directory.GetFiles().ToList();
                            files.Sort(FilesSorter);
                        }
                        catch (Exception ex)
                        {
                            error_message = "We can not get  '" + new_path + "',  content, browsing has been canceled";
                            error_message_info = ex.Message;
                            GetFilesAndDirectory.ReportProgress(-1);
                            WorkerHasFailed = true;
                        }
                    }
                    else
                    {
                        error_message = "We can not find directory '" + new_path + "', browsing has been canceled.";
                        error_message_info = string.Empty;
                        GetFilesAndDirectory.ReportProgress(-1);
                        WorkerHasFailed = true;
                    }
                };

                GetFilesAndDirectory.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    if (!WorkerHasFailed)
                    {
                        //Explorer.DataContext = explorer_data;
                        //explorer_data.Clear();
                        //RootPage.Visibility = Visibility.Hidden;
                        //for (int i = 0; i < sub_directories.Count; i++)
                        //    if (ShowHiddenFiles || (!ShowHiddenFiles && !sub_directories[i].Attributes.HasFlag(FileAttributes.Hidden)))
                        //        if (sub_directories[i].Name.ToLower().Contains(filter_items))
                        //            explorer_data.Add(new ExplorerItem() { isDirectory = true, FullPath = sub_directories[i].Name, Directory = sub_directories[i], Image = Functions.GetFileImage24("directory"), Date = string.Format("{0}/{1}/{2}  ", OrginizeDate(sub_directories[i].LastWriteTimeUtc.Day, 2), OrginizeDate(sub_directories[i].LastWriteTimeUtc.Month, 2), OrginizeDate(sub_directories[i].LastWriteTimeUtc.Year, 4)) + string.Format("{0}:{1}", OrginizeDate(sub_directories[i].LastWriteTimeUtc.Hour, 2), OrginizeDate(sub_directories[i].LastWriteTimeUtc.Minute, 2)), isChecked = false, Size = "", Type = "Directory", ToolTip = sub_directories[i].FullName });

                        //string[] filters = (Filter_ComboBox.SelectedItem as ComboBoxItem).Tag as string[];
                        //foreach (var i in files)
                        //    if (filters.Contains(i.Extension.ToLower()))
                        //        if (ShowHiddenFiles || (!ShowHiddenFiles && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                        //            if (i.Name.ToLower().Contains(filter_items))
                        //                explorer_data.Add(new ExplorerItem() { isDirectory = false, FullPath = i.Name, File = i, Image = Functions.GetFileImage24(i.Extension), Date = string.Format("{0}/{1}/{2}  ", OrginizeDate(i.LastWriteTimeUtc.Day, 2), OrginizeDate(i.LastWriteTimeUtc.Month, 2), OrginizeDate(i.LastWriteTimeUtc.Year, 4)) + string.Format("{0}:{1}", OrginizeDate(i.LastWriteTimeUtc.Hour, 2), OrginizeDate(i.LastWriteTimeUtc.Minute, 2)), Type = Functions.GetFileType(i.Extension), ToolTip = i.FullName });
                        //CurrentPath = new_path;
                        //if (Explorer.Items.Count > 0)
                        //    Explorer.ScrollIntoView(Explorer.Items[0]);
                        //if (explorer_data.Count == 0)
                        //    NoFiles_TextBlock.Visibility = Visibility.Visible;
                        //else
                        //    NoFiles_TextBlock.Visibility = Visibility.Hidden;

                        FillListWithItems(sub_directories, files, filter_text);
                        CurrentPath = new_path;
                    }
                    //else
                    //    new ModrenMessageBox().ShowMessage("error while open '" + new_path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    Explorer.IsEnabled = true;
                    address.Text = CurrentPath;
                };
                Explorer.IsEnabled = false;
                GetFilesAndDirectory.RunWorkerAsync();
            }
            else
            {
                new_path = new_path.ToLower();
                if (new_path.Equals("root"))
                    RootView();
                else
                {
                    RootItemBrowse(old_path, GetSpecialDirictoryPath(new_path), filter_text);
                }
            }
        }

        void FillListWithItems(List<DirectoryInfo> sub_directories, List<FileInfo> files, string filter_string)
        {
            Explorer.DataContext = explorer_data;
            explorer_data.Clear();
            RootPage.Visibility = Visibility.Hidden;
            foreach (var i in sub_directories)
                if (ShowHiddenFiles || (!ShowHiddenFiles && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                    if (i.Name.ToLower().Contains(filter_string))
                        explorer_data.Add(new ExplorerItem() { isDirectory = true, FullPath = i.Name, Directory = i, Image = Functions.GetFileImage24("directory"), Date = Functions.OrginizeDate(i.LastWriteTimeUtc), IsSelected = false, Size = "", Type = "Directory", ToolTip = i.FullName });

            string[] filters = (Filter_ComboBox.SelectedItem as ComboBoxItem).Tag as string[];
            bool all_files = (Filter_ComboBox.SelectedItem as ComboBoxItem).Tag is string;
            foreach (var i in files)
                if (all_files || filters.Contains(i.Extension.ToLower()))
                    if (ShowHiddenFiles || (!ShowHiddenFiles && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                        if (i.Name.ToLower().Contains(filter_string))
                            explorer_data.Add(new ExplorerItem() { isDirectory = false, FullPath = i.Name, File = i, Image = Functions.GetFileImage24(i.Extension), Size = Functions.FileSize(i.Length, 2), Date = Functions.OrginizeDate(i.LastWriteTimeUtc), Type = Functions.GetFileType(i.Extension), ToolTip = i.FullName });
            if (Explorer.Items.Count > 0)
                Explorer.ScrollIntoView(Explorer.Items[0]);
            Explorer.Items.Refresh();
            if (explorer_data.Count == 0)
                NoFiles_TextBlock.Visibility = Visibility.Visible;
            else
                NoFiles_TextBlock.Visibility = Visibility.Hidden;
        }

        string GetSpecialDirictoryPath(string new_path)
        {
            if (new_path.Equals(Environment.UserName.ToLower()))
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            switch (new_path)
            {
                case "root":
                    return "root";
                case "desktop":
                    return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                case "downloads":
                    return Functions.GetDownloadsPath();
                case "music":
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                case "pictures":
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                case "videos":
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                case "program files":
                    return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                case "program files (x86)":
                    return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                case "fonts":
                    return Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
                case "local":
                    return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                case "history":
                    return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            }
            return "root";
        }

        bool IsSpecialDirictory(string new_path)
        {
            string[] SpecialDirectories = { "root", Environment.UserName.ToLower(), "desktop", "downloads", "documents", "music", "pictures", "videos", "program files", "program files (x86)", "fonts", "local", "history" };
            foreach (var i in SpecialDirectories)
                if (i.Equals(new_path))
                    return true;
            return false;
        }

        int FilesSorter(FileInfo x, FileInfo y)
        {
            switch(sort_by)
            {
                case SortItemsBy.Name:
                    return x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.Size:
                    return x.Length.CompareTo(y.Length);
                case SortItemsBy.Type:
                    return x.Extension.ToLower().CompareTo(y.Extension.ToLower());
                case SortItemsBy.DateModified:
                    return x.LastWriteTimeUtc.Ticks.CompareTo(y.LastWriteTimeUtc.Ticks);
            }
            return 0;
        }

        int DirectorySorter(DirectoryInfo x, DirectoryInfo y)
        {
            switch (sort_by)
            {
                case SortItemsBy.Name:
                    return x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.Size:
                    return x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.Type:
                    return x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.DateModified:
                    return x.LastWriteTimeUtc.Ticks.CompareTo(y.LastWriteTimeUtc.Ticks);
            }
            return 0;
        }

        private void ExplorerItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                if ((Explorer.SelectedItem as ExplorerItem).isDirectory)
                    RootItemBrowse(CurrentPath, (Explorer.SelectedItem as ExplorerItem).ToolTip, "");
                else if (dialogMode.Equals(SelectFileDialogMode.OpenFile) || dialogMode.Equals(SelectFileDialogMode.SaveFile))
                    FinishThis(Explorer.SelectedItem as ExplorerItem);
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CurrentPath.Equals("root"))
            {
                string up_path = "root";
                try { up_path = new DirectoryInfo(current_path).Parent.FullName; }
                catch (Exception) { }
                if (!Path.GetPathRoot(CurrentPath).Equals(CurrentPath))
                    RootItemBrowse(CurrentPath, up_path, "");
                else
                    RootItemBrowse(CurrentPath, "root", "");
            }
        }

        private void GoHomeButton_Click(object sender, RoutedEventArgs e)
        {
            RootItemBrowse(CurrentPath, "root", "");
        }

        private void Address_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!address.Text.Equals(current_path))
                    RootItemBrowse(current_path, address.Text, "");
                if (Explorer.Visibility == Visibility.Visible) Explorer.Focus();
                else RootPage.Focus();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RootItemBrowse(current_path, current_path, Filter_TextBox.Text.ToLower());
        }
        
        
        //RootItemBrowse(current_path, (QuickAccess_List.SelectedItem as QuickAccessMenuItem).isDrive ? (QuickAccess_List.SelectedItem as QuickAccessMenuItem).driveInfo.Name : (QuickAccess_List.SelectedItem as QuickAccessMenuItem).Directory.FullName, "");


        #endregion Exploreing

        private void FinishThis(ExplorerItem explorerItem)
        {
            if (CheckFileExists)
                if (dialogMode == SelectFileDialogMode.OpenFile && !File.Exists(explorerItem.ToolTip))
                {
                    Messages.ShowMessage("File is not exists.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    return;
                }
                else if (dialogMode == SelectFileDialogMode.SaveFile && File.Exists(explorerItem.ToolTip))
                {
                    if (Messages.ShowMessage("File is already exists. overwrite?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.No)
                        return;
                }
            if (dialogMode == SelectFileDialogMode.SaveFile)
            {
                if (AddExtension /*&& Path.GetExtension(explorerItem.ToolTip) == ""*/)
                    explorerItem.ToolTip += ((Filter_ComboBox.SelectedItem as ComboBoxItem).Tag as string[])[0];
                try
                {
                    File.Create(explorerItem.ToolTip);
                }
                catch(Exception ex)
                {
                    Messages.ShowMessage("Sorry we can not create file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                }
            }
            SelectedItem = explorerItem.ToolTip;
            SelectedItems = new List<string>();
            SelectedItems.Add(explorerItem.ToolTip);
            return_value = MyMessageBoxResult.OK;
            Close();
        }

        private void ShowSelectedList_Button_Click(object sender, RoutedEventArgs e)
        {
            if (MultiSelect)
            {
                multiselection_splitter.Height = new GridLength(1, GridUnitType.Star);
                //QuickAccess_List.Margin = new Thickness(0, 0, 0, 250);
                SelectedItems_Grid.Visibility = Visibility.Visible;
            }
        }

        private void RootPage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedItemName_TextBox.Text = "";
            if (RootPage.SelectedIndex > 0)
                selectedItemName_TextBox.Text = (RootPage.SelectedItem as RootItem).toolTip;
        }

        private void Explorer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedItemName_TextBox.Text = "";
            if (Explorer.SelectedIndex > -1)
            {
                if (Explorer.SelectedItems.Count == 1)
                    selectedItemName_TextBox.Text = (Explorer.SelectedItem as ExplorerItem).FullPath;
                else
                    foreach (ExplorerItem i in Explorer.SelectedItems)
                        selectedItemName_TextBox.Text += ("\"" + i.FullPath + "\" ");
            }
        }

        #region Search and Filter

        //BackgroundWorker for search
        //timer for filter

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Initializing)
                RootItemBrowse(current_path, current_path, "");
        }


        BackgroundWorker filter_wait_before_start = null;
        int filter_timer = 250;
        bool auto_changed = false;
        bool cancel_filter = false;

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!auto_changed)
            {
                if (Filter_TextBox.Text == "")
                    filter_placeholder.Visibility = Visibility.Visible;
                else
                    filter_placeholder.Visibility = Visibility.Collapsed;
                if (current_path != "root" && e.Changes != null && e.Changes.Count > 0)
                {
                    string path = current_path;
                    if (filter_wait_before_start != null && filter_wait_before_start.IsBusy)
                        filter_timer += 100;
                    else
                    {
                        if (filter_wait_before_start == null || !filter_wait_before_start.IsBusy)
                            filter_wait_before_start = new BackgroundWorker();
                        filter_wait_before_start.DoWork += (object sender2, DoWorkEventArgs e2) =>
                        {
                            while (filter_timer > 0)
                            {
                                Thread.Sleep(25);
                                filter_timer -= 25;
                            }
                            filter_wait_before_start.ReportProgress(0);
                        };
                        filter_wait_before_start.WorkerReportsProgress = true;
                        filter_wait_before_start.ProgressChanged += (object sender2, ProgressChangedEventArgs e2) =>
                        {
                            if (current_path == path && !cancel_filter)
                                RootItemBrowse(current_path, current_path, Filter_TextBox.Text.ToLower());
                            else
                                cancel_filter = false;
                        };
                        filter_wait_before_start.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) => { filter_timer = 250; };
                        filter_wait_before_start.RunWorkerAsync();
                    }
                }
            }
            
        }

        private void FilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchButton_Click(null, null);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (Filter_TextBox.Text != string.Empty)
            {
                cancel_filter = true;
                List<FileInfo> files_found = new List<FileInfo>();
                List<DirectoryInfo> directories_fuond = new List<DirectoryInfo>();
                string[] filters = (Filter_ComboBox.SelectedItem as ComboBoxItem).Tag as string[];
                try
                {
                    DirectoryInfo current = new DirectoryInfo(current_path);
                    SearchForItem(current, Filter_TextBox.Text.ToLower(), files_found, directories_fuond, filters);
                }
                catch (Exception) { }
                FillListWithItems(directories_fuond, files_found, "");
                cancel_filter = false;
            }
        }

        private void SearchForItem(DirectoryInfo current, string p, List<FileInfo> files_found, List<DirectoryInfo> directories_fuond, string[] filters)
        {
            FileInfo[] files = new FileInfo[0];
            DirectoryInfo[] sub_directory = new DirectoryInfo[0];
            try
            {
                if (dialogMode != SelectFileDialogMode.SelectDirectory)
                    files = current.GetFiles();
                sub_directory = current.GetDirectories();
            }
            catch (Exception) { }
            foreach (var i in sub_directory)
            {
                if (i.Name.ToLower().Contains(p))
                    directories_fuond.Add(i);
                SearchForItem(i, p, files_found, directories_fuond, filters);
            }
            bool all_files = (Filter_ComboBox.SelectedItem as ComboBoxItem).Tag is string;
            foreach (var i in files)
            {
                if (all_files || filters.Contains(i.Extension.ToLower()))
                    if (i.Name.ToLower().Contains(p))
                    files_found.Add(i);
            }
        }

        #endregion Search and Filter

        #region Selection List Control

        private void AddToSelectionList_Click(object sender, RoutedEventArgs e)
        {
            if (MultiSelect)
            {
                if (current_path == "root")
                {
                    if (dialogMode == SelectFileDialogMode.SelectDirectory)
                        if (RootPage.SelectedIndex > -1)
                        {
                            if (!ItemIsIn((RootPage.SelectedItem as RootItem).toolTip.ToLower(), multiselection_data))
                                multiselection_data.Add(new MultiSelectionItem() { isDirectory = true, FullPath = (RootPage.SelectedItem as RootItem).toolTip, Image = Functions.GetFileImage24("directory"), Directory = new DirectoryInfo((RootPage.SelectedItem as RootItem).toolTip) });
                        }
                        else
                            Messages.ShowMessage("Please select item first.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    else
                        Messages.ShowMessage("We can not add directories to list.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                }
                else
                {
                    if (Explorer.SelectedIndex > -1)
                        foreach (ExplorerItem i in Explorer.SelectedItems)
                            if (i.isDirectory && dialogMode == SelectFileDialogMode.SelectDirectory)
                            {
                                if (!ItemIsIn(i.ToolTip.ToLower(), multiselection_data))
                                    multiselection_data.Add(new MultiSelectionItem() { isDirectory = true, FullPath = i.ToolTip, Image = Functions.GetFileImage24("directory"), Directory = i.Directory });
                            }
                            else if (!i.isDirectory)
                            {
                                if (!ItemIsIn(i.ToolTip.ToLower(), multiselection_data))
                                    multiselection_data.Add(new MultiSelectionItem() { isDirectory = false, FullPath = i.ToolTip, Image = Functions.GetFileImage24(i.File.Extension), File = i.File });
                            }
                }
                if (multiselection_data.Count > 0)
                    use_multiselection_list = true;
                else
                    use_multiselection_list = false;
            }
        }

        private bool ItemIsIn(string item, ObservableCollection<MultiSelectionItem> multiselection_data)
        {
            foreach (var i in multiselection_data)
            {
                if (i.isDirectory)
                {
                    if ((i.Directory.FullName.ToLower().Equals(item)))
                        return true;
                }
                else
                {
                    if (i.File.FullName.ToLower().Equals(item))
                        return true;
                }
            }
            return false;
        }

        private void RemovefromSelectionList_Click(object sender, RoutedEventArgs e)
        {
            for (int i = MultiSelection_List.SelectedItems.Count - 1; i > -1; i--)
                multiselection_data.RemoveAt(MultiSelection_List.Items.IndexOf(MultiSelection_List.SelectedItems[i]));
            if (multiselection_data.Count == 0)
                use_multiselection_list = false;
        }

        private void ClearSelectionList_Click(object sender, RoutedEventArgs e)
        {
            multiselection_data.Clear();
            use_multiselection_list = false;
            multiselection_splitter.Height = new GridLength(0);
        }

        #endregion Selection List Control

        private void CreateFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (current_path != "root")
            {
                if (CreateFolderEnable)
                {
                    string new_name = null;
                    new_name = new Rename().ShowWindow("new directory", false);
                    string dir_name = null; ;
                    if (new_name != null && new_name != "")
                    {
                        if (Directory.Exists(current_path + "\\" + new_name))
                        {
                            int i = 1;
                            while (Directory.Exists(current_path + "\\" + new_name + " (" + i + ")")) i++;
                            if (Messages.ShowMessage("Directory is already exist.", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "We found a directory with same name you have chosen,Do you want to create '" + new_name + " (" + i + ")' instat?") == MyMessageBoxResult.Yes)
                                dir_name = current_path + "\\" + new_name + " (" + i + ")";
                        }
                        else
                            dir_name = current_path + "\\" + new_name;
                        try
                        {
                            if (dir_name != null) Directory.CreateDirectory(dir_name);
                            RefreshButton_Click(null, null);
                        }
                        catch (Exception ex) { Messages.ShowMessage("We found some problems while creating directory.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                    }
                }
                else
                    Messages.ShowMessage("You can not create directory from this dialog.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            }
            else
                Messages.ShowMessage("You can not create directory here!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!use_multiselection_list && !MultiSelect)
            {
                if (current_path == "root")
                {
                    if (dialogMode != SelectFileDialogMode.SaveFile)
                    {
                        if (RootPage.SelectedIndex > -1)
                            RootItemBrowse(current_path, (RootPage.SelectedItem as RootItem).toolTip, "");
                        else
                            Functions.ShuckWindow(this, true, "Please select an item.");
                    }
                    else
                        Functions.ShuckWindow(this, true, "You can not save file here.");
                }
                else
                {
                    if (Explorer.SelectedIndex > -1)
                    {
                        if ((Explorer.SelectedItem as ExplorerItem).isDirectory)
                            RootItemBrowse(current_path, (Explorer.SelectedItem as ExplorerItem).ToolTip, "");
                        else if (dialogMode != SelectFileDialogMode.SelectDirectory)
                            FinishThis(Explorer.SelectedItem as ExplorerItem);
                    }
                    else
                        if (dialogMode == SelectFileDialogMode.SelectDirectory)
                        {
                            return_value = MyMessageBoxResult.OK;
                            SelectedItem = current_path;
                            SelectedItems = new List<string>();
                            SelectedItems.Add(current_path);
                            Close();
                        }
                        else if (dialogMode == SelectFileDialogMode.OpenFile)
                            Functions.ShuckWindow(this, true, "Please select an item.");
                        else
                        {
                            if (selectedItemName_TextBox.Text == "")
                                Functions.ShuckWindow(this, true, "Please write file name.");
                            else
                                FinishThis(new ExplorerItem() { isDirectory = false, ToolTip = Path.Combine(current_path, selectedItemName_TextBox.Text) });
                        }
                }
            }
            else if (MultiSelect && use_multiselection_list)
            {
                if (multiselection_data.Count > 0)
                {
                    return_value = MyMessageBoxResult.OK;
                    SelectedItem = multiselection_data[multiselection_data.Count - 1].FullPath;
                    SelectedItems = new List<string>();
                    foreach (var i in multiselection_data)
                        SelectedItems.Add(i.FullPath);
                    Close();
                }
                else
                    Functions.ShuckWindow(this, true, "Multi-setection list is empty.");
            }
            else if (MultiSelect)
                if (Explorer.SelectedItems.Count > 0)
                {
                    foreach (ExplorerItem i in Explorer.SelectedItems)
                        if (i.isDirectory && dialogMode == SelectFileDialogMode.SelectDirectory)
                            SelectedItems.Add(i.ToolTip);
                        else if (!i.isDirectory && dialogMode == SelectFileDialogMode.OpenFile)
                            SelectedItems.Add(i.ToolTip);
                    if (SelectedItems.Count > 0)
                    {
                        return_value = MyMessageBoxResult.OK;
                        SelectedItem = ((ExplorerItem)Explorer.SelectedItems[Explorer.SelectedItems.Count - 1]).ToolTip;
                        Close();
                    }
                    else
                        Functions.ShuckWindow(this, true, "You can not choose selected items.");
                }
                else if (dialogMode == SelectFileDialogMode.SelectDirectory)
                {
                    return_value = MyMessageBoxResult.OK;
                    SelectedItem = (RootPage.SelectedItem as RootItem).toolTip;
                    SelectedItems = new List<string>();
                    SelectedItems.Add((RootPage.SelectedItem as RootItem).toolTip);
                    Close();
                }
                else
                    Functions.ShuckWindow(this, true, "Please select an item.");

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            return_value = MyMessageBoxResult.Cancel;
            Close();
        }

        private void SelectedItemNameTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Enter)
            //{

            //}
            //else if (e.Key == Key.Oem5)
            //{
            //    string path = "";
            //    if (Directory.Exists(selectedItemName_TextBox.Text))
            //        path = selectedItemName_TextBox.Text;
            //    else if (Directory.Exists(Path.Combine(current_path, selectedItemName_TextBox.Text)))
            //        path = Path.Combine(current_path, selectedItemName_TextBox.Text);
            //    if(Directory.Exists(path))
            //    {
            //        string[] sub=new string[0];
            //        string[] files=new string[0];
            //        try
            //        {
            //            sub = Directory.GetDirectories(path);
            //            if (dialogMode != SelectFileDialogMode.SelectDirectory)
            //                files = Directory.GetFiles(path);
            //        }
            //        catch(Exception)
            //        { return; }

            //    }


            //}

        }

    }

    internal enum SelectFileDialogMode
    {
        OpenFile = 0,
        SaveFile = 1,
        SelectDirectory = 2,
    }

    internal class QuickAccessMenuItem : GeneralPurpose
    {
        Thickness text_margin;
        public Thickness TextMargin
        {
            get { return text_margin; }
            set { text_margin = value; }
        }

        static Thickness margin = new Thickness(28, 2, 0, 0);

        bool is_enable;
        public bool IsEnable
        {
            get { return is_enable; }
            set
            {
                if (value)
                {
                    ImageHight = 24;
                    TextForeground = new SolidColorBrush(Colors.WhiteSmoke);
                }
                else
                {
                    ImageHight = 0;
                    TextForeground = new SolidColorBrush(Colors.LightGray);
                }
                is_enable = value;
            }
        }

        double font_size;
        public double FontSize
        {
            get { return font_size; }
            set { font_size = value; }
        }

        public DriveInfo driveInfo;
        public bool isDrive;

        double image_hight;
        public double ImageHight
        {
            get { return image_hight; }
            set { image_hight = value; }
        }

        SolidColorBrush text_foreground;
        public SolidColorBrush TextForeground
        {
            get { return text_foreground; }
            set { text_foreground = value; }
        }

        public QuickAccessMenuItem(DirectoryInfo folder, string name)
        {
            FullPath = folder.Name;
            Directory = folder;
            Image = Functions.GetDirectoryImage(name);
            isDrive = false;
            FontSize = 14;
            IsEnable = true;
        }

        public QuickAccessMenuItem(DriveInfo drive)
        {
            FullPath = drive.IsReady ? ((drive.VolumeLabel.Equals(string.Empty) ? "Local Drive" : drive.VolumeLabel) + " (" + drive.Name + ")") : drive.Name;
            driveInfo = drive;
            Image = Functions.GetDriveImage(drive, 24);
            isDrive = true;
            FontSize = 14;
            ImageHight = 24;
        }

        public QuickAccessMenuItem(string title)
        {
            FullPath = title;
            isDrive = false;
            FontSize = 12;
            IsEnable = false;
            ImageHight = 0;
        }
    }

    internal class MultiSelectionItem : GeneralPurpose
    {
        public bool isDirectory;
        public MultiSelectionItem()
        {

        }
    }

    public class SelectFileFilter
    {
        internal string[] extentions;
        internal string display_name;
        public SelectFileFilter() { }

        static public string[] SetExtentions(string extentions)
        {
            char[] splitter = { '|' };
            return extentions.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    internal class RootItem : GeneralPurpose
    {
        public bool isDrive;
        
        public DriveInfo driveInfo;

        string tool_tip;
        public string toolTip
        {
            get { return tool_tip; }
            set { tool_tip = value; }
        }

        string header;
        public string Header
        {
            get { return header; }
            set { header = value; }
        }

        double used_space;
        public double UsedSpace
        {
            get { return used_space; }
            set { used_space = value; }
        }

        Visibility show_used_space;
        public Visibility ShowUsedSpace
        {
            get { return show_used_space; }
            set { show_used_space = value; }
        }

        SolidColorBrush color;
        public SolidColorBrush Color
        {
            get { return color; }
            set { color = value; }
        }

        public RootItem(DriveInfo i)
        {
            Directory = i.RootDirectory;
            FullPath = Functions.GetDriveDescription(i);
            if (i.IsReady)
            {
                ShowUsedSpace = Visibility.Visible;
                if ((i.TotalFreeSpace * 100.0) / i.TotalSize > 15)
                    Color = Brushes.DodgerBlue;
                else
                    Color = Brushes.Tomato;
                if (i.TotalSize > 0)
                    UsedSpace = (i.TotalSize - i.TotalFreeSpace) * 125.0 / i.TotalSize;
                else
                    UsedSpace = 125;
            }
            else
            {
                UsedSpace = 0;
                Color = Brushes.DodgerBlue;
                ShowUsedSpace = Visibility.Collapsed;
            }
            Header = "Disk drivers";
            isDrive = true;
            driveInfo = i;
            toolTip = i.Name;
            Image = Functions.GetDriveImage(i, 64);
        }

        public RootItem(DirectoryInfo di, string header, string image)
        {
            UsedSpace = 0;
            Color = Brushes.DodgerBlue;
            ShowUsedSpace = Visibility.Collapsed;
            FullPath = di.Name;
            Header = header;
            isDrive = false;
            Directory = di;
            toolTip = di.FullName;
            Image = Functions.GetDirectoryImage(image);
        }
    }

    internal class ExplorerItem : GeneralPurpose
    {
        public bool isDirectory;

        bool is_cheacked;
        public bool IsSelected
        {
            get { return is_cheacked; }
            set
            {
                is_cheacked = value;
                OnPropertyChanged("IsSelected");
            }
        }

        string type;
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        string size;
        public string Size
        {
            get { return size; }
            set { size = value; }
        }

        string date;
        public string Date
        {
            get { return date; }
            set { date = value; }
        }

        string tooltip;
        public string ToolTip
        {
            get { return tooltip; }
            set { tooltip = value; }
        }

        public ExplorerItem()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
