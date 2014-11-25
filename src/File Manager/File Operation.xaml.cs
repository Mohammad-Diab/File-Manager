using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
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

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for File_Operation.xaml
    /// </summary>
    public partial class File_Operation : Window
    {
        //Cancel OP is not completed
        int complete = 0, items_count = 0, buffer = Environment.Is64BitOperatingSystem ? 65536 : 32768;
        List<string> directories_name = new List<string>();
        List<FileOperationItem> MainToCopyList;
        List<FileInfo> ToDeleteFiles;
        List<DirectoryInfo> ToDeleteDirectories;
        List<List<FileOperationItem>> ToCopyList = new List<List<FileOperationItem>>();
        Op OP;
        BinaryWriter BW;
        BinaryReader BR;
        List<int> overrite;
        bool?[] file_attributes;
        byte count = 0;
        string _name = "", paste_in;
        double process_bar = 0, old_process;
        long total_files_size = 0;
        bool working = true, work_pause = false, is_play = true;
        List<UnCompleatedOperation> undone, exists_error, undeleted_files;
        List<bool> retry;

        public File_Operation()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            undone = new List<UnCompleatedOperation>();
            exists_error = new List<UnCompleatedOperation>();
            undeleted_files = new List<UnCompleatedOperation>();
            if (OP == Op.Copy)
            {
                to.Text = " to \"" + paste_in + "\"";
                TextBlock1.ToolTip = paste_in;
            }
        }

        //#region Copy or Move

        //internal void CopyDialogShow(string path, List<FileOperationItem> ToCopyList)
        //{
        //    this.MainToCopyList = ToCopyList;
        //    paste_in = path;
        //    BackgroundWorker CollectFiles = new BackgroundWorker();
        //    CollectFiles.DoWork += CollectFiles_DoWork;
        //    CollectFiles.WorkerSupportsCancellation = true;
        //    CollectFiles.RunWorkerCompleted += CollectFiles_RunWorkerCompleted;
        //    CollectFiles.RunWorkerAsync(ToCopyList);
        //    Show();
        //}

        //void CollectFiles_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    List<FileOperationItem> MainToCopyList = (List<FileOperationItem>)e.Argument;
        //    foreach (var i in MainToCopyList)
        //        if (i.Item is DirectoryInfo)
        //            total_files_size += GetAllFiles((DirectoryInfo)i.Item, i.IsCut, i.short_path);
        //        else
        //        {
        //            items_count++;
        //            List<FileOperationItem> File = new List<FileOperationItem>();
        //            ToCopyList.Add(File);
        //            total_files_size += i.Size;
        //            File.Add(i);
        //        }
        //}

        //long GetAllFiles(DirectoryInfo directoryInfo, bool IsCut, string short_path)
        //{
        //    List<FileOperationItem> CurrentDirectory = new List<FileOperationItem>();
        //    ToCopyList.Add(CurrentDirectory);
        //    long size = 0;
        //    foreach (var i in directoryInfo.GetDirectories())
        //        try
        //        { size += GetAllFiles(i, IsCut, short_path + "\\" + i.Name); directories_name.Add(short_path + "\\" + i.Name); }
        //        catch (Exception) { }
        //    var j = directoryInfo.GetFiles();
        //    foreach (var i in j)
        //    {
        //        items_count++;
        //        size += i.Length;
        //        if (short_path == "")
        //        { short_path = short_path.Remove(1, 1); }
        //        CurrentDirectory.Add(new FileOperationItem(i, IsCut, false, short_path + "\\" + i.Name));
        //    }
        //    return size;
        //}

        //void CollectFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    MainProcessBar.IsIndeterminate = false;
        //    items_number.Text = items_count.ToString();
        //    MainProcessBar.Maximum = total_files_size;
        //    Title = Title + " (" + Functions.FileSize(total_files_size, 2) + ")";
        //    NewWorker();
        //}

        //void NewWorker()
        //{
        //    BackgroundWorker CopyFiles = new BackgroundWorker();
        //    CopyFiles.DoWork += CopyFiles_DoWork;
        //    CopyFiles.WorkerSupportsCancellation = true;
        //    CopyFiles.RunWorkerCompleted += CopyFiles_RunWorkerCompleted;
        //    CopyFiles.RunWorkerAsync(ToCopyList);

        //    BackgroundWorker ProcessChangeValue = new BackgroundWorker();
        //    ProcessChangeValue.DoWork += ProcessChangeValue_DoWork;
        //    ProcessChangeValue.WorkerSupportsCancellation = true;
        //    ProcessChangeValue.WorkerReportsProgress = true;
        //    ProcessChangeValue.ProgressChanged += ProcessChangeValue_ProgressChanged;
        //    ProcessChangeValue.RunWorkerAsync();
        //}

        //void CopyFiles_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    for (int i = 0; i < ToCopyList.Count; i++)
        //    {
        //        if (ToCopyList[i].Count == 0 && directories_name.Count > i)
        //        {
        //            string[] directory_paths = Functions.SplitString(directories_name[i]);
        //            for (int k = 0; k < directory_paths.Length; k++)
        //                if (!Directory.Exists(directory_paths[k]))
        //                    Directory.CreateDirectory(directory_paths[k]);
        //        }
        //        for (int j = 0; j < ToCopyList[i].Count && !ToCopyList[i][j].Done; j++)
        //            try
        //            {
        //                //if (ToCopyList[i][j].Item is FileInfo)
        //                //{
        //                    _name = ToCopyList[i][j].Item.FullName;
        //                    string[] directory_paths = Functions.SplitString(ToCopyList[i][j].short_path);
        //                    for (int k = 0; k < directory_paths.Length - 1; k++)    // last one is file name
        //                        if (!Directory.Exists(paste_in + "\\" + directory_paths[k]))
        //                            Directory.CreateDirectory(paste_in + "\\" + directory_paths[k]);
        //                    if ((ToCopyList[i][j].IsCut || (ToCopyList[i][j].Over == true)) && ToCopyList[i][j].Item.FullName.Equals(paste_in + ToCopyList[i][j].short_path))
        //                        continue;
        //                    if (ToCopyList[i][j].Over == false && File.Exists(paste_in + ToCopyList[i][j].short_path))
        //                    {
        //                        exists_error.Add(new UnCompleatedOperation(ToCopyList[i][j], "File is already exists."));
        //                        continue;
        //                    }
        //                    else
        //                    {
        //                        WriteFile(ToCopyList[i][j]);
        //                        ToCopyList[i][j].Done = true;
        //                    }
        //                    if (ToCopyList[i][j].Done && ToCopyList[i][j].IsCut && File.Exists(ToCopyList[i][j].Item.FullName) && ToCopyList[i][j].Item.FullName[0] != paste_in[0])
        //                        try
        //                        { File.Delete(ToCopyList[i][j].Item.FullName); }
        //                        catch (Exception) { undeleted_files.Add(new UnCompleatedOperation(ToCopyList[i][j], "File have not been deleted")); }
        //                    while (work_pause)
        //                        Thread.Sleep(50);
        //                //}
        //                //else
        //                //{
        //                //    string[] directory_paths = Functions.SplitString(ToCopyList[i][j].short_path);
        //                //    string dir = paste_in;
        //                //    for (int k = 0; k < directory_paths.Length; k++)
        //                //        if (!Directory.Exists(dir + "\\" + directory_paths[k]))
        //                //            Directory.CreateDirectory(dir += "\\" + directory_paths[k]);
        //            //    }
        //            }
        //            catch (Exception ex)
        //            {
        //                if (!ToCopyList[i][j].Over.Value && File.Exists(paste_in + "\\" + ToCopyList[i][j].short_path))
        //                    try
        //                    { if (BW != null) BW.Close(); File.Delete(paste_in + "\\" + ToCopyList[i][j].short_path); }
        //                    catch (Exception) { }
        //                undone.Add(new UnCompleatedOperation(ToCopyList[i][j], ex.Message));
        //            }
        //    }
        //}

        //void CopyFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    //for (int i = ToCopyList.Count - 1; i > -1; i--)
        //    //{
        //    //    if (MainToCopyList[i].Item.Exists)
        //    //    {
        //    //        int h = ToCopyList[i].Count;
        //    //        for (int j = 0; j < ToCopyList[i].Count; j++)
        //    //            if (ToCopyList[i][j].Done)
        //    //                h--;
        //    //        if (h == 0)
        //    //            try
        //    //            {
        //    //                ToCopyList.RemoveAt(i);
        //    //                MainToCopyList.RemoveAt(i);
        //    //                if (MainToCopyList[i].Item is DirectoryInfo)
        //    //                    Directory.Delete(MainToCopyList[i].Item.FullName, true);
        //    //                else
        //    //                    File.Delete(MainToCopyList[i].Item.FullName);
        //    //            }
        //    //            catch (Exception) { }
        //    //    }
        //    //}
        //    working = false;
        //    if (undone.Count == 0 && exists_error.Count == 0 && undeleted_files.Count == 0)
        //    {
        //        MainWindow.IsPaste = true;
        //        Close();
        //    }
        //    else
        //    {
        //        working = true;
        //        TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
        //        MainProcessBar.Foreground = new LinearGradientBrush(Color.FromRgb(218, 38, 38), Color.FromRgb(220, 40, 40), 30);
        //        if (exists_error.Count > 0)
        //        {
        //            overrite = new Exists_Files_Window(exists_error, paste_in).ShowThisDialog();
        //            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
        //            MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
        //            ToCopyList = new List<List<FileOperationItem>>();
        //            ToCopyList.Add(new List<FileOperationItem>());
        //            for (int i = overrite.Count - 1; i >= 0; i--)
        //                if (overrite[i] == 1)
        //                { ToCopyList[0].Add(exists_error[i].Item); exists_error[i].Item.Over = true; }
        //                else if (overrite[i] == 2)
        //                { ToCopyList[0].Add(exists_error[i].Item); exists_error[i].Item.Over = null; }
        //                else if (overrite[i] == 0)
        //                    process_bar += exists_error[i].Item.Size;
        //            NewWorker();
        //            exists_error.Clear();
        //        }
        //        else if (undone.Count > 0)
        //        {
        //            retry = new File_s_Transferring_Error(undone, null).ShowThisDialog();
        //            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
        //            MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
        //            ToCopyList = new List<List<FileOperationItem>>();
        //            ToCopyList.Add(new List<FileOperationItem>());
        //            for (int i = retry.Count - 1; i >= 0; i--)
        //                if (retry[i])
        //                    ToCopyList[0].Add(undone[i].Item);
        //                else
        //                {
        //                    process_bar += undone[i].Item.Size;
        //                    complete++;
        //                }
        //            NewWorker();
        //            undone.Clear();
        //        }
        //        else if (undeleted_files.Count > 0)
        //        {
        //            while (undeleted_files.Count > 0)
        //            {
        //                MainProcessBar.Foreground = new LinearGradientBrush(Color.FromRgb(218, 38, 38), Color.FromRgb(220, 40, 40), 30);
        //                retry = new File_s_Transferring_Error(undeleted_files, null).ShowThisDialog();
        //                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
        //                MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
        //                for (int i = undeleted_files.Count - 1; i > -1; i--)
        //                    if (retry[i])
        //                        try
        //                        { undeleted_files[i].Item.Item.Delete(); undeleted_files.RemoveAt(i); }
        //                        catch (Exception) { }
        //                    else
        //                        undeleted_files.RemoveAt(i);
        //                //NewWorker();
        //                //ToCopyList.Clear();
        //            }
        //        }
        //    }
        //    for (int j = 0; j >= 0 && j < ToCopyList.Count; j++)
        //        for (int i = 0; i >= 0 && i < ToCopyList[j].Count; i++)
        //            if (ToCopyList[j][i].IsCut && ToCopyList[j][i].Done && ToCopyList[j][i].Item.Exists)
        //                try
        //                {
        //                    if (ToCopyList[j][i].Item is DirectoryInfo)
        //                        ((DirectoryInfo)ToCopyList[j][i].Item).Delete(true);
        //                    else
        //                        ToCopyList[j][i].Item.Delete();
        //                    //ToCopyList[j].RemoveAt(i);
        //                    i--;
        //                }
        //                catch (Exception) { undeleted_files.Add(new UnCompleatedOperation(ToCopyList[j][i], "File have not been deleted")); }
        //}

        //void ProcessChangeValue_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    while (working)
        //    {
        //        while (work_pause)
        //            System.Threading.Thread.Sleep(500);
        //        System.Threading.Thread.Sleep(200);
        //        ((BackgroundWorker)sender).ReportProgress(0);
        //    }
        //}

        //void ProcessChangeValue_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    MainProcessBar.Value = process_bar;
        //    double val = MainProcessBar.Value * 100 / total_files_size;
        //    percent.Text = Math.Round(val, 0).ToString();
        //    TaskbarItemInfo.ProgressValue = val / 100;
        //    completed.Text = complete.ToString();
        //    if (count % 20 == 0)
        //    {
        //        count = 1;
        //        if (old_process != 0)
        //            speed.Text = Functions.FileSize((process_bar - old_process) / 1.9) + " per second";
        //        old_process = process_bar;
        //    }
        //    else
        //        count++;
        //    item_name.ToolTip = item_name.Text = _name;
        //}

        //void WriteFile(FileOperationItem i)
        //{
        //    string new_path = paste_in + "\\" + i.short_path;
        //    if (File.Exists(paste_in + "\\" + i.short_path))
        //        if (i.Over.Value)
        //            File.Delete(paste_in + "\\" + i.short_path);
        //        else
        //    //if (File.Exists(paste_in + "\\" + i.short_path) && !i.Over.HasValue)
        //    {
        //        FileInfo source_file = new FileInfo(new_path);
        //        string extention = source_file.Extension;
        //        string name;
        //        if (extention.Length > 0)
        //            name = source_file.Name.Remove(source_file.Name.Length - extention.Length);
        //        else
        //            name = source_file.Name;
        //        int num = 1;
        //        while (File.Exists(paste_in + "\\" + name + " (" + num + ")" + extention)) num++;
        //        new_path = paste_in + "\\" + name + " (" + num + ")" + extention;
        //    }
        //    if (i.Item.FullName[0] == paste_in[0] && i.IsCut)
        //    {
        //        File.Move(i.Item.FullName, new_path);
        //        process_bar += i.Size;
        //    }
        //    else
        //    {
        //        BR = new BinaryReader(File.OpenRead(i.Item.FullName));
        //        _name = i.Item.FullName;
        //        BW = new BinaryWriter(File.OpenWrite(new_path));
        //        long k;
        //        for (k = buffer; k <= i.Size; k += buffer)
        //        {
        //            while (work_pause)
        //                Thread.Sleep(500);
        //            BW.Write(BR.ReadBytes(buffer));
        //            process_bar += buffer;
        //        }
        //        while (work_pause)
        //            Thread.Sleep(500);
        //        long remain = (i.Size - (k - buffer));
        //        BW.Write(BR.ReadBytes((int)remain));
        //        process_bar += remain;
        //        File.SetAttributes(new_path, File.GetAttributes(i.Item.FullName));
        //        complete++;
        //        BW.Close();
        //        BR.Close();
        //    }
        //}

        //#endregion

        #region Delete

        internal void DeleteDialogShow(List<FileOperationItem> ToDeleteList)
        {
            Show();
            this.MainToCopyList = ToDeleteList;
            BackgroundWorker CollectFiles = new BackgroundWorker();
            CollectFiles.DoWork += CollectToDeleteItem_DoWork;
            CollectFiles.WorkerSupportsCancellation = true;
            CollectFiles.RunWorkerCompleted += CollectToDeleteFiles_RunWorkerCompleted;
            CollectFiles.RunWorkerAsync(ToCopyList);
        }

        internal void DeleteDialogShow(List<string> files, List<string> directories)
        {
            ToDeleteFiles = new List<FileInfo>();
            ToDeleteDirectories = new List<DirectoryInfo>();
            foreach (string i in files)
                ToDeleteFiles.Add(new FileInfo(i));
            foreach (string i in directories)
                ToDeleteDirectories.Add(new DirectoryInfo(i));
            items_number.Text = (ToDeleteFiles.Count + ToDeleteDirectories.Count).ToString();
            MainProcessBar.Maximum = files.Count + directories.Count;
            MainProcessBar.IsIndeterminate = false;
            NewDeleteWorker();
            ShowDialog();
        }

        private void CollectToDeleteItem_DoWork(object sender, DoWorkEventArgs e)
        {
            List<FileOperationItem> MainToCopyList = (List<FileOperationItem>)e.Argument;
            foreach (var i in MainToCopyList)
                if (i.Item is DirectoryInfo)
                {
                    ToDeleteDirectories.Insert(0, (DirectoryInfo)i.Item);
                    GetAllFiles((DirectoryInfo)i.Item);
                }
                else
                    ToDeleteFiles.Insert(0, (FileInfo)i.Item);
        }

        void GetAllFiles(DirectoryInfo directoryInfo)
        {
            foreach (var i in directoryInfo.GetDirectories())
                try
                { ToDeleteDirectories.Insert(0, i); GetAllFiles(i); }
                catch (Exception) { }
            foreach (var i in directoryInfo.GetFiles())
                ToDeleteFiles.Insert(0, i);
        }

        void CollectToDeleteFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            items_number.Text = (ToDeleteFiles.Count + ToDeleteDirectories.Count).ToString();
            MainProcessBar.Maximum = ToDeleteFiles.Count + ToDeleteDirectories.Count;
            MainProcessBar.IsIndeterminate = false;
            NewDeleteWorker();
        }

        private void NewDeleteWorker()
        {
            BackgroundWorker DeleteFiles = new BackgroundWorker();
            DeleteFiles.DoWork += Delete_DoWork;
            DeleteFiles.WorkerSupportsCancellation = DeleteFiles.WorkerReportsProgress = true;
            DeleteFiles.RunWorkerCompleted += Delete_RunWorkerCompleted;
            DeleteFiles.ProgressChanged += Delete_ProgressChanged;
            DeleteFiles.RunWorkerAsync(ToCopyList);
        }

        private void Delete_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            MainProcessBar.Value = e.ProgressPercentage;
            double val = MainProcessBar.Value * 100.0 / (ToDeleteFiles.Count + ToDeleteDirectories.Count);
            percent.Text = Math.Round(val, 0).ToString();
            TaskbarItemInfo.ProgressValue = val / 100;
            completed.Text = e.ProgressPercentage.ToString();
            if (count % 20 == 0)
            {
                count = 1;
                if (old_process != 0)
                    speed.Text = Math.Round(((process_bar - old_process) / 1.9), 2) + " items per second";
                old_process = process_bar;
            }
            else
                count++;
            item_name.ToolTip = item_name.Text = _name;
        }

        private void Delete_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (undone.Count == 0)
                Close();
            else
            {
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                MainProcessBar.Foreground = new LinearGradientBrush(Color.FromRgb(218, 38, 38), Color.FromRgb(220, 40, 40), 30);
                ////////////////retry = new File_s_Transferring_Error(undone, "Some items have not deleted successfully.<note: default option is skip>").ShowThisDialog();
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));

                ToDeleteDirectories.Clear();
                ToDeleteFiles.Clear();
                for (int i = retry.Count - 1; i >= 0; i--)
                    if (retry[i])
                        if (undone[i].Item.Item is DirectoryInfo)
                            ToDeleteDirectories.Add((DirectoryInfo)undone[i].Item.Item);
                        else
                            ToDeleteFiles.Add((FileInfo)undone[i].Item.Item);
                    else
                        process_bar++;
                NewDeleteWorker();
            }
        }

        void Delete_DoWork(object sender, DoWorkEventArgs e)
        {
            ////////////////BackgroundWorker BW = (BackgroundWorker)sender;
            ////////////////int count = 0;
            ////////////////for (int i=0;i< ToDeleteFiles.Count;i++)
            ////////////////{
            ////////////////    while (work_pause)
            ////////////////        Thread.Sleep(500);
            ////////////////    try
            ////////////////    { ToDeleteFiles[i].Delete(); count++; BW.ReportProgress(count); }
            ////////////////    catch (Exception ex)
            ////////////////    { undone.Add(new UnCompleatedOperation(new FileOperationItem(ToDeleteFiles[i], true, false, ""), ex.Message)); }
            ////////////////}
            ////////////////    for (int j = 0; j < ToDeleteDirectories.Count; j++)
            ////////////////    {
            ////////////////        while (work_pause)
            ////////////////            Thread.Sleep(500);
            ////////////////        _name = ToDeleteDirectories[j].FullName;
            ////////////////        try
            ////////////////        { ((DirectoryInfo)ToDeleteDirectories[j]).Delete(false); count++; BW.ReportProgress(count); }
            ////////////////        catch (Exception ex)
            ////////////////        { undone.Add(new UnCompleatedOperation(new FileOperationItem(ToDeleteDirectories[j], ""), ex.Message)); }
            ////////////////    }
        }

        #endregion

        #region Attributes

        internal void SetAttributesDialogShow(List<FileOperationItem> ListOfItems, bool?[] file_attributes)
        {
            Show();
            op.Text = "Applying attributes: ";
            this.file_attributes = file_attributes;
            this.MainToCopyList = ListOfItems;
            BackgroundWorker CollectFiles = new BackgroundWorker();
            CollectFiles.DoWork += CollectToDeleteItem_DoWork;
            CollectFiles.WorkerSupportsCancellation = true;
            CollectFiles.RunWorkerCompleted += CollectToApplyingAttributesFiles_RunWorkerCompleted;
            CollectFiles.RunWorkerAsync(ToCopyList);
        }

        void CollectToApplyingAttributesFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            items_number.Text = (ToDeleteFiles.Count + ToDeleteDirectories.Count).ToString();
            MainProcessBar.Maximum = ToDeleteFiles.Count + ToDeleteDirectories.Count;
            MainProcessBar.IsIndeterminate = false;
            NewApplyingAttributesWorker();
        }

        void NewApplyingAttributesWorker()
        {
            BackgroundWorker ApplyingAttributes = new BackgroundWorker();
            ApplyingAttributes.DoWork += ApplyingAttributes_DoWork;
            ApplyingAttributes.WorkerSupportsCancellation = true;
            ApplyingAttributes.RunWorkerCompleted += ApplyingAttributes_RunWorkerCompleted;
            ApplyingAttributes.ProgressChanged += Delete_ProgressChanged;
            ApplyingAttributes.RunWorkerAsync(ToCopyList);
        }

        void ApplyingAttributes_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (undone.Count == 0)
                Close();
            else
            {
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                MainProcessBar.Foreground = new LinearGradientBrush(Color.FromRgb(218, 38, 38), Color.FromRgb(220, 40, 40), 30);
                ////////////////////retry = new File_s_Transferring_Error(undone, "We can not apply attributes on some Items.<note: default option is skip>").ShowThisDialog();
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));

                ToDeleteDirectories.Clear();
                ToDeleteFiles.Clear();
                for (int i = retry.Count - 1; i >= 0; i--)
                    if (retry[i])
                        if (undone[i].Item.Item is DirectoryInfo)
                            ToDeleteDirectories.Add((DirectoryInfo)undone[i].Item.Item);
                        else
                            ToDeleteFiles.Add((FileInfo)undone[i].Item.Item);
                    else
                        process_bar++;
                NewApplyingAttributesWorker();
            }
        }

        void ApplyingAttributes_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker BW = (BackgroundWorker)sender;
            int count = 0;
            foreach (var i in ToDeleteFiles)
            {
                while (work_pause)
                    Thread.Sleep(500);
                _name = i.FullName;
                //////////////try
                //////////////{ SetAttributesToFile(i); count++; BW.ReportProgress(count); }
                //////////////catch (Exception ex)
                //////////////{ undone.Add(new UnCompleatedOperation(new FileOperationItem(i, true, false, ""), ex.Message)); }
            }
            foreach (var i in ToDeleteDirectories)
            {
                while (work_pause)
                    Thread.Sleep(500);
                _name = i.FullName;
                ////////////////try
                ////////////////{ SetAttributesToDirectory(i); count++; BW.ReportProgress(count); }
                ////////////////catch (Exception ex)
                ////////////////{ undone.Add(new UnCompleatedOperation(new FileOperationItem(i, ""), ex.Message)); }
            }
        }

        [DllImport("kernel32.dll")]
        public static extern int DeviceIoControl(IntPtr hDevice, int dwIoControlCode, ref short lpInBuffer, int nInBufferSize, IntPtr lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr lpOverlapped);
        int FileCompress(string file_name)
        {
            int lpBytesReturned = 0;
            int FSCTL_SET_COMPRESSION = 0x9C040;
            short COMPRESSION_FORMAT_DEFAULT = 1;
            FileStream stream = File.Open(file_name, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
            int result = DeviceIoControl(stream.Handle, FSCTL_SET_COMPRESSION, ref COMPRESSION_FORMAT_DEFAULT, 2, IntPtr.Zero, 0, ref lpBytesReturned, IntPtr.Zero);
            return result;
        }

        int FileDecompress(string file_name)
        {
            //int lpBytesReturned = 0;
            //int FSCTL_SET_COMPRESSION = 0x9C040;
            //short COMPRESSION_FORMAT_DEFAULT = 1;
            //FileStream stream = File.Open(file_name, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
            //int result = DeviceIoControl(stream.Handle, FSCTL_SET_COMPRESSION, ref COMPRESSION_FORMAT_DEFAULT, 2, IntPtr.Zero, 0, ref lpBytesReturned, IntPtr.Zero);
            //return result;
            return 0;
        }

        int DirectoryCompress(DirectoryInfo dir)
        {
            try
            {
                string objPath = "Win32_Directory.Name=" + "'" + dir.FullName.Replace("\\", @"\\").TrimEnd('\\') + "'";
                using (ManagementObject obj = new ManagementObject(objPath))
                { using (obj.InvokeMethod("Compress", null, null)) { return 1; } }

            }
            catch (Exception ex)
            { System.Diagnostics.Trace.WriteLine("Cannot enable compression for folder '" + dir.FullName + "': " + ex.Message, "WMI"); return 0; }
        }

        int DirectoryDecompress(DirectoryInfo dir)
        {
            //try
            //{
            //    string objPath = "Win32_Directory.Name=" + "'" + dir.FullName.Replace("\\", @"\\").TrimEnd('\\') + "'";
            //    using (ManagementObject obj = new ManagementObject(objPath))
            //    { using (obj.InvokeMethod("DeCompress", null, null)) { } }
            //}
            //catch (Exception ex)
            //{ System.Diagnostics.Trace.WriteLine("Cannot enable compression for folder '" + dir.FullName + "': " + ex.Message, "WMI"); }
            return 0;
        }

        void SetAttributesToFile(FileInfo file_name)
        {
            if (file_attributes[7] == true)
                file_name.Attributes = FileAttributes.Normal;
            else
            {
                FileAttributes default_attributes = file_name.Attributes;
                var Newattribute = CalcFileAttributes(0, default_attributes);
                file_name.Attributes = Newattribute;
                if (default_attributes.HasFlag(FileAttributes.Compressed) != Newattribute.HasFlag(FileAttributes.Compressed))
                {
                    int result;
                    if (Newattribute.HasFlag(FileAttributes.Compressed))
                        result = FileCompress(file_name.FullName);
                    else
                        result = FileDecompress(file_name.FullName);
                    if (result != 1)
                        throw new Exception("Can not compress file '" + file_name + "'");
                }
                if (default_attributes.HasFlag(FileAttributes.Encrypted) != Newattribute.HasFlag(FileAttributes.Encrypted))
                    if (Newattribute.HasFlag(FileAttributes.Encrypted))
                        File.Encrypt(file_name.FullName);
                    else
                        File.Decrypt(file_name.FullName);
            }
        }

        void SetAttributesToDirectory(DirectoryInfo directory_name)
        {
            if (file_attributes[7] == true)
                directory_name.Attributes = (FileAttributes.Normal + 16);
            else
            {
                FileAttributes new_attributes = 0;
                new_attributes += 16;
                if (directory_name.Attributes.HasFlag(FileAttributes.Compressed) != new_attributes.HasFlag(FileAttributes.Compressed))
                {
                    int result;
                    if (new_attributes.HasFlag(FileAttributes.Compressed))
                        result = DirectoryCompress(directory_name);
                    else
                        result = DirectoryDecompress(directory_name);
                    if (result != 1)
                        throw new Exception("Can not compress file '" + directory_name.FullName + "'");
                }
                FileAttributes default_attributes = directory_name.Attributes;
                directory_name.Attributes = CalcFileAttributes(new_attributes, default_attributes);
            }
        }

        FileAttributes CalcFileAttributes(FileAttributes start, FileAttributes default_attributes)
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

        #endregion

        #region Control
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
            if (OP == Op.Copy)
                MainWindow.IsPaste = true;
            try
            {
                //thread.Abort();
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
                    play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/copy-pause.png"));
                    work_pause = false;
                    is_play = true;
                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                    MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
                }
            }
        }

        private void Pause()
        {
            if (is_play == true && TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error)
            {
                is_play = false;
                play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/copy-start.png"));
                work_pause = true;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
                MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(218, 203, 38));
            }
        }

        internal void Show(string path)
        {
            paste_in = path;
            this.Show();
        }
        #endregion
    }
}



