using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for File_Destroyer.xaml
    /// </summary>
    public partial class File_Destroyer : Window
    {
        //dont add file with already in list
        bool CloseWindow = false, starting_window = true;
        DriveInfo selectdrive = null;
        DestroyPages current_page = DestroyPages.FilesDestroy;
        DestroyPages CurrentPage
        {
            get { return current_page; }
            set
            {
                if (value == DestroyPages.DiskDestroy && current_page != DestroyPages.DiskDestroy)
                    GetDrivers();
                current_page = value;
            }
        }

        public File_Destroyer()
        {
            InitializeComponent();
        }

        public File_Destroyer(object list, bool destroy_files)//true file destroyer false disk destroyer
        {
            InitializeComponent();
            if (destroy_files)
                AddFiles(list as string[]);
            else
            {
                CurrentPage = DestroyPages.DiskDestroy;
                selectdrive = list as DriveInfo;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        #region Destroy Files

        List<DestroyListView> destroy_list_data = new List<DestroyListView>();

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.MultiSelect = true;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Select files to destroy";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                AddFiles(sfd.SelectedItems.ToArray());
        }

        private void AddFiles(string[] files)
        {
            foreach (string i in files)
                if (File.Exists(i))
                    destroy_list_data.Add(new DestroyListView(new FileInfo(i)));
            files_list.Items.Refresh();
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Add all files in this directory.";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                main_grid.IsEnabled = false;
                BackgroundWorker add_files_in_directory = new BackgroundWorker();
                add_files_in_directory.WorkerReportsProgress = true;
                add_files_in_directory.DoWork += (object bw_sender, DoWorkEventArgs e_bw) =>
                {
                    GetAllFiles(new DirectoryInfo(sfd.SelectedItem), add_files_in_directory);
                };
                add_files_in_directory.ProgressChanged += (object bw_sender, ProgressChangedEventArgs bw_e) =>
                {
                    destroy_list_data.Add(new DestroyListView((FileInfo)bw_e.UserState));
                };
                add_files_in_directory.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
                {
                    files_list.Items.Refresh();
                    main_grid.IsEnabled = true;
                };
                add_files_in_directory.RunWorkerAsync();
            }
        }

        private void AddDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                main_grid.IsEnabled = false;
                BackgroundWorker add_files_in_directory = new BackgroundWorker();
                add_files_in_directory.WorkerReportsProgress = true;
                add_files_in_directory.DoWork += (object bw_sender, DoWorkEventArgs e_bw) =>
                {
                    GetAllFiles(new DirectoryInfo(path), add_files_in_directory);
                };
                add_files_in_directory.ProgressChanged += (object bw_sender, ProgressChangedEventArgs bw_e) =>
                {
                    destroy_list_data.Add(new DestroyListView((FileInfo)bw_e.UserState));
                };
                add_files_in_directory.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
                {
                    files_list.Items.Refresh();
                    main_grid.IsEnabled = true;
                };
                add_files_in_directory.RunWorkerAsync();
            }
        }

        private void GetAllFiles(DirectoryInfo dir, BackgroundWorker worker)
        {
            try
            {
                var sub_dirs = dir.GetDirectories();
                foreach (var i in sub_dirs)
                {
                    if (CloseWindow)
                        return;
                    try { GetAllFiles(i, worker); }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            FileInfo[] files = null;
            try
            {
                files = dir.GetFiles();
                foreach (var i in files)
                {
                    if (CloseWindow)
                        return;
                    try { worker.ReportProgress(0, i); }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void FilesList_Drop(object sender, DragEventArgs e)
        {
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                if ((data = (string[])e.Data.GetData(DataFormats.FileDrop)).Length == 1 && Directory.Exists(data[0]) )
                    AddDirectory(data[0]);
                else if(data.Length > 0)
                    AddFiles(data);
        }

        private void Destroy_Click(object sender, RoutedEventArgs e)
        {
            if (destroy_list_data.Count > 0)
            {
                if (Messages.ShowMessage("Are you sure you want to destroy all files in list?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "", "These files will permanently lose!") == MyMessageBoxResult.Yes)
                {
                    int repete;
                    bool delete;
                    DestroyMethod method;
                    int.TryParse(repete_tb.Text, out repete);
                    if (repete < 1) repete = 1;
                    else if (repete > 35) repete = 35;
                    delete = delete_files.IsChecked.Value;
                    method = (DestroyMethod)method_cb.SelectedIndex + 1;
                    List<FileInfo> files = new List<FileInfo>();
                    foreach (var i in destroy_list_data)
                        files.Add(i.File);
                    new Operation().StartDestroyFiles(files, method, repete, delete, true);
                    ClearList_Click(null, null);
                    Messages.ShowMessage("Destroying done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                }
            }
            else
                Messages.ShowMessage("Some field is missing.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "List is empty.");
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            //if to remove items > 0.5 total items rebuild list
            //**** test  which fast
            destroy_list_data.RemoveAll(x => files_list.SelectedItems.Contains(x));
            files_list.Items.Refresh();

            //or

            //for (int i = files_list.SelectedItems.Count - 1; i >= 0; i--)
            //    destroy_list_data.RemoveAt(files_list.Items.IndexOf(files_list.SelectedItems[i]));
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            destroy_list_data.Clear();
            files_list.Items.Refresh();
        }

        #endregion

        #region Destroy Disk

        DriveInfo[] drivers;

        private void GetDrivers()
        {
            drive_list.Items.Clear();
            BackgroundWorker get_drivers = new BackgroundWorker();
            drive_list.IsEnabled = destroy_button.IsEnabled = refresh.IsEnabled = false;
            get_drivers.DoWork += (object senderw, DoWorkEventArgs ew) => { try { drivers = DriveInfo.GetDrives(); } catch (Exception) { } };
            get_drivers.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                drive_list.Items.Clear();
                int select_index = -1;
                for (int i = 0; i < drivers.Length; i++)
                    if (drivers[i].IsReady && drivers[i].DriveType != DriveType.CDRom && drivers[i].TotalFreeSpace > 0)
                    {
                        if (selectdrive != null && drivers[i].Name.ToLower() == selectdrive.Name.ToLower())
                            select_index = i;
                        drive_list.Items.Add(drivers[i].Name);
                    }
                drive_list.SelectedIndex = select_index;
                drive_list.IsEnabled = destroy_button.IsEnabled = refresh.IsEnabled = true;
            };
            get_drivers.RunWorkerAsync();
        }

        private void DriveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!starting_window)
                if (drive_list.SelectedIndex > -1)
                {
                    DriveInfo drive = new DriveInfo(drive_list.SelectedItem.ToString());
                    if (drive.IsReady)
                    {
                        drive_icon.Source = Functions.GetDriveImage(drive, 256);
                        drive_name.Text = (drive.VolumeLabel.Equals(string.Empty) ? "Local drive" : drive.VolumeLabel) + string.Format(" ({0})", drive.Name);
                        drive_format.Text = "File system: " + drive.DriveFormat;
                        drive_free_size.Text = "Free space " + Functions.FileSize(drive.AvailableFreeSpace, 2);
                        avalible_size_rec.Width = (total_size_rec.ActualWidth - 2) * (drive.TotalSize - drive.TotalFreeSpace) / drive.TotalSize;
                        avalible_size_rec.ToolTip = "Used space: " + Functions.FileSize(drive.TotalSize - drive.TotalFreeSpace, 2);
                        total_size_rec.ToolTip = "Total space: " + Functions.FileSize(drive.TotalSize, 2);
                    }
                    else
                        GetDrivers();
                }
                else
                {
                    total_size_rec.ToolTip = avalible_size_rec.ToolTip = string.Empty;
                    drive_icon.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/warning-grey.png"));
                    drive_name.Text = "-";
                    drive_format.Text = "-";
                    drive_free_size.Text = "-";
                    avalible_size_rec.Width = 0;
                }
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            GetDrivers();
        }

        private void DestroyDisk_Click(object sender, RoutedEventArgs e)
        {
            if (drive_list.SelectedIndex > -1)
            {
                DriveInfo drive = new DriveInfo(drive_list.SelectedItem.ToString());
                if (drive.IsReady)
                {
                    if (Messages.ShowMessage("Are you sure you want to destroy all deleted files in '" + drive.Name + "'?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "", "Delete files will be unrecoverable!") == MyMessageBoxResult.Yes)
                    {
                        int repeat;
                        bool delete;
                        DestroyMethod method;
                        int.TryParse(disk_repete_tb.Text, out repeat);
                        if (repeat < 1) repeat = 1;
                        else if (repeat > 35) repeat = 35;
                        delete = disk_delete_files.IsChecked.Value;
                        method = (DestroyMethod)(disk_method_cb.SelectedIndex + 1);
                        new Operation().StartDestroyDisk(drive, method, repeat, delete, true);
                        Messages.ShowMessage("Destroying done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                        DriveList_SelectionChanged(null, null);
                    }
                }
                else
                {
                    GetDrivers();
                    Messages.ShowMessage("Your selected drive seem to is not avilable now, please re-select the right drive then try again.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                }
            }
            else
                Messages.ShowMessage("You should select drive letter first.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            files_list.DataContext = destroy_list_data;
            rec.Width = label1.ActualWidth;
            starting_window = false;
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
            CurrentPage = (DestroyPages)tab_panal.Children.IndexOf((Label)sender) + 1;
        }

        private void AboutTool_Click(object sender, RoutedEventArgs e)
        {
            //version 1:
            //basic files destroyer
            //1.1:
            //seure method added
            //1.2:
            //new interface
            //deleted files desrtoyer
            //2:
            //improve interface:
            //improve speed and can now pause op
            //fast (zero an one) method
            //2.1:
            //fast (random) method
            //fixed bug

        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!starting_window)
            {
                //if(current_page==DestroyPages.DiskDestroy)//****
                double t = (-1 * ((int)CurrentPage - 1)) * e.NewSize.Width;
                ThicknessAnimation ta = new ThicknessAnimation(new Thickness(t, 0, 0, 0), new Duration(new TimeSpan(0)));
                mother.BeginAnimation(MarginProperty, ta);
                mother.Margin = new Thickness(t, 0, 0, 0);
                for (int i = 0; i < mother.Children.Count; i++)
                {
                    ((Grid)mother.Children[i]).Width = ActualWidth - 5;
                    ((Grid)mother.Children[i]).Margin = new Thickness(i * e.NewSize.Width, 0, 0, 0);
                }
            }
        }

    }

    internal class DestroyListView : GeneralPurpose
    {
        public DestroyListView(FileInfo file)
        {
            FullPath = file.FullName;
            Image = Functions.GetFileImage24(file.Extension);
            File = file;
        }
    }

    enum DestroyPages
    {
        FilesDestroy = 1,
        DiskDestroy = 2
    }

    internal enum DestroyMethod
    {
        FastZero = 1,
        FastOne = 2,
        FastRandom = 3,
        Secure = 4
    }
}