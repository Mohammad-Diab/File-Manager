using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
using System.Management;

namespace File_Explorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        ContextMenu directory_context_menu, file_context_menu;
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
                Up.IsEnabled = !(value == "Computer");
                directory_address = value;
            }
        }

        public MainWindow()
        {
            Initializer();
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
            back = new Stack<string>();
            front = new Stack<string>();
            if (FileInformation.file_bitmap.Count == 0)
                foreach (var i in FileInformation.file_image)
                    FileInformation.file_bitmap.Add(new BitmapImage(new Uri(StandardImageSource + i)));
        }

        private ListViewItem MakeGridForFile(FileInfo file)
        {
            int file_index = Functions.GetFileIndex(file.Extension);
            ListViewItem Item1 = new ListViewItem();
            Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
            Item1.VerticalAlignment = VerticalAlignment.Stretch;
            Item1.ContextMenu = MakeFileContextMenu(file_index);
            Item1.PreviewMouseDoubleClick += Element_DoubleClick;
            Item1.ContentStringFormat = file.FullName;
            Item1.Tag = file;
            if (show_image)
            {
                System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
                img1.Height = img1.Width = 64;
                img1.HorizontalAlignment = HorizontalAlignment.Left;
                img1.VerticalAlignment = VerticalAlignment.Stretch;
                img1.Source = FileInformation.file_bitmap[file_index];
                if (hiddenfiles && file.Attributes.HasFlag(FileAttributes.Hidden))
                    img1.Opacity = 0.5;
                TextBlock name = new TextBlock();
                name.Text = file.Name;
                name.HorizontalAlignment = HorizontalAlignment.Left;
                name.VerticalAlignment = VerticalAlignment.Stretch;
                name.Margin = name_thickness;
                name.TextTrimming = TextTrimming.CharacterEllipsis;
                TextBlock size = new TextBlock();
                size.Text = Functions.File_Size(file.Length);
                size.HorizontalAlignment = HorizontalAlignment.Left;
                size.VerticalAlignment = VerticalAlignment.Top;
                size.Margin = size_thickness;
                size.TextTrimming = TextTrimming.CharacterEllipsis;
                TextBlock type = new TextBlock();
                type.Text = Functions.GetFileType(file_index, file.Extension);
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
                Item1.Content = Grid1;
            }
            else
                Item1.Content = file.Name;
            return Item1;
        }

        private ListViewItem MakeGridForDirectory(DirectoryInfo folder)
        {
            ListViewItem Item1 = new ListViewItem();
            Item1.HorizontalAlignment = HorizontalAlignment.Stretch;
            Item1.VerticalAlignment = VerticalAlignment.Stretch;
            Item1.ContextMenu = directory_context_menu;
            Item1.PreviewMouseDoubleClick += Element_DoubleClick;
            Item1.ContentStringFormat = folder.FullName;
            Item1.Tag = folder;
            if (show_image)
            {
                System.Windows.Controls.Image img1 = new System.Windows.Controls.Image();
                img1.Height = img1.Width = 64;
                img1.HorizontalAlignment = HorizontalAlignment.Left;
                img1.VerticalAlignment = VerticalAlignment.Stretch;
                img1.Source = FileInformation.file_bitmap[FileInformation.file_bitmap.Count - 2];
                if (hiddenfiles && folder.Attributes.HasFlag(FileAttributes.Hidden))
                    img1.Opacity = 0.5;
                TextBlock text = new TextBlock();
                text.Text = folder.Name;
                text.Margin = folder_thickness;
                text.HorizontalAlignment = HorizontalAlignment.Stretch;
                text.VerticalAlignment = VerticalAlignment.Stretch;
                text.TextTrimming = TextTrimming.CharacterEllipsis;
                Grid Grid1 = new Grid();
                Grid1.HorizontalAlignment = HorizontalAlignment.Stretch;
                Grid1.Children.Add(img1);
                Grid1.Children.Add(text);
                Item1.Content = Grid1;
            }
            else
                Item1.Content = folder.Name;
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
                    ProgressBar drive_size = new ProgressBar();
                    drive_size.Margin = new Thickness(80, 25, 0, 20); ;
                    text.VerticalAlignment = VerticalAlignment.Top;
                    drive_size.Value = 100 - (drive.TotalFreeSpace * 100) / drive.TotalSize;
                    if (drive_size.Value > 90)
                        drive_size.Foreground = System.Windows.Media.Brushes.Crimson;
                    else
                        drive_size.Foreground = System.Windows.Media.Brushes.RoyalBlue;
                    drive_size.Width = 350;
                    TextBlock size = new TextBlock();
                    size.Text = Functions.File_Size(drive.TotalFreeSpace) + " free of " + Functions.File_Size(drive.TotalSize);
                    size.Foreground = System.Windows.Media.Brushes.DarkGray;
                    size.Margin = type_thickness;
                    size.HorizontalAlignment = HorizontalAlignment.Left;
                    size.VerticalAlignment = VerticalAlignment.Top;
                    size.TextTrimming = TextTrimming.CharacterEllipsis;
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
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); if (close) Close(); }
        }

        private void Main_Window_Loaded(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("--Arabic (العربية)--\n**اخلاء مسؤلية**\nهذا البرنامج لم يصدر بعد وهو في نسخة تجريبية لذلك يجب\nأن تكون حذرا في التعامل معه وخاصة خاصية 'الحذف' التي\nقد تقوم وعن غير قصد بحذف احد ملفاتك الضرورية أو الاضرار\nبجهازك.\nيمكنك الاستمتاع  باستعمال كامل الميزات الآخرى ولكن يجب ان تكون حذرا جدا.\n\nالرجاء عدم استخدام خاصية الحذف.\n\n--English (English)--\n**Disclaimer**\nThis app is in alpha version, so be very careful using it\nespecially delete Function, it may delete some of your\nimportant files or damage your system.\nyou can ingoye using all other features, but you should be very careful.\n\nplease don't use 'delete' function.", "FILE MANAGER", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK);
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
            CreateNewListBox(path);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("File Manager v0.5.1-alpha.1\nby: mohammad.diab93@outlook.com", "FILE MANAGER", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
        }

        private void CreateNewListBox(string p)
        {
            try
            {
                ListBox list_box = new ListBox();
                list_box.Width = 250;
                list_box.ToolTip = p;
                //list_box.SelectionChanged += list_box_SelectionChanged;
                if (p == "Computer")
                {
                    foreach (var i in DriveInfo.GetDrives())
                    {
                        ListBoxItem Item1 = new ListBoxItem();
                        Item1.ContextMenu = MakeDriveContextMenu(i);
                        Item1.PreviewMouseDoubleClick += ElementNew_DoubleClick;
                        Item1.ContentStringFormat = i.Name;
                        Item1.Tag = i;
                        Item1.Content = i.Name;
                        list_box.Items.Add(Item1);
                    }
                }
                else
                {
                    foreach (var i in new DirectoryInfo(p).GetDirectories())
                    {
                        ListBoxItem Item1 = new ListBoxItem();
                        Item1.ContextMenu = directory_context_menu;
                        Item1.PreviewMouseDoubleClick += ElementNew_DoubleClick;
                        Item1.ContentStringFormat = i.FullName;
                        Item1.Tag = i;
                        Item1.Content = i.Name;
                        list_box.Items.Add(Item1);
                    }
                    foreach (var i in new DirectoryInfo(p).GetFiles())
                    {
                        ListBoxItem Item1 = new ListBoxItem();
                        Item1.ContextMenu = directory_context_menu;
                        Item1.PreviewMouseLeftButtonUp += Item1_PreviewMouseLeftButtonUp;
                        Item1.ContentStringFormat = i.FullName;
                        Item1.Tag = i;
                        Item1.Content = i.Name;
                        list_box.Items.Add(Item1);
                    }
                }
                stack1.Children.Insert(stack1.Children.Count - 1, list_box);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void Item1_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ViewFileInfo(((ListBoxItem)sender).ContentStringFormat);
        }

        private void ElementNew_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            int x = stack1.Children.Count;
            for (int i = 1; i < stack1.Children.Count - 1; i++)
                if (((ListBox)stack1.Children[i]).ToolTip.ToString().Equals(new DirectoryInfo(((ListBoxItem)sender).ContentStringFormat.ToString()).FullName))
                {
                    x = i;
                    break;
                }
            for (int i = x; i < stack1.Children.Count - 1; i++)
                stack1.Children.RemoveAt(i);
            CreateNewListBox(((ListBoxItem)sender).ContentStringFormat);
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
                MessageBox.Show("You canot move any file or directory from here!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show("You canot copy any file or directory from here!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show("You can not paste any item here!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            else
                MessageBox.Show("There is not any item to paste it!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void NewWindowAdmin_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo PSI = new ProcessStartInfo(System.Windows.Forms.Application.ExecutablePath);
            PSI.Verb = "runas";
            try
            { System.Diagnostics.Process.Start(PSI); }
            catch (Exception err)
            { MessageBox.Show(err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
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
                MessageBox.Show("You can not create directory here", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Quit(object sender, RoutedEventArgs e)
        {
            Process.GetCurrentProcess().Close();
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
            new Identical_File_Remover().ShowDialog();
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
            ManagementObjectSearcher searcher;
            computer_Name = System.Environment.MachineName;
            operating_System = System.Environment.OSVersion.Clone().ToString() + " " + System.Environment.Version.ToString();
            searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            CPU = Environment.CommandLine;
            //foreach (var share in searcher.Get())
            //{
            //    CPU = share["Name"].ToString();
            //    break;
            //}
            searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
            long memory_size = 0;
            foreach (var share in searcher.Get())
            {
                long tmp;
                long.TryParse(share.Properties["Capacity"].Value.ToString(), out tmp);
                memory_size = memory_size + tmp;
            }
            Memory_Size = Functions.File_Size(memory_size) + " Memory";
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
            catch (Exception er)
            { MessageBox.Show(er.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
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
                List<UnCompleatOperation> un_done = new List<UnCompleatOperation>();
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
                    foreach (var i in delete_files)
                        DeleteFile(i, un_done);
                    foreach (var i in delete_dirs)
                        DeleteDirectory(i, un_done);
                }
                while (un_done.Count > 0)
                {
                    var retry = new File_s_transferring_Error(un_done, "Some items have not deleted successfully.<note: default option is skip>").ShowThisDialog();
                    for (int i = retry.Count - 1; i >= 0; i--)
                    {
                        if (retry[i])
                            if (un_done[i].IsDirectory)
                                DeleteDirectory(un_done[i].files_full_name, un_done);
                            else
                                DeleteFile(un_done[i].files_full_name, un_done);
                        un_done.RemoveAt(i);
                    }
                } //MessageBox.Show("Some files haven't been deleted successfully", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteFile(string file_name, List<UnCompleatOperation> un_done)
        {
            try
            { File.Delete(file_name); }
            catch (Exception ex)
            { un_done.Add(new UnCompleatOperation(file_name, ex.Message, false)); }
        }

        private void DeleteDirectory(string directory_name, List<UnCompleatOperation> un_done)
        {
            try
            { Directory.Delete(directory_name, false); }
            catch (Exception ex)
            { un_done.Add(new UnCompleatOperation(directory_name, ex.Message, true)); }
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
                        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Stop); }
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
                        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Stop); }
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
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
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
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void CopyTo_Click(object sender, RoutedEventArgs e)
        {
            string copy_to = new Folder_Picker("Select directory to copy to").ShowThisDialog();
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
                MessageBox.Show("You can not preview more than one item in same time.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OpenInNewWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var i in Explorer.SelectedItems)
                    Functions.OpenWith(((ListViewItem)i).ContentStringFormat, Functions.GetItemType(((ListViewItem)i).ContentStringFormat));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
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
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                    Refresh();
                }
            }
            else
                MessageBox.Show("You can not create any file here", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
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

        //private void SelectItem(string to_select_file)
        //{
        //    int dir_numbers = 0;
        //    foreach (var i in Directory.GetDirectories(path))
        //        if (hiddenfiles || (!hiddenfiles && !new DirectoryInfo(i).Attributes.HasFlag(FileAttributes.Hidden)))
        //            dir_numbers++;
        //    int file_index = 0;
        //    var files = Directory.GetFiles(path);
        //    for (int i = 0; files[i] != path; i++)
        //        if (hiddenfiles || (!hiddenfiles && !File.GetAttributes(files[i]).HasFlag(FileAttributes.Hidden)))
        //            file_index++;
        //    Explorer.SelectedIndex = file_index + dir_numbers;
        //}

//DriveInfo[] Drives;
//List<string> directorys;
//List<string> files;
//DirectoryInfo current_Directory, View_Directory;
//FileInfo View_File;
//string copydir, , ;
//, to_copy_directory;
//int sub_dirs, sub_files;
//long to_copy_files_Size;
//static public string to_go_path, new_Name = null;
//static public bool go_to_path;
//
//string , PrimPath;
//string file_name = null;
//string Path
//{
//    set
//    {
//        if (value == String.Empty)
//            this.Address.Text = "Computer";
//        else
//            this.Address.Text = value;
//    }
//    get
//    { return this.Address.Text; }
//}
//






//private ItemType GetDriveType(string path)
//{
//    DriveInfo d = new DriveInfo(path);
//    if (d.DriveType == DriveType.Fixed)
//        if (d.IsReady)
//            return ItemType.HardDrive;
//        else
//            return ItemType.NotReadyHardDrive;
//    else if (d.DriveType == DriveType.CDRom)
//        if (d.IsReady)
//            return ItemType.DVDRom;
//        else
//            return ItemType.NotReadyDVDRom;
//    else if (d.DriveType == DriveType.Removable)
//        if (d.IsReady)
//            return ItemType.Removable;
//        else
//            return ItemType.NotReadyRemovable;
//    else if (d.DriveType == DriveType.Ram)
//        return ItemType.Ram;
//    else
//        return ItemType.Unknown;
//}







//internal static string GetImageSource(string imagesource, string type, bool Condition)
//{
//    if (type.Length > 0 && type[type.Length - 1] == '_')
//        imagesource = imagesource + "archive-cab.png";
//    else
//        switch (type.ToLower())
//        {
//            case "computer":
//                imagesource = imagesource + "computer.png";
//                break;
//            case "fixed":
//                if (Condition)
//                    imagesource = imagesource + "system-hard-disk.png";
//                else
//                    imagesource = imagesource + "hard-disk.png";
//                break;
//            case "cdrom":
//                if (Condition)
//                    imagesource = imagesource + "dvd-media.png";
//                else
//                    imagesource = imagesource + "optical-drive.png";
//                break;
//            case "removable":
//                if (Condition)
//                    imagesource = imagesource + "removable-usb.png";
//                else
//                    imagesource = imagesource + "removable-media.png";
//                break;
//            case "folder":
//                imagesource = imagesource + "folder.png";
//                break;
//            case ".txt":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".log":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".sh":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".bsh":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".bat":
//                imagesource = imagesource + "text-x.png";
//                break;
//            case ".cmd":
//                imagesource = imagesource + "text-x.png";
//                break;
//            case ".nt":
//                imagesource = imagesource + "text-x.png";
//                break;
//            case ".c":
//                imagesource = imagesource + "text-c.png";
//                break;
//            case ".cpp":
//                imagesource = imagesource + "text-c++.png";
//                break;
//            case ".cs":
//                imagesource = imagesource + "text-cs.png";
//                break;
//            case ".html":
//                imagesource = imagesource + "text-html.png";
//                break;
//            case ".mht":
//                imagesource = imagesource + "text-html.png";
//                break;
//            case ".htm":
//                imagesource = imagesource + "text-html.png";
//                break;
//            case ".shtml":
//                imagesource = imagesource + "text-html.png";
//                break;
//            case ".shtm":
//                imagesource = imagesource + "text-html.png";
//                break;
//            case ".xhtml":
//                imagesource = imagesource + "text-html.png";
//                break;
//            case ".hta":
//                imagesource = imagesource + "text-html.png";
//                break;
//            case ".ini":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".inf":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".reg":
//                imagesource = imagesource + "text-reg.png";
//                break;
//            case ".url":
//                imagesource = imagesource + "text-html.png";
//                break;
//            case ".java":
//                imagesource = imagesource + "text-java.png";
//                break;
//            case ".js":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".nfo":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".pas":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".inc":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".pl":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".pm":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".plx":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".php":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".php3":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".phtml":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".ps":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".ps1":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".py":
//                imagesource = imagesource + "text-python.png";
//                break;
//            case ".pyw":
//                imagesource = imagesource + "text-python.png";
//                break;
//            case ".sql":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".tex":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".vb":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".vbs":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".v":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".vhdl":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".xml":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".xsml":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".xsl":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".xsd":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".kml":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".wsdl":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".yml":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".docx":
//                imagesource = imagesource + "text-richtext.png";
//                break;
//            case ".doc":
//                imagesource = imagesource + "text-richtext.png";
//                break;
//            case ".odt":
//                imagesource = imagesource + "text-richtext.png";
//                break;
//            case ".rtf":
//                imagesource = imagesource + "text-richtext.png";
//                break;
//            case ".docm":
//                imagesource = imagesource + "text-richtext.png";
//                break;
//            case ".dot":
//                imagesource = imagesource + "text-richtext.png";
//                break;
//            case ".dotx":
//                imagesource = imagesource + "text-richtext.png";
//                break;
//            case ".ppsx":
//                imagesource = imagesource + "text-presentation.png";
//                break;
//            case ".pptx":
//                imagesource = imagesource + "text-presentation.png";
//                break;
//            case ".pps":
//                imagesource = imagesource + "text-presentation.png";
//                break;
//            case ".ppt":
//                imagesource = imagesource + "text-presentation.png";
//                break;
//            case ".pptm":
//                imagesource = imagesource + "text-presentation.png";
//                break;
//            case ".potx":
//                imagesource = imagesource + "text-presentation.png";
//                break;
//            case ".pot":
//                imagesource = imagesource + "text-presentation.png";
//                break;
//            case ".odp":
//                imagesource = imagesource + "text-presentation.png";
//                break;
//            case ".xlsx":
//                imagesource = imagesource + "text-spreadsheet.png";
//                break;
//            case ".xlsm":
//                imagesource = imagesource + "text-spreadsheet.png";
//                break;
//            case ".xlsb":
//                imagesource = imagesource + "text-spreadsheet.png";
//                break;
//            case ".xls":
//                imagesource = imagesource + "text-spreadsheet.png";
//                break;
//            case ".xltx":
//                imagesource = imagesource + "text-spreadsheet.png";
//                break;
//            case ".xltm":
//                imagesource = imagesource + "text-spreadsheet.png";
//                break;
//            case ".xlt":
//                imagesource = imagesource + "text-spreadsheet.png";
//                break;
//            case ".ods":
//                imagesource = imagesource + "text-spreadsheet.png";
//                break;
//            case ".accdb":
//                imagesource = imagesource + "text-database.png";
//                break;
//            case ".mdl":
//                imagesource = imagesource + "text-database.png";
//                break;
//            case ".odbc":
//                imagesource = imagesource + "text-database.png";
//                break;
//            case ".accdt":
//                imagesource = imagesource + "text-database.png";
//                break;
//            case ".mdb":
//                imagesource = imagesource + "text-database.png";
//                break;
//            case ".pdf":
//                imagesource = imagesource + "text-pdf.png";
//                break;
//            case ".xps":
//                imagesource = imagesource + "text-xps.png";
//                break;
//            case ".oxps":
//                imagesource = imagesource + "text-xps.png";
//                break;
//            case ".djvu":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".comic":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".chm":
//                imagesource = imagesource + "text-help.png";
//                break;
//            case ".mobi":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".epub":
//                imagesource = imagesource + "text-file.png";
//                break;
//            case ".vdi":
//                imagesource = imagesource + "disk-vdi.png";
//                break;
//            case ".vhd":
//                imagesource = imagesource + "disk-vhd.png";
//                break;
//            case ".hdd":
//                imagesource = imagesource + "disk-hdd.png";
//                break;
//            case ".iso":
//                imagesource = imagesource + "disk-iso.png";
//                break;
//            case ".bin":
//                imagesource = imagesource + "disk-iso.png";
//                break;
//            case ".isz":
//                imagesource = imagesource + "disk-iso.png";
//                break;
//            case ".img":
//                imagesource = imagesource + "disk-iso.png";
//                break;
//            case ".dmg":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".nrg":
//                imagesource = imagesource + "disk-iso.png";
//                break;
//            case ".mou":
//                imagesource = imagesource + "disk-mou.png";
//                break;
//            case ".zip":
//                imagesource = imagesource + "archive-zip.png";
//                break;
//            case ".cab":
//                imagesource = imagesource + "archive-cab.png";
//                break;
//            case ".7z":
//                imagesource = imagesource + "archive-7z.png";
//                break;
//            case ".rar":
//                imagesource = imagesource + "archive-rar.png";
//                break;
//            case ".tar":
//                imagesource = imagesource + "archive-tar.png";
//                break;
//            case ".deb":
//                imagesource = imagesource + "archive-deb.png";
//                break;
//            case ".xz":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".txz":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".lzma":
//                imagesource = imagesource + "archive-lzma.png";
//                break;
//            case ".cpio":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".bz2":
//                imagesource = imagesource + "archive-bz2.png";
//                break;
//            case ".bzip2":
//                imagesource = imagesource + "archive-bz2.png";
//                break;
//            case ".gz":
//                imagesource = imagesource + "archive-gz.png";
//                break;
//            case ".taz":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".fat":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".ntfs":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".hfs":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".rpm":
//                imagesource = imagesource + "archive-rpm.png";
//                break;
//            case ".wim":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".ace":
//                imagesource = imagesource + "archive-ace.png";
//                break;
//            case ".jar":
//                imagesource = imagesource + "archive-jar.png";
//                break;
//            case ".uha":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".squashfs":
//                imagesource = imagesource + "archive-file.png";
//                break;
//            case ".mp3":
//                imagesource = imagesource + "multimedia-mp3.png";
//                break;
//            case ".3gp":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".3ggp2":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".3gpp":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".acc":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".ac3":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".adt":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".adts":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".amr":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".amv":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".avi":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".flv":
//                imagesource = imagesource + "multimedia-flv.png";
//                break;
//            case ".hdmov":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".mid":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".midi":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".mka":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".mkv":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".mp2":
//                imagesource = imagesource + "multimedia-mp3.png";
//                break;
//            case ".mp4":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".mp4v":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".mpeg":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".mpg":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".ogg":
//                imagesource = imagesource + "multimedia-ogg.png";
//                break;
//            case ".ogm":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".ogv":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".opus":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".ra":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".ram":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".rm":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".wav":
//                imagesource = imagesource + "multimedia-wav.png";
//                break;
//            case ".wma":
//                imagesource = imagesource + "multimedia-wma.png";
//                break;
//            case ".wmv":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".wv":
//                imagesource = imagesource + "multimedia-wav.png";
//                break;
//            case ".vob":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".mov":
//                imagesource = imagesource + "multimedia-video.png";
//                break;
//            case ".swf":
//                imagesource = imagesource + "multimedia-flv.png";
//                break;
//            case ".aac":
//                imagesource = imagesource + "multimedia-audio.png";
//                break;
//            case ".jpg":
//                imagesource = imagesource + "multimedia-jpg.png";
//                break;
//            case ".png":
//                imagesource = imagesource + "multimedia-png.png";
//                break;
//            case ".ico":
//                imagesource = imagesource + "multimedia-ico.png";
//                break;
//            case ".bmp":
//                imagesource = imagesource + "multimedia-bmp.png";
//                break;
//            case ".gif":
//                imagesource = imagesource + "multimedia-gif.png";
//                break;
//            case ".tif":
//                imagesource = imagesource + "multimedia-tif.png";
//                break;
//            case ".tiff":
//                imagesource = imagesource + "multimedia-tif.png";
//                break;
//            case ".dib":
//                imagesource = imagesource + "multimedia-image.png";
//                break;
//            case ".jpe":
//                imagesource = imagesource + "multimedia-jpg.png";
//                break;
//            case ".jpeg":
//                imagesource = imagesource + "multimedia-jpg.png";
//                break;
//            case ".jfif":
//                imagesource = imagesource + "multimedia-image.png";
//                break;
//            case ".svg":
//                imagesource = imagesource + "multimedia-image.png";
//                break;
//            case ".jxr":
//                imagesource = imagesource + "multimedia-image.png";
//                break;
//            case ".wdp":
//                imagesource = imagesource + "multimedia-image.png";
//                break;
//            case ".psd":
//                imagesource = imagesource + "multimedia-psd.png";
//                break;
//            case ".pcx":
//                imagesource = imagesource + "multimedia-image.png";
//                break;
//            case ".tga":
//                imagesource = imagesource + "multimedia-image.png";
//                break;
//            case ".fon":
//                imagesource = imagesource + "x-fon.png";
//                break;
//            case ".ttc":
//                imagesource = imagesource + "x-fon.png";
//                break;
//            case ".ttf":
//                imagesource = imagesource + "x-fon.png";
//                break;
//            case ".dll":
//                imagesource = imagesource + "x-dll.png";
//                break;
//            case ".exe":
//                imagesource = imagesource + "x-exe.png";
//                break;
//            case ".cpl":
//                imagesource = imagesource + "x-dll.png";
//                break;
//            case ".h":
//                imagesource = imagesource + "text-script.png";
//                break;
//            case ".drv":
//                imagesource = imagesource + "x-dll.png";
//                break;
//            case ".msc":
//                imagesource = imagesource + "x-exe.png";
//                break;
//            case ".com":
//                imagesource = imagesource + "text-x.png";
//                break;
//            case ".scr":
//                imagesource = imagesource + "x-scr.png";
//                break;
//            case ".msi":
//                imagesource = imagesource + "x-package.png";
//                break;
//            case ".sys":
//                imagesource = imagesource + "x-sys.png";
//                break;
//            case ".db":
//                imagesource = imagesource + "x-sys.png";
//                break;
//            case ".pnf":
//                imagesource = imagesource + "x-sys.png";
//                break;
//            case ".dat":
//                imagesource = imagesource + "file.png";
//                break;
//            case "":
//                imagesource = imagesource + "file.png";
//                break;
//            default:
//                imagesource = imagesource + "unknown-file.png";
//                break;
//        }
//    return imagesource;
//}

//internal static bool IsImage(string extension)
//{
//    extension = extension.ToLower();
//    if (extension.Equals(".png") || extension.Equals(".jpg") || extension.Equals(".bmp") || extension.Equals(".gif") || extension.Equals(".ico") || extension.Equals(".tif") || extension.Equals(".dib") || extension.Equals(".jfif") || extension.Equals(".jpe") || extension.Equals(".jpeg") || extension.Equals(".jxr") || extension.Equals(".tiff") || extension.Equals(".wdp"))
//        return true;
//    return false;
//}

//internal static bool IsVedio(string extension)
//{
//    extension = extension.ToLower();
//    if (extension.Equals(".avi") || extension.Equals(".wmv") || extension.Equals(".3gp") || extension.Equals(".mov") || extension.Equals(".mp4") || extension.Equals(".3g2") || extension.Equals(".3gp2") || extension.Equals(".3gpp") || extension.Equals(".bdmv") || extension.Equals(".flv") || extension.Equals(".hdmov") || extension.Equals(".mpe") || extension.Equals(".mpeg") || extension.Equals(".mpg") || extension.Equals(".ogm") || extension.Equals(".ogv") || extension.Equals(".rm") || extension.Equals(".vob"))
//        return true;
//    return false;
//}

//internal static bool IsAudio(string extension)
//{
//    extension = extension.ToLower();
//    if (extension.Equals(".mp3") || extension.Equals(".wma") || extension.Equals(".acc") || extension.Equals(".ogg") || extension.Equals(".amr") || extension.Equals(".ac3") || extension.Equals(".mp2") || extension.Equals(".oga") || extension.Equals(".ra") || extension.Equals(".ram") || extension.Equals(".wav") || extension.Equals(".wv") || extension.Equals(".mid") || extension.Equals(".midi"))
//        return true;
//    return false;
//}

//internal static bool IsHtml(string extension)
//{
//    extension = extension.ToLower();
//    if (extension.Equals(".svg") || extension.Equals(".swf") || extension.Equals(".htm") || extension.Equals(".html") || extension.Equals(".hta") || extension.Equals(".mht") || extension.Equals(".shtml") || extension.Equals(".shtm") || extension.Equals(".xhtml") || extension.Equals(".php") || extension.Equals(".php3") || extension.Equals(".phtml") || extension.Equals(".xml") || extension.Equals(".xsml") || extension.Equals(".xsl"))
//        return true;
//    return false;
//}

//internal static bool IsText(string extension)
//{
//    extension = extension.ToLower();
//    if (extension.Equals(".txt") || extension.Equals(".cs") || extension.Equals(".cpp") || extension.Equals(".log") || extension.Equals(".sh") || extension.Equals(".bsh") || extension.Equals(".bat") || extension.Equals(".cmd") || extension.Equals(".nt") || extension.Equals(".c") || extension.Equals(".ini") || extension.Equals(".inf") || extension.Equals(".reg") || extension.Equals(".url") || extension.Equals(".java") || extension.Equals(".js") || extension.Equals(".nfo") || extension.Equals(".pas") || extension.Equals(".inc") || extension.Equals(".pl") || extension.Equals(".pm") || extension.Equals(".plx") || extension.Equals(".ps") || extension.Equals(".ps1") || extension.Equals(".py") || extension.Equals(".pyw") || extension.Equals(".tex") || extension.Equals(".vb") || extension.Equals(".vbs") || extension.Equals(".v") || extension.Equals(".vhdl") || extension.Equals(".xsd") || extension.Equals(".kml") || extension.Equals(".wsdl") || extension.Equals(".yml"))
//        return true;
//    return false;
//}

//internal static bool IsArchive(string extension)
//{
//    extension = extension.ToLower();
//    if ((extension.Length > 0 && extension[extension.Length - 1] == '_') || extension.Equals(".7z") || extension.Equals(".zip") || extension.Equals(".rar") || extension.Equals(".001") || extension.Equals(".cab") || extension.Equals(".xz") || extension.Equals(".txz") || extension.Equals(".lzma") || extension.Equals(".tar") || extension.Equals(".cpio") || extension.Equals(".bz2") || extension.Equals(".bzip2") || extension.Equals(".tbz2") || extension.Equals(".tbz") || extension.Equals(".gz") || extension.Equals(".gzip") || extension.Equals(".tgz") || extension.Equals(".tpz") || extension.Equals(".z") || extension.Equals(".taz") || extension.Equals(".lzh") || extension.Equals(".lha") || extension.Equals(".rpm") || extension.Equals(".deb") || extension.Equals(".arj") || extension.Equals(".wim") || extension.Equals(".swm") || extension.Equals(".fat") || extension.Equals(".ntfs") || extension.Equals(".dmg") || extension.Equals(".hfs") || extension.Equals(".xar") || extension.Equals(".squashfs") || extension.Equals(".ace") || extension.Equals(".uha") || extension.Equals(".kgp") || extension.Equals(".kge"))
//        return true;
//    return false;
//}

//internal static bool IsFont(string extension)
//{
//    extension = extension.ToLower();
//    if (extension.Equals(".ttf") || extension.Equals(".fon") || extension.Equals(".ttc"))
//        return true;
//    return false;
//}

//private void Get_all_files(string dir)
//{
//    to_copy_directory.Add(dir);
//    var sub_dirs = Directory.GetDirectories(dir);
//    DirectoryInfo directory = new DirectoryInfo(dir);
//    to_copy_files_Size = 0;
//    foreach (var i in directory.GetFiles())
//    {
//        to_copy_files.Add(i.FullName);
//        to_copy_files_Size += (i.Length);
//    }
//    if (sub_dirs.Length == 0)
//        return;
//    else
//        foreach (var i in sub_dirs)
//            Get_all_files(i);
//}

//private void Refresh(object sender, KeyEventArgs e)
//{
//    if (e.Key == Key.F5)
//        Refresh();
//}

        //private string SubName(string name, string toRemove)
        //{
        //    return name.Remove(0, toRemove.Length);
        //}