/*
 * Dim Path as string = "c:\test"
    Dim strComputer As String = "."
    Dim objWMIService = GetObject("winmgmts:{impersonationLevel=impersonate}!\\.\root\cimv2")
    Dim colFolders = objWMIService.ExecQuery("Select * from Win32_Directory where name = '" & Replace(path, "\", "\\") & "'")
    For Each objFolder In colFolders
        objFolder.Compress()
    Next
*/


//private List<FileOperationItem> attributes_files = new List<FileOperationItem>();
//private List<string> attributes_dirs = new List<string>();
//private List<FileSystemInfo> to_delete_list;

//void CopyFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
//        {
//            for (int i = 0; i >= 0 && i < ToCopyList.Count; i++)
//                if (ToCopyList[i].Item.Exists)
//                    try
//                    {
//                        if (ToCopyList[i].Item is DirectoryInfo)
//                            ((DirectoryInfo)ToCopyList[i].Item).Delete(true);
//                        else
//                            ToCopyList[i].Item.Delete();
//                        ToCopyList.RemoveAt(i);
//                        i--;
//                    }
//                    catch (Exception) { undeleted_files.Add(new UnCompleatedOperation((FileInfo)ToCopyList[i].Item, ToCopyList[i].IsCut, "File have not been deleted")); }
//            working = false;
//            if (undone.Count == 0 && exists_error.Count == 0 && undeleted_files.Count == 0)
//            {
//                MainWindow.IsPaste = true;
//                Close();
//            }
//            else
//            {
//                working = true;
//                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
//                MainProcessBar.Foreground = new LinearGradientBrush(Color.FromRgb(218, 38, 38), Color.FromRgb(220, 40, 40), 30);
//                if (exists_error.Count > 0)
//                {
//                    overrite = new Exists_Files_Window(exists_error, paste_in).ShowThisDialog();
//                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
//                    MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
//                    NewWorker();
//                    ToCopyList = new List<FileOperationItem>();
//                    for (int i = overrite.Count - 1; i >= 0; i--)
//                        if (overrite[i] == 1)
//                            ToCopyList.Add(new FileOperationItem(exists_error[i].files_full_name, exists_error[i].Size, exists_error[i].extension, exists_error[i].file_short_name, exists_error[i].remove_file, true));
//                        else if (overrite[i] == 2)
//                            ToCopyList.Add(new FileOperationItem(exists_error[i].files_full_name, exists_error[i].Size, exists_error[i].extension, exists_error[i].file_short_name, exists_error[i].remove_file, null));
//                        else if (overrite[i] == 0)
//                            process_bar += exists_error[i].Size;
//                    exists_error.Clear();
//                    thread = new Thread(FileMoveOrCopy);
//                }
//                else if (undone.Count > 0)
//                {
//                    if (copy == true)
//                        retry = new File_s_Transferring_Error(undone, null).ShowThisDialog();
//                    else if (copy == false)
//                        retry = new File_s_Transferring_Error(undone, "Some items have not deleted successfully.<note: default option is skip>").ShowThisDialog();
//                    else
//                        retry = new File_s_Transferring_Error(undone, "We can not apply attributes on some Items.<note: default option is skip>").ShowThisDialog();
//                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
//                    MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
//                    NewWorker();
//                    if (copy == true)
//                    {
//                        ToCopyList = new List<FileOperationItem>();
//                        for (int i = retry.Count - 1; i >= 0; i--)
//                            if (retry[i])
//                                ToCopyList.Add(new FileOperationItem(undone[i].files_full_name, undone[i].Size, undone[i].extension, undone[i].file_short_name, undone[i].remove_file, false));
//                            else
//                            {
//                                process_bar += undone[i].Size;
//                                complete++;
//                            }
//                        thread = new Thread(FileMoveOrCopy);
//                    }
//                    else if (copy == false)
//                    {
//                        files.Clear();
//                        folders.Clear();
//                        for (int i = retry.Count - 1; i >= 0; i--)
//                            if (retry[i])
//                                if (undone[i].IsDirectory)
//                                    folders.Add(undone[i].files_full_name);
//                                else
//                                    files.Add(undone[i].files_full_name);
//                            else
//                                process_bar++;
//                        thread = new Thread(Delete);
//                    }
//                    else
//                    {
//                        attributes_files.Clear();
//                        attributes_dirs.Clear();
//                        for (int i = retry.Count - 1; i >= 0; i--)
//                            if (retry[i])
//                                if (undone[i].IsDirectory)
//                                    attributes_dirs.Add(undone[i].files_full_name);
//                                else
//                                    attributes_files.Add(new FileOperationItem(undone[i].files_full_name, undone[i].file_short_name, undone[i].remove_file));
//                            else
//                                process_bar++;
//                        thread = new Thread(SaveAttributes);
//                    }
//                    undone.Clear();
//                }
//                else if (undeleted_files.Count > 0)
//                {
//                    retry = new File_s_Transferring_Error(undeleted_files, null).ShowThisDialog();
//                    TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
//                    MainProcessBar.Foreground = new SolidColorBrush(Color.FromRgb(6, 176, 37));
//                    NewWorker();
//                    ToCopyList.Clear();
//                    thread = new Thread(Undeleted);
//                }
//                NewThread();
//            }
//        }


