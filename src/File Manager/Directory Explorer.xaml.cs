using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Directory_Explorer.xaml
    /// </summary>
    public partial class Directory_Explorer : Window
    {
        bool CloseWindow = false, show_hiddin_files = true;

        SortItemsBy sort_by = SortItemsBy.Name;

        string path = "root";

        string CurrentPath
        {
            get { return path; }
            set { path = value; }
        }

        List<DirectoryOrFile> directories_data = new List<DirectoryOrFile>();
        List<RootItem> root_data = new List<RootItem>();

        bool menu_is_openned = false;
        bool IsOpenned
        {
            get { return menu_is_openned; }
            set
            {
                if (value)
                {
                    file_button.IsEnabled = false;
                    back_grid.Visibility = file_menu.Visibility = Visibility.Visible;
                    menu_list.SelectedIndex = 0;
                    ThicknessAnimation ani = new ThicknessAnimation(new Thickness(250, 0, 0, 0), new Duration(new TimeSpan(1000000)));
                    ani.Completed += (object sender, EventArgs e) =>
                    {
                        file_button.IsEnabled = true;
                        file_button.Content = "BACK";
                    };
                    content.BeginAnimation(MarginProperty, ani);
                }
                else
                {
                    file_button.IsEnabled = false;
                    menu_list.SelectedIndex = -1;
                    ThicknessAnimation ani = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(1000000)));
                    ani.Completed += (object sender, EventArgs e) =>
                    {
                        back_grid.Visibility = file_menu.Visibility = Visibility.Collapsed;
                        file_button.IsEnabled = true;
                        file_button.Content = "FILE";
                    };
                    content.BeginAnimation(MarginProperty, ani);
                }
                menu_is_openned = value;
            }
        }

        sbyte openned_menu = -1;
        sbyte OpennedMenu
        {
            get { return openned_menu; }
            set
            {
                Thickness thick = new Thickness(100, 0, 0, 0);
                for (int i = 1; i < file_menu.Children.Count; i++)
                {
                    if (file_menu.Children[i] is Grid)
                    {
                        file_menu.Children[i].BeginAnimation(MarginProperty, new ThicknessAnimation(thick, new Duration(new TimeSpan(1))));
                        file_menu.Children[i].Visibility = Visibility.Collapsed;
                    }
                }
                if (value > -1)
                {
                    file_menu.Children[value + 1].Visibility = Visibility.Visible;
                    file_menu.Children[value + 1].BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(7, 0, 0, 0), new Duration(new TimeSpan(1500000))));
                }
                openned_menu = value;
            }
        }

        public Directory_Explorer()
        {
            InitializeComponent();
        }

        #region Animations

        private void File_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = !IsOpenned;
        }

        private void Background_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsOpenned)
                IsOpenned = false;
        }

        private void Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (menu_list.SelectedIndex != -1)
                if (menu_list.SelectedIndex != 6 && menu_list.SelectedIndex != 8)   //option and exit
                    OpennedMenu = (sbyte)menu_list.SelectedIndex;
                else if (menu_list.SelectedIndex == 8)
                    Close();
        }

        private void Navigation_Click(object sender, MouseButtonEventArgs e)
        {
            byte k = 0;
            byte it = 0;
            Grid current = (Grid)sender;
            ((TextBlock)current.Children[1]).Text = current.Tag.ToString();
            foreach (Grid i in navigation_panal.Children)
            {
                if (i == current)
                    k = it;
                quick_access_grids.Children[it].Visibility = Visibility.Collapsed;
                it++;
                i.Background = Brushes.Black;
                if (i != current)
                    ((TextBlock)i.Children[1]).Text = string.Empty;
                ((TextBlock)i.Children[1]).Margin = new Thickness(0);
            }
            current.Background = Brushes.White;
            ((TextBlock)current.Children[1]).Margin = new Thickness(32, 6, 5, 0);
            quick_access_grids.Children[k].Visibility = Visibility.Visible;
            quick_access_grids.BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(-200 * k, 34, 0, 0), new Duration(new TimeSpan(1500000))));
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (object sender_bw, DoWorkEventArgs e_bw) => { Thread.Sleep(1); };
            bw.RunWorkerCompleted += (object sender_bw, RunWorkerCompletedEventArgs e_bw) => {
                rec.Width = current.ActualWidth;
                rec.BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(30 * k, 30, 0, 0), new Duration(new TimeSpan(750000))));
            };
            bw.RunWorkerAsync();
        }

        #endregion

        #region Initialize

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Functions.IsRunningAsAdmin())
                Title = "[Administrator]  " + Title;
            Messages.ShowMessage("--Arabic (العربية)--\n**اخلاء مسؤلية**\nهذا البرنامج لم يصدر بعد وهو في نسخة تجريبية لذلك يجب\nأن تكون حذرا في التعامل معه وخاصة خاصية 'الحذف' التي\nقد تقوم وعن غير قصد بحذف احد ملفاتك الضرورية أو الاضرار\nبجهازك.\nيمكنك الاستمتاع  باستعمال كامل الميزات الآخرى ولكن يجب ان تكون حذرا جدا.\n\nالرجاء عدم استخدام خاصية الحذف.\n\n--English (English)--\n**Disclaimer**\nThis app is in alpha version, so be very careful using it\nespecially delete Function, it may delete some of your\nimportant files or damage your system.\nyou can ingoye using all other features, but you should be very careful.\n\nplease don't use 'delete' function.", ModrenMessageButton.OK,ModrenMessageImage.Exclamation,ModrenMessageSound.Exclamation,false, "");
            ConnectListWithData();
            LoadRoot();
            //RootPage.ItemTemplate = new DataTemplate();
            //List<DirectoryOrFile> j = new List<DirectoryOrFile>();
            //RootPage.DataContext = j;
            //j.Add(new DirectoryOrFile(new DirectoryInfo("C:\\")));
            //j.Add(new DirectoryOrFile(new DirectoryInfo("D:\\")));
            //j.Add(new DirectoryOrFile(new DirectoryInfo("F:\\")));
            //RootPage.Items.Refresh();
        }

        private void ConnectListWithData()
        {
            directories_list.DataContext = directories_data;
            root_list.DataContext = root_data;
        }

        private void LoadRoot()
        {
            directories_list.Visibility = Visibility.Collapsed;
            directories_data.Clear();
            root_data = new List<RootItem>();
            root_list.DataContext = root_data;
            directories_list.Items.Refresh();
            root_list.Visibility = Visibility.Visible;

            BackgroundWorker get_drivers = new BackgroundWorker();
            get_drivers.WorkerSupportsCancellation = true;
            DriveInfo[] drivers = new DriveInfo[0];
            bool worker_isbusy = true;
            get_drivers.DoWork += (object senderw, DoWorkEventArgs ew) =>
            {
                drivers = DriveInfo.GetDrives();
                while (worker_isbusy)
                    Thread.Sleep(25);
            };
            get_drivers.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
              {
                  for (int i = drivers.Length - 1; i > -1; i--)
                      quick_access_list.Items.Insert(0, CreateQuickAccessItem(drivers[i]));

                  for (int i = 0; i < drivers.Length; i++)
                      root_data.Add(new RootItem(drivers[i]));
                  quick_access_list.Items.Insert(0, CreateQuickAccessItem("Disk drives"));

                  CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(root_list.ItemsSource);
                  PropertyGroupDescription groupDescription = new PropertyGroupDescription("Header");
                  view.GroupDescriptions.Add(groupDescription);

                  quick_access_list.Items.Insert(0, CreateQuickAccessRoot());

                  root_list.Items.Refresh();
              };
            get_drivers.RunWorkerAsync();

            //directories_list.GroupStyle = root_view.GroupStyle;
            //string tmp_path;
            string[] user_folders = { Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Functions.GetDownloadsPath(),Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) };
            string[] user_folders_images = { "user", "desktop", "downloads", "documents", "music", "pictures", "videos" };

            quick_access_list.Items.Add(CreateQuickAccessItem("User directories"));
            for (int i = 0; i < user_folders.Length; i++)
            {
                if (Directory.Exists(user_folders[i]))
                {
                    DirectoryInfo dir = new DirectoryInfo(user_folders[i]);
                    quick_access_list.Items.Add(CreateQuickAccessItem(dir, user_folders_images[i]));
                    root_data.Add(new RootItem(dir, "User directories", user_folders_images[i]));
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
            quick_access_list.Items.Add(CreateQuickAccessItem("System directories"));
            bool k = false;
            for (int i = 0; i <special_folders.Length; i++)
            {
                if (Directory.Exists(special_folders[i]))
                {
                    if (!k &&i+1<special_folders.Length&& (special_folders[i].ToLower().Equals(special_folders[i + 1].ToLower())))
                    {
                        k = true;
                        continue;
                    }
                    DirectoryInfo dir = new DirectoryInfo(special_folders[i]);
                    quick_access_list.Items.Add(CreateQuickAccessItem(dir, special_folders_image[i]));
                    root_data.Add(new RootItem(dir, "Systerm directories", special_folders_image[i]));

                    //directories_data.Add(new DirectoryOrFile(dir) { Header = "Systerm directories" });
                }
            }
            worker_isbusy = false;
            //for (int i = 0; i < quick_acess_list.Items.Count; i++)
            //{
            //    if (quick_access_data[i].IsEnable)
            //        (quick_acess_list.Items[i] as ListBoxItem).Cursor = Cursors.Hand;
            //    (quick_acess_list.Items[i] as ListBoxItem).IsEnabled = quick_access_data[i].IsEnable;
            //}
            //quick_acess_list.Items.Refresh();
        }

        private object CreateQuickAccessRoot()
        {
            Image image = new Image();
            image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/go-home-48.png"));
            image.Width = image.Height = 20;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.HorizontalAlignment = HorizontalAlignment.Left;

            TextBlock text = new TextBlock();
            text.Text = "root";
            text.Margin = new Thickness(5, 2, 0, 0);
            text.VerticalAlignment = VerticalAlignment.Top;
            text.HorizontalAlignment = HorizontalAlignment.Left;
            text.FontSize = 12;
            text.Foreground = Brushes.LightGray;

            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            stack.Children.Add(image);
            stack.Children.Add(text);

            ListBoxItem item = new ListBoxItem();
            item.Tag = "root";
            item.ContentStringFormat = "root";
            item.Content = stack;
            return item;
        }

        private ListBoxItem CreateQuickAccessItem(DirectoryInfo folder, string picture_name)
        {
            Image image = new Image();
            image.Source = Functions.GetSpecialDirectoryImage(picture_name);
            image.Width = image.Height = 24;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.HorizontalAlignment = HorizontalAlignment.Left;

            TextBlock text = new TextBlock();
            text.Text = folder.Name;
            text.Margin = new Thickness(5, 0, 0, 0);
            text.VerticalAlignment = VerticalAlignment.Top;
            text.HorizontalAlignment = HorizontalAlignment.Left;
            text.FontSize = 14;
            text.Foreground = Brushes.WhiteSmoke;

            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            stack.Children.Add(image);
            stack.Children.Add(text);

            ListBoxItem item = new ListBoxItem();
            item.Tag = folder;
            item.ContentStringFormat = folder.FullName;
            item.Content = stack;
            return item;
        }

        private ListBoxItem CreateQuickAccessItem(DriveInfo drive)
        {
            Image image = new Image();
            image.Source = Functions.GetDriveImage(drive, 24);
            image.Width = image.Height = 24;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.HorizontalAlignment = HorizontalAlignment.Left;

            TextBlock text = new TextBlock();
            text.Text = drive.IsReady ? ((drive.VolumeLabel.Equals(string.Empty) ? "Local Drive" : drive.VolumeLabel) + " (" + drive.Name + ")") : drive.Name;
            text.Margin = new Thickness(5, 0, 0, 0);
            text.VerticalAlignment = VerticalAlignment.Top;
            text.HorizontalAlignment = HorizontalAlignment.Left;
            text.FontSize = 14;
            text.Foreground = Brushes.WhiteSmoke;

            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            stack.Children.Add(image);
            stack.Children.Add(text);

            ListBoxItem item = new ListBoxItem();
            item.Tag = drive;
            item.ContentStringFormat = drive.Name;
            item.Content = stack;
            return item;
        }

        private ListBoxItem CreateQuickAccessItem(string title)
        {
            ListBoxItem item = new ListBoxItem();
            item.FontSize = 12;
            //item.Foreground =;
            item.Margin = new Thickness(5, 0, 0, 0);
            item.IsEnabled = false;
            item.Content = title;
            return item;
        }

        #endregion

        #region Exploring
        private void RootItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                RootItemBrowse("root", (root_list.SelectedItem as RootItem).toolTip, "");
        }

        void RootItemBrowse(string old_path, string new_path, string filter_text)
        {
            if (!IsSpecialDirictory(new_path)) //if user want to go to a special folder by typing its name in address bar
            {
                if (new_path.Length > 0 && new_path[new_path.Length - 1] != '\\')
                    new_path = new_path + "\\";
                bool WorkerHasFailed = false;
                List<DirectoryInfo> sub_directories = new List<DirectoryInfo>();
                List<FileInfo> files = new List<FileInfo>();
                string error_message = string.Empty, error_message_info = string.Empty;
                info_textblock.Text = "Working on it...";
                info_textblock.Visibility = Visibility.Visible;

                BackgroundWorker GetFilesAndDirectory = new BackgroundWorker();
                GetFilesAndDirectory.WorkerReportsProgress = true;
                GetFilesAndDirectory.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    if (e.ProgressPercentage < 0)
                    {
                        Messages.ShowMessage(error_message, ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, error_message_info != string.Empty, error_message_info);
                    }
                };
                GetFilesAndDirectory.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    if (Directory.Exists(new_path))
                    {
                        DirectoryInfo Current_Directory = new DirectoryInfo(new_path);
                        try
                        {
                            sub_directories = Current_Directory.GetDirectories().ToList();
                            sub_directories.Sort(DirectorySorter);
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
                        FillListWithItems(sub_directories, files, filter_text);
                        CurrentPath = new_path;
                    }
                    directories_list.IsEnabled = true;
                    directories_list.Items.Refresh();
                    address_textblock.Text = CurrentPath;
                    if (filter_TextBox.Text.Equals(string.Empty))
                        info_textblock.Text = "This directory is emety.";
                    else
                        info_textblock.Text = "No item much your search.";
                    if (directories_data.Count > 0)
                        info_textblock.Visibility = Visibility.Collapsed;
                };
                directories_list.IsEnabled = false;
                GetFilesAndDirectory.RunWorkerAsync();
            }
            else
            {
                new_path = new_path.ToLower();
                if (new_path.Equals("root"))
                    LoadRoot();
                else
                    RootItemBrowse(old_path, GetSpecialDirictoryPath(new_path), filter_text);
            }
        }

        private void DirectoryItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                if ((directories_list.SelectedItem as DirectoryOrFile).fileinfo is DirectoryInfo)
                    RootItemBrowse(CurrentPath, (directories_list.SelectedItem as DirectoryOrFile).fileinfo.FullName, "");
                else if ((directories_list.SelectedItem as DirectoryOrFile).fileinfo is FileInfo)
                    RunFile((directories_list.SelectedItem as DirectoryOrFile).fileinfo.FullName);
        }

        void FillListWithItems(List<DirectoryInfo> sub_directories, List<FileInfo> files, string filter_string)
        {
            directories_data.Clear();
            root_list.Visibility = Visibility.Collapsed;
            directories_list.Visibility = Visibility.Visible;
            foreach (var i in sub_directories)
                if (show_hiddin_files || (!show_hiddin_files && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                    if (i.Name.ToLower().Contains(filter_string))
                        directories_data.Add(new DirectoryOrFile(i));
            foreach (var i in files)
                if (show_hiddin_files || (!show_hiddin_files && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                    if (i.Name.ToLower().Contains(filter_string))
                        directories_data.Add(new DirectoryOrFile(i));
            if (directories_list.Items.Count > 0)
            {
                directories_list.ScrollIntoView(directories_list.Items[0]);
                info_textblock.Visibility = Visibility.Collapsed;
            }
            if (filter_string.Equals(string.Empty))
                info_textblock.Text = "This directory is emety.";
            else
                info_textblock.Text = "No item much your search.";
        }


        #region Functions

        private void RunFile(string file_name)
        {
            //add to history
            try
            {
                Process.Start(file_name);
            }
            catch(Exception ex)
            { Messages.ShowMessage(string.Format("Sorry we can not open this file '{0}'.", file_name), ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        bool IsSpecialDirictory(string new_path)
        {
            string[] SpecialDirectories = { "root", Environment.UserName.ToLower(), "desktop", "downloads", "documents", "music", "pictures", "videos", "program files", "program files (x86)", "fonts", "local", "history" };
            foreach (var i in SpecialDirectories)
                if (i.Equals(new_path))
                    return true;
            return false;
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

        int FilesSorter(FileInfo x, FileInfo y)
        {
            switch (sort_by)
            {
                case SortItemsBy.Name:
                    return x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.Size:
                    return x.Length.CompareTo(y.Length);
                case SortItemsBy.type:
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
                case SortItemsBy.type:
                    return x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.DateModified:
                    return x.LastWriteTimeUtc.Ticks.CompareTo(y.LastWriteTimeUtc.Ticks);
            }
            return 0;
        }

        #endregion

        #endregion

        private void AddressTextBlock_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!address_textblock.Text.ToLower().Equals(CurrentPath.ToLower()))
                    RootItemBrowse(CurrentPath, address_textblock.Text, "");
                if (directories_list.Visibility == Visibility.Visible)
                    directories_list.Focus();
                else
                    root_list.Focus();
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //RootPage.ItemTemplate = Root3Page.ItemTemplate;
        }


    }

    class DirectoryOrFile
    {
        public DirectoryOrFile(FileInfo file)
        {
            fileinfo = file;
            FileName = file.Name;
            FullPath = file.FullName;
            if (FileInfoCombonents.FilesInformation.ContainsKey(file.Extension.ToLower()))
            {
                Icon = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Bitmap24;
                FileType = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Description;
            }
            else
            {
                Icon = FileInfoCombonents.FilesInformation["."].Bitmap24;
                FileType = file.Extension.Replace(".", "").ToUpper() + " File";
            }
            FileSize = Functions.FileSize(file.Length, 2);
            DateModified = Functions.OrginizeDate(file.LastWriteTime);
        }

        public DirectoryOrFile(DirectoryInfo directory)
        {
            fileinfo = directory;
            FullPath = directory.FullName;
            FileName = directory.Name;
            Icon = FileInfoCombonents.FilesInformation["directory"].Bitmap24;
            FileType = "Directory";
            FileSize = "";
            DateModified = Functions.OrginizeDate(directory.LastWriteTime);
        }

        public DirectoryOrFile(DriveInfo drive)
        {
            FullPath = drive.Name;
            this.drive = drive;
            isDrive = true;
            if (drive.IsReady)
            {
                FileName = drive.IsReady ? ((drive.VolumeLabel.Equals(string.Empty) ? "Local Drive" : drive.VolumeLabel) + " (" + drive.Name + ")") : drive.Name;
                FileSize = Functions.FileSize(drive.AvailableFreeSpace, 2);
                DateModified = Functions.FileSize(drive.TotalSize, 2);
            }
            else
            {
                FileName = drive.Name;
                FileSize = "Drive is not ready!";
                DateModified = "";
            }
            Icon = Functions.GetDriveImage(drive, 24);
            FileType = Functions.GetDriveDescription(drive.DriveType);
        }

        internal string FullPath;

        internal FileSystemInfo fileinfo;

        internal DriveInfo drive;

        internal bool isDrive = false;

        private BitmapImage img_source;
        public BitmapImage Icon
        {
            get { return img_source; }
            set { img_source = value; }
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

        private string file_date;
        public string DateModified
        {
            get { return file_date; }
            set { file_date = value; }
        }

        string header;
        public string Header
        {
            get { return header; }
            set { header = value; }
        }
    }
}
