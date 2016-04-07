using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO.Packaging;
using System.Threading;
using System.Windows.Media.Animation;
using Shell32;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Desktop_Environment.xaml
    /// </summary>
    public partial class Desktop_Environment : Window
    {
        bool is_opend = true;
        PagePanal viewed_page;
        PagePanal ViewedPage
        {
            get { return viewed_page; }
            set
            {
                switch(value)
                {
                    case PagePanal.Home:
                        GetHomePageElements();
                        break;
                    case PagePanal.Apps:
                        RepairAppsPageElements();
                        break;
                    case PagePanal.Games:
                        RepairGamesPageElements();
                        break;
                    case PagePanal.Documents:
                        RepairDocumentPageElements();
                        break;
                }

                viewed_page = value;
            }
        }

        ObservableCollection<HomeItem> home_list_data = new ObservableCollection<HomeItem>();
        ObservableCollection<AppItem> app_list_data = new ObservableCollection<AppItem>();
        ObservableCollection<GameItem> game_list_data = new ObservableCollection<GameItem>();
        ObservableCollection<DocumentItem> document_list_data = new ObservableCollection<DocumentItem>();


        public Desktop_Environment()
        {
            InitializeComponent();
            SafetyTools.SetWindowIcon(this, "DesktopEnvironment");
        }

        bool size_worker_started = false;
        static string[] document_extensions = { ".pdf", ".txt", ".doc", ".docx", ".odt", ".ppt", ".pptx", ".odp", ".xls", ".xlsx", ".ods", ".xps", ".oxps", ".htm", ".html", ".mht", ".rtf" };

        private void Launcher_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            string action = ((Grid)sender).Tag as string;
            switch (action)
            {
                case "pictures":
                    OpenSpecialFolder(Environment.SpecialFolder.MyPictures);
                    return;
                case "music":
                    OpenSpecialFolder(Environment.SpecialFolder.MyMusic);
                    return;
                case "video":
                    OpenSpecialFolder(Environment.SpecialFolder.MyVideos);
                    return;
                case "control":
                    try { Process.Start(new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "control.exe"))); }
                    catch (Exception ex)
                    { Messages.ShowMessage("We can not open the Control Panel.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                    return;
            }
            if (launcher_panal.Children.IndexOf((Grid)sender) > 3)
                return;
            ThicknessAnimation ta = new ThicknessAnimation(new Thickness(0, -(ActualHeight * launcher_panal.Children.IndexOf((Grid)sender)), 0, 0), new Duration(new TimeSpan(2000000)));
            ThicknessAnimation ta2 = new ThicknessAnimation(new Thickness(52, (54 * launcher_panal.Children.IndexOf((Grid)sender)), 0, 0), new Duration(new TimeSpan(2000000)));
            rec.BeginAnimation(MarginProperty, ta2);
            mother.BeginAnimation(MarginProperty, ta);
            foreach (Grid i in launcher_panal.Children)
                i.Background = Brushes.Black;
            ((Grid)sender).Background = Brushes.White;
            ViewedPage = (PagePanal)launcher_panal.Children.IndexOf((Grid)sender) + 1;
            //mother.Margin = new Thickness(0, -(ActualHeight * launcher_panal.Children.IndexOf((Grid)sender)), 0, 0);
        }

        void OpenSpecialFolder(Environment.SpecialFolder folder)
        {
            string path = Environment.GetFolderPath(folder);
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                Directory_Explorer.ExplorePath(path);
            else
                Messages.ShowMessage("We can not find this folder.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, folder.ToString());
        }

        static List<string> ReadFolders(string key)
        {
            string value = Functions.ReadSetting(key);
            return value == null ? new List<string>() : value.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        static bool AddFolder(string key, string title)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.DialogTitle = title;
            if (sfd.ShowWindow() != MyMessageBoxResult.OK || string.IsNullOrEmpty(sfd.SelectedItem))
                return false;
            List<string> folders = ReadFolders(key);
            if (!folders.Any(x => x.Equals(sfd.SelectedItem, StringComparison.OrdinalIgnoreCase)))
                folders.Add(sfd.SelectedItem);
            Functions.SaveSetting(key, string.Join("|", folders.ToArray()));
            return true;
        }

        static bool AddStartMenuShortcut(string folder, string title)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.DialogTitle = title;
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Programs and shortcuts (*.exe, *.lnk, *.bat, *.cmd)", extentions = SelectFileFilter.SetExtentions(".exe|.lnk|.bat|.cmd") });
            if (sfd.ShowWindow() != MyMessageBoxResult.OK || string.IsNullOrEmpty(sfd.SelectedItem))
                return false;
            string target = sfd.SelectedItem;
            try
            {
                Directory.CreateDirectory(folder);
                string name = System.IO.Path.GetFileNameWithoutExtension(target), path = System.IO.Path.Combine(folder, name + ".lnk");
                for (int i = 2; File.Exists(path); i++)
                    path = System.IO.Path.Combine(folder, name + " (" + i + ").lnk");
                if (System.IO.Path.GetExtension(target).ToLower() == ".lnk")
                    File.Copy(target, path);
                else
                {
                    IWshRuntimeLibrary.IWshShortcut shortcut = new IWshRuntimeLibrary.IWshShell_Class().CreateShortcut(path);
                    shortcut.TargetPath = target;
                    shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(target);
                    shortcut.Save();
                }
                return true;
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not add it.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                return false;
            }
        }

        private void AddApp_Click(object sender, RoutedEventArgs e)
        {
            if (AddStartMenuShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Select the program to add"))
                RepairAppsPageElements();
        }

        private void AddAppsFolder_Click(object sender, RoutedEventArgs e)
        {
            if (AddFolder("desktop.app_folders", "Add all programs in this folder"))
                RepairAppsPageElements();
        }

        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            if (AddStartMenuShortcut(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Games"), "Select the game to add"))
                RepairGamesPageElements();
        }

        private void AddDocumentLocation_Click(object sender, RoutedEventArgs e)
        {
            if (AddFolder("desktop.doc_folders", "Add the documents in this folder"))
                RepairDocumentPageElements();
        }

        private void GetFolderApps(List<FileInfo> file_list, DirectoryInfo current, int depth)
        {
            FileInfo[] files = new FileInfo[0];
            DirectoryInfo[] sub_directory = new DirectoryInfo[0];
            try
            {
                files = current.GetFiles("*.exe");
                sub_directory = current.GetDirectories();
            }
            catch (Exception) { }
            foreach (var i in files)
                if (!i.Name.ToLower().StartsWith("unins") && !file_list.Any(x => x.FullName.Equals(i.FullName, StringComparison.OrdinalIgnoreCase)))
                    file_list.Add(i);
            if (depth < 2)
                foreach (var i in sub_directory)
                    if (!Functions.IsLink(i))
                        GetFolderApps(file_list, i, depth + 1);
        }

        #region Set Window as Backgrount Window

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOACTIVATE = 0x0010;

        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        public static void SetBottom(Window window)
        {
            IntPtr hWnd = new WindowInteropHelper(window).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            SetBottom(this);
            if (ActualHeight < 600 || ActualWidth < 800)
                Close();
            if (size_worker_started)
                return;
            size_worker_started = true;
            BackgroundWorker element_size_sitter = new BackgroundWorker();
            element_size_sitter.WorkerReportsProgress = element_size_sitter.WorkerSupportsCancellation = true;
            element_size_sitter.DoWork += (object sender2, DoWorkEventArgs e2) =>
            {
                while (is_opend)
                {
                    Thread.Sleep(1000);
                    element_size_sitter.ReportProgress(0);
                }
            };

            element_size_sitter.ProgressChanged += (object sender2, ProgressChangedEventArgs e2) =>
            {
                for (int i = 0; i < mother.Children.Count; i++)
                {
                    ((Grid)mother.Children[i]).Height = ActualHeight - 5;
                    ((Grid)mother.Children[i]).Margin = new Thickness(0, i * ActualHeight, 0, 0);
                }
            };
            element_size_sitter.RunWorkerAsync();

            //first_tap_header.Margin = new Thickness(0, (ActualHeight - 38 - 540) / 2, 0, 0);
            //if (Process.GetProcessesByName("explorer").Length > 0)
            //    Close();
        }

        #endregion Set Window as Backgrount Window

        #region HomeContent

        private BitmapSource GetOfficeThumbnails(FileInfo fileInfo, string image_path, string image_extention)
        {
            try
            {
                MemoryStream image_stream = null;
                if (image_extention != ".wmf")
                {
                    if (Functions.FindSevenZipLibrary() != null && Functions.PrepareSevenZip())
                        using (SevenZip.SevenZipExtractor sze = new SevenZip.SevenZipExtractor(fileInfo.FullName))
                        {
                            int index = sze.ArchiveFileNames.IndexOf(image_path);
                            if (index > -1)
                            {
                                image_stream = new MemoryStream();
                                sze.ExtractFile(index, image_stream);
                            }
                        }
                }
                else
                    using (Package package = Package.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        foreach (PackagePart i in package.GetParts())
                        {
                            string part_name = i.Uri.OriginalString.ToLower();
                            if (part_name.StartsWith("/docprops/thumbnail.") && !part_name.EndsWith(".wmf") && !part_name.EndsWith(".emf"))
                            {
                                image_stream = new MemoryStream();
                                using (Stream part_stream = i.GetStream(FileMode.Open, FileAccess.Read))
                                    part_stream.CopyTo(image_stream);
                                break;
                            }
                        }
                if (image_stream != null && image_stream.Length > 0)
                {
                    image_stream.Position = 0;
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.DecodePixelWidth = 210;
                    image.StreamSource = image_stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch (Exception) { }

            //Package hh = ZipPackage.Open(@"C:\Users\pal\Desktop\المصنف1.xlsx");
            //var jjj = hh.GetParts();
            //foreach (var i in jjj)
            //    if (i.Uri.AbsolutePath.ToLower() == "/docProps/thumbnail.wmf")
            //    {

            //        break;
            //    }
            //var b = jjj.ToList<PackagePart>();
            //Uri hd = b[2].Uri;
            //var gh = new UriBuilder(hd.OriginalString);

            //if (hh.PartExists(gh.Uri))
            //    return null;
            return null;
        }

        private void GetContent(string path, HomeItem item)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                DirectoryInfo[] directories = di.GetDirectories();
                FileInfo[] files = di.GetFiles();
                byte fil = 0;

                if (directories.Length > 0)
                    item.SmallImage0 = new BitmapImage(new Uri(Functions.StandardImageSource + "48/home-directory.png"));
                else if (files.Length > fil)
                {
                    item.SmallImage0 = Functions.GetFileImage48(files[fil].Extension);
                    fil++;
                }

                if (directories.Length > 1)
                    item.SmallImage1 = new BitmapImage(new Uri(Functions.StandardImageSource + "48/home-directory.png"));
                else if (files.Length > fil)
                {
                    item.SmallImage1 = Functions.GetFileImage48(files[fil].Extension);
                    fil++;
                }

                if (directories.Length > 2)
                    item.SmallImage2 = new BitmapImage(new Uri(Functions.StandardImageSource + "48/home-directory.png"));
                else if (files.Length > fil)
                {
                    item.SmallImage2 = Functions.GetFileImage48(files[fil].Extension);
                    fil++;
                }

                if (files.Length > fil)
                {
                    item.SmallImage3 = Functions.GetFileImage48(files[fil].Extension);
                    fil++;
                }
                else if (directories.Length > 3)
                    item.SmallImage3 = new BitmapImage(new Uri(Functions.StandardImageSource + "48/home-directory.png"));

                if (files.Length > fil)
                {
                    item.SmallImage4 = Functions.GetFileImage48(files[fil].Extension);
                    fil++;
                }
                else if (directories.Length > 4)
                    item.SmallImage4 = new BitmapImage(new Uri(Functions.StandardImageSource + "48/home-directory.png"));
            }
            catch (Exception) { }
        }

        private void GetHomePageElements()
        {
            home_list_data.Clear();
            bool b1 = false, b2 = false;
            BackgroundWorker get_drivers = new BackgroundWorker();
            get_drivers.WorkerSupportsCancellation = true;
            DriveInfo[] drivers = new DriveInfo[0];
            get_drivers.DoWork += (object senderw, DoWorkEventArgs ew) => { drivers = DriveInfo.GetDrives(); };
            get_drivers.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
            {
                for (int i = drivers.Length - 1; i > -1; i--)
                    home_list_data.Insert(0, new HomeItem(drivers[i]));
                string tmp_path;
                if (Directory.Exists(tmp_path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
                    home_list_data.Insert(0, new HomeItem(new DirectoryInfo(tmp_path)));
                home_list_data.Insert(0, new HomeItem());
                b1 = true;
            };
            get_drivers.RunWorkerAsync();

            BackgroundWorker get_desktop_content = new BackgroundWorker();
            get_desktop_content.WorkerSupportsCancellation = true;
            DirectoryInfo[] directories = new DirectoryInfo[0];
            FileInfo[] files = new FileInfo[0];
            get_desktop_content.DoWork += (object senderw, DoWorkEventArgs ew) =>
            {
                string desktop_path;
                if (Directory.Exists(desktop_path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)))
                {
                    try
                    {
                        DirectoryInfo desktop = new DirectoryInfo(desktop_path);
                        directories = desktop.GetDirectories();
                        files = desktop.GetFiles();
                    }
                    catch (Exception) { }
                }
            };
            get_desktop_content.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
            {
                foreach (var i in directories)
                    home_list_data.Add(new HomeItem(i));
                foreach (var i in files)
                    home_list_data.Add(new HomeItem(i));
                b2 = true;
            };
            get_desktop_content.RunWorkerAsync();


            BackgroundWorker get_contents = new BackgroundWorker();
            get_contents.WorkerSupportsCancellation = get_contents.WorkerReportsProgress = true;
            get_contents.DoWork += (object senderw, DoWorkEventArgs ew) =>
            {
                while (!b1 || !b2)
                    Thread.Sleep(200);
                for (int i = 1; i < home_list_data.Count; i++)
                {
                    get_contents.ReportProgress(i);
                    Thread.Sleep(100);
                }
            };
            get_contents.ProgressChanged += (object sender2, ProgressChangedEventArgs e) =>
            {
                if (home_list_data[e.ProgressPercentage].is_drive)
                    GetContent(home_list_data[e.ProgressPercentage].drive.Name, home_list_data[e.ProgressPercentage]);
                else if (home_list_data[e.ProgressPercentage].is_directory)
                    GetContent(home_list_data[e.ProgressPercentage].directory.FullName, home_list_data[e.ProgressPercentage]);
                else
                    GetContent(home_list_data[e.ProgressPercentage]);
                home_list_view.Items.Refresh();
            };
            get_contents.RunWorkerAsync();
            //FileSystemWatcher watcher = new FileSystemWatcher();

        }

        private void GetContent(HomeItem item)
        {
            switch (Functions.GetFileCategory(item.file.Extension))
            {
                case FileCategory.Application:
                    //if (item.file.Extension.ToLower().Equals(".exe"))
                    //{
                    try
                    { item.BigImage = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.Icon.ExtractAssociatedIcon(item.file.FullName).Handle, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()); }
                    catch (Exception)
                    { item.BigImage = Functions.GetFileImage256(item.file.Extension); }
                    //}
                    //else
                    //;
                    break;
                case FileCategory.Html:
                case FileCategory.TextCanRunAsApplication:
                case FileCategory.Text:
                    try
                    {
                        using (StreamReader reader = new StreamReader(new FileStream(item.file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)))
                        if (reader.BaseStream.Length > 100)
                        {
                            char[] buffer = new char[100];
                            int read = reader.Read(buffer, 0, 100);
                            item.TextContent = new string(buffer, 0, read).Replace("\n", "").Replace("\r", "");
                        }
                        else item.TextContent = reader.ReadToEnd().Replace("\n", "").Replace("\r", "");
                    }
                    catch (Exception) { }
                    break;
                case FileCategory.Image:
                    try
                    {
                        BitmapImage myBitmapImage = new BitmapImage();
                        myBitmapImage.BeginInit();
                        myBitmapImage.UriSource = new Uri(item.file.FullName);
                        myBitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        myBitmapImage.DecodePixelWidth = 210;
                        myBitmapImage.EndInit();
                        myBitmapImage.Freeze();
                        item.BigImage = myBitmapImage;
                    }
                    catch (Exception) { }
                    break;
                case FileCategory.Vedio:
                    //Get vedio thumbnail
                    break;
                case FileCategory.Audio:
                    item.BigImage = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/go-music.png"));
                    break;
                case FileCategory.Document:
                    if (item.file.Extension.ToLower().Equals(".docx") ||item.file.Extension.ToLower().Equals(".pptx") || item.file.Extension.ToLower().Equals(".xlsx"))
                        item.BigImage = GetOfficeThumbnails(item.file, @"docProps\thumbnail.wmf", ".wmf");
                    else if (item.file.Extension.ToLower().Equals(".odt") || item.file.Extension.ToLower().Equals(".odp") || item.file.Extension.ToLower().Equals(".ods"))
                        item.BigImage = GetOfficeThumbnails(item.file, @"Thumbnails\thumbnail.png", ".png");
                    break;
                case FileCategory.Font:
                    item.TextContent = "ا ب ج\nA b c";
                    item.TextFontSize = 48;
                    break;
                default:
                    try
                    {
                        using (BinaryReader reader = new BinaryReader(new FileStream(item.file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)))
                        {
                        byte[] buffer;
                        if (reader.BaseStream.Length > 100)
                            buffer = reader.ReadBytes(100);
                        else
                            buffer = reader.ReadBytes((int)reader.BaseStream.Length);
                        item.TextContent = BitConverter.ToString(buffer).Replace("-", " ");
                        //for (int i = 0; i < buffer.Length; i++)
                        //    item.TextContent += ConvToHXD(buffer[i]);
                        }
                    }
                    catch (Exception) { }
                    break;
            }
        }

        private void HomeItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (home_list_view.SelectedIndex > -1)
                {
                    if (home_list_data[home_list_view.SelectedIndex].is_current_programs)
                        Functions.ShowTool(new Directory_Explorer());
                    else if (home_list_data[home_list_view.SelectedIndex].is_directory)
                        Directory_Explorer.ExplorePath(home_list_data[home_list_view.SelectedIndex].directory.FullName);
                    else if (home_list_data[home_list_view.SelectedIndex].is_drive)
                    {
                        if (home_list_data[home_list_view.SelectedIndex].drive.IsReady)
                            Directory_Explorer.ExplorePath(home_list_data[home_list_view.SelectedIndex].drive.Name);
                        else
                            Messages.ShowMessage("Drive is not ready", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    }
                    else
                        try
                        { Process.Start(new ProcessStartInfo(home_list_data[home_list_view.SelectedIndex].file.FullName)); }
                        catch (Exception ex)
                        { Messages.ShowMessage("Error while opening the file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
            }
        }

        #endregion HomeContent

        #region App Page Content

        ObservableCollection<AppItem> app_list_copy = new ObservableCollection<AppItem>();

        private void RepairAppsPageElements()
        {
            app_list_data.Clear();
            app_list_copy.Clear();
            string program_directory = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string all_program_directory = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            List<string> app_folders = ReadFolders("desktop.app_folders");
            List<FileInfo> file_list = new List<FileInfo>();
            BackgroundWorker get_apps = new BackgroundWorker();
            get_apps.WorkerSupportsCancellation = true;
            get_apps.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                try
                {
                    DirectoryInfo prog = new DirectoryInfo(program_directory);
                    DirectoryInfo all_prog = new DirectoryInfo(all_program_directory);
                    GetAllApps(file_list,prog);
                    GetAllApps(file_list, all_prog);
                    foreach (var i in app_folders)
                        if (Directory.Exists(i))
                            GetFolderApps(file_list, new DirectoryInfo(i), 0);
                    file_list.Sort(FileSorter);
                }
                catch (Exception ex) { string g = ex.Message; }
            };

            get_apps.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                //bool getting_icon = false;
                foreach (var i in file_list)
                {
                    string orginal_path = GetShortcutTargetFile(i.FullName);
                    string extention=System.IO.Path.GetExtension(orginal_path).ToLower();
                    if (extention.Equals(".exe") || extention.Equals(".com") || extention.Equals(".bat") || extention.Equals(".cmd") || extention.Equals(".lnk") || extention.Equals(string.Empty))
                    {
                        AppItem item = new AppItem(i) { orginal_path = orginal_path };
                        app_list_data.Add(item);
                        app_list_copy.Add(item);
                    }
                }
                BackgroundWorker get_icons = new BackgroundWorker();
                get_icons.WorkerSupportsCancellation = get_icons.WorkerReportsProgress = true;
                get_icons.DoWork += (object sender2, DoWorkEventArgs e2) =>
                {
                    for (int i = 0; i < app_list_data.Count; i++)
                    {
                        get_icons.ReportProgress(i);
                        //getting_icon = true;
                        //while (getting_icon)
                            Thread.Sleep(50);
                    }
                };
                get_icons.ProgressChanged += (object sender2, ProgressChangedEventArgs e2) =>
                {
                    BitmapSource image = null;
                    try
                    {
                        //description
                        if (File.Exists(app_list_data[e2.ProgressPercentage].orginal_path) || Directory.Exists(app_list_data[e2.ProgressPercentage].orginal_path))
                            //image = GetIcon(app_list_data[e2.ProgressPercentage].orginal_path, false, false);
                            image = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.Icon.ExtractAssociatedIcon(app_list_data[e2.ProgressPercentage].orginal_path).Handle, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    }
                    catch (Exception ex) { string gf = ex.Message; }
                    if (image != null)
                    {
                        app_list_data[e2.ProgressPercentage].SmallImage = image;
                        app_list_view.Items.Refresh();
                    }
                    //getting_icon = false;
                };
                get_icons.RunWorkerAsync();
            };

            get_apps.RunWorkerAsync();
        }

        private void GetAllApps(List<FileInfo> file_list, DirectoryInfo current)
        {
            FileInfo[] files = new FileInfo[0];
            DirectoryInfo[] sub_directory = new DirectoryInfo[0];
            try
            {
                files = current.GetFiles();
                sub_directory = current.GetDirectories();
            }
            catch (Exception) { }
            foreach (var i in sub_directory)
                if (!i.Name.ToLower().Equals("games"))
                    GetAllApps(file_list, i);
            foreach (var i in files)
            {
                if (!(i.Name.ToLower().Contains("uninstall ") || i.Name.ToLower().Equals("uninstall") || i.Name.ToLower().Equals("help")||i.Name.ToLower().Contains(" help")))
                    if (i.Extension.ToLower() == ".exe" || i.Extension.ToLower() == ".lnk" || i.Extension == String.Empty)
                        if (!Contains(file_list, i.Name.ToLower()))
                            file_list.Add(i);
            }
        }

        int FileSorter(FileInfo x, FileInfo y)
        {
            return x.FullName.CompareTo(y.FullName);
        }

        private bool Contains(List<FileInfo> file_list, string p)
        {
            foreach (var i in file_list)
                if (i.Name.ToLower().Equals(p))
                    return true;
            return false;
        }

        public string GetShortcutTargetFile(string shortcutFilename)
        {
            try
            {
                string pathOnly = System.IO.Path.GetDirectoryName(shortcutFilename);
                string filenameOnly = System.IO.Path.GetFileName(shortcutFilename);
                Shell shell = new Shell();
                Folder folder = shell.NameSpace(pathOnly);
                FolderItem folderItem = folder.ParseName(filenameOnly);
                if (folderItem != null)
                {
                    Shell32.ShellLinkObject link = (Shell32.ShellLinkObject)folderItem.GetLink;
                    return link.Path;
                }
            }
            catch (Exception) { }
            return shortcutFilename;
        }

        private void AppsFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            app_list_data.Clear();
            foreach (var i in app_list_copy)
                if (i.Name.ToLower().Contains(app_filter.Text.ToLower()))
                    app_list_data.Add(i);
        }

        private void AppItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (app_list_view.SelectedIndex > -1)
                {
                    if (app_list_data[app_list_view.SelectedIndex].file_info.Exists)
                        try
                        { Process.Start(new ProcessStartInfo(app_list_data[app_list_view.SelectedIndex].file_info.FullName)); }
                        catch (Exception ex)
                        { Messages.ShowMessage("Error while opening the application.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
            }
        }

        public static BitmapSource GetIcon(string path, bool smallIcon, bool isDirectory)
        {
            // SHGFI_USEFILEATTRIBUTES takes the file name and attributes into account if it doesn't exist
            uint flags = SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES;
            //if (smallIcon)
            //    flags |= SHGFI_SMALLICON;

            uint attributes = FILE_ATTRIBUTE_NORMAL;
            //if (isDirectory)
            //    attributes |= FILE_ATTRIBUTE_DIRECTORY;

            SHFILEINFO shfi;
            if (SHGetFileInfo(path, attributes, out shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags) != 0)
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(shfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            
            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32")]
        private static extern int SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint flags);

        private const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
        private const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
        private const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_ARCHIVE = 0x00000020;
        private const uint FILE_ATTRIBUTE_DEVICE = 0x00000040;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_TEMPORARY = 0x00000100;
        private const uint FILE_ATTRIBUTE_SPARSE_FILE = 0x00000200;
        private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        private const uint FILE_ATTRIBUTE_COMPRESSED = 0x00000800;
        private const uint FILE_ATTRIBUTE_OFFLINE = 0x00001000;
        private const uint FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000;
        private const uint FILE_ATTRIBUTE_ENCRYPTED = 0x00004000;
        private const uint FILE_ATTRIBUTE_VIRTUAL = 0x00010000;

        private const uint SHGFI_ICON = 0x000000100;     // get icon
        private const uint SHGFI_DISPLAYNAME = 0x000000200;     // get display name
        private const uint SHGFI_TYPENAME = 0x000000400;     // get type name
        private const uint SHGFI_ATTRIBUTES = 0x000000800;     // get attributes
        private const uint SHGFI_ICONLOCATION = 0x000001000;     // get icon location
        private const uint SHGFI_EXETYPE = 0x000002000;     // return exe type
        private const uint SHGFI_SYSICONINDEX = 0x000004000;     // get system icon index
        private const uint SHGFI_LINKOVERLAY = 0x000008000;     // put a link overlay on icon
        private const uint SHGFI_SELECTED = 0x000010000;     // show icon in selected state
        private const uint SHGFI_ATTR_SPECIFIED = 0x000020000;     // get only specified attributes
        private const uint SHGFI_LARGEICON = 0x000000000;     // get large icon
        private const uint SHGFI_SMALLICON = 0x000000001;     // get small icon
        private const uint SHGFI_OPENICON = 0x000000002;     // get open icon
        private const uint SHGFI_SHELLICONSIZE = 0x000000004;     // get shell size icon
        private const uint SHGFI_PIDL = 0x000000008;     // pszPath is a pidl
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;     // use passed dwFileAttribute

        #endregion App Page Content

        #region Game Page Content

        ObservableCollection<GameItem> game_list_copy = new ObservableCollection<GameItem>();

        private void RepairGamesPageElements()
        {
            game_list_data.Clear();
            game_list_copy.Clear();
            string program_directory = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            string all_program_directory = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
            List<FileInfo> file_list = new List<FileInfo>();
            BackgroundWorker get_games = new BackgroundWorker();
            get_games.WorkerSupportsCancellation = true;
            get_games.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                try
                {
                    DirectoryInfo prog = new DirectoryInfo(program_directory);
                    DirectoryInfo all_prog = new DirectoryInfo(all_program_directory);
                    GetAllGames(file_list, prog);
                    GetAllGames(file_list, all_prog);
                    file_list.Sort(FileSorter);
                }
                catch (Exception ex) { string g = ex.Message; }
            };

            get_games.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                foreach (var i in file_list)
                {
                    string orginal_path = GetShortcutTargetFile(i.FullName);
                    string extention = System.IO.Path.GetExtension(orginal_path).ToLower();
                    if (extention.Equals(".exe") || extention.Equals(".com") || extention.Equals(".bat") || extention.Equals(".cmd") || extention.Equals(".lnk") || extention.Equals(string.Empty))
                    {
                        GameItem item = new GameItem(i) { orginal_path = orginal_path };
                        game_list_data.Add(item);
                        game_list_copy.Add(item);
                    }
                }
                BackgroundWorker get_icons = new BackgroundWorker();
                get_icons.WorkerSupportsCancellation = get_icons.WorkerReportsProgress = true;
                bool getting_icon = false;
                get_icons.DoWork += (object sender2, DoWorkEventArgs e2) =>
                {
                    for (int i = 0; i < game_list_data.Count; i++)
                    {
                        get_icons.ReportProgress(i);
                        getting_icon = true;
                        while (getting_icon)
                            Thread.Sleep(50);
                    }
                };
                get_icons.ProgressChanged += (object sender2, ProgressChangedEventArgs e2) =>
                {
                    BitmapSource image = null;
                    try
                    {
                        //description
                        if (File.Exists(game_list_data[e2.ProgressPercentage].orginal_path) || Directory.Exists(game_list_data[e2.ProgressPercentage].orginal_path))
                            image = icon_of_path_large(game_list_data[e2.ProgressPercentage].orginal_path, true, true);/*GetIcon(app_list_data[e2.ProgressPercentage].orginal_path, false, false);*/
                        //else if(game_list_data[e2.ProgressPercentage].file_info.Exists)
                        //    image = icon_of_path_large(game_list_data[e2.ProgressPercentage].file_info.FullName, true, true);
                        //image = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.Icon.ExtractAssociatedIcon(app_list_data[e2.ProgressPercentage].orginal_path).Handle, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                    }
                    catch (Exception ex) { string gf = ex.Message; }
                    if (image != null)
                    {
                        game_list_data[e2.ProgressPercentage].Image = image;
                        game_list_view.Items.Refresh();
                    }
                    getting_icon = false;
                };
                get_icons.RunWorkerAsync();
            };

            get_games.RunWorkerAsync();
        }

        private void GetAllGames(List<FileInfo> file_list, DirectoryInfo current)
        {
            FileInfo[] files = new FileInfo[0];
            DirectoryInfo[] sub_directory = new DirectoryInfo[0];
            try
            {
                files = current.GetFiles();
                sub_directory = current.GetDirectories();
            }
            catch (Exception) { }
            foreach (var i in sub_directory)
                if (i.Name.ToLower().Equals("games"))
                    GetAllApps(file_list, i);
            //search in all programs for games in list whill be ready soon
            if (current.Name.ToLower().Equals("games"))
                foreach (var i in files)
                {
                    if (!(i.Name.ToLower().Contains("uninstall ") || i.Name.ToLower().Equals("uninstall") || i.Name.ToLower().Equals("help") || i.Name.ToLower().Contains(" help")))
                        if (i.Extension.ToLower() == ".exe" || i.Extension.ToLower() == ".lnk" || i.Extension == String.Empty)
                            if (!Contains(file_list, i.Name.ToLower()))
                                file_list.Add(i);
                }
        }

        private void GameItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (game_list_view.SelectedIndex > -1)
                {
                    if (game_list_data[game_list_view.SelectedIndex].file_info.Exists)
                        try
                        { Process.Start(new ProcessStartInfo(game_list_data[game_list_view.SelectedIndex].file_info.FullName)); }
                        catch (Exception ex)
                        { Messages.ShowMessage("Error while opening the game.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
            }
        }

        private void GamesFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            game_list_data.Clear();
            foreach (var i in game_list_copy)
                if (i.Name.ToLower().Contains(game_filter.Text.ToLower()))
                    game_list_data.Add(i);
        }

        #endregion Game Page Content

        #region Documents Page Content

        ObservableCollection<DocumentItem> document_list_copy = new ObservableCollection<DocumentItem>();

        private void RepairDocumentPageElements()
        {
            document_list_data.Clear();
            document_list_copy.Clear();
            string documents_directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string all_documents_directory = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            List<string> doc_folders = ReadFolders("desktop.doc_folders");
            List<FileInfo> file_list = new List<FileInfo>();
            HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            BackgroundWorker get_docs = new BackgroundWorker();
            get_docs.WorkerSupportsCancellation = true;
            get_docs.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                try
                {
                    DirectoryInfo docs = new DirectoryInfo(documents_directory);
                    DirectoryInfo all_docs = new DirectoryInfo(all_documents_directory);
                    GetAllDocuments(file_list, docs, known);
                    GetAllDocuments(file_list, all_docs, known);
                    foreach (var i in doc_folders)
                        if (Directory.Exists(i))
                            GetAllDocuments(file_list, new DirectoryInfo(i), known);
                    file_list.Sort(FileSorter);
                }
                catch (Exception ex) { string g = ex.Message; }
            };

            get_docs.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                //bool getting_icon = false;
                foreach (var i in file_list)
                {
                    string extention = System.IO.Path.GetExtension(i.FullName).ToLower();
                    if (document_extensions.Contains(extention))
                    {
                        DocumentItem item = new DocumentItem(i);
                        document_list_copy.Add(item);
                    }
                }
                ApplyDocumentFilter();
            };
            get_docs.RunWorkerAsync();
        }

        private void GetAllDocuments(List<FileInfo> file_list, DirectoryInfo current, HashSet<string> known)
        {
            if (Functions.IsLink(current) || file_list.Count > 20000)
                return;
            FileInfo[] files = new FileInfo[0];
            DirectoryInfo[] sub_directory = new DirectoryInfo[0];
            try
            {
                files = current.GetFiles();
                sub_directory = current.GetDirectories();
            }
            catch (Exception) { }
            foreach (var i in sub_directory)
                    GetAllDocuments(file_list, i, known);
            foreach (var i in files)
            {
                string ext = i.Extension.ToLower();
                if (document_extensions.Contains(ext))
                        if (known.Add(i.FullName))
                            file_list.Add(i);
            }
        }

        private void DocumentFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyDocumentFilter();
        }

        private void DocumentType_Changed(object sender, RoutedEventArgs e)
        {
            ApplyDocumentFilter();
        }

        private void DocumentSort_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyDocumentFilter();
        }

        void ApplyDocumentFilter()
        {
            if (doc_types == null || doc_sort == null || doc_filter == null)
                return;
            HashSet<string> types = new HashSet<string>();
            foreach (var i in doc_types.Items)
            {
                CheckBox box = i as CheckBox;
                if (box != null && box.IsChecked == true)
                {
                    string type = "." + box.Content;
                    types.Add(type);
                    if (type == ".html")
                        types.Add(".htm");
                    if (type == ".xps")
                        types.Add(".oxps");
                }
            }
            string text = doc_filter.Text.ToLower();
            IEnumerable<DocumentItem> items = document_list_copy.Where(x => x.Name.ToLower().Contains(text) && (types.Count == 0 || types.Contains(x.file_info.Extension.ToLower())));
            switch (doc_sort.SelectedIndex)
            {
                case 1:
                    items = items.OrderBy(x => x.FileType).ThenBy(x => x.Name);
                    break;
                case 2:
                    items = items.OrderByDescending(x => x.Date);
                    break;
                case 3:
                    items = items.OrderByDescending(x => x.Size);
                    break;
                default:
                    items = items.OrderBy(x => x.Name);
                    break;
            }
            document_list_data.Clear();
            foreach (var i in items)
                document_list_data.Add(i);
        }

        private void DocumentItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (doc_list_view.SelectedIndex > -1)
                {
                    if (document_list_data[doc_list_view.SelectedIndex].file_info.Exists)
                        try
                        { Process.Start(new ProcessStartInfo(document_list_data[doc_list_view.SelectedIndex].file_info.FullName)); }
                        catch (Exception ex)
                        { Messages.ShowMessage("Error while opening the file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
            }
        }

        #endregion Document Page Content

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            home_list_view.DataContext = home_list_data;
            app_list_view.DataContext = app_list_data;
            game_list_view.DataContext = game_list_data;
            doc_list_view.DataContext = document_list_data;
            viewed_page = PagePanal.Home;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            is_opend = false;
        }








        // Constants that we need in the function call

        //private const int SHGFI_ICON = 0x100;

        //private const int SHGFI_SMALLICON = 0x1;

        //private const int SHGFI_LARGEICON = 0x0;

        private const int SHIL_JUMBO = 0x4;
        private const int SHIL_EXTRALARGE = 0x2;

        // This structure will contain information about the file

        //public struct SHFILEINFO
        //{
        //    public IntPtr hIcon;
        //    public int iIcon;
        //    public uint dwAttributes;
        //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        //    public string szDisplayName;
        //    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        //    public string szTypeName;
        //};

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        public static extern Boolean CloseHandle(IntPtr handle);

        private struct IMAGELISTDRAWPARAMS
        {
            public int cbSize;
            public IntPtr himl;
            public int i;
            public IntPtr hdcDst;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int xBitmap;        // x offest from the upperleft of bitmap
            public int yBitmap;        // y offset from the upperleft of bitmap
            public int rgbBk;
            public int rgbFg;
            public int fStyle;
            public int dwRop;
            public int fState;
            public int Frame;
            public int crEffect;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGEINFO
        {
            public IntPtr hbmImage;
            public IntPtr hbmMask;
            public int Unused1;
            public int Unused2;
            public RECT rcImage;
        }

        #region Private ImageList COM Interop (XP)
        [ComImportAttribute()]
        [GuidAttribute("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        //helpstring("Image List"),
        interface IImageList
        {
            [PreserveSig]
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

            [PreserveSig]
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);

            [PreserveSig]
            int SetOverlayImage(int iImage, int iOverlay);

            [PreserveSig]
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

            [PreserveSig]
            int Draw(ref IMAGELISTDRAWPARAMS pimldp);

            [PreserveSig]
            int Remove(int i);

            [PreserveSig]
            int GetIcon(int i, int flags, ref IntPtr picon);

            [PreserveSig]
            int GetImageInfo(int i, ref IMAGEINFO pImageInfo);

            [PreserveSig]
            int Copy(int iDst, IImageList punkSrc, int iSrc, int uFlags);

            [PreserveSig]
            int Merge(int i1, IImageList punk2, int i2, int dx, int dy, ref Guid riid, ref IntPtr ppv);

            [PreserveSig]
            int Clone(ref Guid riid, ref IntPtr ppv);

            [PreserveSig]
            int GetImageRect(int i, ref RECT prc);

            [PreserveSig]
            int GetIconSize(ref int cx, ref int cy);

            [PreserveSig]
            int SetIconSize(int cx, int cy);

            [PreserveSig]
            int GetImageCount(ref int pi);

            [PreserveSig]
            int SetImageCount(int uNewCount);

            [PreserveSig]
            int SetBkColor(int clrBk, ref int pclr);

            [PreserveSig]
            int GetBkColor(ref int pclr);

            [PreserveSig]
            int BeginDrag(int iTrack, int dxHotspot, int dyHotspot);

            [PreserveSig]
            int EndDrag();

            [PreserveSig]
            int DragEnter(IntPtr hwndLock, int x, int y);

            [PreserveSig]
            int DragLeave(IntPtr hwndLock);

            [PreserveSig]
            int DragMove(int x, int y);

            [PreserveSig]
            int SetDragCursorImage(ref IImageList punk, int iDrag, int dxHotspot, int dyHotspot);

            [PreserveSig]
            int DragShowNolock(int fShow);

            [PreserveSig]
            int GetDragImage(ref POINT ppt, ref POINT pptHotspot, ref Guid riid, ref IntPtr ppv);

            [PreserveSig]
            int GetItemFlags(int i, ref int dwFlags);

            [PreserveSig]
            int GetOverlayImage(int iOverlay, ref int piIndex);
        };
        #endregion

        ///
        /// SHGetImageList is not exported correctly in XP.  See KB316931
        /// http://support.microsoft.com/default.aspx?scid=kb;EN-US;Q316931
        /// Apparently (and hopefully) ordinal 727 isn't going to change.
        ///
        [DllImport("shell32.dll", EntryPoint = "#727")]
        private extern static int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

         //The signature of SHGetFileInfo (located in Shell32.dll)
        [DllImport("Shell32.dll")]
        static extern int SHGetFileInfo(string pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

        [DllImport("Shell32.dll")]
        static extern int SHGetFileInfo(IntPtr pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

        [DllImport("shell32.dll", SetLastError = true)]
        static extern int SHGetSpecialFolderLocation(IntPtr hwndOwner, Int32 nFolder, ref IntPtr ppidl);

        [DllImport("user32")]
        public static extern int DestroyIcon(IntPtr hIcon);

        public struct pair
        {
            public System.Drawing.Icon icon { get; set; }
            public IntPtr iconHandleToDestroy { set; get; }

        }

        public static int DestroyIcon2(IntPtr hIcon)
        {
            return DestroyIcon(hIcon);
        }

        private static BitmapSource bitmap_source_of_icon(System.Drawing.Icon ic)
        {
            var ic2 = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(ic.Handle, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            ic2.Freeze();
            return ((BitmapSource)ic2);
        }

        //public static BitmapSource SystemIcon(bool small, ShellLib.ShellApi.CSIDL csidl)
        //{
        //    IntPtr pidlTrash = IntPtr.Zero;
        //    int hr = SHGetSpecialFolderLocation(IntPtr.Zero, (int)csidl, ref pidlTrash);
        //    Debug.Assert(hr == 0);

        //    SHFILEINFO shinfo = new SHFILEINFO();

        //    uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        //    // Get a handle to the large icon
        //    uint flags;
        //    uint SHGFI_PIDL = 0x000000008;
        //    if (!small)
        //    {
        //        flags = SHGFI_PIDL | SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES;
        //    }
        //    else
        //    {
        //        flags = SHGFI_PIDL | SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
        //    }

        //    var res = SHGetFileInfo(pidlTrash, 0, ref shinfo, Marshal.SizeOf(shinfo), flags);
        //    Debug.Assert(res != 0);

        //    var myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);
        //    Marshal.FreeCoTaskMem(pidlTrash);
        //    var bs = bitmap_source_of_icon(myIcon);
        //    myIcon.Dispose();
        //    bs.Freeze(); // importantissimo se no fa memory leak
        //    DestroyIcon(shinfo.hIcon);
        //    CloseHandle(shinfo.hIcon);
        //    return bs;

        //}

        public static BitmapSource icon_of_path(string FileName, bool small, bool checkDisk, bool addOverlay)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
            uint SHGFI_LINKOVERLAY = 0x000008000;

            uint flags;
            if (small)
            {
                flags = SHGFI_ICON | SHGFI_SMALLICON;
            }
            else
            {
                flags = SHGFI_ICON | SHGFI_LARGEICON;
            }
            if (!checkDisk)
                flags |= SHGFI_USEFILEATTRIBUTES;
            if (addOverlay)
                flags |= SHGFI_LINKOVERLAY;

            var res = SHGetFileInfo(FileName, 0, ref shinfo, Marshal.SizeOf(shinfo), flags);
            if (res == 0)
            {
                throw (new System.IO.FileNotFoundException());
            }

            var myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon);

            var bs = bitmap_source_of_icon(myIcon);
            myIcon.Dispose();
            bs.Freeze(); // importantissimo se no fa memory leak
            DestroyIcon(shinfo.hIcon);
            CloseHandle(shinfo.hIcon);
            return bs;

        }

        public static BitmapSource icon_of_path_large(string FileName, bool jumbo, bool checkDisk)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
            uint SHGFI_SYSICONINDEX = 0x4000;

            int FILE_ATTRIBUTE_NORMAL = 0x80;

            uint flags;
            flags = SHGFI_SYSICONINDEX;

            if (!checkDisk)  // This does not seem to work. If I try it, a folder icon is always returned.
                flags |= SHGFI_USEFILEATTRIBUTES;

            var res = SHGetFileInfo(FileName, FILE_ATTRIBUTE_NORMAL, ref shinfo, Marshal.SizeOf(shinfo), flags);
            if (res == 0)
            {
                throw (new System.IO.FileNotFoundException());
            }
            var iconIndex = shinfo.iIcon;

            // Get the System IImageList object from the Shell:
            Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");

            IImageList iml;
            int size = jumbo ? SHIL_JUMBO : SHIL_EXTRALARGE;
            var hres = SHGetImageList(size, ref iidImageList, out  iml); // writes iml
            //if (hres == 0)
            //{
            //    throw (new System.Exception("Error SHGetImageList"));
            //}

            IntPtr hIcon = IntPtr.Zero;
            int ILD_TRANSPARENT = 1;
            hres = iml.GetIcon(iconIndex, ILD_TRANSPARENT, ref hIcon);
            //if (hres == 0)
            //{
            //    throw (new System.Exception("Error iml.GetIcon"));
            //}

            var myIcon = System.Drawing.Icon.FromHandle(hIcon);
            var bs = bitmap_source_of_icon(myIcon);
            myIcon.Dispose();
            bs.Freeze(); // very important to avoid memory leak
            DestroyIcon(hIcon);
            //CloseHandle(hIcon);

            return bs;

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            private int _Left;
            private int _Top;
            private int _Right;
            private int _Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }



        








    }

    public class HomeItem
    {
       public static Random rand = new Random((int)DateTime.Now.Ticks);
       static SolidColorBrush[] brushes = { new SolidColorBrush(Color.FromArgb(205, 164, 196, 0)), new SolidColorBrush(Color.FromArgb(205, 96, 169, 23)), new SolidColorBrush(Color.FromArgb(205, 0, 138, 0)) ,new SolidColorBrush(Color.FromArgb(205,135,121,78))
                                 ,new SolidColorBrush(Color.FromArgb(205,0,171,169)),new SolidColorBrush(Color.FromArgb(205,27,161,226)),new SolidColorBrush(Color.FromArgb(205,62,101,255)),new SolidColorBrush(Color.FromArgb(205,106,0,255))
                                 ,new SolidColorBrush(Color.FromArgb(205,170,0,255)),new SolidColorBrush(Color.FromArgb(205,244,114,208)),new SolidColorBrush(Color.FromArgb(205,216,0,115)),new SolidColorBrush(Color.FromArgb(205,162,0,37))
                                 ,new SolidColorBrush(Color.FromArgb(205,229,20,0)),new SolidColorBrush(Color.FromArgb(205,250,104,0)),new SolidColorBrush(Color.FromArgb(205,240,163,10)),new SolidColorBrush(Color.FromArgb(205,227,200,0))
                                 ,new SolidColorBrush(Color.FromArgb(205,130,90,44)),new SolidColorBrush(Color.FromArgb(205,109,135,100)),new SolidColorBrush(Color.FromArgb(205,100,118,135)),new SolidColorBrush(Color.FromArgb(205,108,96,138))};
        public bool is_directory;
        public bool is_drive;
        public bool is_current_programs = false;

        public FileInfo file;
        public DirectoryInfo directory;
        public DriveInfo drive;

        string header;
        public string Header
        {
            get { return header; }
            set { header = value; }
        }

        string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        string text_content;
        public string TextContent
        {
            get { return text_content; }
            set { text_content = value; }
        }

        double text_font_size;
        public double TextFontSize
        {
            get { return text_font_size; }
            set { text_font_size = value; }
        }

        FontFamily text_font_family;
        public FontFamily TextFontFamily
        {
            get { return text_font_family; }
            set { text_font_family = value; }
        }

        BitmapSource big_image;
        public BitmapSource BigImage
        {
            get { return big_image; }
            set { big_image = value; }
        }

        BitmapImage icon;
        public BitmapImage Icon
        {
            get { return icon; }
            set { icon = value; }
        }

        BitmapImage small_image0;
        public BitmapImage SmallImage0
        {
            get { return small_image0; }
            set { small_image0 = value; }
        }

        BitmapImage small_image1;
        public BitmapImage SmallImage1
        {
            get { return small_image1; }
            set { small_image1 = value; }
        }

        BitmapImage small_image2;
        public BitmapImage SmallImage2
        {
            get { return small_image2; }
            set { small_image2 = value; }
        }

        BitmapImage small_image3;
        public BitmapImage SmallImage3
        {
            get { return small_image3; }
            set { small_image3 = value; }
        }

        BitmapImage small_image4;
        public BitmapImage SmallImage4
        {
            get { return small_image4; }
            set { small_image4 = value; }
        }

        SolidColorBrush background_color;
        public SolidColorBrush BackgroundColor
        {
            get { return background_color; }
            set { background_color = value; }
        }


        public HomeItem(DirectoryInfo di)
        {
            directory = di;
            is_directory = true;
            is_drive = false;
            name = di.Name;
            Icon = new BitmapImage(new Uri(Functions.StandardImageSource + "48/home-directory.png"));
            BackgroundColor = GetColor();
        }

        public HomeItem(FileInfo fi)
        {
            file = fi;
            TextFontSize = 12;
            is_directory = false;
            is_drive = false;
            name = fi.Name;
            Icon = Functions.GetFileImage48(fi.Extension);
            BackgroundColor = GetColor();
        }

        public HomeItem(DriveInfo di)
        {
            drive = di;
            is_directory = false;
            is_drive = true;
            name = di.IsReady ? ((di.VolumeLabel.Equals(string.Empty) ? "Local Drive" : di.VolumeLabel) + " (" + di.Name + ")") : di.Name;
            Icon = new BitmapImage(new Uri(Functions.StandardImageSource + "48/home-drive.png"));
            BackgroundColor = GetColor();
        }

        public HomeItem() //Create item for current app "FILE MANAGER"
        {
            file = new FileInfo(System.Windows.Forms.Application.ExecutablePath);
            is_directory = false;
            is_drive = false;
            name = "FILE MANAGER";
            BigImage = new BitmapImage(new Uri(Functions.StandardImageSource + "home-file-manager.jpg"));
            BackgroundColor = GetColor();
        }

        private SolidColorBrush GetColor()
        {
            return brushes[rand.Next(0, 20)].Clone();
        }
    }

    public class AppItem
    {
        public FileInfo file_info;

        public string orginal_path;

        private String file_name;
        public string Name
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private String file_description;
        public string Description
        {
            get { return file_description; }
            set { file_description = value; }
        }

        private BitmapSource img_source;
        public BitmapSource Image
        {
            get { return img_source; }
            set { img_source = value; }
        }

        private BitmapSource small_img_source;
        public BitmapSource SmallImage
        {
            get { return small_img_source; }
            set { small_img_source = value; }
        }

        public AppItem(FileInfo file)
        {
            file_info = file;
            Name = System.IO.Path.GetFileNameWithoutExtension(file_info.Name);
            Image = Functions.GetFileImage256(".exe");
        }
    }

    public class GameItem
    {
        public FileInfo file_info;

        public string orginal_path;

        private String file_name;
        public string Name
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private String file_description;
        public string Description
        {
            get { return file_description; }
            set { file_description = value; }
        }

        private BitmapSource img_source;
        public BitmapSource Image
        {
            get { return img_source; }
            set { img_source = value; }
        }


        public GameItem(FileInfo file)
        {
            file_info = file;
            Name = System.IO.Path.GetFileNameWithoutExtension(file_info.Name);
            Image = Functions.GetFileImage256(".exe");
        }
    }

    public class DocumentItem
    {
        public FileInfo file_info;

        public string orginal_path;

        private String file_name;
        public string Name
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private String file_description;
        public string Description
        {
            get { return file_description; }
            set { file_description = value; }
        }

        private BitmapSource img_source;
        public BitmapSource Image
        {
            get { return img_source; }
            set { img_source = value; }
        }

        public string FileSize { get; set; }
        public string FileType { get; set; }
        public string FileDate { get; set; }
        public string Folder { get; set; }
        public string Location { get; set; }
        public long Size;
        public DateTime Date;

        public DocumentItem(FileInfo file)
        {
            file_info = file;
            Name = System.IO.Path.GetFileNameWithoutExtension(file_info.Name);
            Image = Functions.GetFileImage24(file.Extension.ToLower());
            FileType = Functions.GetFileType(file.Extension.ToLower());
            Folder = file.DirectoryName;
            Location = file.FullName;
            try
            {
                Size = file.Length;
                Date = file.LastWriteTime;
                FileSize = Functions.FileSize(Size, 2);
                FileDate = Date.ToString("g");
            }
            catch (Exception) { }
        }
    }

    enum PagePanal
    {
        Home = 1,
        Apps = 2,
        Games = 3,
        Documents = 4,
        Picture = 5,
        Music = 6,
        Vedio = 7,
        OpendApp = 8,
        Search = 9,
        ControlPanal = 10
    }
}