/* void ProcessChangeValue_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            MainProcessBar.Value = process_bar;
            double val = MainProcessBar.Value * 100 / total_files_size;
            percent.Text = Math.Round(val, 0).ToString();
            TaskbarItemInfo.ProgressValue = val / 100;
            if (copy == true)
                completed.Text = complete.ToString();
            else
                completed.Text = process_bar.ToString();
            if (count % 20 == 0)
            {
                count = 1;
                if (old_process != 0)
                    if (copy == true) //copy or move or delete or apply attributes
                        speed.Text = Functions.FileSize((process_bar - old_process) / 1.9) + " per second";
                    else
                        speed.Text = Math.Round(((process_bar - old_process) / 1.9), 2) + " items per second";
                old_process = process_bar;
            }
            else
                count++;
            item_name.ToolTip = item_name.Text = _name;
        }
        //private void FileMoveOrCopy()
        //{
        //    for (int i = 0; i < MainToCopyList.Count; i++)
        //    {
        //        try
        //        {
        //            _name = MainToCopyList[i].files_full_name;
        //            string[] directory_paths = Functions.SplitString(MainToCopyList[i].file_short_name);
        //            for (int j = 0; j < directory_paths.Length - 1; j++)
        //                if (!Directory.Exists(paste_in + "\\" + directory_paths[j]))
        //                    Directory.CreateDirectory(paste_in + "\\" + directory_paths[j]);
        //            if ((MainToCopyList[i].remove_file || (MainToCopyList[i].over == true)) && MainToCopyList[i].files_full_name.Equals(paste_in + MainToCopyList[i].file_short_name))
        //                continue;
        //            if (MainToCopyList[i].over == false && File.Exists(paste_in + MainToCopyList[i].file_short_name))
        //            {
        //                exists_error.Add(new UnCompleatedOperation(MainToCopyList[i].files_full_name, MainToCopyList[i].files_size, MainToCopyList[i].extension, MainToCopyList[i].file_short_name, MainToCopyList[i].remove_file, "File is already exists."));
        //                continue;
        //            }
        //            else
        //            {
        //                WriteFile(MainToCopyList[i]);
        //                MainToCopyList[i].Done = true;
        //            }
        //            if (MainToCopyList[i].Done && MainToCopyList[i].remove_file && File.Exists(MainToCopyList[i].files_full_name) && MainToCopyList[i].files_full_name[0] != paste_in[0])
        //                try
        //                { File.Delete(MainToCopyList[i].files_full_name); }
        //                catch (Exception) { undeleted_files.Add(new UnCompleatedOperation(MainToCopyList[i].files_full_name, MainToCopyList[i].files_size, MainToCopyList[i].file_short_name, MainToCopyList[i].extension, MainToCopyList[i].remove_file, "File have not been deleted")); }
        //            while (work_pause)
        //                Thread.Sleep(500);
        //        }
        //        catch (Exception ex)
        //        {
        //            if (File.Exists(paste_in + "\\" + MainToCopyList[i].file_short_name))
        //                try
        //                { if (BW != null) BW.Close(); File.Delete(paste_in + "\\" + MainToCopyList[i].file_short_name); }
        //                catch (Exception) { }
        //            undone.Add(new UnCompleatedOperation(MainToCopyList[i].files_full_name, MainToCopyList[i].files_size, MainToCopyList[i].extension, MainToCopyList[i].file_short_name, MainToCopyList[i].remove_file, ex.Message));
        //        }
        //    }
        //    working = false;
        //}






        //public File_Operation()
        //{
        //    MainToCopyList = new List<FileOperationItem>();
        //    foreach (var i in MainWindow.to_copy_files)
        //        MainToCopyList.Add(i);
        //    InitializeComponent();
        //}

        //public File_Operation(List<FileOperationItem> to_copy)
        //{
        //    MainToCopyList = new List<FileOperationItem>();
        //    foreach (var i in to_copy)
        //        MainToCopyList.Add(i);
        //    InitializeComponent();
        //}*/


