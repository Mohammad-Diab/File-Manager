using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using SevenZip;
using System.ComponentModel;
using System.Threading;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for File_Compresser.xaml
    /// </summary>
    public partial class File_Compresser : Window
    {
       /* PROBLEMS
       // remove directory and all sun from all files list

       */
        CompressItem current_view, root;
        bool project_saved = true, poject_has_saved = false, CloseWindow = false;
        string last_saved_path;
        string archive_path;
        ObservableCollection<CompresserListView> list_items = new ObservableCollection<CompresserListView>();

        CompressWindowMode window_mode = CompressWindowMode.Compress;
        CompressWindowMode WindowMode
        {
            get { return window_mode; }
            set
            {
                if (value == CompressWindowMode.Compress)
                {
                    main_grid.AllowDrop = true;
                    compress.ToolTip = "Compress";
                    extract_all.Visibility = Visibility.Collapsed;
                }
                else
                {
                    main_grid.AllowDrop = false;
                    compress.ToolTip = "Extract";
                    extract_all.Visibility = Visibility.Visible;
                    DoubleAnimation da = new DoubleAnimation(24, new Duration(new TimeSpan(1000000)));
                    extract_all.BeginAnimation(WidthProperty, da);
                }
                //compress.IsEnabled = p;
                save_menu.IsEnabled = save_project.IsEnabled = compress_menu.IsEnabled = files_menu.IsEnabled = add_files.IsEnabled = add_directory.IsEnabled = remove_selected.IsEnabled = clear_list.IsEnabled = value == CompressWindowMode.Compress;
                close_archive.IsEnabled = value == CompressWindowMode.extract;
                window_mode = value;
            }
        }

        public File_Compresser()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            list.DataContext = list_items;
            root = new CompressItem(null, null, root, false, null, null);
            current_view = root;
        }

        private void NewArchive_Click(object sender, RoutedEventArgs e)
        {
            if (!project_saved && root.AllFiles.Count > 0)
            {
                MyMessageBoxResult result = new My_MessageBox("FILE MANAGER", "Do you want to save your project?", "", ""/*To cancel just close this window.*/, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/save-48.png")), null, null).ShowThisDialog();
                if (result == MyMessageBoxResult.Yes)
                {
                    if (SaveProject())
                        ClearList_Click(null, null);
                }
                else if (result == MyMessageBoxResult.No)
                    ClearList_Click(null, null);
            }
            else
                ClearList_Click(null, null);
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (!project_saved && root.AllFiles.Count > 0)
            {
                MyMessageBoxResult result = new My_MessageBox("FILE MANAGER", "Do you want to save your project?", "", ""/*To cancel just close this window.*/, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/save-48.png")), null, null).ShowThisDialog();
                if (result == MyMessageBoxResult.Yes)
                {
                    if (SaveProject())
                    {
                        ClearList_Click(null, null);
                        OpenProject();
                    }
                }
                else if (result == MyMessageBoxResult.No)
                {
                    ClearList_Click(null, null);
                    OpenProject();
                }
            }
            else
            {
                ClearList_Click(null, null);
                OpenProject();
            }
        }

        private void OpenProject()
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.MultiSelect = false;
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Open project file...";
            sfd.AddExtension = false;
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "File Manager Archive (*.fma)", extentions = SelectFileFilter.SetExtentions(".fma") });
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    root = (CompressItem)bf.Deserialize(new FileInfo(sfd.SelectedItem).Open(FileMode.Open));
                    RefreshView(root, "root");
                }
                catch (Exception)
                { new My_MessageBox("FILE MANAGER", "Error while opening file.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            }
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            if (root.AllFiles.Count > 0)
                if (SaveProject())
                    poject_has_saved = true;
        }

        private bool SaveProject()
        {
            BinaryFormatter bf = new BinaryFormatter();
            bool done = false;
            string path = null;
            if (!poject_has_saved)
            {
                SelectFileDialog sfd = new SelectFileDialog();
                sfd.dialogMode = SelectFileDialogMode.SaveFile;
                sfd.MultiSelect = false;
                sfd.CreateFolderEnable = true;
                sfd.AddExtension = true;
                sfd.DialogTitle = "Save project file...";
                sfd.AddExtension = false;
                sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "File Manager Archive (*.fma)", extentions = SelectFileFilter.SetExtentions(".fma") });
                if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                    path = sfd.SelectedItem;
            }
            else
                path = last_saved_path;
            if (path != null)
            {
                try
                {
                    bf.Serialize(File.Open(path, FileMode.OpenOrCreate), root);
                    last_saved_path = path;
                    done = true;
                }
                catch (Exception ex)
                { new My_MessageBox("FILE MANAGER", string.Format("Sory, we can not save file '{0}'.{1}Extra info: {2}", ex.Message, Environment.NewLine, path), new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            }
            return done;
        }

        private void Compress_Click(object sender, RoutedEventArgs e)
        {
            if (WindowMode == CompressWindowMode.Compress)
                new Compress_Files().ShowWindow(root);
            else
            {
                if (list.SelectedItems.Count > 0)
                {
                    SelectFileDialog sfd = new SelectFileDialog();
                    sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
                    sfd.CreateFolderEnable = false;
                    sfd.DialogTitle = "Extract archive in ...";
                    sfd.AddExtension = false;
                    if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                        new ArchiveOperation().StartExtracting(/****/ArchiveType.SevenZip, "", sfd.SelectedItem, false, current_view, (int)ThreadPriorityLevel.Highest);
                }
                else
                    MessageBox.Show("Please select items to extract first.","FILE MANAGER");
            }
        }

        private void ExtractAll_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Extract archive in ...";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                new ArchiveOperation().StartExtracting(/****/ArchiveType.SevenZip, "", sfd.SelectedItem, true, root, (int)ThreadPriorityLevel.Highest);
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            if (current_view == root)
            {
                SelectFileDialog sfd = new SelectFileDialog();
                sfd.dialogMode = SelectFileDialogMode.OpenFile;
                sfd.CreateFolderEnable = false;
                sfd.DialogTitle = "Add files to archive...";
                sfd.AddExtension = false;
                sfd.MultiSelect = true;
                if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                {
                    foreach (string i in sfd.SelectedItems)
                        AddFile(i);
                    RefreshView(current_view, current_path.Text);
                    project_saved = false;
                }
            }
            else
                new My_MessageBox("FILE MANAGER", "You can only add items on 'root' directory.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (current_view == root)
            {
                SelectFileDialog sfd = new SelectFileDialog();
                sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
                sfd.CreateFolderEnable = false;
                sfd.DialogTitle = "Add Directory to archive...";
                sfd.AddExtension = false;
                if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                    AddDirectory(sfd.SelectedItem);
            }
            else
                new My_MessageBox("FILE MANAGER", "You can only add items on 'root' directory.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void RefreshView(CompressItem current, string address)
        {
            if (current.Children != null)
            {
                current_path.Text = address;
                go_up.IsEnabled = current != root;
                go_home.IsEnabled = current != root;
                int x = list.SelectedIndex;
                list_items.Clear();
                foreach (var i in current.Children)
                    list_items.Add(new CompresserListView(i.Value));
                if (list.Items.Count > 0)
                {
                    if (current != current_view)
                        list.ScrollIntoView(list.Items[0]);
                    else if (x > -1 && x < list.Items.Count)
                        list.ScrollIntoView(list_items[x]);
                }
                current_view = current;
            }
        }

        private void GoUp_Click(object sender, RoutedEventArgs e)
        {
            if (current_view.Parent != null)
                RefreshView(current_view.Parent, current_path.Text.Remove(current_path.Text.LastIndexOf('\\')));
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            WindowMode = CompressWindowMode.Compress;
            go_home.IsEnabled = false;
            go_up.IsEnabled = false;
            current_path.Text = "root";
            last_saved_path = string.Empty;
            root = new CompressItem(null, null, null, false, null, null);
            current_view = root;
            list_items.Clear();
            poject_has_saved = project_saved = false;
            
        }

        private void RemoveSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (current_view == root)
            {
                if (list.SelectedItems.Count > 0)
                {
                    for (int i = list.SelectedItems.Count - 1; i > -1; i--)
                    {
                        CompresserListView to_remove = list.SelectedItems[i] as CompresserListView;
                        int index = list_items.IndexOf(to_remove);
                        var item = root.Children[to_remove.FileName.ToLower()];
                        root.Children.Remove(to_remove.FileName.ToLower());
                        root.AllFiles.Remove(item);
                        RefreshView(current_view, current_path.Text);
                    }
                    project_saved = false;
                }
                else
                    new My_MessageBox("FILE MANAGER", "Please select item to remove.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            }
            else
                new My_MessageBox("FILE MANAGER", "You can only remove items from 'root' directory.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void Item_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && list.SelectedIndex > -1 && list_items[list.SelectedIndex].Referance.IsDirectory)
                RefreshView((list.SelectedItem as CompresserListView).Referance, current_path.Text += ("\\" + (list.SelectedItem as CompresserListView).Referance.FileName));
            else if (WindowMode == CompressWindowMode.Compress && e.ChangedButton == MouseButton.Left && list.SelectedIndex > -1)
            {
                try { Process.Start((list.SelectedItem as CompresserListView).Referance.Item.FullName); }
                catch (Exception ex) { new My_MessageBox("FILE MANAGER", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            }
            else if (e.ChangedButton == MouseButton.Left && list.SelectedIndex > -1)
            {
                //extract and excute

                //C:\Users\pal>"C:\Program Files\7-Zip\7z.exe" x "D:\User\Desktop\Project temp.7z" -o"D:\User\Desktop\New folder (2)" 1398177591_Administrative_Tools.png -r
                //Process pr = new Process();
                //pr.StartInfo = new ProcessStartInfo("Tools\\compress\\7z\\7z.exe", " x \"" + archive_path + "\" -o" + Environment.GetEnvironmentVariable("tmp") + " \"" + list_items[list.SelectedIndex].FileFullName + "\" -aoa -r");//output dir
                //pr.Start();
                //pr.WaitForExit();
                //string j = Environment.GetEnvironmentVariable("tmp");
                //try { Process.Start(Environment.GetEnvironmentVariable("tmp") + "\\" + list_items[list.SelectedIndex].FileFullName); }
                //catch (Exception ex) { new My_MessageBox("FILE MANAGER", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            }
        }

        private void GoHome_Click(object sender, RoutedEventArgs e)
        {
            RefreshView(root, "root");
        }

        private void OpenArchive_Click(object sender, RoutedEventArgs e)
        {
            bool open = false;
            if (WindowMode == CompressWindowMode.Compress && !project_saved && root.AllFiles.Count > 0)
            {
                MyMessageBoxResult result = new My_MessageBox("FILE MANAGER", "Do you want to save your project?", "", ""/*To cancel just close this window.*/, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/save-48.png")), null, null).ShowThisDialog();
                if (result == MyMessageBoxResult.Yes)
                {
                    if (SaveProject())
                        open = true;
                }
                else if (result == MyMessageBoxResult.No)
                    open = true;
            }
            else
                open = true;
            if (open)
            {
                //PasswordProtected files;
                ClearList_Click(null, null);
                SelectFileDialog sfd = new SelectFileDialog();
                sfd.dialogMode = SelectFileDialogMode.OpenFile;
                sfd.MultiSelect = false;
                sfd.CreateFolderEnable = false;
                sfd.AddExtension = false;
                sfd.DialogTitle = "Open archive file...";
                sfd.AddExtension = false;
                sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Archive files", extentions = SelectFileFilter.SetExtentions(".7z|.zip|.rar|.001|.gz|.tar|.cab|.iso|.xz|.txz|.lzma|.cpio|.bz2|.bzip|.bzip2|.tbz2|.tbz|.gzip|.tgz|.tpz|.z|.taz|.lzh|.rpm|.deb|.jar|.arj|.vhd|.wim|.swm|.fat|.ntfs|.dmg|.hfs|.xar|.squashfs") });
                sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "All files", extentions = SelectFileFilter.SetExtentions("") });
                if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                {
                    int total = 1, current = 0;
                    process_bar.Visibility = Visibility.Visible;
                    process_bar.IsIndeterminate = false;
                    main_grid.IsEnabled = false;
                    archive_path = sfd.SelectedItem;
                    string library_path = Path.Combine(Environment.CurrentDirectory + @"\Tools\compress\7z\7z.dll");
                    if (File.Exists(library_path))
                    {
                        SevenZipBase.SetLibraryPath(library_path);
                        BackgroundWorker bw = new BackgroundWorker();
                        bw.WorkerReportsProgress = true;
                        ReadOnlyCollection<ArchiveFileInfo> collection = null;
                        bw.DoWork += (object bw_sender, DoWorkEventArgs bw_e) =>
                        {
                            Thread th = new Thread(() =>
                            {
                                while(bw.IsBusy)
                                {
                                    try { bw.ReportProgress(current); }
                                    catch (Exception) { }
                                    if (bw.IsBusy)
                                        Thread.Sleep(10);
                                    else
                                        return;
                                }
                            });
                            th.Start();
                            try
                            {
                                SevenZipExtractor sze = new SevenZipExtractor(archive_path);
                                if (sze.Password != null)
                                {
                                    collection = sze.ArchiveFileData;
                                    if (collection != null)
                                    {
                                        total = collection.Count;
                                        foreach (var i in collection)
                                        {
                                            if (CloseWindow) return;
                                            new CompressItem(i.FileName, i.FileName.Substring(i.FileName.LastIndexOf("\\") + 1), root, root, i.Size, null, i.IsDirectory, true);
                                            current++;
                                        }
                                    }
                                }
                                else
                                {
                                    //Password messagebox
                                }
                                SortList(root, out root.RealSize, out root.CompressedSize);
                        }
                            catch (Exception)
                        { /*new My_MessageBox("FILE MANAGER", "Invalid file or wronge password.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav"));*/ }
                        };

                        bw.ProgressChanged += (object bw_sender, ProgressChangedEventArgs bw_e) =>
                        {
                            process_bar.Value = (bw_e.ProgressPercentage * 100.0) / (total + 0.0);
                        };

                        bw.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
                        {
                            RefreshView(root, "root");
                            WindowMode = CompressWindowMode.extract;
                            process_bar.Visibility = Visibility.Collapsed;
                            main_grid.IsEnabled = true;
                        };
                        bw.RunWorkerAsync();
                    }
                    else
                        MessageBox.Show("Fatal Error" + "We can not find 7-Zip library file.", "FILE MANAGER");
                }
            }
        }

        private void SortList(CompressItem item, out ulong? size, out ulong? compressed_size)
        {
            List<CompressItem> dirs = new List<CompressItem>();
            List<CompressItem> files = new List<CompressItem>();
            compressed_size = size = 0;
            root.Parent = null;
            foreach (var i in item.Children)
            {
                if (i.Value.IsDirectory)
                {
                    dirs.Add(i.Value);
                    ulong? sun_size, sun_comoressed_size;
                    SortList(i.Value, out sun_size, out sun_comoressed_size);
                    size += sun_size;
                    compressed_size += sun_comoressed_size;
                    i.Value.RealSize = sun_size;
                    i.Value.CompressedSize = sun_comoressed_size;
                }
                else
                {
                    files.Add(i.Value);
                    size += i.Value.RealSize;
                    compressed_size += i.Value.CompressedSize;
                }
            }
            dirs.Sort((CompressItem x, CompressItem y) => { return x.FileName.CompareTo(y.FileName); });
            files.Sort((CompressItem x, CompressItem y) => { return x.FileName.CompareTo(y.FileName); });
            item.Children.Clear();
            foreach (var i in dirs) item.Children.Add(i.FileName, i);
            foreach (var i in files) item.Children.Add(i.FileName, i);
        }

        private void CloseArchive_Click(object sender, RoutedEventArgs e)
        {
            ClearList_Click(null, null);
            WindowMode = CompressWindowMode.Compress;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FilesList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string i in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    if (i != string.Empty)
                        if (File.Exists(i))
                            AddFile(i);
                        else if (Directory.Exists(i))
                            AddDirectory(i);
                }
                RefreshView(current_view, current_path.Text);
                project_saved = false;
            }
        }

        private void AddDirectory(string i)
        {
            if (root.AllFiles.Any(x => x.Item.FullName.ToLower().Equals(i.ToLower()) || x.Item.FullName.ToLower().Contains(i.ToLower()) || i.ToLower().Contains(x.Item.FullName.ToLower())))
                new My_MessageBox("FILE MANAGER", "Sory this directory (or one of his sons) is already in the list.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            else if (root.Children.ContainsKey(Path.GetFileName(i).ToLower()))
                new My_MessageBox("FILE MANAGER", "Sorry other item with same name is already exist in the list.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            else
            {
                main_grid.IsEnabled = false;
                process_bar.Visibility = Visibility.Visible;
                root.AllFiles.Add(new CompressItem(new DirectoryInfo(i), root, root, true, null, null));
                RefreshView(current_view, current_path.Text);
                project_saved = false;
                process_bar.Visibility = Visibility.Collapsed;
                main_grid.IsEnabled = true;
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutTool window = new AboutTool();
            window.tool_vertion.Text = "Version: 1.1.2.128";
            window.tool_date.Text = "Date: 17/4/2015";
            window.tool_name.Text = "Compress file manager";
            window.tool_image.Source = Icon;
            //new 
            //Improve archive load speed
            //update file manager archive stracture (small size and fast)
            //progress bar for loading archive
            //fix some bugs
            window.ShowDialog();
        }

        private void AddFile(string i)
        {
            if (root.AllFiles.Any(x => x.Item.FullName.ToLower().Equals(i.ToLower())))
                new My_MessageBox("FILE MANAGER", "Sorry you can not add this file because it is alrady in the list.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            else if (root.Children.ContainsKey(Path.GetFileName(i).ToLower()))
                new My_MessageBox("FILE MANAGER", "Sory other item with same name is already exist in the list.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            else
            {
                FileInfo file = new FileInfo(i);
                new CompressItem(file, root, root, true, (ulong)file.Length, null);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (window_mode == CompressWindowMode.Compress && !project_saved && list.Items.Count > 0)
            {
                MyMessageBoxResult result = new My_MessageBox("FILE MANAGER", "Do you want to save your project?", "", ""/*To cancel just close this window.*/, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/save-48.png")), null, null).ShowThisDialog();
                if (result == MyMessageBoxResult.Yes)
                {
                    if (!SaveProject())
                        e.Cancel = true;
                    else
                        CloseWindow = true;
                }
                else if (result == MyMessageBoxResult.No)
                    CloseWindow = true;
            }
            else
                CloseWindow = true;
        }
    }

    class CompresserListView
    {
        public CompressItem Referance;
        public CompresserListView(CompressItem Item)
        {
            Referance = Item;
            Image = Functions.GetFileImage24(Item.Extention);
            FileName = Item.FileName;
            FileType = Item.FileType;
            FileSize = Item.RealSize.HasValue ? Functions.FileSize(Item.RealSize.Value, 2) : "?";
            CompressedFileSize = Item.CompressedSize.HasValue ? Functions.FileSize(Item.CompressedSize.Value, 2) : "?";
        }

        public void FillList(CompressItem parent, ObservableCollection<CompresserListView> list)
        {
            foreach (var i in parent.GetSuns())
                list.Add(new CompresserListView(i));
        }

        string file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private BitmapImage img_source;
        public BitmapImage Image
        {
            get { return img_source; }
            set { img_source = value; }
        }

        string file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }

        string file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }

        string compressed_file_size;
        public string CompressedFileSize
        {
            get { return compressed_file_size; }
            set { compressed_file_size = value; }
        }

    }

    [Serializable]
    class CompressItem
    {
        internal string FileName;
        internal string Extention;
        internal string FilePath;
        internal string FileType;
        internal List<CompressItem> AllFiles;
        internal Dictionary<string, CompressItem> Children;
        internal bool IsDirectory;
        internal CompressItem Parent;
        internal CompressItem Root;
        internal ulong? RealSize;
        internal ulong? CompressedSize;
        internal FileSystemInfo Item;

        public CompressItem(FileSystemInfo FileSystemItem, CompressItem Parent, CompressItem Root, bool add_sun, ulong? Size, ulong? CompressedSize)
        {
            Item = FileSystemItem;
            this.Parent = Parent;
            this.Root = Root;
            if (!add_sun)
            {
                RealSize = Size;
                this.CompressedSize = CompressedSize;
            }
            if (FileSystemItem == null)
            {
                this.Root = this;
                RealSize = 0;
                CompressedSize = 0;
                Children = new Dictionary<string, CompressItem>();
                AllFiles = new List<CompressItem>();
                IsDirectory = true;
            }
            else
            {
                FileName = FileSystemItem.Name;
                FilePath = FileSystemItem.FullName;
                if (FileSystemItem is FileInfo)
                {
                    RealSize = (ulong)((FileInfo)FileSystemItem).Length;
                    Extention = FileSystemItem.Extension;
                    FileType = Functions.GetFileType(FileSystemItem.Extension);
                    AllFiles = Root.AllFiles;
                    AllFiles.Add(this);
                    IsDirectory = false;
                    Parent.Children.Add(FileName.ToLower(), this);
                }
                else if (FileSystemItem is DirectoryInfo)
                {
                    Extention = "directory";
                    FileType = "Directory";
                    Children = new Dictionary<string, CompressItem>();
                    AllFiles = Root.AllFiles;
                    AllFiles.Add(this);
                    Parent.Children.Add(FileName.ToLower(), this);
                    IsDirectory = true;
                    RealSize = 0;
                    if (add_sun)
                        RealSize += AddSuns(FileSystemItem as DirectoryInfo, this, Root);
                }
                Parent.RealSize += RealSize;
                Parent.CompressedSize += CompressedSize;
            }
        }

        public CompressItem(string file_path, string file_name, CompressItem Parent, CompressItem Root, ulong? Size, ulong? CompressedSize, bool is_directory, bool add_sun)
        {
            this.Parent = Parent;
            this.Root = Root;
            IsDirectory = is_directory;
            FileName = file_name;
            FilePath = file_path;
            RealSize = Size;
            this.CompressedSize = CompressedSize;
            int x = FileName.LastIndexOf('.');
            Extention = x > 1 && x < FileName.Length ? FileName.Substring(x) : ".";
            if (is_directory)
            {
                Children = new Dictionary<string, CompressItem>();
                Extention = "directory";
            }
            FileType = Functions.GetFileType(Extention);
            AllFiles = Root.AllFiles;
            if (add_sun)
                AddItemToTree(this, Root);
        }

        void AddItemToTree(CompressItem item, CompressItem root)
        {
            CompressItem par = root;
            var fan = item.FilePath.Split(Functions.spliter, StringSplitOptions.RemoveEmptyEntries);
            string path = string.Empty;
            for (int i = 0; i < fan.Length - 1; i++)
            {
                if (i == 0)
                    path = fan[i].ToLower();
                else
                    path += "\\" + fan[i].ToLower();
                if (par.Children.ContainsKey(fan[i].ToLower()))
                    par = par.Children[fan[i].ToLower()];
                else
                {
                    CompressItem it = new CompressItem(path, fan[i], par, root, null, null, true, false);
                    par.Children.Add(it.FileName.ToLower(), it);
                    root.AllFiles.Add(it);
                    par = it;
                }
            }
            //error here
            item.Parent = par;
            if (!par.Children.ContainsKey(fan[fan.Length - 1].ToLower()))
            {
                par.Children.Add(item.FileName.ToLower(), item);
                root.AllFiles.Add(item);
            }
        }

        //private bool Contains(string item, out int index)
        //{
        //    int i;
        //    item = item.ToLower();
        //    if (Children != null)
        //        for (i = 0; i < Children.Count; i++)
        //            if (Children[i].FileName.ToLower().Equals(item))
        //            {
        //                index = i;
        //                return true;
        //            }
        //    index = -1;
        //    return false;
        //}

        public List<CompressItem> GetSuns()
        {
            return AllFiles.Where(x => x.Parent == Parent).ToList();
        }

        private ulong AddSuns(DirectoryInfo di, CompressItem parent, CompressItem root)
        {
            ulong size = 0;
            try
            {
                var dirs = di.GetDirectories();
                foreach (var i in dirs)
                {
                    CompressItem sun = new CompressItem(i, parent, root, true, null, null);
                    size += sun.RealSize.HasValue ? sun.RealSize.Value : 0;
                }
            }
            catch (Exception) { }
            try
            {
                var files = di.GetFiles();
                foreach (var i in files)
                    try
                    {
                        new CompressItem(i, parent, root, true, (ulong)i.Length, null);
                        size += (ulong)i.Length;
                    }
                    catch (Exception) { }
            }
            catch (Exception) { }
            return size;
        }

        internal static string[] GetFilsName(List<CompressItem> allFiles)
        {
            List<string> files = new List<string>();
            foreach (var i in allFiles)
                files.Add(i.Item.FullName);
            return files.ToArray();
        }
    }

    enum CompressWindowMode
    {
        Compress = 1,
        extract = 2
    }
}




//private long GetDirectorySize(DirectoryInfo dir)
//{
//    long size = 0;
//    try
//    {
//        foreach (var i in dir.GetFiles())
//            size += i.Length;
//        try
//        {
//            foreach (var i in dir.GetDirectories())
//                try
//                { size += GetDirectorySize(i); }
//                catch (Exception) { }
//        }
//        catch (Exception) { }
//    }
//    catch (Exception) { }
//    return size;
//}

//private void AddFolder_Click(object sender, RoutedEventArgs e)
//{
//    string path = new Folder_Picker().ShowThisDialog();
//    if (path != null)
//        list_items.Add(new ListItem(path/*, ItemType.Directory*/));
//}

//private void AddFiles_Click(object sender, RoutedEventArgs e)
//{
//    var OFD = new Microsoft.Win32.OpenFileDialog();
//    OFD.Multiselect = true;
//    if (OFD.ShowDialog() == true)
//        foreach (var i in OFD.FileNames)
//            list_items.Add(new ListItem(i/*, ItemType.File*/));
//}


//void SetValue(bool p)
//{
//    save_menu.IsEnabled = p;
//    save_project.IsEnabled = p;
//    compress_menu.IsEnabled = p;
//    if (p)
//    {
//        compress.ToolTip = "Compress";
//        extract_all.Visibility = Visibility.Collapsed;
//    }
//    else
//    {
//        compress.ToolTip = "Extract";
//        extract_all.Visibility = Visibility.Visible;
//        DoubleAnimation da = new DoubleAnimation(24, new Duration(new TimeSpan(1000000)));
//        extract_all.BeginAnimation(System.Windows.Controls.Button.WidthProperty, da);
//    }
//    //compress.IsEnabled = p;
//    files_menu.IsEnabled = p;
//    add_files.IsEnabled = p;
//    add_directory.IsEnabled = p;
//    remove_selected.IsEnabled = p;
//    clear_list.IsEnabled = p;
//    close_archive.IsEnabled = !p;
//}




//old compressing
//Process process = new Process();
//ProcessStartInfo psi = new ProcessStartInfo("Tools\\compress\\7z\\7z.exe", " l \"" + ofd.FileName + "\"");
//psi.RedirectStandardOutput = true;
//psi.RedirectStandardInput = true;
//psi.UseShellExecute = false;
//psi.CreateNoWindow = true;
//process.StartInfo = psi;
//process.Start();
//List<string> out_list = new List<string>();
//int[] stage = new int[3];
//int tmp_num = 0, k = 0;
//while (!process.StandardOutput.EndOfStream)
//{
//    string x = process.StandardOutput.ReadLine();
//    if (tmp_num < 3 && x.StartsWith("------------------- ----- ------------ ------------  ------------------------"))
//    {
//        stage[tmp_num] = k;
//        tmp_num++;
//    }
//    out_list.Add(x);
//    k++;
//}
//if (process.ExitCode == 0)
//{
//    project_saved = true;
//    CompressArchiveMode = true;
//    //string type = out_list[stage[0] + 2];
//    for (int i = stage[0] + 1; i < out_list.Count && i < stage[1]; i++)
//    {
//        string date = out_list[i].Substring(0, 19);
//        string atripute = out_list[i].Substring(20, 5);
//        ulong size, c_size;
//        ulong.TryParse(out_list[i].Substring(26, 12).Replace(" ", ""), out size);
//        ulong.TryParse(out_list[i].Substring(39, 12).Replace(" ", ""), out c_size);
//        string name = out_list[i].Substring(53);
//        CompresserListView tmp = new CompresserListView(name, size, c_size, date, atripute, root, root, true);
//    }
//    process.StandardOutput.Close();
//    SortList(root, out root.file_size_long, out root.compressed_size);
//    list_items.Clear();
//    foreach (var i in root.Children)
//        list_items.Add(i);
//} 
//else if (process.ExitCode == 2 || process.ExitCode == 8 || process.ExitCode == 255)
//        new My_MessageBox("FILE MANAGER", out_list[3], new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();