using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Renamer.xaml
    /// </summary>
    public partial class Renamer : Window
    {
        //// sort extention in suggester
        bool starting_window = true, CloseWindow = false, extension_warning = false;

        ExtentionChanger current_page = ExtentionChanger.ChangeExtention;
        ExtentionChanger CurrentPage
        {
            get { return current_page; }
            set
            {
                if (value != current_page)
                {
                    if (value == ExtentionChanger.ChangeExtention)
                    {
                        current_page = value;
                        TabChange_MouseButtonUp(label1, null);
                    }
                    else if (value == ExtentionChanger.ExtentionSuggester)
                    {
                        current_page = value;
                        TabChange_MouseButtonUp(label2, null);
                    }
                }
            }
        }

        Dictionary<string, bool> renamer_dictionary = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, bool> suggest_dictionary = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        List<RenamerListView> renamer_data = new List<RenamerListView>();
        List<RenamerListView> suggest_data = new List<RenamerListView>();
        List<string[]> last_renamed = new List<string[]>();
        ContextMenu settings_menu;
        MenuItem sub_folders, hidden_files, only_sure;
        const string default_extension_label = "Type new extension here (without any dots):";

        public Renamer()
        {
            InitializeComponent();
            SafetyTools.SetWindowIcon(this, "ExtensionChanger");
            MakeSettingsMenu();
        }

        void MakeSettingsMenu()
        {
            settings_menu = new ContextMenu();
            sub_folders = new MenuItem() { Header = "Add the files of sub-folders too", IsCheckable = true, IsChecked = Functions.ReadSetting("renamer.subfolders") != "0", ToolTip = "Used by 'Add folder' and when a folder is dropped on the list" };
            hidden_files = new MenuItem() { Header = "Add hidden and system files", IsCheckable = true, IsChecked = Functions.ReadSetting("renamer.hidden") == "1", ToolTip = "Used by 'Add folder' and when a folder is dropped on the list" };
            only_sure = new MenuItem() { Header = "SUGGEST EXT.: rename only sure suggestions", IsCheckable = true, IsChecked = Functions.ReadSetting("renamer.onlysure") == "1", ToolTip = "Files marked 'Likely' or 'Unsure' are skipped without asking (unless you chose their extension yourself)" };
            sub_folders.Click += (object sender, RoutedEventArgs e) => { Functions.SaveSetting("renamer.subfolders", sub_folders.IsChecked ? "1" : "0"); };
            hidden_files.Click += (object sender, RoutedEventArgs e) => { Functions.SaveSetting("renamer.hidden", hidden_files.IsChecked ? "1" : "0"); };
            only_sure.Click += (object sender, RoutedEventArgs e) => { Functions.SaveSetting("renamer.onlysure", only_sure.IsChecked ? "1" : "0"); };
            settings_menu.Items.Add(sub_folders);
            settings_menu.Items.Add(hidden_files);
            settings_menu.Items.Add(new Separator());
            settings_menu.Items.Add(only_sure);
        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            settings_menu.PlacementTarget = (UIElement)sender;
            settings_menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            settings_menu.IsOpen = true;
        }

        void BrowseForFiles(string title, List<RenamerListView> add_in, object items_count, Dictionary<string, bool> add_in_dictionary)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = title;
            sfd.AddExtension = false;
            sfd.MultiSelect = true;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                AddFiles(add_in, items_count, add_in_dictionary,sfd.SelectedItems);
        }

        void AddFiles(List<RenamerListView> add_in, object items_count, Dictionary<string, bool> add_in_dictionary, List<string> list)
        {
            List<string> folders = new List<string>();
            foreach (string i in list)
            {
                if (Directory.Exists(i))
                    folders.Add(i);
                else if (!add_in_dictionary.ContainsKey(i) && File.Exists(i))
                {
                    FileInfo file = new FileInfo(i);
                    if (Functions.IsLink(file))
                        continue;
                    add_in.Add(new RenamerListView(file));
                    add_in_dictionary.Add(i, true);
                }
            }
            RefreshList(add_in);
            foreach (var i in folders)
                AddDirectory(i, add_in, items_count, add_in_dictionary);
        }

        void RefreshList(List<RenamerListView> list)
        {
            if (list == renamer_data)
            {
                UpdatePreview();
                SetCounter(items_count, renamer_data.Count > 0 ? renamer_data.Count + (renamer_data.Count > 1 ? " Items" : " Item") : "");
            }
            else
            {
                sugester_list.Items.Refresh();
                SetCounter(items_count2, suggest_data.Count > 0 ? suggest_data.Count + (suggest_data.Count > 1 ? " Items" : " Item") : "");
                int detected = suggest_data.Count(x => x.Done && x.Confidence > 0), unknown = suggest_data.Count(x => x.Done && x.Confidence == 0);
                items_done.Text = detected > 0 ? "(" + detected + " detected" : "";
                items_and.Text = detected > 0 && unknown > 0 ? ", " : "";
                items_failed.Text = unknown > 0 ? (detected > 0 ? "" : "(") + unknown + " unknown)" : detected > 0 ? ")" : "";
            }
            UpdateButtons();
        }

        void AddDirectory(string path, List<RenamerListView> add_in, object items_count, Dictionary<string, bool> add_in_dictionary)
        {
            main_grid.IsEnabled = false;
            bool recursive = sub_folders.IsChecked, hidden = hidden_files.IsChecked;
            List<FileInfo> found = new List<FileInfo>();
            HashSet<string> known = new HashSet<string>(add_in_dictionary.Keys, StringComparer.OrdinalIgnoreCase);
            BackgroundWorker add_all_files_in_dir_bw = new BackgroundWorker();
            add_all_files_in_dir_bw.WorkerSupportsCancellation = true;
            add_all_files_in_dir_bw.DoWork += (object sender, DoWorkEventArgs e) => { GetAllFiles(new DirectoryInfo(path), known, found, recursive, hidden); };
            add_all_files_in_dir_bw.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                main_grid.IsEnabled = true;
                foreach (var i in found)
                    if (!add_in_dictionary.ContainsKey(i.FullName))
                        try
                        {
                            add_in.Add(new RenamerListView(i));
                            add_in_dictionary.Add(i.FullName, true);
                        }
                        catch (Exception) { }
                RefreshList(add_in);
                if (found.Count == 0)
                    Messages.ShowMessage("No files were added.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "'" + path + "' has no files" + (recursive ? "" : " (sub-folders are not included, see Settings)") + (hidden ? "." : " or only hidden files (see Settings)."));
            };
            add_all_files_in_dir_bw.RunWorkerAsync();
        }

        private void GetAllFiles(DirectoryInfo dir, HashSet<string> list, List<FileInfo> found, bool recursive, bool hidden)
        {
            DirectoryInfo[] sub_dirs = null;
            try { sub_dirs = dir.GetDirectories(); }
            catch (Exception) { }
            FileInfo[] files = null;
            try
            { files = dir.GetFiles(); }
            catch (Exception) { }
            if (files != null)
                foreach (var i in files)
                    if (!list.Contains(i.FullName) && (hidden || !IsHidden(i)) && !Functions.IsLink(i))
                    {
                        list.Add(i.FullName);
                        found.Add(i);
                    }
            if (sub_dirs == null || !recursive)
                return;
            else
                foreach (var i in sub_dirs)
                    if (!Functions.IsLink(i) && !CloseWindow && (hidden || !IsHidden(i)))
                        GetAllFiles(i, list, found, recursive, hidden);
        }

        static bool IsHidden(FileSystemInfo item)
        {
            try { return (item.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0; }
            catch (Exception) { return false; }
        }

        void SetCounter(object counter, string text)
        {
            if (counter is TextBlock)
                ((TextBlock)counter).Text = text;
            else if (counter is Run)
                ((Run)counter).Text = text;
        }

        void ClearList(Dictionary<string, bool> l1, List<RenamerListView> l2, object t)
        {
            l1.Clear();
            l2.Clear();
            RefreshList(l2);
        }

        private void RemoveItems(IList selected_list, List<RenamerListView> remove_from, Dictionary<string, bool> list1, object p)
        {
            for (int i = selected_list.Count - 1; i >= 0; i--)
            {
                list1.Remove((selected_list[i] as RenamerListView).FullPath);
                remove_from.Remove(selected_list[i] as RenamerListView);
            }
            RefreshList(remove_from);
        }

        private void FilesList_Drop(object sender, DragEventArgs e)
        {
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && (data = (string[])e.Data.GetData(DataFormats.FileDrop)).Length > 0)
            {
                if (sender == renamer_list)
                    AddFiles(renamer_data, items_count, renamer_dictionary, data.ToList());
                else
                    AddFiles(suggest_data, items_count2, suggest_dictionary, data.ToList());
            }
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtons();
        }

        private void List_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;
            if (sender == renamer_list)
                RemoveSelected_Click(null, null);
            else
                SuggesterRemoveSelected_Click(null, null);
        }

        void UpdateButtons()
        {
            SafetyTools.SetState(remove_button, renamer_list.SelectedItems.Count > 0, renamer_data.Count == 0 ? "The list is empty" : "Select files in the list first");
            SafetyTools.SetState(clear_button, renamer_data.Count > 0, "The list is empty");
            SafetyTools.SetState(rename_button, renamer_data.Count > 0, "Add files first");
            SafetyTools.SetState(remove_button2, sugester_list.SelectedItems.Count > 0, suggest_data.Count == 0 ? "The list is empty" : "Select files in the list first");
            SafetyTools.SetState(clear_button2, suggest_data.Count > 0, "The list is empty");
            SafetyTools.SetState(rename_button2, suggest_data.Count > 0, "Add files first");
            SafetyTools.SetState(suggest_button, suggest_data.Count > 0, "Add files first");
            string undo = last_renamed.Count == 0 ? "Nothing to undo: no files were renamed in this window yet" : null;
            SafetyTools.SetState(undo_button, undo == null, undo);
            SafetyTools.SetState(undo_button2, undo == null, undo);
            if (undo == null)
                undo_button.ToolTip = undo_button2.ToolTip = string.Format("Undo last rename: give the {0} file{1} of the last rename {2} old name{1} back", last_renamed.Count, last_renamed.Count == 1 ? "" : "s", last_renamed.Count == 1 ? "its" : "their");
        }

        static string NewName(FileInfo file, string extension, bool remove_old)
        {
            string name = file.Name, current = file.Extension;
            string stem = remove_old && current.Length < name.Length ? name.Substring(0, name.Length - current.Length) : name;
            if (extension.Length == 0)
                return remove_old && stem != name ? stem : null;
            if (!remove_old && current.Equals("." + extension, StringComparison.OrdinalIgnoreCase))
                return null;
            string result = stem + "." + extension;
            return result == name ? null : result;
        }

        void RunRename(List<FileOperationItem> to_rename)
        {
            List<string[]> expected = new List<string[]>();
            foreach (var i in to_rename)
                expected.Add(new string[] { i.Item.FullName, Path.Combine(Path.GetDirectoryName(i.Item.FullName), (string)i.short_path) });
            new Operation().CopyDialogShow(to_rename, OperationType.Rename, "", true);
            List<string[]> done = expected.Where(x => File.Exists(x[1]) && (!File.Exists(x[0]) || string.Equals(x[0], x[1], StringComparison.OrdinalIgnoreCase))).ToList();
            if (done.Count > 0)
                last_renamed = done;
            UpdateButtons();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (last_renamed.Count == 0)
                return;
            List<FileOperationItem> to_rename = new List<FileOperationItem>();
            List<string> missing = new List<string>();
            foreach (var i in last_renamed)
                if (File.Exists(i[1]) && (!File.Exists(i[0]) || string.Equals(i[0], i[1], StringComparison.OrdinalIgnoreCase)))
                    to_rename.Add(new FileOperationItem(new FileInfo(i[1]), false, Path.GetFileName(i[0])));
                else
                    missing.Add(i[1] + (File.Exists(i[0]) ? "  (a file with the old name exists again)" : "  (not found)"));
            if (to_rename.Count == 0)
            {
                Messages.ShowMessage("There is nothing to undo.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", missing.Take(20).ToArray()));
                last_renamed.Clear();
                UpdateButtons();
                return;
            }
            StringBuilder details = new StringBuilder();
            foreach (var i in to_rename.Take(10))
                details.AppendLine(i.Item.Name + "  ->  " + i.short_path);
            if (to_rename.Count > 10)
                details.AppendLine(string.Format("... and {0} more.", to_rename.Count - 10));
            if (missing.Count > 0)
                details.AppendLine(string.Format("\n{0} file(s) can not be changed back:\n", missing.Count) + string.Join("\n", missing.Take(10).ToArray()));
            if (Messages.ShowMessage(string.Format("Give {0} file{1} {2} old name{1} back?", to_rename.Count, to_rename.Count == 1 ? "" : "s", to_rename.Count == 1 ? "its" : "their"), ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, details.ToString()) != MyMessageBoxResult.Yes)
                return;
            new Operation().CopyDialogShow(to_rename, OperationType.Rename, "", true);
            last_renamed.Clear();
            foreach (var i in renamer_data.Concat(suggest_data))
                i.File.Refresh();
            UpdatePreview();
            UpdateButtons();
        }

        void RemoveRenamed(List<RenamerListView> list, Dictionary<string, bool> dictionary)
        {
            foreach (var i in list.ToList())
            {
                i.File.Refresh();
                if (!i.File.Exists)
                {
                    list.Remove(i);
                    dictionary.Remove(i.FullPath);
                }
            }
            RefreshList(list);
        }


        #region Extension Changer

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFiles("Select files to change their extensions...", renamer_data, items_count, renamer_dictionary);
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Add all files in this folder";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                AddDirectory(sfd.SelectedItem, renamer_data, items_count, renamer_dictionary);
            }
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            ClearList(renamer_dictionary, renamer_data, items_count);
        }

        void UpdatePreview()
        {
            if (extension_label == null)
                return;
            string ext = extension.Text.Trim();
            bool remove = remove_extension.IsChecked == true;
            foreach (var i in renamer_data)
            {
                string name = NewName(i.File, ext, remove);
                if (name != null)
                {
                    i.NewName = name;
                    i.NewNameColor = Brushes.Black;
                }
                else
                {
                    i.NewName = ext.Length == 0 && !remove ? "type an extension" : ext.Length == 0 ? "(no extension to remove)" : "(no change, already ." + ext + ")";
                    i.NewNameColor = Brushes.Gray;
                }
            }
            renamer_list.Items.Refresh();
        }

        private void RemoveExtension_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (renamer_data.Count > 0)
            {
                string ext = extension.Text.Trim();
                bool remove = remove_extension.IsChecked.Value;
                if (ext.Length == 0 && !remove)
                {
                    Messages.ShowMessage("Extension field seems empty.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please enter an extension first or tick 'Remove old extension'.");
                    return;
                }
                List<FileOperationItem> to_rename = new List<FileOperationItem>();
                StringBuilder details = new StringBuilder();
                int unchanged = 0;
                foreach (var i in renamer_data)
                {
                    i.File.Refresh();
                    string name = i.File.Exists ? NewName(i.File, ext, remove) : null;
                    if (name == null)
                    {
                        unchanged++;
                        continue;
                    }
                    if (to_rename.Count < 10)
                        details.AppendLine(i.File.Name + "  ->  " + name);
                    to_rename.Add(new FileOperationItem(i.File, false, name));
                }
                if (to_rename.Count == 0)
                {
                    Messages.ShowMessage("No file needs a new name.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, ext.Length == 0 ? "The files have no extension to remove." : "Every file already has the extension '." + ext + "' (or is missing).");
                    return;
                }
                if (to_rename.Count > 10)
                    details.AppendLine(string.Format("... and {0} more.", to_rename.Count - 10));
                if (unchanged > 0)
                    details.AppendLine(string.Format("\n{0} file{1} stay{2} as {3} (already '.{4}' or missing).", unchanged, unchanged == 1 ? "" : "s", unchanged == 1 ? "s" : "", unchanged == 1 ? "it is" : "they are", ext));
                string question = ext.Length == 0 ? string.Format("Remove the extension of {0} file{1}?", to_rename.Count, to_rename.Count == 1 ? "" : "s") : string.Format("Rename {0} file{1}?", to_rename.Count, to_rename.Count == 1 ? "" : "s");
                if (Messages.ShowMessage(question, ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, details.ToString(), "Changing an extension does not convert a file. You can undo this rename with 'Undo last rename'.") != MyMessageBoxResult.Yes)
                    return;
                if (!Functions.ConfirmRiskyItems(to_rename.Select(x => x.Item.FullName), "rename"))
                    return;
                RunRename(to_rename);
                RemoveRenamed(renamer_data, renamer_dictionary);
            }
            else
                Messages.ShowMessage("List is empty!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "First you have to add files then click 'Rename'");
        }

        private void Extension_TextChanged(object sender, TextChangedEventArgs e)
        {
            var InvalidChars = Path.GetInvalidFileNameChars();
            string text = extension.Text, clean = new string(text.Where(x => x != '.' && !InvalidChars.Contains(x) && !char.IsWhiteSpace(x)).ToArray());
            if (clean != text)
            {
                if (text.TrimStart('.') != clean)
                    extension_warning = true;
                extension.Text = clean;
                extension.CaretIndex = clean.Length;
                return;
            }
            if (extension_label != null)
            {
                extension_label.Text = extension_warning ? "An extension can not contain dots, spaces or \\ / : * ? \" < > |" : default_extension_label;
                extension_label.Foreground = extension_warning ? Brushes.Red : Brushes.Blue;
            }
            extension_warning = false;
            UpdatePreview();
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            RemoveItems(renamer_list.SelectedItems, renamer_data, renamer_dictionary, items_count);
        }

        #endregion


        #region Suggest best possible extension

        private void SuggesterAddFile_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFiles("Select files...", suggest_data, items_count2, suggest_dictionary);
        }

        private void SuggesterAddFolder_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Add all files in this folder";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                AddDirectory(sfd.SelectedItem, suggest_data, items_count2, suggest_dictionary);
            }
        }

        private void SuggesterRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            RemoveItems(sugester_list.SelectedItems, suggest_data, suggest_dictionary, items_count2);
        }

        private void SuggesterClearList_Click(object sender, RoutedEventArgs e)
        {
            ClearList(suggest_dictionary, suggest_data, items_count2);
        }

        private void SuggesterRename_Click(object sender, RoutedEventArgs e)
        {
            if (suggest_data.Count > 0)
                Suggest_Click(null, null);
            else
                Messages.ShowMessage("List is empty!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "First you have to add files then click 'Rename'");
        }

        private void Suggest_Click(object sender, RoutedEventArgs e)
        {
            if (suggest_data.Count == 0)
                return;
            BackgroundWorker suggest_bw = new BackgroundWorker();
            suggest_bw.DoWork += SuggestBW_DoWork;
            suggest_bw.ProgressChanged += SuggestBW_ProgressChanged;
            suggest_bw.WorkerSupportsCancellation = suggest_bw.WorkerReportsProgress = true;
            if (sender == null)
                suggest_bw.RunWorkerCompleted += SuggestAndRename_bw_RunWorkerCompleted;
            else
                suggest_bw.RunWorkerCompleted += SuggestBW_RunWorkerCompleted;
            main_grid.IsEnabled = false;
            rename2_progressbar.Value = 0;
            rename2_progressbar.Visibility = Visibility.Visible;
            suggest_bw.RunWorkerAsync(suggest_data.ToList());
        }

        private void SuggestBW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker this_bw = ((BackgroundWorker)sender);
            List<RenamerListView> list = (List<RenamerListView>)e.Argument;
            ParallelOptions options = new ParallelOptions() { MaxDegreeOfParallelism = Math.Max(1, Math.Min(4, Environment.ProcessorCount)) };
            Parallel.For(0, list.Count, options, (i) =>//for (int i = 0; i < list_to_rename2.Count; i++)
            {
                RenamerListView item = list[i];
                if (!item.Done && !CloseWindow)
                {
                    DetectedType type = null;
                    try
                    {
                        item.File.Refresh();
                        if (item.File.Exists)
                            type = FileTypeDetector.Detect(item.File.FullName);
                        if (type == null && item.File.Exists)
                            type = SignatureTable(item.File);
                    }
                    catch (Exception ex) { type = new DetectedType() { Name = "Can not be read: " + ex.Message, Extensions = new string[0], Confidence = 0 }; }
                    if (type == null)
                        type = new DetectedType() { Name = item.File.Exists ? "Unknown" : "File not found", Extensions = new string[0], Confidence = 0 };
                    item.SetDetected(type);
                }
                this_bw.ReportProgress(-1);
            });
        }

        DetectedType SignatureTable(FileInfo file)
        {
            List<byte> file_bytes = new List<byte>();
            List<string> found = new List<string>();
            string name = null;
            int best = 0;
            using (BinaryReader BR = new BinaryReader(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)))
                foreach (var j in FileInfoCombonents.FilesInformation)
                    foreach (var l in j.Value.IDs)
                        if (file.Length >= l.Length + j.Value.Offset)
                        {
                            try
                            {
                                int k;
                                while (l.Length + j.Value.Offset > file_bytes.Count)
                                    file_bytes.Add(BR.ReadByte());
                                for (k = 0; k < l.Length; k++)
                                    if (l[k] != file_bytes[k + j.Value.Offset])
                                        break;
                                if (k == l.Length)
                                {
                                    string ext = j.Value.Extention.TrimStart('.').ToLower();
                                    if (!found.Contains(ext))
                                        found.Add(ext);
                                    if (l.Length > best)
                                    {
                                        best = l.Length;
                                        name = j.Value.Description;
                                    }
                                    break;
                                }
                            }
                            catch (Exception) { }
                        }
            if (found.Count == 0)
                return null;
            return new DetectedType() { Name = (name ?? "").Trim(), Extensions = found.ToArray(), Confidence = best >= 4 ? 2 : 1 };
        }

        private void SuggestBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (suggest_data.Count > 0)
                rename2_progressbar.Value += 1000.0 / suggest_data.Count;
        }

        private void SuggestBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var i in suggest_data)
                i.Done = i.Done || i.TypeName != null;
            rename2_progressbar.Visibility = Visibility.Hidden;
            main_grid.IsEnabled = true;
            RefreshList(suggest_data);
        }

        private void SuggestAndRename_bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SuggestBW_RunWorkerCompleted(null, null);
            bool remove = remove_extension2.IsChecked.Value;
            List<RenamerListView> sure = new List<RenamerListView>(), unsure = new List<RenamerListView>();
            int unchanged = 0;
            foreach (var i in suggest_data)
            {
                string name = i.ChosenExtension == null || !i.File.Exists || (remove && i.File.Extension.Equals("." + i.ChosenExtension, StringComparison.OrdinalIgnoreCase)) ? null : NewName(i.File, i.ChosenExtension, remove);
                if (name == null)
                {
                    unchanged++;
                    continue;
                }
                if (i.Confidence >= 3 || i.SelectedExt != i.AutoExt)
                    sure.Add(i);
                else
                    unsure.Add(i);
            }
            if (unsure.Count > 0)
            {
                if (only_sure.IsChecked)
                    unchanged += unsure.Count;
                else
                {
                    StringBuilder list = new StringBuilder();
                    foreach (var i in unsure.Take(12))
                        list.AppendLine(i.File.Name + "  ->  ." + i.ChosenExtension + "   (" + i.ConfidenceText.ToLower() + ": " + i.TypeName + ")");
                    if (unsure.Count > 12)
                        list.AppendLine(string.Format("... and {0} more.", unsure.Count - 12));
                    switch (Messages.ShowMessage(string.Format("{0} file{1} {2} not surely detected. Rename {3} too?", unsure.Count, unsure.Count == 1 ? "" : "s", unsure.Count == 1 ? "was" : "were", unsure.Count == 1 ? "it" : "them"), list.ToString(), new ButtonContent("Rename them too", ModrenMessageBitmapImage.Default, 1), new ButtonContent("Skip them", ModrenMessageBitmapImage.Skip, 2), new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 3)))
                    {
                        case MyMessageBoxResult.Button1:
                            sure.AddRange(unsure);
                            break;
                        case MyMessageBoxResult.Button2:
                            unchanged += unsure.Count;
                            break;
                        default:
                            return;
                    }
                }
            }
            if (sure.Count == 0)
            {
                Messages.ShowMessage("No file needs a new name.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "Every file already has the right extension, its type is unknown, or it was skipped.");
                return;
            }
            List<FileOperationItem> to_rename = new List<FileOperationItem>();
            StringBuilder details = new StringBuilder();
            foreach (var i in sure)
            {
                string name = NewName(i.File, i.ChosenExtension, remove);
                if (to_rename.Count < 10)
                    details.AppendLine(i.File.Name + "  ->  " + name);
                to_rename.Add(new FileOperationItem(i.File, false, name));
            }
            if (to_rename.Count > 10)
                details.AppendLine(string.Format("... and {0} more.", to_rename.Count - 10));
            if (unchanged > 0)
                details.AppendLine(string.Format("\n{0} file{1} keep{2} {3} name (right extension, unknown type or skipped).", unchanged, unchanged == 1 ? "" : "s", unchanged == 1 ? "s" : "", unchanged == 1 ? "its" : "their"));
            if (Messages.ShowMessage(string.Format("Rename {0} file{1}?", to_rename.Count, to_rename.Count == 1 ? "" : "s"), ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, details.ToString(), "Changing an extension does not convert a file. You can undo this rename with 'Undo last rename'.") != MyMessageBoxResult.Yes)
                return;
            if (!Functions.ConfirmRiskyItems(to_rename.Select(x => x.Item.FullName), "rename"))
                return;
            RunRename(to_rename);
            RemoveRenamed(suggest_data, suggest_dictionary);
        }

        #endregion

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
            CurrentPage = (ExtentionChanger)tab_panal.Children.IndexOf((Label)sender) + 1;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!starting_window)
            {
                double t = (-1 * ((int)CurrentPage - 1)) * e.NewSize.Width;
                ThicknessAnimation ta = new ThicknessAnimation(new Thickness(t, 0, 0, 0), new Duration(new TimeSpan(0)));
                mother.BeginAnimation(MarginProperty, ta);
                for (int i = 0; i < mother.Children.Count; i++)
                {
                    ((Grid)mother.Children[i]).Width = ActualWidth - 5;
                    ((Grid)mother.Children[i]).Margin = new Thickness(i * e.NewSize.Width, 40, 0, 0);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            starting_window = false;
            rec.Width = CurrentPage == ExtentionChanger.ChangeExtention ? label1.ActualWidth : label2.ActualWidth;
            ThicknessAnimation ta2 = new ThicknessAnimation(new Thickness(CurrentPage == ExtentionChanger.ChangeExtention ? 0 : label1.ActualWidth, 32, 0, 0), new Duration(new TimeSpan(2000000)));
            rec.BeginAnimation(MarginProperty, ta2);
            ThicknessAnimation ta = new ThicknessAnimation(new Thickness(CurrentPage == ExtentionChanger.ChangeExtention ? 0 : -1 * ActualWidth, 0, 0, 0), new Duration(new TimeSpan(2000000)));
            mother.BeginAnimation(MarginProperty, ta);
            renamer_list.DataContext = renamer_data;
            sugester_list.DataContext = suggest_data;
            for (int i = 0; i < mother.Children.Count; i++)
            {
                ((Grid)mother.Children[i]).Width = ActualWidth - 5;
                ((Grid)mother.Children[i]).Margin = new Thickness(i * ActualWidth, 40, 0, 0);
            }
            UpdateButtons();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        private void AboutTool_Click(object sender, RoutedEventArgs e)
        {
            //viresion 1:
            //base tool to change files extension
            //
            //1.1:
            //impove speed
            //fix bug
            //
            //1.2:
            //add extension sugester
            //
            //2:
            //new interface
            //impove speed
            //
            //2.1:
            //drug and drop files
            AboutTool window = new AboutTool();
            window.tool_vertion.Text = "Version: 2.1";
            window.tool_date.Text = "";
            window.tool_name.Text = "Super Extension Changer";
            window.tool_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/extension-changer.png"));
            window.Owner = this;
            window.ShowDialog();
        }

        internal bool ShowWindow(List<string> files,string Extention, ExtentionChanger type, bool showDialog)
        {
            CurrentPage = type;
            if (type == ExtentionChanger.ChangeExtention)
            {
                AddFiles(renamer_data, items_count, renamer_dictionary, files);
                extension.Text = Extention;
            }
            else
            {
                AddFiles(suggest_data, items_count2, suggest_dictionary, files);
                Suggest_Click(suggest_button, null);
            }

            if (showDialog)
                ShowDialog();
            else
                Functions.ShowTool(this);
            return true;
        }
    }

    class RenamerListView : GeneralPurpose
    {
        public RenamerListView(FileInfo file)
        {
            FullPath = file.FullName;
            Image = Functions.GetFileImage24(file.Extension);
            File = file;
            possible_ext = new List<string>();
            SelectedExt = -1;
            AutoExt = -1;
            done = false;
        }

        internal void SetDetected(DetectedType type)
        {
            List<string> list = new List<string>();
            foreach (var i in type.Extensions)
                if (!list.Contains("." + i))
                    list.Add("." + i);
            string current = File.Extension.ToLower();
            bool fits = current.Length > 1 && list.Contains(current);
            if (!fits && current.Length > 1 && type.Family != null && FileTypeDetector.FitsFamily(type.Family, current.Substring(1)))
            {
                list.Insert(0, current);
                fits = true;
            }
            if (fits)
            {
                list.Remove(current);
                list.Insert(0, current);
            }
            TypeName = type.Name;
            Confidence = list.Count == 0 ? 0 : type.Confidence;
            if (list.Count == 0)
                list.Add("none");
            ConfidenceText = Confidence >= 3 ? "Sure" : Confidence == 2 ? "Likely" : Confidence == 1 ? "Unsure" : "-";
            ConfidenceColor = Confidence >= 3 ? Brushes.Green : Confidence == 2 ? Brushes.DarkOrange : Confidence == 1 ? Brushes.Red : Brushes.Gray;
            ConfidenceTip = Confidence == 0 ? "The type of this file is unknown: it is not renamed" : (fits ? "The current extension '" + current + "' fits this type: the file keeps its name" : Confidence >= 3 ? "Sure: the content matches this type" : Confidence == 2 ? "Likely: a short signature or a text format, check it before renaming" : "Unsure: only a guess, you are asked before it is renamed");
            PossibleExt = list;
            SelectedExt = AutoExt = 0;
        }

        internal string ChosenExtension
        {
            get
            {
                if (!Done || Confidence == 0 || SelectedExt < 0 || SelectedExt >= PossibleExt.Count || PossibleExt[SelectedExt] == "none")
                    return null;
                return PossibleExt[SelectedExt].TrimStart('.');
            }
        }

        public string NewName { get; set; }
        public Brush NewNameColor { get; set; }
        public string TypeName { get; set; }
        public string ConfidenceText { get; set; }
        public Brush ConfidenceColor { get; set; }
        public string ConfidenceTip { get; set; }
        public int Confidence;
        public int AutoExt;

        List<string> possible_ext;
        public List<string> PossibleExt
        {
            get { return possible_ext; }
            set { possible_ext = value; }
        }

        int selected_ext;
        public int SelectedExt
        {
            get { return selected_ext; }
            set { selected_ext = value; }
        }

        bool done;
        public Boolean Done
        {
            get { return done; }
            set { done = value; }
        }
    }

    enum ExtentionChanger
    {
        ChangeExtention = 1,
        ExtentionSuggester = 2
    }

    class DetectedType
    {
        public string Name, Family;
        public string[] Extensions;
        public int Confidence;
    }

    static class FileTypeDetector
    {
        const int Sure = 3, Likely = 2, Unsure = 1;

        static string[] text_extensions = { "txt", "text", "log", "md", "markdown", "rst", "csv", "tsv", "ini", "cfg", "conf", "config", "inf", "reg", "xml", "xaml", "xsd", "xsl", "xslt", "svg", "html", "htm", "xhtml", "mht", "php", "asp", "aspx", "cshtml", "jsp", "css", "scss", "less", "js", "mjs", "cjs", "ts", "tsx", "jsx", "json", "jsonc", "geojson", "webmanifest", "yml", "yaml", "toml", "cs", "vb", "fs", "c", "h", "cpp", "hpp", "cc", "cxx", "java", "kt", "py", "pyw", "rb", "go", "rs", "swift", "m", "pl", "pm", "lua", "r", "sql", "sh", "bash", "bat", "cmd", "ps1", "psm1", "psd1", "vbs", "wsf", "csproj", "vbproj", "vcxproj", "sln", "props", "targets", "resx", "settings", "manifest", "nuspec", "srt", "vtt", "ass", "ssa", "sub", "lrc", "m3u", "m3u8", "pls", "cue", "nfo", "diz", "tex", "bib", "rtf", "eml", "vcf", "ics", "gitignore", "gitattributes", "editorconfig", "env", "properties", "gradle", "plist", "kml", "gpx", "rss", "atom", "opml", "dtd", "wsdl", "url", "desktop", "lst", "dat", "asm", "s", "pas", "dpr", "bas", "frm", "cls", "d", "dart", "scala", "groovy", "hs", "erl", "ex", "exs", "clj", "lisp", "el", "vim", "awk", "sed", "mak", "makefile", "cmake", "in", "am", "ac", "patch", "diff" };
        static string[] zip_extensions = { "zip", "jar", "war", "ear", "apk", "aab", "xapk", "ipa", "xpi", "crx", "vsix", "nupkg", "appx", "msix", "appxbundle", "msixbundle", "cbz", "kmz", "3mf", "epub", "docx", "docm", "dotx", "dotm", "xlsx", "xlsm", "xltx", "xltm", "xlsb", "pptx", "pptm", "ppsx", "ppsm", "potx", "potm", "vsdx", "odt", "ods", "odp", "odg", "ott", "ots", "otp", "xps", "oxps", "sketch", "xd", "whl", "egg", "aar", "zipx", "mcpack", "mcworld", "pak", "pk3", "pk4", "ora", "kra", "ggb", "sb3", "usdz" };
        static string[] ole_extensions = { "doc", "dot", "xls", "xlt", "xla", "ppt", "pps", "pot", "msg", "msi", "msp", "mst", "vsd", "vss", "vst", "pub", "mpp", "db", "wps", "wpt", "sldprt", "max", "suo", "ost", "oft" };

        internal static bool FitsFamily(string family, string extension)
        {
            extension = extension.ToLower();
            switch (family)
            {
                case "text":
                    if (text_extensions.Contains(extension))
                        return true;
                    try
                    {
                        FileCategory category = Functions.GetFileCategory("." + extension);
                        return category == FileCategory.Text || category == FileCategory.Html || category == FileCategory.TextCanRunAsApplication;
                    }
                    catch (Exception) { return false; }
                case "zip":
                    return zip_extensions.Contains(extension);
                case "ole":
                    return ole_extensions.Contains(extension);
            }
            return false;
        }

        static DetectedType T(string name, int confidence, params string[] extensions)
        {
            return new DetectedType() { Name = name, Confidence = confidence, Extensions = extensions };
        }

        static DetectedType F(string family, string name, int confidence, params string[] extensions)
        {
            return new DetectedType() { Name = name, Confidence = confidence, Extensions = extensions, Family = family };
        }

        static byte[] ReadAt(Stream stream, long offset, int count)
        {
            if (offset < 0 || offset >= stream.Length || count <= 0)
                return new byte[0];
            stream.Position = offset;
            byte[] buffer = new byte[(int)Math.Min(count, stream.Length - offset)];
            int read = 0;
            while (read < buffer.Length)
            {
                int n = stream.Read(buffer, read, buffer.Length - read);
                if (n <= 0)
                    break;
                read += n;
            }
            if (read < buffer.Length)
                Array.Resize(ref buffer, read);
            return buffer;
        }

        static bool Has(byte[] data, int offset, params byte[] signature)
        {
            if (offset < 0 || data.Length < offset + signature.Length)
                return false;
            for (int i = 0; i < signature.Length; i++)
                if (data[offset + i] != signature[i])
                    return false;
            return true;
        }

        static bool HasText(byte[] data, int offset, string text)
        {
            if (offset < 0 || data.Length < offset + text.Length)
                return false;
            for (int i = 0; i < text.Length; i++)
                if (data[offset + i] != (byte)text[i])
                    return false;
            return true;
        }

        static string Ascii(byte[] data, int offset, int count)
        {
            if (offset < 0 || data.Length < offset + count)
                return "";
            return Encoding.ASCII.GetString(data, offset, count);
        }

        static int U16(byte[] data, int offset)
        {
            return data.Length < offset + 2 ? 0 : data[offset] | (data[offset + 1] << 8);
        }

        static long U32(byte[] data, int offset)
        {
            return data.Length < offset + 4 ? 0 : (long)(uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        }

        static int Find(byte[] data, string text, int start, int end)
        {
            byte[] pattern = Encoding.ASCII.GetBytes(text);
            end = Math.Min(end, data.Length - pattern.Length);
            for (int i = Math.Max(0, start); i <= end; i++)
                if (Has(data, i, pattern))
                    return i;
            return -1;
        }

        internal static DetectedType Detect(string path)
        {
            byte[] head;
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                long length = stream.Length;
                if (length == 0)
                    return T("Empty file (0 bytes)", 0);
                head = ReadAt(stream, 0, 65536);
                DetectedType found = Binary(stream, head, length);
                if (found != null)
                    return found;
            }
            return Text(head);
        }

        static DetectedType Binary(Stream stream, byte[] h, long length)
        {
            if (Has(h, 0, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A))
                return T("PNG image", Sure, "png");
            if (Has(h, 0, 0xFF, 0xD8, 0xFF))
                return T("JPEG image", Sure, "jpg", "jpeg", "jfif", "jpe");
            if (HasText(h, 0, "GIF87a") || HasText(h, 0, "GIF89a"))
                return T("GIF image", Sure, "gif");
            if (HasText(h, 0, "8BPS"))
                return T("Photoshop image", Sure, "psd", "psb");
            if (HasText(h, 0, "RIFF") && h.Length >= 12)
            {
                switch (Ascii(h, 8, 4))
                {
                    case "WAVE":
                        return T("WAV audio", Sure, "wav");
                    case "AVI ":
                        return T("AVI video", Sure, "avi");
                    case "WEBP":
                        return T("WebP image", Sure, "webp");
                    case "RMID":
                        return T("MIDI music", Sure, "rmi", "mid");
                    case "ACON":
                        return T("Animated cursor", Sure, "ani");
                    case "CDXA":
                        return T("Video CD track", Likely, "dat", "mpg");
                }
                return null;
            }
            if (HasText(h, 0, "AT&TFORM"))
                return T("DjVu document", Sure, "djvu", "djv");
            if (HasText(h, 0, "FORM") && h.Length >= 12)
            {
                string form = Ascii(h, 8, 4);
                if (form == "AIFF")
                    return T("AIFF audio", Sure, "aiff", "aif");
                if (form == "AIFC")
                    return T("AIFF-C audio", Sure, "aifc", "aiff");
            }
            if (HasText(h, 0, "fLaC"))
                return T("FLAC audio", Sure, "flac");
            if (HasText(h, 0, "OggS"))
            {
                if (Find(h, "OpusHead", 0, 80) >= 0)
                    return T("Opus audio", Sure, "opus", "ogg");
                if (Find(h, "vorbis", 0, 80) >= 0)
                    return T("Ogg Vorbis audio", Sure, "ogg", "oga");
                if (Find(h, "theora", 0, 80) >= 0)
                    return T("Ogg video", Sure, "ogv", "ogg");
                if (Find(h, "FLAC", 0, 80) >= 0)
                    return T("Ogg FLAC audio", Sure, "oga", "ogg");
                return T("Ogg media", Likely, "ogg", "ogx");
            }
            if (HasText(h, 0, "ID3"))
                return T("MP3 audio", Likely, "mp3");
            if (HasText(h, 0, "MThd"))
                return T("MIDI music", Sure, "mid", "midi");
            if (HasText(h, 0, "#!AMR"))
                return T("AMR audio", Sure, "amr");
            if (HasText(h, 0, "MAC "))
                return T("Monkey's Audio", Likely, "ape");
            if (HasText(h, 0, "wvpk"))
                return T("WavPack audio", Sure, "wv");
            if (h.Length >= 12 && HasText(h, 4, "ftyp"))
                return IsoMedia(h);
            if (Has(h, 0, 0x00, 0x00, 0x00, 0x0C, 0x6A, 0x50, 0x20, 0x20))
                return T("JPEG 2000 image", Sure, "jp2");
            if (Has(h, 0, 0x00, 0x00, 0x00, 0x0C, 0x4A, 0x58, 0x4C, 0x20) || Has(h, 0, 0xFF, 0x0A))
                return T("JPEG XL image", Has(h, 0, 0xFF, 0x0A) ? Unsure : Sure, "jxl");
            if (Has(h, 0, 0x1A, 0x45, 0xDF, 0xA3))
            {
                if (Find(h, "webm", 0, 64) >= 0)
                    return T("WebM video", Sure, "webm");
                if (Find(h, "matroska", 0, 64) >= 0)
                    return T("Matroska video", Sure, "mkv", "mka", "mk3d");
                return T("Matroska media", Likely, "mkv");
            }
            if (HasText(h, 0, "FLV") && h.Length > 3 && h[3] == 1)
                return T("Flash video", Sure, "flv");
            if (Has(h, 0, 0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11))
                return T("Windows Media file", Likely, "wmv", "wma", "asf");
            if (Has(h, 0, 0x00, 0x00, 0x01, 0xBA))
                return T("MPEG video", Likely, "mpg", "mpeg", "vob");
            if (Has(h, 0, 0x00, 0x00, 0x01, 0xB3))
                return T("MPEG video", Likely, "mpg", "mpeg", "m1v");
            if (h.Length > 376 && h[0] == 0x47 && h[188] == 0x47 && h[376] == 0x47)
                return T("MPEG transport stream", Likely, "ts", "mts", "m2ts");
            if (HasText(h, 0, ".RMF"))
                return T("RealMedia file", Sure, "rm", "rmvb");
            if (HasText(h, 0, "%PDF-"))
                return T("PDF document", Sure, "pdf");
            if (HasText(h, 0, "{\\rtf"))
                return T("Rich Text document", Sure, "rtf");
            if (HasText(h, 0, "%!PS"))
            {
                int line = Find(h, "\n", 0, 200);
                if (Find(h, "EPSF", 0, line < 0 ? 200 : line) >= 0)
                    return T("Encapsulated PostScript", Sure, "eps");
                return T("PostScript document", Sure, "ps");
            }
            if (Has(h, 0, 0xC5, 0xD0, 0xD3, 0xC6))
                return T("Encapsulated PostScript", Sure, "eps");
            if (HasText(h, 0, "PK\x03\x04") || HasText(h, 0, "PK\x05\x06") || HasText(h, 0, "PK\x07\x08"))
                return Zip(stream, h, length);
            if (HasText(h, 0, "Rar!\x1A\x07"))
                return T("RAR archive", Sure, "rar");
            if (Has(h, 0, 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C))
                return T("7-Zip archive", Sure, "7z");
            if (Has(h, 0, 0x1F, 0x8B, 0x08))
                return Gzip(stream);
            if (HasText(h, 0, "BZh") && h.Length > 3 && h[3] >= (byte)'1' && h[3] <= (byte)'9')
                return T("bzip2 compressed file", Sure, "bz2", "bzip2", "tbz2");
            if (Has(h, 0, 0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00))
                return T("XZ compressed file", Sure, "xz", "txz");
            if (Has(h, 0, 0x28, 0xB5, 0x2F, 0xFD))
                return T("Zstandard compressed file", Sure, "zst");
            if (Has(h, 0, 0x04, 0x22, 0x4D, 0x18))
                return T("LZ4 compressed file", Sure, "lz4");
            if (HasText(h, 0, "LZIP"))
                return T("Lzip compressed file", Sure, "lz");
            if (HasText(h, 0, "MSCF"))
                return T("Cabinet archive", Sure, "cab");
            if (HasText(h, 257, "ustar"))
                return T("TAR archive", Sure, "tar");
            if (HasText(h, 32769, "CD001") || HasText(h, 34817, "CD001") || HasText(h, 36865, "CD001"))
                return T("CD/DVD image", Sure, "iso");
            if (HasText(h, 0, "conectix"))
                return T("Virtual hard disk (VHD)", Sure, "vhd");
            if (HasText(h, 0, "vhdxfile"))
                return T("Virtual hard disk (VHDX)", Sure, "vhdx");
            if (HasText(h, 0, "KDMV"))
                return T("VMware virtual disk", Sure, "vmdk");
            if (Has(h, 0, 0x51, 0x46, 0x49, 0xFB))
                return T("QEMU virtual disk", Sure, "qcow2", "qcow");
            if (Has(h, 64, 0x7F, 0x10, 0xDA, 0xBE))
                return T("VirtualBox virtual disk", Sure, "vdi");
            if (HasText(h, 0, "MZ"))
                return Program(h);
            if (Has(h, 0, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1))
                return Ole(stream, h);
            if (Has(h, 0, 0x7F, 0x45, 0x4C, 0x46))
                return T("Linux or Android program (ELF)", Likely, "so", "elf");
            if (Has(h, 0, 0xCA, 0xFE, 0xBA, 0xBE))
                return T("Java class", Likely, "class");
            if (HasText(h, 0, "dex\n"))
                return T("Android program (DEX)", Sure, "dex");
            if (Has(h, 0, 0x4C, 0x00, 0x00, 0x00, 0x01, 0x14, 0x02, 0x00))
                return T("Windows shortcut", Sure, "lnk");
            if (HasText(h, 0, "OTTO"))
                return T("OpenType font", Sure, "otf");
            if (HasText(h, 0, "ttcf"))
                return T("TrueType font collection", Sure, "ttc");
            if (HasText(h, 0, "wOFF"))
                return T("Web font (WOFF)", Sure, "woff");
            if (HasText(h, 0, "wOF2"))
                return T("Web font (WOFF2)", Sure, "woff2");
            if ((Has(h, 0, 0x00, 0x01, 0x00, 0x00) || HasText(h, 0, "true")) && h.Length > 12)
            {
                int tables = (h[4] << 8) | h[5];
                if (tables > 3 && tables < 64)
                    return T("TrueType font", Likely, "ttf");
            }
            if (HasText(h, 0, "SQLite format 3"))
                return T("SQLite database", Sure, "sqlite", "db", "sqlite3");
            if (HasText(h, 4, "Standard Jet DB"))
                return T("Access database", Sure, "mdb");
            if (HasText(h, 4, "Standard ACE DB"))
                return T("Access database", Sure, "accdb");
            if (HasText(h, 0, "d8:announce") || HasText(h, 0, "d13:announce-list"))
                return T("BitTorrent file", Sure, "torrent");
            if ((HasText(h, 0, "FWS") || HasText(h, 0, "CWS") || HasText(h, 0, "ZWS")) && h.Length > 3 && h[3] < 64)
                return T("Flash animation", Likely, "swf");
            if (HasText(h, 0, "gimp xcf"))
                return T("GIMP image", Sure, "xcf");
            if (HasText(h, 0, "ITSF"))
                return T("Compiled HTML help", Sure, "chm");
            if (HasText(h, 0, "!<arch>\ndebian"))
                return T("Debian package", Sure, "deb");
            if (Has(h, 0, 0xED, 0xAB, 0xEE, 0xDB))
                return T("RPM package", Sure, "rpm");
            if (HasText(h, 0, "II*\0") || HasText(h, 0, "MM\0*"))
            {
                if (HasText(h, 8, "CR"))
                    return T("Canon RAW photo", Sure, "cr2");
                return T("TIFF image", Sure, "tif", "tiff");
            }
            if (HasText(h, 0, "BM") && h.Length > 26)
            {
                if (U32(h, 2) == length && U32(h, 14) >= 12 && U32(h, 14) <= 124)
                    return T("Bitmap image", Sure, "bmp", "dib");
                if (U32(h, 6) == 0 && U32(h, 14) >= 12 && U32(h, 14) <= 124)
                    return T("Bitmap image", Likely, "bmp", "dib");
            }
            if (Has(h, 0, 0x00, 0x00, 0x01, 0x00) && h.Length > 22 && h[4] > 0 && h[5] == 0 && (h[9] == 0 || h[9] == 255))
                return T("Icon", Likely, "ico");
            if (Has(h, 0, 0x00, 0x00, 0x02, 0x00) && h.Length > 22 && h[4] > 0 && h[5] == 0 && (h[9] == 0 || h[9] == 255))
                return T("Cursor", Likely, "cur");
            if (h.Length >= 4 && h[0] == 0xFF && (h[1] & 0xF6) == 0xF0)
                return T("AAC audio", Unsure, "aac");
            if (h.Length >= 4 && h[0] == 0xFF && (h[1] & 0xE0) == 0xE0 && ((h[1] >> 1) & 3) != 0 && (h[2] >> 4) != 0 && (h[2] >> 4) != 15)
                return T("MP3 audio", Unsure, "mp3");
            if (length >= 512)
            {
                byte[] tail = ReadAt(stream, length - 512, 512);
                if (HasText(tail, 0, "conectix"))
                    return T("Virtual hard disk (VHD)", Sure, "vhd");
                if (HasText(tail, 0, "koly"))
                    return T("macOS disk image", Sure, "dmg");
            }
            return null;
        }

        static DetectedType IsoMedia(byte[] h)
        {
            string major = Ascii(h, 8, 4);
            List<string> brands = new List<string>() { major };
            long size = ((long)h[0] << 24) | ((long)h[1] << 16) | ((long)h[2] << 8) | h[3];
            for (int i = 16; i + 4 <= Math.Min(size, h.Length) && i < 256; i += 4)
                brands.Add(Ascii(h, i, 4));
            if (major == "qt  ")
                return T("QuickTime video", Sure, "mov", "qt");
            if (major.StartsWith("3g2"))
                return T("3GPP2 video", Sure, "3g2");
            if (major.StartsWith("3gp") || major.StartsWith("3ge") || major.StartsWith("3gs") || major.StartsWith("3gr"))
                return T("3GPP video", Sure, "3gp");
            if (major == "M4A ")
                return T("MPEG-4 audio", Sure, "m4a", "mp4");
            if (major == "M4B ")
                return T("MPEG-4 audio book", Sure, "m4b", "m4a");
            if (major == "M4P ")
                return T("Protected MPEG-4 audio", Sure, "m4p");
            if (major.StartsWith("M4V"))
                return T("MPEG-4 video", Sure, "m4v", "mp4");
            if (major == "avif" || major == "avis" || ((major == "mif1" || major == "msf1") && brands.Contains("avif")))
                return T("AVIF image", Sure, "avif");
            if (major == "heic" || major == "heix" || major == "hevc" || major == "heim" || major == "heis" || major == "hevm" || major == "hevs" || ((major == "mif1" || major == "msf1") && brands.Contains("heic")))
                return T("HEIC image", Sure, "heic", "heif");
            if (major == "mif1" || major == "msf1")
                return T("HEIF image", Sure, "heif", "heic");
            if (major == "crx ")
                return T("Canon RAW photo", Sure, "cr3");
            if (major == "f4v ")
                return T("Flash MP4 video", Sure, "f4v", "mp4");
            if (major == "jp2 ")
                return T("JPEG 2000 image", Sure, "jp2");
            if (major == "jpx ")
                return T("JPEG 2000 image", Sure, "jpx", "jp2");
            if (major.StartsWith("iso") || major.StartsWith("mp4") || major == "avc1" || major == "dash" || major == "mmp4" || major == "MSNV" || major == "NDAS" || major == "XAVC" || major.StartsWith("mp7"))
                return T("MPEG-4 video", Sure, "mp4", "m4v", "m4a");
            return T("MPEG-4 media", Likely, "mp4");
        }

        static List<string> ZipNames(Stream stream, long length)
        {
            int tail_size = (int)Math.Min(length, 65557);
            byte[] tail = ReadAt(stream, length - tail_size, tail_size);
            int end = -1;
            for (int i = tail.Length - 22; i >= 0; i--)
                if (Has(tail, i, 0x50, 0x4B, 0x05, 0x06))
                {
                    end = i;
                    break;
                }
            if (end < 0)
                return null;
            long directory_size = U32(tail, end + 12), directory_offset = U32(tail, end + 16);
            if ((directory_offset == 0xFFFFFFFF || directory_size == 0xFFFFFFFF) && end >= 20 && Has(tail, end - 20, 0x50, 0x4B, 0x06, 0x07))
            {
                long zip64 = (long)BitConverter.ToUInt64(tail, end - 20 + 8);
                byte[] record = ReadAt(stream, zip64, 56);
                if (!Has(record, 0, 0x50, 0x4B, 0x06, 0x06))
                    return null;
                directory_size = (long)BitConverter.ToUInt64(record, 40);
                directory_offset = (long)BitConverter.ToUInt64(record, 48);
            }
            if (directory_offset < 0 || directory_offset >= length)
                return null;
            byte[] directory = ReadAt(stream, directory_offset, (int)Math.Min(directory_size, 4 * 1024 * 1024));
            List<string> names = new List<string>();
            int position = 0;
            while (position + 46 <= directory.Length && Has(directory, position, 0x50, 0x4B, 0x01, 0x02) && names.Count < 20000)
            {
                int name_length = U16(directory, position + 28), extra = U16(directory, position + 30), comment = U16(directory, position + 32);
                if (position + 46 + name_length > directory.Length)
                    break;
                names.Add(Encoding.UTF8.GetString(directory, position + 46, name_length).Replace('\\', '/'));
                position += 46 + name_length + extra + comment;
            }
            return names;
        }

        static DetectedType Zip(Stream stream, byte[] h, long length)
        {
            if (HasText(h, 0, "PK\x03\x04") && HasText(h, 30, "mimetype") && U16(h, 26) == 8 && U16(h, 8) == 0)
            {
                int start = 30 + 8 + U16(h, 28);
                int count = (int)Math.Min(U32(h, 18), 100);
                string mime = Ascii(h, start, Math.Max(0, Math.Min(count, h.Length - start))).Trim();
                switch (mime)
                {
                    case "application/epub+zip":
                        return T("EPUB e-book", Sure, "epub");
                    case "application/vnd.oasis.opendocument.text":
                        return T("OpenDocument text", Sure, "odt");
                    case "application/vnd.oasis.opendocument.text-template":
                        return T("OpenDocument text template", Sure, "ott");
                    case "application/vnd.oasis.opendocument.spreadsheet":
                        return T("OpenDocument spreadsheet", Sure, "ods");
                    case "application/vnd.oasis.opendocument.spreadsheet-template":
                        return T("OpenDocument spreadsheet template", Sure, "ots");
                    case "application/vnd.oasis.opendocument.presentation":
                        return T("OpenDocument presentation", Sure, "odp");
                    case "application/vnd.oasis.opendocument.presentation-template":
                        return T("OpenDocument presentation template", Sure, "otp");
                    case "application/vnd.oasis.opendocument.graphics":
                        return T("OpenDocument drawing", Sure, "odg");
                    case "application/vnd.oasis.opendocument.formula":
                        return T("OpenDocument formula", Sure, "odf");
                    case "application/x-krita":
                        return T("Krita image", Sure, "kra");
                    case "image/openraster":
                        return T("OpenRaster image", Sure, "ora");
                }
            }
            List<string> names = ZipNames(stream, length);
            if (names == null)
                return F("zip", "ZIP archive (damaged or incomplete)", Likely, "zip");
            Func<string, bool> has = (string name) => names.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
            Func<string, bool> starts = (string name) => names.Any(x => x.StartsWith(name, StringComparison.OrdinalIgnoreCase));
            Func<string, bool> ends = (string name) => names.Any(x => x.EndsWith(name, StringComparison.OrdinalIgnoreCase));
            if (has("[Content_Types].xml"))
            {
                bool macros = ends("vbaProject.bin");
                if (starts("word/"))
                    return macros ? T("Word document with macros", Sure, "docm", "dotm") : T("Word document", Sure, "docx", "dotx", "docm");
                if (starts("xl/"))
                {
                    if (has("xl/workbook.bin"))
                        return T("Excel binary workbook", Sure, "xlsb");
                    return macros ? T("Excel workbook with macros", Sure, "xlsm", "xltm") : T("Excel workbook", Sure, "xlsx", "xltx", "xlsm");
                }
                if (starts("ppt/"))
                {
                    if (has("ppt/presentation.xml") && macros)
                        return T("PowerPoint presentation with macros", Sure, "pptm", "ppsm", "potm");
                    return T("PowerPoint presentation", Sure, "pptx", "ppsx", "potx", "pptm");
                }
                if (starts("visio/"))
                    return T("Visio drawing", Sure, "vsdx");
                if (has("FixedDocumentSequence.fdseq") || has("FixedDocSeq.fdseq"))
                    return T("XPS document", Sure, "xps", "oxps");
                if (has("AppxManifest.xml"))
                    return T("Windows app package", Sure, "msix", "appx");
                if (has("AppxMetadata/AppxBundleManifest.xml"))
                    return T("Windows app bundle", Sure, "msixbundle", "appxbundle");
                if (names.Any(x => x.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase) && !x.Contains('/')))
                    return T("NuGet package", Sure, "nupkg");
                if (has("3D/3dmodel.model"))
                    return T("3D Manufacturing file", Sure, "3mf");
                if (has("extension.vsixmanifest"))
                    return T("Visual Studio extension", Sure, "vsix");
                return F("zip", "Office Open XML package", Likely, "zip");
            }
            if (has("AndroidManifest.xml") && (starts("classes") || has("resources.arsc")))
                return T("Android app", Sure, "apk");
            if (has("base/manifest/AndroidManifest.xml"))
                return T("Android app bundle", Sure, "aab");
            if (starts("Payload/") && names.Any(x => x.Contains(".app/")))
                return T("iPhone app", Sure, "ipa");
            if (has("install.rdf") || (has("manifest.json") && starts("META-INF/mozilla")))
                return T("Firefox add-on", Sure, "xpi");
            if (has("WEB-INF/web.xml"))
                return T("Java web application", Sure, "war");
            if (has("META-INF/MANIFEST.MF") && ends(".class"))
                return T("Java archive", Sure, "jar");
            if (has("META-INF/MANIFEST.MF"))
                return T("Java archive", Likely, "jar", "zip");
            if (has("doc.kml"))
                return T("Google Earth file", Sure, "kmz");
            if (has("mimetype") && starts("META-INF/container.xml"))
                return T("EPUB e-book", Likely, "epub");
            if (names.Any(x => x.EndsWith(".dist-info/WHEEL", StringComparison.OrdinalIgnoreCase)))
                return T("Python wheel", Sure, "whl");
            if (names.Count > 0 && names.Where(x => !x.EndsWith("/")).All(x => Regex.IsMatch(x, @"\.(jpe?g|png|gif|webp|bmp)$", RegexOptions.IgnoreCase)))
                return F("zip", "ZIP archive of images (comic book?)", Sure, "zip", "cbz");
            return F("zip", "ZIP archive", Sure, "zip");
        }

        static DetectedType Gzip(Stream stream)
        {
            try
            {
                stream.Position = 0;
                byte[] buffer = new byte[512];
                int read = 0;
                using (GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress, true))
                    while (read < buffer.Length)
                    {
                        int n = gzip.Read(buffer, read, buffer.Length - read);
                        if (n <= 0)
                            break;
                        read += n;
                    }
                if (read >= 262 && HasText(buffer, 257, "ustar"))
                    return T("TAR archive compressed with gzip", Sure, "tgz", "gz");
            }
            catch (Exception) { }
            return T("gzip compressed file", Sure, "gz", "gzip");
        }

        static DetectedType Program(byte[] h)
        {
            long pe = U32(h, 0x3C);
            if (pe > 0 && pe < h.Length - 96 && HasText(h, (int)pe, "PE\0\0"))
            {
                int characteristics = U16(h, (int)pe + 22), subsystem = U16(h, (int)pe + 24 + 68);
                if ((characteristics & 0x2000) != 0)
                    return T("Windows library (DLL)", Sure, "dll", "ocx", "cpl", "drv", "ax", "mui", "ime", "tlb");
                if (subsystem == 1)
                    return T("Windows driver", Likely, "sys");
                return T("Windows program", Sure, "exe", "scr", "com");
            }
            return T("DOS program", Likely, "exe", "com");
        }

        static DetectedType Ole(Stream stream, byte[] h)
        {
            int shift = U16(h, 0x1E);
            if (shift != 9 && shift != 12)
                return F("ole", "Microsoft compound file", Unsure, "doc");
            int sector = 1 << shift, per_sector = sector / 4;
            List<long> fat = new List<long>();
            for (int i = 0; i < 109; i++)
            {
                long value = U32(h, 0x4C + i * 4);
                if (value >= 0xFFFFFFFA)
                    break;
                fat.Add(value);
            }
            long difat = U32(h, 0x44);
            for (int guard = 0; difat < 0xFFFFFFFA && guard < 1000; guard++)
            {
                byte[] block = ReadAt(stream, (difat + 1) * sector, sector);
                if (block.Length < sector)
                    break;
                for (int i = 0; i < per_sector - 1; i++)
                {
                    long value = U32(block, i * 4);
                    if (value < 0xFFFFFFFA)
                        fat.Add(value);
                }
                difat = U32(block, sector - 4);
            }
            Dictionary<long, byte[]> fat_cache = new Dictionary<long, byte[]>();
            Func<long, long> next = (long current) =>
            {
                long index = current / per_sector;
                if (index >= fat.Count)
                    return 0xFFFFFFFE;
                byte[] block;
                if (!fat_cache.TryGetValue(index, out block))
                {
                    block = ReadAt(stream, (fat[(int)index] + 1) * sector, sector);
                    fat_cache[index] = block;
                }
                return U32(block, (int)(current % per_sector) * 4);
            };
            List<string> names = new List<string>();
            byte[] clsid = null;
            long directory = U32(h, 0x30);
            for (int guard = 0; directory < 0xFFFFFFFA && guard < 256; guard++)
            {
                byte[] block = ReadAt(stream, (directory + 1) * sector, sector);
                for (int i = 0; i + 128 <= block.Length; i += 128)
                {
                    int name_length = U16(block, i + 64);
                    if (name_length >= 2 && name_length <= 64)
                        names.Add(Encoding.Unicode.GetString(block, i, name_length - 2));
                    if (clsid == null)
                    {
                        clsid = new byte[16];
                        Array.Copy(block, i + 80, clsid, 0, 16);
                    }
                }
                directory = next(directory);
            }
            Func<string, bool> has = (string name) => names.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (has("WordDocument"))
                return T("Word 97-2003 document", Sure, "doc", "dot");
            if (has("Workbook") || has("Book"))
                return T("Excel 97-2003 workbook", Sure, "xls", "xlt");
            if (has("PowerPoint Document"))
                return T("PowerPoint 97-2003 presentation", Sure, "ppt", "pps", "pot");
            if (has("VisioDocument"))
                return T("Visio 2003 drawing", Sure, "vsd");
            if (has("Quill"))
                return T("Publisher document", Sure, "pub");
            if (names.Any(x => x.StartsWith("__substg1.0_", StringComparison.OrdinalIgnoreCase) || x.StartsWith("__properties_version1.0", StringComparison.OrdinalIgnoreCase)))
                return T("Outlook message", Sure, "msg");
            if (clsid != null && clsid[1] == 0x10 && clsid[2] == 0x0C && clsid[3] == 0x00 && clsid[8] == 0xC0 && clsid[15] == 0x46)
            {
                if (clsid[0] == 0x84)
                    return T("Windows Installer package", Sure, "msi");
                if (clsid[0] == 0x86)
                    return T("Windows Installer patch", Sure, "msp");
                if (clsid[0] == 0x82)
                    return T("Windows Installer transform", Sure, "mst");
            }
            if (has("Catalog"))
                return T("Thumbnail cache", Likely, "db");
            return F("ole", "Microsoft compound file", Unsure, "doc", "xls", "msg");
        }

        static DetectedType Text(byte[] h)
        {
            string text;
            int start = 0;
            Encoding encoding = null;
            if (Has(h, 0, 0xEF, 0xBB, 0xBF))
            {
                encoding = new UTF8Encoding(false);
                start = 3;
            }
            else if (Has(h, 0, 0xFF, 0xFE))
            {
                encoding = Encoding.Unicode;
                start = 2;
            }
            else if (Has(h, 0, 0xFE, 0xFF))
            {
                encoding = Encoding.BigEndianUnicode;
                start = 2;
            }
            int sample = Math.Min(h.Length, 8192);
            if (encoding == null)
            {
                int zeros_even = 0, zeros_odd = 0, controls = 0;
                for (int i = 0; i < sample; i++)
                {
                    byte b = h[i];
                    if (b == 0)
                    {
                        if (i % 2 == 0)
                            zeros_even++;
                        else
                            zeros_odd++;
                    }
                    else if (b < 0x20 && b != 9 && b != 10 && b != 13 && b != 12 && b != 27)
                        controls++;
                }
                if (zeros_odd > sample / 4 && zeros_even == 0)
                    encoding = Encoding.Unicode;
                else if (zeros_even > sample / 4 && zeros_odd == 0)
                    encoding = Encoding.BigEndianUnicode;
                else if (zeros_even + zeros_odd > 0 || controls > sample / 100)
                    return null;
                else
                    encoding = new UTF8Encoding(false);
            }
            text = encoding.GetString(h, start, h.Length - start);
            if (text.Length > 0 && text[text.Length - 1] == '\uFFFD')
                text = text.Substring(0, text.Length - 1);
            int bad = text.Take(8192).Count(x => x == '\uFFFD' || (x < ' ' && x != '\t' && x != '\n' && x != '\r' && x != '\f' && x != '\x1b'));
            if (bad > Math.Max(2, Math.Min(text.Length, 8192) / 50))
                return null;
            string t = text.TrimStart(), lower = t.Length > 4096 ? t.Substring(0, 4096).ToLower() : t.ToLower();
            if (lower.StartsWith("<?xml"))
            {
                if (lower.Contains("<svg"))
                    return T("SVG image", Sure, "svg");
                if (lower.Contains("<!doctype plist") || lower.Contains("<plist"))
                    return F("text", "Property list", Sure, "plist", "xml");
                if (lower.Contains("<html"))
                    return F("text", "XHTML page", Sure, "xhtml", "html", "htm");
                if (lower.Contains("<kml"))
                    return F("text", "Google Earth KML", Sure, "kml");
                if (lower.Contains("<gpx"))
                    return F("text", "GPS track", Sure, "gpx");
                if (lower.Contains("<rss"))
                    return F("text", "RSS feed", Sure, "rss", "xml");
                return F("text", "XML document", Sure, "xml");
            }
            if (lower.StartsWith("<svg"))
                return T("SVG image", Sure, "svg");
            if (lower.StartsWith("<!doctype html") || lower.StartsWith("<html"))
                return F("text", "Web page", Sure, "html", "htm");
            if (lower.StartsWith("<head") || lower.StartsWith("<body") || (lower.StartsWith("<") && lower.Contains("<html") && lower.Contains("<body")))
                return F("text", "Web page", Likely, "html", "htm");
            if (t.StartsWith("{\\rtf"))
                return T("Rich Text document", Sure, "rtf");
            if (t.StartsWith("Windows Registry Editor Version") || t.StartsWith("REGEDIT4"))
                return T("Registry file", Sure, "reg");
            if (t.StartsWith("WEBVTT"))
                return T("WebVTT subtitles", Sure, "vtt");
            if (Regex.IsMatch(t, @"^\d+\r?\n\d\d:\d\d:\d\d[,.]\d{1,3} --> \d\d:\d\d:\d\d[,.]\d{1,3}"))
                return T("SubRip subtitles", Sure, "srt");
            if (t.StartsWith("[Script Info]"))
                return T("Advanced SubStation subtitles", Sure, "ass", "ssa");
            if (t.StartsWith("BEGIN:VCARD"))
                return T("Contact card", Sure, "vcf");
            if (t.StartsWith("BEGIN:VCALENDAR"))
                return T("Calendar", Sure, "ics");
            if (t.StartsWith("#EXTM3U"))
                return T("Playlist", Sure, "m3u", "m3u8");
            if (lower.StartsWith("#!"))
            {
                string first = lower.Split('\n')[0];
                if (first.Contains("python"))
                    return F("text", "Python script", Sure, "py");
                if (first.Contains("node"))
                    return F("text", "JavaScript", Sure, "js");
                if (first.Contains("perl"))
                    return F("text", "Perl script", Sure, "pl");
                if (first.Contains("ruby"))
                    return F("text", "Ruby script", Sure, "rb");
                if (first.Contains("pwsh") || first.Contains("powershell"))
                    return F("text", "PowerShell script", Sure, "ps1");
                return F("text", "Shell script", Likely, "sh");
            }
            if (lower.StartsWith("@echo off") || lower.StartsWith("@echo on"))
                return F("text", "Batch file", Likely, "bat", "cmd");
            if ((t.StartsWith("{") || t.StartsWith("[")) && LooksLikeJson(t))
                return F("text", "JSON data", Likely, "json");
            if (Regex.IsMatch(t, @"^(Return-Path|Received|From|Date|Message-ID|MIME-Version|Delivered-To|X-[\w-]+):", RegexOptions.Multiline) && Regex.Matches(t.Length > 4096 ? t.Substring(0, 4096) : t, @"^(Return-Path|Received|From|To|Subject|Date|Message-ID|MIME-Version|Content-Type):", RegexOptions.Multiline).Count >= 3)
                return F("text", "E-mail message", Likely, "eml");
            string[] lines = (t.Length > 16384 ? t.Substring(0, 16384) : t).Split('\n').Select(x => x.TrimEnd('\r')).Where(x => x.Trim().Length > 0).ToArray();
            if ((t.Length > 16384 || h.Length >= 65536) && lines.Length > 1)
                lines = lines.Take(lines.Length - 1).ToArray();
            lines = lines.Take(30).ToArray();
            if (lines.Length >= 2 && lines[0].StartsWith("[") && lines[0].TrimEnd().EndsWith("]") && lines.Skip(1).Any(x => Regex.IsMatch(x, @"^\s*[\w .\-]+\s*=")))
                return F("text", "Settings file", Unsure, "ini", "cfg", "inf");
            foreach (char separator in new char[] { ',', ';', '\t' })
                if (lines.Length >= 2)
                {
                    int count = lines[0].Count(x => x == separator);
                    if (count >= 1 && lines.All(x => x.Count(y => y == separator) == count))
                        return F("text", separator == '\t' ? "Tab separated values" : "Comma separated values", lines.Length >= 4 ? Likely : Unsure, separator == '\t' ? "tsv" : "csv", "txt");
                }
            if (lines.Length >= 3 && lines.Count(x => Regex.IsMatch(x, @"^(#{1,6} |\* |- |\d+\. |```)")) >= 2 && lines[0].StartsWith("#"))
                return F("text", "Markdown text", Unsure, "md", "txt");
            return F("text", "Plain text", Likely, "txt");
        }

        static bool LooksLikeJson(string text)
        {
            string sample = text.Length > 4096 ? text.Substring(0, 4096) : text;
            int index = 1;
            while (index < sample.Length && char.IsWhiteSpace(sample[index]))
                index++;
            if (index >= sample.Length)
                return false;
            char next = sample[index];
            if (text[0] == '{')
                return next == '"' || next == '}';
            return next == '{' || next == '[' || next == '"' || next == ']' || next == '-' || char.IsDigit(next) || sample.Substring(index).StartsWith("true") || sample.Substring(index).StartsWith("false") || sample.Substring(index).StartsWith("null");
        }
    }
}