//public File_Operation(List<string> files, List<string> folders)
//{
//    InitializeComponent();
//    copy = false;
//    op.Text = "Deleting: ";
//    total_files_size = files.Count + folders.Count;
//    Title = Title + " (" + total_files_size + " Item" + (total_files_size > 1 ? "s" : "") + ")";
//    this.files = files;
//    this.folders = folders;
//}


//private void DeleteFile(string file_name, List<UnCompleatedOperation> undone)
//{
//    try
//    { File.Delete(file_name); process_bar++; }
//    catch (Exception ex)
//    { undone.Add(new UnCompleatedOperation(file_name, ex.Message, false)); }
//}

//private void DeleteDirectory(string directory_name, List<UnCompleatedOperation> undone)
//{
//    try
//    { Directory.Delete(directory_name, false); process_bar++; }
//    catch (Exception ex)
//    { undone.Add(new UnCompleatedOperation(directory_name, ex.Message, true)); }
//}



//public File_Operation(List<FileOperationItem> files, List<string> dirs, bool?[] file_attributes)
//{
//    InitializeComponent();
//    copy = null;
//    op.Text = "Applying attributes: ";
//    foreach (var i in files)
//        attributes_files.Add(i);
//    foreach (var i in dirs)
//        attributes_dirs.Add(i);
//    this.file_attributes = file_attributes;
//    total_files_size = files.Count + dirs.Count;
//    Title = Title + " (" + total_files_size + " Item" + (total_files_size > 1 ? "s" : "") + ")";
//}

