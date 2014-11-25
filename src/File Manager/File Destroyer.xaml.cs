using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for File_Destroyer.xaml
    /// </summary>
    public partial class File_Destroyer : Window
    {
        bool CloseWindow = false;

        char[] Chars = { '$', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '&', '#', '%', '^', '(', ')', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
        int buf_size = Environment.Is64BitOperatingSystem ? 65535 : 32766;

        public File_Destroyer()
        {
            InitializeComponent();
            files_list.DataContext = list_to_destroy;
            Refresh_Click(null, null);
        }

        byte[] GetBuffer(bool method, int case_number)
        {
            byte[] buffer = new byte[buf_size];
            if (method)
                for (int j = 0; j < buf_size; j++)
                    buffer[j] = 0x00;
            else
            {
                switch (case_number)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 31:
                    case 32:
                    case 33:
                    case 34:
                        Random rand = new Random((int)DateTime.Now.Ticks);
                        rand.NextBytes(buffer);
                        break;
                    case 4:
                        for (int k = 0; k < buf_size; k++)
                            buffer[k] = 0x55;
                        break;
                    case 5:
                        for (int k = 0; k < buf_size; k++)
                            buffer[k] = 0xAA;
                        break;
                    case 6:
                    case 25:
                        for (int k = 2; k < buf_size; k += 3)
                        { buffer[k] = 0x92; buffer[k - 1] = 0x49; buffer[k - 2] = 0x24; }
                        break;
                    case 7:
                    case 26:
                        for (int k = 2; k < buf_size; k += 3)
                        { buffer[k] = 0x49; buffer[k - 1] = 0x24; buffer[k - 2] = 0x92; }
                        break;
                    case 8:
                    case 27:
                        for (int k = 2; k < buf_size; k += 3)
                        { buffer[k] = 0x24; buffer[k - 1] = 0x92; buffer[k - 2] = 0x49; }
                        break;
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:
                    case 16:
                    case 17:
                    case 18:
                    case 19:
                    case 20:
                    case 21:
                    case 22:
                    case 23:
                    case 24:
                        for (int k = 0; k < buf_size; k++)
                            buffer[k] = (byte)(0x11 * (k - 9));
                        break;
                    case 28:
                        for (int k = 2; k < buf_size; k += 3)
                        { buffer[k] = 0x6D; buffer[k - 1] = 0xB6; buffer[k - 2] = 0xDB; }
                        break;
                    case 29:
                        for (int k = 2; k < buf_size; k += 3)
                        { buffer[k] = 0xB6; buffer[k - 1] = 0xDB; buffer[k - 2] = 0x6D; }
                        break;
                    case 30:
                        for (int k = 2; k < buf_size; k += 3)
                        { buffer[k] = 0xDB; buffer[k - 1] = 0x6d; buffer[k - 2] = 0xB6; }
                        break;
                }
            }
            return buffer;
        }

        byte WriteFile(DestroyListView item, byte[] buffer, long size, bool never_pause)
        {
            try
            {
                BinaryWriter BW = new BinaryWriter(File.OpenWrite(item.File.FullName));
                long k;
                for (k = buffer.Length; k <= size; k += buffer.Length)
                {
                    while (pause && !never_pause)
                        Thread.Sleep(200);
                    BW.Write(buffer);
                    if (never_pause)
                        item.CurrentProgress += buffer.Length;
                    else
                        completed_progress_d += buffer.Length;
                }
                long remain = (size - (k - buffer.Length));
                for (int i = 0; i < remain; i++)
                    BW.Write(buffer[i]);
                if (never_pause)
                    item.CurrentProgress += remain;
                else
                    completed_progress_d += remain;

                BW.Close();
                return 1;
            }
            catch (Exception) { return 0; }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        #region Destroy Files

        ObservableCollection<DestroyListView> list_to_destroy = new ObservableCollection<DestroyListView>();
        int complete, repete;
        bool delete, method;
        List<FileInfo> Undone;
        BackgroundWorker add_directory_BackgroundWorker, destroy_files_BackgroundWorker;

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                foreach (string i in OFD.FileNames)
                {
                    FileInfo file = new FileInfo(i);
                    list_to_destroy.Add(new DestroyListView(i, Functions.GetFileImage24(file.Extension), file));
                    items_count.Text = list_to_destroy.Count + (list_to_destroy.Count > 1 ? " Item" : " Items");
                }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                destroy_control.IsEnabled = false;
                add_directory_BackgroundWorker = new BackgroundWorker();
                add_directory_BackgroundWorker.WorkerSupportsCancellation = true;
                add_directory_BackgroundWorker.WorkerReportsProgress = true;
                add_directory_BackgroundWorker.DoWork += (object bw_sender, DoWorkEventArgs e_bw) => { GetAllFiles(new DirectoryInfo(path)); };
                add_directory_BackgroundWorker.ProgressChanged += AddFilesBW_ProgressChanged;
                add_directory_BackgroundWorker.RunWorkerCompleted += AddFilesBW_RunWorkerCompleted;
                add_directory_BackgroundWorker.RunWorkerAsync();
            }
        }

        private void GetAllFiles(DirectoryInfo dir)
        {
            try
            {
                var sub_dirs = dir.GetDirectories();
                foreach (var i in sub_dirs)
                    try { GetAllFiles(i); } catch (Exception) { }
            }
            catch (Exception) { }
            FileInfo[] files = null;
            try
            {
                files = dir.GetFiles();
                foreach (var i in files)
                    try { add_directory_BackgroundWorker.ReportProgress(0, i); } catch (Exception) { }
            }
            catch (Exception) { }
        }

        void AddFilesBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileInfo i = (FileInfo)e.UserState;
            try
            { list_to_destroy.Add(new DestroyListView(i.FullName, Functions.GetFileImage24(i.Extension), i)); }
            catch (Exception) { }
        }

        void AddFilesBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (list_to_destroy.Count > 0)
                items_count.Text = list_to_destroy.Count + (list_to_destroy.Count > 1 ? " Items" : " Item");
            else
                items_count.Text = "";
            destroy_control.IsEnabled = true;
        }

        private void Destroy_Click(object sender, RoutedEventArgs e)
        {
            if (list_to_destroy.Count > 0)
                if (new My_MessageBox("Confirmation", "Are you sure you want to destroy all files in list?", "Caution: these files will permanently lose!", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                {
                    complete = 0;
                    destroy_control.IsEnabled = false;
                    Undone = new List<FileInfo>();
                    destroy_files_BackgroundWorker = new BackgroundWorker();
                    destroy_files_BackgroundWorker.WorkerSupportsCancellation = true;
                    destroy_files_BackgroundWorker.WorkerReportsProgress = true;
                    destroy_files_BackgroundWorker.DoWork += DestroyBW_DoWork;
                    destroy_files_BackgroundWorker.ProgressChanged += DestroyBW_ProgressChanged;
                    destroy_files_BackgroundWorker.RunWorkerCompleted += DestroyBW_RunWorkerCompleted;
                    delete = delete_files.IsChecked.Value;
                    method = method_cb.SelectedIndex == 0 ? true : false;
                    int.TryParse(repete_tb.Text, out repete);
                    if (repete < 1) repete = 1;
                    else if (repete > 999) repete = 999;
                    repete_tb.Text = repete.ToString();
                    total_progress.Visibility = Visibility.Visible;
                    total_progress.Value = 0;
                    total_progress.Maximum = list_to_destroy.Count;
                    destroy_files_BackgroundWorker.RunWorkerAsync();
                }
        }

        private void DestroyBW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            Thread process_thread = new Thread(() =>
            {
                while (bw.IsBusy)
                {
                    bw.ReportProgress(0);
                    Thread.Sleep(200);
                }
            });
            process_thread.Start();
            for (int index = 0; index < list_to_destroy.Count; index++)
            {
                list_to_destroy[index].CurrentProgress = 0;
                try
                {
                    string new_name = GetFileName(list_to_destroy[index].File.Directory.FullName);
                    while (File.Exists(new_name) || Directory.Exists(new_name))
                        new_name = GetFileName(list_to_destroy[index].File.Directory.FullName);
                    list_to_destroy[index].File.MoveTo(new_name);
                }
                catch (Exception) { continue; }
                int dones = 0;
                if (method)
                {
                    list_to_destroy[index].MaximumValue = list_to_destroy[index].File.Length * repete;
                    for (int i = 0; i < repete; i++)
                        WriteFile(list_to_destroy[index], GetBuffer(method, 0), list_to_destroy[index].File.Length, true);
                    dones = 35;
                }
                else
                {
                    list_to_destroy[index].MaximumValue = list_to_destroy[index].File.Length * repete * 35;
                    for (int i = 0; i < repete; i++)
                        for (int j = 0; j < 35; j++)
                            dones += WriteFile(list_to_destroy[index], GetBuffer(method, j), list_to_destroy[index].File.Length, true);
                }
                list_to_destroy[index].CurrentProgress = list_to_destroy[index].MaximumValue;
                try
                {
                    if (dones > 30 && delete)
                        list_to_destroy[index].File.Delete();
                    complete++;
                }
                catch (Exception) { }
            }
        }

        private void DestroyBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            files_list.Items.Refresh();
            total_progress.Value = complete;
        }

        private void DestroyBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            destroy_control.IsEnabled = true;
            total_progress.Visibility = Visibility.Hidden;
            files_list.Items.Refresh();
            new My_MessageBox("FILE MANAGER", "Operation done.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            for (int i = files_list.SelectedItems.Count - 1; i >= 0; i--)
                list_to_destroy.RemoveAt(files_list.Items.IndexOf(files_list.SelectedItems[i]));
            if (list_to_destroy.Count > 0)
                items_count.Text = list_to_destroy.Count + (list_to_destroy.Count == 1 ? " Item" : " Items");
            else
                items_count.Text = string.Empty;
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            list_to_destroy.Clear();
            items_count.Text = "";
        }

        #endregion

        #region Destroy Disk

        bool method2, pause = false;
        DriveInfo drive;
        long completed_progress_d = 0;
        byte file_creating_error = 0;

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                completed_progress_d = 0;
                total_MB.Text = "";
                drive_list.Items.Clear();
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (var i in drives)
                    if (i.IsReady && i.DriveType != DriveType.CDRom && i.TotalFreeSpace > 0)
                        drive_list.Items.Add(i.Name);
            }
            catch (Exception ex)
            { new My_MessageBox("FILE MANAGER", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void DriveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (drive_list.SelectedIndex > -1)
            {
                drive = new DriveInfo(drive_list.SelectedItem.ToString());
                total_MB.Text = Functions.FileSize(drive.TotalFreeSpace, 2);
                completed_MB.Text = "0 byte ";
                total_progress_disk.Maximum = drive.TotalFreeSpace;
                total_progress.Value = 0;
            }
        }

        private void DestroyDisk_Click(object sender, RoutedEventArgs e)
        {
            if (drive_list.SelectedIndex > -1)
            {
                if (new My_MessageBox("Confirmation", "Are you sure you want to destroy all deleted files in '" + drive.Name + "'?", "Caution: these files will permanently lose!", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                {
                    DisableEnableControls(false);
                    file_creating_error = 0;
                    Undone = new List<FileInfo>();
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.WorkerSupportsCancellation = true;
                    bw.WorkerReportsProgress = true;
                    bw.DoWork += DestroyDiskBW_DoWork;
                    bw.ProgressChanged += DestroyDiskBW_ProgressChanged;
                    bw.RunWorkerCompleted += DestroyDiskBW_RunWorkerCompleted;
                    method2 = method_cb2.SelectedIndex == 0 ? true : false;
                    total_MB.Text = Functions.FileSize(drive.TotalFreeSpace, 2);
                    total_progress_disk.Maximum = drive.TotalFreeSpace - 1024.0;
                    bw.RunWorkerAsync();
                }
            }
            else
                new My_MessageBox("File Manager", "Select Disk Drive First", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void DisableEnableControls(bool p)
        {
            drive_list.IsEnabled = refresh.IsEnabled = method_cb2.IsEnabled = destroy_button.IsEnabled = p;
        }

        private void DestroyDiskBW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            Thread process_thread = new Thread(() =>
            {
                while (bw.IsBusy)
                {
                    bw.ReportProgress(0);
                    Thread.Sleep(200);
                }
            });
            process_thread.Start();
            List<string> files_name = new List<string>();
            while (drive.IsReady && drive.TotalFreeSpace > 1073742848 && file_creating_error < 3)
                PrepareAndWriteFile(files_name, bw, 1073741824);
            while (drive.IsReady && drive.TotalFreeSpace > 0 && drive.AvailableFreeSpace > 1025 && file_creating_error < 3)
                PrepareAndWriteFile(files_name, bw, drive.AvailableFreeSpace - 1024);
            if (file_creating_error > 2)
                bw.ReportProgress(-100, "Can not destrory deleted files on this drive, try to cleaning up...");
            int errors = 0;
            StringBuilder message = new StringBuilder();
            message.AppendLine("Some files have not deleted successfull you can delete them manually:");
            for (int i = files_name.Count - 1; i <= 0; i--)
                try
                {
                    File.Delete(files_name[i]);
                    files_name.RemoveAt(i);
                }
                catch (Exception) { errors++; message.AppendLine(files_name[i]); }
            if (errors > 0)
                bw.ReportProgress(-100, message);
        }

        private void PrepareAndWriteFile(List<string> files_name, BackgroundWorker bw, long size)
        {
            FileInfo fi = new FileInfo(GetFileName(drive.Name));
            if (!method2)
                if (WriteFile(new DestroyListView("", Functions.GetFileImage24(fi.Extension), fi), GetBuffer(method2, 0), size, false) == 1)
                    files_name.Add(fi.FullName);
                else
                { bw.ReportProgress(-100, "Something wronge while creating file \"" + fi.FullName + "\""); file_creating_error++; }
            else
            {
                int count = 0;
                for (int k = 0; k < 35; k++)
                    count += WriteFile(new DestroyListView("", Functions.GetFileImage24(fi.Extension), fi), GetBuffer(method2, k), size, false);
                if (count > 25)
                    files_name.Add(fi.FullName);
                else
                { bw.ReportProgress(-100, "Something wronge while creating file \"" + fi.FullName + "\""); file_creating_error++; }
            }
        }

        string GetFileName(string parent)
        {
            string name = "";
            while (true)
            {
                name = parent + "\\$";
                Random rand = new Random((int)DateTime.Now.Ticks);
                for (int i = 0; i < 7; i++)
                    name += Chars[rand.Next(Chars.Length - 1)];
                name += ".";
                for (int i = 0; i < 3; i++)
                    name += Chars[rand.Next(Chars.Length - 1)];
                if (!File.Exists(name) && !Directory.Exists(name))
                    break;
            }
            return name;
        }

        private void DestroyDiskBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
            {
                completed_MB.Text = Functions.FileSize(completed_progress_d, 2);
                total_progress_disk.Value = completed_progress_d;
            }
            else if (e.ProgressPercentage == -100)
                new My_MessageBox("FILE MANAGER", e.UserState.ToString(), e.UserState is StringBuilder ? new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")) : new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), e.UserState is StringBuilder ? new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav") : new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void DestroyDiskBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            total_progress_disk.Value = total_progress_disk.Maximum;
            completed_MB.Text = total_MB.Text;
            DisableEnableControls(true);
            new My_MessageBox("FILE MANAGER", "Operation done.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (pause)
                play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/copy-pause.png"));
            else
                play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/copy-start.png"));
            pause = !pause;
        }

        #endregion
    }

    class DestroyListView : GeneralPurpose
    {
        public DestroyListView(string file_path, BitmapImage img_source, FileInfo file)
        {
            if (file_path == "")
                FullPath = file.FullName;
            FullPath = file_path;
            Image = img_source;
            File = file;
            ProgressBarVisibility = Visibility.Visible;
        }

        private String error_massege;
        public string ErrorMassege
        {
            get { return error_massege; }
            set { error_massege = value; }
        }

        Visibility progressbar_bisibility;
        public Visibility ProgressBarVisibility
        {
            get { return progressbar_bisibility; }
            set { progressbar_bisibility = value; }
        }

        Visibility textblock_bisibility;
        public Visibility TextBlockVisibility
        {
            get { return textblock_bisibility; }
            set { textblock_bisibility = value; }
        }

        private string file_path;

        private BitmapImage img_source;

        private double current_progress;
        public double CurrentProgress
        {
            get { return current_progress; }
            set { current_progress = value; }
        }

        private double maximum_value;
        public double MaximumValue
        {
            get { return maximum_value; }
            set { maximum_value = value; }
        }
    }
}