using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
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
                BuildAddressBar();
                UpdateBookmarkStar();
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
                    UpdateEditMenu();

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

                    ThicknessAnimation menu_anim = new ThicknessAnimation(new Thickness(350, 0, 0, 0), new Duration(SystemParameters.ClientAreaAnimation ? new TimeSpan(1000000) : TimeSpan.Zero));
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



                    ThicknessAnimation menu_anim = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(SystemParameters.ClientAreaAnimation ? new TimeSpan(1000000) : TimeSpan.Zero));
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
                        if (value == 2)
                            UpdateEditMenu();
                        walk_token++;
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
                        file_menu_grids.Children[value + 1].BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(7, 0, 0, 0), new Duration(SystemParameters.ClientAreaAnimation ? new TimeSpan(1500000) : TimeSpan.Zero)));
                    }
                    openned_menu = value;
                }
            }
        }


        public Directory_Explorer()
        {
            InitializeComponent();
            LoadViewSettings();
        }

        static internal void ExplorePath(string path)
        {
            if(Directory.Exists(path))
            {
                Directory_Explorer window = new Directory_Explorer();
                window.Show();
                window.RootItemBrowse("root", path, true, "", true);
                window.Dispatcher.BeginInvoke(new Action(() => { if (window.IsVisible) window.Activate(); }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
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
            if (sender != menu_list.Items[6] && sender != menu_list.Items[9])   //options and exit
                OpennedMenu = (sbyte)menu_list.Items.IndexOf(sender);
            else
            {
                IsOpenned = false;
                {
                    if (sender == menu_list.Items[6])
                        new Options().ShowDialog();
                    else
                        Close();
                }
            }
        }

        private void OldExplorer_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            new MainWindow().Show();
        }

        private void AboutPanel_LinkClick(object sender, EventArgs e)
        {
            IsOpenned = false;
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
            if (sender.Tag.ToString() == "Bookmarks")
                LoadBookmarks();
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
            ((TextBlock)sender.Children[1]).Margin = new Thickness(32, 6, 6, 0);
            quick_access_grids.Children[k].Visibility = Visibility.Visible;
            quick_access_grids.BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(-200 * k, 34, 0, 0), new Duration(new TimeSpan(1500000))));
            MoveTabIndicator(sender, true);
        }

        void MoveTabIndicator(Grid tab, bool animate)
        {
            navigation_panal.UpdateLayout();
            Point position = tab.TranslatePoint(new Point(0, 0), navigation_panal);
            Duration duration = new Duration(animate && SystemParameters.ClientAreaAnimation ? new TimeSpan(750000) : TimeSpan.Zero);
            rec.BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(position.X, 30, 0, 0), duration));
            rec.BeginAnimation(WidthProperty, new DoubleAnimation(tab.ActualWidth, duration));
        }

        #endregion

        #region Initialize

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.IsAdmin)
                Title = "[Administrator]  " + Title;
            ConnectListWithData();
            PrepareColumn(columns_preview_list);
            directories_list.MouseEnter += ColumnLink_MouseEnter;
            directories_list.MouseLeave += ColumnLink_MouseLeave;
            columns_preview.MouseEnter += ColumnLink_MouseEnter;
            columns_preview.MouseLeave += ColumnLink_MouseLeave;
            columns_grid.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((object sender2, ScrollChangedEventArgs e2) => { QueueColumnLinks(); }));
            LoadRoot(true);
            LoadRecent();
            LoadBookmarks();
            UpdateHistoryClear();
            MoveTabIndicator((Grid)navigation_panal.Children[0], false);
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
            columns_scroll.Visibility = Visibility.Collapsed;
            root_list.Visibility = Visibility.Visible;
            BuildColumns();
            BeginLoading("root");

            BackgroundWorker get_drivers = new BackgroundWorker();
            get_drivers.WorkerSupportsCancellation = true;
            List<DriveSnapshot> drivers = new List<DriveSnapshot>();
            bool worker_isbusy = true;
            get_drivers.DoWork += (object senderw, DoWorkEventArgs ew) =>
            {
                try
                {
                    foreach (var i in DriveInfo.GetDrives())
                        drivers.Add(new DriveSnapshot(i));
                }
                catch (Exception) { }
                while (worker_isbusy)
                    Thread.Sleep(25);
            };
            get_drivers.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
              {
                  EndLoading("root");
                  controls_content.IsEnabled = true;
                  for (int i = drivers.Count - 1; i > -1; i--)
                      quick_access_list.Items.Insert(0, CreateQuickAccessItem(Functions.GetDriveImage(drivers[i], 24), 24, drivers[i].description, drivers[i].drive, drivers[i].drive.Name));

                  for (int i = 0; i < drivers.Count; i++)
                      root_data.Insert(i, new RootItem(drivers[i]));
                  quick_access_list.Items.Insert(0, CreateQuickAccessItem("Drives"));

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
                  UpdateHistoryClear();
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
            string[] special_folders_image = { "programfile", "programfile", "fonts", "local", "history" };
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
                    quick_access_list.Items.Add(CreateQuickAccessItem(Functions.GetDirectoryImage(special_folders_image[i]), 24, dir.Name, dir, dir.FullName));
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
                     if (e.ChangedButton != MouseButton.Left)
                         return;
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

        BackgroundWorker browse_worker = null;

        private void RootItemBrowse(string old_path, string new_path, bool add_to_back_menu, string filter_text, bool close_if_fail)
        {
            if (!IsSpecialDirectory(new_path.ToLower())) //if user want to go to a special folder by typing its name in address bar
            {
                try
                {
                    if (!Path.IsPathRooted(new_path) || new_path.StartsWith("\\") && !new_path.StartsWith("\\\\"))
                        throw new ArgumentException("Please write a full path, for example 'C:\\Users'.");
                    new_path = Path.GetFullPath(new_path);
                }
                catch (Exception ex)
                {
                    Messages.ShowMessage("We can not find directory '" + new_path + "', browsing has been canceled.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, ex.Message);
                    if (!address_editing)
                        address_textblock.Text = CurrentPath;
                    controls_content.IsEnabled = true;
                    if (close_if_fail)
                        Close();
                    return;
                }
                if (new_path.Length > 0 && new_path[new_path.Length - 1] != '\\')
                    new_path = new_path + "\\";
                bool WorkerHasFailed = false;
                List<DirectoryInfo> sub_directories = new List<DirectoryInfo>();
                List<FileInfo> files = new List<FileInfo>();
                string error_message = string.Empty, error_message_info = string.Empty;
                int token = ++browse_token;
                BackgroundWorker GetFilesAndDirectory = new BackgroundWorker();
                browse_worker = GetFilesAndDirectory;
                GetFilesAndDirectory.WorkerReportsProgress = true;
                GetFilesAndDirectory.WorkerSupportsCancellation = true;
                GetFilesAndDirectory.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    if (e.ProgressPercentage < 0 && token == browse_token)
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
                            done = true;
                            if (sort_by == SortItemsBy.Random || sort_by == SortItemsBy.RandomDescending)
                            {
                                Shuffle(sub_directories);
                                Shuffle(files);
                            }
                            else
                            {
                                sub_directories.Sort(DirectorySorter);
                                files.Sort(FilesSorter);
                            }
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
                    if (token != browse_token)
                        return;
                    EndLoading("browse");
                    controls_content.IsEnabled = true;
                    if (!WorkerHasFailed)
                    {
                        if (IsOpenned && CurrentPath.ToLower() != new_path.ToLower())
                            IsOpenned = false;
                        ColumnsRemember();
                        FillListWithItems(sub_directories, files, filter_text);
                        CurrentPath = new_path;
                        if (filter_text == "")
                            AddRecent(new_path, true);
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
                        UpdateHistoryClear();
                    }
                    else if(close_if_fail)
                    {
                        Close();
                        return;
                    }
                    directories_list.Items.Refresh();
                    if (!WorkerHasFailed)
                        AddressEditMode(false);
                    if (!address_editing)
                        address_textblock.Text = CurrentPath;
                    if (filter_text.Equals(string.Empty))
                        info_textblock.Text = "This directory is empty.";
                    else
                        info_textblock.Text = "No items match your search.";
                    info_textblock.Visibility = CurrentPath == "root" || directories_data.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                    items_count.Text = CurrentPath == "root" ? root_data.Count.ToString() : directories_data.Count.ToString();
                    item_unit.Text = directories_data.Count == 1 ? " item" : " items";
                    if (directories_list.SelectedIndex == -1)
                    {
                        items_selected.Visibility = Visibility.Collapsed;
                        items_selected_size.Visibility = Visibility.Collapsed;
                    }
                    if (!WorkerHasFailed)
                        ColumnsBrowsed();
                    else
                    {
                        select_after_browse = null;
                        reselect_after_browse = null;
                    }
                };
                BeginLoading("browse");
                GetFilesAndDirectory.RunWorkerAsync();
            }
            else
            {
                new_path = new_path.ToLower();
                if (new_path.Equals("root"))
                {
                    browse_token++;
                    browse_worker = null;
                    EndLoading("browse");
                    LoadRoot(add_to_back_menu);
                    AddressEditMode(false);
                    address_textblock.Text = "root";
                }
                else
                    RootItemBrowse(old_path, GetSpecialDirectoryPath(new_path), add_to_back_menu, filter_text, close_if_fail);
            }
        }

        private void DirectoryItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && directories_list.SelectedItem != null)
                if ((directories_list.SelectedItem as DirectoryOrFile).fileinfo is DirectoryInfo)
                    RootItemBrowse(CurrentPath, (directories_list.SelectedItem as DirectoryOrFile).fileinfo.FullName, true, "", false);
                else if ((directories_list.SelectedItem as DirectoryOrFile).fileinfo is FileInfo)
                    RunFile((directories_list.SelectedItem as DirectoryOrFile).fileinfo.FullName);
        }

        private void DirectoriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsOpenned && !opening_menu)
                IsOpenned = false;
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
            UpdateColumnsPreview();
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
            list_filter = filter_string;
            directories_data.Clear();
            root_list.Visibility = Visibility.Collapsed;
            root_list.SelectedIndex = -1;
            columns_scroll.Visibility = Visibility.Visible;
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
                info_textblock.Text = "No items match your search.";
        }

        int browse_token = 0;

        bool IsBrowsing()
        {
            return browse_worker != null && browse_worker.IsBusy;
        }

        private void Up_Btm_Click(object sender, RoutedEventArgs e)
        {
            if (IsBrowsing())
                return;
            if (CurrentPath.ToLower() != "root")
            {
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
            if (!IsBrowsing())
                CurrentBackIndex++;
        }

        private void Forward_Btm_Click(object sender, RoutedEventArgs e)
        {
            if (!IsBrowsing())
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
            if (e.Key == Key.Enter && address_editing && !IsBrowsing())
            {
                if (!address_textblock.Text.ToLower().Equals(CurrentPath.ToLower()))
                    RootItemBrowse(CurrentPath, address_textblock.Text, true, "", false);
                else
                    AddressEditMode(false);
            }
            else if (e.Key == Key.Escape)
                AddressEditMode(false);
        }

        private void Refresh_btm_Click(object sender, RoutedEventArgs e)
        {
            if (refresh_stop.Visibility == Visibility.Visible)
                StopLoading();
            else
                Refresh();
        }

        bool opening_menu = false;

        private void Explorer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            opening_menu = true;
            IsOpenned = true;
            OpennedMenu = (sender == directories_list && directories_list.SelectedItems.Count == 0) || (sender == root_list && root_list.SelectedItems.Count == 0 && CurrentPath != "root") ? (sbyte)1 : (sbyte)2;
            opening_menu = false;
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
                {
                    IsOpenned = false;
                    DeteteSelectetFiles(Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
                }
            }
        }

        #region Functions

        private void RunFile(string file_name)
        {
            //add to history
            string extention = Path.GetExtension(file_name).ToLower();
            if (extention == ".enf" || extention == ".enp" || extention == ".fme")
            {
                switch (Messages.ShowMessage("This file is encrypted by File Manager, what do you want to do?", "", new ButtonContent("Decrypt it", ModrenMessageBitmapImage.Default, 1), new ButtonContent("View package content", ModrenMessageBitmapImage.Default, 2), new ButtonContent("Open it with Windows", ModrenMessageBitmapImage.Default, 3), new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 4)))
                {
                    case MyMessageBoxResult.Button1:
                        Functions.ShowTool(new Files_Encrepter(new string[] { file_name }, false));
                        AddRecent(file_name, false);
                        return;
                    case MyMessageBoxResult.Button2:
                        Files_Encrepter window = new Files_Encrepter();
                        window.OpenPackage(file_name);
                        Functions.ShowTool(window);
                        AddRecent(file_name, false);
                        return;
                    case MyMessageBoxResult.Button3:
                        break;
                    default:
                        return;
                }
            }
            try
            {
                Process.Start(file_name);
                AddRecent(file_name, false);
            }
            catch (Exception ex)
            { Messages.ShowMessage(string.Format("Sorry we can not open this file '{0}'.", file_name), ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        bool IsSpecialDirectory(string new_path)
        {
            new_path = new_path.ToLower();
            string[] SpecialDirectories = { "root", Environment.UserName.ToLower(), "desktop", "downloads", "documents", "music", "pictures", "videos", "program files", "program files (x86)", "fonts", "local", "history" };
            foreach (var i in SpecialDirectories)
                if (i.Equals(new_path))
                    return true;
            return false;
        }

        string GetSpecialDirectoryPath(string new_path)
        {
            new_path = new_path.ToLower();
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
                    return (new Random((int)(DateTime.UtcNow.Ticks % 100000000)).Next(50000) % 3) - 1;
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

        void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                T k = list[i];
                list[i] = list[j];
                list[j] = k;
            }
        }

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
            columns_cache.Clear();
            if (CurrentPath == "root")
            {
                int index = root_list.SelectedIndex;
                LoadRoot(false);
                if (index < root_list.Items.Count && index > -1)
                    root_list.SelectedIndex = index;
            }
            else
            {
                if (directories_list.SelectedItem is DirectoryOrFile)
                {
                    select_after_browse = (directories_list.SelectedItem as DirectoryOrFile).FullPath;
                    reselect_after_browse = directories_list.SelectedItems.Cast<DirectoryOrFile>().Select(x => ColumnKey(x.FullPath)).ToList();
                }
                RootItemBrowse(CurrentPath, CurrentPath, false, filter_TextBox.Text.ToLower(), false);
            }
        }

        HashSet<string> loading_tasks = new HashSet<string>();
        System.Windows.Threading.DispatcherTimer loading_timer = null;

        void BeginLoading(string task)
        {
            loading_tasks.Add(task);
            if (loading_bar.Visibility == Visibility.Visible)
            {
                if (task == "browse")
                    info_textblock.Visibility = Visibility.Collapsed;
                UpdateLoadingButton();
                return;
            }
            if (loading_timer == null)
            {
                loading_timer = new System.Windows.Threading.DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(300) };
                loading_timer.Tick += (object sender, EventArgs e) =>
                {
                    loading_timer.Stop();
                    if (loading_tasks.Count > 0 && !CloseWindow)
                        ShowLoading(true);
                };
            }
            if (!loading_timer.IsEnabled)
                loading_timer.Start();
        }

        void EndLoading(string task)
        {
            if (!loading_tasks.Remove(task))
                return;
            if (loading_tasks.Count > 0)
            {
                UpdateLoadingButton();
                return;
            }
            if (loading_timer != null)
                loading_timer.Stop();
            ShowLoading(false);
        }

        void ShowLoading(bool show)
        {
            TranslateTransform move = (TranslateTransform)loading_bar_runner.RenderTransform;
            if (show && loading_bar.Visibility != Visibility.Visible)
            {
                loading_bar.Visibility = Visibility.Visible;
                if (loading_tasks.Contains("browse"))
                    info_textblock.Visibility = Visibility.Collapsed;
                if (SystemParameters.ClientAreaAnimation)
                {
                    loading_bar_runner.HorizontalAlignment = HorizontalAlignment.Left;
                    loading_bar_runner.Width = 90;
                    loading_bar_runner.Opacity = 1;
                    DoubleAnimation run = new DoubleAnimation(-90, Math.Max(address_grid.ActualWidth, 200), new Duration(TimeSpan.FromMilliseconds(1400)));
                    run.EasingFunction = new SineEase() { EasingMode = EasingMode.EaseInOut };
                    run.RepeatBehavior = RepeatBehavior.Forever;
                    move.BeginAnimation(TranslateTransform.XProperty, run);
                }
                else
                {
                    move.BeginAnimation(TranslateTransform.XProperty, null);
                    move.X = 0;
                    loading_bar_runner.HorizontalAlignment = HorizontalAlignment.Stretch;
                    loading_bar_runner.Width = double.NaN;
                    loading_bar_runner.Opacity = 0.6;
                }
            }
            else if (!show)
            {
                move.BeginAnimation(TranslateTransform.XProperty, null);
                loading_bar.Visibility = Visibility.Collapsed;
            }
            UpdateLoadingButton();
        }

        bool CanStopLoading()
        {
            return loading_tasks.Any(x => x != "root");
        }

        void UpdateLoadingButton()
        {
            bool stop = loading_bar.Visibility == Visibility.Visible && CanStopLoading();
            refresh_image.Visibility = stop ? Visibility.Collapsed : Visibility.Visible;
            refresh_stop.Visibility = stop ? Visibility.Visible : Visibility.Collapsed;
            refresh_btm.ToolTip = stop ? "Stop loading (Esc)" : "Refresh";
        }

        void StopLoading()
        {
            if (loading_tasks.Contains("browse"))
            {
                browse_token++;
                browse_worker = null;
                select_after_browse = null;
                reselect_after_browse = null;
                menu_after_browse = false;
                controls_content.IsEnabled = true;
                if (!address_editing)
                    address_textblock.Text = CurrentPath;
                info_textblock.Visibility = CurrentPath == "root" || directories_data.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                EndLoading("browse");
            }
            if (loading_tasks.Contains("columns"))
            {
                columns_token++;
                EndLoading("columns");
            }
            if (loading_tasks.Contains("preview"))
            {
                columns_preview_token++;
                columns_preview_text.Text = "Loading was stopped.";
                EndLoading("preview");
            }
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
                Refresh();
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
            new Directory_Explorer().Show();
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

        void UpdateEditMenu()
        {
            bool at_root = CurrentPath == "root";
            edit_cut.IsEnabled = !at_root && directories_list.SelectedItems.Count > 0;
            edit_cut.ToolTip = at_root ? "Drives can not be moved" : edit_cut.IsEnabled ? null : "Select at least one item";
            edit_copy.IsEnabled = at_root ? root_list.SelectedItems.Count > 0 : directories_list.SelectedItems.Count > 0;
            edit_copy.ToolTip = edit_copy.IsEnabled ? null : "Select at least one item";
            edit_paste.IsEnabled = copy_list.Count > 0 && (at_root ? root_list.SelectedItems.Count > 0 : directories_list.SelectedItems.Count == 0 || (directories_list.SelectedItems.Count == 1 && (directories_list.SelectedItem as DirectoryOrFile).isDirectory));
            edit_paste.ToolTip = copy_list.Count == 0 ? "Nothing was cut or copied yet" : edit_paste.IsEnabled ? null : at_root ? "Select a drive to paste into" : "Select one directory to paste into, or nothing to paste here";
            edit_delete.IsEnabled = !at_root && directories_list.SelectedItems.Count > 0;
            edit_delete.ToolTip = at_root ? "Drives can not be deleted" : edit_delete.IsEnabled ? "Hold Shift to delete permanently" : "Select at least one item";
            edit_rename.IsEnabled = at_root ? root_list.SelectedItem is RootItem && (root_list.SelectedItem as RootItem).isDrive : directories_list.SelectedItems.Count > 0;
            edit_rename.ToolTip = edit_rename.IsEnabled ? null : at_root ? "Select a drive to rename it" : "Select at least one item";
            edit_select_all.IsEnabled = edit_invert.IsEnabled = !at_root && directories_list.Items.Count > 0;
            edit_select_all.ToolTip = edit_invert.ToolTip = edit_select_all.IsEnabled ? null : at_root ? "Only one drive can be selected" : "This directory is empty";
            edit_select_none.IsEnabled = at_root ? root_list.SelectedItems.Count > 0 : directories_list.SelectedItems.Count > 0;
            edit_select_none.ToolTip = edit_select_none.IsEnabled ? null : "Nothing is selected";
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
                        HashSet<object> selected = new HashSet<object>(directories_list.SelectedItems.Cast<object>());
                        List<object> unselected = directories_list.Items.Cast<object>().Where(x => !selected.Contains(x)).ToList();
                        directories_list.SelectedItems.Clear();
                        foreach (var item in unselected)
                            directories_list.SelectedItems.Add(item);
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
            ViewSettingChanged();
        }

        private void ViewShowExtension_Checked(object sender, RoutedEventArgs e)
        {
            show_extension = true;
            ViewSettingChanged();
        }

        private void ViewShowExtension_Unchecked(object sender, RoutedEventArgs e)
        {
            show_extension = false;
            ViewSettingChanged();
        }

        private void ViewShowHiddenFiles_Unchecked(object sender, RoutedEventArgs e)
        {
            show_hiddin_files = false;
            ViewSettingChanged();
        }

        private void ViewSortbyName_Checked(object sender, RoutedEventArgs e)
        {
            SetSort(SortItemsBy.Name);
        }

        private void ViewSortbyType_Checked(object sender, RoutedEventArgs e)
        {
            SetSort(SortItemsBy.Type);
        }

        private void ViewSortbySize_Checked(object sender, RoutedEventArgs e)
        {
            SetSort(SortItemsBy.Size);
        }

        private void ViewSortbyDate_Checked(object sender, RoutedEventArgs e)
        {
            SetSort(SortItemsBy.DateModified);
        }

        private void ViewSortRandom_Checked(object sender, RoutedEventArgs e)
        {
            SetSort(SortItemsBy.Random);
        }

        private void ViewSortByAscending_Checked(object sender, RoutedEventArgs e)
        {
            if (sort_by >= SortItemsBy.RandomDescending)
                sort_by = sort_by - 100;
            ViewSettingChanged();
        }

        private void ViewSortByAscending_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sort_by < SortItemsBy.RandomDescending)
                sort_by = sort_by + 100;
            ViewSettingChanged();
        }

        private void ViewAsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = (sender as ListBox).SelectedIndex;
            if (index > -1 && index != view_mode && directories_list != null)
            {
                SetViewMode(index);
                if (!loading_view_settings)
                    Functions.SaveSetting("view_mode", index.ToString());
            }
        }

        #region Details Commands

        private void DetailsAnalizeDirectory_Click(object sender, RoutedEventArgs e)
        {
            List<string> directories = new List<string>();
            if (CurrentPath != "root")
            {
                if (directories_list.SelectedItems.Count == 0)
                    directories.Add(CurrentPath);
                else
                    foreach (DirectoryOrFile i in directories_list.SelectedItems)
                        if (i.isDirectory)
                            directories.Add(i.FullPath);
            }
            else if (root_list.SelectedItems.Count == 1)
            {
                if ((root_list.SelectedItem as RootItem).isDrive && !(root_list.SelectedItem as RootItem).driveInfo.IsReady)
                {
                    Messages.ShowMessage("Drive is not ready", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    return;
                }
                directories.Add((root_list.SelectedItem as RootItem).toolTip);
            }
            if (directories.Count == 0)
                Messages.ShowMessage("Select one directory or more to analyze.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else
                Functions.ShowTool(new Big_File_Finder(directories));
        }

        private void Search_btm_Click(object sender, RoutedEventArgs e)
        {
            string text = filter_TextBox.Text.ToLower();
            if (CurrentPath == "root")
                Messages.ShowMessage("Open a directory first, then search in it.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else if (text.Trim().Length == 0)
                Messages.ShowMessage("Write what you are looking for in the search box first.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else if (!IsBrowsing())
            {
                string search_root = CurrentPath;
                List<DirectoryInfo> found_directories = new List<DirectoryInfo>();
                List<FileInfo> found_files = new List<FileInfo>();
                BackgroundWorker search_worker = new BackgroundWorker();
                browse_worker = search_worker;
                int token = ++browse_token;
                search_worker.DoWork += (object sender2, DoWorkEventArgs e2) =>
                {
                    Stack<DirectoryInfo> to_search = new Stack<DirectoryInfo>();
                    to_search.Push(new DirectoryInfo(search_root));
                    while (to_search.Count > 0 && found_directories.Count + found_files.Count < 5000 && !CloseWindow && token == browse_token)
                    {
                        DirectoryInfo current = to_search.Pop();
                        try
                        {
                            foreach (var i in current.GetDirectories())
                            {
                                if (i.Name.ToLower().Contains(text) && (show_hiddin_files || !i.Attributes.HasFlag(FileAttributes.Hidden)))
                                    found_directories.Add(i);
                                if (!Functions.IsLink(i))
                                    to_search.Push(i);
                            }
                            foreach (var i in current.GetFiles())
                                if (i.Name.ToLower().Contains(text) && (show_hiddin_files || !i.Attributes.HasFlag(FileAttributes.Hidden)))
                                    found_files.Add(i);
                        }
                        catch (Exception) { }
                    }
                };
                search_worker.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
                {
                    if (token != browse_token)
                        return;
                    EndLoading("browse");
                    if (CurrentPath != search_root)
                        return;
                    directories_data.Clear();
                    foreach (var i in found_directories)
                        directories_data.Add(new DirectoryOrFile(i, show_extension, true) { FileName = i.FullName.Substring(search_root.Length) });
                    foreach (var i in found_files)
                        directories_data.Add(new DirectoryOrFile(i, show_extension, false) { FileName = i.FullName.Substring(search_root.Length) });
                    directories_list.Items.Refresh();
                    info_textblock.Text = "No items match your search.";
                    info_textblock.Visibility = directories_data.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                    items_count.Text = directories_data.Count.ToString();
                    item_unit.Text = directories_data.Count == 1 ? " item found" : " items found";
                    if (found_directories.Count + found_files.Count >= 5000)
                        Messages.ShowMessage("Too many results, only the first 5000 are shown.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                };
                BeginLoading("browse");
                search_worker.RunWorkerAsync();
            }
        }

        private void FilterTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Search_btm_Click(null, null);
        }

        private void ViewHelp_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = false;
            Functions.OpenUrl(Functions.HelpLink);
        }

        private void details_calc_hash_btm_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPath == "root" || directories_list.SelectedItems.Count != 1 || !((directories_list.SelectedItem as DirectoryOrFile).fileinfo is FileInfo))
                return;
            FileInfo file = new FileInfo((directories_list.SelectedItem as DirectoryOrFile).FullPath);
            IsOpenned = false;
            if (file.Exists)
                new Preview().ShowHashDialog(file);
            else
                Messages.ShowMessage("We can not find '" + file.FullName + "', it may have been moved or deleted.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
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

        bool?[] SelectedAttributes()
        {
            bool? compressed = details_attribute_compressed.IsChecked, encrypted = details_attribute_encrypted.IsChecked;
            if (compressed != true && encrypted != true && details_attribute_not_encrypted_compressed.IsChecked != true)
                compressed = encrypted = null;
            bool?[] attributes = { details_attribute_read_only.IsChecked, details_attribute_hidden.IsChecked, details_attribute_system.IsChecked, details_attribute_archive.IsChecked, compressed, encrypted, details_attribute_not_content_indexed.IsChecked, details_attribute_normal.IsChecked };
            return attributes;
        }

        private void DetailsApplyAttrepute_Click(object sender, RoutedEventArgs e)
        {
            bool?[] attributes = SelectedAttributes();
            List<FileOperationItem> list = new List<FileOperationItem>();
            if (CurrentPath != "root")
            {
                if (directories_list.SelectedItems.Count == 0)
                    list.Add(new FileOperationItem(new DirectoryInfo(CurrentPath), false, CalcFileAttributes(FileAttributes.Directory, new DirectoryInfo(CurrentPath).Attributes, attributes)));
                foreach (DirectoryOrFile i in directories_list.SelectedItems)
                {
                    if (i.fileinfo is FileInfo)
                        list.Add(new FileOperationItem(i.fileinfo as FileInfo, false, CalcFileAttributes(0, i.fileinfo.Attributes, attributes)));
                    else if (i.fileinfo is DirectoryInfo)
                        list.Add(new FileOperationItem(i.fileinfo as DirectoryInfo, false, CalcFileAttributes(FileAttributes.Directory, i.fileinfo.Attributes, attributes)));
                }
            }
            else if (root_list.SelectedItem != null)
                list.Add(new FileOperationItem(new DirectoryInfo((root_list.SelectedItem as RootItem).toolTip), false, CalcFileAttributes(FileAttributes.Directory, new DirectoryInfo((root_list.SelectedItem as RootItem).toolTip).Attributes, attributes)));
            if (list.Count > 0 && Functions.ConfirmRiskyItems(list.Select(x => x.Item.FullName), "change the attributes of"))
                new Operation().CopyDialogShow(list, OperationType.Attributes, "", true);
        }

        private void DetailsApplyAttreputeToAllContent_Click(object sender, RoutedEventArgs e)
        {
            if (!DetailsWalkIsDone())
                return;
            bool?[] attributes = SelectedAttributes();
            List<FileOperationItem> list = new List<FileOperationItem>();
            foreach (var i in directory_list)
                list.Add(new FileOperationItem((DirectoryInfo)i.Item, false, CalcFileAttributes(FileAttributes.Directory, i.Item.Attributes, attributes)));
            foreach (var i in files_list)
                list.Add(new FileOperationItem((FileInfo)i.Item, false, CalcFileAttributes(0, i.Item.Attributes, attributes)));
            if (list.Count > 0 && Functions.ConfirmRiskyItems(SelectedPaths(), "change the attributes of all content of"))
                new Operation().CopyDialogShow(list, OperationType.Attributes, "", true);
        }

        List<string> SelectedPaths()
        {
            List<string> paths = new List<string>();
            if (CurrentPath == "root")
            {
                if (root_list.SelectedItem != null)
                    paths.Add((root_list.SelectedItem as RootItem).toolTip);
            }
            else if (directories_list.SelectedItems.Count == 0)
                paths.Add(CurrentPath);
            else
                foreach (DirectoryOrFile i in directories_list.SelectedItems)
                    paths.Add(i.FullPath);
            return paths;
        }

        bool DetailsWalkIsDone()
        {
            if (calc_directory_size == null || calc_directory_size.IsBusy || !details_walk_done)
            {
                Messages.ShowMessage("Sorry, the app is still reading the selected items, please try again in a moment.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                return false;
            }
            return true;
        }

        List<string> DetailsFiles()
        {
            if (!DetailsWalkIsDone())
                return null;
            List<string> tmp = new List<string>();
            foreach (var i in files_list)
                tmp.Add(i.Item.FullName);
            if (tmp.Count == 0)
            {
                Messages.ShowMessage("There are no files in the selected items.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                return null;
            }
            return tmp;
        }

        private void details_view_btm_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.GoToPath = false;
            ViewDetails();
            if (MainWindow.GoToPath)
            {
                MainWindow.GoToPath = false;
                IsOpenned = false;
                RootItemBrowse(CurrentPath, MainWindow.to_go_path, true, "", false);
            }
        }

        void ViewDetails()
        {
            if (CurrentPath == "root" && root_list.SelectedItems.Count == 1)
            {
                if ((root_list.SelectedItem as RootItem).isDrive)
                    new Preview().ShowDialog(new DriveInfo((root_list.SelectedItem as RootItem).toolTip), ItemType.Drive);
                else
                    new Preview().ShowDialog(new DirectoryInfo((root_list.SelectedItem as RootItem).toolTip), ItemType.Directory);
            }
            else if (CurrentPath != "root" && directories_list.SelectedItems.Count == 0)
            {
                if (Directory.Exists(CurrentPath))
                    new Preview().ShowDialog(new DirectoryInfo(CurrentPath), ItemType.Directory);
            }
            else if (directories_list.SelectedItems.Count == 1)
            {
                FileSystemInfo item = (directories_list.SelectedItem as DirectoryOrFile).fileinfo;
                item.Refresh();
                if (item is DirectoryInfo && item.Exists)
                    new Preview().ShowDialog(item, ItemType.Directory);
                else if (item is FileInfo && item.Exists)
                    new Preview().ShowDialog(item, ItemType.File);
                else
                    Messages.ShowMessage("We can not find '" + item.FullName + "', it may have been moved or deleted.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
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
            bool permanent = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            IsOpenned = false;
            DeteteSelectetFiles(permanent);
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
            List<string> tmp = DetailsFiles();
            if (tmp != null)
                Functions.ShowTool(new Files_Encrepter(tmp.ToArray(), true));
        }

        private void details_destroy_btm_Click(object sender, RoutedEventArgs e)
        {
            bool destroy_files = false;
            if (CurrentPath == "root")
            {
                if (root_list.SelectedIndex != -1)
                {
                    RootItem item = root_list.SelectedItem as RootItem;
                    if (item.isDrive)
                    {
                        switch (Messages.ShowMessage("What do you wat to do:", item.driveInfo.Name.ToLower() == Functions.SystemDrive ? "This is the system drive, Windows runs from it." : "",
                           new ButtonContent("destroy all files in the drive", ModrenMessageBitmapImage.Default, 1),
                           new ButtonContent("destroy deleted files in the drive", ModrenMessageBitmapImage.Default, 2),
                           new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 3)))
                        {
                            case MyMessageBoxResult.Button1:
                                destroy_files = true;
                                break;
                            case MyMessageBoxResult.Button2:
                                Functions.ShowTool(new File_Destroyer(item.driveInfo, false));
                                break;
                        }
                    }
                    else
                        destroy_files = true;
                }
            }
            if (CurrentPath != "root" || destroy_files)
            {
                List<string> tmp = DetailsFiles();
                if (tmp != null)
                    Functions.ShowTool(new File_Destroyer(tmp.ToArray(), true));
            }
        }

        private void details_find_extension_btm_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPath == "root" && root_list.SelectedIndex < 0)
                return;
            List<string> tmp = DetailsFiles();
            if (tmp != null && Functions.ConfirmRiskyItems(SelectedPaths(), "change the extensions of files in"))
                new Renamer().ShowWindow(tmp, "", ExtentionChanger.ExtentionSuggester, false);
        }

        private void details_find_duplicate_btm_Click(object sender, RoutedEventArgs e)
        {
            List<string> items = SelectedPaths();
            if (items.Count > 0)
                Functions.ShowTool(new Identical_File_Remover(items));
        }

        private void details_find_empty_btm_Click(object sender, RoutedEventArgs e)
        {
            List<string> items = new List<string>();
            foreach (var i in SelectedPaths())
                if (Directory.Exists(i))
                    items.Add(i);
            if (items.Count > 0)
                Functions.ShowTool(new Empty_Folder_Remover(items));
        }

        private void tools_open_Btm_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            IsOpenned = false;
            Window win = null;
            switch ((sender as ListBoxItem).Tag.ToString())
            {
                case "1":
                    win = new File_Compresser();
                    break;
                case "2":
                    if (!Functions.ShowOpenedTool<System_Monitor>())
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
                Functions.ShowTool(win);
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
                string folder = CurrentPath.TrimEnd('\\');
                if (File.Exists(folder + "\\" + new_name + extension))
                {
                    int i = 1;
                    while (File.Exists(folder + "\\" + new_name + " (" + i + ")" + extension)) i++;
                    if (Messages.ShowMessage("File with name '" + new_name + extension + "' is already exist.", ModrenMessageButton.YesNo, ModrenMessageImage.Rename, ModrenMessageSound.Question, true, string.Format("Do you want to create file '{0} ({1}){2}' instat of '{0}{2}'?", new_name, i, extension)) == MyMessageBoxResult.Yes)
                        file_name = folder + "\\" + new_name + " (" + i + ")" + extension;
                }
                else
                    file_name = folder + "\\" + new_name + extension;
                try { if (file_name != null) Functions.CreateNewFile(file_name, extension); }
                catch (Exception ex) { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                //Refresh();
            }
        }

        private void ChangeFilesDate(FileDate date_type, OperationTree root)
        {
            if (directories_list.IsVisible && directories_list.SelectedIndex > -1)
            {
                DateTime base_date = date_type == FileDate.CreationDate ? directories_data[directories_list.SelectedIndex].fileinfo.CreationTime : date_type == FileDate.ModifiedDate ? directories_data[directories_list.SelectedIndex].fileinfo.LastWriteTime : directories_data[directories_list.SelectedIndex].fileinfo.LastAccessTime;
                DateTime? date = new ChangeDate().ShowDialog(base_date);
                if (date.HasValue)
                {
                    foreach (var i in root.children)
                        i.item.Refresh();
                    OperationTree unsucess = new OperationTree(null, false, date_type, OperationType.ChangeFileDate, null, false, string.Format("we can not change {0} time for these items.", date_type == FileDate.CreationDate ? "creation" : date_type == FileDate.ModifiedDate ? "last modified" : "last access"));
                    foreach (var i in root.children)
                    {
                        try
                        {
                            switch (date_type)
                            {
                                case FileDate.CreationDate:
                                    i.item.CreationTime = date.Value;
                                    break;
                                case FileDate.ModifiedDate:
                                    i.item.LastWriteTime = date.Value;
                                    break;
                                case FileDate.LastAccessDate:
                                    i.item.LastAccessTime = date.Value;
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
                        File_s_Transferring_Error n = new File_s_Transferring_Error(unsucess, string.Format("We can not change {0} time for these items.", date_type == FileDate.CreationDate ? "creation" : date_type == FileDate.ModifiedDate ? "last modified" : "last access"));
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
                                        i.item.CreationTime = date.Value;
                                        break;
                                    case FileDate.ModifiedDate:
                                        i.item.LastWriteTime = date.Value;
                                        break;
                                    case FileDate.LastAccessDate:
                                        i.item.LastAccessTime = date.Value;
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
                            details_creation_time.Text = "creation time: " + Functions.OrginizeDate(directories_data[directories_list.SelectedIndex].fileinfo.CreationTime);
                            break;
                        case FileDate.ModifiedDate:
                            details_modified_time.Text = "last modified time: " + Functions.OrginizeDate(directories_data[directories_list.SelectedIndex].fileinfo.LastWriteTime);
                            break;
                        case FileDate.LastAccessDate:
                            details_last_accessed_time.Text = "last accessed time: " + Functions.OrginizeDate(directories_data[directories_list.SelectedIndex].fileinfo.LastAccessTime);
                            break;
                    }
                }
            }
        }

        FileAttributes SetAttribute(FileAttributes attributes, FileAttributes flag, bool? value)
        {
            if (value == true)
                return attributes | flag;
            if (value == false)
                return attributes & ~flag;
            return attributes;
        }

        FileAttributes CalcFileAttributes(FileAttributes start, FileAttributes default_attributes, bool?[] selected_attributes)
        {
            if (selected_attributes[7] == true)
                return FileAttributes.Normal;
            FileAttributes new_attributes = start | (default_attributes & (FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive | FileAttributes.Temporary | FileAttributes.SparseFile | FileAttributes.ReparsePoint | FileAttributes.Compressed | FileAttributes.Offline | FileAttributes.NotContentIndexed | FileAttributes.Encrypted));
            new_attributes = SetAttribute(new_attributes, FileAttributes.ReadOnly, selected_attributes[0]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.Hidden, selected_attributes[1]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.System, selected_attributes[2]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.Archive, selected_attributes[3]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.Compressed, selected_attributes[4]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.Encrypted, selected_attributes[5]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.NotContentIndexed, selected_attributes[6]);
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
                FileSystemInfo reference = (directories_list.SelectedItem as DirectoryOrFile).fileinfo;
                string new_name = new Rename().ShowWindow(reference.FullName, true);
                if (new_name != null)
                {
                    if (CurrentPath != "root" && directories_list.SelectedItems.Count > 0)
                    {
                        List<FileSystemInfo> items = new List<FileSystemInfo>();
                        foreach (DirectoryOrFile i in directories_data)
                            if (directories_list.SelectedItems.Contains(i))
                                items.Add(i.fileinfo);
                        if (!Functions.ConfirmRiskyItems(items.Select(x => x.FullName), "rename"))
                            return;
                        List<string> names = Functions.MultiRenameNames(items, reference, new_name);
                        List<FileOperationItem> to_rename_list = new List<FileOperationItem>();
                        for (int i = 0; i < items.Count; i++)
                            if (items[i] is DirectoryInfo)
                                to_rename_list.Add(new FileOperationItem((DirectoryInfo)items[i], false, names[i]));
                            else
                                to_rename_list.Add(new FileOperationItem((FileInfo)items[i], false, names[i]));
                        new Operation().CopyDialogShow(to_rename_list, OperationType.Rename, new_name, true);
                    }
                    Refresh();
                }
            }
        }

        private void DeteteSelectetFiles(bool permanent)
        {
            if (CurrentPath != "root")
            {
                if (directories_list.SelectedItems.Count > 0)
                {
                    List<FileSystemInfo> items = new List<FileSystemInfo>();
                    foreach (DirectoryOrFile i in directories_list.SelectedItems)
                        items.Add(i.fileinfo);
                    if (Functions.ConfirmDelete(items, permanent) && Functions.DeleteItems(items, permanent, this))
                        Refresh();
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

        int walk_token = 0;
        bool details_walk_done = false;

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
            int token = ++walk_token;
            details_walk_done = false;
            List<FileOperationItem> walk_files = new List<FileOperationItem>(), walk_directories = new List<FileOperationItem>();
            string text = details_title.Text;
            long size = 0, size_on_disk = 0;
            BackgroundWorker walker = new BackgroundWorker();
            calc_directory_size = walker;
            walker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                Thread progress = new Thread(() =>
                {
                    while (walker.IsBusy)
                    {
                        try { walker.ReportProgress(0); }
                        catch (Exception) { }
                        if (token != walk_token) return;
                        Thread.Sleep(250);
                    }
                });
                progress.IsBackground = true;
                progress.Start();
                foreach (var i in files)
                {
                    if (token != walk_token)
                        return;
                    walk_files.Add(new FileOperationItem(i, false, ""));
                    try
                    {
                        size += i.Length;
                        size_on_disk += Functions.CalcFileSizeOnDisk(i);
                    }
                    catch (Exception) { }
                }
                foreach (var i in directories)
                {
                    if (token != walk_token)
                        return;
                    walk_directories.Add(new FileOperationItem(i, false, null));
                    try
                    { GetSubFilesAndDirectories(i, ref size, ref size_on_disk, token, walk_files, walk_directories); }
                    catch (Exception) { }
                }
            };

            walker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                if (token == walk_token)
                    ApplySizeResult(text, is_drive, drive_used_space, size, size_on_disk, walk_directories.Count, walk_files.Count);
            };

            walker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (token != walk_token)
                    return;
                files_list = walk_files;
                directory_list = walk_directories;
                details_walk_done = e.Error == null;
                ApplySizeResult(text, is_drive, drive_used_space, size, size_on_disk, walk_directories.Count, walk_files.Count);
                details_calc_size_pb.Visibility = Visibility.Collapsed;
                details_calc_size_pb.IsIndeterminate = false;
            };

            walker.WorkerReportsProgress = true;
            walker.RunWorkerAsync();
        }

        private void GetSubFilesAndDirectories(DirectoryInfo directory, ref long size, ref long size_on_disk, int token, List<FileOperationItem> walk_files, List<FileOperationItem> walk_directories)
        {
            if (token != walk_token)
                return;
            if (!Functions.IsLink(directory))
            {
                var d = directory.GetDirectories();
                foreach (var i in d)
                {
                    if (token != walk_token)
                        return;
                    walk_directories.Add(new FileOperationItem(i, false, null));
                    try
                    { GetSubFilesAndDirectories(i, ref size, ref size_on_disk, token, walk_files, walk_directories); }
                    catch (Exception) { }
                }
                var f = directory.GetFiles();
                foreach (var i in f)
                {
                    walk_files.Add(new FileOperationItem(i, false, ""));
                    size += i.Length;
                    size_on_disk += Functions.CalcFileSizeOnDisk(i);
                }
            }
        }

        private void ApplySizeResult(string text, bool is_drive, double drive_used_space, long size, long size_on_disk, int directories_count, int files_count)
        {
            details_total_count.Text = "contains ";
            details_total_count.Text += (directories_count > 0 ? directories_count + (directories_count == 1 ? " directory " : " directories ") : "");
            if (details_total_count.Text != "contains " && files_count > 0)
                details_total_count.Text += "and ";
            details_total_count.Text += (files_count > 0 ? files_count + (files_count == 1 ? " file " : " files ") : "");
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
                details_calc_size_pb.Visibility = details_attribute.Visibility = details_tools.Visibility = details_date.Visibility = view_details_btm.Visibility = Visibility.Collapsed;
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
                            details_creation_time.Text = "creation time: " + Functions.OrginizeDate(selected_directory.CreationTime);
                            details_modified_time.Text = "last modified time: " + Functions.OrginizeDate(selected_directory.LastWriteTime);
                            details_last_accessed_time.Text = "last accessed time: " + Functions.OrginizeDate(selected_directory.LastAccessTime);
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
                    details_delete_btm.Visibility = details_move_to_btm.Visibility = Visibility.Collapsed;
                    if (root_list.SelectedItem is RootItem && !(root_list.SelectedItem as RootItem).isDrive)
                        details_rename_btm.Visibility = Visibility.Collapsed;
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
                        details_location.Text = directories_list.SelectedItems.Count == 0 ? "current directory: " + CurrentPath : (directories_list.SelectedItems.Count > 1 ? "all items in: " : "item in: ") + CurrentPath;
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
                        details_creation_time.Text = "creation time: " + (same_creation ? Functions.OrginizeDate(first_item.fileinfo.CreationTime) : "muliple-values");
                        details_modified_time.Text = "last modified time: " + (same_modified ? Functions.OrginizeDate(first_item.fileinfo.LastWriteTime) : "muliple-values");
                        details_last_accessed_time.Text = "last accessed time: " + (same_access ? Functions.OrginizeDate(first_item.fileinfo.LastAccessTime) : "muliple-values");
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
                            view_details_btm.Visibility = Visibility.Collapsed;
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
                        details_selected.Text = directories_list.SelectedItems.Count == 0 ? "nothing selected, details of the current directory" : (files_count + directories_count) + (files_count == 0 ? " director" + (directories_count > 1 ? "ies" : "y") : directories_count == 0 ? " file" + (files_count > 1 ? "s" : "") : " items") + " selected" + ("");
                        if (directories_list.SelectedItems.Count == 0)
                            details_rename_btm.Visibility = details_delete_btm.Visibility = details_move_to_btm.Visibility = details_copy_to_btm.Visibility = Visibility.Collapsed;
                        if (directories_list.SelectedItems.Count > 1)
                            details_view_btm.Visibility = Visibility.Collapsed;
                        if (directory_att == false)
                            details_find_empty_btm.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void RestDetailsToDefault()
        {
            walk_token++;
            details_walk_done = false;
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
            files_list = new List<FileOperationItem>();
            directory_list = new List<FileOperationItem>();
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

        static string RecentFile()
        {
            return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "File Manager"), "recent.txt");
        }

        static object recent_lock = new object();

        static void AddRecent(string path, bool is_directory)
        {
            if (path == null || path.ToLower() == "root")
                return;
            try
            {
                lock (recent_lock)
                {
                    List<string> lines = File.Exists(RecentFile()) ? File.ReadAllLines(RecentFile()).ToList() : new List<string>();
                    lines.RemoveAll(x => x.Split('|').Length != 3 || string.Equals(x.Split('|')[2], path, StringComparison.OrdinalIgnoreCase));
                    lines.Insert(0, (is_directory ? "D" : "F") + "|" + DateTime.UtcNow.Ticks + "|" + path);
                    int directories = 0, files = 0;
                    lines = lines.Where(x => x.StartsWith("D|") ? ++directories <= 20 : x.StartsWith("F|") && ++files <= 20).ToList();
                    Directory.CreateDirectory(Path.GetDirectoryName(RecentFile()));
                    File.WriteAllLines(RecentFile(), lines.ToArray());
                }
            }
            catch (Exception) { }
        }

        private void recent_menu_Opened()
        {
            LoadRecent();
        }

        void LoadRecent()
        {
            recent_directories_list.Items.Clear();
            recent_files_list.Items.Clear();
            List<string> lines = new List<string>();
            try
            {
                lock (recent_lock)
                    if (File.Exists(RecentFile()))
                        lines = File.ReadAllLines(RecentFile()).ToList();
            }
            catch (Exception) { }
            string[] last_group = { null, null };
            foreach (var i in lines)
            {
                string[] parts = i.Split('|');
                long ticks;
                if (parts.Length != 3 || !long.TryParse(parts[1], out ticks))
                    continue;
                bool is_directory = parts[0] == "D";
                ListBox list = is_directory ? recent_directories_list : recent_files_list;
                DateTime date = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().Date;
                string group = date == DateTime.Today ? "today" : date == DateTime.Today.AddDays(-1) ? "yesterday" : date.Day + "/" + date.Month + "/" + date.Year;
                if (last_group[is_directory ? 0 : 1] != group)
                {
                    last_group[is_directory ? 0 : 1] = group;
                    list.Items.Add(new ListBoxItem() { Content = group, Margin = new Thickness(0, 10, 0, 0), Background = null, Foreground = new SolidColorBrush(Color.FromRgb(0xAF, 0xAF, 0xAF)), IsEnabled = false });
                }
                StackPanel stack = new StackPanel() { Margin = new Thickness(5, 0, 0, 0), Orientation = Orientation.Horizontal, Height = 18 };
                stack.Children.Add(new Image() { Width = 14, Height = 14, Margin = new Thickness(5, 0, 0, 0), Stretch = Stretch.Fill, Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Explorer Interface/recent.png")) });
                stack.Children.Add(new TextBlock() { Text = parts[2], VerticalAlignment = VerticalAlignment.Top, Foreground = Brushes.White, Margin = new Thickness(5, 1, 0, 0), FontSize = 12 });
                ListBoxItem item = new ListBoxItem() { Content = stack, Tag = parts[2], ToolTip = parts[2], Cursor = Cursors.Hand };
                item.MouseUp += (object sender, MouseButtonEventArgs e) =>
                {
                    string path = (sender as ListBoxItem).Tag.ToString();
                    IsOpenned = false;
                    if (is_directory)
                    {
                        if (Directory.Exists(path))
                            RootItemBrowse(CurrentPath, path, true, "", false);
                        else
                            Messages.ShowMessage("We can not find directory '" + path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    }
                    else if (File.Exists(path))
                        RunFile(path);
                    else
                        Messages.ShowMessage("We can not find the file '" + path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                };
                list.Items.Add(item);
            }
            if (recent_directories_list.Items.Count == 0)
                recent_directories_list.Items.Add(new ListBoxItem() { Content = "no recent directories yet", Background = null, Foreground = new SolidColorBrush(Color.FromRgb(0xAF, 0xAF, 0xAF)), IsEnabled = false });
            if (recent_files_list.Items.Count == 0)
                recent_files_list.Items.Add(new ListBoxItem() { Content = "no recent files yet", Background = null, Foreground = new SolidColorBrush(Color.FromRgb(0xAF, 0xAF, 0xAF)), IsEnabled = false });
        }

        #endregion Recent

        #region Address bar

        bool address_editing = false;

        void AddressEditMode(bool edit)
        {
            if (edit == address_editing)
                return;
            address_editing = edit;
            if (edit)
            {
                address_textblock.Text = CurrentPath;
                address_bar.Visibility = Visibility.Hidden;
                address_textblock.Visibility = Visibility.Visible;
                address_textblock.Focus();
                address_textblock.SelectAll();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (address_editing && !address_textblock.IsKeyboardFocusWithin)
                    {
                        address_textblock.Focus();
                        address_textblock.SelectAll();
                    }
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
            else
            {
                bool had_focus = address_textblock.IsKeyboardFocusWithin;
                address_textblock.Visibility = Visibility.Hidden;
                address_bar.Visibility = Visibility.Visible;
                address_textblock.Text = CurrentPath;
                if (had_focus)
                    FocusExplorerList();
            }
        }

        void FocusExplorerList()
        {
            if (directories_list.Visibility == Visibility.Visible)
                directories_list.Focus();
            else
                root_list.Focus();
        }

        private void AddressBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AddressEditMode(true);
        }

        private void AddressBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FitAddressBar();
        }

        private void AddressTextBlock_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (address_editing && e.NewFocus is DependencyObject && Window.GetWindow((DependencyObject)e.NewFocus) == this)
                AddressEditMode(false);
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Visual source = e.OriginalSource as Visual;
            if (!address_editing || source == null || PresentationSource.FromVisual(source) != PresentationSource.FromVisual(this) || address_textblock.IsAncestorOf(source))
                return;
            AddressEditMode(false);
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control) || (e.Key == Key.System && e.SystemKey == Key.D && Keyboard.Modifiers == ModifierKeys.Alt) || (e.Key == Key.F4 && Keyboard.Modifiers == ModifierKeys.None))
            {
                e.Handled = true;
                if (address_editing)
                {
                    address_textblock.Focus();
                    address_textblock.SelectAll();
                }
                else
                    AddressEditMode(true);
            }
            else if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None && CanStopLoading())
            {
                e.Handled = true;
                StopLoading();
            }
            else if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None && IsOpenned)
            {
                e.Handled = true;
                if (file_button.IsEnabled)
                    IsOpenned = false;
            }
            else if (!IsOpenned && !address_editing)
                ExplorerShortcut(e);
        }

        void ExplorerShortcut(KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            bool typing = Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase;
            if (Keyboard.Modifiers == ModifierKeys.Alt && (key == Key.Left || key == Key.Right || key == Key.Up))
            {
                e.Handled = true;
                if (key == Key.Left)
                    Back_Btm_Click(null, null);
                else if (key == Key.Right)
                    Forward_Btm_Click(null, null);
                else if (CurrentPath != "root")
                    Up_Btm_Click(null, null);
            }
            else if (key == Key.Back && Keyboard.Modifiers == ModifierKeys.None && !typing)
            {
                e.Handled = true;
                Back_Btm_Click(null, null);
            }
            else if (key == Key.F5 && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                if (!IsBrowsing())
                    Refresh();
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && !typing && (key == Key.C || key == Key.X || key == Key.V || key == Key.A))
            {
                e.Handled = true;
                UpdateEditMenu();
                if (key == Key.C && edit_copy.IsEnabled)
                    EditCopy_Btm_Click(null, null);
                else if (key == Key.X && edit_cut.IsEnabled)
                    EditCut_Btm_Click(null, null);
                else if (key == Key.V && edit_paste.IsEnabled)
                    EditPaste_Btm_Click(null, null);
                else if (key == Key.A && edit_select_all.IsEnabled)
                    EditSelectAll_Btm_Click(null, null);
            }
        }

        void BuildAddressBar()
        {
            address_segments.Children.Clear();
            address_root_text.Visibility = path == "root" ? Visibility.Visible : Visibility.Collapsed;
            address_trail.Text = path == "root" ? "" : path;
            if (path != "root")
            {
                try
                {
                    string root = Path.GetPathRoot(path), current = root;
                    address_segments.Children.Add(CreateAddressSegment(DriveTitle(root), root));
                    foreach (var i in path.Substring(root.Length).Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        current = Path.Combine(current, i) + "\\";
                        address_segments.Children.Add(CreateAddressSegment(i, current));
                    }
                }
                catch (Exception)
                {
                    address_segments.Children.Clear();
                    address_segments.Children.Add(CreateAddressSegment(path, path));
                }
            }
            FitAddressBar();
        }

        string DriveTitle(string root)
        {
            RootItem drive = root_data.FirstOrDefault(x => x.isDrive && string.Equals(x.toolTip, root, StringComparison.OrdinalIgnoreCase));
            return drive != null && !string.IsNullOrEmpty(drive.FullPath) ? drive.FullPath : root.TrimEnd('\\');
        }

        StackPanel CreateAddressSegment(string name, string directory)
        {
            StackPanel segment = new StackPanel() { Orientation = Orientation.Horizontal };
            Button button = new Button() { Style = (Style)FindResource("address_button"), ToolTip = directory };
            button.Content = new TextBlock() { Text = name, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 220, VerticalAlignment = VerticalAlignment.Center };
            button.Click += (object sender, RoutedEventArgs e) =>
            {
                if (!IsBrowsing() && !string.Equals(directory, CurrentPath, StringComparison.OrdinalIgnoreCase))
                    RootItemBrowse(CurrentPath, directory, true, "", false);
            };
            segment.Children.Add(button);
            Button chevron = new Button() { Style = (Style)FindResource("address_button"), Padding = new Thickness(3, 0, 3, 0), ToolTip = "Subdirectories", Tag = directory };
            chevron.Content = new System.Windows.Shapes.Path() { Data = Geometry.Parse("M 0,0 L 3.5,3.5 L 0,7"), Stroke = new SolidColorBrush(Color.FromRgb(0x41, 0x40, 0x3B)), StrokeThickness = 1.2, RenderTransformOrigin = new Point(0.5, 0.5), VerticalAlignment = VerticalAlignment.Center };
            chevron.Click += AddressChevron_Click;
            segment.Children.Add(chevron);
            return segment;
        }

        void FitAddressBar()
        {
            int count = address_segments.Children.Count;
            Size any = new Size(double.PositiveInfinity, double.PositiveInfinity);
            bool trail = view_mode == 6 && path != "root";
            address_trail.Visibility = trail ? Visibility.Visible : Visibility.Collapsed;
            if (trail)
            {
                foreach (UIElement i in address_segments.Children)
                    i.Visibility = Visibility.Collapsed;
                address_overflow.Visibility = Visibility.Collapsed;
                address_root_btm.Measure(any);
                address_root_chevron.Measure(any);
                address_trail.MaxWidth = Math.Max(0, address_bar.ActualWidth - 16 - address_root_btm.DesiredSize.Width - address_root_chevron.DesiredSize.Width);
                return;
            }
            if (count == 0)
            {
                address_overflow.Visibility = Visibility.Collapsed;
                return;
            }
            address_root_btm.Measure(any);
            address_root_chevron.Measure(any);
            address_overflow.Visibility = Visibility.Visible;
            address_overflow.Measure(any);
            double available = address_bar.ActualWidth - 32 - address_root_btm.DesiredSize.Width - address_root_chevron.DesiredSize.Width, used = 0;
            int first = count - 1;
            for (int i = count - 1; i >= 0; i--)
            {
                UIElement segment = address_segments.Children[i];
                segment.Visibility = Visibility.Visible;
                segment.Measure(any);
                if (i < count - 1 && used + segment.DesiredSize.Width + (i > 0 ? address_overflow.DesiredSize.Width : 0) > available)
                    break;
                used += segment.DesiredSize.Width;
                first = i;
            }
            for (int i = 0; i < count; i++)
                address_segments.Children[i].Visibility = i < first ? Visibility.Collapsed : Visibility.Visible;
            address_overflow.Visibility = first > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AddressRoot_Click(object sender, RoutedEventArgs e)
        {
            if (!IsBrowsing() && CurrentPath != "root")
                RootItemBrowse(CurrentPath, "root", true, "", false);
        }

        private void AddressChevron_Click(object sender, RoutedEventArgs e)
        {
            Button chevron = sender as Button;
            string directory = chevron.Tag as string;
            ContextMenu menu = new ContextMenu();
            if (directory == null)
            {
                foreach (var i in root_data.Where(x => x.isDrive))
                    menu.Items.Add(CreateAddressMenuItem(i.FullPath, i.toolTip, i.Image));
                if (menu.Items.Count == 0)
                    try
                    {
                        foreach (var i in DriveInfo.GetDrives())
                            menu.Items.Add(CreateAddressMenuItem(i.Name, i.Name, null));
                    }
                    catch (Exception) { }
                if (menu.Items.Count == 0)
                    menu.Items.Add(new MenuItem() { Header = "no drives", IsEnabled = false });
                ShowAddressMenu(chevron, menu);
                return;
            }
            menu.Items.Add(new MenuItem() { Header = "loading...", IsEnabled = false });
            ShowAddressMenu(chevron, menu);
            List<DirectoryInfo> sub_directories = new List<DirectoryInfo>();
            string error = null;
            bool show_hidden = show_hiddin_files;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (object sender2, DoWorkEventArgs e2) =>
            {
                try
                {
                    sub_directories = new DirectoryInfo(directory).GetDirectories().Where(x => show_hidden || !x.Attributes.HasFlag(FileAttributes.Hidden)).OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            };
            worker.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
            {
                menu.Items.Clear();
                if (error != null)
                    menu.Items.Add(new MenuItem() { Header = new TextBlock() { Text = error }, IsEnabled = false });
                else if (sub_directories.Count == 0)
                    menu.Items.Add(new MenuItem() { Header = "no subdirectories", IsEnabled = false });
                else
                    foreach (var i in sub_directories)
                        menu.Items.Add(CreateAddressMenuItem(i.Name, i.FullName, Functions.GetDirectoryImage(i.FullName)));
            };
            worker.RunWorkerAsync();
        }

        private void AddressOverflow_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = new ContextMenu();
            for (int i = address_segments.Children.Count - 1; i >= 0; i--)
                if (address_segments.Children[i].Visibility == Visibility.Collapsed)
                {
                    Button button = (address_segments.Children[i] as StackPanel).Children[0] as Button;
                    menu.Items.Add(CreateAddressMenuItem((button.Content as TextBlock).Text, button.ToolTip.ToString(), Functions.GetDirectoryImage(button.ToolTip.ToString())));
                }
            ShowAddressMenu(sender as Button, menu);
        }

        MenuItem CreateAddressMenuItem(string header, string target, ImageSource icon)
        {
            MenuItem item = new MenuItem() { Header = new TextBlock() { Text = header }, ToolTip = target };
            if (icon != null)
                item.Icon = new Image() { Source = icon, Width = 16, Height = 16 };
            item.Click += (object sender, RoutedEventArgs e) =>
            {
                if (!IsBrowsing())
                    RootItemBrowse(CurrentPath, target, true, "", false);
            };
            return item;
        }

        void ShowAddressMenu(Button button, ContextMenu menu)
        {
            menu.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            menu.PlacementTarget = button;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            System.Windows.Shapes.Path arrow = button.Content as System.Windows.Shapes.Path;
            if (arrow != null)
            {
                arrow.RenderTransform = new RotateTransform(90);
                menu.Closed += (object sender, RoutedEventArgs e) => { arrow.RenderTransform = null; };
            }
            menu.IsOpen = true;
        }

        #endregion Address bar

        #region View settings

        int view_mode = 3;
        GridView details_view = null;
        bool loading_view_settings = false;

        void SetViewMode(int mode)
        {
            if (details_view == null)
                details_view = directories_list.View as GridView;
            view_mode = mode;
            object selected = directories_list.SelectedItem;
            if (mode == 3)
            {
                directories_list.ClearValue(ItemsControl.ItemTemplateProperty);
                directories_list.ClearValue(ItemsControl.ItemsPanelProperty);
                directories_list.ClearValue(Control.HorizontalContentAlignmentProperty);
                directories_list.ClearValue(ScrollViewer.HorizontalScrollBarVisibilityProperty);
                directories_list.View = details_view;
            }
            else
            {
                directories_list.View = null;
                directories_list.ItemTemplate = (DataTemplate)FindResource(new string[] { "icons_template", "large_icons_template", "list_template", "", "tiles_template", "content_template", "columns_template" }[mode]);
                if (mode == 5 || mode == 6)
                {
                    directories_list.ClearValue(ItemsControl.ItemsPanelProperty);
                    directories_list.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                    ScrollViewer.SetHorizontalScrollBarVisibility(directories_list, ScrollBarVisibility.Disabled);
                }
                else
                {
                    FrameworkElementFactory panel = new FrameworkElementFactory(typeof(VirtualizingWrapPanel));
                    panel.SetValue(VirtualizingWrapPanel.ItemWidthProperty, new double[] { 92, 132, 242, 0, 256 }[mode]);
                    panel.SetValue(VirtualizingWrapPanel.ItemHeightProperty, new double[] { 92, 140, 24, 0, 62 }[mode]);
                    panel.SetValue(VirtualizingWrapPanel.OrientationProperty, mode == 2 ? Orientation.Vertical : Orientation.Horizontal);
                    directories_list.ItemsPanel = new ItemsPanelTemplate(panel);
                    directories_list.ClearValue(Control.HorizontalContentAlignmentProperty);
                    directories_list.ClearValue(ScrollViewer.HorizontalScrollBarVisibilityProperty);
                }
            }
            columns_current.Width = mode == 6 ? new GridLength(220) : new GridLength(1, GridUnitType.Star);
            columns_scroll.HorizontalScrollBarVisibility = mode == 6 ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled;
            if (mode == 6)
                directories_list.BorderThickness = new Thickness(1, 0, 1, 1);
            else
                directories_list.ClearValue(Control.BorderThicknessProperty);
            if (selected != null)
                directories_list.ScrollIntoView(selected);
            BuildColumns();
            FitAddressBar();
        }

        void LoadViewSettings()
        {
            loading_view_settings = true;
            string value;
            int number;
            if ((value = Functions.ReadSetting("show_hidden_files")) != null)
                view_show_hidden.IsChecked = value == "1";
            if ((value = Functions.ReadSetting("show_extension")) != null)
                view_show_extension.IsChecked = value == "1";
            if (int.TryParse(Functions.ReadSetting("sort_by"), out number) && number >= 0)
            {
                RadioButton[] sort_buttons = { view_sort_random, view_sort_name, view_sort_size, view_sort_type, null, view_sort_date };
                if (number % 100 < sort_buttons.Length && sort_buttons[number % 100] != null)
                {
                    sort_buttons[number % 100].IsChecked = true;
                    view_sort_ascending.IsChecked = number < 100;
                }
            }
            if (int.TryParse(Functions.ReadSetting("view_mode"), out number) && number >= 0 && number < 7)
                view_mode_list.SelectedIndex = number;
            loading_view_settings = false;
        }

        void SetSort(SortItemsBy key)
        {
            sort_by = view_sort_ascending != null && view_sort_ascending.IsChecked == false ? key + 100 : key;
            ViewSettingChanged();
        }

        void ViewSettingChanged()
        {
            if (!IsLoaded || loading_view_settings)
                return;
            Functions.SaveSetting("show_hidden_files", show_hiddin_files ? "1" : "0");
            Functions.SaveSetting("show_extension", show_extension ? "1" : "0");
            Functions.SaveSetting("sort_by", ((int)sort_by).ToString());
            if (CurrentPath != "root" && !IsBrowsing())
                Refresh();
        }

        #endregion View settings

        #region Columns view

        Dictionary<string, List<DirectoryOrFile>> columns_cache = new Dictionary<string, List<DirectoryOrFile>>();
        string list_filter = "";
        string select_after_browse = null;
        List<string> reselect_after_browse = null;
        bool menu_after_browse = false;
        int columns_token = 0, columns_preview_token = 0, columns_depth = -1;
        List<ColumnLink> column_links = new List<ColumnLink>();
        bool column_links_queued = false;
        static string[] preview_image_extensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".ico" };

        static string ColumnKey(string directory)
        {
            return directory.TrimEnd('\\').ToLowerInvariant();
        }

        List<string> ColumnsChain(string directory)
        {
            List<string> chain = new List<string>();
            try
            {
                string root = Path.GetPathRoot(directory), current = root.EndsWith("\\") ? root : root + "\\";
                foreach (var i in directory.Substring(root.Length).Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    chain.Add(current);
                    current = Path.Combine(current, i) + "\\";
                }
            }
            catch (Exception)
            {
                chain.Clear();
            }
            return chain;
        }

        bool ReadColumn(string directory, List<DirectoryInfo> sub_directories, List<FileInfo> files)
        {
            try
            {
                DirectoryInfo current = new DirectoryInfo(directory);
                sub_directories.AddRange(current.GetDirectories());
                files.AddRange(current.GetFiles());
                if (sort_by == SortItemsBy.Random || sort_by == SortItemsBy.RandomDescending)
                {
                    Shuffle(sub_directories);
                    Shuffle(files);
                }
                else
                {
                    sub_directories.Sort(DirectorySorter);
                    files.Sort(FilesSorter);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        List<DirectoryOrFile> ColumnItems(List<DirectoryInfo> sub_directories, List<FileInfo> files)
        {
            List<DirectoryOrFile> items = new List<DirectoryOrFile>();
            foreach (var i in sub_directories)
                if (show_hiddin_files || !i.Attributes.HasFlag(FileAttributes.Hidden))
                    items.Add(new DirectoryOrFile(i, show_extension, true));
            foreach (var i in files)
                if (show_hiddin_files || !i.Attributes.HasFlag(FileAttributes.Hidden))
                    items.Add(new DirectoryOrFile(i, show_extension, false));
            return items;
        }

        void PrepareColumn(ListBox column)
        {
            column.Focusable = false;
            column.ItemContainerStyle = (Style)FindResource("column_item");
            column.AllowDrop = true;
            column.PreviewMouseDown += ColumnItem_PreviewMouseDown;
            column.AddHandler(UIElement.MouseRightButtonUpEvent, new MouseButtonEventHandler(ColumnItem_MouseButtonUp), true);
            column.DragOver += Column_DragOver;
            column.Drop += Column_Drop;
        }

        ListBox CreateColumn(string directory)
        {
            ListBox column = new ListBox();
            column.Width = 220;
            column.Tag = directory;
            column.ToolTip = directory;
            column.Background = Brushes.White;
            column.Foreground = directories_list.Foreground;
            column.BorderBrush = columns_preview.BorderBrush;
            column.BorderThickness = new Thickness(1, 0, 0, 1);
            column.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            column.ItemTemplate = (DataTemplate)FindResource("columns_template");
            ScrollViewer.SetHorizontalScrollBarVisibility(column, ScrollBarVisibility.Disabled);
            VirtualizingStackPanel.SetIsVirtualizing(column, true);
            VirtualizingStackPanel.SetVirtualizationMode(column, VirtualizationMode.Recycling);
            PrepareColumn(column);
            column.MouseEnter += ColumnLink_MouseEnter;
            column.MouseLeave += ColumnLink_MouseLeave;
            return column;
        }

        void FillColumn(ListBox column, List<DirectoryOrFile> items, string selected)
        {
            column.ItemsSource = items;
            string key = ColumnKey(selected);
            DirectoryOrFile item = items.FirstOrDefault(x => ColumnKey(x.FullPath) == key);
            if (item != null)
            {
                column.SelectedItem = item;
                column.ScrollIntoView(item);
            }
            QueueColumnLinks();
        }

        string ColumnTitle(string directory)
        {
            try
            {
                string root = Path.GetPathRoot(directory);
                if (string.Equals(root.TrimEnd('\\'), directory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                    return DriveTitle(root.EndsWith("\\") ? root : root + "\\");
                return Path.GetFileName(directory.TrimEnd('\\'));
            }
            catch (Exception)
            {
                return directory;
            }
        }

        ImageSource ColumnIcon(string directory)
        {
            RootItem drive = root_data.FirstOrDefault(x => x.isDrive && string.Equals(x.toolTip.TrimEnd('\\'), directory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
            if (drive != null && drive.Image != null)
                return drive.Image;
            return Functions.GetDirectoryImage(directory);
        }

        static SolidColorBrush AccentBrush(int alpha)
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromArgb((byte)Math.Max(0, Math.Min(255, alpha)), 0x00, 0x78, 0xD7));
            brush.Freeze();
            return brush;
        }

        void PaintColumnLink(ColumnLink link, bool hover)
        {
            if (link.Column == columns_preview)
            {
                link.Header.Background = new SolidColorBrush(hover ? Color.FromRgb(0xE8, 0xE8, 0xE8) : Color.FromRgb(0xF2, 0xF2, 0xF2));
                columns_preview.Background = new SolidColorBrush(hover ? Color.FromRgb(0xF2, 0xF2, 0xF2) : Color.FromRgb(0xFA, 0xFA, 0xFA));
                link.Underline.Fill = Brushes.Transparent;
                return;
            }
            int step = Math.Min(link.Depth, 6);
            link.Header.Background = AccentBrush(10 + step * 4 + (link.Current ? 16 : 0) + (hover ? 24 : 0));
            Brush tint = AccentBrush(2 + step * 2 + (link.Current ? 6 : 0) + (hover ? 10 : 0));
            if (link.Column is Control)
                ((Control)link.Column).Background = tint;
            else if (link.Column is Border)
                ((Border)link.Column).Background = tint;
            link.Underline.Fill = link.Current ? AccentBrush(255) : hover ? AccentBrush(110) : Brushes.Transparent;
        }

        Border CreateColumnHeader(string title, ImageSource icon, string tip, string directory, string select, FrameworkElement column, int depth, bool current)
        {
            DockPanel dock = new DockPanel();
            if (directory != null)
            {
                Button chevron = new Button() { Style = (Style)FindResource("address_button"), Padding = new Thickness(6, 0, 6, 0), ToolTip = "Subdirectories", Tag = directory };
                chevron.Content = new System.Windows.Shapes.Path() { Data = Geometry.Parse("M 0,0 L 3.5,3.5 L 0,7"), Stroke = new SolidColorBrush(Color.FromRgb(0x41, 0x40, 0x3B)), StrokeThickness = 1.2, RenderTransformOrigin = new Point(0.5, 0.5), VerticalAlignment = VerticalAlignment.Center };
                chevron.Click += AddressChevron_Click;
                DockPanel.SetDock(chevron, Dock.Right);
                dock.Children.Add(chevron);
            }
            if (column == columns_preview)
            {
                TextBlock mark = new TextBlock() { Text = "preview", FontSize = 11, FontStyle = FontStyles.Italic, Foreground = Brushes.Gray, Margin = new Thickness(4, 0, directory == null ? 8 : 2, 0), VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
                DockPanel.SetDock(mark, Dock.Right);
                dock.Children.Add(mark);
            }
            DockPanel label = new DockPanel();
            if (icon != null)
                label.Children.Add(new Image() { Source = icon, Width = 16, Height = 16, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            label.Children.Add(new TextBlock() { Text = title, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, FontWeight = current ? FontWeights.SemiBold : FontWeights.Normal });
            Button main = new Button() { Style = (Style)FindResource("column_header_button"), Content = label, ToolTip = tip };
            if (directory != null)
            {
                main.Cursor = Cursors.Hand;
                main.Click += (object sender, RoutedEventArgs e) =>
                {
                    if (current)
                    {
                        FocusExplorerList();
                        return;
                    }
                    if (IsBrowsing())
                        return;
                    select_after_browse = select;
                    RootItemBrowse(CurrentPath, directory, true, "", false);
                };
            }
            else
                main.IsHitTestVisible = false;
            dock.Children.Add(main);
            System.Windows.Shapes.Rectangle underline = new System.Windows.Shapes.Rectangle() { Height = current ? 2 : 1, VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false };
            Grid inner = new Grid();
            inner.Children.Add(dock);
            inner.Children.Add(underline);
            Border cell = new Border() { Height = 28, BorderBrush = columns_preview.BorderBrush, Child = inner, ToolTip = directory == null ? tip : null };
            if (column == columns_preview)
                cell.BorderThickness = new Thickness(0, 1, 1, 0);
            else if (current)
                cell.BorderThickness = new Thickness(1, 1, 1, 0);
            else
            {
                cell.BorderThickness = new Thickness(1, 1, 0, 0);
                cell.Width = 220;
            }
            ColumnLink link = new ColumnLink() { Header = cell, Underline = underline, Column = column, Depth = depth, Current = current };
            cell.MouseEnter += ColumnLink_MouseEnter;
            cell.MouseLeave += ColumnLink_MouseLeave;
            PaintColumnLink(link, false);
            column_links.Add(link);
            return cell;
        }

        ColumnLink FindColumnLink(object element)
        {
            return column_links.FirstOrDefault(x => x.Header == element || x.Column == element);
        }

        private void ColumnLink_MouseEnter(object sender, MouseEventArgs e)
        {
            ColumnLink link = FindColumnLink(sender);
            if (link != null)
                PaintColumnLink(link, true);
        }

        private void ColumnLink_MouseLeave(object sender, MouseEventArgs e)
        {
            ColumnLink link = FindColumnLink(sender);
            if (link != null)
                PaintColumnLink(link, link.Header.IsMouseOver || link.Column.IsMouseOver);
        }

        void SlideColumnIn(params FrameworkElement[] elements)
        {
            if (!SystemParameters.ClientAreaAnimation)
                return;
            foreach (var i in elements)
            {
                TranslateTransform move = new TranslateTransform(28, 0);
                i.RenderTransform = move;
                DoubleAnimation slide = new DoubleAnimation(28, 0, new Duration(TimeSpan.FromMilliseconds(150))) { EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut } };
                slide.Completed += (object sender, EventArgs e) => { QueueColumnLinks(); };
                move.BeginAnimation(TranslateTransform.XProperty, slide);
                i.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(150))));
            }
        }

        void QueueColumnLinks()
        {
            if (column_links_queued)
                return;
            column_links_queued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                column_links_queued = false;
                DrawColumnLinks();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ColumnsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueColumnLinks();
        }

        void DrawColumnLinks()
        {
            columns_links.Children.Clear();
            if (view_mode != 6 || CloseWindow || columns_scroll.Visibility != Visibility.Visible || column_links.Count < 2)
                return;
            for (int i = 0; i + 1 < column_links.Count; i++)
            {
                ListBox list = column_links[i].Column as ListBox;
                Border next = column_links[i + 1].Header;
                if (list == null || list.SelectedItems.Count != 1 || !next.IsVisible || !list.IsVisible)
                    continue;
                FrameworkElement row = list.ItemContainerGenerator.ContainerFromItem(list.SelectedItem) as FrameworkElement;
                if (row == null || !row.IsVisible)
                    continue;
                try
                {
                    Point start = row.TranslatePoint(new Point(row.ActualWidth, row.ActualHeight / 2), columns_links);
                    Point top = list.TranslatePoint(new Point(0, 0), columns_links);
                    Point end = next.TranslatePoint(new Point(0, next.ActualHeight), columns_links);
                    double y = Math.Max(top.Y + 4, Math.Min(top.Y + list.ActualHeight - 4, start.Y));
                    if (y <= end.Y + 1)
                        continue;
                    double r = Math.Min(6, y - end.Y), from_x = Math.Min(start.X, end.X - r);
                    PathFigure figure = new PathFigure() { StartPoint = new Point(from_x, y) };
                    figure.Segments.Add(new LineSegment(new Point(end.X - r, y), true));
                    figure.Segments.Add(new QuadraticBezierSegment(new Point(end.X, y), new Point(end.X, y - r), true));
                    figure.Segments.Add(new LineSegment(new Point(end.X, end.Y), true));
                    PathGeometry geometry = new PathGeometry();
                    geometry.Figures.Add(figure);
                    bool preview = column_links[i + 1].Column == columns_preview;
                    double opacity = (Math.Abs(y - start.Y) > 1 ? 0.35 : 0.8) * (preview ? 0.7 : 1);
                    System.Windows.Shapes.Path line = new System.Windows.Shapes.Path() { Data = geometry, Stroke = AccentBrush(255), StrokeThickness = 1.5, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, StrokeLineJoin = PenLineJoin.Round, Opacity = opacity };
                    if (preview)
                    {
                        line.StrokeDashArray = new DoubleCollection() { 2, 2.5 };
                        line.StrokeDashCap = PenLineCap.Round;
                    }
                    columns_links.Children.Add(line);
                    System.Windows.Shapes.Ellipse dot = new System.Windows.Shapes.Ellipse() { Width = 5, Height = 5, Fill = AccentBrush(255), Opacity = opacity };
                    Canvas.SetLeft(dot, end.X - 2.5);
                    Canvas.SetTop(dot, end.Y - 2.5);
                    columns_links.Children.Add(dot);
                }
                catch (Exception) { }
            }
        }

        void BuildColumns()
        {
            columns_token++;
            int token = columns_token;
            EndLoading("columns");
            columns_parents.Children.Clear();
            columns_headers.Children.Clear();
            column_links.Clear();
            columns_current_header.Child = null;
            if (view_mode != 6 || CurrentPath == "root" || directories_list.Visibility != Visibility.Visible)
            {
                columns_parents.Visibility = columns_headers.Visibility = columns_current_header.Visibility = Visibility.Collapsed;
                directories_list.Background = Brushes.White;
                columns_preview.Background = Brushes.White;
                columns_depth = -1;
                UpdateColumnsPreview();
                return;
            }
            List<string> chain = ColumnsChain(CurrentPath);
            HashSet<string> keys = new HashSet<string>(chain.Select(x => ColumnKey(x)));
            foreach (var i in columns_cache.Keys.Where(x => !keys.Contains(x)).ToList())
                columns_cache.Remove(i);
            List<string> missing = new List<string>();
            for (int i = 0; i < chain.Count; i++)
            {
                ListBox column = CreateColumn(chain[i]);
                columns_parents.Children.Add(column);
                string next = i + 1 < chain.Count ? chain[i + 1] : CurrentPath;
                columns_headers.Children.Add(CreateColumnHeader(ColumnTitle(chain[i]), ColumnIcon(chain[i]), chain[i], chain[i], next, column, i, false));
                if (columns_cache.ContainsKey(ColumnKey(chain[i])))
                    FillColumn(column, columns_cache[ColumnKey(chain[i])], next);
                else
                    missing.Add(chain[i]);
            }
            columns_current_header.Child = CreateColumnHeader(ColumnTitle(CurrentPath), ColumnIcon(CurrentPath), CurrentPath, CurrentPath, null, directories_list, chain.Count, true);
            columns_parents.Visibility = columns_headers.Visibility = chain.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            columns_current_header.Visibility = Visibility.Visible;
            if (columns_depth < 0)
            {
                if (SystemParameters.ClientAreaAnimation)
                    foreach (UIElement i in new UIElement[] { columns_headers, columns_current_header })
                        i.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(150))));
            }
            else if (chain.Count > columns_depth)
                SlideColumnIn(directories_list, columns_current_header);
            columns_depth = chain.Count;
            UpdateColumnsPreview();
            ScrollColumnsToEnd();
            QueueColumnLinks();
            if (missing.Count == 0)
                return;
            BeginLoading("columns");
            Dictionary<string, KeyValuePair<List<DirectoryInfo>, List<FileInfo>>> results = new Dictionary<string, KeyValuePair<List<DirectoryInfo>, List<FileInfo>>>();
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                foreach (var i in missing)
                {
                    if (token != columns_token)
                        return;
                    List<DirectoryInfo> sub_directories = new List<DirectoryInfo>();
                    List<FileInfo> files = new List<FileInfo>();
                    if (ReadColumn(i, sub_directories, files))
                        results[i] = new KeyValuePair<List<DirectoryInfo>, List<FileInfo>>(sub_directories, files);
                }
            };
            worker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (token != columns_token || CloseWindow)
                    return;
                EndLoading("columns");
                for (int i = 0; i < chain.Count && i < columns_parents.Children.Count; i++)
                {
                    ListBox column = columns_parents.Children[i] as ListBox;
                    if (results.ContainsKey(chain[i]))
                    {
                        List<DirectoryOrFile> items = ColumnItems(results[chain[i]].Key, results[chain[i]].Value);
                        columns_cache[ColumnKey(chain[i])] = items;
                        FillColumn(column, items, i + 1 < chain.Count ? chain[i + 1] : CurrentPath);
                    }
                    else if (missing.Contains(chain[i]))
                        column.ToolTip = "We can not read '" + chain[i] + "'";
                }
                ScrollColumnsToEnd();
            };
            worker.RunWorkerAsync();
        }

        void UpdateColumnsPreview()
        {
            columns_preview_token++;
            int token = columns_preview_token;
            EndLoading("preview");
            column_links.RemoveAll(x => x.Column == columns_preview);
            if (view_mode != 6 || CurrentPath == "root" || directories_list.Visibility != Visibility.Visible || directories_list.SelectedItems.Count == 0 || !(directories_list.SelectedItem is DirectoryOrFile) || (directories_list.SelectedItem as DirectoryOrFile).fileinfo == null)
            {
                columns_preview.Visibility = columns_preview_header.Visibility = Visibility.Collapsed;
                columns_preview_header.Child = null;
                columns_preview_list.ItemsSource = null;
                columns_preview_image.Source = null;
                QueueColumnLinks();
                return;
            }
            bool was_hidden = columns_preview.Visibility != Visibility.Visible;
            int depth = ColumnsChain(CurrentPath).Count + 1;
            DirectoryOrFile item = directories_list.SelectedItem as DirectoryOrFile;
            if (directories_list.SelectedItems.Count > 1)
                columns_preview_header.Child = CreateColumnHeader(directories_list.SelectedItems.Count + " items selected", null, CurrentPath, null, null, columns_preview, depth, false);
            else if (item.isDirectory)
                columns_preview_header.Child = CreateColumnHeader(item.FileName, item.Icon, "Preview of '" + item.FullPath + "'. Click here or double-click the folder to open it", item.FullPath, "", columns_preview, depth, false);
            else
                columns_preview_header.Child = CreateColumnHeader(item.FileName, item.Icon, "Preview of '" + item.FullPath + "'", null, null, columns_preview, depth, false);
            columns_preview.Visibility = columns_preview_header.Visibility = Visibility.Visible;
            if (was_hidden)
                SlideColumnIn(columns_preview, columns_preview_header);
            QueueColumnLinks();
            columns_preview_list.Visibility = columns_preview_info.Visibility = Visibility.Collapsed;
            columns_preview_text.Visibility = Visibility.Visible;
            columns_preview_list.ItemsSource = null;
            columns_preview_image.Source = null;
            ScrollColumnsToEnd();
            if (directories_list.SelectedItems.Count > 1)
            {
                columns_preview_text.Text = directories_list.SelectedItems.Count + " items selected";
                return;
            }
            if (item.isDirectory)
            {
                string directory = item.FullPath.EndsWith("\\") ? item.FullPath : item.FullPath + "\\";
                columns_preview_list.Tag = directory;
                columns_preview_text.Text = "";
                BeginLoading("preview");
                List<DirectoryInfo> sub_directories = new List<DirectoryInfo>();
                List<FileInfo> files = new List<FileInfo>();
                bool done = false;
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    Thread.Sleep(150);
                    if (token == columns_preview_token)
                        done = ReadColumn(directory, sub_directories, files);
                };
                worker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    if (token != columns_preview_token || CloseWindow)
                        return;
                    EndLoading("preview");
                    if (!done)
                    {
                        columns_preview_text.Text = "We can not read this directory.";
                        return;
                    }
                    List<DirectoryOrFile> items = ColumnItems(sub_directories, files);
                    columns_preview_list.ItemsSource = items;
                    columns_preview_list.Visibility = Visibility.Visible;
                    if (items.Count == 0)
                        columns_preview_text.Text = "This directory is empty.";
                    else
                        columns_preview_text.Visibility = Visibility.Collapsed;
                };
                worker.RunWorkerAsync();
            }
            else
            {
                FileInfo file = item.fileinfo as FileInfo;
                columns_preview_text.Visibility = Visibility.Collapsed;
                columns_preview_info.Visibility = Visibility.Visible;
                columns_preview_info.ScrollToTop();
                columns_preview_image.Source = item.HugeIcon;
                columns_preview_name.Text = file.Name;
                columns_preview_details.Text = "Type: " + item.FileType + "\nSize: " + item.FileSize + "\nModified: " + item.DateModified + "\nCreated: " + Functions.OrginizeDate(file.CreationTime);
                if (preview_image_extensions.Contains(file.Extension.ToLower()) && file.Length < 52428800)
                {
                    string path = file.FullName;
                    ThreadPool.QueueUserWorkItem((object state) =>
                    {
                        try
                        {
                            BitmapImage image = new BitmapImage();
                            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                            {
                                image.BeginInit();
                                image.CacheOption = BitmapCacheOption.OnLoad;
                                image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                                image.DecodePixelWidth = 472;
                                image.StreamSource = stream;
                                image.EndInit();
                            }
                            image.Freeze();
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (token == columns_preview_token)
                                    columns_preview_image.Source = image;
                            }));
                        }
                        catch (Exception) { }
                    });
                }
            }
        }

        void ScrollColumnsToEnd()
        {
            if (view_mode == 6)
                Dispatcher.BeginInvoke(new Action(() => { columns_scroll.ScrollToRightEnd(); }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        void ColumnsRemember()
        {
            if (view_mode == 6 && CurrentPath != "root" && list_filter == "" && directories_list.Visibility == Visibility.Visible)
                columns_cache[ColumnKey(CurrentPath)] = new List<DirectoryOrFile>(directories_data);
        }

        void ColumnsBrowsed()
        {
            string target = select_after_browse;
            bool menu = menu_after_browse;
            List<string> others = reselect_after_browse;
            select_after_browse = null;
            reselect_after_browse = null;
            menu_after_browse = false;
            BuildColumns();
            if (target == null)
                return;
            string key = ColumnKey(target);
            DirectoryOrFile item = target == "" ? directories_data.FirstOrDefault() : directories_data.FirstOrDefault(x => ColumnKey(x.FullPath) == key);
            if (item == null)
            {
                directories_list.Focus();
                return;
            }
            directories_list.SelectedItem = item;
            if (others != null && others.Count > 1)
                foreach (var i in directories_data.Where(x => x != item && others.Contains(ColumnKey(x.FullPath))).ToList())
                    directories_list.SelectedItems.Add(i);
            directories_list.ScrollIntoView(item);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ListViewItem container = directories_list.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                if (container != null)
                    container.Focus();
                else
                    directories_list.Focus();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
            if (menu)
            {
                opening_menu = true;
                IsOpenned = true;
                OpennedMenu = 2;
                opening_menu = false;
            }
        }

        private void ColumnItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ListBox column = sender as ListBox;
            ListBoxItem item = e.OriginalSource is DependencyObject ? ItemsControl.ContainerFromElement(column, (DependencyObject)e.OriginalSource) as ListBoxItem : null;
            if (item == null || !(item.Content is DirectoryOrFile))
                return;
            e.Handled = true;
            if (e.ChangedButton != MouseButton.Left || e.ClickCount != 2 || IsBrowsing())
                return;
            DirectoryOrFile target = item.Content as DirectoryOrFile;
            if (target.isDirectory)
                RootItemBrowse(CurrentPath, target.FullPath, true, "", false);
            else
                RunFile(target.FullPath);
        }

        private void ColumnItem_MouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListBox column = sender as ListBox;
            ListBoxItem item = e.OriginalSource is DependencyObject ? ItemsControl.ContainerFromElement(column, (DependencyObject)e.OriginalSource) as ListBoxItem : null;
            if (item == null || !(item.Content is DirectoryOrFile) || !(column.Tag is string))
                return;
            e.Handled = true;
            if (IsBrowsing())
                return;
            select_after_browse = (item.Content as DirectoryOrFile).FullPath;
            menu_after_browse = e.ChangedButton == MouseButton.Right;
            RootItemBrowse(CurrentPath, column.Tag as string, true, "", false);
        }

        string ColumnDropTarget(ListBox column, DragEventArgs e)
        {
            ListBoxItem item = e.OriginalSource is DependencyObject ? ItemsControl.ContainerFromElement(column, (DependencyObject)e.OriginalSource) as ListBoxItem : null;
            if (item != null && item.Content is DirectoryOrFile && (item.Content as DirectoryOrFile).isDirectory)
                return (item.Content as DirectoryOrFile).FullPath;
            return column.Tag as string;
        }

        private void Column_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = GetDropEffect(e, ColumnDropTarget(sender as ListBox, e));
            e.Handled = true;
        }

        private void Column_Drop(object sender, DragEventArgs e)
        {
            DropItems(e, ColumnDropTarget(sender as ListBox, e));
        }

        private void DirectoryList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (view_mode != 6 || CurrentPath == "root" || Keyboard.Modifiers != ModifierKeys.None || (e.Key != Key.Left && e.Key != Key.Right))
                return;
            e.Handled = true;
            if (IsBrowsing())
                return;
            if (e.Key == Key.Right)
            {
                DirectoryOrFile item = directories_list.SelectedItem as DirectoryOrFile;
                if (directories_list.SelectedItems.Count == 1 && item != null && item.isDirectory)
                {
                    select_after_browse = "";
                    RootItemBrowse(CurrentPath, item.FullPath, true, "", false);
                }
            }
            else
            {
                DirectoryInfo parent = null;
                try
                {
                    parent = new DirectoryInfo(CurrentPath).Parent;
                }
                catch (Exception) { }
                if (parent != null)
                {
                    select_after_browse = CurrentPath;
                    RootItemBrowse(CurrentPath, parent.FullName, true, "", false);
                }
            }
        }

        private void ColumnsPreviewOpen_Click(object sender, RoutedEventArgs e)
        {
            DirectoryOrFile item = directories_list.SelectedItem as DirectoryOrFile;
            if (item != null && !item.isDirectory)
                RunFile(item.FullPath);
        }

        #endregion Columns view

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
            walk_token++;
            CloseWindow = true;
            if (loading_timer != null)
                loading_timer.Stop();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (IsOpenned && !IsActive && !Application.Current.Windows.Cast<Window>().Any(x => x.IsActive))
                    IsOpenned = false;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }


        Dictionary<string, bool> bookmark_dictionary = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private void ClearHistor_btm_Click(object sender, RoutedEventArgs e)
        {
            if (back_menu_data.Count < 2 || Messages.ShowMessage("Clear the history of this window?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "The current location stays in the list.") != MyMessageBoxResult.Yes)
                return;
            if (CurrentBackIndex > -1 && CurrentBackIndex < back_menu_data.Count)
            {
                var item = back_menu_data[CurrentBackIndex];
                back_menu_data.Clear();
                back_menu_data.Add(item);
                current_back_index = 0;
                history_list.Items.Refresh();
                history_list.SelectedIndex = 0;
            }
            UpdateHistoryClear();
        }

        void UpdateHistoryClear()
        {
            history_clear_btm.IsEnabled = back_menu_data.Count > 1;
            history_clear_btm.ToolTip = history_clear_btm.IsEnabled ? "Clear the history (the current location stays)" : "The history is empty";
        }

        private void bookmark_AddThisLocation_Click(object sender, RoutedEventArgs e)
        {
            AddBookmark(CurrentPath);
        }

        private void bookmark_Star_Click(object sender, RoutedEventArgs e)
        {
            LoadBookmarks();
            if (bookmark_dictionary.ContainsKey(CurrentPath))
                RemoveBookmark(CurrentPath);
            else
                AddBookmark(CurrentPath);
        }

        private void bookmark_Manage_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = new ContextMenu();
            ListBoxItem selected = bookmark_list.SelectedItem as ListBoxItem;
            if (selected == null || selected.ContentStringFormat == null)
                menu.Items.Add(new MenuItem() { Header = "Select a bookmark to open, rename, move or remove it", IsEnabled = false });
            else
                FillBookmarkMenu(menu, selected.ContentStringFormat);
            menu.Items.Add(new Separator());
            MenuItem menu_item = new MenuItem();
            menu_item.Header = "Remove missing bookmarks";
            menu_item.Click += (object sender2, RoutedEventArgs e2) =>
            {
                List<string[]> list = ReadBookmarks();
                list.RemoveAll(x => !Directory.Exists(x[0]));
                SaveBookmarks(list);
            };
            menu.Items.Add(menu_item);
            menu_item = new MenuItem();
            menu_item.Header = "Sort by name";
            menu_item.Click += (object sender2, RoutedEventArgs e2) =>
            {
                SaveBookmarks(ReadBookmarks().OrderBy(x => BookmarkName(x), StringComparer.CurrentCultureIgnoreCase).ToList());
            };
            menu.Items.Add(menu_item);
            menu.PlacementTarget = sender as UIElement;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        void FillBookmarkMenu(ContextMenu menu, string path)
        {
            MenuItem menu_item = new MenuItem();
            menu_item.Header = "Open";
            menu_item.FontWeight = FontWeights.Bold;
            menu_item.Click += (object sender, RoutedEventArgs e) =>
            {
                if (!IsBrowsing())
                    RootItemBrowse(CurrentPath, path, true, "", false);
            };
            menu.Items.Add(menu_item);
            menu_item = new MenuItem();
            menu_item.Header = "Open in new window";
            menu_item.Click += (object sender, RoutedEventArgs e) =>
            {
                if (Directory.Exists(path))
                    ExplorePath(path);
                else
                    Messages.ShowMessage("We can not find directory '" + path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            };
            menu.Items.Add(menu_item);
            menu.Items.Add(new Separator());
            menu_item = new MenuItem();
            menu_item.Header = "Rename";
            menu_item.Click += (object sender, RoutedEventArgs e) => { RenameBookmark(path); };
            menu.Items.Add(menu_item);
            menu_item = new MenuItem();
            menu_item.Header = "Move up";
            menu_item.Click += (object sender, RoutedEventArgs e) => { MoveBookmark(path, -1); };
            menu.Items.Add(menu_item);
            menu_item = new MenuItem();
            menu_item.Header = "Move down";
            menu_item.Click += (object sender, RoutedEventArgs e) => { MoveBookmark(path, 1); };
            menu.Items.Add(menu_item);
            menu.Items.Add(new Separator());
            menu_item = new MenuItem();
            menu_item.Header = "Remove";
            menu_item.Click += (object sender, RoutedEventArgs e) => { RemoveBookmark(path); };
            menu.Items.Add(menu_item);
        }

        void AddBookmark(string path)
        {
            LoadBookmarks();
            if (path == "root")
                Messages.ShowMessage("This location can not be added to the bookmarks.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else if (bookmark_dictionary.ContainsKey(path))
                Messages.ShowMessage("This path is already exist in this list!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else
            {
                List<string[]> list = ReadBookmarks();
                list.Add(new string[] { path, "" });
                SaveBookmarks(list);
            }
        }

        void RemoveBookmark(string path)
        {
            List<string[]> list = ReadBookmarks();
            list.RemoveAll(x => string.Equals(x[0], path, StringComparison.OrdinalIgnoreCase));
            SaveBookmarks(list);
        }

        void MoveBookmark(string path, int offset)
        {
            List<string[]> list = ReadBookmarks();
            int index = list.FindIndex(x => string.Equals(x[0], path, StringComparison.OrdinalIgnoreCase));
            if (index < 0 || index + offset < 0 || index + offset >= list.Count)
                return;
            string[] item = list[index];
            list.RemoveAt(index);
            list.Insert(index + offset, item);
            SaveBookmarks(list);
        }

        void RenameBookmark(string path)
        {
            List<string[]> list = ReadBookmarks();
            string[] item = list.FirstOrDefault(x => string.Equals(x[0], path, StringComparison.OrdinalIgnoreCase));
            if (item == null)
                return;
            string new_name = new Rename().ShowWindow(BookmarkName(item), false);
            if (new_name != null)
            {
                item[1] = new_name.Trim();
                SaveBookmarks(list);
            }
        }

        static string BookmarkName(string[] bookmark)
        {
            if (bookmark[1].Length > 0)
                return bookmark[1];
            try
            {
                DirectoryInfo dir = new DirectoryInfo(bookmark[0]);
                return dir.Parent == null ? dir.FullName : dir.Name;
            }
            catch (Exception)
            {
                return bookmark[0];
            }
        }

        static string BookmarksFile()
        {
            return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "File Manager"), "bookmarks.txt");
        }

        static object bookmark_lock = new object();

        static List<string[]> ReadBookmarks()
        {
            List<string[]> list = new List<string[]>();
            try
            {
                lock (bookmark_lock)
                    if (File.Exists(BookmarksFile()))
                        foreach (var i in File.ReadAllLines(BookmarksFile()))
                        {
                            int split = i.IndexOf('|');
                            string path = (split < 0 ? i : i.Substring(0, split)).Trim(), name = split < 0 ? "" : i.Substring(split + 1).Trim();
                            if (path.Length == 0 || path.IndexOfAny(Path.GetInvalidPathChars()) != -1 || !Path.IsPathRooted(path) || (path.StartsWith("\\") && !path.StartsWith("\\\\")))
                                continue;
                            if (!path.EndsWith("\\"))
                                path += "\\";
                            if (!list.Any(x => string.Equals(x[0], path, StringComparison.OrdinalIgnoreCase)))
                                list.Add(new string[] { path, name });
                        }
            }
            catch (Exception) { }
            return list;
        }

        void SaveBookmarks(List<string[]> list)
        {
            try
            {
                lock (bookmark_lock)
                {
                    string file = BookmarksFile(), temp = file + ".tmp";
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllLines(temp, list.Select(x => x[1].Length > 0 ? x[0] + "|" + x[1] : x[0]).ToArray());
                    if (File.Exists(file))
                        File.Replace(temp, file, null);
                    else
                        File.Move(temp, file);
                }
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not save the bookmarks.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, ex.Message);
            }
            LoadBookmarks();
        }

        Point bookmark_drag_start;
        ListBoxItem bookmark_drag_item = null;

        void LoadBookmarks()
        {
            string selected = bookmark_list.SelectedItem is ListBoxItem ? (bookmark_list.SelectedItem as ListBoxItem).ContentStringFormat : null;
            bookmark_dictionary.Clear();
            bookmark_list.Items.Clear();
            foreach (var i in ReadBookmarks())
            {
                string path = i[0];
                ListBoxItem item;
                try
                {
                    item = CreateQuickAccessItem(Functions.GetDirectoryImage(path), 24, BookmarkName(i), new DirectoryInfo(path), path);
                }
                catch (Exception)
                {
                    continue;
                }
                item.ToolTip = i[1].Length > 0 ? i[1] + "\n" + path : path;
                ContextMenu menu = new ContextMenu();
                FillBookmarkMenu(menu, path);
                item.ContextMenu = menu;
                item.PreviewMouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
                {
                    bookmark_drag_start = e.GetPosition(bookmark_list);
                    bookmark_drag_item = item;
                };
                item.PreviewMouseMove += (object sender, MouseEventArgs e) =>
                {
                    if (e.LeftButton == MouseButtonState.Pressed && bookmark_drag_item == item)
                    {
                        Vector distance = e.GetPosition(bookmark_list) - bookmark_drag_start;
                        if (Math.Abs(distance.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(distance.Y) > SystemParameters.MinimumVerticalDragDistance)
                        {
                            bookmark_drag_item = null;
                            DragDrop.DoDragDrop(item, new DataObject("FileManagerBookmark", path), DragDropEffects.Move);
                        }
                    }
                };
                bookmark_dictionary[path] = false;
                bookmark_list.Items.Add(item);
                if (string.Equals(path, selected, StringComparison.OrdinalIgnoreCase))
                    bookmark_list.SelectedItem = item;
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    if (!Directory.Exists(path))
                        Dispatcher.BeginInvoke(new Action(() => { item.Opacity = 0.5; }));
                });
            }
            if (bookmark_list.Items.Count == 0)
                bookmark_list.Items.Add(new ListBoxItem() { Content = "Press + to bookmark this location", FontSize = 12, Margin = new Thickness(5, 0, 0, 0), Foreground = new SolidColorBrush(Color.FromRgb(0xAF, 0xAF, 0xAF)), IsEnabled = false });
            UpdateBookmarkStar();
        }

        void UpdateBookmarkStar()
        {
            bool bookmarked = bookmark_dictionary.ContainsKey(CurrentPath);
            bookmark_star.IsEnabled = CurrentPath != "root";
            bookmark_star.Opacity = bookmarked ? 1 : 0.35;
            bookmark_star.ToolTip = CurrentPath == "root" ? "Open a directory to bookmark it" : bookmarked ? "Remove this location from the bookmarks" : "Add this location to the bookmarks";
        }

        private void BookmarkList_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent("FileManagerBookmark") ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void BookmarkList_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("FileManagerBookmark"))
                return;
            e.Handled = true;
            string path = e.Data.GetData("FileManagerBookmark") as string;
            List<string[]> list = ReadBookmarks();
            int index = list.FindIndex(x => string.Equals(x[0], path, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return;
            int target = list.Count;
            ListBoxItem target_item = e.OriginalSource is DependencyObject ? ItemsControl.ContainerFromElement(bookmark_list, (DependencyObject)e.OriginalSource) as ListBoxItem : null;
            if (target_item != null && target_item.ContentStringFormat != null)
            {
                target = list.FindIndex(x => string.Equals(x[0], target_item.ContentStringFormat, StringComparison.OrdinalIgnoreCase));
                if (target < 0)
                    target = list.Count;
                else if (e.GetPosition(target_item).Y > target_item.ActualHeight / 2)
                    target++;
            }
            string[] item = list[index];
            list.RemoveAt(index);
            if (target > index)
                target--;
            list.Insert(target, item);
            SaveBookmarks(list);
        }

        DragDropEffects GetDropEffect(DragEventArgs e, string target)
        {
            if (target == null || target == "root" || !e.Data.GetDataPresent(DataFormats.FileDrop))
                return DragDropEffects.None;
            string[] items = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (items == null || items.Length == 0)
                return DragDropEffects.None;
            if (e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
                return DragDropEffects.Copy;
            if (e.KeyStates.HasFlag(DragDropKeyStates.ShiftKey))
                return DragDropEffects.Move;
            try
            {
                string source_root = Path.GetPathRoot(items[0]), target_root = Path.GetPathRoot(target);
                if (source_root.StartsWith("\\\\") || target_root.StartsWith("\\\\") || new DriveInfo(target_root).DriveType == DriveType.Network)
                    return DragDropEffects.Copy;
                return string.Equals(source_root, target_root, StringComparison.OrdinalIgnoreCase) ? DragDropEffects.Move : DragDropEffects.Copy;
            }
            catch (Exception) { return DragDropEffects.Copy; }
        }

        void DropItems(DragEventArgs e, string target)
        {
            DragDropEffects effect = GetDropEffect(e, target);
            if (effect == DragDropEffects.None)
                return;
            List<FileOperationItem> list = new List<FileOperationItem>();
            foreach (string i in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                if (Directory.Exists(i))
                    list.Add(new FileOperationItem(new DirectoryInfo(i), effect == DragDropEffects.Move, new DirectoryInfo(i).Name));
                else if (File.Exists(i))
                    list.Add(new FileOperationItem(new FileInfo(i), effect == DragDropEffects.Move, new FileInfo(i).Name));
            }
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            if (list.Count > 0)
                new Operation().CopyDialogShow(list, OperationType.Copy, target, false);
        }

        private void DirectoryList_DragOver(object sender, DragEventArgs e)
        {
            string target = CurrentPath;
            if (e.OriginalSource is DependencyObject)
            {
                ListViewItem item = ItemsControl.ContainerFromElement(directories_list, (DependencyObject)e.OriginalSource) as ListViewItem;
                if (item != null && item.Content is DirectoryOrFile && (item.Content as DirectoryOrFile).isDirectory)
                    target = (item.Content as DirectoryOrFile).FullPath;
            }
            e.Effects = GetDropEffect(e, target);
            e.Handled = true;
        }

        private void DirectoryItem_DropOn(object sender, DragEventArgs e)
        {
            if (((sender as ListViewItem).Content as DirectoryOrFile).isDirectory)
                DropItems(e, ((sender as ListViewItem).Content as DirectoryOrFile).FullPath);
        }

        private void DirectoryList_Drop(object sender, DragEventArgs e)
        {
            if (!e.Handled && CurrentPath != "root")
                DropItems(e, CurrentPath);
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
                string special = Functions.SpecialDirectoryIcon(file.FullName);
                if (special != null)
                {
                    Icon = Icons.GetIcons.GetIcon(special, 24);
                    LargeIcon = Icons.GetIcons.GetIcon(special, 64);
                    HugeIcon = Icons.GetIcons.GetIcon(special, 256);
                }
                else
                {
                    Icon = FileInfoCombonents.FilesInformation["directory"].Bitmap24;
                    LargeIcon = FileInfoCombonents.FilesInformation["directory"].Bitmap64;
                    HugeIcon = FileInfoCombonents.FilesInformation["directory"].Bitmap256;
                }
                FileType = "Directory";
                FileSize = "";
            }
            else
            {
                FileSize = Functions.FileSize((file as FileInfo).Length, 2);
                if (FileInfoCombonents.FilesInformation.ContainsKey(file.Extension.ToLower()))
                {
                    Icon = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Bitmap24;
                    LargeIcon = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Bitmap64;
                    HugeIcon = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Bitmap256;
                    FileType = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Description;
                }
                else
                {
                    Icon = FileInfoCombonents.FilesInformation["."].Bitmap24;
                    LargeIcon = FileInfoCombonents.FilesInformation["."].Bitmap64;
                    HugeIcon = FileInfoCombonents.FilesInformation["."].Bitmap256;
                    FileType = file.Extension.Replace(".", "").ToUpper() + " File";
                }
            }
            DateModified = Functions.OrginizeDate(file.LastWriteTime);
        }

        public DirectoryOrFile(DriveInfo drive)
        {
            FullPath = drive.Name;
            this.drive = drive;
            isDrive = true;
            isDirectory = false;
            if (drive.IsReady)
            {
                FileName = Functions.GetDriveDescription(drive);
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
            LargeIcon = HugeIcon = Functions.GetDriveImage(drive, 64);
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

        private BitmapImage large_img_source;
        public BitmapImage LargeIcon
        {
            get { return large_img_source; }
            set { large_img_source = value; }
        }

        private BitmapImage huge_img_source;
        public BitmapImage HugeIcon
        {
            get { return huge_img_source; }
            set { huge_img_source = value; }
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
                    Name = Functions.GetDriveDescription(drive);
                }
                else
                {
                    Name = dir.Name;
                    Icon = Functions.GetDirectoryImage(DirectoryPath);
                }
            }

        }


    };

    class ColumnLink
    {
        internal Border Header;
        internal System.Windows.Shapes.Rectangle Underline;
        internal FrameworkElement Column;
        internal int Depth;
        internal bool Current;
    }

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

    public class VirtualizingWrapPanel : VirtualizingPanel, System.Windows.Controls.Primitives.IScrollInfo
    {
        public static readonly DependencyProperty ItemWidthProperty = DependencyProperty.Register("ItemWidth", typeof(double), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register("ItemHeight", typeof(double), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public double ItemWidth
        {
            get { return (double)GetValue(ItemWidthProperty); }
            set { SetValue(ItemWidthProperty, value); }
        }

        public double ItemHeight
        {
            get { return (double)GetValue(ItemHeightProperty); }
            set { SetValue(ItemHeightProperty, value); }
        }

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        Size extent = new Size(0, 0), viewport = new Size(0, 0);
        Point offset = new Point(0, 0);
        ScrollViewer owner;
        bool can_horizontally_scroll, can_vertically_scroll;
        int per_line = 1;

        bool IsHorizontal
        {
            get { return Orientation == Orientation.Horizontal; }
        }

        protected override bool HasLogicalOrientation
        {
            get { return true; }
        }

        protected override Orientation LogicalOrientation
        {
            get { return IsHorizontal ? Orientation.Vertical : Orientation.Horizontal; }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            UIElementCollection children = InternalChildren;
            System.Windows.Controls.Primitives.IItemContainerGenerator generator = ItemContainerGenerator;
            ItemsControl items_owner = ItemsControl.GetItemsOwner(this);
            int count = items_owner == null ? 0 : items_owner.Items.Count;
            double width = double.IsInfinity(availableSize.Width) ? ItemWidth * 4 : availableSize.Width;
            double height = double.IsInfinity(availableSize.Height) ? ItemHeight * 4 : availableSize.Height;
            per_line = Math.Max(1, IsHorizontal ? (int)(width / ItemWidth) : (int)(height / ItemHeight));
            int lines = (count + per_line - 1) / per_line;
            Size new_extent = IsHorizontal ? new Size(per_line * ItemWidth, lines * ItemHeight) : new Size(lines * ItemWidth, per_line * ItemHeight);
            Size new_viewport = new Size(width, height);
            if (new_extent != extent || new_viewport != viewport)
            {
                extent = new_extent;
                viewport = new_viewport;
                offset.X = Math.Max(0, Math.Min(offset.X, extent.Width - viewport.Width));
                offset.Y = Math.Max(0, Math.Min(offset.Y, extent.Height - viewport.Height));
                if (owner != null)
                    owner.InvalidateScrollInfo();
            }
            int first = 0, last = -1;
            if (count > 0)
            {
                double start = IsHorizontal ? offset.Y : offset.X, length = IsHorizontal ? height : width, line_size = IsHorizontal ? ItemHeight : ItemWidth;
                int first_line = (int)Math.Floor(start / line_size), last_line = (int)Math.Ceiling((start + length) / line_size);
                first = Math.Max(0, Math.Min(count - 1, first_line * per_line));
                last = Math.Max(first, Math.Min(count - 1, (last_line + 1) * per_line - 1));
                System.Windows.Controls.Primitives.GeneratorPosition start_position = generator.GeneratorPositionFromIndex(first);
                int child_index = start_position.Offset == 0 ? start_position.Index : start_position.Index + 1;
                using (generator.StartAt(start_position, System.Windows.Controls.Primitives.GeneratorDirection.Forward, true))
                {
                    for (int i = first; i <= last; i++, child_index++)
                    {
                        bool newly_realized;
                        UIElement child = generator.GenerateNext(out newly_realized) as UIElement;
                        if (child == null)
                            break;
                        if (newly_realized)
                        {
                            if (child_index >= children.Count)
                                AddInternalChild(child);
                            else
                                InsertInternalChild(child_index, child);
                            generator.PrepareItemContainer(child);
                        }
                        child.Measure(new Size(ItemWidth, ItemHeight));
                    }
                }
            }
            for (int i = children.Count - 1; i >= 0; i--)
            {
                int index = generator.IndexFromGeneratorPosition(new System.Windows.Controls.Primitives.GeneratorPosition(i, 0));
                if ((index < first || index > last) && !children[i].IsKeyboardFocusWithin)
                {
                    generator.Remove(new System.Windows.Controls.Primitives.GeneratorPosition(i, 0), 1);
                    RemoveInternalChildRange(i, 1);
                }
            }
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElementCollection children = InternalChildren;
            System.Windows.Controls.Primitives.IItemContainerGenerator generator = ItemContainerGenerator;
            for (int i = 0; i < children.Count; i++)
            {
                int index = generator.IndexFromGeneratorPosition(new System.Windows.Controls.Primitives.GeneratorPosition(i, 0));
                if (index < 0)
                    continue;
                int line = index / per_line, position = index % per_line;
                if (IsHorizontal)
                    children[i].Arrange(new Rect(position * ItemWidth - offset.X, line * ItemHeight - offset.Y, ItemWidth, ItemHeight));
                else
                    children[i].Arrange(new Rect(line * ItemWidth - offset.X, position * ItemHeight - offset.Y, ItemWidth, ItemHeight));
            }
            return finalSize;
        }

        protected override void OnItemsChanged(object sender, System.Windows.Controls.Primitives.ItemsChangedEventArgs args)
        {
            switch (args.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    if (args.ItemUICount > 0)
                        RemoveInternalChildRange(args.Position.Index, args.ItemUICount);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    if (InternalChildren.Count > 0)
                        RemoveInternalChildRange(0, InternalChildren.Count);
                    break;
            }
            base.OnItemsChanged(sender, args);
        }

        protected override void BringIndexIntoView(int index)
        {
            ScrollToIndex(index);
        }

        void ScrollToIndex(int index)
        {
            if (index < 0 || per_line < 1)
                return;
            int line = index / per_line;
            if (IsHorizontal)
            {
                double top = line * ItemHeight;
                if (top < offset.Y)
                    SetVerticalOffset(top);
                else if (top + ItemHeight > offset.Y + viewport.Height)
                    SetVerticalOffset(top + ItemHeight - viewport.Height);
            }
            else
            {
                double left = line * ItemWidth;
                if (left < offset.X)
                    SetHorizontalOffset(left);
                else if (left + ItemWidth > offset.X + viewport.Width)
                    SetHorizontalOffset(left + ItemWidth - viewport.Width);
            }
        }

        public ScrollViewer ScrollOwner
        {
            get { return owner; }
            set { owner = value; }
        }

        public bool CanHorizontallyScroll
        {
            get { return can_horizontally_scroll; }
            set { can_horizontally_scroll = value; }
        }

        public bool CanVerticallyScroll
        {
            get { return can_vertically_scroll; }
            set { can_vertically_scroll = value; }
        }

        public double ExtentWidth
        {
            get { return extent.Width; }
        }

        public double ExtentHeight
        {
            get { return extent.Height; }
        }

        public double ViewportWidth
        {
            get { return viewport.Width; }
        }

        public double ViewportHeight
        {
            get { return viewport.Height; }
        }

        public double HorizontalOffset
        {
            get { return offset.X; }
        }

        public double VerticalOffset
        {
            get { return offset.Y; }
        }

        public void LineUp()
        {
            SetVerticalOffset(offset.Y - (IsHorizontal ? ItemHeight : 16));
        }

        public void LineDown()
        {
            SetVerticalOffset(offset.Y + (IsHorizontal ? ItemHeight : 16));
        }

        public void LineLeft()
        {
            SetHorizontalOffset(offset.X - (IsHorizontal ? 16 : ItemWidth));
        }

        public void LineRight()
        {
            SetHorizontalOffset(offset.X + (IsHorizontal ? 16 : ItemWidth));
        }

        public void PageUp()
        {
            SetVerticalOffset(offset.Y - viewport.Height);
        }

        public void PageDown()
        {
            SetVerticalOffset(offset.Y + viewport.Height);
        }

        public void PageLeft()
        {
            SetHorizontalOffset(offset.X - viewport.Width);
        }

        public void PageRight()
        {
            SetHorizontalOffset(offset.X + viewport.Width);
        }

        public void MouseWheelUp()
        {
            if (IsHorizontal)
                SetVerticalOffset(offset.Y - ItemHeight);
            else
                SetHorizontalOffset(offset.X - ItemWidth);
        }

        public void MouseWheelDown()
        {
            if (IsHorizontal)
                SetVerticalOffset(offset.Y + ItemHeight);
            else
                SetHorizontalOffset(offset.X + ItemWidth);
        }

        public void MouseWheelLeft()
        {
            SetHorizontalOffset(offset.X - 16);
        }

        public void MouseWheelRight()
        {
            SetHorizontalOffset(offset.X + 16);
        }

        public void SetHorizontalOffset(double value)
        {
            value = Math.Max(0, Math.Min(value, extent.Width - viewport.Width));
            if (value != offset.X)
            {
                offset.X = value;
                if (owner != null)
                    owner.InvalidateScrollInfo();
                InvalidateMeasure();
            }
        }

        public void SetVerticalOffset(double value)
        {
            value = Math.Max(0, Math.Min(value, extent.Height - viewport.Height));
            if (value != offset.Y)
            {
                offset.Y = value;
                if (owner != null)
                    owner.InvalidateScrollInfo();
                InvalidateMeasure();
            }
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            DependencyObject current = visual;
            while (current != null && VisualTreeHelper.GetParent(current) != this)
                current = VisualTreeHelper.GetParent(current);
            UIElement child = current as UIElement;
            if (child != null)
            {
                int position = InternalChildren.IndexOf(child);
                if (position >= 0)
                    ScrollToIndex(ItemContainerGenerator.IndexFromGeneratorPosition(new System.Windows.Controls.Primitives.GeneratorPosition(position, 0)));
            }
            return rectangle;
        }
    }

    enum FileDate
    {
        CreationDate = 1,
        ModifiedDate = 2,
        LastAccessDate = 3
    }
}
