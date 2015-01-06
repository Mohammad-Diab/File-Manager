using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        Op operation_type;
        OperationTree root, already_exist, unsucsess;
        long progress_value, completed_count, old_progress_value;
        int progress_rount = 1;
        BinaryReader BR;
        BinaryWriter BW;
        bool sleep_thread = false, pause_copy = false;
        BackgroundWorker OpBW;

        public Operation()
        {
            InitializeComponent();
        }

        internal void CopyDialogShow(List<FileOperationItem> ToCopyList, Op operation_type, string path_to_paste, bool show_dialog)
        {
            this.operation_type = operation_type;
            root = new OperationTree(null, false, "", operation_type, null, false, "");
            paste_in = path_to_paste;
            operation.Text = Title = operation_type == Op.Copy ? "Copying" : operation_type == Op.Delete ? "Deleting" : operation_type == Op.Rename ? "Renaming" : operation_type == Op.Attributes ? "Applaying Attributes" : operation_type == Op.Extract ? "Extracting Files" : "";
            BackgroundWorker CollectFiles = new BackgroundWorker();
            CollectFiles.DoWork += CollectFiles_DoWork;
            if (operation_type == Op.Copy)
                CollectFiles.RunWorkerCompleted += CollectFiles_RunWorkerCompleted;
            else if (operation_type == Op.Delete)
                CollectFiles.RunWorkerCompleted += CollectFilesToDelete_RunWorkerCompleted;
            else if (operation_type == Op.Rename)
                CollectFiles.RunWorkerCompleted += CollectFilesToRename_RunWorkerCompleted;
            else if (operation_type == Op.Attributes)
                CollectFiles.RunWorkerCompleted += CollectFilesToAttribute_RunWorkerCompleted;
            //else if (operation_type == Op.Extract)
            //    CollectFiles.RunWorkerCompleted += CollectFilesToExtract_RunWorkerCompleted;
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
                    root.children.Add(new OperationTree((FileInfo)i.Item, i.IsCut, i.short_path, operation_type, root, ""));
                else
                    root.children.Add(new OperationTree((DirectoryInfo)i.Item, i.IsCut, i.short_path, operation_type, root, operation_type == Op.Copy || operation_type == Op.Delete, ""));
        }

        void CompleteCollectFiles(long maximum_progress_bar_value, string title)
        {
            progress_bar.IsIndeterminate = false;
            progress_bar.Foreground = Brushes.RoyalBlue;
            progress_bar.Value = 0;
            progress_bar.Maximum = maximum_progress_bar_value;
            ChangeRemain(maximum_progress_bar_value, !(operation_type == Op.Copy || operation_type == Op.Encrypt));
            Title = Title + " (" + title + ")";
            OpBW = new BackgroundWorker();
            OpBW.WorkerReportsProgress = true;
            if (operation_type == Op.Copy)
                OpBW.DoWork += Copying_DoWork;
            else if (operation_type == Op.Delete)
                OpBW.DoWork += Deleting_DoWork;
            else if (operation_type == Op.Rename)
                OpBW.DoWork += Renaming_DoWork;
            else if (operation_type == Op.Attributes)
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
                    case Op.Copy:
                        val = progress_bar.Value * 100 / (root.size + 0.000000001);
                        ChangeRemain(root.size - progress_value, false);
                        break;
                    case Op.Delete:
                    case Op.Attributes:
                        val = progress_bar.Value * 100.0 / (root.files_count + root.directory_count + 0.000000001);
                        ChangeRemain(root.files_count + root.directory_count - progress_value, true);
                        break;
                    case Op.Rename:
                        val = progress_bar.Value * 100.0 / (root.children.Count + 0.000000001);
                        ChangeRemain(root.children.Count - progress_value, true);
                        break;
                    case Op.Extract:
                        val = progress_value;
                        ChangeRemain((root.size * (100 - progress_value)) / 100, false);
                        break;
                }
                percent.Text = Math.Round(val, 0) + "%";
                TaskbarItemInfo.ProgressValue = val / 100;
                completed.Text = completed_count.ToString();
                if (progress_rount % 20 == 0)
                {
                    progress_rount = 1;
                    if (operation_type == Op.Copy)
                        speed.Text = Functions.FileSize((progress_value - old_progress_value) / 2, 1) + " per second";
                    else if (operation_type == Op.Delete)
                    {
                        double x = Math.Round(progress_value - old_progress_value / 2.0, 1);
                        speed.Text = (x > 2 ? x : 60 * x) + " item" + (x > 2 ? "s" : x * 60 > 2 ? "s" : "") + " per " + (x > 2 ? "second" : "minute");
                    }
                    else if (operation_type == Op.Extract)
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
            if (operation_type == Op.Copy)
                MainWindow.IsPaste = true;
            Close();
        }

        void ChangeRemain(long size, bool item)
        {
            string[] str;
            if (!item)
            {
                char[] spliter = { ' ' };
                str = Functions.FileSize(size, 0).Split(spliter, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                str = new string[2];
                str[0] = size.ToString();
                str[1] = size > 1 ? "Items" : "Item";
            }
            remain.Text = str[0];
            remain_unit.Text = str[1];
            remain_title.Width = str[0].Length > 2 ? ((str[0].Length - 3) * 26 + 78) : 65;
            remain_unit.Margin = new Thickness(40 + (str[0].Length - 1) * 26, 47, 0, 0);
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
                    OpBW.ReportProgress(0);
                    while (sleep_thread)
                        Thread.Sleep(100);
                    while (pause_copy)
                        System.Threading.Thread.Sleep(100);
                    System.Threading.Thread.Sleep(200);
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
                    File.Move(i.item.FullName, new_path);
                    progress_value += i.size;
                    completed_count++;
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
            unsucsess = new OperationTree(null, false, "", operation_type, null, false, "Can't delete item");
            OperationTree temp;
            foreach (var i in root.children)
                DeleteSun(i);
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
                    foreach (var j in i.children)
                        DeleteSun(j);
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
                    OpBW.ReportProgress(0);
                    while (sleep_thread || pause_copy)
                        Thread.Sleep(100);
                    System.Threading.Thread.Sleep(200);
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
        internal void ExtractDialogShow(object archive, string extract_in, object files_names/*null to extract all*/, Op operation_type, string parameter1, bool parameter2)
        {
            this.operation_type = operation_type;
            if (operation_type == Op.Extract)
            {
                paste_in = extract_in;
                pause_button.IsEnabled = false;
                root = new OperationTree(null, false, "", operation_type, null, false, "");
                List<string> names = new List<string>();
                OpBW = new BackgroundWorker();
                ulong total_size = 0;
                foreach (CompresserListView i in (System.Collections.IList)files_names)
                    if (i.IsDirectory)
                        GetAllContaint(i, names, ref total_size);
                    else
                    { names.Add(i.FileFullName); total_size += i.file_size_long; }

                root.short_path = names.ToArray();
                if (files_names == null)
                    root.size = ((SevenZipExtractor)archive).PackedSize;
                else
                    root.size = (long)total_size;
                progress_bar.IsIndeterminate = false;
                progress_bar.Foreground = Brushes.RoyalBlue;
                progress_bar.Value = 0;
                progress_bar.Maximum = 100;
                ChangeRemain(((SevenZipExtractor)archive).FilesCount, true);
                Title = Title + " (Extracting...)";
                OpBW = new BackgroundWorker();
                OpBW.WorkerReportsProgress = true;
                OpBW.DoWork += Extracting_DoWork;
                OpBW.RunWorkerCompleted += Operation_RunWorkerCompleted;
                OpBW.ProgressChanged += Operation_ProgressChanged;
                OpBW.RunWorkerAsync(archive);
                ShowDialog();
            }
            else if (operation_type == Op.Compress)
            {
                Title = Title + " (Comprissing...)";
                paste_in = extract_in;
                List<string> list = new List<string>();
                foreach (CompresserListView i in (List<CompresserListView>)files_names)
                    if (i.IsDirectory)
                    {
                        //AddAllSuns(i.FileFullName, list);
                        list.Add(i.FileFullName);
                    }
                    else list.Add(i.FileFullName);
                root = new OperationTree(null, false, list.ToArray(), operation_type, null, false, "");
                OpBW = new BackgroundWorker();
                OpBW.WorkerReportsProgress = true;
                OpBW.DoWork += (object k, DoWorkEventArgs e) =>
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
                        SevenZipCompressor szc = (SevenZipCompressor)archive;
                        szc.Compressing += (object sender, SevenZip.ProgressEventArgs ex) => { progress_value = ex.PercentDone; };
                        szc.FileCompressionStarted += (object sender, FileNameEventArgs ex) => { current_file_name = ex.FileName; completed_count++; };
                        szc.CompressionFinished += (object sender, EventArgs ex) => { done = true; };
                        szc.FilesFound += (object sender, IntEventArgs ex) => { };
                        //if (root.short_path == null)
                        if (parameter2)
                        {
                            File.Copy(@"tools\compress\7z\sfx.sfx", extract_in);
                            szc.BeginCompressFiles(File.Open(extract_in, FileMode.OpenOrCreate), (string[])root.short_path);
                        }
                        else
                            szc.BeginCompressFiles(paste_in.ToString(), (string[])root.short_path);
                        //else
                        //    sze.BeginExtractFiles(paste_in, (string[])root.short_path);
                        while (!done) { Thread.Sleep(50); }
                    }
                    catch (Exception ex) { string x = ex.Message; }
                };
                OpBW.RunWorkerCompleted += Operation_RunWorkerCompleted;
                OpBW.ProgressChanged += Operation_ProgressChanged;
                OpBW.RunWorkerAsync();
                ShowDialog();
            }
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
            foreach (CompresserListView j in i.Children)
                if (j.IsDirectory)
                    GetAllContaint(j, names, ref size);
                else
                { names.Add(j.FileFullName); size += i.file_size_long; }
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
                progress_bar.Foreground = Brushes.RoyalBlue;
            }
        }

        private void Stop()
        {
            if (operation_type == Op.Copy)
            {
                MainWindow.IsPaste = true;
                try
                {
                    if (BW != null)
                        BW.Close();
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
            Close();
        }

        private void Pause()
        {
            if (TaskbarItemInfo.ProgressState != System.Windows.Shell.TaskbarItemProgressState.Error && !pause_copy)
            {
                pause_copy = true;
                play_pause_img.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/copy-start.png"));
                progress_bar.Foreground = Brushes.Gray;
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
        public Op operation;
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

        public OperationTree(FileInfo file, bool is_cut, object short_path, Op operation, OperationTree parent, string error)
        {
            this.parent = parent;
            //if (parent != null)
            //    parent.size += file.Length;
            item = file;
            completed = 0;
            done = false;
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

        public OperationTree(DirectoryInfo directory, bool is_cut, object short_path, Op operation, OperationTree parent, bool get_files, string error)
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
}



