using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace File_Explorer
{
    ///<summary>
    ///Interaction logic for MainWindow.xaml
    ///</summary>
    public partial class MainWindow : Window
    {
        ContextMenu directory_context_menu, file_context_menu;
        BackgroundWorker b1, b2;
        public static List<FileElement> to_copy_files;
        static public bool go_to_path = false;
        public static string to_go_path = "";
        bool hiddenfiles = false, show_image = true, fake = false;
        public static bool is_paste = false;
        Stack<string> back, front;
        string directory_address = "Computer", to_select_file = null;
        public static string StandardImageSource = "pack://application:,,,/File Explorer;component/Images/Icons/", systemDrive = "";
        static string computer_Name, operating_System, CPU = "!", Memory_Size;
        Thickness name_thickness = new Thickness(80, 5, 0, 0), size_thickness = new Thickness(80, 25, 0, 0), type_thickness = new Thickness(80, 45, 0, 0), folder_thickness = new Thickness(80, 25, 0, 0);

        string path
        {
            get { return directory_address; }
            set
            {
                //Up.IsEnabled = !(value == "Computer");
                directory_address = value;
            }
        }

        public MainWindow()
        {
            Initializer();
            MessageBox.Show("--Arabic (العربية)--\n**اخلاء مسؤلية**\nهذا البرنامج لم يصدر بعد وهو في نسخة تجريبية لذلك يجب\nأن تكون حذرا في التعامل معه وخاصة خاصية 'الحذف' التي\nقد تقوم وعن غير قصد بحذف احد ملفاتك الضرورية أو الاضرار\nبجهازك.\nيمكنك الاستمتاع  باستعمال كامل الميزات الآخرى ولكن يجب ان تكون حذرا جدا.\n\nالرجاء عدم استخدام خاصية الحذف.\n\n--English (English)--\n**Disclaimer**\nThis app is in alpha version, so be very careful using it\nespecially delete Function, it may delete some of your\nimportant files or damage your system.\nyou can ingoye using all other features, but you should be very careful.\n\nplease don't use 'delete' function.", "FILE MANAGER", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
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

        private void Initializer()
        {
            InitializeComponent();
            if (Process.GetCurrentProcess().StartInfo.Verb == "runas")
                Title = Title + " (Administrator)";
            back = new Stack<string>();
            front = new Stack<string>();
            if (FileInformation.file_bitmap.Count == 0)
                foreach (var i in FileInformation.file_image)
                    FileInformation.file_bitmap.Add(new BitmapImage(new Uri(StandardImageSource + i)));
        }

        private ListViewItem MakeGridForFile(FileInfo file)
        {
            ListViewItem Item1 = new ListViewItem();
            Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
            Item1.VerticalAlignment = VerticalAlignment.Stretch;
            Item1.ContextMenu = MakeFileContextMenu(0/*file_index*/);
            Item1.PreviewMouseDoubleClick += Element_DoubleClick;
            Item1.ContentStringFormat = file.FullName;
            Item1.Tag = file;
            Item1.Name = "f";
            return Item1;
        }

        private void AddItemsToFileGrid()
        {
            foreach (ListViewItem i in Explorer.Items)
                if (i.Name == "f")
                    if (show_image)
                    {
                        int file_index = Functions.GetFileIndex(((FileInfo)i.Tag).Extension);
                        System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
                        img1.Height = img1.Width = 64;
                        img1.HorizontalAlignment = HorizontalAlignment.Left;
                        img1.VerticalAlignment = VerticalAlignment.Stretch;
                        img1.Source = FileInformation.file_bitmap[file_index];
                        if (hiddenfiles && ((FileInfo)i.Tag).Attributes.HasFlag(FileAttributes.Hidden))
                            img1.Opacity = 0.5;
                        TextBlock name = new TextBlock();
                        name.Text = ((FileInfo)i.Tag).Name;
                        name.HorizontalAlignment = HorizontalAlignment.Left;
                        if (((FileInfo)i.Tag).Attributes.HasFlag(FileAttributes.Compressed))
                            name.Foreground = System.Windows.Media.Brushes.RoyalBlue;
                        else if (((FileInfo)i.Tag).Attributes.HasFlag(FileAttributes.Encrypted))
                            name.Foreground = System.Windows.Media.Brushes.Green;
                        name.VerticalAlignment = VerticalAlignment.Stretch;
                        name.Margin = name_thickness;
                        name.TextTrimming = TextTrimming.CharacterEllipsis;
                        TextBlock size = new TextBlock();
                        size.Text = Functions.File_Size(((FileInfo)i.Tag).Length);
                        size.HorizontalAlignment = HorizontalAlignment.Left;
                        size.VerticalAlignment = VerticalAlignment.Top;
                        size.Margin = size_thickness;
                        size.TextTrimming = TextTrimming.CharacterEllipsis;
                        TextBlock type = new TextBlock();
                        type.Text = Functions.GetFileType(file_index, ((FileInfo)i.Tag).Extension);
                        type.HorizontalAlignment = HorizontalAlignment.Left;
                        type.VerticalAlignment = VerticalAlignment.Top;
                        type.Margin = type_thickness;
                        type.TextTrimming = TextTrimming.CharacterEllipsis;
                        Grid Grid1 = new Grid();
                        Grid1.HorizontalAlignment = HorizontalAlignment.Stretch;
                        Grid1.Children.Add(img1);
                        Grid1.Children.Add(name);
                        Grid1.Children.Add(type);
                        Grid1.Children.Add(size);
                        i.Content = Grid1;
                    }
                    else
                        i.Content = ((FileInfo)i.Tag).Name;
        }

        private void AddItemsToDirectoryGrid()
        {
            foreach (ListViewItem i in Explorer.Items)
                if (i.Name == "d")
                    if (show_image)
                    {
                        System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
                        img1.Height = img1.Width = 64;
                        img1.HorizontalAlignment = HorizontalAlignment.Left;
                        img1.VerticalAlignment = VerticalAlignment.Stretch;
                        img1.Source = FileInformation.file_bitmap[FileInformation.file_bitmap.Count - 2];
                        if (hiddenfiles && ((DirectoryInfo)i.Tag).Attributes.HasFlag(FileAttributes.Hidden))
                            img1.Opacity = 0.5;
                        TextBlock text = new TextBlock();
                        text.Text = ((DirectoryInfo)i.Tag).Name;
                        if (((DirectoryInfo)i.Tag).Attributes.HasFlag(FileAttributes.Compressed))
                            text.Foreground = System.Windows.Media.Brushes.RoyalBlue;
                        else if (((DirectoryInfo)i.Tag).Attributes.HasFlag(FileAttributes.Encrypted))
                            text.Foreground = System.Windows.Media.Brushes.Green;
                        text.Margin = folder_thickness;
                        text.HorizontalAlignment = HorizontalAlignment.Stretch;
                        text.VerticalAlignment = VerticalAlignment.Stretch;
                        text.TextTrimming = TextTrimming.CharacterEllipsis;
                        Grid Grid1 = new Grid();
                        Grid1.HorizontalAlignment = HorizontalAlignment.Stretch;
                        Grid1.Children.Add(img1);
                        Grid1.Children.Add(text);
                        i.Content = Grid1;
                    }
                    else
                        i.Content = ((DirectoryInfo)i.Tag).Name;
        }

        private ListViewItem MakeGridForDirectory(DirectoryInfo folder)
        {
            ListViewItem Item1 = new ListViewItem();
            Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
            Item1.VerticalAlignment = VerticalAlignment.Stretch;
            Item1.ContextMenu = directory_context_menu;
            Item1.PreviewMouseDoubleClick += Element_DoubleClick;
            Item1.ContentStringFormat = folder.FullName;
            Item1.Name = "d";
            Item1.Tag = folder;
            return Item1;
        }

        private ListViewItem MakeGridForDrive(string path)
        {
            DriveInfo drive = new DriveInfo(path);
            ListViewItem Item1 = new ListViewItem();
            Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
            Item1.VerticalAlignment = VerticalAlignment.Stretch;
            Item1.ContextMenu = MakeDriveContextMenu(drive);
            Item1.PreviewMouseDoubleClick += Element_DoubleClick;
            Item1.ContentStringFormat = drive.Name;
            Item1.Tag = drive;
            if (show_image)
            {
                System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
                img1.Height = img1.Width = 64;
                img1.HorizontalAlignment = HorizontalAlignment.Left;
                img1.VerticalAlignment = VerticalAlignment.Stretch;
                img1.Source = new BitmapImage(new Uri(Functions.GetDriveImage(path, StandardImageSource)));
                TextBlock text = new TextBlock();
                text.Text = drive.Name;
                text.Margin = name_thickness;
                text.HorizontalAlignment = HorizontalAlignment.Left;
                text.VerticalAlignment = VerticalAlignment.Top;
                text.TextTrimming = TextTrimming.CharacterEllipsis;
                Grid Grid1 = new Grid();
                Grid1.HorizontalAlignment = HorizontalAlignment.Stretch;
                Grid1.Children.Add(img1);
                Grid1.Children.Add(text);
                if (drive.IsReady)
                {
                    text.VerticalAlignment = VerticalAlignment.Top;
                    Rectangle drive_container = new Rectangle();
                    drive_container.Margin = new Thickness(80, 25, 0, 20);
                    drive_container.Stroke = Brushes.Gray;
                    drive_container.Fill = new LinearGradientBrush(Color.FromRgb(230, 230, 230), Color.FromRgb(230, 230, 230), 30);
                    drive_container.Width = 350;
                    Rectangle drive_size = new Rectangle();
                    drive_size.HorizontalAlignment = HorizontalAlignment.Left;
                    drive_size.Margin = new Thickness(81, 26, 0, 21);
                    if (100 - (drive.TotalFreeSpace * 100) / drive.TotalSize > 90)
                        drive_size.Fill = new LinearGradientBrush(Color.FromRgb(218, 38, 38), Color.FromRgb(220, 40, 40), 30);
                    else
                        drive_size.Fill = new LinearGradientBrush(Color.FromRgb(38, 160, 218), Color.FromRgb(40, 160, 220), 30);
                    drive_size.Width = drive_container.Width - (drive.TotalFreeSpace * drive_container.Width) / drive.TotalSize - 2;
                    TextBlock size = new TextBlock();
                    size.Text = Functions.File_Size(drive.TotalFreeSpace) + " free of " + Functions.File_Size(drive.TotalSize);
                    size.Foreground = System.Windows.Media.Brushes.DarkGray;
                    size.Margin = type_thickness;
                    size.HorizontalAlignment = HorizontalAlignment.Left;
                    size.VerticalAlignment = VerticalAlignment.Top;
                    size.TextTrimming = TextTrimming.CharacterEllipsis;
                    Grid1.Children.Add(drive_container);
                    Grid1.Children.Add(drive_size);
                    Grid1.Children.Add(size);
                }
                Item1.Content = Grid1;
            }
            else
                Item1.Content = drive.Name;
            return Item1;
        }

        private void Navigation(string path, bool add_to_back, bool close)
        {
            try
            {
                if (b1 != null && b1.IsBusy)
                    b1.CancelAsync();
                if (b2 != null && b2.IsBusy)
                    b2.CancelAsync();
                fake = true;
                Address1.Items.Clear();
                fake = false;
                if (path == "Computer")
                {
                    var drives = DriveInfo.GetDrives();
                    Explorer.Items.Clear();
                    foreach (var i in drives)
                        Explorer.Items.Add(MakeGridForDrive(i.Name));
                }
                else
                {
                    DirectoryInfo directory = new DirectoryInfo(path);
                    var subdirectories = directory.GetDirectories();
                    var files = directory.GetFiles();
                    Explorer.Items.Clear();
                    if (to_copy_files != null && to_copy_files.Count > 0)
                        ((MenuItem)directory_context_menu.Items[7]).IsEnabled = true;
                    else
                        ((MenuItem)directory_context_menu.Items[7]).IsEnabled = false;
                    foreach (var i in subdirectories)
                        if (hiddenfiles || (!hiddenfiles && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                            Explorer.Items.Add(MakeGridForDirectory(i));
                    foreach (var i in files)
                        if (hiddenfiles || (!hiddenfiles && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                            Explorer.Items.Add(MakeGridForFile(i));
                    b1 = new BackgroundWorker();
                    b1.WorkerReportsProgress = true;
                    b1.DoWork += b1_DoWork;
                    b1.ProgressChanged += b1_ProgressChanged;
                    b1.WorkerSupportsCancellation = true;
                    b1.RunWorkerAsync();
                    b2 = new BackgroundWorker();
                    b2.WorkerReportsProgress = true;
                    b2.DoWork += b2_DoWork;
                    b2.ProgressChanged += b2_ProgressChanged;
                    b2.WorkerSupportsCancellation = true;
                    b2.RunWorkerAsync();
                }
                count.Text = Explorer.Items.Count + " Item(s)";
                if (Address.Text != path)
                {
                    this.path = path;
                    Address.Text = path;
                    if (add_to_back)
                        back.Push(this.path);
                    backMenu.Items.Insert(0, this.path);
                }
                if (Explorer.Items.Count > 0)
                    Explorer.ScrollIntoView(Explorer.Items[0]);
                if (back.Count < 2)
                    Back.IsEnabled = false;
                else
                    Back.IsEnabled = true;
                if (to_copy_files != null && to_copy_files.Count > 0)
                    paste_context_menu.IsEnabled = true;
                else
                    paste_context_menu.IsEnabled = false;
            }
            catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); if (close) Close(); }
        }

        void b1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            AddItemsToFileGrid();
        }

        void b1_DoWork(object sender, DoWorkEventArgs e)
        {
            b1.ReportProgress(0);
        }

        void b2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            AddItemsToDirectoryGrid();
        }

        void b2_DoWork(object sender, DoWorkEventArgs e)
        {
            b2.ReportProgress(0);
        }

        private void Main_Window_Loaded(object sender, RoutedEventArgs e)
        {
            systemDrive = new DirectoryInfo(Environment.SystemDirectory).Root.FullName;
            MakeDirectoryContextMenu();
            if (path == "Computer")
            {
                back.Push(this.path);
                backMenu.Items.Insert(0, this.path);
            }
            Navigation(path, true, true); ;
            if (CPU.Equals("!"))
                CollectSystemInformation();
            name.Text = computer_Name;
            type.Text = operating_System;
            size.Text = CPU;
            data_Modified.Text = Memory_Size;
            if (to_select_file != null)
                SelectItem(to_select_file);
            string f = Process.GetCurrentProcess().StartInfo.Verb;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("File Manager v0.5.1-alpha.3\nby: mohammad.diab93@outlook.com", "FILE MANAGER", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        private void New_Window(object sender, RoutedEventArgs e)
        {
            new MainWindow("Computer").Show();
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            if (path != "Computer")
            {
                if (to_copy_files == null)
                    to_copy_files = new List<FileElement>();
                else
                {
                    if (!is_paste)
                        if (new My_MessageBox("Append file to list", "Do you want to append files to the list?", "", "", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/list.png")), null/*buttons*/, null/* button_actions*/).ShowThisDialog() == MyMessageBoxResult.No)
                            to_copy_files.Clear();
                        else
                        {
                            to_copy_files.Clear();
                            is_paste = false;
                        }
                    else
                        to_copy_files.Clear();
                }
                foreach (var i in Explorer.SelectedItems)
                    if (File.Exists(((ListViewItem)i).ContentStringFormat))
                        to_copy_files.Add(new FileElement(((ListViewItem)i).ContentStringFormat, ((ListViewItem)i).ContentStringFormat.Remove(0, path.Length), true));
                    else
                        Functions.AddAllFiles((((ListViewItem)i).ContentStringFormat), path, true, to_copy_files);
            }
            else
                new My_MessageBox("Cut", "You can not move any item from here!", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (path != "Computer")
            {
                if (to_copy_files == null)
                    to_copy_files = new List<FileElement>();
                else
                {
                    if (!is_paste)
                        if (new My_MessageBox("Append file to list", "Do you want to append files to the list?", "", "", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/list.png")), null, null).ShowThisDialog() == MyMessageBoxResult.No)
                            to_copy_files.Clear();
                        else
                        {
                            to_copy_files.Clear();
                            is_paste = false;
                        }
                    else
                        to_copy_files.Clear();
                }
                foreach (var i in Explorer.SelectedItems)
                    if (File.Exists(((ListViewItem)i).ContentStringFormat))
                        to_copy_files.Add(new FileElement(((ListViewItem)i).ContentStringFormat, ((ListViewItem)i).ContentStringFormat.Remove(0, path.Length), false));
                    else
                        Functions.AddAllFiles((((ListViewItem)i).ContentStringFormat), path, false, to_copy_files);
            }
            else
                new My_MessageBox("Copy", "You can not copy any item from here!", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            if (to_copy_files != null && to_copy_files.Count > 0)
                if (path != "Computer")
                {
                    File_Operation window = new File_Operation();
                    window.Show(path);
                }
                else
                    new My_MessageBox("Paste", "You can not paste any item here!", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
            else
                new My_MessageBox("Paste", "There is not any item to paste it!", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void NewWindowAdmin_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo PSI = new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath);
            PSI.Verb = "runas";
            try
            { System.Diagnostics.Process.Start(PSI); }
            catch (Exception ex)
            { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            if (path != "Computer")
            {
                string new_name = null;
                new_name = new Rename("new directory").ShowthisDialog();
                NewDirectory(new_name);
                Refresh();
            }
            else
                new My_MessageBox("Create new directory", "You can not create directory here!", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowHiddenFiles_Click(object sender, RoutedEventArgs e)
        {
            hiddenfiles = !hiddenfiles;
            Refresh();
        }

        private void ShowImage_Click(object sender, RoutedEventArgs e)
        {
            show_image = !show_image;
            Refresh();
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
            new Renamer().ShowDialog();
        }

        private void SystemMonitor_Click(object sender, RoutedEventArgs e)
        {
            new System_Monitor().ShowDialog();
        }

        private void EmptyDirectoriesRemover_Click(object sender, RoutedEventArgs e)
        {
            (new Empty_Folder_Remover()).Show();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            Explorer.SelectAll();
        }

        private void HH(object sender, RoutedEventArgs e)
        {
            string p = new Folder_Picker().ShowThisDialog();
            string c = p;
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

        private void Explorer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //Explorer.UnselectAll();
        }

        private void CollectSystemInformation()
        {
            //ManagementObjectSearcher searcher;
            //computer_Name = System.Environment.MachineName;
            //operating_System = System.Environment.OSVersion.Clone().ToString() + " " + System.Environment.Version.ToString();
            //searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            //CPU = Environment.CommandLine;
            //foreach (var share in searcher.Get())
            //{
            //    CPU = share["Name"].ToString();
            //    break;
            //}
            //searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
            //long memory_size = 0;
            //foreach (var share in searcher.Get())
            //{
            //    long tmp;
            //    long.TryParse(share.Properties["Capacity"].Value.ToString(), out tmp);
            //    memory_size = memory_size + tmp;
            //}
            //Memory_Size = Functions.File_Size(memory_size) + " Memory";
        }

        private void ViewFolderInfo(string path)
        {
            DirectoryInfo view_directory = new DirectoryInfo(path);
            int sub_dirs_count = 0, sub_files_count = 0;
            img.Source = FileInformation.file_bitmap[FileInformation.file_bitmap.Count - 2];
            name.Text = view_directory.Name;
            type.Text = "Directory";
            try
            {
                sub_dirs_count = Directory.GetDirectories(view_directory.FullName).Length;
                sub_files_count = Directory.GetFiles(view_directory.FullName).Length;
            }
            catch (Exception ex)
            { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            SetDefualt();
            size_title.Text = "Contains: ";
            size.Text = sub_dirs_count + " Folders & " + sub_files_count + " Files";
            data_Modified.Text = view_directory.LastWriteTime.ToString();
        }

        private void ViewFileInfo(string path)
        {
            FileInfo view_file = new FileInfo(path);
            int file_index = Functions.GetFileIndex(view_file.Extension);
            if (FileInformation.file_type[file_index] == FileCategory.Image)
            {
                try
                {
                    img.Source = new BitmapImage(new Uri(view_file.FullName));
                    //if (new Bitmap(view_file.FullName).Height < 200 || new Bitmap(view_file.FullName).Height < 200)
                    //    img.Stretch = Stretch.None;
                }
                catch (Exception) { img.Source = FileInformation.file_bitmap[file_index]; }
            }
            else
                img.Source = FileInformation.file_bitmap[file_index];
            SetDefualt();
            name.Text = view_file.Name;
            type.Text = Functions.GetFileType(file_index, view_file.Extension);
            size.Text = Functions.File_Size(view_file.Length);
            data_Modified.Text = view_file.LastWriteTime.ToString();
        }

        private void ViewDriveInfo(string path)
        {
            DriveInfo drive = new DriveInfo(path);
            img.Source = new BitmapImage(new Uri(Functions.GetDriveImage(path, StandardImageSource)));
            if (drive.IsReady)
            {
                name.Text = drive.VolumeLabel + " (" + drive.Name + ")";
                type_title.Text = "Format: ";
                type.Text = drive.DriveFormat + " File System";
                size_title.Text = "Disk Size: ";
                size.Text = Functions.File_Size(drive.TotalSize) + " (" + Functions.File_Size(drive.AvailableFreeSpace) + " Free)";
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
            img.Source = new BitmapImage(new Uri(StandardImageSource + "multi-selection.png"));
            name.Text = "Multi items have been selected";
            bool change = false;
            string file_type = "";
            long total_files_size = 0;
            int k = 0;
            foreach (ListViewItem i in Explorer.SelectedItems)
            {
                if (File.Exists(i.ContentStringFormat))
                {
                    files_count++;
                    total_files_size += ((FileInfo)i.Tag).Length;
                    if (!change)
                    {
                        string tmp = FileInformation.file_description[Functions.GetFileIndex(((FileInfo)i.Tag).Extension)];
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
                type.Text = "Multi Type Selected";
            else
                type.Text = "All Types '" + file_type + "'";
            size_title.Text = "All Files Size: ";
            size.Text = Functions.File_Size(total_files_size);
            data_Modified.Text = "Multi items have been selected";
        }

        private void SetDefualt()
        {
            name_title.Text = "Name: ";
            type_title.Text = "Type: ";
            size_title.Text = "Size: ";
            data_Modified_title.Text = "Data Modified: ";
        }

        private void ViewComputerInfo()
        {
            name.Text = computer_Name;
            type.Text = operating_System;
            size.Text = CPU;
            data_Modified.Text = Memory_Size;
            img.Source = new BitmapImage(new Uri(StandardImageSource + "computer.png"));
        }

        private void SelectItem(string to_select_item)
        {
            foreach (ListViewItem i in Explorer.Items)
                if (i.ContentStringFormat == to_select_item)
                {
                    Explorer.SelectedItem = i;
                    Explorer.ScrollIntoView(i);
                    break;
                }
        }

        private void Go_UP(object sender, RoutedEventArgs e)
        {
            string new_path, old_path = path;
            string[] split_path = path.Split(Functions.spliter, StringSplitOptions.RemoveEmptyEntries);
            if (path != "Computer" || path != string.Empty)
            {
                if (split_path.Length == 1)
                    new_path = "Computer";
                else
                    new_path = new DirectoryInfo(path).Parent.FullName;
                Navigation(new_path, true, false);
                front = new Stack<string>();
                Forword.IsEnabled = false;
                SelectItem(old_path);
            }
        }

        private void Address_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
                Navigation(Address.Text, true, false);
            else if (e.Key == Key.Oem5)
            {
                fake = true;
                Address1.Items.Clear();
                Address1.IsDropDownOpen = true;
                if (Directory.Exists(Address.Text))
                    try
                    {
                        DirectoryInfo n = new DirectoryInfo(Address.Text);
                        foreach (var i in n.GetDirectories())
                            if (hiddenfiles || (!hiddenfiles && !i.Attributes.HasFlag(FileAttributes.Hidden)))
                                Address1.Items.Add(i.FullName);
                    }
                    catch (Exception) { }
                fake = false;
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (back.Count > 1)
            {
                string new_path = back.Pop(), old_path = path;
                front.Push(new_path);
                Navigation(back.Peek(), false, false);
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
                string new_path = front.Pop(), old_path = path;
                back.Push(new_path);
                Navigation(new_path, false, false);
                SelectItem(old_path);
            }
            if (front.Count < 1)
                Forword.IsEnabled = false;
            //View_Items_Basic_Info(new_path, "Folder");
        }

        private void Explorer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Explorer.SelectedItems.Count > 0)
            {
                total_files_size.Text = "";
                selected.Text = "«" + Explorer.SelectedItems.Count + " item(s) selected»";
                if (Explorer.SelectedItems.Count == 1 || this.path == "Computer")
                {
                    string path = ((ListViewItem)Explorer.SelectedItem).ContentStringFormat;
                    if (this.path == "Computer")
                        ViewDriveInfo(path);
                    else
                    {
                        if (File.Exists(path))
                            ViewFileInfo(path);
                        else if (Directory.Exists(path))
                            ViewFolderInfo(path);
                    }
                }
                else if (Explorer.SelectedItems.Count > 1)
                {
                    long total_size = 0;
                    foreach (ListViewItem i in Explorer.SelectedItems)
                        if (File.Exists(i.ContentStringFormat))
                            total_size += ((FileInfo)i.Tag).Length;
                    total_files_size.Text = "Total selected files size: " + Functions.File_Size(total_size);
                    ViewMultiItemsInfo();
                }
            }
            else
                selected.Text = string.Empty;

        }

        private void Explorer_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            go_to_path = false;
            if (e.Key.Equals(Key.Space) && Explorer.SelectedIndex > -1)
                PreviewItem();
            else if (e.Key.Equals(Key.Enter) && Explorer.SelectedIndex > -1)
                OpenFileorDirectory();
            else if (e.Key == Key.Delete)
                DeteteSelectetFiles();
            else if (e.Key == Key.F2)
                if (path == "Computer")
                    RenameDrive_Click(null, null);
                else
                    RenameFile_Click(null, null);
        }

        private void PreviewItem()
        {
            int sub_dir = 0, sub_files = 0;
            string path = ((ListViewItem)Explorer.SelectedItem).ContentStringFormat;
            try
            {
                sub_dir = Directory.GetDirectories(path).Length;
                sub_files = Directory.GetFiles(path).Length;
            }
            catch (Exception) { }
            if (this.path == "Computer")
                new Viewer(path, ItemType.Drive, 0, 0).ShowDialog();
            else if (File.Exists(path))
                new Viewer(path, ItemType.File, 0, 0).ShowDialog();
            else if (Directory.Exists(path))
                new Viewer(path, ItemType.Directory, sub_dir, sub_files).ShowDialog();
            if (go_to_path)
                Navigation(to_go_path, true, false);
        }

        private void DeteteSelectetFiles()
        {
            if (path != "Computer" && Explorer.SelectedItems.Count > 0)
            {
                //List<UnCompleatOperation> un_done = new List<UnCompleatOperation>();
                My_MessageBox messagebox;
                if (Explorer.SelectedItems.Count > 1)
                    messagebox = new My_MessageBox(Explorer.SelectedItems.Count);
                else
                    messagebox = new My_MessageBox((FileSystemInfo)((ListViewItem)Explorer.SelectedItem).Tag);
                if (messagebox.ShowThisDialog() == MyMessageBoxResult.Yes)
                {
                    List<string> delete_files = new List<string>();
                    List<string> delete_dirs = new List<string>();
                    foreach (var i in Explorer.SelectedItems)
                    {
                        if (File.Exists(((ListViewItem)i).ContentStringFormat))
                            delete_files.Add(((ListViewItem)i).ContentStringFormat);
                        else if (Directory.Exists(((ListViewItem)i).ContentStringFormat))
                        {
                            Functions.AddAllFiles(((ListViewItem)i).ContentStringFormat, delete_files);
                            Functions.AddAllDirectories(((ListViewItem)i).ContentStringFormat, delete_dirs);
                            delete_dirs.Add(((ListViewItem)i).ContentStringFormat);
                        }
                    }
                    new File_Operation(delete_files, delete_dirs).Show();
                }
            }
        }

        private void OpenFileorDirectory()
        {
            bool new_window = false;
            if (Explorer.SelectedItems.Count < 20 && Explorer.SelectedItems.Count > 0)
            {
                List<string> paths = new List<string>();
                foreach (ListViewItem i in Explorer.SelectedItems)
                    paths.Add(i.ContentStringFormat);
                if (path == "Computer")
                {
                    foreach (string i in paths)
                        try
                        {
                            if (new_window)
                                new MainWindow(i).Show();
                            else
                            {
                                Navigation(i, true, false);
                                front = new Stack<string>();
                                Forword.IsEnabled = false;
                                new_window = true;
                            }
                        }
                        catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
                }
                else
                {
                    foreach (string i in paths)
                        try
                        {
                            if (File.Exists(i))
                                System.Diagnostics.Process.Start(new ProcessStartInfo(i));
                            else if (Directory.Exists(i))
                            {
                                if (new_window)
                                    new MainWindow(i).Show();
                                else
                                {
                                    Navigation(i, true, false);
                                    front = new Stack<string>();
                                    Forword.IsEnabled = false;
                                    new_window = true;
                                }
                            }
                        }
                        catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
                }
            }
        }

        private void Element_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                OpenFileorDirectory();
                //if (Explorer.SelectedItems.Count == 1)
                //{
                //    string path = ((ListBoxItem)Explorer.SelectedItem).ContentStringFormat;
                //    if (this.path == "Computer")
                //        Navigation(path, true,false);
                //    else
                //        if (File.Exists(path))
                //            System.Diagnostics.Process.Start(new ProcessStartInfo(path));
                //        else if (Directory.Exists(path))
                //            Navigation(path, true,false);
                //}
                //else 

                //if (e.ChangedButton.Equals(MouseButton.Left) && ((Explorer.SelectedIndex > -1 && Explorer.SelectedIndex < directorys.Count) || new_path.Equals(String.Empty)))
                //PSI.Verb = "runas";
                //psi.UseShellExecute = true;
                //files[Explorer.SelectedIndex - privous_Directorys.Count].Open(FileMode.Open);
            }
        }

        private void Refresh(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void Refresh()
        {
            int index = Explorer.SelectedIndex;
            Navigation(path, false, false);
            Explorer.SelectedIndex = index;
            if (index > -1 && index < Explorer.Items.Count)
                Explorer.ScrollIntoView(Explorer.Items[index]);
        }

        private void Surfing_Back(object sender, SelectionChangedEventArgs e)
        {
            Navigation((String)(backMenu.Items[backMenu.SelectedIndex]), true, false);
            //Address.FontFamily = new FontFamily(new Uri("D:\\Windows\\Fonts\\consolai.ttf"), "Consolas");
        }

        private void Refresh(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
                Refresh();
        }

        private void FileCompresser_Click(object sender, RoutedEventArgs e)
        {
            new File_Compresser().Show();
        }

        private ContextMenu MakeDriveContextMenu(DriveInfo drive)
        {
            ContextMenu drive_context_menu = new ContextMenu();
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
                if (to_copy_files != null && to_copy_files.Count > 0)
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
                drive_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Copy to";
                menu_item.Click += CopyTo_Click;
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

        private ContextMenu MakeFileContextMenu(int file_index)
        {
            if (file_context_menu == null)
            {
                file_context_menu = new ContextMenu();
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
                menu_item.Click += CreateShortcut;
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
            return file_context_menu;
        }

        private void MakeDirectoryContextMenu()
        {
            if (directory_context_menu == null)
            {
                directory_context_menu = new ContextMenu();
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
                menu_item.Header = "Paste in this location";
                menu_item.IsEnabled = false;
                menu_item.Click += PasteIn_Click;
                directory_context_menu.Items.Add(menu_item);
                directory_context_menu.Items.Add(new Separator());
                menu_item = new MenuItem();
                menu_item.Header = "Create shortcut";
                menu_item.Click += CreateShortcut;
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

        private void Duplicate_Click(object sender, RoutedEventArgs e)
        {
            List<FileElement> files = new List<FileElement>();
            foreach (ListViewItem i in Explorer.SelectedItems)
                if (File.Exists(i.ContentStringFormat))
                {
                    FileInfo file = new FileInfo(i.ContentStringFormat);
                    string new_file_name = i.ContentStringFormat.Remove(i.ContentStringFormat.Length - file.Extension.Length) + "- Copy" + file.Extension;
                    if (File.Exists(i.ContentStringFormat.Remove(i.ContentStringFormat.Length - file.Extension.Length) + "- Copy" + file.Extension))
                    {
                        int k = 1;
                        while (File.Exists(i.ContentStringFormat.Remove(i.ContentStringFormat.Length - file.Extension.Length) + "- Copy (" + k + ")" + file.Extension)) k++;
                        new_file_name = i.ContentStringFormat.Remove(i.ContentStringFormat.Length - file.Extension.Length) + "- Copy (" + k + ")" + file.Extension;
                    }
                    files.Add(new FileElement(i.ContentStringFormat, new_file_name.Remove(0, path.Length), false));
                }
                else
                {
                    List<FileElement> to_copy = new List<FileElement>();
                    string paste_in = i.ContentStringFormat + "- Copy";
                    if (Directory.Exists(i.ContentStringFormat + "- Copy"))
                    {
                        int k = 1;
                        while (Directory.Exists(i.ContentStringFormat + "- Copy (" + k + ")")) k++;
                        paste_in = i.ContentStringFormat + "- Copy (" + k + ")";
                    }
                    foreach (var j in Directory.GetFiles(i.ContentStringFormat))
                        to_copy.Add(new FileElement(j, j.Remove(0, i.ContentStringFormat.Length), false));
                    foreach (var j in Directory.GetDirectories(i.ContentStringFormat))
                        Functions.AddAllFiles(j, i.ContentStringFormat, false, to_copy);
                    try
                    { Directory.CreateDirectory(paste_in); }
                    catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); return; }
                    File_Operation window = new File_Operation(to_copy);
                    window.Show(paste_in);
                }
            if (files.Count > 0)
            {
                File_Operation window2 = new File_Operation(files);
                window2.Show(path);
            }
            Refresh();
        }

        private void PasteIn_Click(object sender, RoutedEventArgs e)
        {
            File_Operation window = new File_Operation();
            window.Show(((ListViewItem)Explorer.SelectedItem).ContentStringFormat);
        }

        private void RenameFile_Click(object sender, RoutedEventArgs e)
        {
            List<UnCompleatOperation> un_done = new List<UnCompleatOperation>();
            if (path != "Computer")
            {
                string new_name = new Rename(((FileSystemInfo)((ListViewItem)Explorer.SelectedItem).Tag).Name).ShowthisDialog();
                if (new_name != null)
                {
                    foreach (ListViewItem i in Explorer.SelectedItems)
                        if (File.Exists(i.ContentStringFormat))
                            RenameFile(i.ContentStringFormat, new_name, un_done);
                        else
                            RenameDirectory(i.ContentStringFormat, new_name, un_done);
                    while (un_done.Count > 0)
                    {
                        var retry = new File_s_transferring_Error(un_done, "Some items have not renamed successfully.<note: default option is skip>").ShowThisDialog();
                        for (int i = retry.Count - 1; i >= 0; i--)
                        {
                            if (retry[i])
                                if (un_done[i].IsDirectory)
                                    RenameDirectory(un_done[i].files_full_name, new_name, un_done);
                                else
                                    RenameFile(un_done[i].files_full_name, new_name, un_done);
                            un_done.RemoveAt(i);
                        }
                    }
                }
            }
            Refresh();
        }

        private void RenameFile(string file_name, string new_name, List<UnCompleatOperation> un_done)
        {
            try
            {
                FileInfo source_file = new FileInfo(file_name);
                string extention = source_file.Extension;
                int k = 0;
                if (File.Exists(source_file.DirectoryName + "\\" + new_name + extention))
                {
                    k = 1;
                    while (File.Exists(source_file.DirectoryName + "\\" + new_name + " (" + k + ")" + extention)) k++;
                    source_file.MoveTo(source_file.DirectoryName + "\\" + new_name + " (" + k + ")" + extention);
                }
                else
                    source_file.MoveTo(source_file.DirectoryName + "\\" + new_name + extention);
            }
            catch (Exception ex) { un_done.Add(new UnCompleatOperation(file_name, ex.Message, false)); }
        }

        private void RenameDirectory(string dir_name, string new_name, List<UnCompleatOperation> un_done)
        {
            try
            {
                DirectoryInfo source_directory = new DirectoryInfo(dir_name);
                int k = 0;
                if (Directory.Exists(source_directory.Parent.FullName + "\\" + new_name))
                {
                    k = 1;
                    while (Directory.Exists(source_directory.Parent.FullName + "\\" + new_name + " (" + k + ")")) k++;
                    source_directory.MoveTo(source_directory.Parent.FullName + "\\" + new_name + " (" + k + ")");
                }
                else
                    source_directory.MoveTo(source_directory.Parent.FullName + "\\" + new_name);
            }
            catch (Exception ex) { un_done.Add(new UnCompleatOperation(dir_name, ex.Message, true)); }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            DeteteSelectetFiles();
        }

        private void CreateShortcut(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            //new Properties_Window(((ListViewItem)Explorer.SelectedItem).ContentStringFormat).Show();
        }

        private void RenameDrive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string new_name = new Rename(new DriveInfo(((ListViewItem)Explorer.SelectedItem).ContentStringFormat).VolumeLabel).ShowthisDialog();
                if (new_name != null)
                    new DriveInfo(((ListViewItem)Explorer.SelectedItem).ContentStringFormat).VolumeLabel = new_name;
            }
            catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void CopyTo_Click(object sender, RoutedEventArgs e)
        {
            string copy_to = new Folder_Picker("Select directory to copy to", true).ShowThisDialog();
            if (copy_to != null)
            {
                List<FileElement> to_copy = new List<FileElement>();
                Functions.AddAllFiles(((ListViewItem)Explorer.SelectedItem).ContentStringFormat, "", false, to_copy);
                File_Operation window = new File_Operation(to_copy);
                window.Show(copy_to);
                Refresh();
            }
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
                new My_MessageBox("Viewer", "You can not preview more than one item in same time.", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var i in Explorer.SelectedItems)
                    Functions.OpenWith(((ListViewItem)i).ContentStringFormat, Functions.GetItemType(((ListViewItem)i).ContentStringFormat));
            }
            catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileorDirectory();
        }

        private void Address_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!fake)
                Navigation(Address1.SelectedItem.ToString(), true, false);
        }

        private void Explorer_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //Explorer.SelectedIndex = -1;
        }

        private void NewFile(string extension)
        {
            if (path != "Computer")
            {
                string new_name = null;
                new_name = new Rename("new file").ShowthisDialog();
                string file_name = null; ;
                if (new_name != null)
                {
                    if (File.Exists(path + "\\" + new_name + extension))
                    {
                        int i = 1;
                        while (File.Exists(path + "\\" + new_name + " (" + i + ")" + extension)) i++;
                        if (new My_MessageBox("New File", "Do you want to create file '" + new_name + " (" + i + ")" + extension + "' instat of '" + new_name + extension + "'", "", "", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/new-file.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                            file_name = path + "\\" + new_name + " (" + i + ")" + extension;
                    }
                    else
                        file_name = path + "\\" + new_name + extension;
                    try { if (file_name != null) File.Create(file_name); }
                    catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
                    Refresh();
                }
            }
            else
                new My_MessageBox("Create new file", "You can not create any file here!", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void NewDirectory(string new_name)
        {
            string dir_name = null; ;
            if (new_name != null)
            {
                if (Directory.Exists(path + "\\" + new_name))
                {
                    int i = 1;
                    while (Directory.Exists(path + "\\" + new_name + " (" + i + ")")) i++;
                    if (new My_MessageBox("New directory", "Do you want to create directory '" + new_name + " (" + i + ")' instat of '" + new_name + "'", "", "", new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/new-folder.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                        dir_name = path + "\\" + new_name + " (" + i + ")";
                }
                else
                    dir_name = path + "\\" + new_name;
                try { if (dir_name != null) Directory.CreateDirectory(dir_name); }
                catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            }
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
    }
}
