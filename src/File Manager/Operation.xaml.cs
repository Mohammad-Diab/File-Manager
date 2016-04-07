using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        List<double> speed_samples = new List<double>();

        OperationTree root, already_exist, unsucsess;
        long progress_value, completed_count, old_progress_value;
        int progress_rount = 1, skipped_count = 0;
        bool sleep_thread = false, pause_copy = false, file_is_moved = false, CloseWindow = false, operation_finished = false, disk_destroy_busy = false;
        internal List<string> result_errors = new List<string>();
        internal List<string> result_notes = new List<string>();
        internal List<FileInfo> verified_files = new List<FileInfo>();

        internal long CompletedCount
        {
            get { return completed_count; }
        }

        internal int SkippedCount
        {
            get { return skipped_count; }
        }

        internal bool Stopped
        {
            get { return CloseWindow; }
        }

        internal const string WipeFolderName = "FileManager-Wipe";
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
            if (operation_type == OperationType.Copy)
            {
                ToCopyList = PrepareCopyList(ToCopyList, path_to_paste);
                if (ToCopyList == null)
                    return;
                path_to_paste = Path.GetFullPath(path_to_paste).TrimEnd('\\');
            }
            paste_in = path_to_paste;
            operation.Text = Title = operation_type == OperationType.Copy ? (ToCopyList.TrueForAll(x => x.IsCut) ? "Moving" : "Copying") : operation_type == OperationType.Delete ? "Deleting" : operation_type == OperationType.Rename ? "Renaming" : operation_type == OperationType.Attributes ? "Applaying Attributes" : operation_type == OperationType.Extract ? "Extracting Files" : "";
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

        List<FileOperationItem> PrepareCopyList(List<FileOperationItem> source_list, string path_to_paste)
        {
            string target;
            try { target = Path.GetFullPath(path_to_paste).TrimEnd('\\'); }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not paste here.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                return null;
            }
            List<FileOperationItem> list = new List<FileOperationItem>();
            List<string> refused = new List<string>(), cut_items = new List<string>();
            foreach (var i in source_list)
            {
                FileOperationItem item = i.Item is FileInfo ? new FileOperationItem((FileInfo)i.Item, i.IsCut, i.short_path) : new FileOperationItem((DirectoryInfo)i.Item, i.IsCut, i.short_path);
                item.Item.Refresh();
                string source = item.Item.FullName.TrimEnd('\\');
                if (!item.Item.Exists)
                {
                    refused.Add(source + "\n    (it does not exist or the drive is not ready)");
                    continue;
                }
                if (item.Item is DirectoryInfo && (string.Equals(source, target, StringComparison.OrdinalIgnoreCase) || target.StartsWith(source + "\\", StringComparison.OrdinalIgnoreCase)))
                {
                    refused.Add(source + "\n    (a directory can not be copied or moved into itself)");
                    continue;
                }
                if (item.Item is DirectoryInfo && ((DirectoryInfo)item.Item).Parent == null)
                    item.short_path = "Drive " + source.TrimEnd(':', '\\');
                string parent = Path.GetDirectoryName(source);
                if (parent != null && string.Equals(parent.TrimEnd('\\'), target, StringComparison.OrdinalIgnoreCase))
                {
                    if (item.IsCut)
                        continue;
                    item.short_path = Functions.GetCopyName(target, item.Item.Name, item.Item is FileInfo);
                }
                if (item.IsCut)
                    cut_items.Add(source);
                list.Add(item);
            }
            if (refused.Count > 0)
                Messages.ShowMessage(string.Format("{0} item{1} skipped.", refused.Count, refused.Count > 1 ? "s were" : " was"), ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", refused.ToArray()));
            if (list.Count == 0 || !Functions.ConfirmRiskyItems(cut_items, "move"))
                return null;
            return list;
        }

        private void CollectFiles_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (FileOperationItem i in (List<FileOperationItem>)e.Argument)
            {
                if (CloseWindow) return;
                if (i.Item is FileInfo)
                    root.children.Add(new OperationTree((FileInfo)i.Item, i.IsCut, i.short_path, OperationType, root, ""));
                else
                    root.children.Add(new OperationTree((DirectoryInfo)i.Item, i.IsCut, i.short_path, OperationType, root, OperationType == OperationType.Copy || OperationType == OperationType.Delete, ""));
            }
        }

        void CompleteCollectFiles(long maximum_progress_bar_value, string title)
        {
            if (CloseWindow)
            {
                operation_finished = true;
                Close();
                return;
            }
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
            OpBW.RunWorkerCompleted += Operation_RunWorkerCompleted;
            OpBW.ProgressChanged += Operation_ProgressChanged;
            OpBW.RunWorkerAsync(root);
        }

        void StartProgressThread(BackgroundWorker worker)
        {
            Thread progress_thread = new Thread(() =>
            {
                while (true)
                {
                    if (!worker.IsBusy)
                        break;
                    try
                    { worker.ReportProgress(0); }
                    catch (Exception) { }
                    while ((sleep_thread || pause_copy) && !CloseWindow)
                        Thread.Sleep(100);
                    Thread.Sleep(200);
                }
            });
            progress_thread.IsBackground = true;
            progress_thread.Start();
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
                    double per_second = (progress_value - old_progress_value) / 4.0;
                    if (operation_type == OperationType.Copy)
                        speed.Text = Functions.FileSize(per_second, 1) + "/s";
                    else if (operation_type == OperationType.Delete || operation_type == OperationType.Attributes)
                    {
                        double x = Math.Round(per_second, 1);
                        speed.Text = string.Format("{0} item{1} per {2}", x > 2 ? x : 60 * x, x > 2 ? "s" : x * 60 > 2 ? "s" : "", x > 2 ? "second" : "minute");
                    }
                    else if (operation_type == OperationType.Extract)
                    {
                        per_second = (progress_value - old_progress_value) * root.size / 400.0;
                        speed.Text = Functions.FileSize(per_second, 1) + "/s";
                    }
                    AddSpeedSample(per_second);
                    old_progress_value = progress_value;
                }
                else
                    progress_rount++;

                item_name.ToolTip = item_name.Text = current_file_name;
            }
        }

        void AddSpeedSample(double value)
        {
            speed_samples.Add(Math.Max(0, value));
            if (speed_samples.Count > 60)
                speed_samples.RemoveAt(0);
            double max = speed_samples.Max();
            double width = speed_graph.ActualWidth, height = speed_graph.ActualHeight;
            if (width <= 0 || height <= 0)
                return;
            PointCollection points = new PointCollection();
            for (int i = 0; i < speed_samples.Count; i++)
                points.Add(new Point(speed_samples.Count > 1 ? i * width / (speed_samples.Count - 1) : width, height - (max > 0 ? speed_samples[i] / max * (height - 2) : 0) - 1));
            speed_line.Points = points;
            speed_line.Stroke = ProgressBrush;
            speed_graph.ToolTip = "Speed during the last " + speed_samples.Count * (operation_type == OperationType.Destroy || operation_type == OperationType.DiskDestroy || operation_type == OperationType.Encrypt || operation_type == OperationType.Decrypt ? 2 : 4) + " seconds";
        }

        private void Operation_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            operation_finished = true;
            if (e.Error != null)
                Messages.ShowMessage("The operation has stopped because of an error.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, e.Error.Message);
            List<string> unreadable = root != null ? root.unreadable : null;
            if (unreadable != null && unreadable.Count > 0)
                Messages.ShowMessage(string.Format("{0} director{1} could not be read and {2} skipped.", unreadable.Count, unreadable.Count > 1 ? "ies" : "y", unreadable.Count > 1 ? "were" : "was"), ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", unreadable.Take(50).ToArray()));
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
            OperationTree k = add_in;
            bool continue_search = true;
            for (int j = queue.Count - 1; j > -1; j--)
            {
                int index = 0;
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
                        t = new OperationTree((DirectoryInfo)queue[j].item, queue[j].is_cut, queue[j].short_path, queue[j].operation, queue[j].parent, false, j == 0 ? queue[j].error_message : "");
                    else
                        t = new OperationTree((FileInfo)queue[j].item, queue[j].is_cut, queue[j].short_path, queue[j].operation, queue[j].parent, queue[j].error_message);
                    t.size = queue[j].size;
                    k.children.Add(t);
                    k = t;
                    continue_search = false;
                }
            }
        }

        static bool SameRoot(string first, string second)
        {
            try { return string.Equals(Path.GetPathRoot(first).TrimEnd('\\'), Path.GetPathRoot(second).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase); }
            catch (Exception) { return false; }
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
            StartProgressThread(OpBW);
            already_exist = new OperationTree(null, root.is_cut, "", operation_type, null, false, "File already exist");
            unsucsess = new OperationTree(null, root.is_cut, "", operation_type, null, false, "Some thing wrong");
            OperationTree temp;
            foreach (var i in root.children)
            {
                if (CloseWindow) return;
                CopySun(i);
            }
            while (!CloseWindow)
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
                        if (CloseWindow) return;
                        if (i.user_option == OperationOption.Skip)
                        {
                            i.Done = true;
                            progress_value += i.size;
                            completed_count++;
                            skipped_count++;
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
                    {
                        if (CloseWindow) return;
                        if (i.user_option == OperationOption.Skip)
                        {
                            i.Done = true;
                            progress_value += i.size;
                            completed_count++;
                            skipped_count++;
                        }
                        else
                            CopySun(i);
                    }
                }
                if (unsucsess.children.Count == 0 && already_exist.children.Count == 0)
                    break;
            }
        }

        private void CopySun(OperationTree i)
        {
            if (CloseWindow)
                return;
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
                        skipped_count++;
                    }
                    else
                    {
                        string target_path = paste_in + "\\" + i.short_path;
                        if (File.Exists(target_path) && i.user_option == OperationOption.NoValue)
                        {
                            i.error_message = "File already existe.";
                            AddError(i, already_exist);
                        }
                        else if (Directory.Exists(target_path))
                        {
                            i.error_message = "A directory with the same name already exists in the target.";
                            AddError(i, unsucsess);
                        }
                        else
                        {
                            long free_space = -1;
                            string drive_format = "";
                            try
                            {
                                DriveInfo target_drive = new DriveInfo(Path.GetPathRoot(paste_in));
                                if (target_drive.IsReady)
                                {
                                    free_space = target_drive.AvailableFreeSpace;
                                    drive_format = target_drive.DriveFormat.ToLower();
                                }
                            }
                            catch (Exception) { }
                            bool same_drive_move = i.is_cut && SameRoot(i.item.FullName, paste_in);
                            if (!same_drive_move && i.user_option != OperationOption.Continue && free_space >= 0 && free_space <= i.size)
                            {
                                i.error_message = "Target drive's free space is less than file size";
                                AddError(i, unsucsess);
                            }
                            else if ((drive_format == "fat" || drive_format == "fat32") && i.size > 4294967295)
                            {
                                i.error_message = "Target's drive's file format can't fit files more than 4 GB.";
                                AddError(i, unsucsess);
                            }
                            else
                                WriteFile(i);
                        }
                    }
                }
                else if (i.is_link)
                {
                    current_file_name = i.item.FullName;
                    string link_path = paste_in + "\\" + i.short_path;
                    try
                    {
                        if (Directory.Exists(link_path) || File.Exists(link_path))
                            throw new IOException("An item with the same name already exists in the target.");
                        CreatePath(link_path, 1);
                        if (i.is_cut && SameRoot(i.item.FullName, link_path))
                            Directory.Move(i.item.FullName, link_path);
                        else
                        {
                            Classes.JunctionPoint.Create(link_path, Classes.JunctionPoint.GetTarget(i.item.FullName), true);
                            if (i.is_cut)
                                Directory.Delete(i.item.FullName, false);
                        }
                        i.Done = true;
                        completed_count++;
                    }
                    catch (Exception ex)
                    {
                        i.error_message = "This link was not copied (links are never followed): " + ex.Message;
                        AddError(i, unsucsess);
                    }
                }
                else
                {
                    if (!CreatePath(paste_in + "\\" + i.short_path, 0))
                    {
                        i.error_message = "Can't create directory in the target.";
                        AddError(i, unsucsess);
                        return;
                    }
                    foreach (var j in i.children)
                    {
                        if (CloseWindow) return;
                        CopySun(j);
                    }
                    if (i.children.Count == i.completed)
                    {
                        i.Done = true;
                        if (i.is_cut)
                        {
                            try
                            {
                                if (((DirectoryInfo)i.item).GetFiles().Length + ((DirectoryInfo)i.item).GetDirectories().Length == 0)
                                    i.item.Delete();
                                else
                                {
                                    i.error_message = "Can't delete directory, it is not empty.";
                                    AddError(i, unsucsess);
                                }
                            }
                            catch (Exception) { i.error_message = "Can't delete directory"; AddError(i, unsucsess); }
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
            try
            {
                string directory = to == 1 ? Path.GetDirectoryName(path) : path;
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                return true;
            }
            catch (Exception) { return false; }
        }

        void WriteFile(OperationTree i)
        {
            string new_path = paste_in + "\\" + i.short_path;
            bool resume = false, created = false;
            long done_bytes = 0;
            try
            {
                if (File.Exists(new_path))
                {
                    string source_id = Functions.GetFileId(i.item.FullName);
                    if (string.Equals(Path.GetFullPath(new_path), Path.GetFullPath(i.item.FullName), StringComparison.OrdinalIgnoreCase) || (source_id != null && source_id == Functions.GetFileId(new_path)))
                        throw new IOException("The source and the target are the same file.");
                    if (i.user_option == OperationOption.Over)
                    {
                        File.SetAttributes(new_path, FileAttributes.Normal);
                        File.Delete(new_path);
                    }
                    else if (i.user_option == OperationOption.CopyWithNewName)
                    {
                        FileInfo target_file = new FileInfo(new_path);
                        string extention = target_file.Extension;
                        string name;
                        name = extention.Length > 0 ? target_file.Name.Remove(target_file.Name.Length - extention.Length) : target_file.Name;
                        int num = 1;
                        while (File.Exists(Path.Combine(target_file.DirectoryName, name + " (" + num + ")" + extention))) num++;
                        new_path = Path.Combine(target_file.DirectoryName, name + " (" + num + ")" + extention);
                    }
                    else if (i.user_option == OperationOption.Continue)
                        resume = true;
                    else
                        throw new IOException("The file already exists in the target.");
                }
                if (!CreatePath(new_path, 1))
                    throw new IOException("Can't write file on drive.");
                if (!resume && i.is_cut && SameRoot(i.item.FullName, new_path))
                {
                    File.Move(i.item.FullName, new_path);
                    progress_value += i.size;
                    completed_count++;
                    i.Done = true;
                    return;
                }
                using (FileStream source = new FileStream(i.item.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream target = new FileStream(new_path, resume ? FileMode.Append : FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    created = !resume;
                    if (resume)
                    {
                        if (target.Length > source.Length)
                            throw new IOException("The file in the target is bigger than the source, it can not be continued.");
                        source.Position = target.Length;
                        done_bytes = target.Length;
                        progress_value += done_bytes;
                    }
                    byte[] buffer = new byte[Functions.buffer];
                    int read;
                    while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        while (pause_copy && !CloseWindow)
                            Thread.Sleep(250);
                        if (CloseWindow)
                            throw new OperationCanceledException();
                        target.Write(buffer, 0, read);
                        progress_value += read;
                        done_bytes += read;
                    }
                }
                try
                {
                    File.SetAttributes(new_path, File.GetAttributes(i.item.FullName));
                    File.SetCreationTimeUtc(new_path, i.item.CreationTimeUtc);
                    File.SetLastAccessTimeUtc(new_path, i.item.LastAccessTimeUtc);
                    File.SetLastWriteTimeUtc(new_path, i.item.LastWriteTimeUtc);
                }
                catch (Exception) { }
                i.Done = true;
                if (i.is_cut)
                    try
                    {
                        File.SetAttributes(i.item.FullName, FileAttributes.Normal);
                        i.item.Delete();
                    }
                    catch (Exception) { i.error_message = "Can't delete file"; AddError(i, unsucsess); }
                completed_count++;
            }
            catch (OperationCanceledException)
            {
                progress_value -= done_bytes;
                if (created)
                    try { File.Delete(new_path); }
                    catch (Exception) { }
            }
            catch (Exception ex)
            {
                progress_value -= done_bytes;
                if (created)
                    try { File.Delete(new_path); }
                    catch (Exception) { }
                i.error_message = ex.Message;
                AddError(i, unsucsess);
            }
        }

        #endregion

        #region Delete Region

        private void CollectFilesToDelete_DoWork(object sender, DoWorkEventArgs e)
        {
            foreach (FileOperationItem i in (List<FileOperationItem>)e.Argument)
            {
                if (CloseWindow) return;
                if (i.Item is FileInfo)
                    root.children.Add(new OperationTree((FileInfo)i.Item, false, i.short_path, OperationType.Delete, root, ""));
                else
                    root.children.Add(new OperationTree((DirectoryInfo)i.Item, false, i.short_path, operation_type, root, true, ""));
            }
        }

        private void CollectFilesToDelete_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                operation_finished = true;
                Messages.ShowMessage("We could not collect the items to delete, nothing was deleted.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, e.Error.Message);
                Close();
                return;
            }
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
            StartProgressThread(OpBW);
            OperationTree temp;
            foreach (var i in root.children)
            {
                if (CloseWindow) return;
                DeleteSun(i);
                while (pause_copy && !CloseWindow)
                    Thread.Sleep(100);
            }
            while (!CloseWindow)
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
                    {
                        if (CloseWindow) return;
                        if (i.user_option == OperationOption.Skip)
                        {
                            i.Done = true;
                            progress_value++;
                            completed_count++;
                            skipped_count++;
                        }
                        else
                            DeleteSun(i);
                    }
                }
                if (unsucsess.children.Count == 0)
                    break;
            }
        }

        private void DeleteSun(OperationTree i)
        {
            if (CloseWindow)
                return;
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
                        skipped_count++;
                    }
                    else
                    {
                        i.item.Refresh();
                        if (i.item.Exists)
                            try
                            {
                                if (i.item.Attributes.HasFlag(FileAttributes.ReadOnly))
                                    i.item.Attributes &= ~FileAttributes.ReadOnly;
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
                else if (i.is_link)
                {
                    current_file_name = i.item.FullName;
                    try
                    {
                        Directory.Delete(i.item.FullName, false);
                        i.Done = true;
                        progress_value++;
                        completed_count++;
                    }
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
                        if (CloseWindow) return;
                        DeleteSun(j);
                        while (pause_copy && !CloseWindow)
                            Thread.Sleep(100);
                    }
                    if (i.children.Count == i.completed)
                    {
                        current_file_name = i.item.FullName;
                        i.item.Refresh();
                        if (i.item.Exists)
                        {
                            long d = -1;
                            try
                            { d = ((DirectoryInfo)i.item).GetDirectories().Length + ((DirectoryInfo)i.item).GetFiles().Length; }
                            catch (Exception) { }
                            if (d == 0)
                            {
                                try
                                {
                                    if (i.item.Attributes.HasFlag(FileAttributes.ReadOnly))
                                        i.item.Attributes &= ~FileAttributes.ReadOnly;
                                    i.item.Delete();
                                    i.Done = true;
                                    progress_value++;
                                    completed_count++;
                                }
                                catch (Exception ex) { i.error_message = ex.Message; AddError(i, unsucsess); }
                            }
                            else
                            {
                                i.error_message = d < 0 ? "Can't read the directory content." : "Can't delete directory, it is not empty.";
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

        #endregion

        #region Rename Region

        private void CollectFilesToRename_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CompleteCollectFiles(root.children.Count, root.children.Count + " item" + ((root.children.Count > 1) ? "s" : ""));
        }

        private void Renaming_DoWork(object sender, DoWorkEventArgs e)
        {
            StartProgressThread(OpBW);
            unsucsess = new OperationTree(null, false, "", operation_type, null, false, "Can't Rename item");
            OperationTree temp;
            foreach (var i in root.children)
            {
                if (CloseWindow) return;
                RenameSun(i);
            }
            while (!CloseWindow)
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
                            skipped_count++;
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
            if (!i.Done && !CloseWindow)
            {
                current_file_name = i.item.FullName;
                string folder = Path.GetDirectoryName(i.item.FullName.TrimEnd('\\'));
                string target = Path.Combine(folder, i.short_path.ToString());
                if (i.user_option == OperationOption.Skip || i.item.FullName == target)
                {
                    i.Done = true;
                    progress_value++;
                    completed_count++;
                }
                else if (i.item.Exists)
                {
                    string new_name = target;
                    bool case_only = string.Equals(i.item.FullName, target, StringComparison.OrdinalIgnoreCase);
                    if (!case_only && (File.Exists(target) || Directory.Exists(target)))
                    {
                        string name = i.children == null ? Path.GetFileNameWithoutExtension(target) : Path.GetFileName(target);
                        string extention = i.children == null ? Path.GetExtension(target) : "";
                        int num = 1;
                        do new_name = Path.Combine(folder, name + " (" + num++ + ")" + extention);
                        while (File.Exists(new_name) || Directory.Exists(new_name));
                    }
                    string temp_name = null;
                    try
                    {
                        if (case_only)
                        {
                            temp_name = Path.Combine(folder, Guid.NewGuid().ToString("N"));
                            if (i.children == null)
                                File.Move(i.item.FullName, temp_name);
                            else
                                Directory.Move(i.item.FullName, temp_name);
                            if (i.children == null)
                                File.Move(temp_name, new_name);
                            else
                                Directory.Move(temp_name, new_name);
                        }
                        else if (i.children == null)
                            File.Move(i.item.FullName, new_name);
                        else
                            Directory.Move(i.item.FullName, new_name);
                        i.Done = true;
                        progress_value++;
                        completed_count++;
                    }
                    catch (Exception ex)
                    {
                        if (temp_name != null)
                            try
                            {
                                if (File.Exists(temp_name))
                                    File.Move(temp_name, i.item.FullName);
                                else if (Directory.Exists(temp_name))
                                    Directory.Move(temp_name, i.item.FullName);
                            }
                            catch (Exception) { }
                        i.error_message = ex.Message;
                        AddError(i, unsucsess);
                    }
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
            StartProgressThread(OpBW);
            unsucsess = new OperationTree(null, false, "", operation_type, null, false, "Can't applying attribute to item");
            OperationTree temp;
            foreach (var i in root.children)
            {
                if (CloseWindow) return;
                ApplyingAttributeToSun(i);
            }
            while (!CloseWindow)
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
                            skipped_count++;
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
            if (!i.Done && !CloseWindow)
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
                    current_file_name = i.item.FullName;
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
                i.item.Refresh();
                bool is_compressed = i.item.Attributes.HasFlag(FileAttributes.Compressed), is_encrypted = i.item.Attributes.HasFlag(FileAttributes.Encrypted);
                if (!is_compressed && fa.HasFlag(FileAttributes.Compressed))
                    result = (i.item is DirectoryInfo) ? DirectoryCompress(i.item, true) : FileCompress(i.item.FullName, true);
                else if (is_compressed && !fa.HasFlag(FileAttributes.Compressed) && !fa.HasFlag(FileAttributes.Encrypted))
                    result = (i.item is DirectoryInfo) ? DirectoryCompress(i.item, false) : FileCompress(i.item.FullName, false);
                if (!is_encrypted && fa.HasFlag(FileAttributes.Encrypted))
                    File.Encrypt(i.item.FullName);
                else if (is_encrypted && !fa.HasFlag(FileAttributes.Encrypted))
                    File.Decrypt(i.item.FullName);
                FileAttributes simple = fa & (FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive | FileAttributes.Temporary | FileAttributes.Offline | FileAttributes.NotContentIndexed);
                i.item.Attributes = simple == 0 ? FileAttributes.Normal : simple;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int DeviceIoControl(IntPtr hDevice, int dwIoControlCode, ref short lpInBuffer, int nInBufferSize, IntPtr lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr lpOverlapped);
        int FileCompress(string file_name, bool compress)
        {
            int lpBytesReturned = 0;
            int FSCTL_SET_COMPRESSION = 0x9C040;
            short COMPRESSION_FORMAT = (short)(compress ? 1 : 0);
            using (FileStream stream = File.Open(file_name, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None))
                return DeviceIoControl(stream.SafeFileHandle.DangerousGetHandle(), FSCTL_SET_COMPRESSION, ref COMPRESSION_FORMAT, 2, IntPtr.Zero, 0, ref lpBytesReturned, IntPtr.Zero) != 0 ? 1 : 0;
        }

        int DirectoryCompress(FileSystemInfo dir, bool compress)
        {
            try
            {
                string objPath = "Win32_Directory.Name=" + "'" + dir.FullName.Replace("\\", @"\\").Replace("'", @"\'").TrimEnd('\\') + "'";
                using (ManagementObject obj = new ManagementObject(objPath))
                using (ManagementBaseObject result = obj.InvokeMethod(compress ? "Compress" : "Uncompress", null, null))
                    return Convert.ToInt32(result["ReturnValue"]) == 0 ? 1 : 0;
            }
            catch (Exception ex)
            { System.Diagnostics.Trace.WriteLine("Cannot change compression for folder '" + dir.FullName + "': " + ex.Message, "WMI"); return 0; }
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
        Random name_random = new Random();

        internal static void CleanWipeFolders()
        {
            try
            {
                foreach (var i in DriveInfo.GetDrives())
                    try
                    {
                        if ((i.DriveType == DriveType.Fixed || i.DriveType == DriveType.Removable) && i.IsReady)
                            DeleteWipeFolder(Path.Combine(i.Name, WipeFolderName));
                    }
                    catch (Exception) { }
            }
            catch (Exception) { }
        }

        static void DeleteWipeFolder(string folder)
        {
            if (Directory.Exists(folder) && !Functions.IsLink(new DirectoryInfo(folder)))
            {
                foreach (var i in Directory.GetFiles(folder))
                    DeleteQuietly(i);
                try { Directory.Delete(folder, false); }
                catch (Exception) { }
            }
        }

        private void Destroy_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage < 0)
            {
                progress_bar.Foreground = Brushes.Tomato;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                if (e.ProgressPercentage == -1)
                    new File_s_Transferring_Error(unsucsess, "We can not destroy these files, what do you want to do with them?").ShowThisDialog();
                progress_bar.Foreground = ProgressBrush;
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                sleep_thread = false;
            }
            else
            {
                progress_bar.Value = Math.Min(progress_value, progress_bar.Maximum);
                double val = 0;
                val = progress_bar.Value * 100.0 / (progress_bar.Maximum + double.Epsilon);
                ChangeRemain(Math.Max(0, progress_bar.Maximum - progress_value), false);
                percent.Text = Math.Round(val, 0) + "%";
                TaskbarItemInfo.ProgressValue = val / 100;
                completed.Text = completed_count.ToString();
                if (progress_rount % 20 == 0)
                {
                    progress_rount = 1;
                    double per_second = (progress_value - old_progress_value) / 2.0;
                    speed.Text = Functions.FileSize(per_second, 1) + "/s";
                    AddSpeedSample(per_second);
                    old_progress_value = progress_value;
                }
                else
                    progress_rount++;
                item_name.Text = current_file_name;
                if (name_textblock.ActualWidth > ActualWidth - name_textblock.Margin.Left - name_textblock.Margin.Right)
                    item_name.ToolTip = current_file_name;
            }
        }

        string GetFileName(string parent)
        {
            string drive_name = Path.GetPathRoot(parent).ToLower();
            if (!drives_format.ContainsKey(drive_name))
                try { drives_format.Add(drive_name, new DriveInfo(drive_name).DriveFormat.ToLower()); }
                catch (Exception) { drives_format.Add(drive_name, ""); }

            StringBuilder name = new StringBuilder(string.Empty);
            while (true)
            {
                name.Clear();
                name.Append(parent.TrimEnd('\\'));
                name.Append("\\$");
                int max_file_name = 8;
                switch (drives_format[drive_name])
                {
                    case "ntfs":
                    case "fat16":
                    case "fat32":
                    case "exfat":
                        if (parent.Length > 15)
                            max_file_name = 240 - parent.Length;
                        else
                            max_file_name = 220;
                        break;
                    case "fat":
                    case "fat12":
                        max_file_name = 8;
                        break;
                }
                if (max_file_name < 8)
                    max_file_name = 8;
                for (int i = 0; i < max_file_name - 1; i++)
                    name.Append(Chars[name_random.Next(Chars.Length - 1)]);
                name.Append(".");
                for (int i = 0; i < 3; i++)
                    name.Append(Chars[name_random.Next(Chars.Length - 1)]);
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!operation_finished && !CloseWindow)
            {
                if (Messages.ShowMessage("Stop this operation?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") != MyMessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
                StopOperation();
            }
            if (disk_destroy_busy)
            {
                e.Cancel = true;
                operation.Text = operation_type == OperationType.DiskDestroy ? "Cleaning up, please wait..." : "Stopping, please wait...";
            }
        }

        #endregion

        #region Encrypt Files

        static byte[] file_imprint = { 0x46, 0x4D, 0x45, 0x3A, 0x20, 0x65, 0x46, 0x69, 0x6C, 0x20, 0x4D, 0x61, 0x6E, 0x61, 0x67, 0x65, 0x72, 0x20, 0x45, 0x6E, 0x63, 0x72, 0x79, 0x70, 0x74, 0x65, 0x64, 0x20, 0x46, 0x69, 0x6C, 0x65, 0x2E, 0x42, 0x79, 0x20, 0x44, 0x72, 0x2E, 0x4D, 0x6F, 0x68, 0x61, 0x6D, 0x6D, 0x61, 0x64 };//FME: File Manager Encrypted File.By Dr.Mohammad

        static byte[] fme_signature = { 16, 191, 91, 144, 31, 2, 70, 73, 76, 69, 32, 77, 65, 78, 65, 71, 69, 82, 32, 101, 110, 99, 114, 121, 112, 116, 101, 100, 32, 102, 105, 108, 101, 44, 32, 118, 101, 114, 115, 105, 111, 110, 58, 32 };
        const int fme_iterations = 200000, fme_header_length = 122;
        static RNGCryptoServiceProvider random_generator = new RNGCryptoServiceProvider();

        static byte[] RandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            lock (random_generator)
                random_generator.GetBytes(bytes);
            return bytes;
        }

        internal static byte[] LegacyPassword(string password)
        {
            byte[] bytes = new byte[password.Length * sizeof(char)];
            Buffer.BlockCopy(password.ToCharArray(), 0, bytes, 0, bytes.Length);
            using (MD5 md5 = new MD5Cng())
                return md5.ComputeHash(bytes);
        }

        static void DeriveKeys(string password, byte[] salt, int iterations, out byte[] encryption_key, out byte[] mac_key)
        {
            using (Rfc2898DeriveBytes derive = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), salt, iterations))
            {
                encryption_key = derive.GetBytes(32);
                mac_key = derive.GetBytes(32);
            }
        }

        static bool SameBytes(byte[] first, byte[] second)
        {
            if (first == null || second == null || first.Length != second.Length)
                return false;
            int difference = 0;
            for (int i = 0; i < first.Length; i++)
                difference |= first[i] ^ second[i];
            return difference == 0;
        }

        static string SafeFileName(string name)
        {
            name = Path.GetFileName((name ?? "").Replace('/', '\\'));
            foreach (char i in Path.GetInvalidFileNameChars())
                name = name.Replace(i, '_');
            name = name.Trim().TrimEnd('.');
            return name.Length == 0 ? "file" : name;
        }

        static string UniqueFilePath(string folder, string name)
        {
            string path = Path.Combine(folder, name);
            if (!File.Exists(path) && !Directory.Exists(path) && !File.Exists(path + ".partial"))
                return path;
            string extention = Path.GetExtension(name), base_name = Path.GetFileNameWithoutExtension(name);
            int num = 1;
            do path = Path.Combine(folder, base_name + " (" + num++ + ")" + extention);
            while (File.Exists(path) || Directory.Exists(path) || File.Exists(path + ".partial"));
            return path;
        }

        static string OutputFileName(string folder, string extention)
        {
            string zeroz = "0000000000";
            int num = 1;
            string file_name = "file" + zeroz.Substring(0, 10 - num.ToString().Length) + num.ToString() + extention;
            while (File.Exists(Path.Combine(folder, file_name)) || File.Exists(Path.Combine(folder, file_name + ".partial")))
            {
                num++;
                file_name = "file" + zeroz.Substring(0, 10 - num.ToString().Length) + num.ToString() + extention;
            }
            return Path.Combine(folder, file_name);
        }

        static void DeleteQuietly(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
            }
            catch (Exception) { }
        }

        void EncryptToFile(List<FileInfo> files, string password, string output_path, bool encrypt_files_name, bool is_package)
        {
            string temp_path = output_path + ".partial";
            List<FmeItem> items = new List<FmeItem>();
            foreach (var i in files)
                items.Add(new FmeItem() { name = i.Name, iv = new byte[16], mac = new byte[32], plain_hash = new byte[32] });
            bool done = false;
            try
            {
                byte[] salt = RandomBytes(16), index_iv = RandomBytes(16), encryption_key, mac_key;
                DeriveKeys(password, salt, fme_iterations, out encryption_key, out mac_key);
                int index_length = StoredIndexLength(items, encrypt_files_name);
                using (FileStream output = new FileStream(temp_path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                {
                    output.Position = fme_header_length + index_length;
                    for (int i = 0; i < files.Count; i++)
                    {
                        current_file_name = files[i].FullName;
                        string error = EncryptStream(files[i], output, encryption_key, mac_key, items[i]);
                        if (error != null)
                        {
                            result_errors.Add(files[i].FullName + "\n    (" + error + ")");
                            if (is_package && !CloseWindow)
                                result_errors.Add(output_path + "\n    (the package was not created because a file failed)");
                            throw new OperationCanceledException();
                        }
                    }
                    byte[] stored_index = StoreIndex(items, encrypt_files_name, encryption_key, index_iv);
                    byte[] header = new byte[fme_header_length - 32];
                    using (MemoryStream header_stream = new MemoryStream(header))
                    using (BinaryWriter header_writer = new BinaryWriter(header_stream))
                    {
                        header_writer.Write(is_package ? (byte)205 : (byte)204);
                        header_writer.Write(encrypt_files_name ? (byte)140 : (byte)139);
                        header_writer.Write(fme_signature);
                        header_writer.Write(4f);
                        header_writer.Write(salt);
                        header_writer.Write(fme_iterations);
                        header_writer.Write(index_iv);
                        header_writer.Write(stored_index.Length);
                    }
                    byte[] index_mac;
                    using (HMACSHA256 hmac = new HMACSHA256(mac_key))
                    {
                        hmac.TransformBlock(header, 0, header.Length, null, 0);
                        hmac.TransformFinalBlock(stored_index, 0, stored_index.Length);
                        index_mac = hmac.Hash;
                    }
                    output.Position = 0;
                    output.Write(header, 0, header.Length);
                    output.Write(index_mac, 0, index_mac.Length);
                    output.Write(stored_index, 0, stored_index.Length);
                    output.Flush(true);
                }
                for (int i = 0; i < files.Count; i++)
                {
                    if (CloseWindow)
                        throw new OperationCanceledException();
                    current_file_name = output_path;
                    string error;
                    if (!VerifyEncryptedItem(temp_path, items[i], encryption_key, mac_key, out error) || files[i].Length != items[i].plain_length)
                    {
                        result_errors.Add(files[i].FullName + "\n    (the encrypted copy could not be verified" + (error == null ? "" : ": " + error) + ")");
                        throw new OperationCanceledException();
                    }
                }
                File.Move(temp_path, output_path);
                done = true;
                verified_files.AddRange(files);
                completed_count += files.Count;
                result_notes.Add(output_path);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            { result_errors.Add(output_path + "\n    (" + ex.Message + ")"); }
            if (!done)
                DeleteQuietly(temp_path);
        }

        string EncryptStream(FileInfo file, Stream output, byte[] encryption_key, byte[] mac_key, FmeItem item)
        {
            try
            {
                item.iv = RandomBytes(16);
                item.data_offset = output.Position;
                using (FileStream input = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                using (HMACSHA256 hmac = new HMACSHA256(mac_key))
                using (SHA256 hasher = new SHA256Cng())
                {
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = encryption_key;
                    aes.IV = item.iv;
                    hmac.TransformBlock(item.iv, 0, item.iv.Length, null, 0);
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    using (CryptoStream crypto = new CryptoStream(new HashStream(hmac, output), encryptor, CryptoStreamMode.Write))
                    {
                        byte[] buffer = new byte[Functions.buffer];
                        int read;
                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            while (pause_copy && !CloseWindow)
                                Thread.Sleep(50);
                            if (CloseWindow)
                                return "stopped";
                            hasher.TransformBlock(buffer, 0, read, null, 0);
                            crypto.Write(buffer, 0, read);
                            item.plain_length += read;
                            progress_value += read;
                        }
                        crypto.FlushFinalBlock();
                    }
                    hasher.TransformFinalBlock(new byte[0], 0, 0);
                    hmac.TransformFinalBlock(new byte[0], 0, 0);
                    item.plain_hash = hasher.Hash;
                    item.mac = hmac.Hash;
                    item.data_length = output.Position - item.data_offset;
                }
                return null;
            }
            catch (Exception ex) { return ex.Message; }
        }

        bool VerifyEncryptedItem(string path, FmeItem item, byte[] encryption_key, byte[] mac_key, out string error)
        {
            error = null;
            try
            {
                using (FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (!CheckItemMac(input, item, mac_key))
                    {
                        error = "the data is damaged";
                        return false;
                    }
                    using (SHA256 hasher = new SHA256Cng())
                    {
                        using (CryptoStream crypto = new CryptoStream(new HashStream(hasher), CreateDecryptor(encryption_key, item.iv), CryptoStreamMode.Write))
                        {
                            if (!CopyRegion(input, item.data_offset, item.data_length, crypto, true))
                            {
                                error = "stopped";
                                return false;
                            }
                            crypto.FlushFinalBlock();
                        }
                        hasher.TransformFinalBlock(new byte[0], 0, 0);
                        return SameBytes(hasher.Hash, item.plain_hash);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        static ICryptoTransform CreateDecryptor(byte[] encryption_key, byte[] iv)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                return aes.CreateDecryptor(encryption_key, iv);
            }
        }

        bool CopyRegion(Stream input, long start, long length, Stream output, bool count_progress)
        {
            input.Position = start;
            byte[] buffer = new byte[Functions.buffer];
            long remain = length;
            while (remain > 0)
            {
                while (pause_copy && !CloseWindow)
                    Thread.Sleep(50);
                if (CloseWindow)
                    return false;
                int read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remain));
                if (read <= 0)
                    throw new EndOfStreamException("The encrypted file is shorter than expected.");
                output.Write(buffer, 0, read);
                remain -= read;
                if (count_progress)
                    progress_value += read;
            }
            return true;
        }

        bool CheckItemMac(Stream input, FmeItem item, byte[] mac_key)
        {
            using (HMACSHA256 hmac = new HMACSHA256(mac_key))
            {
                hmac.TransformBlock(item.iv, 0, item.iv.Length, null, 0);
                if (!CopyRegion(input, item.data_offset, item.data_length, new HashStream(hmac), false))
                    return false;
                hmac.TransformFinalBlock(new byte[0], 0, 0);
                return SameBytes(hmac.Hash, item.mac);
            }
        }

        static byte[] SerializeIndex(List<FmeItem> items)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(items.Count);
                foreach (var i in items)
                {
                    writer.Write(i.name);
                    writer.Write(i.plain_length);
                    writer.Write(i.data_offset);
                    writer.Write(i.data_length);
                    writer.Write(i.iv);
                    writer.Write(i.mac);
                    writer.Write(i.plain_hash);
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        static List<FmeItem> DeserializeIndex(byte[] index)
        {
            List<FmeItem> items = new List<FmeItem>();
            using (MemoryStream stream = new MemoryStream(index))
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
            {
                int count = reader.ReadInt32();
                if (count < 0 || count > 1000000)
                    throw new InvalidDataException("The file index is damaged.");
                for (int i = 0; i < count; i++)
                    items.Add(new FmeItem() { name = reader.ReadString(), plain_length = reader.ReadInt64(), data_offset = reader.ReadInt64(), data_length = reader.ReadInt64(), iv = reader.ReadBytes(16), mac = reader.ReadBytes(32), plain_hash = reader.ReadBytes(32) });
            }
            return items;
        }

        static int StoredIndexLength(List<FmeItem> items, bool encrypt_files_name)
        {
            int length = SerializeIndex(items).Length;
            return encrypt_files_name ? (length / 16 + 1) * 16 : length;
        }

        static byte[] StoreIndex(List<FmeItem> items, bool encrypt_files_name, byte[] encryption_key, byte[] index_iv)
        {
            byte[] index = SerializeIndex(items);
            if (!encrypt_files_name)
                return index;
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform encryptor = aes.CreateEncryptor(encryption_key, index_iv))
                    return encryptor.TransformFinalBlock(index, 0, index.Length);
            }
        }

        internal static FmeFileInfo ReadFmeHeader(Stream input, string password)
        {
            FmeFileInfo info = new FmeFileInfo();
            byte[] start = new byte[47];
            input.Position = 0;
            int count = input.Read(start, 0, start.Length);
            if (count == start.Length && SameBytes(start, file_imprint))
            {
                info.version = 1;
                return info;
            }
            input.Position = 0;
            BinaryReader reader = new BinaryReader(input);
            if (input.Length < 50)
                return info;
            byte b0 = reader.ReadByte(), b1 = reader.ReadByte();
            if ((b0 != 204 && b0 != 205) || (b1 != 139 && b1 != 140) || !SameBytes(reader.ReadBytes(44), fme_signature))
                return info;
            info.is_package = b0 == 205;
            info.encrypt_files_name = b1 == 140;
            float version = reader.ReadSingle();
            if (version != 4f)
            {
                info.version = 3;
                return info;
            }
            info.version = 4;
            info.salt = reader.ReadBytes(16);
            info.iterations = reader.ReadInt32();
            info.index_iv = reader.ReadBytes(16);
            int index_length = reader.ReadInt32();
            byte[] index_mac = reader.ReadBytes(32);
            if (info.iterations < 1 || index_length < 0 || index_length > input.Length)
                throw new InvalidDataException("The file header is damaged.");
            byte[] stored_index = reader.ReadBytes(index_length);
            if (!info.encrypt_files_name)
                info.items = DeserializeIndex(stored_index);
            if (password == null)
                return info;
            DeriveKeys(password, info.salt, info.iterations, out info.encryption_key, out info.mac_key);
            byte[] header = new byte[fme_header_length - 32];
            input.Position = 0;
            input.Read(header, 0, header.Length);
            using (HMACSHA256 hmac = new HMACSHA256(info.mac_key))
            {
                hmac.TransformBlock(header, 0, header.Length, null, 0);
                hmac.TransformFinalBlock(stored_index, 0, stored_index.Length);
                info.password_is_correct = SameBytes(hmac.Hash, index_mac);
            }
            if (info.password_is_correct && info.encrypt_files_name)
                using (ICryptoTransform decryptor = CreateDecryptor(info.encryption_key, info.index_iv))
                    info.items = DeserializeIndex(decryptor.TransformFinalBlock(stored_index, 0, stored_index.Length));
            return info;
        }

        internal static List<string> ReadPlainNames(string path)
        {
            try
            {
                using (FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    FmeFileInfo info = ReadFmeHeader(input, null);
                    List<string> names = new List<string>();
                    if (info.version == 4 && info.items != null)
                        foreach (var i in info.items)
                            names.Add(i.name);
                    else if (info.version == 3 && !info.encrypt_files_name)
                    {
                        EncryptFileHeader header;
                        if (ReadHeader(new BinaryReader(input), Files_Encrepter.PreparePassword(Files_Encrepter.general_password, Files_Encrepter.general_secret_number, 128), null, out header) == 0)
                            foreach (var i in header.indices)
                                names.Add(i.file_name);
                    }
                    return names.Count > 0 ? names : null;
                }
            }
            catch (Exception) { return null; }
        }

        internal static List<FmeItem> ReadPackageContent(string path, string password, out string error)
        {
            error = null;
            try
            {
                using (FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    FmeFileInfo info = ReadFmeHeader(input, password);
                    if (info.version == 4)
                    {
                        if (!info.password_is_correct)
                            error = "Wrong password or the file is damaged.";
                        else
                        {
                            for (int i = 0; i < info.items.Count; i++)
                                info.items[i].id = i + 1;
                            return info.items;
                        }
                    }
                    else if (info.version == 3)
                    {
                        EncryptFileHeader header;
                        int result = ReadHeader(new BinaryReader(input), Files_Encrepter.PreparePassword(Files_Encrepter.general_password, Files_Encrepter.general_secret_number, 128), LegacyPassword(password), out header);
                        if (result == -1)
                            error = "Wrong password.";
                        else if (result == 0)
                        {
                            List<FmeItem> items = new List<FmeItem>();
                            foreach (var i in header.indices)
                                items.Add(new FmeItem() { id = i.id, name = i.file_name, plain_length = i.length });
                            return items;
                        }
                        else
                            error = "This is not a File Manager encrypted file.";
                    }
                    else if (info.version == 1)
                        error = "This file was encrypted by File Manager 0.8.1 and holds one file only, decrypt it from the 'DE-ENCRYPT FILES' tab.";
                    else
                        error = "This is not a File Manager encrypted file.";
                }
            }
            catch (Exception ex) { error = ex.Message; }
            return null;
        }

        internal void StartDecrept(List<FileInfo> files_list, string password, string save_in, bool package_in_directory, List<int> only_ids, bool show_dialog)
        {
            operation_type = OperationType.Decrypt;
            ProgressBrush = Brushes.LimeGreen;
            Title = operation.Text = string.Format("Decrypting {0} {1}", files_list.Count, files_list.Count > 1 ? "items" : "item");
            double max_value = 0;
            foreach (FileInfo i in files_list)
                try { max_value += i.Length; }
                catch (Exception) { }
            progress_bar.Maximum = max_value + 1;
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
                        catch (Exception) { }
                        Thread.Sleep(100);
                    }
                });
                process_thread.IsBackground = true;
                process_thread.Start();

                foreach (var file in files_list)
                {
                    if (CloseWindow) break;
                    current_file_name = file.FullName;
                    try
                    {
                        using (FileStream input = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            FmeFileInfo info = ReadFmeHeader(input, null);
                            string folder = save_in;
                            if (package_in_directory && info.is_package)
                            {
                                folder = UniqueFilePath(save_in, Path.GetFileNameWithoutExtension(file.Name));
                                Directory.CreateDirectory(folder);
                            }
                            if (info.version == 4)
                                DecryptVersion4(input, file, password, folder, only_ids);
                            else if (info.version == 3)
                                DecryptVersion3(input, file, password, folder, only_ids);
                            else if (info.version == 1)
                                DecryptLegacyFile(input, file, password, folder);
                            else
                                result_errors.Add(file.FullName + "\n    (this is not a File Manager encrypted file)");
                        }
                    }
                    catch (Exception ex)
                    { result_errors.Add(file.FullName + "\n    (" + ex.Message + ")"); }
                }
            };

            decrypt_files_BackgroundWorker.ProgressChanged += Destroy_ProgressChanged;

            decrypt_files_BackgroundWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                if (e.Error != null)
                    result_errors.Add(e.Error.Message);
                operation_finished = true;
                Close();
            };
            progress_bar.IsIndeterminate = false;
            decrypt_files_BackgroundWorker.RunWorkerAsync();
            if (show_dialog)
                ShowDialog();
            else
                Show();
        }

        void DecryptVersion4(FileStream input, FileInfo file, string password, string save_in, List<int> only_ids)
        {
            FmeFileInfo info = ReadFmeHeader(input, password);
            if (!info.password_is_correct)
            {
                result_errors.Add(file.FullName + "\n    (wrong password or the file is damaged, nothing was written)");
                return;
            }
            for (int i = 0; i < info.items.Count; i++)
            {
                if (CloseWindow) return;
                if (only_ids != null && !only_ids.Contains(i + 1))
                    continue;
                FmeItem item = info.items[i];
                if (!CheckItemMac(input, item, info.mac_key))
                {
                    if (!CloseWindow)
                        result_errors.Add(file.FullName + " : " + item.name + "\n    (the data is damaged, nothing was written)");
                    continue;
                }
                string target = UniqueFilePath(save_in, SafeFileName(item.name)), temp_path = target + ".partial";
                bool done = false;
                try
                {
                    using (SHA256 hasher = new SHA256Cng())
                    {
                        using (FileStream output = new FileStream(temp_path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        using (CryptoStream crypto = new CryptoStream(new HashStream(hasher, output), CreateDecryptor(info.encryption_key, item.iv), CryptoStreamMode.Write))
                        {
                            if (!CopyRegion(input, item.data_offset, item.data_length, crypto, true))
                                throw new OperationCanceledException();
                            crypto.FlushFinalBlock();
                        }
                        hasher.TransformFinalBlock(new byte[0], 0, 0);
                        if (!SameBytes(hasher.Hash, item.plain_hash))
                            throw new InvalidDataException("the decrypted data does not match the original");
                    }
                    File.Move(temp_path, target);
                    done = true;
                    completed_count++;
                    if (!string.Equals(Path.GetFileName(target), item.name, StringComparison.OrdinalIgnoreCase))
                        result_notes.Add(item.name + " -> " + target);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                { result_errors.Add(file.FullName + " : " + item.name + "\n    (" + ex.Message + ")"); }
                if (!done)
                    DeleteQuietly(temp_path);
            }
        }

        void DecryptVersion3(FileStream input, FileInfo file, string password, string save_in, List<int> only_ids)
        {
            byte[] general_decrypter = Files_Encrepter.PreparePassword(Files_Encrepter.general_password, Files_Encrepter.general_secret_number, 128);
            byte[] dec_password = LegacyPassword(password);
            BinaryReader reader = new BinaryReader(input);
            input.Position = 0;
            EncryptFileHeader header;
            switch (ReadHeader(reader, general_decrypter, dec_password, out header))
            {
                case 0:
                    foreach (var catalog in header.indices)
                    {
                        if (CloseWindow) return;
                        if (only_ids != null && !only_ids.Contains(catalog.id))
                            continue;
                        string target = UniqueFilePath(save_in, SafeFileName(catalog.file_name)), temp_path = target + ".partial";
                        string error = DecryptFile(temp_path, Files_Encrepter.PreparePassword(dec_password, header.secret_number, 128), reader, catalog);
                        if (error == null)
                            try
                            {
                                File.Move(temp_path, target);
                                completed_count++;
                                if (!string.Equals(Path.GetFileName(target), catalog.file_name, StringComparison.OrdinalIgnoreCase))
                                    result_notes.Add(catalog.file_name + " -> " + target);
                                continue;
                            }
                            catch (Exception ex) { error = ex.Message; }
                        DeleteQuietly(temp_path);
                        if (!CloseWindow)
                            result_errors.Add(file.FullName + " : " + catalog.file_name + "\n    (" + error + ")");
                    }
                    break;
                case -1:
                    result_errors.Add(file.FullName + "\n    (wrong password, nothing was written)");
                    break;
                default:
                    result_errors.Add(file.FullName + "\n    (this is not a File Manager encrypted file)");
                    break;
            }
        }

        string DecryptFile(string output_path, byte[] decryption_code, BinaryReader br, EncryptFileHeaderIndex catalog)
        {
            try
            {
                if (catalog.start_index < 0 || catalog.length < 0 || catalog.start_index + catalog.length > br.BaseStream.Length)
                    return "the file index is damaged";
                br.BaseStream.Position = catalog.start_index;
                using (FileStream output = new FileStream(output_path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    long position = 0;
                    while (position < catalog.length)
                    {
                        while (pause_copy && !CloseWindow)
                            Thread.Sleep(50);
                        if (CloseWindow)
                            return "stopped";
                        byte[] part = br.ReadBytes((int)Math.Min(Functions.buffer, catalog.length - position));
                        if (part.Length == 0)
                            return "the encrypted file is shorter than expected";
                        for (int i = 0; i < part.Length; i++)
                            part[i] = (byte)((byte)(part[i] + 1) ^ decryption_code[(position + i) % decryption_code.Length]);
                        output.Write(part, 0, part.Length);
                        position += part.Length;
                        progress_value += part.Length;
                    }
                }
                return null;
            }
            catch (Exception ex) { return ex.Message; }
        }

        static int ReadHeader(BinaryReader BR, byte[] general_code, byte[] user_password, out EncryptFileHeader header)
        {
            //1 is incurrect file
            //0 everything OK
            //-1 invalid Password
            bool is_package, encrypt_files_name;
            BR.BaseStream.Position = 0;
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
            byte[] file_bytes = BR.ReadBytes(44);
            if (!SameBytes(file_bytes, fme_signature))
            {
                header = null;
                return 1;
            }
            var ver = BR.ReadBytes(4);
            float version = BitConverter.ToSingle(ver, 0);
            var secret_number = Files_Encrepter.DecryptBytes(BR.ReadBytes(128), general_code);
            var file_password = BR.ReadBytes(128);
            if (user_password == null && encrypt_files_name)
            {
                header = null;
                return -1;
            }
            if (user_password == null || ComparePasswoeds(user_password, file_password))
            {
                var b2 = BR.ReadBytes(8);
                long first_file_index = BitConverter.ToInt64(b2, 0);
                var b3 = BR.ReadBytes(8);
                long first_file_length = BitConverter.ToInt64(b3, 0);
                var b4 = BR.ReadBytes(4);
                int files_count = BitConverter.ToInt32(b4, 0);
                if (files_count < 0 || files_count > 100000)
                {
                    header = null;
                    return 1;
                }
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

        static bool ComparePasswoeds(byte[] user_password, byte[] password_in_file)
        {
            user_password = Files_Encrepter.HashBytes(user_password, 128);
            for (int i = 0; i < 128; i++)
                if (user_password[i] != password_in_file[i])
                    return false;
            return true;
        }

        void DecryptLegacyFile(FileStream input, FileInfo file, string password, string save_in)
        {
            byte[] dec_password = LegacyPassword(password);
            input.Position = file_imprint.Length;
            byte[] random_numbers = new byte[16], secret_code = new byte[16];
            if (input.Read(random_numbers, 0, 16) != 16 || input.Read(secret_code, 0, 16) != 16)
            {
                result_errors.Add(file.FullName + "\n    (the file is damaged)");
                return;
            }
            for (int i = 0; i < 16; i++)
                if (secret_code[i] != (byte)(random_numbers[i] ^ dec_password[i]))
                {
                    result_errors.Add(file.FullName + "\n    (wrong password, nothing was written)");
                    return;
                }
            byte[] password_hash;
            using (MD5 md5 = new MD5Cng())
            using (SHA256 sha = new SHA256Cng())
                password_hash = sha.ComputeHash(md5.ComputeHash(dec_password));
            byte[] decrypt_phrase = new byte[Functions.buffer];
            for (int i = 0; i < decrypt_phrase.Length; i++)
                if (i % 3 == 0)
                    decrypt_phrase[i] = secret_code[(i / 3) % 16];
                else
                    decrypt_phrase[i] = password_hash[(i - (i / 3)) % 32];
            string name = file.Extension.ToLower().Equals(".fme") ? Path.GetFileNameWithoutExtension(file.Name) : file.Name;
            string target = UniqueFilePath(save_in, SafeFileName(name)), temp_path = target + ".partial";
            bool done = false;
            try
            {
                using (FileStream output = new FileStream(temp_path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    long position = 0;
                    byte[] buffer = new byte[Functions.buffer];
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        while (pause_copy && !CloseWindow)
                            Thread.Sleep(50);
                        if (CloseWindow)
                            throw new OperationCanceledException();
                        for (int i = 0; i < read; i++)
                            buffer[i] = (byte)((byte)(buffer[i] + 1) ^ decrypt_phrase[(position + i) % decrypt_phrase.Length]);
                        output.Write(buffer, 0, read);
                        position += read;
                        progress_value += read;
                    }
                }
                File.Move(temp_path, target);
                done = true;
                completed_count++;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            { result_errors.Add(file.FullName + "\n    (" + ex.Message + ")"); }
            if (!done)
                DeleteQuietly(temp_path);
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
            speed_graph.Visibility = speed_textblock.Visibility = complete_textblock.Visibility = remain.Visibility = remain_unit.Visibility = remain_title.Visibility = Visibility.Collapsed;
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

                ProcessStartInfo psi = new ProcessStartInfo(Functions.AppPath("Tools\\compress\\iso\\unisz.exe"), " \"" + file.FullName + "\" " + password);
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
                    operation_finished = true;
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

        void StopOperation()
        {
            CloseWindow = true;
            pause_copy = false;
        }

        private void Stop()
        {
            StopOperation();
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
        public bool is_cut, is_link;
        public List<string> unreadable;

        bool done;
        public Boolean Done
        {
            get { return done; }
            set
            {
                if (value && !done && parent != null)
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
                try
                {
                    if (file.Exists)
                        size = file.Length;
                }
                catch (Exception) { }
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
            is_link = directory != null && Functions.IsLink(directory);
            if (get_files && !is_link)
                GetSuns(directory, this);
            foreach (var i in children)
            {
                files_count += i.files_count;
                directory_count += i.directory_count;
            }
            error_message = error;
        }

        void AddUnreadable(string path, string message)
        {
            OperationTree top = this;
            while (top.parent != null)
                top = top.parent;
            lock (top)
            {
                if (top.unreadable == null)
                    top.unreadable = new List<string>();
                top.unreadable.Add(path + "\n    (" + message + ")");
            }
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
            catch (Exception ex) { AddUnreadable(directory.FullName, ex.Message); }
            try
            {
                var files = directory.GetFiles();
                foreach (var i in files)
                    try
                    { children.Add(new OperationTree(i, is_cut, short_path + "\\" + i.Name, operation, operationTree, "")); size += i.Length; }
                    catch (Exception ex) { AddUnreadable(i.FullName, ex.Message); }
            }
            catch (Exception ex) { AddUnreadable(directory.FullName, ex.Message); }
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

    class FmeItem
    {
        internal int id;
        internal string name;
        internal long plain_length, data_offset, data_length;
        internal byte[] iv, mac, plain_hash;
    }

    class FmeFileInfo
    {
        internal int version;
        internal bool is_package, encrypt_files_name, password_is_correct;
        internal byte[] salt, index_iv, encryption_key, mac_key;
        internal int iterations;
        internal List<FmeItem> items;
    }

    class HashStream : Stream
    {
        HashAlgorithm hasher;
        Stream inner;

        public HashStream(HashAlgorithm hasher)
        {
            this.hasher = hasher;
        }

        public HashStream(HashAlgorithm hasher, Stream inner)
        {
            this.hasher = hasher;
            this.inner = inner;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > 0)
                hasher.TransformBlock(buffer, offset, count, null, 0);
            if (inner != null)
                inner.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            if (inner != null)
                inner.Flush();
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }

    public enum OperationOption
    {
        NoValue = 0,
        Over = 1,
        Skip = 2,
        CopyWithNewName = 3,
        Retry = 4,
        Continue = 5
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