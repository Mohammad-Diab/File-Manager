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
        double process_bar = 0;
        long files_size;
        bool? is_play = null;
        bool going_on = true;
        BackgroundWorker worker;
        Thread thread;
        string paste_in;
        List<FileElement> to_copy_list = new List<FileElement>();
        List<UnCompleatOperation> undone;
        List<UnCompleatOperation> exists_error;
        List<UnCompleatOperation> undeleted_files;
        List<int> overrite;
        List<bool> retry;
        BinaryWriter BW;
        public File_Operation()
        {
            foreach (var i in MainWindow.to_copy_files)
                to_copy_list.Add(i);
            InitializeComponent();
        }

        public File_Operation(List<FileElement> to_copy)
        {
            foreach (var i in to_copy)
                to_copy_list.Add(i);
            InitializeComponent();
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (undone.Count == 0 && exists_error.Count == 0 && undeleted_files.Count == 0)
            {
                MainWindow.is_paste = true;
                Close();
            }
            else
            {
                if (exists_error.Count > 0)
                {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                    overrite = new Exists_Files_Window(exists_error, paste_in).ShowThisDialog();
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = true;
                    worker.DoWork += worker_DoWork;
                    worker.ProgressChanged += worker_ProgressChanged;
                    worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                    worker.RunWorkerAsync();
                    to_copy_list = new List<FileElement>();
                    for (int i = overrite.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            if (overrite[i] == 1)
                                WriteFile(new FileElement(exists_error[i].files_full_name, exists_error[i].files_size, exists_error[i].file_short_name, exists_error[i].remove_file), true);
                            else if (overrite[i] == 2)
                                WriteFile(new FileElement(exists_error[i].files_full_name, exists_error[i].files_size, exists_error[i].file_short_name, exists_error[i].remove_file), false);
                        }
                        catch (Exception ex)
                        {
                            if (File.Exists(paste_in + "\\" + exists_error[i].file_short_name))
                                try
                                { BW.Close(); File.Delete(paste_in + "\\" + exists_error[i].file_short_name); }
                                catch (Exception) { }
                            undone.Add(new UnCompleatOperation(exists_error[i].files_full_name, exists_error[i].files_size, exists_error[i].file_short_name, exists_error[i].remove_file, ex.Message));
                        }
                        exists_error.RemoveAt(i);
                    }
                }
                else if (undone.Count > 0)
                {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                    retry = new File_s_transferring_Error(undone, null).ShowThisDialog();
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = true;
                    worker.DoWork += worker_DoWork;
                    worker.ProgressChanged += worker_ProgressChanged;
                    worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                    worker.RunWorkerAsync();
                    to_copy_list = new List<FileElement>();
                    for (int i = undone.Count - 1; i >= 0; i--)
                    {
                        if (retry[i])
                            try
                            { WriteFile(new FileElement(undone[i].files_full_name, undone[i].files_size, undone[i].file_short_name, undone[i].remove_file), false); }
                            catch (Exception ex)
                            {
                                if (File.Exists(paste_in + "\\" + undone[i].file_short_name))
                                    try
                                    { if (BW != null) BW.Close(); File.Delete(paste_in + "\\" + undone[i].file_short_name); }
                                    catch (Exception) { }
                                undone.Add(new UnCompleatOperation(undone[i].files_full_name, undone[i].files_size, undone[i].file_short_name, undone[i].remove_file, ex.Message));
                            }
                        undone.RemoveAt(i);
                    }
                }
                else if (undeleted_files.Count > 0)
                {
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                    retry = new File_s_transferring_Error(undeleted_files, null).ShowThisDialog();
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    worker = new BackgroundWorker();
                    worker.WorkerReportsProgress = true;
                    worker.DoWork += worker_DoWork;
                    worker.ProgressChanged += worker_ProgressChanged;
                    worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                    worker.RunWorkerAsync();
                    to_copy_list = new List<FileElement>();
                    for (int i = undeleted_files.Count - 1; i >= 0; i--)
                    {
                        if (retry[i])
                            try
                            { File.Delete(undeleted_files[i].files_full_name); }
                            catch (Exception)
                            { undeleted_files.Add(new UnCompleatOperation(undeleted_files[i].files_full_name, undeleted_files[i].files_size, undeleted_files[i].file_short_name, undeleted_files[i].remove_file, "File have not been deleted")); }
                        undeleted_files.RemoveAt(i);
                    }
                }
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (going_on)
            {
                System.Threading.Thread.Sleep(250);
                worker.ReportProgress(0);
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            PB.Value = process_bar;
            double val = PB.Value * 100 / files_size;
            percent.Text = Math.Round(val, 2).ToString();
            TaskbarItemInfo.ProgressValue = val / 100;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            worker = new BackgroundWorker();
            undone = new List<UnCompleatOperation>();
            exists_error = new List<UnCompleatOperation>();
            undeleted_files = new List<UnCompleatOperation>();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
            thread = new Thread(FileAction);
            thread.Priority = ThreadPriority.Highest;
            files_size = 0;
            foreach (var i in to_copy_list)
                files_size += i.files_size;
            PB.Maximum = files_size;
        }

        private void FileAction()
        {
            process_bar = 0;
            foreach (var i in to_copy_list)
            {
                try
                {
                    string[] directory_paths = Functions.SplitString(i.file_short_name);
                    for (int j = 0; j < directory_paths.Length - 1; j++)
                        if (!Directory.Exists(paste_in + "\\" + directory_paths[j]))
                            Directory.CreateDirectory(paste_in + "\\" + directory_paths[j]);
                    if (File.Exists(paste_in + i.file_short_name))
                    {
                        exists_error.Add(new UnCompleatOperation(i.files_full_name, i.files_size, i.file_short_name, i.remove_file, "File is already exists."));
                        continue;
                    }
                    else
                    {
                        WriteFile(i, false);
                        i.done = true;
                    }
                    if (i.done && i.remove_file && File.Exists(i.files_full_name) && i.files_full_name[0] != paste_in[0])
                        try
                        { File.Delete(i.files_full_name); }
                        catch (Exception) { undeleted_files.Add(new UnCompleatOperation(i.files_full_name, i.files_size, i.file_short_name, i.remove_file, "File have not been deleted")); }
                }
                catch (Exception ex)
                {
                    if (File.Exists(paste_in + "\\" + i.file_short_name))
                        try
                        { if (BW != null) BW.Close(); File.Delete(paste_in + "\\" + i.file_short_name); }
                        catch (Exception) { }
                    undone.Add(new UnCompleatOperation(i.files_full_name, i.files_size, i.file_short_name, i.remove_file, ex.Message));
                }
            }
            going_on = false;
        }

        private void WriteFile(FileElement i, bool over)
        {
            string new_path = paste_in + "\\" + i.file_short_name;
            if (File.Exists(paste_in + "\\" + i.file_short_name))
                if (over)
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
                BinaryReader BR = new BinaryReader(File.OpenRead(i.files_full_name));
                BW = new BinaryWriter(File.OpenWrite(new_path));
                long k;
                for (k = 65536; k <= i.files_size; k += 65536)
                {
                    BW.Write(BR.ReadBytes(65536));
                    process_bar += 65536;
                }
                BW.Write(BR.ReadBytes((int)(i.files_size - (k - 65536))));
                process_bar += (i.files_size - (k - 65536));
                BW.Close();
                BR.Close();
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (is_play == true)
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
            if (TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error)
                if (is_play != null)
                {
                    if (is_play == false)
                        thread.Resume();
                    thread.Abort();
                    going_on = false;
                }
                else
                    Close();
        }

        private void Start()
        {
            if (TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error)
            {
                if (is_play == null)
                    thread.Start();
                else if (is_play == false)
                    thread.Resume();
                if (is_play == null || is_play == false)
                {
                    play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/copy-pause.png"));
                    is_play = true;
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
            }
        }

        private void Pause()
        {
            if (is_play == true && TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error)
            {
                is_play = false;
                play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/copy-start.png"));
                thread.Suspend();
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
            }
        }

        internal void Show(string path)
        {
            paste_in = path;
            this.Show();
        }

        //public void FileAction()
        //{
        //    worker2 = new BackgroundWorker();
        //    worker2.ProgressChanged += worker2_ProgressChanged;
        //    worker2.WorkerReportsProgress = true;
        //    process_bar = 0;
        //    long files_size = 0;
        //    foreach (var i in MainWindow.to_copy_files)
        //        files_size += i.files_size;
        //    foreach (var i in MainWindow.to_copy_files)
        //    {
        //        List<string> directory_paths = i.file_short_name.Split('\\').ToList<string>()/*, StringSplitOptions.RemoveEmptyEntries)*/;
        //        directory_paths.RemoveAt(directory_paths.Count - 1);
        //        directory_paths.RemoveAt(0);
        //        foreach (var j in directory_paths)
        //        {
        //            string tmp = MainWindow.pathh + "\\" + j;
        //            if (!File.Exists(tmp))
        //                Directory.CreateDirectory(tmp);
        //        }
        //        if (i.files_full_name[0] == MainWindow.pathh[0])
        //        {
        //            if (i.remove_file)
        //                File.Move(i.files_full_name, MainWindow.pathh + i.file_short_name);
        //            else
        //                File.Copy(i.files_full_name, MainWindow.pathh + i.file_short_name);
        //            process_bar += (i.files_size + 0.0) / files_size;
        //        }
        //        else
        //        {
        //            BinaryReader BR = new BinaryReader(File.OpenRead(i.files_full_name));
        //            BinaryWriter BW = new BinaryWriter(File.OpenWrite(MainWindow.pathh + i.file_short_name));
        //            long k;
        //            for (k = 0; k < i.files_size; k += 65536)
        //            {
        //                BW.Write(BR.ReadBytes(65536));
        //                process_bar += 6553600.0 / files_size;
        //                if (process_bar > process_bar_persnt + 1)
        //                {
        //                    process_bar_persnt++;
        //                    worker2.ReportProgress(process_bar_persnt);
        //                }
        //                //PB.Value = process_bar;
        //                //new Task(MoveProcessBar).RunSynchronously();
        //                TaskbarItemInfo.ProgressValue = process_bar / 100;
        //            }
        //            BW.Write(BR.ReadBytes((int)(i.files_size - (k - 65536))));
        //            process_bar += ((i.files_size - (k - 65536)) * 100.0) / files_size;
        //            TaskbarItemInfo.ProgressValue = process_bar / 100;
        //            //PB.Value = process_bar;
        //            BW.Close();
        //            BR.Close();
        //        }
        //    }
        //    MainWindow.is_paste = true;
        //    this.Close();
        //}
    }
}