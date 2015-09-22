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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Stack<string> back, front;
        ObservableCollection<MainListView> MainList = new ObservableCollection<MainListView>();
        bool Address_SelectChange_IsFake = false, CloseIfFailed = false, WorkerAsyncCancel = false;
        double items_count = 0;
        public static bool IsPaste = false;
        bool ShowHiddenFiles = false;// ShowImages = true;
        static public bool GoToPath = false;


        BackgroundWorker show_progress, GetFilesAndDirectory;
        List<FileOperationItem> ToCopyList;

        public static string to_go_path = "";

        string current_address = "Computer", next_address = "", to_select_file = null;
        static string computer_Name, operating_System, CPU = "Core", Memory_Size;
        Thickness name_thickness = new Thickness(80, 5, 0, 0), size_thickness = new Thickness(80, 25, 0, 0), type_thickness = new Thickness(80, 45, 0, 0), folder_thickness = new Thickness(80, 25, 0, 0);
        string path
        {
            get { return current_address; }
            set
            {
                Up.IsEnabled = !(value.ToLower().Equals("computer"));
                current_address = value;
            }
        }

        #region Instractor & Initialize Window

        public MainWindow()
        {
            Initializer();
            img.Source = Icons.GetIcons.GetIcon("computer.png", 256);
            if (!ContainProcess(Process.GetProcesses(), "explorer", "explorer.exe"))
                new Desktop_Environment().Show();
        }

        private bool ContainProcess(Process[] process, string p, string q)
        {
            foreach (Process i in process)
                if (i.ProcessName.ToLower().Equals(p) || i.ProcessName.ToLower().Equals(q))
                    return true;
            return false;
        }

        public MainWindow(string PrimePath)
        {
            Initializer();
            path = PrimePath;
        }

        public MainWindow(string prim_path, string to_select_file)
        {
            Initializer();
            path = prim_path;
            this.to_select_file = to_select_file;
        }

        void Initializer()
        {
            InitializeComponent();
            BinaryFormatter BF = new BinaryFormatter();
            if (File.Exists("FileInfo.dat"))
            {
                List<BasicFileInfoCombonents> info = null;
                Stream filestream = File.Open("FileInfo.dat", FileMode.Open);
                bool err = false;
                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        info = (List<BasicFileInfoCombonents>)BF.Deserialize(filestream);
                        err = false; break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(250);
                        err = true;
                    }
                }
                filestream.Close();
                if (err)
                    ShowErrorAndClose("Can not load 'FileInfo.dat' File.");

                if (info == null || !BasicFileInfoCombonents.ConvertToStandard(info))
                    ShowErrorAndClose("The file 'FileInfo.dat' is not valid.");
                if (Functions.IsRunningAsAdmin())
                    Title = "[Administrator]  " + Title;
                back = new Stack<string>();
                front = new Stack<string>();
                Explorer.DataContext = MainList;
            }
            else
                ShowErrorAndClose("Can not find 'FileInfo.dat' File.");
            //ListView.AddHandler(Control.MouseDoubleClickEvent, new RoutedEventHandler(HandleDoubleClick);
        }

        void ShowErrorAndClose(string error)
        {
            Messages.ShowMessage("Something wrong, sorry app will close now.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, error);
            Close();
        }

        #endregion

        #region Explore

        bool stop_progress = true;

        void ShowProgress()
        {
            show_progress = new BackgroundWorker();
            show_progress.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                int i = 2;
                Thread.Sleep(500);
                while (!stop_progress)
                {
                    if (i > 0)
                        show_progress.ReportProgress(i);
                    else
                        i += 2;
                    Thread.Sleep(100);
                    i *= 2;
                }
            };
            show_progress.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            { progressbar.Value += 100 / e.ProgressPercentage; };
            show_progress.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) => { progressbar.Value = 100; };
            show_progress.WorkerReportsProgress = show_progress.WorkerSupportsCancellation = true;
            show_progress.RunWorkerAsync();
            progressbar.Visibility = Visibility.Visible;
        }

        void ExplorePath(string new_path, bool close_if_failed, bool add_to_back)
        {
            WorkerAsyncCancel = false;
            stop_progress = false;
            progressbar.Value = 0;
            ShowProgress();
            //اختبار ملف أو مجلد
            GetDirectoryContent(new_path, close_if_failed, add_to_back);
        }

        private void GetDirectoryContent(string new_path, bool close_if_failed, bool add_to_back)
        {
            bool first_get = true;
            DriveInfo[] drives = new DriveInfo[0];
            List<DirectoryInfo> sub_directories = new List<DirectoryInfo>();
            List<FileInfo> files = new List<FileInfo>();

            GetFilesAndDirectory = new BackgroundWorker();
            GetFilesAndDirectory.WorkerReportsProgress = true;
            string message = "", extra = "";

            GetFilesAndDirectory.DoWork += (object sender, DoWorkEventArgs e) =>
            {   //desktop //documents //music .... etc
                if (new_path.ToLower().Equals("computer"))
                {
                    try
                    { drives = DriveInfo.GetDrives(); }
                    catch (Exception ex)
                    {
                        message = "can not load drives info, the app will exit now.";
                        extra = ex.Message;
                        WorkerAsyncCancel = true;
                        return;
                    }
                    GetFilesAndDirectory.ReportProgress(10);
                    for (int i = 0; i < drives.Length; i++)
                        GetFilesAndDirectory.ReportProgress(1, i);
                }
                else
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
                            message = "can not get directory content, app will exit now.";
                            extra = ex.Message;
                            WorkerAsyncCancel = true;
                            return;
                        }
                        GetFilesAndDirectory.ReportProgress(10);
                        for (int i = 0; i < sub_directories.Count; i++)
                            if (ShowHiddenFiles || (!ShowHiddenFiles && !sub_directories[i].Attributes.HasFlag(FileAttributes.Hidden)))
                                GetFilesAndDirectory.ReportProgress(2, i);
                        for (int i = 0; i < files.Count; i++)
                            if (ShowHiddenFiles || (!ShowHiddenFiles && !files[i].Attributes.HasFlag(FileAttributes.Hidden)))
                                GetFilesAndDirectory.ReportProgress(3, i);
                        GetFilesAndDirectory.ReportProgress(4);
                    }
                    else
                        WorkerAsyncCancel = true;
                }
            };

            GetFilesAndDirectory.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                if (first_get)
                {
                    MainList.Clear();
                    first_get = false;
                }
                switch (e.ProgressPercentage)
                {
                    case 1:
                        MainList.Add(new MainListView(drives[(int)e.UserState]));
                        break;
                    case 2:
                        MainList.Add(new MainListView(sub_directories[(int)e.UserState]));
                        break;
                    case 3:
                        MainList.Add(new MainListView(files[(int)e.UserState]));
                        break;
                    case 4:
                        count.Text = MainList.Count + (MainList.Count > 1 ? " Items" : " Item");
                        if (Explorer.Items.Count > 0)
                            Explorer.ScrollIntoView(Explorer.Items[0]);
                        break;
                    case 10:
                        path = new_path;
                        GetFilesAndDirectory.WorkerSupportsCancellation = true;
                        break;
                }
            };

            GetFilesAndDirectory.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (!WorkerAsyncCancel)
                {
                    Address_SelectChange_IsFake = true;
                    AddressHelper.Items.Clear();
                    Address_SelectChange_IsFake = false;
                    if (back.Count < 1)
                        Back.IsEnabled = false;
                    else
                        Back.IsEnabled = true;
                    if (ToCopyList != null && ToCopyList.Count > 0)
                        paste_context_menu.IsEnabled = true;
                    else
                        paste_context_menu.IsEnabled = false;
                    if (Address.Text != new_path)
                    {
                        if (add_to_back)
                            back.Push(this.path);
                        backMenu.Items.Insert(0, this.path);
                        Address.Text = path;
                    }

                    progressbar.Value = 0;
                    progressbar.Visibility = Visibility.Hidden;
                    stop_progress = true;
                }
                else
                {
                    next_address = Address.Text;
                    ExplorePath(Address.Text, close_if_failed, add_to_back);
                    if (message == "")
                        Messages.ShowMessage("Path is unreadable!", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
                    else
                        Messages.ShowMessage(message, ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, extra == "", extra);
                    if (CloseIfFailed) Close();
                }
            };
            GetFilesAndDirectory.RunWorkerAsync();
        }

        private int FilesSorter(FileInfo x, FileInfo y)
        {
            return 0;
        }

        private int DirectorySorter(DirectoryInfo x, DirectoryInfo y)
        {
            return 0;
        }

        void Exploring(bool close_if_failed, bool add_to_back)
        {
            try
            {
                WorkerAsyncCancel = false;
                stop_progress = false;
                show_progress = new BackgroundWorker();
                show_progress.WorkerSupportsCancellation = true;
                show_progress.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    BackgroundWorker BW = ((BackgroundWorker)sender);
                    int i = 3;
                    while (!stop_progress)
                    {
                        Thread.Sleep(100);
                        BW.ReportProgress(i);
                        i *= 3;
                    }
                    BW.ReportProgress(-1);
                };
                show_progress.ProgressChanged += ShowProgress_ProgressChanged;
                show_progress.WorkerReportsProgress = true;
                show_progress.RunWorkerAsync();
                Address_SelectChange_IsFake = true;
                AddressHelper.Items.Clear();
                Address_SelectChange_IsFake = false;
                BackgroundWorker GetFilesAndDirectory = new BackgroundWorker();
                MainList.Clear();
                GetFilesAndDirectory.WorkerReportsProgress = GetFilesAndDirectory.WorkerSupportsCancellation = true;
                GetFilesAndDirectory.DoWork += GetFilesAndDirectory_DoWork;
                GetFilesAndDirectory.ProgressChanged += GetFilesAndDirectory_ProgressChanged;
                GetFilesAndDirectory.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    progressbar.Value = 100;
                    progressbar.Visibility = Visibility.Hidden;
                    stop_progress = true;
                    if (!WorkerAsyncCancel)
                    {
                        if (back.Count < 1)
                            Back.IsEnabled = false;
                        else
                            Back.IsEnabled = true;
                        if (ToCopyList != null && ToCopyList.Count > 0)
                            paste_context_menu.IsEnabled = true;
                        else
                            paste_context_menu.IsEnabled = false;
                        path = next_address;
                        if (Address.Text != next_address)
                        {
                            if (add_to_back)
                                back.Push(this.path);
                            backMenu.Items.Insert(0, this.path);
                            Address.Text = path;
                        }
                        next_address = "";
                    }
                    else
                    {
                        //next_address = path;
                        ExplorePath(path, false, true);
                        Messages.ShowMessage("Drive is not ready!.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
                        if (CloseIfFailed) Close();
                    }
                };
                /*GetFilesAndDirectory_RunWorkerCompleted;*/
                CloseIfFailed = close_if_failed;
                GetFilesAndDirectory.RunWorkerAsync(add_to_back);
                progressbar.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not view '"+ next_address+"' content.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                if (close_if_failed) Close();
            }
        }

        #endregion


        void Item_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ContextMenu c = (((ListViewItem)sender).Content as MainListView).Menu;
            if (c == null)
            {
                MainListView item = ((ListViewItem)sender).Content as MainListView;
                c = new ContextMenu();
                if (item.File == null)
                    MakeDriveContextMenu(item.Drive, c);
                else if (item.File is FileInfo)
                    File_ContextMenuCreator(c);
                else
                    Directory_ContextMenuCreator(c);
            }
            //c.IsOpen = true;
            //Explorer.ContextMenu.IsOpen = false;
        }

        void File_ContextMenuCreator(ContextMenu file_context_menu)
        {
            //ContextMenu file_context_menu = ((ListViewItem)sender).ContextMenu;
            if (file_context_menu.Items.Count < 1)
            {
                MenuItem menu_item = new MenuItem();
                menu_item.Header = "Open";
                menu_item.Click += Open_Click;
                file_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Open with";
                menu_item.Click += OpenInNewWindow_Click;
                file_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Preview";
                menu_item.Click += Preview_Click;
                file_context_menu.Items.Add(menu_item);
                file_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Cut";
                menu_item.Click += Cut_Click;
                file_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Copy";
                menu_item.Click += Copy_Click;
                file_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Duplicate";
                menu_item.Click += Duplicate_Click;
                file_context_menu.Items.Add(menu_item);
                file_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Create shortcut";
                //menu_item.Click += CreateShortcut;
                file_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Delete";
                menu_item.Click += Delete_Click;
                file_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Rename";
                menu_item.Click += RenameFile_Click;
                file_context_menu.Items.Add(menu_item);
                file_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Properties";
                menu_item.Click += Properties_Click;
                file_context_menu.Items.Add(menu_item);
            }
        }

        void Directory_ContextMenuCreator(ContextMenu directory_context_menu)
        {
            //ContextMenu directory_context_menu = ((ListViewItem)sender).ContextMenu;
            if (directory_context_menu.Items.Count == 0)
            {
                MenuItem menu_item = new MenuItem();
                menu_item.Header = "Open";
                menu_item.Click += Open_Click;
                directory_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Open in new window";
                menu_item.Click += OpenInNewWindow_Click;
                directory_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Preview";
                menu_item.Click += Preview_Click;
                directory_context_menu.Items.Add(menu_item);
                directory_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Cut";
                menu_item.Click += Cut_Click;
                directory_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Copy";
                menu_item.Click += Copy_Click;
                directory_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Duplicate";
                menu_item.Click += Duplicate_Click;
                directory_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Paste into directory";
                menu_item.IsEnabled = false;
                menu_item.Click += PasteIn_Click;
                directory_context_menu.Items.Add(menu_item);
                directory_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Create shortcut";
                //menu_item.Click += CreateShortcut;
                directory_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Delete";
                menu_item.Click += Delete_Click;
                directory_context_menu.Items.Add(menu_item);
                menu_item = new MenuItem();
                menu_item.Header = "Rename";
                menu_item.Click += RenameFile_Click;
                directory_context_menu.Items.Add(menu_item);
                directory_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Properties";
                menu_item.Click += Properties_Click;
                directory_context_menu.Items.Add(menu_item);
            }
        }

        #region delete

        void GetFilesAndDirectory_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker BW = (BackgroundWorker)sender;
            if (next_address == "Computer")
            {
                var drives = DriveInfo.GetDrives();
                foreach (var i in drives)
                    BW.ReportProgress(1, i);
            }
            else
            {
                DirectoryInfo Current_Directory = new DirectoryInfo(next_address);
                try
                {
                    var subdirectories = Current_Directory.GetDirectories();
                    var files = Current_Directory.GetFiles();
                    items_count = files.Length + subdirectories.Length;
                    for (int i = 0; i < subdirectories.Length; i++)
                        if (ShowHiddenFiles || (!ShowHiddenFiles && !subdirectories[i].Attributes.HasFlag(FileAttributes.Hidden)))
                            BW.ReportProgress(2, subdirectories[i]);
                    for (int i = 0; i < files.Length; i++)
                        if (ShowHiddenFiles || (!ShowHiddenFiles && !files[i].Attributes.HasFlag(FileAttributes.Hidden)))
                            BW.ReportProgress(3, files[i]);
                    BW.ReportProgress(4);
                }
                catch (Exception) { BW.ReportProgress(10, BW); }
            }
        }

        //void GetFilesAndDirectory_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    progressbar.Value = 100;
        //    progressbar.Visibility = Visibility.Hidden;
        //    stop_progress = true;
        //    if (!WorkerAsyncCancel)
        //    {
        //        if (back.Count < 1)
        //            Back.IsEnabled = false;
        //        else
        //            Back.IsEnabled = true;
        //        if (ToCopyList != null && ToCopyList.Count > 0)
        //            paste_context_menu.IsEnabled = true;
        //        else
        //            paste_context_menu.IsEnabled = false;
        //        path = next_address;
        //        if (Address.Text != next_address)
        //        {
        //            if(add_to_back)
        //            back.Push(this.path);
        //            backMenu.Items.Insert(0, this.path);
        //            Address.Text = path;
        //        }
        //        next_address = "";
        //    }
        //    else
        //    {
        //        next_address = path;
        //        Exploring(false, true);
        //        new My_MessageBox("File Manager", "Drive is not ready!.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); if (CloseIfFailed) Close();
        //    }
        //}

        void GetFilesAndDirectory_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case 1:
                    MainList.Add(new MainListView((DriveInfo)e.UserState));
                    break;
                case 2:
                    MainList.Add(new MainListView((DirectoryInfo)e.UserState));
                    break;
                case 3:
                    MainList.Add(new MainListView((FileInfo)e.UserState));
                    break;
                case 4:
                    count.Text = MainList.Count + (MainList.Count > 1 ? " Items" : " Item");
                    if (Explorer.Items.Count > 0)
                        Explorer.ScrollIntoView(Explorer.Items[0]);
                    break;
                case 10:
                    WorkerAsyncCancel = true;
                    ((BackgroundWorker)e.UserState).CancelAsync();
                    break;
            }

        }

        void ShowProgress_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker BW = ((BackgroundWorker)sender);
            int i = 3;
            while (!stop_progress)
            {
                Thread.Sleep(100);
                BW.ReportProgress(i);
                i *= 3;
            }
            BW.ReportProgress(-1);
        }

        void ShowProgress_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > 0)
                progressbar.Value += 100 / e.ProgressPercentage;
            else
                progressbar.Value = 0;
        }

        #endregion

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Functions.SystemDrive = new DirectoryInfo(Environment.SystemDirectory).Root.FullName;
            //next_address = path;
            ExplorePath(path, true, true);
            //if (CPU.Equals("!"))
            //    CollectSystemInformation();
            name.Text = computer_Name;
            type.Text = operating_System;
            size.Text = CPU;
            data_Modified.Text = Memory_Size;
            if (to_select_file != null)
                SelectItem(to_select_file);
            string f = Process.GetCurrentProcess().StartInfo.Verb;
        }

        void SelectItem(string to_select_item)
        {
            for (int i = 0; i < Explorer.Items.Count; i++)
                if ((Explorer.Items[i] as MainListView).FullPath.ToLower().Equals(to_select_item.ToLower()))
                {
                    Explorer.SelectedIndex = i;
                    Explorer.ScrollIntoView(Explorer.Items[i]);
                    return;
                }
        }

        #region Menu Command

        void New_Window(object sender, RoutedEventArgs e)
        {
            new MainWindow("Computer").Show();
        }

        private void NewWindowAdmin_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo PSI = new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath);
            PSI.Verb = "runas";
            try
            { Process.Start(PSI); }
            catch (Exception ex)
            { Messages.ShowMessage("Error while openning the app.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void NewDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (path != "Computer")
            {
                string new_name = null;
                new_name = new Rename().ShowWindow("new directory", false);
                NewDirectory(new_name);
                Refresh();
            }
            else
                Messages.ShowMessage("You can not create directory here!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Close();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowHiddenFiles_Click(object sender, RoutedEventArgs e)
        {
            ShowHiddenFiles = !ShowHiddenFiles;
            Refresh();
        }

        private void ShowImage_Click(object sender, RoutedEventArgs e)
        {
            //ShowImages = !ShowImages;
            //Refresh();
        }

        private void DublicateFileRemover_Click(object sender, RoutedEventArgs e)
        {
            new Identical_File_Remover().Show();
        }

        private void BigFileFinder_Click(object sender, RoutedEventArgs e)
        {
            new Big_File_Finder().Show();
        }

        private void SuperFilesRenamer_Click(object sender, RoutedEventArgs e)
        {
            new Renamer().Show();
        }

        private void SystemMonitor_Click(object sender, RoutedEventArgs e)
        {
            new System_Monitor().Show();
        }

        private void EmptyDirectoriesRemover_Click(object sender, RoutedEventArgs e)
        {
            (new Empty_Folder_Remover()).Show();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            Explorer.SelectAll();
        }

        private void InverseSelection_Click(object sender, RoutedEventArgs e)
        {
            //var selected = Explorer.SelectedItems;
            //var unselected = new;
            //foreach(var i in Explorer.Items)
            //{
            //    if (!selected.Contains(i))
            //        unselected.Add(i);
            //}
            //Explorer.SelectedItems = unselected;
        }

        private void NewShortcut_Click(object sender, RoutedEventArgs e)
        {
            New_Shortcut.CreateShortcut("", true);
        }

        private void PlanText_Click(object sender, RoutedEventArgs e)
        {
            NewFile(".txt");
        }

        private void HTML_Click(object sender, RoutedEventArgs e)
        {
            NewFile(".htm");
        }

        private void Bitmap_Click(object sender, RoutedEventArgs e)
        {
            NewFile(".png");
        }

        private void DOCX_Click(object sender, RoutedEventArgs e)
        {
            NewFile(".docx");
        }

        private void ODT_Click(object sender, RoutedEventArgs e)
        {
            NewFile(".odt");
        }

        private void PPTX_Click(object sender, RoutedEventArgs e)
        {
            NewFile(".pptx");
        }

        private void ODP_Click(object sender, RoutedEventArgs e)
        {
            NewFile(".odp");
        }

        private void XLSX_Click(object sender, RoutedEventArgs e)
        {
            NewFile(".xlsx");
        }

        private void ODS_Click(object sender, RoutedEventArgs e)
        {
            NewFile(".ods");
        }

        private void FileCompresser_Click(object sender, RoutedEventArgs e)
        {
            new File_Compresser().Show();
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            //new Properties_Window(((ListViewItem)Explorer.SelectedItem).ContentStringFormat).Show();
        }

        private void Format_Click(object sender, RoutedEventArgs e)
        {
            //new Format_Window(((ListViewItem)Explorer.SelectedItem).ContentStringFormat).Show();
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            if (Explorer.SelectedItems.Count == 1)
                PreviewItem();
            else
                Messages.ShowMessage("You can not preview more than one item in same 'Preview' window.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            if (path != "Computer")
            {
                if (ToCopyList == null)
                    ToCopyList = new List<FileOperationItem>();
                else
                {
                    if (!IsPaste)
                    {
                        if (Messages.ShowMessage("Do you want to append files to the list?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "In clipboard there is items have not been paste yet, if you click 'NO' thies items will be removed from clipboard.") == MyMessageBoxResult.No)
                            ToCopyList.Clear();
                    }
                    else
                        ToCopyList.Clear();
                }
                foreach (MainListView i in Explorer.SelectedItems)
                    if (i.File != null && i.File is FileInfo)
                        ToCopyList.Add(new FileOperationItem((FileInfo)i.File, true, i.File.FullName.Substring(path.Length)));
                    else if (i.File != null)
                        ToCopyList.Add(new FileOperationItem((DirectoryInfo)i.File, true, i.File.FullName.Substring(path.Length)));
            }
            else
                Messages.ShowMessage("You can not move any item from here.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (path != "Computer")
            {
                if (ToCopyList == null)
                    ToCopyList = new List<FileOperationItem>();
                else
                {
                    if (!IsPaste)
                    {
                        if (Messages.ShowMessage("Do you want to append files to the list?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "In clipboard there is items have not been paste yet, if you click 'NO' thies items will be removed from clipboard.") == MyMessageBoxResult.No)
                            ToCopyList.Clear();
                    }
                    else
                        ToCopyList.Clear();
                }
                foreach (MainListView i in Explorer.SelectedItems)
                    if (i.File != null && i.File is FileInfo)
                        ToCopyList.Add(new FileOperationItem((FileInfo)i.File, false, i.File.FullName.Substring(path.Length)));
                    else if (i.File != null)
                        ToCopyList.Add(new FileOperationItem((DirectoryInfo)i.File, false, i.File.FullName.Substring(path.Length)));
            }
            else
                foreach (MainListView i in Explorer.SelectedItems)
                    if (i.Drive.IsReady)
                        ToCopyList.Add(new FileOperationItem(i.Drive.RootDirectory, false, ""));
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            if (ToCopyList != null && ToCopyList.Count > 0)
                if (path != "Computer")
                    new Operation().CopyDialogShow(ToCopyList, OperationType.Copy, path, false);
                else
                    Messages.ShowMessage("You can not paste any item here.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else
                Messages.ShowMessage("Clipboard is empty.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "There is not any item to paste it!");
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            DeteteSelectetFiles();
        }

        private void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var i in Explorer.SelectedItems)
                    Functions.OpenWith(((ListViewItem)i).ContentStringFormat, Functions.GetItemType(((ListViewItem)i).ContentStringFormat));
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
            }
        }

        private void RenameDrive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string new_name = new Rename().ShowWindow(((MainListView)Explorer.SelectedItem).Drive.VolumeLabel, false);
                if (new_name != null)
                    ((MainListView)Explorer.SelectedItem).Drive.VolumeLabel = new_name;
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileOrDirectory();
        }

        private void VirtualMachine_Click(object sender, RoutedEventArgs e)
        {
            new Virtual_Machine().Show();
        }

        private void Eject_Click(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        #endregion


        #region Function

        private void NewFile(string extension)
        {
            if (path != "Computer")
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
                    catch (Exception ex)
                    { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                    Refresh();
                }
            }
            else
                Messages.ShowMessage("You can not create any files here.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void NewDirectory(string new_name)
        {
            string dir_name = null; ;
            if (new_name != null && new_name != "")
            {
                if (Directory.Exists(path + "\\" + new_name))
                {
                    int i = 1;
                    while (Directory.Exists(path + "\\" + new_name + " (" + i + ")")) i++;
                    if (Messages.ShowMessage("Directory with same name is exist.", ModrenMessageButton.YesNo, ModrenMessageImage.Rename, ModrenMessageSound.Question, true, "Do you want to create directory '" + new_name + " (" + i + ")' instat of '" + new_name + "'") == MyMessageBoxResult.Yes)
                        dir_name = path + "\\" + new_name + " (" + i + ")";
                }
                else
                    dir_name = path + "\\" + new_name;
                try { if (dir_name != null) Directory.CreateDirectory(dir_name); }
                catch (Exception ex) { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            }
        }

        private void PreviewItem()
        {
            if (Address.Text.ToLower() == "computer")
                new Preview().ShowDialog(MainList[Explorer.SelectedIndex].Drive, ItemType.Drive);
            else if (MainList[Explorer.SelectedIndex].File is FileInfo && MainList[Explorer.SelectedIndex].File.Exists)
                new Preview().ShowDialog(MainList[Explorer.SelectedIndex].File, ItemType.File);
            else if (MainList[Explorer.SelectedIndex].File.Exists)
                new Preview().ShowDialog(MainList[Explorer.SelectedIndex].File, ItemType.Directory);
            if (GoToPath)
            {
                //next_address = to_go_path;
                ExplorePath(to_go_path, false, true);
            }
        }

        private void DeteteSelectetFiles()
        {
            if (path != "Computer" && Explorer.SelectedItems.Count > 0)
            {
                bool delete = false;
                if (Explorer.SelectedItems.Count > 1)
                {
                    StringBuilder items_name = new StringBuilder();
                    foreach (var i in Explorer.SelectedItems)
                        items_name.AppendLine((i as MainListView).FullPath);
                    if (Messages.ShowMessage(string.Format("Are you sure you want to permanently delete these {0} items", Explorer.SelectedItems.Count), ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, items_name.ToString()) == MyMessageBoxResult.Yes)
                        delete = true;
                }
                else if (Messages.ShowMessage("Are you sure you want to permanently delete these item", ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, Messages.GetItemInfo((Explorer.SelectedItem as MainListView).File)) == MyMessageBoxResult.Yes)
                    delete = true;
                if (delete)
                {
                    List<FileOperationItem> to_delete_list = new List<FileOperationItem>();
                    foreach (MainListView i in Explorer.SelectedItems)
                        if (i.File is DirectoryInfo)
                            to_delete_list.Add(new FileOperationItem((DirectoryInfo)i.File, false, ""));
                        else
                            to_delete_list.Add(new FileOperationItem((FileInfo)i.File, false, ""));
                    new Operation().CopyDialogShow(to_delete_list, OperationType.Delete, "", false);
                }
            }
            else
                Messages.ShowMessage("You can not delete any items from here.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void OpenFileOrDirectory()
        {
            bool new_window = false;
            if (Explorer.SelectedItems.Count < 20 && Explorer.SelectedItems.Count > 0)
            {
                List<MainListView> list = new List<MainListView>();
                foreach (MainListView i in Explorer.SelectedItems)
                    list.Add(i);
                if (path.ToLower().Equals("computer"))
                {
                    foreach (MainListView i in list)
                        try
                        {
                            if (new_window)
                                new MainWindow(i.FullPath).Show();
                            else
                            {
                                //next_address = i.FullPath;
                                ExplorePath(i.FullPath, false, true);
                                //Exploring(false, true);
                                front.Clear();
                                Forword.IsEnabled = false;
                                new_window = true;
                            }
                        }
                        catch (Exception ex) { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
                else
                {
                    foreach (MainListView i in list)
                        try
                        {
                            if (i.File is FileInfo)
                                Process.Start(new ProcessStartInfo(i.FullPath));
                            else if (i.File is DirectoryInfo)
                            {
                                if (new_window)
                                    new MainWindow(i.FullPath).Show();
                                else
                                {
                                    //next_address = i.FullPath;
                                    ExplorePath(i.FullPath, false, true);
                                    //Exploring(false, true);
                                    front.Clear();
                                    Forword.IsEnabled = false;
                                    new_window = true;
                                }
                            }
                        }
                        catch (Exception ex) { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
            }
        }

        private void Refresh()
        {
            int index = Explorer.SelectedIndex;
            //next_address = path;
            ExplorePath(path, false, false);
            Explorer.SelectedIndex = index;
            if (index > -1 && index < Explorer.Items.Count)
                Explorer.ScrollIntoView(Explorer.Items[index]);
        }

        #endregion


        #region Interface

        private void Address_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                if (Directory.Exists(Address.Text))
                {
                    //next_address = Address.Text;
                    ExplorePath(Address.Text, false, true);
                }
                else if (File.Exists(Address.Text))
                    try { Process.Start(new ProcessStartInfo(Address.Text)); }
                    catch (Exception ex)
                    { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                else
                {
                    Address.Text = path;
                    Messages.ShowMessage("Path is not exist.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                }
            }
            else if (e.Key == Key.Oem5)
            {
                Address_SelectChange_IsFake = true;
                AddressHelper.Items.Clear();
                AddressHelper.IsDropDownOpen = true;
                if (Directory.Exists(Address.Text))
                    try
                    {
                        DirectoryInfo DI = new DirectoryInfo(Address.Text);
                        try
                        {
                            foreach (var i in DI.GetDirectories())
                                if (ShowHiddenFiles || (!ShowHiddenFiles && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                                    AddressHelper.Items.Add(i.FullName);
                        }
                        catch (Exception) { }
                    }
                    catch (Exception) { }
                Address_SelectChange_IsFake = false;
            }
            else if (AddressHelper.Items.Count > 0)
            {
                AddressHelper.IsDropDownOpen = true;
                DirectoryInfo DI = (new DirectoryInfo((string)AddressHelper.Items[0])).Parent;
                AddressHelper.Items.Clear();
                try
                {
                    foreach (var i in DI.GetDirectories())
                        if (ShowHiddenFiles || (!ShowHiddenFiles && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                            if (Address.Text.Length <= i.FullName.Length)
                                if (i.FullName.Substring(0, Address.Text.Length).ToLower().Equals(Address.Text.ToLower()))
                                    AddressHelper.Items.Add(i.FullName);
                }
                catch (Exception) { }
            }
        }

        private void GoUP_Click(object sender, RoutedEventArgs e)
        {
            if (GetFilesAndDirectory != null && GetFilesAndDirectory.IsBusy)
                while (!GetFilesAndDirectory.WorkerSupportsCancellation)
                    Thread.Sleep(10);
            string new_path, old_path = path;
            string[] split_path = path.Split(Functions.spliter, StringSplitOptions.RemoveEmptyEntries);
            if (!path.ToLower().Equals("computer"))
            {
                if (split_path.Length == 1)
                    new_path = "computer";
                else
                    try
                    { new_path = new DirectoryInfo(path).Parent.FullName; }
                    catch (Exception ex)
                    {
                        Messages.ShowMessage("Sorry we can not go up.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                        return;
                    }
                if (GetFilesAndDirectory.WorkerSupportsCancellation && GetFilesAndDirectory.IsBusy)
                    GetFilesAndDirectory.CancelAsync();
                //next_address = new_path;
                ExplorePath(new_path, false, true);
                front.Clear();
                Forword.IsEnabled = false;
                SelectItem(old_path);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (back.Count > 1)
            {
                string old_path = path;
                front.Push(path);
                back.Pop();
                //next_address = back.Peek();
                ExplorePath(back.Peek(), false, false);
                Forword.IsEnabled = true;
                SelectItem(old_path);
            }
            if (back.Count < 2)
                Back.IsEnabled = false;
            //View_Items_Basic_Info(new_path, "Folder");
        }

        private void GoFront_Click(object sender, RoutedEventArgs e)
        {
            if (front.Count > 0)
            {
                //next_address = front.Pop();
                string old_path = path;
                back.Push(next_address);
                ExplorePath(front.Pop(), false, false);
                SelectItem(old_path);
            }
            if (front.Count < 1)
                Forword.IsEnabled = false;
            //View_Items_Basic_Info(new_path, "Folder");
        }

        private void Explorer_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            GoToPath = false;
            if (e.Key.Equals(Key.Space) && Explorer.SelectedIndex > -1)
                PreviewItem();
            else if (e.Key.Equals(Key.Enter) && Explorer.SelectedIndex > -1)
                OpenFileOrDirectory();
            else if (e.Key == Key.Delete)
                DeteteSelectetFiles();
            else if (e.Key == Key.F2)
                if (path == "Computer")
                    RenameDrive_Click(null, null);
                else
                    RenameFile_Click(null, null);
        }

        private void Explorer_KeyUP(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
                Refresh();
        }

        private void Item_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                OpenFileOrDirectory();
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void SurfingBackMenu_SelectionChange(object sender, SelectionChangedEventArgs e)
        {
            //next_address = (backMenu.Items[backMenu.SelectedIndex]).ToString();
            ExplorePath((backMenu.Items[backMenu.SelectedIndex]).ToString(), false, false);
        }

        private void Duplicate_Click(object sender, RoutedEventArgs e)
        {
            ////////////List<FileOperationItem> files = new List<FileOperationItem>();
            ////////////foreach (MainListView i in Explorer.SelectedItems)
            ////////////    if (i.File is FileInfo && i.File.Exists)
            ////////////        files.Add(new FileOperationItem((FileInfo)i.File, false, null, i.File.FullName.Substring(path.Length - 1)));
            ////////////    else if (i.File is DirectoryInfo)
            ////////////        files.Add(new FileOperationItem((DirectoryInfo)i.File, 0, false, null, i.File.FullName.Substring(path.Length - 1)));
            ////////////new File_Operation().CopyDialogShow(path, files);
            //string new_file_name = i.FullPath.Remove(i.FullPath.Length - i.File.Extension.Length) + "- Copy" + i.File.Extension;
            //if (File.Exists(new_file_name))
            //{
            //    int k = 1;
            //    while (File.Exists(i.FullPath.Remove(i.FullPath.Length - i.File.Extension.Length) + "- Copy (" + k + ")" + i.File.Extension)) k++;
            //    new_file_name = i.FullPath.Remove(i.FullPath.Length - i.File.Extension.Length) + "- Copy (" + k + ")" + i.File.Extension;
            //}
            //else
            //{
            //    string paste_in = i.FullPath + "- Copy";
            //    if (Directory.Exists(i.ContentStringFormat + "- Copy"))
            //    {
            //        int k = 1;
            //        while (Directory.Exists(i.ContentStringFormat + "- Copy (" + k + ")")) k++;
            //        paste_in = i.ContentStringFormat + "- Copy (" + k + ")";
            //    }
            //    foreach (var j in Directory.GetFiles(i.ContentStringFormat))
            //        to_copy.Add(new FileOperationItem(j, j.Remove(0, i.ContentStringFormat.Length), false));
            //    foreach (var j in Directory.GetDirectories(i.ContentStringFormat))
            //        Functions.AddAllFiles(j, i.ContentStringFormat, false, to_copy);
            //    try
            //    { Directory.CreateDirectory(paste_in); }
            //    catch (Exception ex) 
            //    { 
            //      Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
            //       return; 
            //    }
            //    File_Operation window = new File_Operation(to_copy);
            //    window.Show(paste_in);
            //}
            //if (files.Count > 0)
            //{
            //    File_Operation window2 = new File_Operation(files);
            //    window2.Show(path);
            //}
            //Refresh();
        }

        private void PasteIn_Click(object sender, RoutedEventArgs e)
        {//****
            //new Operation().CopyDialogShow(((MainListView)Explorer.SelectedItem).FullPath);
        }

        private void RenameFile_Click(object sender, RoutedEventArgs e)
        {
            if (path != "Computer")
            {
                string new_name = new Rename().ShowWindow(((MainListView)Explorer.SelectedItem).File.FullName, true);
                if (new_name != null)
                {
                    if (path != "Computer" && Explorer.SelectedItems.Count > 0)
                    {
                        List<FileOperationItem> to_rename_list = new List<FileOperationItem>();
                        foreach (MainListView i in Explorer.SelectedItems)
                            if (i.File is DirectoryInfo)
                                to_rename_list.Add(new FileOperationItem((DirectoryInfo)i.File, false, new_name));
                            else
                                to_rename_list.Add(new FileOperationItem((FileInfo)i.File, false, new_name));
                        new Operation().CopyDialogShow(to_rename_list, OperationType.Rename, new_name, true);
                    }
                }
            }
            Refresh();
        }

        private void Address_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!Address_SelectChange_IsFake)
            {
                //next_address = AddressHelper.SelectedItem.ToString();
                ExplorePath(AddressHelper.SelectedItem.ToString(), false, true);
            }
        }

        #endregion


        #region View Quick Information About Element

        private void Explorer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Explorer.SelectedItems.Count > 0)
            {
                total_files_size.Text = "";
                selected.Text = "«" + Explorer.SelectedItems.Count + " item(s) selected»";
                if (Explorer.SelectedItems.Count == 1 || path.ToLower().Equals("computer"))
                {
                    if (path.ToLower().Equals("computer"))
                        ViewDriveInfo(((MainListView)Explorer.SelectedItem).Drive);
                    else
                    {
                        FileSystemInfo file = ((MainListView)Explorer.SelectedItem).File;
                        if (file.Exists && file is FileInfo)
                            ViewFileInfo((FileInfo)file);
                        else if (file.Exists)
                            ViewFolderInfo((DirectoryInfo)file);
                    }
                }
                else if (Explorer.SelectedItems.Count > 1)
                {
                    long total_size = 0;
                    if (Explorer.SelectedItems.Count < 101)
                    {
                        foreach (MainListView i in Explorer.SelectedItems)
                            if (i.File != null && i.File is FileInfo)
                                total_size += ((FileInfo)i.File).Length;
                        total_files_size.Text = "Total selected files size: " + Functions.FileSize(total_size, 2);
                    }
                    else
                        total_files_size.Text = "";
                    ViewMultiItemsInfo();
                }
            }
            else
                selected.Text = string.Empty;
        }

        private void ViewFolderInfo(DirectoryInfo di)
        {
            //int sub_dirs_count = 0, sub_files_count = 0;
            img.Source = FileInfoCombonents.FilesInformation["directory"].Bitmap256;
            name.Text = di.Name;
            type.Text = "Directory";
            //try
            //{
            //    sub_dirs_count = Directory.GetDirectories(di.FullName).Length;
            //    sub_files_count = Directory.GetFiles(di.FullName).Length;
            //}
            //catch (Exception) { }
            SetDefualt();
            size_title.Text = " 1 Directory";
            size.Text = " Selected";
            //size_title.Text = "Contains: ";
            //size.Text = sub_dirs_count + " Folders & " + sub_files_count + " Files";
            data_Modified.Text = di.LastWriteTime.ToString();
        }

        private void ViewFileInfo(FileInfo fi)
        {
            SetDefualt();
            name.Text = fi.Name;
            size.Text = Functions.FileSize(fi.Length, 2);
            data_Modified.Text = fi.LastWriteTime.ToString();
            img.Source = Functions.GetFileImage256(fi.Extension);
            type.Text = Functions.GetFileType(fi.Extension);
            if (FileInfoCombonents.FilesInformation.ContainsKey(fi.Extension.ToLower()))
                if (FileInfoCombonents.FilesInformation[fi.Extension.ToLower()].Category == FileCategory.Image)
                    try { img.Source = new BitmapImage(new Uri(fi.FullName)); }
                    catch (Exception) { }
        }

        private void ViewDriveInfo(DriveInfo drive)
        {
            img.Source = Functions.GetDriveImage(drive, 256);
            if (drive.IsReady)
            {
                name.Text = drive.VolumeLabel + " (" + drive.Name + ")";
                type_title.Text = "Format: ";
                type.Text = drive.DriveFormat + " File System";
                size_title.Text = "Disk Size: ";
                size.Text = Functions.FileSize(drive.TotalSize, 2) + " (" + Functions.FileSize(drive.AvailableFreeSpace, 2) + " Free)";
            }
            else
            {
                SetDefualt();
                name.Text = drive.Name;
                type.Text = drive.DriveType.ToString();
                size_title.Text = "";
                size.Text = "Drive is not Ready";
            }
            data_Modified.Text = "";
            data_Modified_title.Text = "";
        }

        private void ViewMultiItemsInfo()
        {
            SetDefualt();
            int dirs_count = 0, files_count = 0;
            name.Text = "Multi items have been selected";
            bool change = false;
            string file_type = "", ext = "";
            if (Explorer.SelectedItems.Count > 0)
                ext = (Explorer.SelectedItem as MainListView).File is FileInfo ? (Explorer.SelectedItem as MainListView).File.Extension : "Directory";
            long total_files_size = 0;
            int k = 0;
            foreach (MainListView i in Explorer.SelectedItems)
            {
                if (i.File != null && i.File is FileInfo)
                {
                    files_count++;
                    total_files_size += ((FileInfo)i.File).Length;
                    if (!change)
                    {
                        string tmp = Functions.GetFileType(((FileInfo)i.File).Extension);
                        if (file_type != tmp)
                        {
                            file_type = tmp;
                            if (k == 1)
                                change = true;
                            k++;
                        }
                    }
                }
                else
                {
                    dirs_count++;
                    if (!change)
                    {
                        if (file_type != "Directory")
                        {
                            file_type = "Directory";
                            if (k == 1)
                                change = true;
                            k++;
                        }
                    }
                }
            }
            if (change)
            {
                type.Text = "Multi Type Selected";
                img.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/multi-selection.png"));
            }
            else
            {
                type.Text = "All Types '" + file_type + "'";
                img.Source = Functions.GetFileImage256(ext);
            }
            size_title.Text = "All Files Size: ";
            size.Text = Functions.FileSize(total_files_size, 2);
            data_Modified.Text = "Multi items have been selected";
        }

        private void ViewComputerInfo()
        {
            name.Text = computer_Name;
            type.Text = operating_System;
            size.Text = CPU;
            data_Modified.Text = Memory_Size;
            img.Source = new BitmapImage(new Uri(Functions.StandardImageSource + "256/" + "computer.png"));
        }

        #endregion


        private void SetDefualt()
        {
            name_title.Text = "Name: ";
            type_title.Text = "Type: ";
            size_title.Text = "Size: ";
            data_Modified_title.Text = "Data Modified: ";
        }

        private ContextMenu MakeDriveContextMenu(DriveInfo drive, ContextMenu drive_context_menu)
        {
            //ContextMenu drive_context_menu = new ContextMenu();
            MenuItem menu_item = new MenuItem();
            menu_item.Header = "Open";
            menu_item.Click += Open_Click;
            drive_context_menu.Items.Add(menu_item);
            menu_item = new MenuItem();
            menu_item.Header = "Open in new window";
            menu_item.Click += OpenInNewWindow_Click;
            drive_context_menu.Items.Add(menu_item);
            menu_item = new MenuItem();
            menu_item.Header = "Preview";
            menu_item.Click += Preview_Click;
            drive_context_menu.Items.Add(menu_item);
            if (drive.IsReady)
            {
                drive_context_menu.Items.Add(new Separator());
                if (ToCopyList != null && ToCopyList.Count > 0)
                {
                    menu_item = new MenuItem();
                    menu_item.Header = "Paste in this location";
                    menu_item.Click += PasteIn_Click;
                    drive_context_menu.Items.Add(menu_item);
                    drive_context_menu.Items.Add(new Separator());
                }
                menu_item = new MenuItem();
                menu_item.Header = "Format";
                menu_item.Click += Format_Click;
                drive_context_menu.Items.Add(menu_item);
                if (drive.DriveType == DriveType.Removable)
                {
                    menu_item = new MenuItem();
                    menu_item.Header = "Eject";
                    menu_item.Click += Eject_Click;
                }
                drive_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Copy to";
                //menu_item.Click += CopyTo_Click;
                drive_context_menu.Items.Add(menu_item);
                drive_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Rename";
                menu_item.Click += RenameDrive_Click;
                drive_context_menu.Items.Add(menu_item);
            }
            drive_context_menu.Items.Add(new Separator());
            menu_item = new MenuItem();
            menu_item.Header = "Properties";
            menu_item.Click += Properties_Click;
            drive_context_menu.Items.Add(menu_item);
            return drive_context_menu;
        }

        private void Explorer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //Explorer.UnselectAll();
        }




        private void Explorer_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (Explorer.SelectedIndex > -1)
                Explorer.ContextMenu.IsOpen = false;
        }

        private void Explorer_KeyUP(object sender, RoutedEventArgs e)
        {
            Refresh(null, null);
        }

        private void FilesDestoyer_Click(object sender, RoutedEventArgs e)
        {
            new File_Destroyer().Show();
        }

        private void SortByName_Click(object sender, RoutedEventArgs e)
        {
            List<MainListView> list = MainList.ToList<MainListView>();
            list.Sort((MainListView x, MainListView y) => { return x.FileName.CompareTo(y.FileName); });
            MainList.Clear();
            foreach (var i in list)
                MainList.Add(i);
        }

        private void SortBySize_Click(object sender, RoutedEventArgs e)
        {
            List<MainListView> list = MainList.ToList<MainListView>();
            list.Sort((MainListView x, MainListView y) =>
            {
                return ((x.File is FileInfo) ? (x.File as FileInfo).Length : 0).CompareTo((y.File is FileInfo) ? (y.File as FileInfo).Length : 0);
            });
            MainList.Clear();
            foreach (var i in list)
                MainList.Add(i);
        }

        private void TypeBySize_Click(object sender, RoutedEventArgs e)
        {
            List<MainListView> list = MainList.ToList<MainListView>();
            list.Sort((MainListView x, MainListView y) => { return x.FileType.CompareTo(y.FileType); });
            MainList.Clear();
            foreach (var i in list)
                MainList.Add(i);
        }

        private void DateModifiedBySize_Click(object sender, RoutedEventArgs e)
        {
            List<MainListView> list = MainList.ToList<MainListView>();
            list.Sort((MainListView x, MainListView y) => { return x.File.LastWriteTime.Ticks.CompareTo(y.File.LastWriteTime.Ticks); });
            MainList.Clear();
            foreach (var i in list)
                MainList.Add(i);
        }

        private void ShowDetailsPanal_Click(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).IsChecked)
            {
                Properties.Visibility = Visibility.Visible;
                Explorer.Margin = new Thickness(0, 0, 220, 19);
            }
            else
            {
                Properties.Visibility = Visibility.Collapsed;
                Explorer.Margin = new Thickness(0, 0, 0, 19);
            }
        }

        private void FilesEncreptor_Click(object sender, RoutedEventArgs e)
        {
            new Files_Encrepter().Show();
        }

        private void OpenFileDialog_Click(object sender, RoutedEventArgs e)
        {
            //SelectFileDialog df = new SelectFileDialog() { InistialDirectory = "C:\\Windows", dialogMode = SelectFileDialogMode.OpenFile, MultiSelect = true };
            //df.FilterMenu.Add(new SelectFileFilter() { extentions = ".exe|.bat".Split('|'), display_name = "Application" });
            //df.ShowWindow();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            new about_app().ShowDialog();
        }


    }


    public class MainListView
    {
        public MainListView(FileInfo file)
        {
            File = file;
            FileName = file.Name;
            FullPath = File.FullName;
            if (FileInfoCombonents.FilesInformation.ContainsKey(file.Extension.ToLower()))
            {
                Icon = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Bitmap24;
                FileType = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Description;
                if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].HasSubtext)
                    FileExt = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Subtext;
                else
                    FileExt = "";
            }
            else
            {
                Icon = FileInfoCombonents.FilesInformation["."].Bitmap24;
                FileType = file.Extension.Length > 1 ? file.Extension.ToLower().Substring(1).ToUpper() + " File" : "File";
                if (file.Extension.Length > 1 && file.Extension.Length < 6)
                    FileExt = file.Extension.ToLower().Substring(1);
                else
                    FileExt = "";
            }
            FileSize = Functions.FileSize(file.Length, 2);
            DateModified = file.LastWriteTime.ToString();
        }

        public MainListView(DirectoryInfo directory)
        {
            File = directory;
            FullPath = directory.FullName;
            FileName = directory.Name;
            Icon = FileInfoCombonents.FilesInformation["directory"].Bitmap24;
            FileType = "Directory";
            FileSize = "";
            FileExt = "";
            DateModified = directory.LastWriteTimeUtc.ToString();
        }

        public MainListView(DriveInfo drive)
        {
            File = null;
            FullPath = drive.Name;
            Drive = drive;
            if (drive.IsReady)
            {
                FileName = drive.VolumeLabel + " (" + drive.Name + ")";
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
            FileExt = "";
        }

        ContextMenu menu;
        public ContextMenu Menu
        {
            get { return menu; }
            set { menu = value; }
        }

        FileSystemInfo file;
        public FileSystemInfo File
        {
            get { return file; }
            set { file = value; }
        }

        public string FullPath;

        DriveInfo drive;
        public DriveInfo Drive
        {
            get { return drive; }
            set { drive = value; }
        }

        private String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private String file_extention;
        public string FileExt
        {
            get { return file_extention; }
            set { file_extention = value; }
        }

        private BitmapImage img_source;
        public BitmapImage Icon
        {
            get { return img_source; }
            set { img_source = value; }
        }

        private String file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }

        private String file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }

        private String file_date;
        public string DateModified
        {
            get { return file_date; }
            set { file_date = value; }
        }
    }
}





//private ListViewItem MakeGridForFile(FileInfo file)
//{
//    ListViewItem Item = new ListViewItem();
//    //Item.HorizontalAlignment = HorizontalAlignment.Stretch;
//    //Item.VerticalAlignment = VerticalAlignment.Stretch;
//    Item.ContextMenuOpening += File_ContextMenuCreator;
//    Item.PreviewMouseDoubleClick += Element_DoubleClick;
//    Item.ContentStringFormat = file.FullName;
//    Item.ContextMenu = new System.Windows.Controls.ContextMenu();
//    Item.Tag = file;
//    if (ShowImages)
//    {
//        int file_index = Functions.GetFileIndex(file.Extension);
//        System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
//        img1.Height = img1.Width = 64;
//        img1.HorizontalAlignment = HorizontalAlignment.Left;
//        //img1.VerticalAlignment = VerticalAlignment.Stretch;
//        img1.Source = FileInformation.file_bitmap_64[file_index];
//        if (ShowHiddenFiles && file.Attributes.HasFlag(FileAttributes.Hidden))
//            img1.Opacity = 0.6;
//        TextBlock name = new TextBlock();
//        name.HorizontalAlignment = HorizontalAlignment.Left;
//        if (file.Attributes.HasFlag(FileAttributes.Compressed))
//            name.Foreground = System.Windows.Media.Brushes.RoyalBlue;
//        else if (file.Attributes.HasFlag(FileAttributes.Encrypted))
//            name.Foreground = System.Windows.Media.Brushes.Green;
//        //name.VerticalAlignment = VerticalAlignment.Stretch;
//        name.Text = file.Name + "\r" + Functions.FileSize(file.Length) + "\r" + Functions.GetFileType(file_index, file.Extension);
//        name.Margin = name_thickness;
//        name.TextTrimming = TextTrimming.CharacterEllipsis;
//        name.TextAlignment=TextAlignment.Left;
//        name.LineHeight = 20;
//        //TextBlock size = new TextBlock();
//        //size.Text = Functions.File_Size(file.Length);
//        //size.HorizontalAlignment = HorizontalAlignment.Left;
//        //size.VerticalAlignment = VerticalAlignment.Top;
//        //size.Margin = size_thickness;
//        //size.TextTrimming = TextTrimming.CharacterEllipsis;
//        //TextBlock type = new TextBlock();
//        //type.Text = Functions.GetFileType(file_index, file.Extension);
//        //type.HorizontalAlignment = HorizontalAlignment.Left;
//        //type.VerticalAlignment = VerticalAlignment.Top;
//        //type.Margin = type_thickness;
//        //type.TextTrimming = TextTrimming.CharacterEllipsis;
//        Grid Grid1 = new Grid();
//        //Grid1.HorizontalAlignment = HorizontalAlignment.Stretch;
//        Grid1.Children.Add(img1);
//        Grid1.Children.Add(name);
//        //Grid1.Children.Add(type);
//        //Grid1.Children.Add(size);
//        Item.Content = Grid1;
//    }
//    else
//        Item.Content = file.Name;
//    return Item;
//}

//private ListViewItem MakeGridForDirectory(DirectoryInfo folder)
//{
//    ListViewItem Item = new ListViewItem();
//    //Item.HorizontalAlignment = HorizontalAlignment.Stretch;
//    //Item.VerticalAlignment = VerticalAlignment.Stretch;
//    Item.ContextMenuOpening += Directory_ContextMenuCreator;
//    Item.PreviewMouseDoubleClick += Element_DoubleClick;
//    Item.ContentStringFormat = folder.FullName;
//    Item.ContextMenu = new System.Windows.Controls.ContextMenu();
//    if (ShowImages)
//    {
//        System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
//        img1.Height = img1.Width = 64;
//        img1.HorizontalAlignment = HorizontalAlignment.Left;
//        //img1.VerticalAlignment = VerticalAlignment.Stretch;
//        img1.Source = FileInformation.file_bitmap_64[FileInformation.file_bitmap_64.Count - 2];
//        if (ShowHiddenFiles && folder.Attributes.HasFlag(FileAttributes.Hidden))
//            img1.Opacity = 0.5;
//        TextBlock text = new TextBlock();
//        text.Text = folder.Name;
//        if (folder.Attributes.HasFlag(FileAttributes.Compressed))
//            text.Foreground = System.Windows.Media.Brushes.Crimson;
//        else if (folder.Attributes.HasFlag(FileAttributes.Encrypted))
//            text.Foreground = System.Windows.Media.Brushes.Green;
//        text.Margin = folder_thickness;
//        //text.HorizontalAlignment = HorizontalAlignment.Stretch;
//        //text.VerticalAlignment = VerticalAlignment.Stretch;
//        text.TextTrimming = TextTrimming.CharacterEllipsis;
//        Grid Grid1 = new Grid();
//        //Grid1.HorizontalAlignment = HorizontalAlignment.Stretch;
//        Grid1.Children.Add(img1);
//        Grid1.Children.Add(text);
//        Item.Content = Grid1;
//    }
//    else
//        Item.Content = folder.Name;
//    Item.Tag = folder;
//    return Item;
//}

//private ListViewItem MakeGridForDrive(string path)
//{
//    DriveInfo drive = new DriveInfo(path);
//    ListViewItem Item1 = new ListViewItem();
//    Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
//    Item1.VerticalAlignment = VerticalAlignment.Stretch;
//    Item1.ContextMenu = MakeDriveContextMenu(drive);
//    Item1.PreviewMouseDoubleClick += Element_DoubleClick;
//    Item1.ContentStringFormat = drive.Name;
//    Item1.Tag = drive;
//    if (ShowImages)
//    {
//        System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
//        img1.Height = img1.Width = 64;
//        img1.HorizontalAlignment = HorizontalAlignment.Left;
//        img1.VerticalAlignment = VerticalAlignment.Stretch;
//        img1.Source = new BitmapImage(new Uri(Functions.GetDriveImage(path, StandardImageSource + "64/")));
//        TextBlock text = new TextBlock();
//        text.Text = drive.Name;
//        text.Margin = name_thickness;
//        text.HorizontalAlignment = HorizontalAlignment.Left;
//        text.VerticalAlignment = VerticalAlignment.Top;
//        text.TextTrimming = TextTrimming.CharacterEllipsis;
//        Grid Grid1 = new Grid();
//        Grid1.HorizontalAlignment = HorizontalAlignment.Stretch;
//        Grid1.Children.Add(img1);
//        Grid1.Children.Add(text);
//        if (drive.IsReady)
//        {
//            text.VerticalAlignment = VerticalAlignment.Top;
//            Rectangle drive_container = new Rectangle();
//            drive_container.Margin = new Thickness(80, 25, 0, 20);
//            drive_container.Stroke = Brushes.Gray;
//            drive_container.Fill = new LinearGradientBrush(Color.FromRgb(230, 230, 230), Color.FromRgb(230, 230, 230), 30);
//            drive_container.Width = 350;
//            Rectangle drive_size = new Rectangle();
//            drive_size.HorizontalAlignment = HorizontalAlignment.Left;
//            drive_size.Margin = new Thickness(81, 26, 0, 21);
//            if (100 - (drive.TotalFreeSpace * 100) / drive.TotalSize > 90)
//                drive_size.Fill = new LinearGradientBrush(Color.FromRgb(218, 38, 38), Color.FromRgb(220, 40, 40), 30);
//            else
//            drive_size.Fill = new LinearGradientBrush(Color.FromRgb(38, 160, 218), Color.FromRgb(40, 160, 220), 30);
//            double size1 = drive_container.Width - (drive.TotalFreeSpace * drive_container.Width) / drive.TotalSize - 2;
//            drive_size.Width = size1 < 0 ? 0 : size1;
//            TextBlock size = new TextBlock();
//            size.Text = Functions.FileSize(drive.TotalFreeSpace) + " free of " + Functions.FileSize(drive.TotalSize);
//            size.Foreground = System.Windows.Media.Brushes.DarkGray;
//            size.Margin = type_thickness;
//            size.HorizontalAlignment = HorizontalAlignment.Left;
//            size.VerticalAlignment = VerticalAlignment.Top;
//            size.TextTrimming = TextTrimming.CharacterEllipsis;
//            Grid1.Children.Add(drive_container);
//            Grid1.Children.Add(drive_size);
//            Grid1.Children.Add(size);
//        }
//        Item1.Content = Grid1;
//    }
//    else
//        Item1.Content = drive.Name;
//    return Item1;
//}

//private void Navigation(string path, bool add_to_back, bool close)
//{
//    try
//    {
//        fake = true;
//        Address1.Items.Clear();
//        fake = false;
//        progressbar.Visibility = Visibility.Visible;
//        //show_progress = new BackgroundWorker();
//        //show_progress.WorkerReportsProgress = true;
//        //show_progress.DoWork += show_progress_DoWork;
//        //show_progress.ProgressChanged += show_progress_ProgressChanged;
//        //show_progress.WorkerSupportsCancellation = true;
//        //stop_progress = false;
//        //show_progress.RunWorkerAsync();
//        if (path == "Computer")
//        {
//            var drives = DriveInfo.GetDrives();
//            Explorer.Items.Clear();
//            foreach (var i in drives)
//                Explorer.Items.Add(MakeGridForDrive(i.Name));
//        }
//        else
//        {
//            DirectoryInfo Current_Directory = new DirectoryInfo(path);
//            var subdirectories = Current_Directory.GetDirectories();
//            var files = Current_Directory.GetFiles();
//            items_count = files.Length + subdirectories.Length + 4;
//            Explorer.Items.Clear();
//            //long n = DateTime.Now.Ticks;
//            for (int i = 0; i < subdirectories.Length; i++)
//                if (ShowHiddenFiles || (!ShowHiddenFiles && !subdirectories[i].Attributes.HasFlag(FileAttributes.Hidden)))
//                {
//                    Explorer.Items.Add(MakeGridForDirectory(subdirectories[i]));
//                    //show_progress.ReportProgress(i);
//                }
//            for (int i = 0; i < files.Length; i++)
//                if (ShowHiddenFiles || (!ShowHiddenFiles && !files[i].Attributes.HasFlag(FileAttributes.Hidden)))
//                {
//                    Explorer.Items.Add(MakeGridForFile(files[i]));
//                    //show_progress.ReportProgress(subdirectories.Length + i);
//                }
//            count.Text = Explorer.Items.Count + (Explorer.Items.Count > 1 ? " Items" : " Item");
//            //MessageBox.Show((DateTime.Now.Ticks - n).ToString());
//            if (Explorer.Items.Count > 0)
//                Explorer.ScrollIntoView(Explorer.Items[0]);
//            //show_progress.ReportProgress(subdirectories.Length + files.Length + 1);
//            if (back.Count < 2)
//                Back.IsEnabled = false;
//            else
//                Back.IsEnabled = true;
//            //show_progress.ReportProgress(subdirectories.Length + files.Length + 1);
//            if (to_copy_files != null && to_copy_files.Count > 0)
//                paste_context_menu.IsEnabled = true;
//            else
//                paste_context_menu.IsEnabled = false;
//            //show_progress.ReportProgress(subdirectories.Length + files.Length + 1);
//        }
//        if (Address.Text != path)
//        {
//            this.path = path;
//            Address.Text = path;
//            if (add_to_back)
//                back.Push(this.path);
//            backMenu.Items.Insert(0, this.path);
//        }
//        progressbar.Visibility = Visibility.Hidden;
//        stop_progress = true;
//    }
//    catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); if (close) Close(); }
//}


//private ContextMenu MakeFileContextMenu(int file_index)
//{
//    if (file_context_menu == null)
//    {
//        file_context_menu = new ContextMenu();
//        MenuItem menu_item = new MenuItem();
//        menu_item.Header = "Open";
//        menu_item.Click += Open_Click;
//        file_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Open with";
//        menu_item.Click += OpenInNewWindow_Click;
//        file_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Preview";
//        menu_item.Click += Preview_Click;
//        file_context_menu.Items.Add(menu_item);
//        file_context_menu.Items.Add(new Separator());
//        menu_item = new MenuItem();
//        menu_item.Header = "Cut";
//        menu_item.Click += Cut_Click;
//        file_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Copy";
//        menu_item.Click += Copy_Click;
//        file_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Duplicate";
//        menu_item.Click += Duplicate_Click;
//        file_context_menu.Items.Add(menu_item);
//        file_context_menu.Items.Add(new Separator());
//        menu_item = new MenuItem();
//        menu_item.Header = "Create shortcut";
//        menu_item.Click += CreateShortcut;
//        file_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Delete";
//        menu_item.Click += Delete_Click;
//        file_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Rename";
//        menu_item.Click += RenameFile_Click;
//        file_context_menu.Items.Add(menu_item);
//        file_context_menu.Items.Add(new Separator());
//        menu_item = new MenuItem();
//        menu_item.Header = "Properties";
//        menu_item.Click += Properties_Click;
//        file_context_menu.Items.Add(menu_item);
//    }
//    return file_context_menu;
//}

//private void MakeDirectoryContextMenu()
//{
//    if (directory_context_menu == null)
//    {
//        directory_context_menu = new ContextMenu();
//        MenuItem menu_item = new MenuItem();
//        menu_item.Header = "Open";
//        menu_item.Click += Open_Click;
//        directory_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Open in new window";
//        menu_item.Click += OpenInNewWindow_Click;
//        directory_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Preview";
//        menu_item.Click += Preview_Click;
//        directory_context_menu.Items.Add(menu_item);
//        directory_context_menu.Items.Add(new Separator());
//        menu_item = new MenuItem();
//        menu_item.Header = "Cut";
//        menu_item.Click += Cut_Click;
//        directory_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Copy";
//        menu_item.Click += Copy_Click;
//        directory_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Duplicate";
//        menu_item.Click += Duplicate_Click;
//        directory_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Paste in this location";
//        menu_item.IsEnabled = false;
//        menu_item.Click += PasteIn_Click;
//        directory_context_menu.Items.Add(menu_item);
//        directory_context_menu.Items.Add(new Separator());
//        menu_item = new MenuItem();
//        menu_item.Header = "Create shortcut";
//        menu_item.Click += CreateShortcut;
//        directory_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Delete";
//        menu_item.Click += Delete_Click;
//        directory_context_menu.Items.Add(menu_item);
//        menu_item = new MenuItem();
//        menu_item.Header = "Rename";
//        menu_item.Click += RenameFile_Click;
//        directory_context_menu.Items.Add(menu_item);
//        directory_context_menu.Items.Add(new Separator());
//        menu_item = new MenuItem();
//        menu_item.Header = "Properties";
//        menu_item.Click += Properties_Click;
//        directory_context_menu.Items.Add(menu_item);
//    }
//}




//private void CopyTo_Click(object sender, RoutedEventArgs e)
//{
//    string copy_to = new Folder_Picker("Select directory to copy to", true).ShowThisDialog();
//    if (copy_to != null)
//    {
//        List<FileOperationItem> to_copy = new List<FileOperationItem>();
//        Functions.AddAllFiles(((ListViewItem)Explorer.SelectedItem).ContentStringFormat, "", false, to_copy);
//        File_Operation window = new File_Operation(to_copy);
//        window.Show(copy_to);
//        Refresh();
//    }
//}



//private void CollectSystemInformation()
//{
//    //ManagementObjectSearcher searcher;
//    //computer_Name = System.Environment.MachineName;
//    //operating_System = System.Environment.OSVersion.Clone().ToString() + " " + System.Environment.Version.ToString();
//    //searcher = new ManagementObjectSearcher("select * from Win32_Processor");
//    //CPU = Environment.CommandLine;
//    ////foreach (var share in searcher.Get())
//    ////{
//    ////    CPU = share["Name"].ToString();
//    ////    break;
//    ////}
//    //searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
//    //long memory_size = 0;
//    //foreach (var share in searcher.Get())
//    //{
//    //    long tmp;
//    //    long.TryParse(share.Properties["Capacity"].Value.ToString(), out tmp);
//    //    memory_size = memory_size + tmp;
//    //}
//    //Memory_Size = Functions.File_Size(memory_size) + " Memory";
//}