//private void SaveAttributes()
//{
//    foreach (var i in attributes_files)
//    {
//        while (work_pause)
//            Thread.Sleep(500);
//        _name = i.files_full_name;
//        SetAttributesToFile(i.files_full_name);
//    }
//    foreach (var i in attributes_dirs)
//    {
//        while (work_pause)
//            Thread.Sleep(500);
//        _name = i;
//        SetAttributesToDirectory(i);
//    }
//    working = false;
//}

//private void NewThread()
//{
//    thread.IsBackground = true;
//    thread.Priority = ThreadPriority.Highest;
//    thread.Start();
//}


//void Undone()
//{
//    if (undone.Count > 0)
//    {
//        var retry = new File_s_Transferring_Error(undone, "We can not apply attributes on some Items").ShowThisDialog();
//        for (int i = retry.Count - 1; i > -1; i--)
//        {
//            if (retry[i])
//                if (undone[i].IsDirectory)
//                    SetAttributesToDirectory(undone[i].files_full_name);
//                else
//                    SetAttributesToFile(undone[i].files_full_name);
//            undone.RemoveAt(i);
//        }
//        Undone();
//    }
//}

//private void worker_DoWork(object sender, DoWorkEventArgs e)
//{
//    while (working)
//    {
//        while (work_pause)
//            System.Threading.Thread.Sleep(500);
//        System.Threading.Thread.Sleep(100);
//        worker.ReportProgress(0);
//    }
//}

