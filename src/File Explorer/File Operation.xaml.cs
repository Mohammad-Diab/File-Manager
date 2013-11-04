using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Explorer
{
    /// <summary>
    /// Interaction logic for File_Operation.xaml
    /// </summary>
    public partial class File_Operation : Window
    {
        int complete = 0, buffer = 65536;
        string _name = "", paste_in;
        double process_bar = 0, old_process;
        long files_size = 0, old_ticks;
        bool going_on = true, task_off = false, is_play = true;
        bool? copy = true;
        bool?[] file_attributes;
        BackgroundWorker worker;
        Thread thread;
        List<FileElement> to_copy_list;
        List<UnCompleatOperation> undone, exists_error, undeleted_files;
        List<string> files, folders;
        List<int> overrite;
        List<bool> retry;
        BinaryWriter BW;
        BinaryReader BR;
        private List<FileElement> attributes_files = new List<FileElement>();
        private List<string> attributes_dirs = new List<string>();
        public File_Operation()
        {
            to_copy_list = new List<FileElement>();
            foreach (var i in MainWindow.to_copy_files)
                to_copy_list.Add(i);
            InitializeComponent();
        }

        public File_Operation(List<FileElement> to_copy)
        {
            to_copy_list = new List<FileElement>();
            foreach (var i in to_copy)
                to_copy_list.Add(i);
            InitializeComponent();
        }

        public File_Operation(List<string> files, List<string> folders)
        {
            InitializeComponent();
            copy = false;
            op.Text = "Deleting: ";
            files_size = files.Count + folders.Count;
            this.files = files;
            this.folders = folders;
        }

        public File_Operation(List<FileElement> files, List<string> dirs, bool?[] file_attributes)
        {
            InitializeComponent();
            copy = null;
            op.Text = "Applying attributes: ";
            foreach (var i in files)
                attributes_files.Add(i);
            foreach (var i in dirs)
                attributes_dirs.Add(i);
            this.file_attributes = file_attributes;
            files_size = files.Count + dirs.Count;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (undone.Count == 0 && exists_error.Count == 0 && undeleted_files.Count == 0)
            {
                if (copy == true)
                    MainWindow.is_paste = true;
                Close();
            }
            else
            {
                going_on = true;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                PB.Foreground = new LinearGradientBrush(Color.FromRgb(218, 38, 38), Color.FromRgb(220, 40, 40), 30);
                if (exists_error.Count > 0)
                {
                    overrite = new Exists_Files_Window(exists_error, paste_in).ShowThisDialog();
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    PB.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
                    NewWorker();
                    to_copy_list = new List<FileElement>();
                    for (int i = overrite.Count - 1; i >= 0; i--)
                        if (overrite[i] == 1)
                            to_copy_list.Add(new FileElement(exists_error[i].files_full_name, exists_error[i].files_size, exists_error[i].extension, exists_error[i].file_short_name, exists_error[i].remove_file, true));
                        else if (overrite[i] == 2)
                            to_copy_list.Add(new FileElement(exists_error[i].files_full_name, exists_error[i].files_size, exists_error[i].extension, exists_error[i].file_short_name, exists_error[i].remove_file, null));
                        else if (overrite[i] == 0)
                            process_bar += exists_error[i].files_size;
                    exists_error.Clear();
                    thread = new Thread(FileMoveOrCopy);
                }
                else if (undone.Count > 0)
                {
                    if (copy == true)
                        retry = new File_s_transferring_Error(undone, null).ShowThisDialog();
                    else if (copy == false)
                        retry = new File_s_transferring_Error(undone, "Some items have not deleted successfully.<note: default option is skip>").ShowThisDialog();
                    else
                        retry = new File_s_transferring_Error(undone, "We can not apply attributes on some Items.<note: default option is skip>").ShowThisDialog();
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    PB.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
                    NewWorker();
                    if (copy == true)
                    {
                        to_copy_list = new List<FileElement>();
                        for (int i = retry.Count - 1; i >= 0; i--)
                            if (retry[i])
                                to_copy_list.Add(new FileElement(undone[i].files_full_name, undone[i].files_size, undone[i].extension, undone[i].file_short_name, undone[i].remove_file, false));
                            else
                            {
                                process_bar += undone[i].files_size;
                                complete++;
                            }
                        thread = new Thread(FileMoveOrCopy);
                    }
                    else if (copy == false)
                    {
                        files.Clear();
                        folders.Clear();
                        for (int i = retry.Count - 1; i >= 0; i--)
                            if (retry[i])
                                if (undone[i].IsDirectory)
                                    folders.Add(undone[i].files_full_name);
                                else
                                    files.Add(undone[i].files_full_name);
                            else
                                process_bar++;
                        thread = new Thread(Delete);
                    }
                    else
                    {
                        attributes_files.Clear();
                        attributes_dirs.Clear();
                        for (int i = retry.Count - 1; i >= 0; i--)
                            if (retry[i])
                                if (undone[i].IsDirectory)
                                    attributes_dirs.Add(undone[i].files_full_name);
                                else
                                    attributes_files.Add(new FileElement(undone[i].files_full_name, undone[i].file_short_name, undone[i].remove_file));
                            else
                                process_bar++;
                        thread = new Thread(SaveAttributes);
                    }
                    undone.Clear();
                }
                else if (undeleted_files.Count > 0)
                {
                    retry = new File_s_transferring_Error(undeleted_files, null).ShowThisDialog();
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    PB.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
                    NewWorker();
                    to_copy_list.Clear();
                    thread = new Thread(Undeleted);
                }
                NewThread();
            }
        }

        private void NewWorker()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (going_on)
            {
                while (task_off)
                    System.Threading.Thread.Sleep(500);
                System.Threading.Thread.Sleep(100);
                worker.ReportProgress(0);
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            PB.Value = process_bar;
            double val = PB.Value * 100 / files_size;
            percent.Text = Math.Round(val, 0).ToString();
            TaskbarItemInfo.ProgressValue = val / 100;
            if (copy == true)
                completed.Text = complete.ToString();
            else
                completed.Text = process_bar.ToString();
            long new_ticks = DateTime.UtcNow.Ticks;
            if (old_process != 0)
                if (copy == true)
                    speed.Text = Functions.File_Size((long)((process_bar - old_process) / ((new_ticks - old_ticks) / 10000000))) + " per second";
                else
                    speed.Text = (process_bar - old_process) / ((new_ticks - old_ticks) / 10000000) + " items per second";
            old_process = process_bar;
            old_ticks = new_ticks;
            item_name.ToolTip = item_name.Text = _name;
        }

        private void NewThread()
        {
            thread.IsBackground = true;
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            undone = new List<UnCompleatOperation>();
            exists_error = new List<UnCompleatOperation>();
            undeleted_files = new List<UnCompleatOperation>();
            NewWorker();
            if (copy == true)
            {
                to.Text = " to \"" + paste_in + "\"";
                TextBlock1.ToolTip = paste_in;
                thread = new Thread(FileMoveOrCopy);
                foreach (var i in to_copy_list)
                    files_size += i.files_size;
                items_number.Text = to_copy_list.Count + "";
            }
            else if (copy == false)
            {
                thread = new Thread(Delete);
                items_number.Text = files_size + "";
            }
            else
            {
                thread = new Thread(SaveAttributes);
                items_number.Text = files_size + "";
            }
            PB.Maximum = files_size;
            NewThread();
        }

        private void SaveAttributes()
        {
            foreach (var i in attributes_files)
            {
                while (task_off)
                    Thread.Sleep(500);
                _name = i.files_full_name;
                SetAttributesToFile(i.files_full_name);
            }
            foreach (var i in attributes_dirs)
            {
                while (task_off)
                    Thread.Sleep(500);
                _name = i;
                SetAttributesToDirectory(i);
            }
            going_on = false;
        }

        private void Undone()
        {
            if (undone.Count > 0)
            {
                var retry = new File_s_transferring_Error(undone, "We can not apply attributes on some Items").ShowThisDialog();
                for (int i = retry.Count - 1; i > -1; i--)
                {
                    if (retry[i])
                        if (undone[i].IsDirectory)
                            SetAttributesToDirectory(undone[i].files_full_name);
                        else
                            SetAttributesToFile(undone[i].files_full_name);
                    undone.RemoveAt(i);
                }
                Undone();
            }
        }

        private void SetAttributesToFile(string file_name)
        {
            try
            {
                if (file_attributes[7] == true)
                    File.SetAttributes(file_name, FileAttributes.Normal);
                else
                {
                    FileAttributes default_attributes = File.GetAttributes(file_name);
                    var Newattribute = CalcFileAttributes(0, default_attributes);
                    File.SetAttributes(file_name, Newattribute);
                    if (default_attributes.HasFlag(FileAttributes.Encrypted) != Newattribute.HasFlag(FileAttributes.Encrypted))
                        if (Newattribute.HasFlag(FileAttributes.Encrypted))
                            File.Encrypt(file_name);
                        else
                            File.Decrypt(file_name);
                }
                process_bar++;
            }
            catch (Exception ex) { undone.Add(new UnCompleatOperation(file_name, ex.Message, false)); }
        }

        private void SetAttributesToDirectory(string directory_name)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(directory_name);
                if (file_attributes[7] == true)
                    dir.Attributes = (FileAttributes.Normal + 16);
                else
                {
                    FileAttributes new_attributes = 0;
                    new_attributes += 16;
                    FileAttributes default_attributes = dir.Attributes;
                    dir.Attributes = CalcFileAttributes(new_attributes, default_attributes);
                }
                process_bar++;
            }
            catch (Exception ex) { undone.Add(new UnCompleatOperation(directory_name, ex.Message, true)); }
        }

        private FileAttributes CalcFileAttributes(FileAttributes start, FileAttributes default_attributes)
        {
            FileAttributes new_attributes = start;
            if (file_attributes[0] == true)
                new_attributes += 1;
            if (file_attributes[1] == true)
                new_attributes += 2;
            if (file_attributes[2] == true)
                new_attributes += 4;
            if (file_attributes[3] == true)
                new_attributes += 32;
            if (default_attributes.HasFlag(FileAttributes.Temporary))
                new_attributes += 256;
            if (default_attributes.HasFlag(FileAttributes.SparseFile))
                new_attributes += 512;
            if (default_attributes.HasFlag(FileAttributes.ReparsePoint))
                new_attributes += 1024;
            if (file_attributes[4] == true)
                new_attributes += 2048;
            if (default_attributes.HasFlag(FileAttributes.Offline))
                new_attributes += 4096;
            if (file_attributes[5] == true)
                new_attributes += 16384;
            if (file_attributes[6] == true)
                new_attributes += 131072;
            return new_attributes;
        }

        private void FileMoveOrCopy()
        {
            for (int i = 0; i < to_copy_list.Count; i++)
            {
                try
                {
                    _name = to_copy_list[i].files_full_name;
                    string[] directory_paths = Functions.SplitString(to_copy_list[i].file_short_name);
                    for (int j = 0; j < directory_paths.Length - 1; j++)
                        if (!Directory.Exists(paste_in + "\\" + directory_paths[j]))
                            Directory.CreateDirectory(paste_in + "\\" + directory_paths[j]);
                    if ((to_copy_list[i].remove_file || (to_copy_list[i].over == true)) && to_copy_list[i].files_full_name.Equals(paste_in + to_copy_list[i].file_short_name))
                        continue;
                    if (to_copy_list[i].over == false && File.Exists(paste_in + to_copy_list[i].file_short_name))
                    {
                        exists_error.Add(new UnCompleatOperation(to_copy_list[i].files_full_name, to_copy_list[i].files_size, to_copy_list[i].extension, to_copy_list[i].file_short_name, to_copy_list[i].remove_file, "File is already exists."));
                        continue;
                    }
                    else
                    {
                        WriteFile(to_copy_list[i]);
                        to_copy_list[i].done = true;
                    }
                    if (to_copy_list[i].done && to_copy_list[i].remove_file && File.Exists(to_copy_list[i].files_full_name) && to_copy_list[i].files_full_name[0] != paste_in[0])
                        try
                        { File.Delete(to_copy_list[i].files_full_name); }
                        catch (Exception) { undeleted_files.Add(new UnCompleatOperation(to_copy_list[i].files_full_name, to_copy_list[i].files_size, to_copy_list[i].file_short_name, to_copy_list[i].extension, to_copy_list[i].remove_file, "File have not been deleted")); }
                    while (task_off)
                        Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    if (File.Exists(paste_in + "\\" + to_copy_list[i].file_short_name))
                        try
                        { if (BW != null) BW.Close(); File.Delete(paste_in + "\\" + to_copy_list[i].file_short_name); }
                        catch (Exception) { }
                    undone.Add(new UnCompleatOperation(to_copy_list[i].files_full_name, to_copy_list[i].files_size, to_copy_list[i].extension, to_copy_list[i].file_short_name, to_copy_list[i].remove_file, ex.Message));
                }
            }
            going_on = false;
        }

        private void WriteFile(FileElement i)
        {
            string new_path = paste_in + "\\" + i.file_short_name;
            if (File.Exists(paste_in + "\\" + i.file_short_name))
                if (i.over == true)
                    File.Delete(paste_in + "\\" + i.file_short_name);
                else
                {
                    FileInfo source_file = new FileInfo(new_path);
                    string extention = source_file.Extension;
                    string name;
                    if (extention.Length > 0)
                        name = source_file.Name.Remove(source_file.Name.Length - extention.Length);
                    else
                        name = source_file.Name;
                    int num = 1;
                    while (File.Exists(paste_in + "\\" + name + " (" + num + ")" + extention)) num++;
                    new_path = paste_in + "\\" + name + " (" + num + ")" + extention;
                }
            if (i.files_full_name[0] == paste_in[0] && i.remove_file)
            {
                File.Move(i.files_full_name, new_path);
                process_bar += i.files_size;
            }
            else
            {
                BR = new BinaryReader(File.OpenRead(i.files_full_name));
                _name = i.files_full_name;
                BW = new BinaryWriter(File.OpenWrite(new_path));
                long k;
                for (k = buffer; k <= i.files_size; k += buffer)
                {
                    while (task_off)
                        Thread.Sleep(500);
                    BW.Write(BR.ReadBytes(buffer));
                    process_bar += buffer;
                }
                while (task_off)
                    Thread.Sleep(500);
                long remain = (i.files_size - (k - buffer));
                BW.Write(BR.ReadBytes((int)remain));
                process_bar += remain;
                File.SetAttributes(new_path, File.GetAttributes(i.files_full_name));
                complete++;
                BW.Close();
                BR.Close();
            }
        }

        private void Delete()
        {
            foreach (var i in files)
            {
                while (task_off)
                    Thread.Sleep(500);
                _name = i;
                DeleteFile(i, undone);
            }
            foreach (var i in folders)
            {
                while (task_off)
                    Thread.Sleep(500);
                _name = i;
                DeleteDirectory(i, undone);
            }
            going_on = false;
        }

        private void DeleteFile(string file_name, List<UnCompleatOperation> undone)
        {
            try
            { File.Delete(file_name); process_bar++; }
            catch (Exception ex)
            { undone.Add(new UnCompleatOperation(file_name, ex.Message, false)); }
        }

        private void DeleteDirectory(string directory_name, List<UnCompleatOperation> undone)
        {
            try
            { Directory.Delete(directory_name, false); process_bar++; }
            catch (Exception ex)
            { undone.Add(new UnCompleatOperation(directory_name, ex.Message, true)); }
        }

        private void Undeleted()
        {
            for (int i = undeleted_files.Count - 1; i >= 0; i--)
            {
                if (retry[i])
                    try
                    { File.Delete(undeleted_files[i].files_full_name); }
                    catch (Exception)
                    { undeleted_files.Add(new UnCompleatOperation(undeleted_files[i].files_full_name, undeleted_files[i].files_size, undeleted_files[i].extension, undeleted_files[i].file_short_name, undeleted_files[i].remove_file, "File have not been deleted")); }
                undeleted_files.RemoveAt(i);
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (is_play)
                Pause();
            else
                Start();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Start_ThumbButtonInfo_Click(object sender, EventArgs e)
        {
            Start();
        }

        private void Pause_ThumbButtonInfo_Click(object sender, EventArgs e)
        {
            Pause();
        }

        private void Stop_ThumbButtonInfo_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void Stop()
        {
            if (copy == true)
                MainWindow.is_paste = true;
            try
            {
                thread.Abort();
                BW.Close();
                Close();
            }
            catch (Exception) { }
        }

        private void Start()
        {
            if (TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error)
            {
                if (!is_play)
                {
                    play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/copy-pause.png"));
                    task_off = false;
                    is_play = true;
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    PB.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
                }
            }
        }

        private void Pause()
        {
            if (is_play == true && TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error)
            {
                is_play = false;
                play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/copy-start.png"));
                task_off = true;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
                PB.Foreground = new SolidColorBrush(Color.FromRgb(218, 203, 38));
            }
        }

        internal void Show(string path)
        {
            paste_in = path;
            this.Show();
        }
    }
}
