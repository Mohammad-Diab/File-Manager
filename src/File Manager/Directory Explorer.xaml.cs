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
    /// 

        //Drug and drop
        //if file type is unkonwn try to get type and icon from windows
    public partial class Directory_Explorer : Window
    {
        bool CloseWindow = false, show_hiddin_files = true, show_extension = true;

        SortItemsBy sort_by = SortItemsBy.Name;

        string path = "root";

        string CurrentPath
        {
            get { return path; }
            set
            {
                if (value.ToLower() == "root")
                {
                    path = "root";
                    up_btm.IsEnabled = false;
                }
                else
                {
                    up_btm.IsEnabled = true;
                    path = value;
                }
            }
        }

        List<DirectoryOrFile> directories_data = new List<DirectoryOrFile>();
        List<RootItem> root_data = new List<RootItem>();
        List<BackMenuItem> back_menu_data = new List<BackMenuItem>();
        bool update_history_index = true;

        int current_back_index = 0;
        int CurrentBackIndex
        {
            get { return current_back_index; }
            set
            {
                if (value > -1 && value < back_menu_data.Count)
                {
                    if (back_menu_data[value].Path != CurrentPath)
                        RootItemBrowse(CurrentPath, back_menu_data[value].Path, false, "", false);
                    current_back_index = value;
                }
            }
        }

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

                    MainMenuItem_Click(menu_list.Items[directories_list.SelectedIndex > -1 ? 2 : 0], null);

                    ThicknessAnimation f_rec_ani = new ThicknessAnimation(new Thickness(20, 4, 0, 0), new Duration(new TimeSpan(500000)));
                    f_rec_ani.Completed += (object sender, EventArgs e) =>
                     {
                         ThicknessAnimation s_rec_ani = new ThicknessAnimation(new Thickness(20, 9, 0, 0), new Duration(new TimeSpan(500000)));
                         s_rec_ani.Completed += (object sender2, EventArgs e2) =>
                         {
                             ThicknessAnimation t_rec_ani = new ThicknessAnimation(new Thickness(20, 14, 0, 0), new Duration(new TimeSpan(500000)));
                             t_rec_ani.Completed += (object sender3, EventArgs e3) =>
                             {
                                 //for (int i = 1; i < hbrg_recs.Children.Count; i++)
                                 //    hbrg_recs.Children[i].Visibility = Visibility.Collapsed;
                                 //move arow
                                 hbrg_recs.Children[0].Visibility = Visibility.Visible;
                                 ThicknessAnimation arr_ani = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(750000)));
                                 hbrg_recs.Children[0].BeginAnimation(MarginProperty, arr_ani);
                             };
                             hbrg_recs.Children[3].BeginAnimation(MarginProperty, t_rec_ani);
                         };
                         hbrg_recs.Children[2].BeginAnimation(MarginProperty, s_rec_ani);
                     };
                    hbrg_recs.Children[1].BeginAnimation(MarginProperty, f_rec_ani);

                    ThicknessAnimation menu_anim = new ThicknessAnimation(new Thickness(350, 0, 0, 0), new Duration(new TimeSpan(1000000)));
                    menu_anim.Completed += (object sender, EventArgs e) =>
                    {
                        file_button.IsEnabled = true;
                        //file_button.Content = "BACK";
                    };
                    content.BeginAnimation(MarginProperty, menu_anim);
                }
                else
                {
                    file_button.IsEnabled = false;
                    //menu_list.SelectedIndex = -1;
                    ThicknessAnimation arr_ani = new ThicknessAnimation(new Thickness(-40, 0, 0, 0), new Duration(new TimeSpan(500000)));
                    arr_ani.Completed += (object sender4, EventArgs e4) => 
                    {
                        hbrg_recs.Children[0].Visibility = Visibility.Hidden;
                        ThicknessAnimation f_rec_ani = new ThicknessAnimation(new Thickness(0, 14, 0, 0), new Duration(new TimeSpan(750000)));
                        f_rec_ani.Completed += (object sender, EventArgs e) =>
                        {
                            ThicknessAnimation s_rec_ani = new ThicknessAnimation(new Thickness(0, 9, 0, 0), new Duration(new TimeSpan(750000)));
                            s_rec_ani.Completed += (object sender2, EventArgs e2) =>
                            { hbrg_recs.Children[1].BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(0, 4, 0, 0), new Duration(new TimeSpan(750000)))); };
                            hbrg_recs.Children[2].BeginAnimation(MarginProperty, s_rec_ani);
                        };
                        //for (int i = 1; i < hbrg_recs.Children.Count; i++)
                        //    hbrg_recs.Children[i].Visibility = Visibility.Visible;
                        hbrg_recs.Children[3].BeginAnimation(MarginProperty, f_rec_ani);

                    };
                    hbrg_recs.Children[0].BeginAnimation(MarginProperty, arr_ani);



                    ThicknessAnimation menu_anim = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(1000000)));
                    menu_anim.Completed += (object sender, EventArgs e) =>
                    {
                        back_grid.Visibility = file_menu.Visibility = Visibility.Collapsed;
                        file_button.IsEnabled = true;
                        //file_button.Content = "FILE";
                    };
                    content.BeginAnimation(MarginProperty, menu_anim);
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
                if (value != openned_menu)
                {
                    if (value == 1)
                    {
                        file_menu.HorizontalAlignment = HorizontalAlignment.Stretch;
                        file_menu.Width = double.NaN;
                        CalcInfoAboutItems();
                    }
                    else
                    {
                        if (value == 5)
                            recent_menu_Opened();
                        cancel_calc_dir_size = true;
                        file_menu.HorizontalAlignment = HorizontalAlignment.Left;
                        file_menu.Width = 550;
                    }
                    Thickness thick = new Thickness(100, 0, 0, 0);
                    for (int i = 1; i < file_menu_grids.Children.Count; i++)
                    {
                        if (file_menu_grids.Children[i] is Grid)
                        {
                            file_menu_grids.Children[i].BeginAnimation(MarginProperty, new ThicknessAnimation(thick, new Duration(new TimeSpan(1))));
                            file_menu_grids.Children[i].Visibility = Visibility.Collapsed;
                        }
                    }
                    if (value > -1)
                    {
                        file_menu_grids.Children[value + 1].Visibility = Visibility.Visible;
                        file_menu_grids.Children[value + 1].BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(7, 0, 0, 0), new Duration(new TimeSpan(1500000))));
                    }
                    openned_menu = value;
                }
            }
        }


        public Directory_Explorer()
        {
            InitializeComponent();
        }

        static internal void ExplorePath(string path)
        {
            if(Directory.Exists(path))
            {
                Directory_Explorer window = new Directory_Explorer();
                window.Show();
                window.RootItemBrowse("root", path, true, "", true);
            }
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

        private void MainMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender != menu_list.Items[6] && sender != menu_list.Items[8] && sender != menu_list.Items[9])   //options, legacy explorer and exit
                OpennedMenu = (sbyte)menu_list.Items.IndexOf(sender);
            else
            {
                IsOpenned = false;
                {
                    if (sender == menu_list.Items[6])
                        new Options().ShowDialog();
                    else if (sender == menu_list.Items[8])
                        new MainWindow().Show();
                    else
                        Close();
                }
            }
        }

        //private void Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (menu_list.SelectedIndex != -1)
        //        if (menu_list.SelectedIndex != 6 && menu_list.SelectedIndex != 8)   //view option and exit
        //            OpennedMenu = (sbyte)menu_list.SelectedIndex;
        //        else if (menu_list.SelectedIndex == 8)
        //            Close();
        //}

        private void Navigation_Click(object sender, MouseButtonEventArgs e)
        {
            Grid current = (Grid)sender;
            MainTabNavigation(current);
        }

        private void ViewHistory_btm_Click(object sender, RoutedEventArgs e)
        {
            MainTabNavigation(history_tab_grid);
        }

        void MainTabNavigation(Grid sender)
        {
            byte k = 0;
            byte it = 0;
            ((TextBlock)sender.Children[1]).Text = sender.Tag.ToString();
            foreach (Grid i in navigation_panal.Children)
            {
                if (i == sender)
                    k = it;
                quick_access_grids.Children[it].Visibility = Visibility.Collapsed;
                it++;
                i.Background = Brushes.Black;
                if (i != sender)
                    ((TextBlock)i.Children[1]).Text = string.Empty;
                ((TextBlock)i.Children[1]).Margin = new Thickness(0);
            }
            sender.Background = Brushes.White;
            ((TextBlock)sender.Children[1]).Margin = new Thickness(32, 6, 5, 0);
            quick_access_grids.Children[k].Visibility = Visibility.Visible;
            quick_access_grids.BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(-200 * k, 34, 0, 0), new Duration(new TimeSpan(1500000))));
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (object sender_bw, DoWorkEventArgs e_bw) => { Thread.Sleep(1); };
            bw.RunWorkerCompleted += (object sender_bw, RunWorkerCompletedEventArgs e_bw) =>
            {
                rec.Width = sender.ActualWidth;
                rec.BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(30 * k, 30, 0, 0), new Duration(new TimeSpan(750000))));
            };
            bw.RunWorkerAsync();
        }

        #endregion

        #region Initialize

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.IsAdmin)
                Title = "[Administrator]  " + Title;
            Messages.ShowMessage("--Arabic (العربية)--\n**اخلاء مسؤلية**\nهذا البرنامج لم يصدر بعد وهو في نسخة تجريبية لذلك يجب\nأن تكون حذرا في التعامل معه وخاصة خاصية 'الحذف' التي\nقد تقوم وعن غير قصد بحذف احد ملفاتك الضرورية أو الاضرار\nبجهازك.\nيمكنك الاستمتاع  باستعمال كامل الميزات الآخرى ولكن يجب ان تكون حذرا جدا.\n\nالرجاء عدم استخدام خاصية الحذف.\n\n--English (English)--\n**Disclaimer**\nThis app is in alpha version, so be very careful using it\nespecially delete Function, it may delete some of your\nimportant files or damage your system.\nyou can ingoye using all other features, but you should be very careful.\n\nplease don't use 'delete' function.", ModrenMessageButton.OK,ModrenMessageImage.Exclamation,ModrenMessageSound.Exclamation,false, "");
            ConnectListWithData();
            LoadRoot(true);
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
            history_list.DataContext = back_menu_data;
        }

        private void LoadRoot(bool add_to_back_menu)
        {
            directories_list.Visibility = Visibility.Collapsed;
            directories_data.Clear();
            root_data = new List<RootItem>();
            root_list.DataContext = root_data;
            quick_access_list.Items.Clear();
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
                  controls_content.IsEnabled = true;
                  for (int i = drivers.Length - 1; i > -1; i--)
                      quick_access_list.Items.Insert(0, CreateQuickAccessItem(Functions.GetDriveImage(drivers[i], 24), 24, drivers[i].IsReady ? ((drivers[i].VolumeLabel.Equals(string.Empty) ? "Local disk" : drivers[i].VolumeLabel) + " (" + drivers[i].Name + ")") : drivers[i].Name, drivers[i], drivers[i].Name));

                  for (int i = 0; i < drivers.Length; i++)
                      root_data.Add(new RootItem(drivers[i]));
                  quick_access_list.Items.Insert(0, CreateQuickAccessItem("Local disks"));

                  CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(root_list.ItemsSource);
                  PropertyGroupDescription groupDescription = new PropertyGroupDescription("Header");
                  view.GroupDescriptions.Add(groupDescription);

                  quick_access_list.Items.Insert(0, CreateQuickAccessItem(new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/go-home-48.png")), 20, "root", "root", "root"));
                  CurrentPath = "root";
                  if (add_to_back_menu)
                  {
                      if (back_menu_data.Count > CurrentBackIndex)
                      {
                          back_menu_data.RemoveRange(0, CurrentBackIndex);
                          current_back_index = 0;
                      }
                      back_menu_data.Insert(0, new BackMenuItem("root"));
                      history_list.Items.Refresh();
                  }
                  if (history_list.Items.Count > CurrentBackIndex)
                      history_list.SelectedIndex = CurrentBackIndex;
                  root_list.Items.Refresh();

                  items_count.Text = root_data.Count.ToString();
                  item_unit.Text = root_data.Count == 1 ? " item" : " items";
                  if (root_list.SelectedIndex == -1)
                      items_selected.Visibility = Visibility.Collapsed;
                  items_selected_size.Visibility = Visibility.Collapsed;
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
                    quick_access_list.Items.Add(CreateQuickAccessItem(Functions.GetDirectoryImage(user_folders_images[i]), 24, dir.Name, dir, dir.FullName));
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
                    quick_access_list.Items.Add(CreateQuickAccessItem(Functions.GetDirectoryImage(user_folders_images[i]), 24, dir.Name, dir, dir.FullName));
                    root_data.Add(new RootItem(dir, "System directories", special_folders_image[i]));

                    //directories_data.Add(new DirectoryOrFile(dir) { Header = "Systerm directories" });
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
                     string path = ((ListBoxItem)sender).ContentStringFormat.ToLower();
                     if (IsSpecialDirectory(path))
                         path = GetSpecialDirectoryPath(path);
                     if (path.Length > 1 && path[path.Length - 1] != '\\')
                         path += '\\';
                     if (CurrentPath.ToLower() != path)
                         RootItemBrowse(CurrentPath, ((ListBoxItem)sender).ContentStringFormat, true, "", false);
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

        #endregion

        #region Exploring

        private void RootItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                if (root_list.SelectedItem != null)
                    RootItemBrowse("root", (root_list.SelectedItem as RootItem).toolTip, true, "", false);
        }

        private void RootItemBrowse(string old_path, string new_path, bool add_to_back_menu, string filter_text, bool close_if_fail)
        {
            if (!IsSpecialDirectory(new_path)) //if user want to go to a special folder by typing its name in address bar
            {
                if (new_path.Length > 0 && new_path[new_path.Length - 1] != '\\')
                    new_path = new_path + "\\";
                bool WorkerHasFailed = false;
                List<DirectoryInfo> sub_directories = new List<DirectoryInfo>();
                List<FileInfo> files = new List<FileInfo>();
                string error_message = string.Empty, error_message_info = string.Empty;
                info_textblock.Text = "working on it...";
                info_textblock.Visibility = Visibility.Visible;
                BackgroundWorker GetFilesAndDirectory = new BackgroundWorker();
                GetFilesAndDirectory.WorkerReportsProgress = true;
                GetFilesAndDirectory.WorkerSupportsCancellation = true;
                GetFilesAndDirectory.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    if (e.ProgressPercentage < 0)
                        Messages.ShowMessage(error_message, ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, error_message_info != string.Empty, error_message_info);
                };

                GetFilesAndDirectory.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    if (Directory.Exists(new_path))
                    {
                        bool done = false;
                        Thread get_directory_timer = new Thread(() =>
                        {
                            bool failed = true;
                            for (int i = 0; i < 2000; i++)
                            {
                                Thread.Sleep(30);
                                if (done)
                                {
                                    failed = false;
                                    return;
                                }
                            }
                            if (failed)
                            {
                                error_message = "time out error";
                                error_message_info = "";
                                if (GetFilesAndDirectory.IsBusy)
                                    GetFilesAndDirectory.ReportProgress(-1);
                                else
                                    return;
                                WorkerHasFailed = true;
                                GetFilesAndDirectory.CancelAsync();
                            }
                        });
                        get_directory_timer.Start();
                        DirectoryInfo Current_Directory = new DirectoryInfo(new_path);
                        try
                        {
                            sub_directories = Current_Directory.GetDirectories().ToList();
                            files = Current_Directory.GetFiles().ToList();
                            sub_directories.Sort(DirectorySorter);
                            done = true;
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
                    stop_exploring_progressbar = controls_content.IsEnabled = true;
                    if (!WorkerHasFailed)
                    {
                        FillListWithItems(sub_directories, files, filter_text);
                        CurrentPath = new_path;
                        if (add_to_back_menu)
                        {
                            if (back_menu_data.Count > CurrentBackIndex)
                            {
                                back_menu_data.RemoveRange(0, CurrentBackIndex);
                                current_back_index = 0;
                            }
                            back_menu_data.Insert(0, new BackMenuItem(new_path));
                        }
                        history_list.Items.Refresh();
                        if (history_list.Items.Count > CurrentBackIndex && update_history_index)
                            history_list.SelectedIndex = CurrentBackIndex;
                    }
                    else if(close_if_fail)
                    {
                        Close();
                        return;
                    }
                    directories_list.IsEnabled = true;
                    directories_list.Items.Refresh();
                    address_textblock.Text = CurrentPath;
                    if (filter_text.Equals(string.Empty))
                        info_textblock.Text = "This directory is empty.";
                    else
                        info_textblock.Text = "No items matsh your search.";
                    if (CurrentPath == "root" || directories_data.Count > 0)
                        info_textblock.Visibility = Visibility.Collapsed;
                    items_count.Text = CurrentPath == "root" ? root_data.Count.ToString() : directories_data.Count.ToString();
                    item_unit.Text = directories_data.Count == 1 ? " item" : " items";
                    if (directories_list.SelectedIndex == -1)
                    {
                        items_selected.Visibility = Visibility.Collapsed;
                        items_selected_size.Visibility = Visibility.Collapsed;
                    }

                };
                directories_list.IsEnabled = false;
                ShowProgress();
                GetFilesAndDirectory.RunWorkerAsync();
            }
            else
            {
                new_path = new_path.ToLower();
                if (new_path.Equals("root"))
                {
                    LoadRoot(add_to_back_menu);
                    address_textblock.Text = "root";
                }
                else
                    RootItemBrowse(old_path, GetSpecialDirectoryPath(new_path), add_to_back_menu, filter_text, close_if_fail);
            }
        }

        private void DirectoryItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                if ((directories_list.SelectedItem as DirectoryOrFile).fileinfo is DirectoryInfo)
                    RootItemBrowse(CurrentPath, (directories_list.SelectedItem as DirectoryOrFile).fileinfo.FullName, true, "", false);
                else if ((directories_list.SelectedItem as DirectoryOrFile).fileinfo is FileInfo)
                    RunFile((directories_list.SelectedItem as DirectoryOrFile).fileinfo.FullName);
        }

        private void DirectoriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            items_selected_count.Text = directories_list.SelectedItems.Count.ToString();
            items_selected_size.Visibility = Visibility.Collapsed;
            if (directories_list.SelectedIndex == -1)
                items_selected.Visibility = Visibility.Collapsed;
            else
            {
                if (directories_list.SelectedItems.Count < 101)
                {
                    long files_size = 0;
                    bool has_directory = false;
                    foreach (DirectoryOrFile i in directories_list.SelectedItems)
                    {
                        if (i.isDirectory)
                        {
                            has_directory = true;
                            break;
                        }
                        else
                            files_size += (i.fileinfo as FileInfo).Length;
                    }
                    if (!has_directory)
                    {
                        items_selected_size.Visibility = Visibility.Visible;
                        items_selected_size.Text = Functions.FileSize(files_size, 2);
                    }
                }
                items_selected.Visibility = Visibility.Visible;
            }
        }

        private void RootList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            items_selected_count.Text = root_list.SelectedItems.Count.ToString();
            items_selected_size.Visibility = Visibility.Collapsed;
            if (root_list.SelectedIndex == -1)
                items_selected.Visibility = Visibility.Collapsed;
            else
                items_selected.Visibility = Visibility.Visible;
        }

        void FillListWithItems(List<DirectoryInfo> sub_directories, List<FileInfo> files, string filter_string)
        {
            directories_data.Clear();
            root_list.Visibility = Visibility.Collapsed;
            root_list.SelectedIndex = -1;
            directories_list.Visibility = Visibility.Visible;
            foreach (var i in sub_directories)
                if (show_hiddin_files || (!show_hiddin_files && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                    if (i.Name.ToLower().Contains(filter_string))
                        directories_data.Add(new DirectoryOrFile(i, show_extension, true));
            foreach (var i in files)
                if (show_hiddin_files || (!show_hiddin_files && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                    if (i.Name.ToLower().Contains(filter_string))
                        directories_data.Add(new DirectoryOrFile(i, show_extension, false));
            if (directories_list.Items.Count > 0)
            {
                directories_list.ScrollIntoView(directories_list.Items[0]);
                info_textblock.Visibility = Visibility.Collapsed;
            }
            if (filter_string.Equals(string.Empty))
                info_textblock.Text = "This directory is empty.";
            else
                info_textblock.Text = "No items matsh your search.";
        }

        private void Up_Btm_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPath.ToLower() != "root")
            {
                controls_content.IsEnabled = false;
                if (Path.GetPathRoot(CurrentPath).Equals(CurrentPath))
                    RootItemBrowse(CurrentPath, "root", true, "", false);
                else
                    try
                    {
                        DirectoryInfo m = new DirectoryInfo(CurrentPath);
                        string new_path = m.Parent.FullName;
                        RootItemBrowse(CurrentPath, new_path, true, "", false);
                    }
                    catch (Exception ex)
                    { Messages.ShowMessage("Sorry we can not go up.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            }
            else
                Messages.ShowMessage("Sorry you are now on top level so you can not go up farther more.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            //string[] split_path = path.Split(Functions.spliter, StringSplitOptions.RemoveEmptyEntries);
            //if (!path.ToLower().Equals("computer"))
            //{
            //    if (split_path.Length == 1)
            //        new_path = "computer";
            //    else
            //        try
            //        { new_path = new DirectoryInfo(path).Parent.FullName; }
            //        catch (Exception ex)
            //        {
            //            Messages.ShowMessage("Sorry we can not go up.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
            //            return;
            //        }
            //    if (GetFilesAndDirectory.WorkerSupportsCancellation && GetFilesAndDirectory.IsBusy)
            //        GetFilesAndDirectory.CancelAsync();
            //    //next_address = new_path;
            //    ExplorePath(new_path, false, true);
            //    front.Clear();
            //    Forword.IsEnabled = false;
            //    SelectItem(old_path);
            //}
            //if (!CurrentPath.Equals("root"))
            //{
            //    string up_path = "root";
            //    try { up_path = new DirectoryInfo(current_path).Parent.FullName; }
            //    catch (Exception) { }
            //    if (!Path.GetPathRoot(CurrentPath).Equals(CurrentPath))
            //        RootItemBrowse(CurrentPath, up_path, "");
            //    else
            //        RootItemBrowse(CurrentPath, "root", "");
            //}
        }

        private void Back_Btm_Click(object sender, RoutedEventArgs e)
        {
            CurrentBackIndex++;
        }

        private void Forward_Btm_Click(object sender, RoutedEventArgs e)
        {
            CurrentBackIndex--;
        }

        private void HistoryLst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (history_list.SelectedIndex > -1)
            {
                update_history_index = false;
                CurrentBackIndex = history_list.SelectedIndex;
                update_history_index = true;
            }
        }

        private void AddressTextBlock_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!address_textblock.Text.ToLower().Equals(CurrentPath.ToLower()))
                    RootItemBrowse(CurrentPath, address_textblock.Text, true, "", false);
                if (directories_list.Visibility == Visibility.Visible)
                    directories_list.Focus();
                else
                    root_list.Focus();
            }
        }

        private void Refresh_btm_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void Explorer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsOpenned = true;
            OpennedMenu = 2;
        }

        private void RootList_KeyUp(object sender, KeyEventArgs e)
        {
            if (root_list.SelectedIndex > -1)
            {
                if (e.Key.Equals(Key.Space))
                {
                    IsOpenned = true;
                    OpennedMenu = 1;
                }
                else if (e.Key.Equals(Key.Enter))
                    RootItemBrowse("root", (root_list.SelectedItem as RootItem).toolTip, true, "", false);
                else if (e.Key == Key.F2)
                    RenameDrive();
            }
        }

        private void DirectoryList_KeyUp(object sender, KeyEventArgs e)
        {
            if (directories_list.SelectedIndex > -1)
            {
                if (e.Key.Equals(Key.Space))
                {
                    IsOpenned = true;
                    OpennedMenu = 1;
                }
                else if (e.Key.Equals(Key.Enter))
                {
                    if (directories_list.SelectedItems.Count < 30)
                    {
                        bool new_window = false;
                        List<DirectoryOrFile> selected_files = new List<DirectoryOrFile>();
                        foreach (DirectoryOrFile i in directories_list.SelectedItems)
                            selected_files.Add(i);
                        foreach (var i in selected_files)
                        {
                            if (i.isDirectory)
                                if (new_window)
                                    ExplorePath(i.FullPath);
                                else
                                {
                                    RootItemBrowse("root", (directories_list.SelectedItem as DirectoryOrFile).FullPath, true, "", false);
                                    new_window = true;
                                }
                            else
                                RunFile(i.FullPath);
                        }
                    }
                }
                else if (e.Key == Key.F2)
                    RenameFileInfo();
                else if (e.Key == Key.Delete)
                    details_delete_btm_Click(null, null);
            }
        }

        #region Functions

        private void RunFile(string file_name)
        {
            //add to history
            try
            {
                Process.Start(file_name);
            }
            catch (Exception ex)
            { Messages.ShowMessage(string.Format("Sorry we can not open this file '{0}'.", file_name), ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        bool IsSpecialDirectory(string new_path)
        {
            string[] SpecialDirectories = { "root", Environment.UserName.ToLower(), "desktop", "downloads", "documents", "music", "pictures", "videos", "program files", "program files (x86)", "fonts", "local", "history" };
            foreach (var i in SpecialDirectories)
                if (i.Equals(new_path))
                    return true;
            return false;
        }

        string GetSpecialDirectoryPath(string new_path)
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
                case SortItemsBy.Random:
                case SortItemsBy.RandomDescending:
                    return new Random((int)DateTime.UtcNow.Ticks).Next(0, 50000) % 2;
                default:
                case SortItemsBy.Name:
                    return x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.Size:
                    return x.Length.CompareTo(y.Length);
                case SortItemsBy.Type:
                    return x.Extension.ToLower().CompareTo(y.Extension.ToLower());
                case SortItemsBy.DateModified:
                    return x.LastWriteTimeUtc.Ticks.CompareTo(y.LastWriteTimeUtc.Ticks);
                case SortItemsBy.NameDescending:
                    return -1 * x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.SizeDescending:
                    return -1 * x.Length.CompareTo(y.Length);
                case SortItemsBy.TypeDescending:
                    return -1 * x.Extension.ToLower().CompareTo(y.Extension.ToLower());
                case SortItemsBy.DateModifiedDescending:
                    return -1 * x.LastWriteTimeUtc.Ticks.CompareTo(y.LastWriteTimeUtc.Ticks);
            }
        }

        Random rand = new Random((int)DateTime.UtcNow.Ticks);
        int DirectorySorter(DirectoryInfo x, DirectoryInfo y)
        {
            switch (sort_by)
            {
                case SortItemsBy.Random:
                case SortItemsBy.RandomDescending:
                    return rand.Next(0, 50000) % 2;
                case SortItemsBy.Name:
                case SortItemsBy.Size:
                case SortItemsBy.Type:
                    return x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.DateModified:
                    return x.LastWriteTimeUtc.Ticks.CompareTo(y.LastWriteTimeUtc.Ticks);
                case SortItemsBy.NameDescending:
                case SortItemsBy.SizeDescending:
                case SortItemsBy.TypeDescending:
                    return -1 * x.Name.ToLower().CompareTo(y.Name.ToLower());
                case SortItemsBy.DateModifiedDescending:
                    return -1 * x.LastWriteTimeUtc.Ticks.CompareTo(y.LastWriteTimeUtc.Ticks);
            }
            return 0;
        }

        private void Refresh()
        {
            if (CurrentPath == "root")
            {
                int index = root_list.SelectedIndex;
                LoadRoot(false);
                if (index < root_list.Items.Count && index > -1)
                    root_list.SelectedIndex = index;
            }
            else
            {
                int index = directories_list.SelectedIndex;
                RootItemBrowse(CurrentPath, CurrentPath, false, filter_TextBox.Text.ToLower(), false);
                if (index < directories_list.Items.Count && index > -1)
                    directories_list.SelectedIndex = index;
            }
        }

        BackgroundWorker show_exploring_progressbar;
        bool stop_exploring_progressbar;

        private void ShowProgress()
        {
            exploring_progress.Value = 0;
            stop_exploring_progressbar = false;
            show_exploring_progressbar = new BackgroundWorker();
            show_exploring_progressbar.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                int i = 2;
                Thread.Sleep(500);
                while (!stop_exploring_progressbar)
                {
                    if (i > 0)
                        show_exploring_progressbar.ReportProgress(i);
                    else
                        i += 2;
                    Thread.Sleep(100);
                    i *= 2;
                }
            };
            show_exploring_progressbar.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            { exploring_progress.Value += 100 / e.ProgressPercentage; };
            show_exploring_progressbar.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                exploring_progress.Value = 100;
                exploring_progress.Visibility = Visibility.Hidden;
            };
            show_exploring_progressbar.WorkerReportsProgress = show_exploring_progressbar.WorkerSupportsCancellation = true;
            show_exploring_progressbar.RunWorkerAsync();
            exploring_progress.Visibility = Visibility.Visible;
        }


        #endregion

        #endregion

        #region Main Menu Command

        private void NewCreateDirectory_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            if (CurrentPath != "root")
            {
                string new_name = null;
                new_name = new Rename().ShowWindow("new directory", false);
                MakeDirectory(new_name);
                Refresh();
            }
            else
                Messages.ShowMessage("You can not create directory here!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void NewCreateFile_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            if (CurrentPath != "root")
            {
                switch (Messages.ShowMessage("Select the type of file do you want to create:", "",
                    new ButtonContent(Functions.GetFileType(".txt"), ModrenMessageBitmapImage.Default, 1),
                    new ButtonContent(Functions.GetFileType(".html"), ModrenMessageBitmapImage.Default, 2),
                    new ButtonContent(Functions.GetFileType(".bmp"), ModrenMessageBitmapImage.Default, 3),
                    new ButtonContent(Functions.GetFileType(".docx"), ModrenMessageBitmapImage.Default, 4),
                    new ButtonContent(Functions.GetFileType(".odt"), ModrenMessageBitmapImage.Default, 5),
                    new ButtonContent(Functions.GetFileType(".pptx"), ModrenMessageBitmapImage.Default, 6),
                    new ButtonContent(Functions.GetFileType(".odp"), ModrenMessageBitmapImage.Default, 7),
                    new ButtonContent(Functions.GetFileType(".xlsx"), ModrenMessageBitmapImage.Default, 8),
                    new ButtonContent(Functions.GetFileType(".ods"), ModrenMessageBitmapImage.Default, 9),
                    new ButtonContent(Functions.GetFileType(".zip"), ModrenMessageBitmapImage.Default, 10),
                    new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 11)))
                {
                    case MyMessageBoxResult.Button1:
                        MakeFile(".txt");
                        break;
                    case MyMessageBoxResult.Button2:
                        MakeFile(".html");
                        break;
                    case MyMessageBoxResult.Button3:
                        MakeFile(".bmp");
                        break;
                    case MyMessageBoxResult.Button4:
                        MakeFile(".docx");
                        break;
                    case MyMessageBoxResult.Button5:
                        MakeFile(".odt");
                        break;
                    case MyMessageBoxResult.Button6:
                        MakeFile(".pptx");
                        break;
                    case MyMessageBoxResult.Button7:
                        MakeFile(".odp");
                        break;
                    case MyMessageBoxResult.Button8:
                        MakeFile(".xlsx");
                        break;
                    case MyMessageBoxResult.Button9:
                        MakeFile(".ods");
                        break;
                    case MyMessageBoxResult.Button10:
                        MakeFile(".zip");
                        break;
                }
            }
            else
                Messages.ShowMessage("You can not create any files here.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            //image for files
        }

        private void NewCreateShortcut_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            string primename = "";
            if (directories_list.Visibility == Visibility.Visible && directories_list.SelectedIndex > -1)
                primename = directories_data[directories_list.SelectedIndex].FullPath;
            else if (root_list.Visibility == Visibility.Visible && root_list.SelectedIndex > -1)
                primename = root_data[root_list.SelectedIndex].toolTip;
            New_Shortcut.CreateShortcut(primename, false);
        }

        private void NewWindow_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            new Directory_Explorer().ShowDialog();
        }

        private void NewWindowAdmin_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            ProcessStartInfo PSI = new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath);
            PSI.Verb = "runas";
            try
            { Process.Start(PSI); }
            catch (Exception ex)
            { Messages.ShowMessage("Error while openning the app.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        List<FileOperationItem> copy_list = new List<FileOperationItem>();
        bool has_pasted = true;

        private void EditCopy_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            if ((CurrentPath != "root" && directories_list.SelectedItems.Count > 0) || (CurrentPath == "root" && root_list.SelectedItems.Count > 0))
            {
                if (!has_pasted)
                {
                    if (Messages.ShowMessage("Do you want to append files to the list?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "In clipboard there is items have not been paste yet, if you click 'NO' thies items will be removed from clipboard.") == MyMessageBoxResult.No)
                        copy_list.Clear();
                }
                else
                    copy_list.Clear();
                if (CurrentPath != "root")
                {
                    foreach (DirectoryOrFile i in directories_list.SelectedItems)
                        if (i.fileinfo != null && i.fileinfo is FileInfo)
                            copy_list.Add(new FileOperationItem((FileInfo)i.fileinfo, false, i.fileinfo.FullName.Replace(CurrentPath, "")));
                        else if (i.fileinfo != null)
                            copy_list.Add(new FileOperationItem((DirectoryInfo)i.fileinfo, false, i.fileinfo.FullName.Replace(CurrentPath, "")));
                }
                else
                {
                    RootItem i;
                    if ((i = root_list.SelectedItem as RootItem).isDrive)
                    {
                        if (i.driveInfo.IsReady)
                            copy_list.Add(new FileOperationItem(i.driveInfo.RootDirectory, false, ""));
                    }
                    else
                        copy_list.Add(new FileOperationItem(i.Directory, false, i.Directory.FullName.Replace(i.Directory.Parent.FullName, "")));
                }
            }
            else
                Messages.ShowMessage("You did not selected any item to copy.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void EditCut_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            if (CurrentPath != "root")
            {
                if (directories_list.SelectedIndex > -1)
                {
                    if (!has_pasted)
                    {
                        if (Messages.ShowMessage("Do you want to append files to the list?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "In clipboard there is items have not been paste yet, if you click 'NO' thies items will be removed from clipboard.") == MyMessageBoxResult.No)
                            copy_list.Clear();
                    }
                    else
                        copy_list.Clear();
                    foreach (DirectoryOrFile i in directories_list.SelectedItems)
                        if (i.fileinfo != null && i.fileinfo is FileInfo)
                            copy_list.Add(new FileOperationItem((FileInfo)i.fileinfo, true, i.fileinfo.FullName.Replace(CurrentPath, "")));
                        else if (i.fileinfo != null)
                            copy_list.Add(new FileOperationItem((DirectoryInfo)i.fileinfo, true, i.fileinfo.FullName.Replace(CurrentPath, "")));
                }
                else
                    Messages.ShowMessage("You did not selected any item to move.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            }
            else
                Messages.ShowMessage("You can not move any item from here.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void EditPaste_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            if (copy_list.Count > 0)
            {
                if (CurrentPath == "root")
                {
                    if (root_list.SelectedIndex == -1)
                        Messages.ShowMessage("You can not paste any item here.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    else
                        new Operation().CopyDialogShow(copy_list, OperationType.Copy, (root_list.SelectedItem as RootItem).toolTip, false);
                }
                else
                {
                    if (directories_list.SelectedIndex == -1)
                        new Operation().CopyDialogShow(copy_list, OperationType.Copy, CurrentPath, false);
                    else if (directories_list.SelectedItems.Count == 1 && (directories_list.SelectedItem as DirectoryOrFile).isDirectory)
                        new Operation().CopyDialogShow(copy_list, OperationType.Copy, (directories_list.SelectedItem as DirectoryOrFile).FullPath, false);
                    else
                        Messages.ShowMessage("You can not paste here.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                }
            }
            else
                Messages.ShowMessage("Clipboard is empty.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "There is not any item to paste it!");
        }

        private void EditSelectAll_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            if (CurrentPath != "root")
                directories_list.SelectAll();
        }

        private void EditSelectNone_Btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            if (CurrentPath == "root")
                root_list.SelectedIndex = -1;
            else
                directories_list.SelectedIndex = -1;
        }

        private void EditInvertSelection_Btm_Click(object sender, RoutedEventArgs e)
        {
            //List<object> g = new List<object>();
            //g.AddRange(x => { !root_list.SelectedItems.Contains(x); });
            //if (CurrentPath == "root")
            //    foreach (var i in root_list.Items)
            //        if (root_list.SelectedItems.Contains(i))
            //            root_list.SelectedItems.Remove(i);
            //        else
            //            root_list.SelectedItems.Add(i);
            //else
            if (CurrentPath != "root")
            {
                if (directories_list.SelectedItems.Count == 0)
                {
                    directories_list.SelectAll();
                }

                else if (directories_list.SelectedItems.Count == directories_list.Items.Count)
                {
                    directories_list.SelectedIndex = -1;
                }

                else
                {
                    //foreach (var i in directories_data)
                    {
                        foreach (var item in directories_list.Items)
                            (item as ListViewItem).IsSelected = !(item as ListViewItem).IsSelected;
                        //if (directories_list.SelectedItems.Contains(i))
                        //    directories_list.SelectedItems.Remove(i);
                        //else
                        //    directories_list.SelectedItems.Add(i);
                    }
                }

            }
        }

        private void ViewShowHiddenFiles_Checked(object sender, RoutedEventArgs e)
        {
            show_hiddin_files = true;
        }

        private void ViewShowExtension_Checked(object sender, RoutedEventArgs e)
        {
            show_extension = true;
        }

        private void ViewShowExtension_Unchecked(object sender, RoutedEventArgs e)
        {
            show_extension = false;
        }

        private void ViewShowHiddenFiles_Unchecked(object sender, RoutedEventArgs e)
        {
            show_hiddin_files = false;
        }

        private void ViewSortbyName_Checked(object sender, RoutedEventArgs e)
        {
            sort_by = SortItemsBy.Name;
        }

        private void ViewSortbyType_Checked(object sender, RoutedEventArgs e)
        {
            sort_by = SortItemsBy.Type;
        }

        private void ViewSortbySize_Checked(object sender, RoutedEventArgs e)
        {
            sort_by = SortItemsBy.Size;
        }

        private void ViewSortbyDate_Checked(object sender, RoutedEventArgs e)
        {
            sort_by = SortItemsBy.DateModified;
        }

        private void ViewSortRandom_Checked(object sender, RoutedEventArgs e)
        {
            sort_by = SortItemsBy.Random;
        }

        private void ViewSortByAscending_Checked(object sender, RoutedEventArgs e)
        {
            if (sort_by >= SortItemsBy.RandomDescending)
                sort_by = sort_by - 100;
        }

        private void ViewSortByAscending_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sort_by < SortItemsBy.RandomDescending)
                sort_by = sort_by + 100;
        }

        private void ViewAsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListBox).SelectedIndex != 3 && (sender as ListBox).SelectedIndex > -1)
            {
                NotAvailyable_btm_Click(sender, null);
                (sender as ListBox).SelectedIndex = 3;
            }
        }

        #region Details Commands

        private void DetailsAnalizeDirectory_Click(object sender, RoutedEventArgs e)
        {
            //more than one directory
            if (CurrentPath != "root")
            {
                if (directories_list.SelectedItems.Count == 1)
                    new Big_File_Finder((directories_list.SelectedItem as DirectoryOrFile).FullPath).Show();
                else
                    NotAvailyable_btm_Click(sender, e);
            }
            else if (root_list.SelectedItems.Count == 1)
            {
                if ((root_list.SelectedItem as RootItem).isDrive)
                {
                    if ((root_list.SelectedItem as RootItem).driveInfo.IsReady)
                        new Big_File_Finder((root_list.SelectedItem as RootItem).toolTip).Show();
                    else
                        Messages.ShowMessage("Drive is not ready", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                }
                else
                    new Big_File_Finder((root_list.SelectedItem as RootItem).toolTip).Show();
            }
        }

        private void DetailsCreationChange_Click(object sender, RoutedEventArgs e)
        {
            OperationTree root = new OperationTree(null, false, "", OperationType.ChangeFileDate, null, false, "");
            if (CurrentPath != "root")
            {
                foreach (DirectoryOrFile i in directories_list.SelectedItems)
                    if (i.fileinfo is DirectoryInfo)
                        root.children.Add(new OperationTree((DirectoryInfo)i.fileinfo, false, "", OperationType.ChangeFileDate, root, false, ""));
                    else
                        root.children.Add(new OperationTree((FileInfo)i.fileinfo, false, "", OperationType.ChangeFileDate, root, ""));
            }
            ChangeFilesDate(FileDate.CreationDate, root);
        }

        private void DetailsModifiedChange_Click(object sender, RoutedEventArgs e)
        {
            OperationTree root = new OperationTree(null, false, "", OperationType.ChangeFileDate, null, false, "");
            if (CurrentPath != "root")
            {
                foreach (DirectoryOrFile i in directories_list.SelectedItems)
                    if (i.fileinfo is DirectoryInfo)
                        root.children.Add(new OperationTree((DirectoryInfo)i.fileinfo, false, "", OperationType.ChangeFileDate, root, false, ""));
                    else
                        root.children.Add(new OperationTree((FileInfo)i.fileinfo, false, "", OperationType.ChangeFileDate, root, ""));
            }
            ChangeFilesDate(FileDate.ModifiedDate, root);
        }

        private void DetailsLastAccessChange_Click(object sender, RoutedEventArgs e)
        {
            OperationTree root = new OperationTree(null, false, "", OperationType.ChangeFileDate, null, false, "");
            if (CurrentPath != "root")
            {
                foreach (DirectoryOrFile i in directories_list.SelectedItems)
                    if (i.fileinfo is DirectoryInfo)
                        root.children.Add(new OperationTree((DirectoryInfo)i.fileinfo, false, "", OperationType.ChangeFileDate, root, false, ""));
                    else
                        root.children.Add(new OperationTree((FileInfo)i.fileinfo, false, "", OperationType.ChangeFileDate, root, ""));
            }
            ChangeFilesDate(FileDate.LastAccessDate, root);
        }

        //private void ReverseAttribute_Click(object sender, RoutedEventArgs e)
        //{

        //}

        private void DetailsNormalAttribute_Checked(object sender, RoutedEventArgs e)
        {
            //details_attribute_reverse.Visibility = Visibility.Visible;
            ThicknessAnimation ani = new ThicknessAnimation(new Thickness(260, 27, 10, 0), new Thickness(360, 27, 10, 0), new Duration(new TimeSpan(500000)));
            details_attribute_read_only.IsChecked = details_attribute_hidden.IsChecked = details_attribute_archive.IsChecked = details_attribute_compressed.IsChecked = details_attribute_encrypted.IsChecked = details_attribute_system.IsChecked = false;
            ani.Completed += (object sender2, EventArgs e2) =>
            { details_attribute_grid.Visibility = Visibility.Collapsed; };
            details_attribute_grid.BeginAnimation(MarginProperty, ani);
        }

        private void DetailsOtherAttributes_Checked(object sender, RoutedEventArgs e)
        {
            details_attribute_grid.Visibility /*= details_attribute_reverse.Visibility */= Visibility.Visible;
            ThicknessAnimation ani = new ThicknessAnimation(new Thickness(360, 27, 10, 0), new Thickness(260, 27, 10, 0), new Duration(new TimeSpan(500000)));
            details_attribute_grid.BeginAnimation(MarginProperty, ani);
        }

        private void DetailsAnyAttributes_Checked(object sender, RoutedEventArgs e)
        {
            //details_attribute_reverse.Visibility = Visibility.Visible;
        }

        private void DetailsApplyAttrepute_Click(object sender, RoutedEventArgs e)
        {
            bool?[] attributes = { details_attribute_read_only.IsChecked, details_attribute_hidden.IsChecked, details_attribute_system.IsChecked, details_attribute_archive.IsChecked, details_attribute_compressed.IsChecked, details_attribute_encrypted.IsChecked, details_attribute_not_content_indexed.IsChecked, details_attribute_normal.IsChecked };
            List<FileOperationItem> list = new List<FileOperationItem>();
            if (CurrentPath != "root")
            {
                foreach (DirectoryOrFile i in directories_list.SelectedItems)
                {
                    if (i.fileinfo is FileInfo)
                        list.Add(new FileOperationItem(i.fileinfo as FileInfo, false, CalcFileAttributes(FileAttributes.Directory, i.fileinfo.Attributes, attributes)));
                    else if (i.fileinfo is DirectoryInfo)
                        list.Add(new FileOperationItem(i.fileinfo as DirectoryInfo, false, CalcFileAttributes(FileAttributes.Directory, i.fileinfo.Attributes, attributes)));
                }
            }
            else
                list.Add(new FileOperationItem(new DirectoryInfo((root_list.SelectedItem as RootItem).toolTip), false, CalcFileAttributes(FileAttributes.Directory, new DirectoryInfo((root_list.SelectedItem as RootItem).toolTip).Attributes, attributes)));
            new Operation().CopyDialogShow(list, OperationType.Attributes, "", true);
        }

        private void DetailsApplyAttreputeToAllContent_Click(object sender, RoutedEventArgs e)
        {
            if (calc_directory_size != null)
                if (calc_directory_size.IsBusy)
                    Messages.ShowMessage("Sorry, the app is busy now, please try again later.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                else
                {
                    bool?[] attributes = { details_attribute_read_only.IsChecked, details_attribute_hidden.IsChecked, details_attribute_system.IsChecked, details_attribute_archive.IsChecked, details_attribute_compressed.IsChecked, details_attribute_encrypted.IsChecked, details_attribute_not_content_indexed.IsChecked, details_attribute_normal.IsChecked };
                    List<FileOperationItem> list = new List<FileOperationItem>();

                    if (CurrentPath != "root")
                    {
                        foreach (DirectoryOrFile i in directories_list.SelectedItems)
                        {
                            if (i.fileinfo is FileInfo)
                                list.Add(new FileOperationItem(i.fileinfo as FileInfo, false, CalcFileAttributes(FileAttributes.Directory, i.fileinfo.Attributes, attributes)));
                            else if (i.fileinfo is DirectoryInfo)
                                list.Add(new FileOperationItem(i.fileinfo as DirectoryInfo, false, CalcFileAttributes(FileAttributes.Directory, i.fileinfo.Attributes, attributes)));
                        }
                    }
                    else
                        list.Add(new FileOperationItem(new DirectoryInfo((root_list.SelectedItem as RootItem).toolTip), false, CalcFileAttributes(FileAttributes.Directory, new DirectoryInfo((root_list.SelectedItem as RootItem).toolTip).Attributes, attributes)));

                    foreach (var i in directory_list)
                    {
                        i.short_path = CalcFileAttributes(FileAttributes.Directory, i.Item.Attributes, attributes);
                        list.Add(i);
                    }
                    foreach (var i in files_list)
                    {
                        i.short_path = CalcFileAttributes(0, i.Item.Attributes, attributes);
                        list.Add(i);
                    }
                    new Operation().CopyDialogShow(list, OperationType.Attributes, "", true);
                }
        }

        private void details_view_btm_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPath == "root" && root_list.SelectedItems.Count == 1)
            {
                if ((root_list.SelectedItem as RootItem).isDrive)
                    new Preview().ShowDialog(new DriveInfo((root_list.SelectedItem as RootItem).toolTip), ItemType.Drive);
                else
                    new Preview().ShowDialog(new DirectoryInfo((root_list.SelectedItem as RootItem).toolTip), ItemType.Directory);
            }
            else if (directories_list.SelectedItems.Count == 1)
            {
                if ((directories_list.SelectedItem as DirectoryOrFile).fileinfo is DirectoryInfo && (directories_list.SelectedItem as DirectoryOrFile).fileinfo.Exists)
                    new Preview().ShowDialog((directories_list.SelectedItem as DirectoryOrFile).fileinfo, ItemType.Directory);
                else if ((directories_list.SelectedItem as DirectoryOrFile).fileinfo is FileInfo && (directories_list.SelectedItem as DirectoryOrFile).fileinfo.Exists)
                    new Preview().ShowDialog((directories_list.SelectedItem as DirectoryOrFile).fileinfo, ItemType.File);
            }
            else if (directories_list.SelectedItems.Count > 1)
            {
                //same type view all music, picture, video or text 
            }
        }

        private void details_rename_btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            if (CurrentPath == "root")
                if ((root_list.SelectedItem as RootItem).isDrive)
                    RenameDrive();
                else
                    Messages.ShowMessage("You can not rename this directory.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else
                RenameFileInfo();
        }

        private void details_delete_btm_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            DeteteSelectetFiles();
        }

        private void details_move_to_btm_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPath != "root" && directories_list.SelectedItems.Count > 0)
            {
                IsOpenned = false;
                SelectFileDialog sfd = new SelectFileDialog();
                sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
                sfd.DialogTitle = "Move items to...";
                sfd.AddExtension = false;
                if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                    new Operation().CopyDialogShow(PrepareOperationList(OperationType.Copy, true), OperationType.Copy, sfd.SelectedItem, false);
            }
        }

        private void details_copy_to_btm_Click(object sender, RoutedEventArgs e)
        {
            if ((CurrentPath != "root" && directories_list.SelectedItems.Count > 0)||(CurrentPath == "root" && root_list.SelectedItems.Count > 0))
            {
                SelectFileDialog sfd = new SelectFileDialog();
                sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
                sfd.DialogTitle = "Copy items to...";
                sfd.AddExtension = false;
                if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                    new Operation().CopyDialogShow(PrepareOperationList(OperationType.Copy, false), OperationType.Copy, sfd.SelectedItem, false);
            }
        }

        private void details_encrypt_to_btm_Click(object sender, RoutedEventArgs e)
        {
            if (calc_directory_size != null)
                if (calc_directory_size.IsBusy)
                    Messages.ShowMessage("Sorry, the app is busy now, please try again later.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                else
                {
                    List<string> tmp = new List<string>();
                    foreach (var i in files_list)
                        tmp.Add(i.Item.FullName);
                    new Files_Encrepter(tmp.ToArray(), true).Show();
                }
        }

        private void details_destroy_btm_Click(object sender, RoutedEventArgs e)
        {
            bool destroy_files = false;
            if (CurrentPath == "root")
            {
                if (root_list.SelectedIndex != -1)
                    if ((root_list.SelectedItem as RootItem).isDrive)
                    {
                        if ((root_list.SelectedItem as RootItem).driveInfo.Name.ToLower() == Functions.SystemDrive.ToLower())
                            destroy_files = true;
                        else
                        {
                            switch (Messages.ShowMessage("What do you wat to do:", "",
                               new ButtonContent("destroy all files in the drive", ModrenMessageBitmapImage.Default, 1),
                               new ButtonContent("destroy deleted files in the drive", ModrenMessageBitmapImage.Default, 2),
                               new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 3)))
                            {
                                case MyMessageBoxResult.Button1:
                                    destroy_files = true;
                                    break;
                                case MyMessageBoxResult.Button2:
                                    new File_Destroyer((root_list.SelectedItem as RootItem).driveInfo, false).Show();
                                    break;
                            }
                        }
                    }
                    else
                        Messages.ShowMessage("This directory is a part of operating system you can not destroy it.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            }
            if (CurrentPath != "root" || destroy_files)
            {
                if (calc_directory_size != null)
                    if (calc_directory_size.IsBusy)
                        Messages.ShowMessage("Sorry, the app is busy now, please try again later.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    else
                    {
                        List<string> tmp = new List<string>();
                        foreach (var i in files_list)
                            tmp.Add(i.Item.FullName);
                        new File_Destroyer(tmp.ToArray(), true).Show();
                    }
            }
        }

        private void details_find_extension_btm_Click(object sender, RoutedEventArgs e)
        {
            bool go = false;
            if ((CurrentPath == "root" && root_list.SelectedIndex > -1 && (root_list.SelectedItem as RootItem).isDrive) && (root_list.SelectedItem as RootItem).driveInfo.Name.ToLower() != Functions.SystemDrive.ToLower())
                go = true;
            else
                Messages.ShowMessage("the item that you selected is a part of operating system and you can not modify it.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            if (go || directories_list.SelectedItems.Count > 0)
            {
                if (calc_directory_size != null)
                    if (calc_directory_size.IsBusy)
                        Messages.ShowMessage("Sorry, the app is busy now, please try again later.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    else
                    {
                        List<string> tmp = new List<string>();
                        foreach (var i in files_list)
                            tmp.Add(i.Item.FullName);
                        new Renamer().ShowWindow(tmp, "", ExtentionChanger.ExtentionSuggester, false);
                    }
            }
        }

        private void details_find_duplicate_btm_Click(object sender, RoutedEventArgs e)
        {
            List<string> items = new List<string>();
            if (CurrentPath == "root")
            {
                if (root_list.SelectedIndex > -1)
                    items.Add((root_list.SelectedItem as RootItem).toolTip);
            }
            else if (directories_list.SelectedItems.Count > 0)
                foreach (DirectoryOrFile i in directories_list.SelectedItems)
                    items.Add(i.FullPath);
            if (items.Count > 0)
                new Identical_File_Remover(items).Show();

        }

        private void details_find_empty_btm_Click(object sender, RoutedEventArgs e)
        {
            if ("root" == CurrentPath)
            {
                if (root_list.SelectedIndex > -1)
                    new Empty_Folder_Remover((root_list.SelectedItem as RootItem).toolTip).Show();
            }
            else if (directories_list.SelectedItems.Count > 0)
            {
                if (directories_list.SelectedItems.Count == 1)
                    new Empty_Folder_Remover((directories_list.SelectedItem as DirectoryOrFile).FullPath).Show();
                else
                    NotAvailyable_btm_Click(sender, e);
            }
        }

        private void tools_open_Btm_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            IsOpenned = false;
            Window win = null;
            switch ((sender as ListBoxItem).Tag.ToString())
            {
                case "1":
                    win = new File_Compresser();
                    break;
                case "2":
                    win = new System_Monitor();
                    break;
                case "3":
                    win = new Renamer();
                    break;
                case "4":
                    win = new Identical_File_Remover();
                    break;
                case "5":
                    win = new Big_File_Finder();
                    break;
                case "6":
                    win = new Empty_Folder_Remover();
                    break;
                case "7":
                    win = new File_Destroyer();
                    break;
                case "8":
                    win = new Files_Encrepter();
                    break;
                case "9":
                    win = new Virtual_Machine();
                    break;
                case "100":
                    Messages.ShowMessage("Thank you for using FILE MANAGER", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                    break;
            }
            if (win != null)
            {
                win.Show();
                win.Focus();
            }
        }

        #endregion Details Commands

        #region Functions

        private void MakeDirectory(string new_name)
        {
            if (CurrentPath.Length > 0)
            {
                string dir_name = null;
                if (new_name != null && new_name != "")
                {
                    if (Directory.Exists(CurrentPath + (CurrentPath[CurrentPath.Length - 1] == '\\' ? "" : "\\") + new_name))
                    {
                        int i = 1;
                        while (Directory.Exists(CurrentPath + (CurrentPath[CurrentPath.Length - 1] == '\\' ? "" : "\\") + new_name + " (" + i + ")")) i++;
                        if (Messages.ShowMessage("Directory with same name is exist.", ModrenMessageButton.YesNo, ModrenMessageImage.Rename, ModrenMessageSound.Question, true, "Do you want to create directory '" + new_name + " (" + i + ")' instat of '" + new_name + "'") == MyMessageBoxResult.Yes)
                            dir_name = CurrentPath + (CurrentPath[CurrentPath.Length - 1] == '\\' ? "" : "\\") + new_name + " (" + i + ")";
                    }
                    else
                        dir_name = CurrentPath + (CurrentPath[CurrentPath.Length - 1] == '\\' ? "" : "\\") + new_name;
                    try { if (dir_name != null) Directory.CreateDirectory(dir_name); }
                    catch (Exception ex) { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
            }
            else
                Messages.ShowMessage("Something wrong.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Sorry, we found error while creating directory please refrsh window then try again.");
        }

        private void MakeFile(string extension)
        {
            string new_name = null;
            new_name = new Rename().ShowWindow("new file", false);
            string file_name = null; ;
            if (new_name != null)
            {
                if (File.Exists(path + "\\" + new_name + extension))
                {
                    int i = 1;
                    while (File.Exists(path + "\\" + new_name + " (" + i + ")" + extension)) i++;
                    if (Messages.ShowMessage("File with name '" + new_name + extension + "' is already exist.", ModrenMessageButton.YesNo, ModrenMessageImage.Rename, ModrenMessageSound.Question, true, string.Format("New File", "Do you want to create file '{0} ({1}){2}' " + new_name, i, extension) + string.Format("instat of '{0}'", new_name + extension)) == MyMessageBoxResult.Yes)
                        file_name = path + "\\" + new_name + " (" + i + ")" + extension;
                }
                else
                    file_name = path + "\\" + new_name + extension;
                try { if (file_name != null) File.Create(file_name); }
                catch (Exception ex) { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                //Refresh();
            }
        }

        private void ChangeFilesDate(FileDate date_type, OperationTree root)
        {
            if (directories_list.IsVisible && directories_list.SelectedIndex > -1)
            {
                DateTime base_date = date_type == FileDate.CreationDate ? directories_data[directories_list.SelectedIndex].fileinfo.CreationTimeUtc : date_type == FileDate.ModifiedDate ? directories_data[directories_list.SelectedIndex].fileinfo.LastWriteTimeUtc : directories_data[directories_list.SelectedIndex].fileinfo.LastAccessTimeUtc;
                DateTime? date = new ChangeDate().ShowDialog(base_date);
                if (date.HasValue)
                {
                    OperationTree unsucess = new OperationTree(null, false, date_type, OperationType.ChangeFileDate, null, false, string.Format("we can not change {0} time for these items.", date_type == FileDate.CreationDate ? "creation" : date_type == FileDate.ModifiedDate ? "last modified" : "last access"));
                    foreach (var i in root.children)
                    {
                        try
                        {
                            switch (date_type)
                            {
                                case FileDate.CreationDate:
                                    i.item.CreationTimeUtc = date.Value;
                                    break;
                                case FileDate.ModifiedDate:
                                    i.item.LastWriteTimeUtc = date.Value;
                                    break;
                                case FileDate.LastAccessDate:
                                    i.item.LastAccessTimeUtc = date.Value;
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (i.item is DirectoryInfo)
                                unsucess.children.Add(new OperationTree((DirectoryInfo)i.item, false, "", OperationType.ChangeFileDate, unsucess, false, ex.Message));
                            else
                                unsucess.children.Add(new OperationTree((FileInfo)i.item, false, "", OperationType.ChangeFileDate, unsucess, ex.Message));
                        }
                    }
                    while (unsucess.children.Count > 0)
                    {
                        root.children.Clear();
                        File_s_Transferring_Error n = new File_s_Transferring_Error(unsucess, "We can not change creation time for these files.");
                        n.ShowThisDialog();
                        foreach (var i in unsucess.children)
                            if (i.user_option == OperationOption.Retry)
                                root.children.Add(i);
                        unsucess.children.Clear();
                        foreach (var i in root.children)
                        {
                            try
                            {
                                switch (date_type)
                                {
                                    case FileDate.CreationDate:
                                        i.item.CreationTimeUtc = date.Value;
                                        break;
                                    case FileDate.ModifiedDate:
                                        i.item.LastWriteTimeUtc = date.Value;
                                        break;
                                    case FileDate.LastAccessDate:
                                        i.item.LastAccessTimeUtc = date.Value;
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (i.item is DirectoryInfo)
                                    unsucess.children.Add(new OperationTree((DirectoryInfo)i.item, false, "", OperationType.ChangeFileDate, unsucess, false, ex.Message));
                                else
                                    unsucess.children.Add(new OperationTree((FileInfo)i.item, false, "", OperationType.ChangeFileDate, unsucess, ex.Message));
                            }
                        }
                    }
                    switch (date_type)
                    {
                        case FileDate.CreationDate:
                            details_creation_time.Text = "creation time: " + Functions.OrginizeDate(directories_data[directories_list.SelectedIndex].fileinfo.CreationTimeUtc);
                            break;
                        case FileDate.ModifiedDate:
                            details_modified_time.Text = "last modified time: " + Functions.OrginizeDate(directories_data[directories_list.SelectedIndex].fileinfo.CreationTimeUtc);
                            break;
                        case FileDate.LastAccessDate:
                            details_last_accessed_time.Text = "last accessed time: " + Functions.OrginizeDate(directories_data[directories_list.SelectedIndex].fileinfo.CreationTimeUtc);
                            break;
                    }
                }
            }
        }

        FileAttributes CalcFileAttributes(FileAttributes start, FileAttributes default_attributes, bool?[] selected_attributes)
        {
            FileAttributes new_attributes = start;
            if (selected_attributes[0] == true)
                new_attributes += 1;
            if (selected_attributes[1] == true)
                new_attributes += 2;
            if (selected_attributes[2] == true)
                new_attributes += 4;
            if (selected_attributes[3] == true)
                new_attributes += 32;
            if (default_attributes.HasFlag(FileAttributes.Temporary))
                new_attributes += 256;
            if (default_attributes.HasFlag(FileAttributes.SparseFile))
                new_attributes += 512;
            if (default_attributes.HasFlag(FileAttributes.ReparsePoint))
                new_attributes += 1024;
            if (selected_attributes[4] == true)
                new_attributes += 2048;
            if (default_attributes.HasFlag(FileAttributes.Offline))
                new_attributes += 4096;
            if (selected_attributes[5] == true)
                new_attributes += 16384;
            if (selected_attributes[6] == true)
                new_attributes += 8192;
            if (selected_attributes[7].Value)
                new_attributes = FileAttributes.Normal;
            return new_attributes;
        }

        private void RenameDrive()
        {
            if (root_list.SelectedIndex != -1)
                try
                {
                    string new_name = new Rename().ShowWindow((root_list.SelectedItem as RootItem).driveInfo.VolumeLabel, false);
                    if (new_name != null)
                        (root_list.SelectedItem as RootItem).driveInfo.VolumeLabel = new_name;
                }
                catch (Exception ex)
                { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void RenameFileInfo()
        {
            if (CurrentPath != "root" && directories_list.SelectedItems.Count > 0)
            {
                string new_name = new Rename().ShowWindow((directories_list.SelectedItem as DirectoryOrFile).fileinfo.FullName, true);
                if (new_name != null)
                {
                    if (CurrentPath != "root" && directories_list.SelectedItems.Count > 0)
                    {
                        List<FileOperationItem> to_rename_list = new List<FileOperationItem>();
                        foreach (DirectoryOrFile i in directories_list.SelectedItems)
                            if (i.fileinfo is DirectoryInfo)
                                to_rename_list.Add(new FileOperationItem((DirectoryInfo)i.fileinfo, false, new_name));
                            else
                                to_rename_list.Add(new FileOperationItem((FileInfo)i.fileinfo, false, new_name));
                        new Operation().CopyDialogShow(to_rename_list, OperationType.Rename, new_name, true);
                    }
                    Refresh();
                }
            }
        }

        private void DeteteSelectetFiles()
        {
            if (CurrentPath != "root")
            {
                if (directories_list.SelectedItems.Count > 0)
                {
                    bool delete = false;
                    if (directories_list.SelectedItems.Count > 1)
                    {
                        StringBuilder items_name = new StringBuilder();
                        foreach (var i in directories_list.SelectedItems)
                            items_name.AppendLine((i as DirectoryOrFile).FullPath);
                        if (Messages.ShowMessage(string.Format("Are you sure you want to permanently delete these {0} items", directories_list.SelectedItems.Count), ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, items_name.ToString()) == MyMessageBoxResult.Yes)
                            delete = true;
                    }
                    else if (Messages.ShowMessage("Are you sure you want to permanently delete this item", ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, Messages.GetItemInfo((directories_list.SelectedItem as DirectoryOrFile).fileinfo)) == MyMessageBoxResult.Yes)
                        delete = true;
                    if (delete)
                        new Operation().CopyDialogShow(PrepareOperationList(OperationType.Delete, false), OperationType.Delete, "", false);
                }
            }
            else
                Messages.ShowMessage("You can not delete any items from here.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private List<FileOperationItem> PrepareOperationList(OperationType op_type, bool is_cut)
        {
            List<FileOperationItem> list = new List<FileOperationItem>();
            if (CurrentPath != "root" && directories_list.SelectedItems.Count > 0)
            {
                foreach (DirectoryOrFile i in directories_list.SelectedItems)
                {
                    if (i.isDirectory)
                        list.Add(new FileOperationItem(i.fileinfo as DirectoryInfo, is_cut, op_type == OperationType.Delete ? "" : i.fileinfo.Name));
                    else
                        list.Add(new FileOperationItem(i.fileinfo as FileInfo, is_cut, op_type == OperationType.Delete ? "" : i.fileinfo.Name));
                }
            }
            else if (op_type == OperationType.Copy && CurrentPath == "root" && root_list.SelectedIndex > -1)
                list.Add(new FileOperationItem((root_list.SelectedItem as RootItem).Directory as DirectoryInfo, false, (root_list.SelectedItem as RootItem).Directory.Name));
            return list;
        }

        #endregion

        #endregion

        #region Details

        bool cancel_calc_dir_size = false;

        BackgroundWorker calc_directory_size = null;
        List<FileOperationItem> files_list = new List<FileOperationItem>();
        List<FileOperationItem> directory_list = new List<FileOperationItem>();

        private void ClaculateItemsSize(List<DirectoryInfo> directories, List<FileInfo> files, bool is_drive)
        {
            details_calc_size_pb.Visibility = Visibility.Visible;
            details_calc_size_pb.IsIndeterminate = true;
            double drive_used_space = double.Epsilon;
            DriveInfo root;
            try
            {
                if (directories.Count > 0)
                    drive_used_space = ((root = new DriveInfo(directories[0].Root.FullName)).TotalSize - root.TotalFreeSpace);
                else if (files.Count > 0)
                    drive_used_space = ((root = new DriveInfo(files[0].Directory.Root.FullName)).TotalSize - root.TotalFreeSpace);
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not calc items size", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                return;
            }
            cancel_calc_dir_size = false;
            string text = details_title.Text;
            long size = 0, size_on_disk = 0;
            calc_directory_size = new BackgroundWorker();
            calc_directory_size.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                Thread progress = new Thread(() =>
                {
                    while (calc_directory_size.IsBusy)
                    {
                        try { calc_directory_size.ReportProgress(0); }
                        catch (Exception) { }
                        if (cancel_calc_dir_size) return;
                        Thread.Sleep(250);
                    }
                });
                progress.Start();
                foreach (var i in files)
                {
                    if (cancel_calc_dir_size)
                        return;
                    files_list.Add(new FileOperationItem(i, false, ""));
                    size += i.Length;
                    size_on_disk += Functions.CalcFileSizeOnDisk(i);
                }
                foreach (var i in directories)
                {
                    if (cancel_calc_dir_size)
                        return;
                    directory_list.Add(new FileOperationItem(i, false, null));
                    try
                    { GetSubFilesAndDirectories(i, ref size, ref size_on_disk); }
                    catch (Exception) { }
                }
            };

            calc_directory_size.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                ApplySizeResult(text, is_drive, drive_used_space, size, size_on_disk);
            };

            calc_directory_size.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                ApplySizeResult(text, is_drive, drive_used_space, size, size_on_disk);
                details_calc_size_pb.Visibility = Visibility.Collapsed;
                details_calc_size_pb.IsIndeterminate = false;
            };

            calc_directory_size.WorkerReportsProgress = true;
            calc_directory_size.RunWorkerAsync();
        }

        private void GetSubFilesAndDirectories(DirectoryInfo directory, ref long size, ref long size_on_disk)
        {
            if (cancel_calc_dir_size)
                return;
            if (!Classes.JunctionPoint.Exists(directory.FullName))
            {
                var d = directory.GetDirectories();
                foreach (var i in d)
                {
                    directory_list.Add(new FileOperationItem(i, false, null));
                    try
                    { GetSubFilesAndDirectories(i, ref size, ref size_on_disk); }
                    catch (Exception) { }
                }
                var f = directory.GetFiles();
                foreach (var i in f)
                {
                    files_list.Add(new FileOperationItem(i, false, ""));
                    size += i.Length;
                    size_on_disk += Functions.CalcFileSizeOnDisk(i);
                }
            }
            //else
            //{
            //    string g = directory.FullName;
            //}
        }

        private void ApplySizeResult(string text, bool is_drive, double drive_used_space, long size, long size_on_disk)
        {
            details_total_count.Text = "contains ";
            details_total_count.Text += (directory_list.Count > 0 ? directory_list.Count + (directory_list.Count == 1 ? " directory " : " directories ") : "");
            if (details_total_count.Text != "contains " && files_list.Count > 0)
                details_total_count.Text += "and ";
            details_total_count.Text += (files_list.Count > 0 ? files_list.Count + (files_list.Count == 1 ? " file " : " files ") : "");
            if (is_drive)
                details_total_count.Text += string.Format(" ({0})", Functions.FileSize(size, 2));
            if (!is_drive)
            {
                details_size.Text = "total size " + Functions.FileSize(size_on_disk, 2) + " on disk (" + Math.Round(size_on_disk * 100.0 / drive_used_space, 3) + "%)";
                details_title.Text = text + string.Format(" ({0})", Functions.FileSize(size, 2));
            }
        }

        private void CalcInfoAboutItems()
        {
            RestDetailsToDefault();
            if (root_list.SelectedIndex == -1 && directories_list.SelectedIndex == -1 && CurrentPath == "root")
            {
                details_selected.Text = "0 items selected";
                details_location.Text = "current path: " + CurrentPath;
                details_total_count.Text = "contains: 0 items";
                details_size.Text = "total size: 0 byte on disk (0.0%)";
                details_calc_size_pb.Visibility = details_attribute.Visibility = details_tools.Visibility = details_date.Visibility = Visibility.Collapsed;
                view_details_btm.IsEnabled = false;
            }
            else
            {
                List<DirectoryInfo> directories = new List<DirectoryInfo>();
                List<FileInfo> files = new List<FileInfo>();
                int files_count = 0, directories_count = 0;
                FileCategory files_extention = FileCategory.Unknown;
                long creation_time, modified_time, access_time;

                if (CurrentPath == "root")
                {
                    if ((root_list.SelectedItems.Count == 1))
                    {
                        RootItem selected_item;
                        if ((selected_item = root_list.SelectedItem as RootItem).isDrive)
                        {
                            details_date.Visibility = Visibility.Collapsed;
                            details_title.Text = selected_item.FullPath;
                            details_selected.Text = "1 item selected (disk drive)";
                            details_location.Text = "item on: root";
                            files_extention = FileCategory.Drive;
                            if (selected_item.driveInfo.IsReady)
                            {
                                details_title.Text += (" (" + Functions.FileSize(selected_item.driveInfo.TotalSize, 2) + ")");
                                details_size.Text = "free space: " + Functions.FileSize(selected_item.driveInfo.TotalFreeSpace, 2) + (Math.Round((100.0 * selected_item.driveInfo.TotalFreeSpace) / selected_item.driveInfo.TotalSize, 2));
                                DirectoryInfo drive_directory = new DirectoryInfo(selected_item.driveInfo.Name);
                                directories.Add(new DirectoryInfo(selected_item.driveInfo.Name));
                                details_attribute_other.IsChecked = true;
                                details_attribute_normal.IsEnabled = details_attribute_system.IsEnabled = details_attribute_read_only.IsEnabled = details_attribute_hidden.IsEnabled = details_attribute_archive.IsEnabled = false;
                                details_attribute_compressed.IsChecked = drive_directory.Attributes.HasFlag(FileAttributes.Compressed);
                                details_attribute_encrypted.IsChecked = drive_directory.Attributes.HasFlag(FileAttributes.Encrypted);
                                details_attribute_not_encrypted_compressed.IsChecked = !(drive_directory.Attributes.HasFlag(FileAttributes.Compressed) || drive_directory.Attributes.HasFlag(FileAttributes.Encrypted));
                                details_attribute_not_content_indexed.IsChecked = drive_directory.Attributes.HasFlag(FileAttributes.NotContentIndexed);
                                details_calc_hash_btm.Visibility = details_move_to_btm.Visibility = details_delete_btm.Visibility = Visibility.Collapsed;
                                ClaculateItemsSize(directories, files, true);
                            }
                            else
                            {
                                details_size.Text = "drive is not ready";
                                details_total_count.Visibility = details_tools.Visibility = details_attribute.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            //general
                            details_location.Text = "item in: " + selected_item.Directory.Parent.FullName;
                            files_extention = FileCategory.Directory;
                            details_selected.Text = "1 directory selected";
                            details_title.Text = selected_item.Directory.Name;
                            DirectoryInfo selected_directory = (root_list.SelectedItem as RootItem).Directory;
                            details_creation_time.Text = "creation time: " + Functions.OrginizeDate(selected_directory.CreationTimeUtc);
                            details_modified_time.Text = "last modified time: " + Functions.OrginizeDate(selected_directory.LastWriteTimeUtc);
                            details_last_accessed_time.Text = "last accessed time: " + Functions.OrginizeDate(selected_directory.LastAccessTimeUtc);
                            directories.Add((root_list.SelectedItem as RootItem).Directory);
                            ClaculateItemsSize(directories, files, false);

                            //attribute
                            details_attribute_normal.IsEnabled = false;
                            details_attribute_other.IsChecked = true;
                            details_attribute_read_only.IsChecked = selected_directory.Attributes.HasFlag(FileAttributes.ReadOnly);
                            details_attribute_hidden.IsChecked = selected_directory.Attributes.HasFlag(FileAttributes.Hidden);
                            details_attribute_archive.IsChecked = selected_directory.Attributes.HasFlag(FileAttributes.Archive);
                            details_attribute_compressed.IsChecked = selected_directory.Attributes.HasFlag(FileAttributes.Compressed);
                            details_attribute_encrypted.IsChecked = selected_directory.Attributes.HasFlag(FileAttributes.Encrypted);
                            if (selected_directory.Attributes.HasFlag(FileAttributes.Compressed) == false && selected_directory.Attributes.HasFlag(FileAttributes.Encrypted) == false)
                                details_attribute_not_encrypted_compressed.IsChecked = true;
                            details_attribute_system.IsChecked = selected_directory.Attributes.HasFlag(FileAttributes.System);
                            details_attribute_not_content_indexed.IsChecked = selected_directory.Attributes.HasFlag(FileAttributes.NotContentIndexed);

                            //tools
                            details_calc_hash_btm.Visibility = Visibility.Collapsed;
                        }
                    }
                    directories_count = root_list.SelectedItems.Count;
                }
                else
                {
                    //if (directories_list.SelectedItems.Count > 0)
                    {
                        DirectoryOrFile first_item;
                        if (directories_list.SelectedItems.Count > 0)
                            first_item = directories_list.SelectedItem as DirectoryOrFile;
                        else
                            first_item = new DirectoryOrFile(new DirectoryInfo(CurrentPath), show_extension, true);
                        details_location.Text = (directories_list.SelectedItems.Count > 1 ? "all items in: " : "item in: ") + CurrentPath;
                        creation_time = first_item.fileinfo.CreationTimeUtc.Ticks;
                        modified_time = first_item.fileinfo.LastWriteTimeUtc.Ticks;
                        access_time = first_item.fileinfo.LastAccessTimeUtc.Ticks;
                        bool same_creation = true, same_modified = true, same_access = true;
                        bool? read_only = first_item.fileinfo.Attributes.HasFlag(FileAttributes.ReadOnly),
                            hidden = first_item.fileinfo.Attributes.HasFlag(FileAttributes.Hidden),
                            system = first_item.fileinfo.Attributes.HasFlag(FileAttributes.System),
                            directory_att = first_item.fileinfo.Attributes.HasFlag(FileAttributes.Directory),
                            archive = first_item.fileinfo.Attributes.HasFlag(FileAttributes.Archive),
                             normal = first_item.fileinfo.Attributes.HasFlag(FileAttributes.Normal),
                              compressed = first_item.fileinfo.Attributes.HasFlag(FileAttributes.Compressed),
                        not_content_indexed = first_item.fileinfo.Attributes.HasFlag(FileAttributes.NotContentIndexed),
                        encrypted = first_item.fileinfo.Attributes.HasFlag(FileAttributes.Encrypted);
                        if (directories_list.SelectedItems.Count > 0)
                        {
                            foreach (DirectoryOrFile i in directories_list.SelectedItems)
                            {
                                if (read_only != null) read_only = i.fileinfo.Attributes.HasFlag(FileAttributes.ReadOnly) == read_only ? read_only : null;
                                if (hidden != null) hidden = i.fileinfo.Attributes.HasFlag(FileAttributes.Hidden) == hidden ? hidden : null;
                                if (system != null) system = i.fileinfo.Attributes.HasFlag(FileAttributes.System) == system ? system : null;
                                if (directory_att != null) directory_att = i.fileinfo.Attributes.HasFlag(FileAttributes.Directory) == directory_att ? directory_att : null;
                                if (archive != null) archive = i.fileinfo.Attributes.HasFlag(FileAttributes.Archive) == archive ? archive : null;
                                if (normal != null) normal = i.fileinfo.Attributes.HasFlag(FileAttributes.Normal) == normal ? normal : null;
                                if (compressed != null) compressed = i.fileinfo.Attributes.HasFlag(FileAttributes.Compressed) == compressed ? compressed : null;
                                if (not_content_indexed != null) not_content_indexed = i.fileinfo.Attributes.HasFlag(FileAttributes.NotContentIndexed) == not_content_indexed ? not_content_indexed : null;
                                if (encrypted != null) encrypted = i.fileinfo.Attributes.HasFlag(FileAttributes.Encrypted) == encrypted ? encrypted : null;

                                same_creation = same_creation && i.fileinfo.CreationTimeUtc.Ticks == creation_time;
                                same_modified = same_modified && i.fileinfo.LastWriteTimeUtc.Ticks == modified_time;
                                same_access = same_access && i.fileinfo.LastAccessTimeUtc.Ticks == access_time;
                                if (i.isDirectory)
                                {
                                    directories_count++;
                                    directories.Add(i.fileinfo as DirectoryInfo);
                                }
                                else if (!i.isDrive)
                                {
                                    files_count++;
                                    files.Add(i.fileinfo as FileInfo);
                                }
                            }
                        }
                        else
                        {
                            same_creation = same_modified = same_access = true;
                            directories_count = 1;
                            directories.Add(first_item.fileinfo as DirectoryInfo);
                        }
                        if (files_count + directories_count == 1)
                            details_title.Text = first_item.fileinfo.Name;
                        else
                            details_title.Text = (files_count + directories_count) + " items selected";
                        ClaculateItemsSize(directories, files, false);
                        details_creation_time.Text = "creation time: " + (same_creation ? Functions.OrginizeDate(first_item.fileinfo.CreationTimeUtc) : "muliple-values");
                        details_modified_time.Text = "last modified time: " + (same_creation ? Functions.OrginizeDate(first_item.fileinfo.LastWriteTimeUtc) : "muliple-values");
                        details_last_accessed_time.Text = "last accessed time: " + (same_creation ? Functions.OrginizeDate(first_item.fileinfo.LastAccessTimeUtc) : "muliple-values");
                        //attribute
                        if (directory_att != false)
                        {
                            details_attribute_normal.IsEnabled = false;
                            details_attribute_other.IsChecked = true;
                            //details_attribute_grid.Visibility = Visibility.Visible;
                        }
                        if (normal == true)
                            details_attribute_normal.IsChecked = normal;
                        else
                        {
                            details_attribute_read_only.IsThreeState = read_only == null;
                            details_attribute_hidden.IsThreeState = hidden == null;
                            details_attribute_system.IsThreeState = system == null;
                            details_attribute_archive.IsThreeState = archive == null;
                            details_attribute_compressed.IsThreeState = compressed == null;
                            details_attribute_encrypted.IsThreeState = encrypted == null;
                            details_attribute_not_content_indexed.IsThreeState = not_content_indexed == null;
                            details_attribute_read_only.IsChecked = read_only;
                            details_attribute_hidden.IsChecked = hidden;
                            details_attribute_archive.IsChecked = archive;
                            details_attribute_compressed.IsChecked = compressed;
                            details_attribute_encrypted.IsChecked = encrypted;
                            if (compressed == false && encrypted == false)
                                details_attribute_not_encrypted_compressed.IsChecked = true;
                            details_attribute_system.IsChecked = system;
                            details_attribute_not_content_indexed.IsChecked = not_content_indexed;
                        }

                        //tools
                        if (directory_att == true)
                            details_calc_hash_btm.Visibility = Visibility.Collapsed;
                        else if (directory_att == false)
                        {
                            view_details_btm.IsEnabled = false;
                            details_attribute_apply_to_all.Visibility = Visibility.Collapsed;
                            if (files_count > 1)
                            {
                                if (!(files_extention == FileCategory.Audio || files_extention == FileCategory.Html || files_extention == FileCategory.Image || files_extention == FileCategory.Vedio))
                                    details_view_btm.Visibility = Visibility.Collapsed;
                                details_calc_hash_btm.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                details_find_empty_btm.Visibility = details_find_dublicate_btm.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            details_view_btm.Visibility = details_calc_hash_btm.Visibility = details_find_dublicate_btm.Visibility = details_find_empty_btm.Visibility = Visibility.Collapsed;
                        }
                        details_selected.Text = (files_count + directories_count) + (files_count == 0 ? " director" + (directories_count > 1 ? "ies" : "y") : directories_count == 0 ? " file" + (files_count > 1 ? "s" : "") : " items") + " selected" + ("");
                    }
                }
            }
        }

        private void RestDetailsToDefault()
        {
            cancel_calc_dir_size = true;
            details_title.Text = "DETAILS";
            details_creation_time.Text = details_last_accessed_time.Text = details_modified_time.Text = details_selected.Text = details_total_count.Text = details_size.Text = details_location.Text = "";
            details_attribute.Visibility = details_date.Visibility = view_details_btm.Visibility = details_view_btm.Visibility = details_rename_btm.Visibility = details_delete_btm.Visibility =
                details_move_to_btm.Visibility = details_copy_to_btm.Visibility = details_encrypt_btm.Visibility = details_destroy_btm.Visibility = details_calc_hash_btm.Visibility =
                details_find_extension_btm.Visibility = details_find_dublicate_btm.Visibility = details_find_empty_btm.Visibility = details_tools.Visibility = Visibility.Visible;
            details_attribute_normal.IsEnabled = view_details_btm.IsEnabled = true;
            //details_attribute_reverse.Visibility = Visibility.Collapsed;
            details_attribute_apply_to_all.Visibility = Visibility.Visible;
            details_attribute_other.IsChecked = true;
            details_attribute_not_content_indexed.IsChecked = details_attribute_system.IsChecked = details_attribute_not_encrypted_compressed.IsChecked = details_attribute_encrypted.IsChecked =
                details_attribute_compressed.IsChecked = details_attribute_archive.IsChecked = details_attribute_hidden.IsChecked = details_attribute_read_only.IsChecked = false;
            files_list.Clear();
            directory_list.Clear();
        }

        #endregion Details

        #region Recent

        private void recent_lbl_MouseEnter(object sender, MouseEventArgs e)
        {
            foreach (Label i in recent_tab_header.Children)
                if (sender != i)
                {
                    i.Foreground = Brushes.White;
                    //unerline
                }
            (sender as Label).Foreground = Brushes.SkyBlue;
        }

        private void recent_lbl_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Label).Foreground = Brushes.White;
        }

        private void recent_lbl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ThicknessAnimation nn = new ThicknessAnimation(new Thickness(recent_tab_header.Children.IndexOf(sender as Label) * 400, 68, 0, 0), new Duration(new TimeSpan(1500000)));
            recent_files_list.Visibility = Visibility.Visible;
            recent_directories_list.Visibility = Visibility.Visible;
            nn.Completed += (object sender2, EventArgs e2) =>
            {
                if (sender == recent_directories_lbl)
                    recent_files_list.Visibility = Visibility.Collapsed;
                else if (sender == recent_files_lbl)
                    recent_directories_list.Visibility = Visibility.Collapsed;
            };
            recent_tabs_gird.BeginAnimation(MarginProperty, nn);
        }

        List<RecentItem> recent_directories = new List<RecentItem>(500);
        List<RecentItem> recent_files = new List<RecentItem>(500);

        private void recent_menu_Opened()
        {
            long today_ticks, yesterday_ticks, old_ticks, last_week_ticks, last_month_ticks, older_ticks;
            DateTime now = DateTime.UtcNow;
            today_ticks = DateTime.Today.Ticks;
            long one_day_ticks = 864000000000;
            yesterday_ticks = today_ticks - one_day_ticks;
            old_ticks = yesterday_ticks - one_day_ticks;
            last_week_ticks = today_ticks - one_day_ticks * 7;
            last_month_ticks = new DateTime(now.Year, now.Month, 1).Ticks;
            older_ticks = last_month_ticks - DateTime.DaysInMonth(now.Month > 1 ? now.Year : now.Year - 1, now.Month > 1 ? now.Month - 1 : 12) * one_day_ticks;
            //recent_directories_list.Items.Clear();
            for (int i = recent_directories.Count - 1; i > -1; i--)
            {


            }
        }

        #endregion Recent

        #region Filter and search

        BackgroundWorker filter_wait_before_start = null;
        int filter_timer = 250;

        private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (filter_TextBox.Text == "")
                filter_placeholder.Visibility = Visibility.Visible;
            else
                filter_placeholder.Visibility = Visibility.Collapsed;
            if (CurrentPath != "root" && e.Changes != null && e.Changes.Count > 0)
            {
                string path = CurrentPath;
                if (filter_wait_before_start != null && filter_wait_before_start.IsBusy)
                {
                    filter_timer += 100;
                }
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
                        if (!CloseWindow && CurrentPath == path)
                            RootItemBrowse(CurrentPath, CurrentPath, false, filter_TextBox.Text.ToLower(), false);
                    };
                    filter_wait_before_start.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) => { filter_timer = 250; };
                    filter_wait_before_start.RunWorkerAsync();
                }
            }
        }

        #endregion Filter and search

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            cancel_calc_dir_size = true;
        }


        Dictionary<string, bool> bookmark_dictionary = new Dictionary<string, bool>();
        bool show_warning_again = true;

        private void ClearHistor_btm_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentBackIndex > -1 && CurrentBackIndex < back_menu_data.Count)
            {
                var item = back_menu_data[CurrentBackIndex];
                back_menu_data.Clear();
                back_menu_data.Add(item);
                current_back_index = 0;
                history_list.Items.Refresh();
            }
        }

        private void bookmark_AddThisLocation_Click(object sender, RoutedEventArgs e)
        {
            if (bookmark_dictionary.ContainsKey(CurrentPath))
                Messages.ShowMessage("This path is already exist in this list!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else
            {
                if (show_warning_again)
                    Messages.ShowMessage("Warning!!!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "when you close this window this list will be clear.", out show_warning_again);
                bookmark_dictionary.Add(CurrentPath, false);
                bookmark_list.Items.Add(CreateQuickAccessItem(Functions.GetDirectoryImage(CurrentPath), 24, new DirectoryInfo(CurrentPath).Name, new DirectoryInfo(CurrentPath), CurrentPath));
            }
        }

        private void DirectoryItem_DropOn(object sender, DragEventArgs e)
        {
            //تعارض مع DirectoryList_Drop
            //if (((sender as ListViewItem).Content as DirectoryOrFile).isDirectory)
            //{
            //    directories_list.SelectedItem = (sender as ListViewItem).Content;
            //    if (e.Data.GetDataPresent(DataFormats.FileDrop))
            //    {
            //        List<FileOperationItem> list = new List<FileOperationItem>();
            //        foreach (string i in (string[])e.Data.GetData(DataFormats.FileDrop))
            //        {
            //            if (Directory.Exists(i))
            //                list.Add(new FileOperationItem(new DirectoryInfo(i), i.Substring(0, 2).ToLower() == ((sender as ListViewItem).Content as DirectoryOrFile).FullPath.Substring(0, 2).ToLower(), new DirectoryInfo(i).Name));
            //            else if (File.Exists(i))
            //                list.Add(new FileOperationItem(new FileInfo(i), i.Substring(0, 2).ToLower() == ((sender as ListViewItem).Content as DirectoryOrFile).FullPath.Substring(0, 2).ToLower(), new FileInfo(i).Name));
            //        }
            //        if (list.Count > 0)
            //            new Operation().CopyDialogShow(list, OperationType.Copy, ((sender as ListViewItem).Content as DirectoryOrFile).FullPath, false);
            //    }
            //}
        }

        private void DirectoryList_Drop(object sender, DragEventArgs e)
        {
            //تعارض مع DirectoryItem_DropOn
            if (CurrentPath != "root")
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    List<FileOperationItem> list = new List<FileOperationItem>();
                    foreach (string i in (string[])e.Data.GetData(DataFormats.FileDrop))
                    {
                        if (Directory.Exists(i))
                            list.Add(new FileOperationItem(new DirectoryInfo(i), i.Substring(0, 2).ToLower() == CurrentPath.Substring(0, 2).ToLower(), new DirectoryInfo(i).Name));
                        else if (File.Exists(i))
                            list.Add(new FileOperationItem(new FileInfo(i), i.Substring(0, 2).ToLower() == CurrentPath.Substring(0, 2).ToLower(), new FileInfo(i).Name));
                    }
                    if (list.Count > 0)
                        new Operation().CopyDialogShow(list, OperationType.Copy, CurrentPath, false);
                }
            }
        }

        private void NotAvailyable_btm_Click(object sender, RoutedEventArgs e)
        {
            Messages.ShowMessage("Sorry this feature is not available right now.", ModrenMessageButton.OK, ModrenMessageImage.Sad, ModrenMessageSound.Exclamation, false, "");
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //RootPage.ItemTemplate = Root3Page.ItemTemplate;
        }

        //private void file_menu_grids_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    IsOpenned = false;
        //}
    }

    class DirectoryOrFile
    {

        public DirectoryOrFile(FileSystemInfo file, bool show_extention, bool is_directory)
        {
            fileinfo = file;
            if (is_directory || show_extention)
                FileName = file.Name;
            else
                try { FileName = Path.GetFileNameWithoutExtension(file.FullName); }
                catch (Exception)
                {
                    if (file.Extension.Length > 0)
                        FileName = file.Name.Remove(file.Name.Length - file.Extension.Length);
                    else
                        FileName = file.Name;
                }
            FullPath = file.FullName;
            isDirectory = is_directory;
            if (is_directory)
            {
                Icon = FileInfoCombonents.FilesInformation["directory"].Bitmap24;
                FileType = "Directory";
                FileSize = "";
            }
            else
            {
                FileSize = Functions.FileSize((file as FileInfo).Length, 2);
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
            }
            DateModified = Functions.OrginizeDate(file.LastWriteTimeUtc);
        }

        public DirectoryOrFile(DriveInfo drive)
        {
            FullPath = drive.Name;
            this.drive = drive;
            isDrive = true;
            isDirectory = false;
            if (drive.IsReady)
            {
                FileName = drive.IsReady ? ((drive.VolumeLabel.Equals(string.Empty) ? "Local disk" : drive.VolumeLabel) + " (" + drive.Name + ")") : drive.Name;
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
            FileType = Functions.GetDriveDescription(drive);
        }

        internal string FullPath;

        internal FileSystemInfo fileinfo;

        internal DriveInfo drive;

        internal bool isDrive = false;

        internal bool isDirectory;

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

    class BackMenuItem
    {
        internal bool IsRoot;
        internal DirectoryInfo dir;

        private BitmapImage img_source;
        public BitmapImage Icon
        {
            get { return img_source; }
            set { img_source = value; }
        }

        private string directory_name;
        public string Name
        {
            get { return directory_name; }
            set { directory_name = value; }
        }

        private string directory_path;
        public string Path
        {
            get { return directory_path; }
            set { directory_path = value; }
        }

        public BackMenuItem(string DirectoryPath)
        {
            Path = DirectoryPath;
            if (DirectoryPath.ToLower() == "root")
            {
                Name = "root";
                IsRoot = true;
                Icon = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/go-home-28.png"));
            }
            else
            {
                IsRoot = false;
                dir = new DirectoryInfo(DirectoryPath);
                if (dir.Root.FullName == dir.FullName)
                {
                    DriveInfo drive = new DriveInfo(DirectoryPath);
                    Icon = Functions.GetDriveImage(drive, 24);
                    Name = drive.IsReady ? ((drive.VolumeLabel.Equals(string.Empty) ? "Local disk" : drive.VolumeLabel) + " (" + drive.Name + ")") : drive.Name;
                }
                else
                {
                    Name = dir.Name;
                    Icon = Functions.GetDirectoryImage(DirectoryPath);
                }
            }

        }


    };

    class RecentItem
    {
        public RecentItem(FileSystemInfo file_info)
        {
            item = file_info;
            added_date = DateTime.UtcNow;
        }

        //internal DateTime last_used_date;
        internal DateTime added_date;
        internal FileSystemInfo item;
        //internal int used_time;
    };

    enum FileDate
    {
        CreationDate = 1,
        ModifiedDate = 2,
        LastAccessDate = 3
    }
}