//private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
//{

//    //Move ProcessBar with animation
//    //BackgroundWorker process_animation = new BackgroundWorker();
//    //process_animation.DoWork += g_DoWork2;
//    //process_animation.ProgressChanged += g_ProgressChanged2;
//    //process_animation.WorkerReportsProgress = true;
//    //process_animation.RunWorkerAsync(e.ProgressPercentage);

//    ////
//    PB.Value = process_bar;
//    double val = PB.Value * 100 / (ToDeleteFiles.Count + ToDeleteDirectories.Count);
//    percent.Text = Math.Round(val, 0).ToString();
//    TaskbarItemInfo.ProgressValue = val / 100;
//    if (copy == true)
//        completed.Text = complete.ToString();
//    else
//        completed.Text = process_bar.ToString();
//    if (count % 20 == 0)
//    {
//        count = 1;
//        if (old_process != 0)
//            if (copy == true) //copy or move or delete or apply attributes
//                speed.Text = Functions.FileSize((process_bar - old_process) / 1.9) + " per second";
//            else
//                speed.Text = Math.Round(((process_bar - old_process) / 1.9), 2) + " items per second";
//        old_process = process_bar;
//    }
//    else
//        count++;
//    item_name.ToolTip = item_name.Text = _name;
//}

//private void Undeleted()
//{
//    for (int i = undeleted_files.Count - 1; i >= 0; i--)
//    {
//        if (retry[i])
//            try
//            { File.Delete(undeleted_files[i].files_full_name); }
//            catch (Exception)
//            { undeleted_files.Add(new UnCompleatedOperation(undeleted_files[i].files_full_name, undeleted_files[i].Size, undeleted_files[i].extension, undeleted_files[i].file_short_name, undeleted_files[i].remove_file, "File have not been deleted")); }
//        undeleted_files.RemoveAt(i);
//    }
//}