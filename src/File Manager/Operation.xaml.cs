using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Operation.xaml
    /// </summary>
    public partial class Operation : Window
    {
        string paste_in, current_file_name;
        OperationType operation_type;
        OperationType OperationType
        {
            get { return operation_type; }
            set
            {
                switch (value)
                {// for comress Brushes.MediumSlateBlue
                    case OperationType.Copy:
                        ProgressBrush = Brushes.DodgerBlue;
                        break;
                    case OperationType.Rename:
                        ProgressBrush = Brushes.GreenYellow;
                        break;
                    case OperationType.Attributes:
                        ProgressBrush = Brushes.Navy;
                        break;
                    case OperationType.Delete:
                        ProgressBrush = Brushes.Sienna;
                        break;
                    case OperationType.Destroy:
                        ProgressBrush = Brushes.Black;
                        break;
                    case OperationType.DiskDestroy:
                        ProgressBrush = Brushes.Maroon;
                        break;
                    case OperationType.Decrypt:
                        ProgressBrush = Brushes.LimeGreen;
                        break;
                    case OperationType.Encrypt:
                        ProgressBrush = Brushes.OliveDrab;
                        break;
                    case OperationType.ConvertISZ_ToISO:
                        ProgressBrush = Brushes.DarkCyan;
                        break;
                    default:
                        ProgressBrush = Brushes.DodgerBlue;
                        break;
                }
                operation_type = value;
            }
        }

        List<long> old = new List<long>();
        List<long> newnew = new List<long>();

        OperationTree root, already_exist, unsucsess, undeleted;
        long progress_value, completed_count, old_progress_value;
        int progress_rount = 1;
        BinaryReader BR;
        BinaryWriter BW;
        bool sleep_thread = false, pause_copy = false, file_is_moved = false, CloseWindow = false;
        BackgroundWorker OpBW;
        Brush progress_brush = Brushes.DodgerBlue;
        Brush ProgressBrush
        {
            get { return progress_brush; }
            set
            {
                remain.Foreground = value;
                operation.Foreground = value;
                progress_bar.Foreground = value;
                completed.Foreground = more_than_one.Foreground = complete_item_run.Foreground = value;
                speed.Foreground = value;
                progress_brush = value;
            }
        }

        public Operation()
        {
            InitializeComponent();
        }

        internal void CopyDialogShow(List<FileOperationItem> ToCopyList, OperationType operation_type, string path_to_paste, bool show_dialog)
        {
            OperationType = operation_type;
            root = new OperationTree(null, false, "", operation_type, null, false, "");
            paste_in = path_to_paste;
            operation.Text = Title = operation_type == OperationType.Copy ? "Copying" : operation_type == OperationType.Delete ? "Deleting" : operation_type == OperationType.Rename ? "Renaming" : operation_type == OperationType.Attributes ? "Applaying Attributes" : operation_type == OperationType.Extract ? "Extracting Files" : "";
            BackgroundWorker CollectFiles = new BackgroundWorker();
            if (operation_type != OperationType.Delete)
                CollectFiles.DoWork += CollectFiles_DoWork;
            if (operation_type == OperationType.Copy)
            {
                MainWindow.IsPaste = true;
                CollectFiles.RunWorkerCompleted += CollectFiles_RunWorkerCompleted;
            }
            else if (operation_type == OperationType.Delete)
            {
                CollectFiles.DoWork += CollectFilesToDelete_DoWork;
                CollectFiles.RunWorkerCompleted += CollectFilesToDelete_RunWorkerCompleted;
            }
            else if (operation_type == OperationType.Rename)
                CollectFiles.RunWorkerCompleted += CollectFilesToRename_RunWorkerCompleted;
            else if (operation_type == OperationType.Attributes)
                CollectFiles.RunWorkerCompleted += CollectFilesToAttribute_RunWorkerCompleted;
            //else if (operation_type == Op.Extract)
            //    CollectFiles.RunWorkerCompleted += CollectFilesToExtract_RunWorkerCompleted;
            unsucsess = new OperationTree(null, false, "", operation_type, null, false, "Can't delete item");
            CollectFiles.RunWorkerAsync(ToCopyList);
            if (show_dialog)
                ShowDialog();
            else
                Show();
        }

        private void CollectFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (FileOperationItem i in (List<FileOperationItem>)e.Argument)
                if (i.Item is FileInfo)
                    root.children.Add(new OperationTree((FileInfo)i.Item, i.IsCut, i.short_path, OperationType, root, ""));
                else
                    root.children.Add(new OperationTree((DirectoryInfo)i.Item, i.IsCut, i.short_path, OperationType, root, OperationType == OperationType.Copy || OperationType == OperationType.Delete, ""));
        }

        void CompleteCollectFiles(long maximum_progress_bar_value, string title)
        {
            progress_bar.IsIndeterminate = false;
            progress_bar.Value = 0;
            progress_bar.Maximum = maximum_progress_bar_value;
            ChangeRemain(maximum_progress_bar_value, !(OperationType == OperationType.Copy || OperationType == OperationType.Encrypt));
            Title = Title + " (" + title + ")";
            OpBW = new BackgroundWorker();
            OpBW.WorkerReportsProgress = true;
            if (OperationType == OperationType.Copy)
                OpBW.DoWork += Copying_DoWork;
            else if (OperationType == OperationType.Delete)
                OpBW.DoWork += Deleting_DoWork;
            else if (OperationType == OperationType.Rename)
                OpBW.DoWork += Renaming_DoWork;
            else if (OperationType == OperationType.Attributes)
                OpBW.DoWork += ApplyingAttribute_DoWork;
            //else if (operation_type == Op.Extract)
            //    OpBW.DoWork += ExtractingFiles_DoWork;
            OpBW.RunWorkerCompleted += Operation_RunWorkerCompleted;
            OpBW.ProgressChanged += Operation_ProgressChanged;
            OpBW.RunWorkerAsync(root);
        }

        private void Operation_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage < 0)
            {
                progress_bar.Foreground = Brushes.Tomato;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                if (((int)e.UserState) == 1)
                    new Exists_Files_Window(already_exist, paste_in).ShowThisDialog();
                else if (((int)e.UserState) == 2)
                    new File_s_Transferring_Error(unsucsess, null).ShowThisDialog();
                progress_bar.Foreground = Brushes.RoyalBlue;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                sleep_thread = false;
            }
            else
            {
                progress_bar.Value = progress_value;
                double val = 0;
                switch (operation_type)
                {
                    case OperationType.Copy:
                        val = progress_bar.Value * 100 / (root.size + double.Epsilon);
                        cut_sign_image.Visibility = file_is_moved ? Visibility.Visible : Visibility.Collapsed;
                        ChangeRemain(root.size - progress_value, false);
                        break;
                    case OperationType.Delete:
                    case OperationType.Attributes:
                        val = progress_bar.Value * 100.0 / (root.files_count + root.directory_count + double.Epsilon);
                        ChangeRemain(root.files_count + root.directory_count - progress_value, true);
                        break;
                    case OperationType.Rename:
                        val = progress_bar.Value * 100.0 / (root.children.Count + double.Epsilon);
                        ChangeRemain(root.children.Count - progress_value, true);
                        break;
                    case OperationType.Extract:
                        val = progress_value;
                        ChangeRemain((root.size * (100 - progress_value)) / 100, false);
                        break;
                }
                percent.Text = Math.Round(val, 0) + "%";
                TaskbarItemInfo.ProgressValue = val / 100;
                completed.Text = completed_count.ToString();

                if (progress_rount % 20 == 0)
                {
                    old.Add(old_progress_value);
                    newnew.Add(progress_value);
                    progress_rount = 1;
                    if (operation_type == OperationType.Copy)
                        speed.Text = Functions.FileSize((progress_value - old_progress_value) / 4, 1) + " per second";
                    else if (operation_type == OperationType.Delete || operation_type == OperationType.Attributes)
                    {
                        double x = Math.Round((progress_value - old_progress_value) / 4.0, 1);
                        speed.Text = string.Format("{0} item{1} per {2}", x > 2 ? x : 60 * x, x > 2 ? "s" : x * 60 > 2 ? "s" : "", x > 2 ? "second" : "minute");
                    }
                    else if (operation_type == OperationType.Extract)
                        speed.Text = Functions.FileSize((progress_value - old_progress_value) * root.size / 100.0, 1) + " per second";
                    old_progress_value = progress_value;
                }
                else
                    progress_rount++;

                item_name.ToolTip = item_name.Text = current_file_name;
            }
        }

        private void Operation_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Close();
        }

        void ChangeRemain(double size, bool is_items)
        {
            string remain_number;
            remain_number = Functions.FileSize1000(size, 3);
            remain.Text = remain_number;
            remain_unit.Text = (is_items ? "item" : "byte") + (size > 1 ? "s" : "");

            //remain_title.Width = str[0].Length > 2 ? ((str[0].Length - 3) * 26 + 78) : 65;
            //remain_unit.Margin = new Thickness(100 - (10 * (4 - remain.Text.Length)), 40, 0, 0);
        }

        private void AddError(OperationTree item, OperationTree add_in)
        {
            List<OperationTree> queue = new List<OperationTree>();
            OperationTree i = item;
            while (i != null && i != root)
            {
                queue.Add(i);
                i = i.parent;
            }
            for (int j = queue.Count - 1; j > -1; j--)
            {
                OperationTree k = add_in;
                int index = 0;
                bool continue_search = true;
                if (continue_search && k.Contains(queue[j], out index))
                {
                    k = k.children[index];
                    if (k.error_message.Equals(""))
                        k.error_message = queue[0].error_message;
                }
                else
                {//////////////////ex_,dl_,mp_,wa_.....
                    OperationTree t;
                    if (queue[j].item is DirectoryInfo)
                        t = new OperationTree((DirectoryInfo)queue[j].item, queue[j].is_cut, queue[j].short_path, queue[j].operation, queue[j].parent, false, queue[j].error_message);
                    else
                        t = new OperationTree((FileInfo)queue[j].item, queue[j].is_cut, queue[j].short_path, queue[j].operation, queue[j].parent, queue[j].error_message);
                    k.children.Add(t);
                    k = t;
                    continue_search = false;
                }
            }
        }

        #region Copy and Move Region

        private void CollectFiles_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var i in root.children)
                root.size += i.size;
            CompleteCollectFiles(root.size, Functions.FileSize(root.size, 0));
        }

        private void Copying_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread progress_thread = new Thread(() => 
            {
                while (true)
                {
                    if (!OpBW.IsBusy)
                        break;
                    try
                    { OpBW.ReportProgress(0); }
                    catch (Exception) { }
                    while (sleep_thread)
                        Thread.Sleep(100);
                    while (pause_copy)
                        Thread.Sleep(100);
                    Thread.Sleep(200);
                }
            });
            progress_thread.Start();
            already_exist = new OperationTree(null, root.is_cut, "", operation_type, null, false, "File already exist");
            unsucsess = new OperationTree(null, root.is_cut, "", operation_type, null, false, "Some thing wrong");
            OperationTree temp;
            foreach (var i in root.children)
                CopySun(i);
            while (true)
            {
                if (already_exist.children.Count > 0)
                {
                    sleep_thread = true;
                    OpBW.ReportProgress(-1, 1);
                    while (sleep_thread)
                        Thread.Sleep(100);
                    temp = new OperationTree(null, root.is_cut, "", operation_type, null, false, "");
                    foreach (var i in already_exist.children)
                        temp.children.Add(i);
                    already_exist.children.Clear();
                    foreach (var i in temp.children)
                    {
                        if (i.user_option == OperationOption.Skip)
                        {
                            i.Done = true;
                            progress_value += i.size;
                            completed_count++;
                        }
                        else
                            CopySun(i);
                    }
                }
                if (unsucsess.children.Count > 0)   //undeleted
                {
                    sleep_thread = true;
                    OpBW.ReportProgress(-1, 2);
                    while (sleep_thread)
                        Thread.Sleep(100);
                    temp = new OperationTree(null, root.is_cut, "", operation_type, null, false, "");
                    foreach (var i in unsucsess.children)
                        temp.children.Add(i);
                    unsucsess.children.Clear();
                    foreach (var i in temp.children)
                        if (i.user_option == OperationOption.Skip)
                        {
                            i.Done = true;
                            progress_value += i.size;
                            completed_count++;
                        }
                        else
                            CopySun(i);
                }
                if (unsucsess.children.Count == 0 && already_exist.children.Count == 0)
                    break;
            }
        }

        private void CopySun(OperationTree i)
        {
            if (!i.Done)
            {
                if (i.children == null)
                {
                    current_file_name = i.item.FullName;
                    file_is_moved = i.is_cut;
                    if (i.user_option == OperationOption.Skip)
                    {
                        i.Done = true;
                        progress_value += i.size;
                        completed_count++;
                    }
                    else
                    {
                        if (CreatePath(paste_in + "\\" + i.short_path, 1))
                            if (File.Exists(paste_in + "\\" + i.short_path) && i.user_option == OperationOption.NoValue)
                            {
                                i.error_message = "File already existe.";
                                AddError(i, already_exist);
                            }
                            else
                            {
                                DriveInfo target_drive = new DriveInfo(paste_in[0] + ":\\");
                                if (target_drive.IsReady && target_drive.AvailableFreeSpace > i.size)
                                    if (!((target_drive.DriveFormat.ToLower() == "fat" || target_drive.DriveFormat.ToLower() == "fat32") && (i.size > 4294967295)))
                                        WriteFile(i);
                                    else
                                    {
                                        i.error_message = "Target's drive's file format can't fit files more than 4 GB.";
                                        AddError(i, unsucsess);
                                    }
                                else
                                {
                                    i.error_message = "Target drive's free space is less than file size";
                                    AddError(i, unsucsess);
                                }
                            }
                        else
                        {
                            i.error_message = "Can't write file on drive.";
                            AddError(i, unsucsess);
                        }
                    }
                }
                else
                {
                    CreatePath(paste_in + "\\" + i.short_path, 0);
                    foreach (var j in i.children)
                        CopySun(j);
                    if (i.children.Count == i.completed)
                    {
                        i.Done = true;
                        if (i.is_cut)
                        {
                            if (((DirectoryInfo)i.item).GetFiles().Length + ((DirectoryInfo)i.item).GetDirectories().Length == 0)
                                try { i.item.Delete(); }
                                catch (Exception) { i.error_message = "Can't delete directory"; AddError(i, unsucsess); }
                            else
                            {
                                i.error_message = "Can't delete directory, it is not empty.";
                                AddError(i, unsucsess);
                            }
                        }
                    }
                }
            }
            else if (i.user_option == OperationOption.Retry && i.is_cut)
            {
                try
                { i.item.Delete(); }
                catch (Exception) { i.error_message = "Can't delete file"; AddError(i, unsucsess); }
            }
        }

        private bool CreatePath(string path, int to)
        {
            //try
            //{
            //    Directory.CreateDirectory(path);
            //    return true;
            //}
            //catch(Exception)
            //{ return false; }
            string[] s = path.Split(Functions.spliter, StringSplitOptions.RemoveEmptyEntries);
            string pp = s[0];
            for (int i = 1; i < s.Length - to; i++)
            {
                pp += ("\\" + s[i]);
                if (!Directory.Exists(pp))
                    try { Directory.CreateDirectory(pp); }
                    catch (Exception) { return false; }
            }
            return true;
        }

        void WriteFile(OperationTree i)
        {
            //error while copying
            string new_path = paste_in + "\\" + i.short_path;
            bool copy = true;
            if (File.Exists(paste_in + "\\" + i.short_path))
                if (i.user_option == OperationOption.Over)
                    File.Delete(paste_in + "\\" + i.short_path);
                else if (i.user_option == OperationOption.CopyWithNewName)
                {
                    FileInfo target_file = new FileInfo(new_path);
                    string extention = target_file.Extension;
                    string name;
                    name = extention.Length > 0 ? target_file.Name.Remove(target_file.Name.Length - extention.Length) : target_file.Name;
                    int num = 1;
                    while (File.Exists(paste_in + "\\" + name + " (" + num + ")" + extention)) num++;
                    new_path = paste_in + "\\" + name + " (" + num + ")" + extention;
                }
            if (copy && CreatePath(new_path, 1))
            {
                if (i.item.FullName[0] == paste_in[0] && i.is_cut)
                {
                    try
                    {
                        File.Move(i.item.FullName, new_path);
                        progress_value += i.size;
                        completed_count++;
                    }
                    catch(Exception ex)
                    {
                        i.error_message = ex.Message;
                        unsucsess.children.Add(i);
                    }
                }
                else
                {
                    BR = new BinaryReader(File.OpenRead(i.item.FullName));
                    BW = new BinaryWriter(File.OpenWrite(new_path));
                    long k;
                    for (k = Functions.buffer; k <= i.size; k += Functions.buffer)
                    {
                        while (pause_copy)
                            Thread.Sleep(250);
                        BW.Write(BR.ReadBytes(Functions.buffer));
                        progress_value += Functions.buffer;
                    }
                    long remain = (i.size - (k - Functions.buffer));
                    BW.Write(BR.ReadBytes((int)remain));
                    progress_value += remain;
                    BW.Close();
                    BR.Close();
                    try
                    {
                        File.SetAttributes(new_path, File.GetAttributes(i.item.FullName));
                        File.SetCreationTime(new_path, i.item.CreationTime);
                        File.SetCreationTimeUtc(new_path, i.item.CreationTimeUtc);
                        File.SetLastAccessTime(new_path, i.item.LastAccessTime);
                        File.SetLastAccessTimeUtc(new_path, i.item.LastAccessTimeUtc);
                        File.SetLastWriteTime(new_path, i.item.LastWriteTime);
                        File.SetLastWriteTimeUtc(new_path, i.item.LastWriteTimeUtc);
                    }
                    catch (Exception) { }
                    i.Done = true;
                    if(i.is_cut)
                        try
                        { i.item.Delete(); }
                        catch (Exception) { i.error_message = "Can't delete file"; AddError(i, unsucsess); }
                    completed_count++;
                }
            }
        }

        #endregion

        #region Delete Region

        private void CollectFilesToDelete_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (FileOperationItem i in (List<FileOperationItem>)e.Argument)
                if (i.Item is FileInfo)
                    root.children.Add(new OperationTree((FileInfo)i.Item, false, i.short_path, OperationType.Delete, root, ""));
                else
                {
                    if (Classes.JunctionPoint.Exists(i.Item.FullName))
                        try
                        { Classes.JunctionPoint.Delete(i.Item.FullName); }
                        catch (Exception ex) { AddError(new OperationTree((DirectoryInfo)i.Item, true, i.short_path, operation_type, root, true, ex.Message), unsucsess); }
                    else
                        root.children.Add(new OperationTree((DirectoryInfo)i.Item, false, i.short_path, operation_type, root, true, ""));
                }
        }

        private void CollectFilesToDelete_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var i in root.children)
            {
                root.directory_count += i.directory_count;
                root.files_count += i.files_count;
            }
            root.directory_count--; //it self it's not a directory its just container
            CompleteCollectFiles(root.directory_count + root.files_count, (root.directory_count + root.files_count) + " item" + ((root.directory_count + root.files_count > 1) ? "s" : ""));
        }

        private void Deleting_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread progress_thread = new Thread(() =>
            {
                while (true)
                {
                    if (!OpBW.IsBusy)
                        break;
                    OpBW.ReportProgress(0);
                    while (sleep_thread || pause_copy)
                        Thread.Sleep(100);
                    System.Threading.Thread.Sleep(200);
                }
            });
            progress_thread.Start();
            OperationTree temp;
            foreach (var i in root.children)
            {
                DeleteSun(i);
                while (pause_copy)
                    Thread.Sleep(100);
            }
            while (true)
            {
                if (unsucsess.children.Count > 0)
                {
                    sleep_thread = true;
                    OpBW.ReportProgress(-1, 2);
                    while (sleep_thread)
                        Thread.Sleep(100);
                    temp = new OperationTree(null, false, "", operation_type, null, false, "");
                    foreach (var i in unsucsess.children)
                        temp.children.Add(i);
                    unsucsess.children.Clear();
                    foreach (var i in temp.children)
                        if (i.user_option == OperationOption.Skip)
                        {
                            i.Done = true;
                            progress_value++;
                            completed_count++;
                        }
                        else
                            DeleteSun(i);
                }
                if (unsucsess.children.Count == 0)
                    break;
            }
        }

        private void DeleteSun(OperationTree i)
        {
            if (!i.Done)
            {
                if (i.children == null)
                {
                    current_file_name = i.item.FullName;
                    if (i.user_option == OperationOption.Skip)
                    {
                        i.Done = true;
                        progress_value++;
                        completed_count++;
                    }
                    else
                    {
                        if (i.item.Exists)
                            try
                            {
                                i.item.Delete();
                                i.Done = true;
                                progress_value++;
                                completed_count++;
                            }
                            catch (Exception ex) { i.error_message = ex.Message; AddError(i, unsucsess); }
                        else
                        {
                            i.error_message = "file is not exist.";
                            AddError(i, unsucsess);
                        }
                    }
                }
                else
                {
                    if (i.is_cut)
                    {
                        if (Classes.JunctionPoint.Exists(i.item.FullName))
                            try
                            { Classes.JunctionPoint.Delete(i.item.FullName); }
                            catch (Exception ex)
                            {
                                i.error_message = ex.Message;
                                AddError(i, unsucsess);
                            }
                    }
                    else
                    {
                        foreach (var j in i.children)
                        {
                            DeleteSun(j);
                            while (pause_copy)
                                Thread.Sleep(100);
                        }
                        if (i.children.Count == i.completed)
                        {
                            if (i.item.Exists)
                            {
                                long d = 0;
                                try
                                { d = ((DirectoryInfo)i.item).GetDirectories().Length + ((DirectoryInfo)i.item).GetFiles().Length; }
                                catch (Exception) { }
                                if (d == 0)
                                {
                                    try
                                    {
                                        i.item.Delete();
                                        i.Done = true;
                                        progress_value++;
                                        completed_count++;
                                    }
                                    catch (Exception ex) { i.error_message = ex.Message; AddError(i, unsucsess); }
                                }
                                else
                                {
                                    i.error_message = "Can't delete directory, it is not empty.";
                                    AddError(i, unsucsess);
                                }
                            }
                            else
                            {
                                i.error_message = "Directory is not exist.";
                                AddError(i, unsucsess);
                            }
                        }
                    }
                }
            }
        }

        //private void Deleting_ProgressChanged(object sender, ProgressChangedEventArgs e)
        //{
        //    if (e.ProgressPercentage < 0)
        //    {
        //        if (((int)e.UserState) == 2)
        //            new File_s_Transferring_Error(unsucsess, paste_in).ShowThisDialog();
        //        sleep_thread = false;
        //    }
        //    else
        //    {
        //        progress_bar.Value = progress_value;
                
        //        double val = progress_bar.Value * 100 / root.files_count + root.directory_count;
        //        percent.Text = Math.Round(val, 0) + "%";
        //        TaskbarItemInfo.ProgressValue = val / 100;
        //        completed.Text = completed_count.ToString();
        //        ChangeRemain(root.size - progress_value, false);
        //        if (progress_rount % 20 == 0)
        //        {
        //            progress_rount = 1;
        //            if (old_progress_value != 0)
        //                speed.Text = Math.Round(progress_value - old_progress_value / 2.0, 1) + " items per second";
        //            old_progress_value = progress_value;
        //        }
        //        else
        //            progress_rount++;
        //        item_name.ToolTip = item_name.Text = current_file_name;
        //    }
        //}

        #endregion

        #region Rename Region

        private void CollectFilesToRename_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CompleteCollectFiles(root.children.Count, root.children.Count + " item" + ((root.directory_count + root.files_count > 1) ? "s" : ""));
        }

        private void Renaming_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread progress_thread = new Thread(() =>
            {
                while (true)
                {
                    if (!OpBW.IsBusy)
                        break;
                    try { OpBW.ReportProgress(0); }
                    catch (Exception) { }
                    while (sleep_thread || pause_copy)
                        Thread.Sleep(100);
                    System.Threading.Thread.Sleep(200);
                }
            });
            progress_thread.Start();
            unsucsess = new OperationTree(null, false, "", operation_type, null, false, "Can't Rename item");
            OperationTree temp;
            foreach (var i in root.children)
                RenameSun(i);
            while (true)
            {
                if (unsucsess.children.Count > 0)
                {
                    sleep_thread = true;
                    OpBW.ReportProgress(-1, 2);
                    while (sleep_thread)
                        Thread.Sleep(100);
                    temp = new OperationTree(null, false, "", operation_type, null, false, "");
                    foreach (var i in unsucsess.children)
                        temp.children.Add(i);
                    unsucsess.children.Clear();
                    foreach (var i in temp.children)
                        if (i.user_option == OperationOption.Skip)
                        {
                            i.Done = true;
                            progress_value++;
                            completed_count++;
                        }
                        else
                            RenameSun(i);
                }
                if (unsucsess.children.Count == 0)
                    break;
            }
        }

        private void RenameSun(OperationTree i)
        {
            if (!i.Done)
            {
                //paste_in = "";
                current_file_name = i.item.FullName;
                if (i.user_option == OperationOption.Skip || i.item.FullName == i.item.FullName.Substring(0, i.item.FullName.LastIndexOf('\\')) + "\\" + i.short_path)
                {
                    i.Done = true;
                    progress_value++;
                    completed_count++;
                }
                else if (i.item.Exists)
                {
                    string new_name;
                    if (File.Exists(i.item.FullName.Substring(0, i.item.FullName.LastIndexOf('\\')) + "\\" + i.short_path))
                    {
                        FileInfo target_file = new FileInfo(i.item.FullName.Substring(0, i.item.FullName.LastIndexOf('\\')) + "\\" + i.short_path);
                        string name;
                        name = target_file.Extension.Length > 0 ? target_file.Name.Remove(target_file.Name.Length - target_file.Extension.Length) : target_file.Name;
                        int num = 1;
                        while (File.Exists(name + i.short_path + "\\" + " (" + num + ")" + target_file.Extension)) num++;
                        new_name = i.item.FullName.Substring(0, i.item.FullName.LastIndexOf('\\')) + "\\" + i.short_path + " (" + num + ")" + target_file.Extension;
                    }
                    else
                        new_name = i.item.FullName.Substring(0, i.item.FullName.LastIndexOf('\\')) + "\\" + i.short_path;
                    try
                    {
                        if (i.children == null)
                            ((FileInfo)i.item).MoveTo(new_name);
                        else
                            ((DirectoryInfo)i.item).MoveTo(new_name);
                        i.Done = true;
                        progress_value++;
                        completed_count++;
                    }
                    catch (Exception ex) { i.error_message = ex.Message; AddError(i, unsucsess); }
                }
                else
                {
                    i.error_message = "file is not exist.";
                    AddError(i, unsucsess);
                }
            }
        }

        #endregion

        #region Attribute Region

        private void CollectFilesToAttribute_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var i in root.children)
            {
                root.directory_count += i.directory_count;
                root.files_count += i.files_count;
            }
            root.directory_count--; //it self it's not a directory its just container
            CompleteCollectFiles(root.directory_count + root.files_count, (root.directory_count + root.files_count) + " item" + ((root.directory_count + root.files_count > 1) ? "s" : ""));
        }

        private void ApplyingAttribute_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread progress_thread = new Thread(() =>
            {
                while (true)
                {
                    if (!OpBW.IsBusy)
                        break;
                    try
                    { OpBW.ReportProgress(0); }
                    catch (Exception) { }
                    while (sleep_thread || pause_copy)
                        Thread.Sleep(100);
                    Thread.Sleep(200);
                }
            });
            progress_thread.Start();
            unsucsess = new OperationTree(null, false, "", operation_type, null, false, "Can't applying attribute to item");
            OperationTree temp;
            foreach (var i in root.children)
                ApplyingAttributeToSun(i);
            while (true)
            {
                if (unsucsess.children.Count > 0)
                {
                    sleep_thread = true;
                    OpBW.ReportProgress(-1, 2);
                    while (sleep_thread)
                        Thread.Sleep(100);
                    temp = new OperationTree(null, false, "", operation_type, null, false, "");
                    foreach (var i in unsucsess.children)
                        temp.children.Add(i);
                    unsucsess.children.Clear();
                    foreach (var i in temp.children)
                        if (i.user_option == OperationOption.Skip)
                        {
                            i.Done = true;
                            progress_value++;
                            completed_count++;
                        }
                        else
                            ApplyingAttributeToSun(i);
                }
                if (unsucsess.children.Count == 0)
                    break;
            }
        }

        private void ApplyingAttributeToSun(OperationTree i)
        {
            if (!i.Done)
            {
                if (i.children == null)
                {
                    current_file_name = i.item.FullName;
                    if (i.user_option == OperationOption.Skip)
                    {
                        i.Done = true;
                        progress_value++;
                        completed_count++;
                    }
                    else
                    {
                        if (i.item.Exists)
                            ApplyAttribute(i);
                        else
                        {
                            i.error_message = "file is not exist.";
                            AddError(i, unsucsess);
                        }
                    }
                }
                else
                {
                    foreach (var j in i.children)
                        ApplyingAttributeToSun(j);
                    if (i.item.Exists)
                        ApplyAttribute(i);
                    else
                    {
                        i.error_message = "Directory is not exist.";
                        AddError(i, unsucsess);
                    }

                }
            }
        }

        private void ApplyAttribute(OperationTree i)
        {
            try
            {
                FileAttributes fa = (FileAttributes)i.short_path;
                int result = -1;
                if (!i.item.Attributes.HasFlag(FileAttributes.Compressed) && fa.HasFlag(FileAttributes.Compressed))
                    result = (i.item is DirectoryInfo) ? DirectoryCompress(i.item) : FileCompress(i.item.FullName);
                else if (!i.item.Attributes.HasFlag(FileAttributes.Encrypted) && fa.HasFlag(FileAttributes.Encrypted))
                    File.Encrypt(i.item.FullName);
                else if (i.item.Attributes.HasFlag(FileAttributes.Compressed) && !fa.HasFlag(FileAttributes.Compressed))
                    result = (i.item is DirectoryInfo) ? DirectoryDecompress(i.item) : FileDecompress(i.item.FullName);
                else if (i.item.Attributes.HasFlag(FileAttributes.Encrypted) && !fa.HasFlag(FileAttributes.Encrypted))
                    File.Decrypt(i.item.FullName);
                //if (i.item.Attributes.HasFlag(FileAttributes.Compressed) != fa.HasFlag(FileAttributes.Compressed))
                //{
                //    if (fa.HasFlag(FileAttributes.Compressed))
                //        result = (i.item is DirectoryInfo) ? DirectoryCompress(i.item) : FileCompress(i.item.FullName);
                //    else
                //        result = (i.item is DirectoryInfo) ? DirectoryDecompress(i.item) : FileDecompress(i.item.FullName);
                //}
                //if (i.item.Attributes.HasFlag(FileAttributes.Encrypted) != fa.HasFlag(FileAttributes.Encrypted))
                //    if (fa.HasFlag(FileAttributes.Encrypted))
                //        File.Encrypt(i.item.FullName);
                //    else
                //        File.Decrypt(i.item.FullName);
                i.item.Attributes = fa;
                if (result == 0)
                {
                    i.error_message = "Can't compress / decompress item.";
                    AddError(i, unsucsess);
                }
                else
                {
                    i.Done = true;
                    progress_value++;
                    completed_count++;
                }
            }
            catch (Exception ex) { i.error_message = ex.Message; AddError(i, unsucsess); }
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

        int DirectoryCompress(FileSystemInfo dir)
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

        int DirectoryDecompress(FileSystemInfo dir)
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

        #endregion

        //#region Extract
        //Process P;
        //private void CollectFilesToExtract_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        //{
        //    root.size = 0;
        //    CompleteCollectFiles(root.size, Functions.FileSize(root.size, 0));
        //}

        //private void ExtractingFiles_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    int done = -1;
        //    string error_message = "";
        //    Thread tt = new Thread(() =>
        //    {
        //        try
        //        {
        //            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Temp\archive.tmp";
        //            StreamWriter st = new StreamWriter(File.Open(path, FileMode.OpenOrCreate));
        //            StringBuilder files_paths = new StringBuilder();
        //            foreach (var i in root.Children)
        //                st.WriteLine(i.FileFullName);
        //            st.Close();
        //            string compress_method = " -p\"";
        //            if (encrypt)
        //                compress_method = " -hp\"";
        //            foreach (var i in root.Children)
        //                files_paths.Append("\"" + i.FileFullName + "\" ");
        //            switch (compress_tool)
        //            {
        //                case @"Tools\compress\7z\7z.exe":
        //                    Compress(" a \"" + file_path + "\" @\"" + path + "\" " + (type_extention == "" ? "" : ("-t" + type_extention)) + (sfx_value ? " -sfx7z.sfx" : "") + (password_st == string.Empty ? "" : (" -p\"" + password_st + "\"")) + (encrypt_headers ? " -mhe" : String.Empty) + (type_extention != "wim" && type_extention != "tar" ? " -mx=" + compress_level[level_value] : "") + (format_value == 0 ? " -ms=" + sold_block_size[solid] : ""));
        //                    break;
        //                case @"Tools\compress\rar\rar.exe":
        //                    Compress("a -m" + (level_value > 1 ? level_value + 1 : level_value) + (solid > 0 ? " -s" : "") + (" -ma" + (format_value - 6)) + (sfx_value ? " -sfx" : "") + (password_st == string.Empty ? "" : (compress_method + password_st + "\"")) + " \"" + file_path + "\" " + files_paths + "");
        //                    break;
        //                case @"Tools\compress\arc\arc.exe":
        //                    Compress("a -m" + compress_level[level_value] + (" -s" + sold_block_size[solid]) + (sfx_value ? " -sfx" : "") + (password_st == String.Empty ? "" : (" -p\"" + password_st + "\"")) + " \"" + file_path + "\" @\"" + path + "\" ");
        //                    break;
        //                case @"Tools\compress\kgb\kgb2_console.exe":
        //                    foreach (var i in root.Children)
        //                        files_paths.Append("\"" + i.FileFullName + "\" ");
        //                    Compress("-a7 -m" + compress_level[level_value] /*+ (sfx.IsChecked.Value ? " -sfx" : "")*/ + (password_st == String.Empty ? "" : (" -p\"" + password_st + "\"")) + " \"" + file_path + "\" " + files_paths.ToString());
        //                    break;
        //                case @"Tools\compress\uha\uha.exe":
        //                    Compress("a -r+ -ed- -pr m" + (level_value < 4 ? level_value : 3) + (level_value == 4 ? " -md32768" : "") + " -mm+ -md+ -b32768" + (password_st == String.Empty ? "" : (" -pw\"" + password_st + "\"")) + (encrypt_headers ? " -ph" : String.Empty) + " \"" + file_path + "\" " + files_paths);
        //                    break;
        //            }
        //            File.Delete(path);
        //            done = 0;
        //            if (root.Children.Count == 1 && root.Children[0].IsDirectory || root.Children.Count > 1)
        //                try
        //                {
        //                    switch (format_value)
        //                    {
        //                        case 4:
        //                            MultiStageCompress("bzip2");
        //                            break;
        //                        case 6:
        //                            MultiStageCompress("gzip");
        //                            break;
        //                        case 17:
        //                            MultiStageCompress("xz");
        //                            break;
        //                    }
        //                }
        //                catch (Exception ex) { done = 1; error_message = ex.Message; }
        //        }
        //        catch (Exception ex) { done = 1; error_message = ex.Message; }
        //    });
        //    tt.Start();
        //}

        //void bw_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    while (done < 0)
        //        Thread.Sleep(100);
        //    exit_code = P.ExitCode;
        //}

        //void Compress(string arguments)
        //{
        //    ProcessStartInfo psi = new ProcessStartInfo(compress_tool, arguments);
        //    P = new Process();
        //    P.StartInfo = psi;
        //    P.Start();
        //    P.WaitForExit();
        //}

        //void MultiStageCompress(string format)
        //{
        //    FileInfo n = new FileInfo(file_name.Text);
        //    n.MoveTo(n.FullName.Remove(n.FullName.Length - n.Extension.Length) + ".tar");
        //    Compress(" a \"" + file_name.Text + "\" \"" + n.FullName + "\" -t" + format + " -mx=" + compress_level[level.SelectedIndex]);
        //    n.Delete();
        //}

        //#endregion

        #region Extract
        internal void ExtractDialogShow(object archive, string extract_in, object files_names/*null to extract all*/, OperationType operation_type, string parameter1, bool parameter2)
        {
            //////////this.operation_type = operation_type;
            //////////if (operation_type == Op.Extract)
            //////////{
            //////////    paste_in = extract_in;
            //////////    pause_button.IsEnabled = false;
            //////////    root = new OperationTree(null, false, "", operation_type, null, false, "");
            //////////    List<string> names = new List<string>();
            //////////    OpBW = new BackgroundWorker();
            //////////    ulong total_size = 0;
            //////////    foreach (CompresserListView i in (System.Collections.IList)files_names)
            //////////        if (i.IsDirectory)
            //////////            GetAllContaint(i, names, ref total_size);
            //////////        else
            //////////        { names.Add(i.FileFullName); total_size += i.file_size_long; }

            //////////    root.short_path = names.ToArray();
            //////////    if (files_names == null)
            //////////        root.size = ((SevenZipExtractor)archive).PackedSize;
            //////////    else
            //////////        root.size = (long)total_size;
            //////////    progress_bar.Foreground = Brushes.RoyalBlue;
            //////////    progress_bar.Value = 0;
            //////////    progress_bar.Maximum = 100;
            //////////    ChangeRemain(((SevenZipExtractor)archive).FilesCount, true);
            //////////    Title = Title + " (Extracting...)";
            //////////    OpBW = new BackgroundWorker();
            //////////    OpBW.WorkerReportsProgress = true;
            //////////    OpBW.DoWork += Extracting_DoWork;
            //////////    OpBW.RunWorkerCompleted += Operation_RunWorkerCompleted;
            //////////    OpBW.ProgressChanged += Operation_ProgressChanged;
            //////////    OpBW.RunWorkerAsync(archive);
            //////////    progress_bar.IsIndeterminate = false;
            //////////    ShowDialog();
            //////////}
            //////////else if (operation_type == Op.Compress)
            //////////{
            //////////    Title = Title + " (Comprissing...)";
            //////////    paste_in = extract_in;
            //////////    List<string> list = new List<string>();
            //////////    foreach (CompresserListView i in (List<CompresserListView>)files_names)
            //////////        if (i.IsDirectory)
            //////////        {
            //////////            AddAllSuns(i.FileFullName, list);
            //////////            //list.Add(i.FileFullName);
            //////////        }
            //////////        else list.Add(i.FileFullName);
            //////////    root = new OperationTree(null, false, list.ToArray(), operation_type, null, false, "");
            //////////    OpBW = new BackgroundWorker();
            //////////    OpBW.WorkerReportsProgress = true;
            //////////    OpBW.DoWork += (object k, DoWorkEventArgs e) =>
            //////////    {
            //////////        Thread progress_thread = new Thread(() =>
            //////////        {
            //////////            while (true)
            //////////            {
            //////////                if (!OpBW.IsBusy)
            //////////                    break;
            //////////                OpBW.ReportProgress(0);
            //////////                Thread.Sleep(200);
            //////////            }
            //////////        });
            //////////        progress_thread.Start();
            //////////        try
            //////////        {
            //////////            bool done = false;
            //////////            SevenZipCompressor szc = (SevenZipCompressor)archive;
            //////////            szc.Compressing += (object sender, SevenZip.ProgressEventArgs ex) => { progress_value += ex.PercentDelta; };
            //////////            szc.FileCompressionStarted += (object sender, FileNameEventArgs ex) => { current_file_name = ex.FileName; };
            //////////            szc.FileCompressionFinished += (object sender, EventArgs ex) => { completed_count++; };
            //////////            szc.CompressionFinished += (object sender, EventArgs ex) => { done = true; };
            //////////            szc.FilesFound += (object sender, IntEventArgs ex) => { };
                        
            //////////            //if (root.short_path == null)
            //////////            if (parameter2)
            //////////            {
            //////////                File.Copy(@"tools\compress\7z\sfx.sfx", extract_in);
            //////////                szc.BeginCompressFiles(File.Open(extract_in, FileMode.OpenOrCreate), (string[])root.short_path);
            //////////            }
            //////////            else
            //////////                szc.BeginCompressFiles(paste_in.ToString(), (string[])root.short_path);
            //////////            //else
            //////////            //    sze.BeginExtractFiles(paste_in, (string[])root.short_path);
            //////////            while (!done) { Thread.Sleep(50); }
            //////////        }
            //////////        catch (Exception ex) { string x = ex.Message; }
            //////////    };
            //////////    OpBW.RunWorkerCompleted += Operation_RunWorkerCompleted;
            //////////    OpBW.ProgressChanged += Operation_ProgressChanged;
            //////////    OpBW.RunWorkerAsync();
            //////////    progress_bar.IsIndeterminate = false;
            //////////    ShowDialog();
            //////////}
            //    new My_MessageBox("File Manager", "Invalid archive or wrong passwod.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void AddAllSuns(string directory, List<string> list)
        {
            foreach (var i in Directory.GetDirectories(directory))
            {
                //list.Add(i);
                AddAllSuns(i, list);
            }
            foreach (var i in Directory.GetFiles(directory))
                list.Add(i);
        }

        //private void Compressing_DoWork(object compresser, DoWorkEventArgs e)
        //{
        //    Thread progress_thread = new Thread(() =>
        //    {
        //        while (true)
        //        {
        //            if (!OpBW.IsBusy)
        //                break;
        //            OpBW.ReportProgress(0);
        //            Thread.Sleep(200);
        //        }
        //    });
        //    progress_thread.Start();
        //    try
        //    {
        //        bool done = false;
        //        SevenZipCompressor szc = (SevenZipCompressor)e.Argument;
        //        szc.Compressing += (object sender, SevenZip.ProgressEventArgs ex) => { progress_value = ex.PercentDone; };
        //        szc.FileCompressionStarted += (object sender, FileNameEventArgs ex) => { current_file_name = ex.FileName; completed_count++; };
        //        szc.CompressionFinished += (object sender, EventArgs ex) => { done = true; };
        //        szc.FilesFound += (object sender, IntEventArgs ex) => { };
        //        //if (root.short_path == null)
        //        szc.BeginCompressFiles(paste_in.ToString(), (string[])root.short_path);
        //        //else
        //        //    sze.BeginExtractFiles(paste_in, (string[])root.short_path);
        //        while (!done) { Thread.Sleep(50); }
        //    }
        //    catch (Exception ex) { string x = ex.Message; }
        //}
        

        void GetAllContaint(CompresserListView i, List<string> names, ref ulong size)
        {
            //////foreach (CompresserListView j in i.Children)
            //////    if (j.IsDirectory)
            //////        GetAllContaint(j, names, ref size);
            //////    else
            //////    { names.Add(j.FileFullName); size += i.file_size_long; }
        }

        void Extracting_DoWork(object archive, DoWorkEventArgs e)// sze.Check();
        {
            Thread progress_thread = new Thread(() =>
            {
                while (true)
                {
                    if (!OpBW.IsBusy)
                        break;
                    OpBW.ReportProgress(0);
                    Thread.Sleep(200);
                }
            });
            progress_thread.Start();
            try
            {
                bool done = false;
                SevenZipExtractor sze = (SevenZipExtractor)e.Argument;
                sze.Extracting += (object sender, SevenZip.ProgressEventArgs ex) => { progress_value = ex.PercentDone; };
                sze.FileExtractionStarted += (object sender, FileInfoEventArgs ex) => { current_file_name = ex.FileInfo.FileName; completed_count++; };
                sze.ExtractionFinished += (object sender, EventArgs ex) => { done = true; };
                sze.FileExists += (object sender, FileOverwriteEventArgs ex) => { };
                if (root.short_path == null)
                    sze.BeginExtractArchive(paste_in);
                else
                    sze.BeginExtractFiles(paste_in, (string[])root.short_path);
                while (!done) { Thread.Sleep(50); }
            }
            catch (Exception ex) { string x = ex.Message; }
        }

        #endregion

        #region Destroy

        //improve eror can set file name or other thing
        //scan if path is too long before try copy delete or other OP.



        Dictionary<string, string> drives_format = new Dictionary<string, string>();
        char[] Chars = { '$', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '&', '#', '%', '^', '(', ')', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };

        internal void StartDestroyFiles(List<FileInfo> files_list, DestroyMethod method, int repeat, bool delete, bool show_dialog)
        {
            operation_type = OperationType.Destroy;
            undeleted = new OperationTree(null, false, null, OperationType.Destroy, null, false, "Can not delete file.");
            unsucsess = new OperationTree(null, false, "", operation_type, null, false, "can not destroy file.");
            Title = string.Format("Destroying {0} {1}", files_list.Count, files_list.Count > 1 ? "items" : "item");
            double max_value = 0;
            foreach (FileInfo i in files_list)
            {
                max_value += i.Length;
                while (pause_copy)
                {
                    Thread.Sleep(50);
                    if (CloseWindow)
                        return;
                }
            }
            max_value *= repeat;
            if (method == DestroyMethod.Secure)
                max_value *= 35;
            progress_bar.Maximum = max_value;
            BackgroundWorker destroy_files_BackgroundWorker = new BackgroundWorker();
            destroy_files_BackgroundWorker.WorkerReportsProgress = true;

            destroy_files_BackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                BackgroundWorker bw = (BackgroundWorker)sender;
                Thread process_thread = new Thread(() =>
                {
                    while (bw.IsBusy)
                    {
                        try
                        { bw.ReportProgress(1); }
                        catch (Exception) { }
                        Thread.Sleep(100);
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                    }
                });
                process_thread.Start();

                for (int index = 0; index < files_list.Count; index++)
                {
                    if (CloseWindow) return;
                    while (pause_copy)
                    {
                        Thread.Sleep(50);
                        if (CloseWindow)
                            return;
                    }
                    DestroyFile(files_list[index], method, repeat, delete);
                }

                while (unsucsess.children.Count > 0)
                {

                    sleep_thread = true;
                    destroy_files_BackgroundWorker.ReportProgress(-1);
                    while (sleep_thread)
                        Thread.Sleep(50);
                    OperationTree temp = new OperationTree(null, false, "", operation_type, null, false, "");
                    double rem = max_value;
                    foreach (var i in unsucsess.children)
                    {
                        temp.children.Add(i);
                        //rem -= i.size;
                    }
                    //old_progress_value = progress_value = Convert.ToInt64(rem);
                    unsucsess.children.Clear();
                    foreach (var i in temp.children)
                    {
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                        if (i.user_option == OperationOption.Skip)
                        {
                            i.Done = true;
                            progress_value += (i.size * repeat * (method == DestroyMethod.Secure ? 35 : 1));
                        }
                        else
                        {
                            if (CloseWindow) return;
                            DestroyFile((FileInfo)i.item, method, repeat, delete);
                        }
                    }
                }

                while (undeleted.children.Count > 0)
                {
                    sleep_thread = true;
                    destroy_files_BackgroundWorker.ReportProgress(-2);
                    while (sleep_thread)
                        Thread.Sleep(50);
                    OperationTree temp = new OperationTree(null, false, "", operation_type, null, false, "");
                    foreach (var i in undeleted.children)
                        temp.children.Add(i);
                    undeleted.children.Clear();
                    foreach (var i in temp.children)
                    {
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                        if (i.user_option == OperationOption.Skip)
                            i.Done = true;
                        else
                        {
                            if (CloseWindow) return;
                            try { i.item.Delete(); }
                            catch (Exception) { undeleted.children.Add(i); }
                        }
                    }
                }
            };

            destroy_files_BackgroundWorker.ProgressChanged += Destroy_ProgressChanged;

            destroy_files_BackgroundWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) => { Close(); };
            progress_bar.IsIndeterminate = false;
            destroy_files_BackgroundWorker.RunWorkerAsync();
            if (show_dialog)
                ShowDialog();
            else
                Show();
        }

        internal void StartDestroyDisk(DriveInfo drive, DestroyMethod method, int repeat, bool delete, bool show_dialog)
        {
            operation_type = OperationType.DiskDestroy;
            undeleted = new OperationTree(null, false, null, OperationType.Destroy, null, false, "Can not delete file.");
            operation.Text = Title = string.Format("Destroying {0}", drive.Name);
            ProgressBrush = Brushes.Maroon;
            progress_bar.Maximum = drive.AvailableFreeSpace * repeat * (method == DestroyMethod.Secure ? 35 : 1);
            BackgroundWorker destroy_disk_BackgroundWorker = new BackgroundWorker();
            destroy_disk_BackgroundWorker.WorkerReportsProgress = true;

            destroy_disk_BackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                BackgroundWorker bw = (BackgroundWorker)sender;
                Thread process_thread = new Thread(() =>
                {
                    while (bw.IsBusy)
                    {
                        bw.ReportProgress(1);
                        Thread.Sleep(100);
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                    }
                });
                process_thread.Start();

                int file_creating_error = 0;
                List<string> files_name = new List<string>();
                long max_file_size = 2147483648;
                switch (drive.DriveFormat.ToLower())
                {
                    case "ntfs":
                        max_file_size = 17592186044416;
                        break;
                    case "fat16":
                        max_file_size = 2147483648;
                        break;
                    case "fat32":
                        max_file_size = 4294967294;
                        break;
                    case "exfat":
                        max_file_size = long.MaxValue;
                        break;
                    case "fat":
                        max_file_size = 2147483648;
                        break;
                    case "fat12":
                        max_file_size = 16777216;
                        break;
                }
                while (drive.IsReady && drive.TotalFreeSpace > max_file_size && file_creating_error < 3)
                {
                    if (CloseWindow) return;
                    while (pause_copy)
                    {
                        Thread.Sleep(50);
                        if (CloseWindow)
                            return;
                    }
                    PrepareAndWriteFile(drive, files_name, max_file_size, method, bw, ref file_creating_error);
                }

                while (drive.IsReady && drive.TotalFreeSpace > 0 && drive.AvailableFreeSpace > 1 && file_creating_error < 3)
                {
                    if (CloseWindow) return;
                    while (pause_copy)
                    {
                        Thread.Sleep(50);
                        if (CloseWindow)
                            return;
                    }
                    PrepareAndWriteFile(drive, files_name, drive.AvailableFreeSpace - 1, method, bw, ref file_creating_error);
                }

                while (true)
                {
                    try
                    {
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                        if (CloseWindow) return;
                        string path;
                        File.Create(path = GetFileName(drive.Name)).Close();
                        files_name.Add(path);
                    }
                    catch (Exception ex) { string f = ex.Message; break; }
                }

                if (file_creating_error > 2)
                {
                    sleep_thread = true;
                    destroy_disk_BackgroundWorker.ReportProgress(-3, "Can not destrory deleted files on this drive" + (delete ? ", try to cleaning up..." : "."));
                    while (sleep_thread)
                        Thread.Sleep(100);
                }
                StringBuilder message = new StringBuilder();
                message.AppendLine("Some files have not deleted successfull you can delete them manually:");
                if (delete)
                {
                    for (int i = files_name.Count - 1; i >= 0; i--)
                        try
                        { File.Delete(files_name[i]); }
                        catch (Exception ex)
                        { undeleted.children.Add(new OperationTree(new FileInfo(files_name[i]), false, "", OperationType.DiskDestroy, root, ex.Message)); }
                }

                while (undeleted.children.Count > 0)
                {
                    while (pause_copy)
                    {
                        Thread.Sleep(50);
                        if (CloseWindow)
                            return;
                    }
                    sleep_thread = true;
                    destroy_disk_BackgroundWorker.ReportProgress(-2);
                    while (sleep_thread)
                        Thread.Sleep(200);
                    OperationTree temp = new OperationTree(null, false, "", operation_type, null, false, "");
                    foreach (var i in undeleted.children)
                        temp.children.Add(i);
                    undeleted.children.Clear();
                    foreach (var i in temp.children)
                    {
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                        if (i.user_option == OperationOption.Skip)
                            i.Done = true;
                        else
                        {
                            if (CloseWindow) return;
                            try { i.item.Delete(); }
                            catch (Exception) { undeleted.children.Add(i); }
                        }
                    }
                }
            };

            destroy_disk_BackgroundWorker.ProgressChanged += Destroy_ProgressChanged;

            destroy_disk_BackgroundWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) => { Close(); };
            progress_bar.IsIndeterminate = false;
            destroy_disk_BackgroundWorker.RunWorkerAsync();
            if (show_dialog)
                ShowDialog();
            else
                Show();
        }

        private void Destroy_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage < 0)
            {
                progress_bar.Foreground = Brushes.Tomato;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                if (e.ProgressPercentage == -1)
                    new File_s_Transferring_Error(unsucsess, "We can not destroy these files, what do you want to do with them?").ShowThisDialog();
                else if (e.ProgressPercentage == -2)
                    new File_s_Transferring_Error(undeleted, "We destroy these files but they can not been deleted, what do you want to do with them?").ShowThisDialog();
                else if (e.ProgressPercentage == -3)
                    Messages.ShowMessage(e.UserState.ToString(), ModrenMessageButton.OK, e.UserState is StringBuilder ? ModrenMessageImage.Exclamation : ModrenMessageImage.Stop, e.UserState is StringBuilder ? ModrenMessageSound.Exclamation : ModrenMessageSound.Stop, false, "");
                progress_bar.Foreground = ProgressBrush;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                sleep_thread = false;
            }
            else
            {
                progress_bar.Value = progress_value;
                double val = 0;
                val = progress_bar.Value * 100.0 / (progress_bar.Maximum + double.Epsilon);
                ChangeRemain(progress_bar.Maximum - progress_value, false);
                percent.Text = Math.Round(val, 0) + "%";
                TaskbarItemInfo.ProgressValue = val / 100;
                completed.Text = completed_count.ToString();
                if (progress_rount % 20 == 0)
                {
                    progress_rount = 1;
                    speed.Text = Functions.FileSize((progress_value - old_progress_value) / 2, 1) + " per second";
                    old_progress_value = progress_value;
                }
                else
                    progress_rount++;
                item_name.Text = current_file_name;
                if (name_textblock.ActualWidth > ActualWidth - name_textblock.Margin.Left - name_textblock.Margin.Right)
                    item_name.ToolTip = current_file_name;
            }
        }

        private void DestroyFile(FileInfo fileInfo, DestroyMethod method, int repeat, bool delete)
        {
            current_file_name = fileInfo.FullName;
            try
            {
                string new_name = GetFileName(fileInfo.DirectoryName);
                fileInfo.MoveTo(new_name);
            }
            catch (Exception ex)
            {
                unsucsess.children.Add(new OperationTree(fileInfo, false, null, OperationType.Destroy, null, ex.Message));
                return;
            }
            int dones = 0;
            if (method == DestroyMethod.FastOne || method == DestroyMethod.FastZero || method == DestroyMethod.FastRandom)
            {
                for (int i = 0; i < repeat; i++)
                {
                    while (pause_copy)
                    {
                        Thread.Sleep(50);
                        if (CloseWindow)
                            return;
                    }
                    if (WriteFile(fileInfo, GetBuffer(method, 0), fileInfo.Length) == 1)
                    {
                        dones += 35;
                        if (CloseWindow) return;
                    }
                }
            }
            else if (method == DestroyMethod.Secure)
            {
                for (int i = 0; i < repeat; i++)
                    for (int j = 0; j < 35; j++)
                    {
                        dones += WriteFile(fileInfo, GetBuffer(method, j), fileInfo.Length);
                        if (CloseWindow) return;
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                    }
            }
            try
            {
                if (dones > 30 * repeat)
                {
                    if (delete)
                        fileInfo.Delete();
                }
                else
                {
                    old_progress_value = progress_value = progress_value - (method == DestroyMethod.Secure ? dones * fileInfo.Length : Convert.ToInt64((dones / 35.0) * fileInfo.Length));
                    undeleted.children.Add(new OperationTree(fileInfo, false, null, OperationType.Destroy, null, "Some operations have failed."));
                }
                    completed_count++;
            }
            catch (Exception ex)
            { undeleted.children.Add(new OperationTree(fileInfo, false, null, OperationType.Destroy, null, ex.Message)); }
        }

        byte[] GetBuffer(DestroyMethod method, int case_number)
        {
            byte[] buffer = new byte[Functions.buffer];
            if (method == DestroyMethod.Secure)
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
                        for (int k = 0; k < Functions.buffer; k++)
                            buffer[k] = 0x55;
                        break;
                    case 5:
                        for (int k = 0; k < Functions.buffer; k++)
                            buffer[k] = 0xAA;
                        break;
                    case 6:
                    case 25:
                        for (int k = 2; k < Functions.buffer; k += 3)
                        { buffer[k] = 0x92; buffer[k - 1] = 0x49; buffer[k - 2] = 0x24; }
                        break;
                    case 7:
                    case 26:
                        for (int k = 2; k < Functions.buffer; k += 3)
                        { buffer[k] = 0x49; buffer[k - 1] = 0x24; buffer[k - 2] = 0x92; }
                        break;
                    case 8:
                    case 27:
                        for (int k = 2; k < Functions.buffer; k += 3)
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
                        for (int k = 0; k < Functions.buffer; k++)
                            buffer[k] = (byte)(0x11 * (k - 9));
                        break;
                    case 28:
                        for (int k = 2; k < Functions.buffer; k += 3)
                        { buffer[k] = 0x6D; buffer[k - 1] = 0xB6; buffer[k - 2] = 0xDB; }
                        break;
                    case 29:
                        for (int k = 2; k < Functions.buffer; k += 3)
                        { buffer[k] = 0xB6; buffer[k - 1] = 0xDB; buffer[k - 2] = 0x6D; }
                        break;
                    case 30:
                        for (int k = 2; k < Functions.buffer; k += 3)
                        { buffer[k] = 0xDB; buffer[k - 1] = 0x6d; buffer[k - 2] = 0xB6; }
                        break;
                }
            }
            else if (method == DestroyMethod.FastRandom)
            {
                Random rand = new Random((int)(DateTime.Now.Ticks / 34553786873));
                rand.NextBytes(buffer);
            }
            else
            {
                byte values;
                switch (method)
                {
                    case DestroyMethod.FastOne:
                        values = 0xFF;
                        break;
                    default:
                        values = 0x00;
                        break;
                }
                for (int j = 0; j < Functions.buffer; j++)
                    buffer[j] = values;
            }

            return buffer;
        }

        byte WriteFile(FileInfo file, byte[] buffer, long size)
        {
            if (CloseWindow) return 0;
            long k = 0;
            try
            {
                BinaryWriter BW = new BinaryWriter(file.OpenWrite());
                for (k = buffer.Length; k <= size; k += buffer.Length)
                {
                    while (pause_copy)
                    {
                        Thread.Sleep(50);
                        if (CloseWindow)
                        {
                            BW.Close();
                            return 0;
                        }
                    }
                    if (CloseWindow)
                    {
                        BW.Close();
                        return 0;
                    }
                    BW.Write(buffer);
                    progress_value += buffer.Length;
                }
                long remain = (size - (k - buffer.Length));
                if (CloseWindow)
                {
                    BW.Close();
                    return 0;
                }
                BW.Write(buffer, 0, (int)remain);
                progress_value += remain;
                BW.Close();
                return 1;
            }
            catch (Exception)
            {
                progress_value -= k;
                return 0;
            }
        }

        string GetFileName(string parent)
        {
            string drive_name = Path.GetPathRoot(parent).ToLower();
            if (!drives_format.ContainsKey(drive_name))
                drives_format.Add(drive_name, new DriveInfo(drive_name).DriveFormat.ToLower());

            StringBuilder name = new StringBuilder(string.Empty);
            while (true)
            {
                name.Append(parent);
                name.Append("\\$");
                Random rand = new Random((int)DateTime.Now.Ticks);
                int max_file_name = 8;
                switch (drives_format[drive_name])
                {
                    case "ntfs":
                    case "fat16":
                    case "fat32":
                    case "exfat":
                        if (parent.Length > 15)
                            max_file_name = 253 - parent.Length;
                        else
                            max_file_name = 238;
                        break;
                    case "fat":
                    case "fat12":
                        if (parent.Length > 15)
                            max_file_name = 248 - parent.Length;
                        else
                            max_file_name = 238;
                        break;
                }
                for (int i = 0; i < max_file_name - 1; i++)
                    name.Append(Chars[rand.Next(Chars.Length - 1)]);
                name.Append(".");
                for (int i = 0; i < 3; i++)
                    name.Append(Chars[rand.Next(Chars.Length - 1)]);
                if (!File.Exists(name.ToString()) && !Directory.Exists(name.ToString()))
                    break;
            }
            return name.ToString();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            remain.Foreground = ProgressBrush;
            operation.Foreground = ProgressBrush;
            if (pause_copy)
                progress_bar.Foreground = Brushes.Gold;
            else if (TaskbarItemInfo.ProgressState == System.Windows.Shell.TaskbarItemProgressState.Error)
                progress_bar.Foreground = Brushes.Red;
            else
                progress_bar.Foreground = ProgressBrush;
            completed.Foreground = more_than_one.Foreground = complete_item_run.Foreground = ProgressBrush;
            speed.Foreground = ProgressBrush;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            remain.Foreground = Brushes.Gray;
            operation.Foreground = Brushes.Gray;
            progress_bar.Foreground = Brushes.Gray;
            completed.Foreground = more_than_one.Foreground = complete_item_run.Foreground = Brushes.Gray;
            speed.Foreground = Brushes.Gray;
        }

        private void PrepareAndWriteFile(DriveInfo drive, List<string> files_name, long size, DestroyMethod method, BackgroundWorker bw, ref int file_creating_error)
        {
            FileInfo fi = new FileInfo(GetFileName(drive.Name));
            if (method == DestroyMethod.Secure)
            {
                int count = 0;
                for (int k = 0; k < 35; k++)
                    count += WriteFile(fi, GetBuffer(method, k), size);
                if (count > 30)
                    files_name.Add(fi.FullName);
                else
                { bw.ReportProgress(-100, "Something wronge while creating file \"" + fi.FullName + "\""); file_creating_error++; }
            }
            else
            {
                if (WriteFile(fi, GetBuffer(method, 0), size) == 1)
                    files_name.Add(fi.FullName);
                else
                { bw.ReportProgress(-100, "Something wronge while creating file \"" + fi.FullName + "\""); file_creating_error++; }
            }
        }

        #endregion

        #region Encrypt Files

        byte[] file_imprint = { 0x46, 0x4D, 0x45, 0x3A, 0x20, 0x65, 0x46, 0x69, 0x6C, 0x20, 0x4D, 0x61, 0x6E, 0x61, 0x67, 0x65, 0x72, 0x20, 0x45, 0x6E, 0x63, 0x72, 0x79, 0x70, 0x74, 0x65, 0x64, 0x20, 0x46, 0x69, 0x6C, 0x65, 0x2E, 0x42, 0x79, 0x20, 0x44, 0x72, 0x2E, 0x4D, 0x6F, 0x68, 0x61, 0x6D, 0x6D, 0x61, 0x64 };//FME: File Manager Encrypted File.By Dr.Mohammad

        internal void StartEncrept(List<FileInfo> files_list, byte[] EncPassword, bool delete_files, bool overwrite_files, string save_in, bool encrypt_files_name, bool is_package, bool show_dialog)
        {
            operation_type = OperationType.Encrypt;
            //already_exist = new OperationTree(null, false, "", operation_type, null, false, "this file already exist.");
            unsucsess = new OperationTree(null, false, "", operation_type, null, false, "can not encrypt file.");
            Title = string.Format("Encrypting {0} {1}", files_list.Count, files_list.Count > 1 ? "items" : "item");
            byte[] general_encrypter = Files_Encrepter.PreparePassword(Files_Encrepter.general_password, Files_Encrepter.general_secret_number, 128);
            double max_value = 0;
            foreach (FileInfo i in files_list)
            {
                max_value += i.Length;
                while (pause_copy)
                {
                    Thread.Sleep(50);
                    if (CloseWindow)
                        return;
                }
            }
            progress_bar.Maximum = max_value;
            BackgroundWorker encrypt_files_BackgroundWorker = new BackgroundWorker();
            encrypt_files_BackgroundWorker.WorkerReportsProgress = true;

            encrypt_files_BackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                BackgroundWorker bw = (BackgroundWorker)sender;
                Thread process_thread = new Thread(() =>
                {
                    while (bw.IsBusy)
                    {
                        try { bw.ReportProgress(1); }
                        catch (Exception) { }
                        Thread.Sleep(100);
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                    }
                });
                process_thread.Start();

                if (is_package)
                {
                    byte[] user_secret_number = Files_Encrepter.GetSecretNumber(128);
                    EncryptFileHeader header = new EncryptFileHeader(is_package, encrypt_files_name, 3, Files_Encrepter.EncryptBytes(user_secret_number, general_encrypter), Files_Encrepter.HashBytes(EncPassword, 128), files_list.Count);
                    byte[] user_encrypter = Files_Encrepter.PreparePassword(EncPassword, user_secret_number, 128);
                    string zeroz = "0000000000";
                    int num = 1;
                    string file_name = "file" + zeroz.Substring(0, 10 - num.ToString().Length) + num.ToString() + ".enp";
                    if (save_in[save_in.Length - 1] != '\\') save_in += '\\';
                    while (File.Exists(save_in + file_name))
                    {
                        num++;
                        file_name = "file" + zeroz.Substring(0, 10 - num.ToString().Length) + num.ToString() + ".enp";
                    }
                    BinaryWriter BW = new BinaryWriter(File.OpenWrite(save_in + file_name));
                    int id_counter = 0;
                    foreach (var file in files_list)
                    {
                        id_counter++;
                        header.indices.Add(new EncryptFileHeaderIndex(id_counter, file, encrypt_files_name ? user_encrypter: general_encrypter));
                    }
                    BW.BaseStream.Position = 0;
                    for (int i = 0; i < files_list.Count; i++)
                    {
                        if (CloseWindow) return;
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                        EncryptFile(files_list[i], user_encrypter, BW, i == 0 ? header.GetFirstFileIndex() : 0, header.indices[i]);
                        byte[] buf = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x06, 0x05, 0x07 };
                        BW.Write(buf);
                    }
                    header.SetFirstFileInfo(header.indices[0].start_index, header.indices[0].length);
                    WriteHeader(BW, header);
                    BW.Close();
                }

                else
                {
                    foreach (var i in files_list)
                    {
                        byte[] user_secret_number = Files_Encrepter.GetSecretNumber(128);
                        EncryptFileHeader header = new EncryptFileHeader(is_package, encrypt_files_name, 3, Files_Encrepter.EncryptBytes(user_secret_number, general_encrypter), Files_Encrepter.HashBytes(EncPassword, 128), 1);
                        byte[] user_encrypter = Files_Encrepter.PreparePassword(EncPassword, user_secret_number, 128);
                        string zeroz = "0000000000";
                        int num = 1;
                        string file_name = "file" + zeroz.Substring(0, 10 - num.ToString().Length) + num.ToString() + ".enf";
                        if (save_in[save_in.Length - 1] != '\\') save_in += '\\';
                        while (File.Exists(save_in + file_name))
                        {
                            num++;
                            file_name = "file" + zeroz.Substring(0, 10 - num.ToString().Length) + num.ToString() + ".enf";
                        }
                        BinaryWriter BW = new BinaryWriter(File.OpenWrite(save_in + file_name));
                        header.indices.Add(new EncryptFileHeaderIndex(1, i, encrypt_files_name ? user_encrypter : general_encrypter));
                        EncryptFile(i, user_encrypter, BW, header.GetFirstFileIndex(), header.indices[0]);
                        if (CloseWindow) return;
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                        WriteHeader(BW, header);
                        BW.Close();
                    }
                }
                
                //for (int index = 0; index < files_list.Count; index++)
                //{
                //    if (CloseWindow) return;
                //    while (pause_copy)
                //    {
                //        Thread.Sleep(50);
                //        if (CloseWindow)
                //            return;
                //    }
                //    EncryptFile(files_list[index], EncPassword, save_in);
                //}

                //while (unsucsess.children.Count > 0)
                //{

                //    sleep_thread = true;
                //    encrypt_files_BackgroundWorker.ReportProgress(-1);
                //    while (sleep_thread)
                //        Thread.Sleep(50);
                //    OperationTree temp = new OperationTree(null, false, "", operation_type, null, false, "");
                //    double rem = max_value;
                //    foreach (var i in unsucsess.children)
                //    {
                //        temp.children.Add(i);
                //        //rem -= i.size;
                //    }
                //    //old_progress_value = progress_value = Convert.ToInt64(rem);
                //    unsucsess.children.Clear();
                //    foreach (var i in temp.children)
                //    {
                //        while (pause_copy)
                //        {
                //            Thread.Sleep(50);
                //            if (CloseWindow)
                //                return;
                //        }
                //        if (i.user_option == OperationOption.Skip)
                //        {
                //            i.Done = true;
                //            progress_value += i.size;
                //        }
                //        else
                //        {
                //            if (CloseWindow) return;
                //            EncryptFile((FileInfo)i.item, EncPassword, save_in);
                //        }
                //    }
                //}
            };

            encrypt_files_BackgroundWorker.ProgressChanged += Destroy_ProgressChanged;

            encrypt_files_BackgroundWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (delete_files)
                    new File_Destroyer(files_list, true).Show();
                Close();
            };
            progress_bar.IsIndeterminate = false;
            encrypt_files_BackgroundWorker.RunWorkerAsync();
            if (show_dialog)
                ShowDialog();
            else
                Show();
        }

        void WriteHeader(BinaryWriter bw, EncryptFileHeader header)
        {
            bw.BaseStream.Position = 0;
            //8 byte then "FILE MANAGER encrypted file, version: "
            if (header.is_package) bw.Write((byte)205); else bw.Write((byte)204);
            if (header.encrypt_files_name) bw.Write((byte)140); else bw.Write((byte)139);
            byte[] fileb = { 16, 191, 91, 144, 31, 2, 70, 73, 76, 69, 32, 77, 65, 78, 65, 71, 69, 82, 32, 101, 110, 99, 114, 121, 112, 116, 101, 100, 32, 102, 105, 108, 101, 44, 32, 118, 101, 114, 115, 105, 111, 110, 58, 32 };
            bw.Write(fileb);
            bw.Write(header.version);
            bw.Write(header.secret_number);
            bw.Write(header.password_hash);
            bw.Write(header.GetFirstFileIndex());
            bw.Write(header.first_file_length);
            bw.Write(header.indices.Count);
            foreach(var i in header.indices)
            {
                bw.Write(i.id);
                bw.Write(i.file_name_length);
                bw.Write(i.fileName);
                bw.Write(i.start_index);
                bw.Write(i.length);
                bw.Write(i.crc);
            }
        }

        private byte EncryptFile(FileInfo file, byte[] encryption_code, BinaryWriter bw, long start_index, EncryptFileHeaderIndex index)
        {
            BinaryReader br = null;
            try
            {
                bw.BaseStream.Position += start_index;
                long position = bw.BaseStream.Position;
                br = new BinaryReader(file.OpenRead());
                long k;
                for (k = Functions.buffer; k <= file.Length; k += Functions.buffer)
                {
                    while (pause_copy)
                    {
                        Thread.Sleep(50);
                        if (CloseWindow)
                        {
                            bw.Close();
                            return 0;
                        }
                    }
                    if (CloseWindow)
                    {
                        bw.Close();
                        return 0;
                    }
                    bw.Write(Files_Encrepter.EncryptBytes(br.ReadBytes(Functions.buffer), encryption_code));
                    progress_value += Functions.buffer;
                }
                long remain = (file.Length - (k - Functions.buffer));
                if (CloseWindow)
                {
                    bw.Close();
                    return 0;
                }
                bw.Write(Files_Encrepter.EncryptBytes(br.ReadBytes((int)remain), encryption_code));
                progress_value += remain;
                br.Close();
                index.start_index = position;
                index.length = file.Length;
                return 1;
            }
            catch (Exception ex)
            {
                try { if (br != null) br.Close(); }
                catch (Exception) { }
                unsucsess.children.Add(new OperationTree(file, false, "", OperationType.Encrypt, root, ex.Message));
                return 0;
            }
        }

        private byte[] GetRandomNumber(SHA256Cng sha1, MD5 md5)
        {
            Random ran = new Random((int)(DateTime.Now.Ticks / 34553786873));
            byte[] num = new byte[512];
            ran.NextBytes(num);
            for (int i = 0; i < num.Length; i++)
                num[i] = (byte)(num[i] / 2 + 5);
            num = sha1.ComputeHash(num);
            for (int i = 0; i < num.Length; i++)
                num[i] = (byte)~num[i];
            num = md5.ComputeHash(num);
            return num;
        }

        internal void StartDecrept(List<FileInfo> files_list, byte[] DecPassword, string save_in, bool show_dialog)
        {
            operation_type = OperationType.Decrypt;
            unsucsess = new OperationTree(null, false, "", operation_type, null, false, "can not decrypt file.");
            Title = string.Format("Decrypting {0} {1}", files_list.Count, files_list.Count > 1 ? "items" : "item");
            byte[] general_decrypter = Files_Encrepter.PreparePassword(Files_Encrepter.general_password, Files_Encrepter.general_secret_number, 128);
            double max_value = 0;
            foreach (FileInfo i in files_list)
            {
                max_value += i.Length;
                while (pause_copy)
                {
                    Thread.Sleep(50);
                    if (CloseWindow)
                        return;
                }
            }
            progress_bar.Maximum = max_value;
            BackgroundWorker decrypt_files_BackgroundWorker = new BackgroundWorker();
            decrypt_files_BackgroundWorker.WorkerReportsProgress = true;

            decrypt_files_BackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                BackgroundWorker bw = (BackgroundWorker)sender;
                Thread process_thread = new Thread(() =>
                {
                    while (bw.IsBusy)
                    {
                        try { bw.ReportProgress(1); }
                        catch (Exception ex) { }
                        Thread.Sleep(100);
                        while (pause_copy)
                        {
                            Thread.Sleep(50);
                            if (CloseWindow)
                                return;
                        }
                    }
                });
                process_thread.Start();

                foreach (var file in files_list)
                {
                    BinaryReader BR = new BinaryReader(file.OpenRead());
                    EncryptFileHeader header;
                    try
                    {
                        switch (ReadHeader(BR, general_decrypter, DecPassword, out header))
                        {
                            case 0:
                                if (save_in[save_in.Length - 1] != '\\')
                                    save_in += '\\';
                                foreach (var catalog in header.indices)
                                {
                                    if (CloseWindow) return;
                                    while (pause_copy)
                                    {
                                        Thread.Sleep(50);
                                        if (CloseWindow)
                                            return;
                                    }
                                    DecryptFile(new FileInfo(save_in + catalog.file_name), Files_Encrepter.PreparePassword(DecPassword, header.secret_number, 128), BR, catalog);
                                    if (header.is_package)
                                        BR.BaseStream.Position += 8;
                                }
                                break;
                            case 1:
                                break;
                            case -1:
                                break;
                        }
                    }
                    catch (Exception) { }
                }
            };

            decrypt_files_BackgroundWorker.ProgressChanged += Destroy_ProgressChanged;

            decrypt_files_BackgroundWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) => { Close(); };
            progress_bar.IsIndeterminate = false;
            decrypt_files_BackgroundWorker.RunWorkerAsync();
            if (show_dialog)
                ShowDialog();
            else
                Show();
        }

        private byte DecryptFile(FileInfo output_file, byte[] decryption_code, BinaryReader br, EncryptFileHeaderIndex catalog)
        {
            BinaryWriter bw = null;
            try
            {
                br.BaseStream.Position = catalog.start_index;
                bw = new BinaryWriter(output_file.OpenWrite());
                long k;
                for (k = Functions.buffer; k <= catalog.length; k += Functions.buffer)
                {
                    while (pause_copy)
                    {
                        Thread.Sleep(50);
                        if (CloseWindow)
                        {
                            bw.Close();
                            return 0;
                        }
                    }
                    if (CloseWindow)
                    {
                        bw.Close();
                        return 0;
                    }
                    bw.Write(Files_Encrepter.DecryptBytes(br.ReadBytes(Functions.buffer), decryption_code));
                    progress_value += Functions.buffer;
                }
                long remain = (catalog.length - (k - Functions.buffer));
                if (CloseWindow)
                {
                    bw.Close();
                    return 0;
                }
                bw.Write(Files_Encrepter.DecryptBytes(br.ReadBytes((int)remain), decryption_code));
                progress_value += remain;
                bw.Close();
                return 1;
            }
            catch (Exception ex)
            {
                try { if (br != null) br.Close(); }
                catch (Exception) { }
                ////unsucsess.children.Add(new OperationTree(catalog, false, "", OperationType.Encrypt, root, ex.Message));
                return 0;
            }
        }

        private int ReadHeader(BinaryReader BR, byte[] general_code, byte[] user_password, out EncryptFileHeader header)
        {
            //1 is incurrect file 
            //0 everything OK
            //-1 invalid Password
            bool is_package, encrypt_files_name;
            byte b0 = BR.ReadByte();
            if (b0 == 204) is_package = false;
            else if (b0 == 205) is_package = true;
            else
            {
                header = null;
                return 1;
            }
            byte b1 = BR.ReadByte();
            if (b1 == 139) encrypt_files_name = false;
            else if (b1 == 140) encrypt_files_name = true;
            else
            {
                header = null;
                return 1;
            }
            byte[] orginal = { 16, 191, 91, 144, 31, 2, 70, 73, 76, 69, 32, 77, 65, 78, 65, 71, 69, 82, 32, 101, 110, 99, 114, 121, 112, 116, 101, 100, 32, 102, 105, 108, 101, 44, 32, 118, 101, 114, 115, 105, 111, 110, 58, 32 };
            byte[] file_bytes = BR.ReadBytes(44);
            for (int i = 0; i < file_bytes.Length; i++)
                if (orginal[i] != file_bytes[i])
                {
                    header = null;
                    return 1;
                }
            var ver = BR.ReadBytes(4);
            float version = BitConverter.ToSingle(ver, 0);
            var secret_number = Files_Encrepter.DecryptBytes(BR.ReadBytes(128), general_code);
            var file_password = BR.ReadBytes(128);
            if (ComparePasswoeds(user_password, file_password))
            {
                var b2 = BR.ReadBytes(8);
                long first_file_index = BitConverter.ToInt64(b2, 0);
                var b3 = BR.ReadBytes(8);
                long first_file_length = BitConverter.ToInt64(b3, 0);
                var b4 = BR.ReadBytes(4);
                int files_count = BitConverter.ToInt32(b4, 0);
                header = new EncryptFileHeader(is_package, encrypt_files_name, version, secret_number, user_password, files_count);
                header.SetFirstFileInfo(first_file_index, first_file_length);
                byte[] decrypter = encrypt_files_name ? Files_Encrepter.PreparePassword(user_password, secret_number, 128) : general_code;
                for (int i = 0; i < files_count; i++)
                {
                    var id = BR.ReadBytes(4);
                    byte file_name_length = BR.ReadByte();
                    var file_name = BR.ReadBytes(file_name_length * 2);
                    var start_index = BR.ReadBytes(8);
                    var length = BR.ReadBytes(8);
                    var crc = BR.ReadBytes(8);
                    header.indices.Add(new EncryptFileHeaderIndex(id, file_name, start_index, length, crc, decrypter));
                }
                return 0;
            }
            else
            {
                header = null;
                return -1;
            }
        }

        private bool ComparePasswoeds(byte[] user_password, byte[] password_in_file)
        {
            user_password = Files_Encrepter.HashBytes(user_password, 128);
            for (int i = 0; i < 128; i++)
                if (user_password[i] != password_in_file[i])
                    return false;
            return true;
        }

        #endregion Encrypt Files

        #region ConvertToIso

        internal void ConvertToIso(FileInfo File, string save_in, bool show_dialog)
        {
            OperationType = OperationType.ConvertISZ_ToISO;
            paste_in = save_in;
            operation.Text = Title = "Converting to ISO";
            pause_button.IsEnabled = false;
            operation.Margin = new Thickness(5, 4, 0, 0);
            name_textblock.Margin = new Thickness(10, 30, 10, 0);
            progress_bar.Margin = new Thickness(10, 54, 50, 0);
            Height = 120;
            speed_textblock.Visibility = complete_textblock.Visibility = remain.Visibility = remain_unit.Visibility = remain_title.Visibility = Visibility.Collapsed;
            name_textblock.Text = File.FullName;
            progress_bar.IsIndeterminate = false;
            ConvertToISO(File, "");
            if (show_dialog)
                ShowDialog();
            else
                Show();
        }

        private void ConvertToISO(FileInfo file, string password)
        {
            bool first_output = true, password_nedded = false;
            BackgroundWorker convert_to_iso = new BackgroundWorker();
            progress_bar.Foreground = Brushes.DarkCyan;
            convert_to_iso.WorkerReportsProgress = true;
            Process pr = new Process();
            convert_to_iso.DoWork += (object sender2, DoWorkEventArgs e2) =>
            {
                Thread progress = new Thread(() =>
                {
                    while (convert_to_iso.IsBusy)
                    {
                        if (CloseWindow)
                        {
                            try
                            {
                                pr.Kill();
                            }
                            catch (Exception) { }
                            break;
                        }
                        Thread.Sleep(200);
                        if (convert_to_iso.IsBusy)
                        {
                            FileInfo output = new FileInfo(file.FullName.Remove(file.FullName.Length - 1) + "o");
                            if (output.Exists)
                                try
                                { convert_to_iso.ReportProgress(Convert.ToInt32((output.Length / file.Length) * 100)); }
                                catch (Exception) { }
                        };
                    }
                });
                progress.Start();

                ProcessStartInfo psi = new ProcessStartInfo(Environment.CurrentDirectory + "\\Tools\\compress\\iso\\unisz.exe", " \"" + file.FullName + "\" " + password);
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                pr.StartInfo = psi;

                pr.OutputDataReceived += (object sender3, DataReceivedEventArgs e3) =>
                {
                    string error = e3.Data;
                    if (error != null)
                        error = error.ToLower();
                    switch (error)
                    {
                        case null:
                            convert_to_iso.ReportProgress(-1);
                            break;
                        case "password is needed.":
                            password_nedded = true;
                            convert_to_iso.ReportProgress(-2);
                            break;
                        case "file open error.":
                            convert_to_iso.ReportProgress(-3);
                            break;
                        case "error filename.":
                        case "not an .isz file.":
                            convert_to_iso.ReportProgress(-4);
                            break;
                        case "file create error":
                            convert_to_iso.ReportProgress(-5);
                            break;
                        case "read error.error uncompressing isz file.":
                            convert_to_iso.ReportProgress(-6);
                            break;
                    }
                };

                try
                {
                    pr.Start();
                    pr.BeginOutputReadLine();
                    pr.WaitForExit();
                }
                catch (Exception ex)
                { convert_to_iso.ReportProgress(-7, ex.Message); }
            };

            convert_to_iso.ProgressChanged += (object sender2, ProgressChangedEventArgs e2) =>
            {
                if (e2.ProgressPercentage < -1)
                {
                    progress_bar.Foreground = Brushes.Tomato;
                    first_output = false;
                }
                switch (e2.ProgressPercentage)
                {
                    case -1:
                        if (first_output)
                            Messages.ShowMessage("Converting operation done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "Converting has completed.\noutput file saved to same source's directory.");
                        break;
                    case -2:
                        if (Messages.ShowMessageWithPassword("Password is needed.", ModrenMessageButton.OKCancel, ModrenMessageImage.Password, ModrenMessageSound.Exclamation, true, "Please enter password for encrepted file.","",out password) == MyMessageBoxResult.OK)
                        {
                            if (!pr.HasExited)
                                try
                                { pr.Kill(); }
                                catch (Exception) { }
                            ConvertToISO(file, password);
                        }
                        break;
                    case -3:
                        Messages.ShowMessage("The file is damaged or unsuported.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
                        break;
                    case -4:
                        Messages.ShowMessage("The file is not an .ISZ file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
                        break;
                    case -5:
                        Messages.ShowMessage("We can't create output file.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "if the '.isz' fle is in write protected media copy it to other location.");
                        break;
                    case -6:
                        Messages.ShowMessage("Wrong password or the file is damaged.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
                        break;
                    case -7:
                        Messages.ShowMessage("We found error while converting the file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, e2.UserState.ToString());
                        break;
                    default:
                        if (e2.ProgressPercentage > 90)
                            progress_bar.Value = 90;
                        else
                            progress_bar.Value = e2.ProgressPercentage;
                        percent.Text = progress_bar.Value + "%";
                        TaskbarItemInfo.ProgressValue = progress_bar.Value / 100;
                        break;
                }
            };

            convert_to_iso.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
            {
                if (password_nedded)
                    password_nedded = false;
                else
                {
                    progress_bar.Value = 100;
                    Close();
                }
            };
            convert_to_iso.RunWorkerAsync();
        }


        #endregion

        #region Controls

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (pause_copy)
                Start();
            else
                Pause();
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

        private void Start()
        {
            if (TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error && pause_copy)
            {
                play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/copy-pause.png"));
                pause_copy = false;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                progress_bar.Foreground = progress_brush;
            }
        }

        private void Stop()
        {
            if (operation_type == OperationType.Copy)
            {
                try
                {
                    if (BW != null)
                        BW.Close();
                }
                catch (Exception) { }
                try
                {
                    if (BR != null)
                        BR.Close();
                }
                catch (Exception) { }
            }
            try
            {
                if (OpBW != null && OpBW.IsBusy)
                    OpBW.CancelAsync();
            }
            catch (Exception) { }
            CloseWindow = true;
            Close();
        }

        private void Pause()
        {
            if (TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error && !pause_copy)
            {
                pause_copy = true;
                play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/copy-start.png"));
                progress_bar.Foreground = Brushes.Gold;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Paused;
            }
        }

        #endregion

    }

    public class OperationTree
    {
        public OperationTree parent;
        public List<OperationTree> children;
        public FileSystemInfo item;
        public long completed;
        public long size, files_count, directory_count;
        public  OperationOption user_option;
        public string  error_message;
        public object short_path;
        public OperationType operation;
        public bool is_cut;

        bool done;
        public Boolean Done
        {
            get { return done; }
            set
            {
                if (value && parent != null)
                    parent.completed++;
                done = value;
            }
        }

        public OperationTree(FileInfo file, bool is_cut, object short_path, OperationType operation, OperationTree parent, string error)
        {
            this.parent = parent;
            //if (parent != null)
            //    parent.size += file.Length;
            item = file;
            completed = 0;
            done = false;
            if (file != null)
                if (file.Exists)
                    size = file.Length;
            this.operation = operation;
            this.short_path = short_path;
            this.is_cut = is_cut;
            files_count = 1;
            directory_count = 0;
            user_option = OperationOption.NoValue;
            error_message = error;
        }

        public OperationTree(DirectoryInfo directory, bool is_cut, object short_path, OperationType operation, OperationTree parent, bool get_files, string error)
        {
            this.operation = operation;
            this.parent = parent;
            item = directory;
            completed = files_count = 0;
            done = false;
            this.short_path = short_path;
            this.is_cut = is_cut;
            user_option = OperationOption.NoValue;
            children = new List<OperationTree>();
            directory_count = 1;
            if (get_files)
                GetSuns(directory, this);
            foreach (var i in children)
            {
                files_count += i.files_count;
                directory_count += i.directory_count;
            }
            error_message = error;
        }

        void GetSuns(DirectoryInfo directory, OperationTree operationTree)
        {
            try
            {
                var dirs = directory.GetDirectories();
                foreach (var i in dirs)
                {
                    OperationTree sun = new OperationTree(i, is_cut, short_path + "\\" + i.Name, operation, operationTree, true, "");
                    children.Add(sun);
                    size += sun.size;
                }
            }
            catch (Exception) { }
            try
            {
                var files = directory.GetFiles();
                foreach (var i in files)
                    try
                    { children.Add(new OperationTree(i, is_cut, short_path + "\\" + i.Name, operation, operationTree, "")); size += i.Length; }
                    catch (Exception) { }
            }
            catch (Exception) { }
        }

        internal bool Contains(OperationTree operationTree, out int index)
        {
            for (int i = 0; i < children.Count;i++ )
                if (children[i].item == operationTree.item)
                {
                    index = i;
                    return true;
                }
            index=-1;
            return false;
        }
    }

    public enum OperationOption
    {
        NoValue = 0,
        Over = 1,
        Skip = 2,
        CopyWithNewName = 3,
        Retry = 4
    }

    public enum OperationType
    {
        Copy = 0,
        Attributes = 1,
        Delete = 2,
        Rename = 3,
        Destroy = 4,
        DiskDestroy = 5,
        Encrypt = 6,
        Decrypt = 7,
        Compress = 8,
        Extract = 9,
        CreateShortcut = 10,
        ConvertISZ_ToISO = 11,
        ChangeFileDate = 12
    }
}