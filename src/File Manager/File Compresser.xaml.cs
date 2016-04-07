using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
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
        CompressItem current_view, root;
        bool project_saved = true, poject_has_saved = false, CloseWindow = false, busy = false, search_mode = false;
        string last_saved_path;
        string archive_path, archive_password = "";
        InArchiveFormat archive_format = InArchiveFormat.SevenZip;
        int archive_volumes = 1;
        bool archive_has_encrypted = false, archive_names_encrypted = false, archive_solid = false, archive_read_only = false;
        long archive_packed_size = 0, archive_unpacked_size = 0;
        Dictionary<int, string> archive_entries = new Dictionary<int, string>();
        List<string> archive_properties = new List<string>();
        List<string> project_items = new List<string>(), project_excludes = new List<string>(), temp_folders = new List<string>();
        ObservableCollection<CompresserListView> list_items = new ObservableCollection<CompresserListView>();
        const string ProjectHeader = "File Manager archive project";

        CompressWindowMode window_mode = CompressWindowMode.Compress;
        CompressWindowMode WindowMode
        {
            get { return window_mode; }
            set
            {
                window_mode = value;
                compress.Tag = value == CompressWindowMode.Compress ? "Compress" : "Extract the selected items, or all items when nothing is selected (Ctrl+E)";
                compress_image.Source = new BitmapImage(new Uri(value == CompressWindowMode.Compress ? "pack://application:,,,/File Manager;component/Images/Appearance/compress.png" : "pack://application:,,,/File Manager;component/Images/Menu/extract-archive.png"));
                UpdateTitle();
                UpdateCommands();
            }
        }

        public File_Compresser()
        {
            InitializeComponent();
            list.DataContext = list_items;
            root = new CompressItem(null, null, null, false, null, null);
            current_view = root;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTitle();
            UpdateCommands();
        }

        #region States

        void UpdateTitle()
        {
            string name = window_mode == CompressWindowMode.extract ? Path.GetFileName(archive_path) : !string.IsNullOrEmpty(last_saved_path) ? Path.GetFileNameWithoutExtension(last_saved_path) : null;
            Title = "Compressed Files Manager" + (name != null ? " - " + name : "") + (window_mode == CompressWindowMode.Compress && !project_saved && project_items.Count > 0 ? " *" : "");
        }

        static void SetState(FrameworkElement element, bool enabled, string reason)
        {
            element.IsEnabled = enabled;
            ToolTipService.SetShowOnDisabled(element, true);
            string description = element.Tag as string;
            element.ToolTip = enabled ? description : description == null ? reason : description + "\n" + reason;
        }

        string ModifyReason()
        {
            if (archive_format != InArchiveFormat.SevenZip && archive_format != InArchiveFormat.Zip && archive_format != InArchiveFormat.Tar)
                return "Only 7z, zip and tar archives can be changed.";
            if (archive_volumes > 1)
                return "Archives split into volumes can not be changed.";
            if (archive_names_encrypted)
                return "Archives with encrypted file names can not be changed.";
            if (archive_read_only)
                return "The archive file is read-only.";
            return null;
        }

        void UpdateCommands()
        {
            bool archive_mode = window_mode == CompressWindowMode.extract;
            int selected = list.SelectedItems.Count;
            bool has_items = archive_mode ? root.AllFiles.Count > 0 : project_items.Count > 0;
            string modify_reason = archive_mode ? ModifyReason() : null, no_archive = "Open an archive first (ARCHIVE > Open archive).", no_items = "Add files or folders first.";
            CompressItem single = selected == 1 ? (list.SelectedItem as CompresserListView).Referance : null;
            SetState(save_menu, !archive_mode && has_items, archive_mode ? "An opened archive is saved after every change, projects are lists of files to compress." : no_items);
            SetState(save_project, save_menu.IsEnabled, (string)save_menu.ToolTip);
            SetState(save_as_menu, save_menu.IsEnabled, (string)save_menu.ToolTip);
            SetState(compress_menu, !archive_mode && has_items, archive_mode ? "Close the archive first, Compress makes a new archive from a project." : no_items);
            SetState(compress, has_items, archive_mode ? "The archive is empty." : no_items);
            SetState(extract_menu, archive_mode && selected > 0, archive_mode ? "Select the items to extract first." : no_archive);
            SetState(context_extract, extract_menu.IsEnabled, (string)extract_menu.ToolTip);
            SetState(extract_all_menu, archive_mode && has_items, archive_mode ? "The archive is empty." : no_archive);
            SetState(close_archive, archive_mode, "No archive is open.");
            string add_reason = search_mode ? "Leave the search results first (Go to root)." : archive_mode ? modify_reason : current_view != root ? "Go back to 'root' to add files, inside a folder you see what it has on the disk." : null;
            SetState(add_files_menu, add_reason == null, add_reason);
            SetState(add_folder_menu, add_reason == null, add_reason);
            SetState(add_files, add_reason == null, add_reason);
            SetState(add_directory, add_reason == null, add_reason);
            string rename_reason = !archive_mode ? "Rename works inside an opened archive, in a project the names on the disk are used." : modify_reason != null ? modify_reason : selected != 1 ? "Select one item to rename." : null;
            SetState(rename_menu, rename_reason == null, rename_reason);
            SetState(rename_button, rename_reason == null, rename_reason);
            SetState(context_rename, rename_reason == null, rename_reason);
            string remove_reason = selected == 0 ? "Select the items to remove first." : archive_mode ? modify_reason : null;
            SetState(remove_menu, remove_reason == null, remove_reason);
            SetState(remove_selected, remove_reason == null, remove_reason);
            SetState(context_remove, remove_reason == null, remove_reason);
            SetState(select_all_menu, list_items.Count > 0, "The list is empty.");
            SetState(clear_menu, !archive_mode && has_items, archive_mode ? "Clear list works on projects, use ARCHIVE > Close archive." : "The list is already empty.");
            SetState(clear_list, clear_menu.IsEnabled, (string)clear_menu.ToolTip);
            SetState(test_menu, archive_mode && has_items, archive_mode ? "The archive is empty." : no_archive);
            SetState(test_archive, test_menu.IsEnabled, (string)test_menu.ToolTip);
            string properties_reason = archive_mode ? null : selected > 1 ? "Select one item." : !has_items ? no_items : null;
            SetState(properties_menu, properties_reason == null, properties_reason);
            SetState(properties_button, properties_reason == null, properties_reason);
            SetState(context_properties, properties_reason == null, properties_reason);
            SetState(search_menu, root.AllFiles.Count > 0, "The list is empty.");
            SetState(search_button, root.AllFiles.Count > 0, "The list is empty.");
            SetState(refresh_menu, archive_mode || has_items, no_items);
            SetState(refresh_button, refresh_menu.IsEnabled, (string)refresh_menu.ToolTip);
            SetState(go_up, search_mode || current_view != root, "You are at 'root'.");
            SetState(go_home, search_mode || current_view != root, "You are at 'root'.");
            SetState(context_open, single != null, "Select one item to open.");
        }

        void SetBusy(bool value, bool indeterminate)
        {
            busy = value;
            main_grid.IsEnabled = !value;
            process_bar.IsIndeterminate = indeterminate;
            process_bar.Value = 0;
            process_bar.Visibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        bool ConfirmDiscardProject()
        {
            if (window_mode != CompressWindowMode.Compress || project_saved || project_items.Count == 0)
                return true;
            switch (Messages.ShowMessage("Do you want to save your project?", "The changes of the list are lost if you do not save them.", new ButtonContent("Save", ModrenMessageBitmapImage.Default, 1), new ButtonContent("Don't save", ModrenMessageBitmapImage.Default, 2), new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 3)))
            {
                case MyMessageBoxResult.Button1:
                    return SaveProject(false);
                case MyMessageBoxResult.Button2:
                    return true;
                default:
                    return false;
            }
        }

        void NewProject()
        {
            search_mode = false;
            search.Text = "";
            last_saved_path = string.Empty;
            poject_has_saved = false;
            project_saved = true;
            project_items.Clear();
            project_excludes.Clear();
            archive_path = null;
            archive_password = "";
            archive_entries = new Dictionary<int, string>();
            archive_properties = new List<string>();
            root = new CompressItem(null, null, null, false, null, null);
            current_view = root;
            list_items.Clear();
            current_path.Text = "root";
            WindowMode = CompressWindowMode.Compress;
        }

        #endregion

        #region Project

        private void NewArchive_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmDiscardProject())
                NewProject();
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmDiscardProject())
                OpenProject();
        }

        private void OpenProject()
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.MultiSelect = false;
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Open project file...";
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "File Manager Archive (*.fma)", extentions = SelectFileFilter.SetExtentions(".fma") });
            if (sfd.ShowWindow() != MyMessageBoxResult.OK)
                return;
            List<string> items = new List<string>(), excludes = new List<string>();
            try
            {
                string[] lines = File.ReadAllLines(sfd.SelectedItem, Encoding.UTF8);
                if (lines.Length > 0 && lines[0].Trim() == ProjectHeader)
                {
                    foreach (string i in lines)
                    {
                        int split = i.IndexOf('|');
                        if (split < 0)
                            continue;
                        string key = i.Substring(0, split), value = i.Substring(split + 1).Trim();
                        if (value == "")
                            continue;
                        if (key == "item")
                            items.Add(value);
                        else if (key == "exclude")
                            excludes.Add(value);
                    }
                }
                else
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Binder = new SafeBinder("File_Manager.CompressItem", "System.IO.FileInfo", "System.IO.DirectoryInfo", "System.Collections.Generic.List`1", "System.Collections.Generic.Dictionary`2", "System.Collections.Generic.KeyValuePair`2", "System.Collections.Generic.GenericEqualityComparer`1", "System.Collections.Generic.EqualityComparer`1", "System.Collections.Generic.ObjectEqualityComparer`1", "System.String", "System.UInt64", "System.Boolean", "System.Nullable`1");
                    CompressItem legacy;
                    using (FileStream stream = new FileInfo(sfd.SelectedItem).Open(FileMode.Open, FileAccess.Read))
                        legacy = (CompressItem)bf.Deserialize(stream);
                    if (legacy.Children != null)
                        foreach (var i in legacy.Children.Values)
                            if (i.Item != null)
                                items.Add(i.Item.FullName.TrimEnd('\\'));
                }
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("Error while opening file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                return;
            }
            NewProject();
            project_items.AddRange(items.Distinct(StringComparer.OrdinalIgnoreCase));
            project_excludes.AddRange(excludes.Distinct(StringComparer.OrdinalIgnoreCase));
            last_saved_path = sfd.SelectedItem;
            poject_has_saved = true;
            project_saved = true;
            UpdateTitle();
            ScanProject(true, null);
        }

        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            if (project_items.Count > 0)
                SaveProject(false);
        }

        private void SaveProjectAs_Click(object sender, RoutedEventArgs e)
        {
            if (project_items.Count > 0)
                SaveProject(true);
        }

        private bool SaveProject(bool save_as)
        {
            string path = null;
            if (!poject_has_saved || save_as || string.IsNullOrEmpty(last_saved_path))
            {
                SelectFileDialog sfd = new SelectFileDialog();
                sfd.dialogMode = SelectFileDialogMode.SaveFile;
                sfd.MultiSelect = false;
                sfd.CreateFolderEnable = true;
                sfd.AddExtension = true;
                sfd.DialogTitle = "Save project file...";
                sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "File Manager Archive (*.fma)", extentions = SelectFileFilter.SetExtentions(".fma") });
                if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                    path = sfd.SelectedItem;
            }
            else
                path = last_saved_path;
            if (path == null)
                return false;
            string temp = path + ".partial";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(ProjectHeader);
                sb.AppendLine("version|2");
                foreach (var i in project_items)
                    sb.AppendLine("item|" + i);
                foreach (var i in project_excludes)
                    sb.AppendLine("exclude|" + i);
                File.WriteAllText(temp, sb.ToString(), Encoding.UTF8);
                if (File.Exists(path))
                {
                    try { File.Replace(temp, path, null); }
                    catch (Exception)
                    {
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.Delete(path);
                        File.Move(temp, path);
                    }
                }
                else
                    File.Move(temp, path);
                last_saved_path = path;
                poject_has_saved = true;
                project_saved = true;
                UpdateTitle();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(temp))
                        File.Delete(temp);
                }
                catch (Exception) { }
                Messages.ShowMessage(string.Format("Sorry, we can not save file '{0}'.", path), ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                return false;
            }
        }

        void ScanProject(bool report_links, Action done)
        {
            List<string> items = new List<string>(project_items), excludes = new List<string>(project_excludes), missing = new List<string>();
            int[] counters = new int[2];
            CompressItem new_root = null;
            string go_to = NodePath(current_view);
            SetBusy(true, true);
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (object bw_sender, DoWorkEventArgs bw_e) =>
            {
                new_root = CompressItem.BuildProject(items, excludes, missing, counters);
            };
            bw.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
            {
                SetBusy(false, true);
                if (CloseWindow)
                    return;
                if (bw_e.Error != null)
                {
                    Messages.ShowMessage("We can not read the files of the project.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, bw_e.Error.Message);
                    return;
                }
                root = new_root;
                current_view = root;
                RefreshView(FindNode(go_to));
                List<string> notes = new List<string>();
                if (missing.Count > 0)
                {
                    project_items.RemoveAll(x => missing.Contains(x));
                    project_saved = false;
                    UpdateTitle();
                    notes.Add(string.Format("{0} item{1} no longer exist{2} and {3} removed from the list:\n", missing.Count, missing.Count > 1 ? "s" : "", missing.Count > 1 ? "" : "s", missing.Count > 1 ? "were" : "was") + string.Join("\n", missing.Take(20).ToArray()));
                }
                if (counters[1] > 0)
                    notes.Add(string.Format("{0} folder{1} could not be read (no permission), {2} not in the list.", counters[1], counters[1] > 1 ? "s" : "", counters[1] > 1 ? "they are" : "it is"));
                if (report_links && counters[0] > 0)
                    notes.Add(string.Format("{0} folder link{1} (junction or symbolic link) {2} not followed.", counters[0], counters[0] > 1 ? "s" : "", counters[0] > 1 ? "were" : "was"));
                if (notes.Count > 0)
                    Messages.ShowMessage("Some items are not in the list.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n\n", notes.ToArray()));
                if (done != null)
                    done();
            };
            bw.RunWorkerAsync();
        }

        void AddToProject(IEnumerable<string> paths)
        {
            List<string> notes = new List<string>();
            HashSet<string> names = new HashSet<string>(project_items.Select(x => Path.GetFileName(x).ToLower()));
            bool changed = false;
            foreach (string i in paths)
            {
                string full;
                try { full = Path.GetFullPath(i).TrimEnd('\\'); }
                catch (Exception) { continue; }
                if (Path.GetDirectoryName(full) == null || full.EndsWith(":"))
                {
                    notes.Add(full + ": a whole drive can not be added, add its folders instead.");
                    continue;
                }
                if (!File.Exists(full) && !Directory.Exists(full))
                {
                    notes.Add(full + ": not found.");
                    continue;
                }
                if (project_items.Any(x => x.Equals(full, StringComparison.OrdinalIgnoreCase)))
                {
                    notes.Add(full + ": already in the list.");
                    continue;
                }
                string owner = project_items.FirstOrDefault(x => full.StartsWith(x + "\\", StringComparison.OrdinalIgnoreCase));
                if (owner != null)
                {
                    if (project_excludes.Any(x => full.StartsWith(x + "\\", StringComparison.OrdinalIgnoreCase)))
                        notes.Add(full + ": it is inside a folder that was removed from the list, add that folder again.");
                    else if (project_excludes.RemoveAll(x => x.Equals(full, StringComparison.OrdinalIgnoreCase) || x.StartsWith(full + "\\", StringComparison.OrdinalIgnoreCase)) > 0)
                        changed = true;
                    else
                        notes.Add(full + ": already in the list inside '" + owner + "'.");
                    continue;
                }
                List<string> inside = project_items.Where(x => x.StartsWith(full + "\\", StringComparison.OrdinalIgnoreCase)).ToList();
                foreach (var j in inside)
                {
                    project_items.Remove(j);
                    names.Remove(Path.GetFileName(j).ToLower());
                }
                if (names.Contains(Path.GetFileName(full).ToLower()))
                {
                    project_items.AddRange(inside);
                    foreach (var j in inside)
                        names.Add(Path.GetFileName(j).ToLower());
                    notes.Add(full + ": another item with the same name is already in the list.");
                    continue;
                }
                project_items.Add(full);
                names.Add(Path.GetFileName(full).ToLower());
                changed = true;
            }
            if (notes.Count > 0)
                Messages.ShowMessage("Some items were not added.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", notes.Take(20).ToArray()) + (notes.Count > 20 ? "\n..." : ""));
            if (changed)
            {
                project_saved = false;
                UpdateTitle();
                ScanProject(true, null);
            }
        }

        void RemoveFromProject(List<CompressItem> nodes)
        {
            HashSet<CompressItem> removed = new HashSet<CompressItem>();
            foreach (var node in nodes.OrderBy(x => Depth(x)))
            {
                if (node.Parent == null || node.Item == null || Ancestors(node).Any(x => removed.Contains(x)))
                    continue;
                node.Parent.Children.Remove(node.FileName.ToLower());
                string path = node.Item.FullName.TrimEnd('\\');
                if (node.Parent == root)
                {
                    project_items.RemoveAll(x => x.Equals(path, StringComparison.OrdinalIgnoreCase));
                    project_excludes.RemoveAll(x => x.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    project_excludes.RemoveAll(x => x.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase));
                    project_excludes.Add(path);
                }
                foreach (var i in Descendants(node))
                    removed.Add(i);
                ulong size = node.RealSize.HasValue ? node.RealSize.Value : 0;
                foreach (var i in Ancestors(node))
                    if (i.RealSize.HasValue && i.RealSize.Value >= size)
                        i.RealSize -= size;
            }
            root.AllFiles.RemoveAll(x => removed.Contains(x));
            project_saved = false;
            UpdateTitle();
            if (removed.Contains(current_view) || Ancestors(current_view).Any(x => removed.Contains(x)))
                current_view = root;
            if (search_mode)
                SearchButton_Click(null, null);
            else
                RefreshView(current_view);
        }

        private void CompressProject_Click(object sender, RoutedEventArgs e)
        {
            if (window_mode != CompressWindowMode.Compress)
                return;
            if (project_items.Count == 0)
            {
                Messages.ShowMessage("Please add files to compress first.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                return;
            }
            ScanProject(false, () =>
            {
                if (root.AllFiles.Count == 0)
                    Messages.ShowMessage("There are no files to compress.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                else
                    new Compress_Files().ShowWindow(root, SuggestedName());
            });
        }

        string SuggestedName()
        {
            if (project_items.Count == 0)
                return null;
            string first = project_items[0], folder = Path.GetDirectoryName(first);
            if (!string.IsNullOrEmpty(last_saved_path))
                return Path.Combine(folder, Path.GetFileNameWithoutExtension(last_saved_path));
            if (project_items.Count == 1)
                return Path.Combine(folder, File.Exists(first) ? Path.GetFileNameWithoutExtension(first) : Path.GetFileName(first));
            string folder_name = Path.GetFileName(folder.TrimEnd('\\'));
            return Path.Combine(folder, string.IsNullOrEmpty(folder_name) ? "Archive" : folder_name);
        }

        #endregion

        #region Archive

        private void OpenArchive_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmDiscardProject())
                return;
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.MultiSelect = false;
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Open archive file...";
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Archive files", extentions = SelectFileFilter.SetExtentions(".7z|.zip|.rar|.001|.gz|.tar|.cab|.iso|.xz|.txz|.lzma|.cpio|.bz2|.bzip|.bzip2|.tbz2|.tbz|.gzip|.tgz|.tpz|.z|.taz|.lzh|.rpm|.deb|.jar|.arj|.vhd|.wim|.swm|.fat|.ntfs|.dmg|.hfs|.xar|.squashfs") });
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "All files", extentions = SelectFileFilter.SetExtentions("") });
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                OpenArchive(sfd.SelectedItem, false);
        }

        internal void OpenArchive(string path, bool extract_all_now)
        {
            if (!Functions.PrepareSevenZip())
                return;
            NewProject();
            archive_path = path;
            archive_password = "";
            archive_names_encrypted = false;
            LoadArchive(extract_all_now, null);
        }

        void LoadArchive(bool extract_all_now, string go_to)
        {
            int total = 1, current = 0;
            bool any_encrypted = false, solid = false;
            string error = null, path = archive_path, password = archive_password;
            InArchiveFormat format = InArchiveFormat.SevenZip;
            int volumes = 1;
            long packed = 0, unpacked = 0;
            Dictionary<int, string> entries = new Dictionary<int, string>();
            List<string> properties = new List<string>();
            CompressItem new_root = new CompressItem(null, null, null, false, null, null);
            SetBusy(true, false);
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += (object bw_sender, DoWorkEventArgs bw_e) =>
            {
                Thread th = new Thread(() =>
                {
                    while (bw.IsBusy)
                    {
                        try { bw.ReportProgress(current); }
                        catch (Exception) { }
                        if (bw.IsBusy)
                            Thread.Sleep(10);
                        else
                            return;
                    }
                });
                th.IsBackground = true;
                th.Start();
                try
                {
                    using (SevenZipExtractor sze = password == "" ? new SevenZipExtractor(path) : new SevenZipExtractor(path, password))
                    {
                        ReadOnlyCollection<ArchiveFileInfo> collection = sze.ArchiveFileData;
                        total = collection.Count;
                        format = sze.Format;
                        solid = sze.IsSolid;
                        packed = sze.PackedSize;
                        unpacked = sze.UnpackedSize;
                        try { volumes = Math.Max(1, sze.VolumeFileNames.Count); }
                        catch (Exception) { }
                        try
                        {
                            foreach (var i in sze.ArchiveProperties)
                                if (i.Value != null && i.Value.ToString() != "")
                                    properties.Add(i.Name + ": " + i.Value);
                        }
                        catch (Exception) { }
                        foreach (var i in collection)
                        {
                            if (CloseWindow)
                                return;
                            if (i.Encrypted)
                                any_encrypted = true;
                            entries[i.Index] = i.FileName;
                            CompressItem.AddArchiveEntry(new_root, i, path, collection.Count == 1);
                            current++;
                        }
                    }
                    CompressItem.ComputeSizes(new_root);
                }
                catch (Exception ex)
                { error = ex.Message; }
            };

            bw.ProgressChanged += (object bw_sender, ProgressChangedEventArgs bw_e) =>
            {
                process_bar.Value = (bw_e.ProgressPercentage * 100.0) / (total + 0.0);
            };

            bw.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
            {
                SetBusy(false, true);
                if (CloseWindow)
                    return;
                if (error != null || (new_root.AllFiles.Count == 0 && go_to == null))
                {
                    string new_password;
                    if (Messages.ShowMessageWithPassword("We can not open this archive.", ModrenMessageButton.OKCancel, ModrenMessageImage.Password, ModrenMessageSound.Exclamation, true, (error == null ? "The archive is empty, damaged or encrypted." : error) + "\nIf the archive is encrypted enter its password.", "", out new_password) == MyMessageBoxResult.OK && new_password != "")
                    {
                        archive_password = new_password;
                        archive_names_encrypted = true;
                        LoadArchive(extract_all_now, go_to);
                    }
                    else if (go_to == null)
                        NewProject();
                    return;
                }
                root = new_root;
                current_view = root;
                archive_entries = entries;
                archive_properties = properties;
                archive_format = format;
                archive_volumes = volumes;
                archive_solid = solid;
                archive_packed_size = packed;
                archive_unpacked_size = unpacked;
                archive_has_encrypted = any_encrypted;
                try { archive_read_only = new FileInfo(path).IsReadOnly; }
                catch (Exception) { archive_read_only = false; }
                search_mode = false;
                WindowMode = CompressWindowMode.extract;
                RefreshView(go_to == null ? root : FindNode(go_to));
                if (extract_all_now)
                    ExtractAll_Click(null, null);
            };
            bw.RunWorkerAsync();
        }

        bool AskArchivePassword()
        {
            if (!archive_has_encrypted || archive_password != "")
                return true;
            string password;
            if (Messages.ShowMessageWithPassword("This archive has encrypted files.", ModrenMessageButton.OKCancel, ModrenMessageImage.Password, ModrenMessageSound.Question, true, "Enter the password to extract them.", "", out password) == MyMessageBoxResult.OK && password != "")
            {
                archive_password = password;
                return true;
            }
            return false;
        }

        void AfterOperation(ArchiveOperation operation)
        {
            if (operation.broken_count > 0 && archive_has_encrypted && !archive_names_encrypted)
                archive_password = "";
        }

        private void CloseArchive_Click(object sender, RoutedEventArgs e)
        {
            NewProject();
        }

        private void Compress_Click(object sender, RoutedEventArgs e)
        {
            if (window_mode == CompressWindowMode.Compress)
                CompressProject_Click(sender, e);
            else if (list.SelectedItems.Count > 0)
                ExtractSelected_Click(sender, e);
            else
                ExtractAll_Click(sender, e);
        }

        List<CompressItem> SelectedNodes()
        {
            List<CompressItem> nodes = new List<CompressItem>();
            foreach (CompresserListView i in list.SelectedItems)
                nodes.Add(i.Referance);
            return nodes;
        }

        private void ExtractSelected_Click(object sender, RoutedEventArgs e)
        {
            if (window_mode != CompressWindowMode.extract)
                return;
            if (list.SelectedItems.Count > 0)
                Extract(SelectedNodes());
            else
                Messages.ShowMessage("Please select items to extract first.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void ExtractAll_Click(object sender, RoutedEventArgs e)
        {
            if (window_mode == CompressWindowMode.extract)
                Extract(root.Children.Values.ToList());
        }

        void Extract(List<CompressItem> nodes)
        {
            List<CompressItem> top = nodes.Where(x => !Ancestors(x).Any(y => nodes.Contains(y))).ToList();
            if (top.Count == 0 || !AskArchivePassword())
                return;
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = true;
            sfd.DialogTitle = "Extract archive in ...";
            sfd.AddExtension = false;
            try { sfd.InitialDirectory = Path.GetDirectoryName(archive_path); }
            catch (Exception) { }
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                ArchiveOperation operation = new ArchiveOperation();
                operation.StartExtracting(archive_path, archive_password, sfd.SelectedItem, top, archive_entries, true);
                AfterOperation(operation);
            }
        }

        void OpenFromArchive(CompressItem item)
        {
            if (!AskArchivePassword())
                return;
            string temp_folder = Path.Combine(Path.Combine(Path.GetTempPath(), "File Manager"), Guid.NewGuid().ToString("N").Substring(0, 8));
            temp_folders.Add(temp_folder);
            ArchiveOperation operation = new ArchiveOperation();
            bool done = operation.StartExtracting(archive_path, archive_password, temp_folder, new List<CompressItem>() { item }, archive_entries, false);
            AfterOperation(operation);
            if (done)
            {
                string extracted = Path.Combine(temp_folder, ArchiveOperation.SafeName(item.FileName));
                try { Process.Start(extracted); }
                catch (Exception ex)
                { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            }
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            if (window_mode != CompressWindowMode.extract || !AskArchivePassword())
                return;
            ArchiveOperation operation = new ArchiveOperation();
            operation.StartTesting(archive_path, archive_password);
            AfterOperation(operation);
        }

        static OutArchiveFormat OutFormat(InArchiveFormat format)
        {
            return format == InArchiveFormat.Zip ? OutArchiveFormat.Zip : format == InArchiveFormat.Tar ? OutArchiveFormat.Tar : OutArchiveFormat.SevenZip;
        }

        void UpdateArchive(ArchiveUpdate update)
        {
            string go_to = search_mode ? null : NodePath(current_view);
            if (new ArchiveOperation().StartUpdating(archive_path, archive_password, OutFormat(archive_format), update))
                LoadArchive(false, go_to == null ? "root" : go_to);
        }

        void AddToArchive(IEnumerable<string> paths)
        {
            string reason = ModifyReason();
            if (reason == null && search_mode)
                reason = "Leave the search results first (Go to root).";
            if (reason != null)
            {
                Messages.ShowMessage("We can not add files to this archive.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, reason);
                return;
            }
            if (archive_has_encrypted && !AskArchivePassword())
                return;
            CompressItem target = current_view;
            ArchiveUpdate update = new ArchiveUpdate();
            update.Target = target == root ? "" : target.FilePath;
            HashSet<string> taken = new HashSet<string>(target.Children.Keys);
            HashSet<string> sources = new HashSet<string>();
            int conflict_for_all = 0;
            foreach (string i in paths)
            {
                string full;
                try { full = Path.GetFullPath(i).TrimEnd('\\'); }
                catch (Exception) { continue; }
                if (Path.GetDirectoryName(full) == null || full.EndsWith(":"))
                {
                    update.Notes.Add(full + ": a whole drive can not be added, add its folders instead.");
                    continue;
                }
                bool is_file = File.Exists(full);
                if (!is_file && !Directory.Exists(full))
                {
                    update.Notes.Add(full + ": not found.");
                    continue;
                }
                if (string.Equals(full, archive_path, StringComparison.OrdinalIgnoreCase) || archive_path.StartsWith(full + "\\", StringComparison.OrdinalIgnoreCase))
                {
                    update.Notes.Add(full + ": the archive can not be added to itself.");
                    continue;
                }
                string name = Path.GetFileName(full), final_name = name;
                if (!sources.Add(name.ToLower()))
                {
                    update.Notes.Add(full + ": another item with the same name is being added.");
                    continue;
                }
                CompressItem existing;
                if (target.Children.TryGetValue(name.ToLower(), out existing))
                {
                    int answer = conflict_for_all != 0 ? conflict_for_all : AskAddConflict(name, ref conflict_for_all);
                    if (answer == 0)
                        return;
                    if (answer == 2)
                        continue;
                    if (answer == 1)
                    {
                        foreach (var j in Descendants(existing))
                            foreach (int k in j.Indexes)
                                update.Changes[k] = null;
                    }
                    else
                    {
                        string base_name = is_file ? Path.GetFileNameWithoutExtension(name) : name, extention = is_file ? Path.GetExtension(name) : "";
                        int number = 2;
                        do final_name = base_name + " (" + number++ + ")" + extention;
                        while (taken.Contains(final_name.ToLower()) || sources.Contains(final_name.ToLower()));
                    }
                }
                taken.Add(final_name.ToLower());
                update.Sources.Add(full);
                update.FinalNames[name.ToLower()] = final_name;
            }
            if (update.Sources.Count == 0)
            {
                if (update.Notes.Count > 0)
                    Messages.ShowMessage("Nothing was added.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", update.Notes.ToArray()));
                return;
            }
            string guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            foreach (var child in root.Children.Values)
                if (update.FinalNames.ContainsKey(child.FileName.ToLower()))
                {
                    string aside = child.FileName + ".fm-" + guid;
                    bool any = false;
                    foreach (var j in Descendants(child))
                        foreach (int k in j.Indexes)
                            if (!update.Changes.ContainsKey(k))
                            {
                                update.Changes[k] = aside + j.FilePath.Substring(child.FilePath.Length);
                                any = true;
                            }
                    if (any)
                        update.Aside[aside.ToLower()] = child.FileName;
                }
            if (Messages.ShowMessage(string.Format("Add {0} item{1} to the archive?", update.Sources.Count, update.Sources.Count > 1 ? "s" : ""), ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "Into: " + NodePath(target) + "\n" + string.Join("\n", update.Sources.Take(10).ToArray()) + (update.Sources.Count > 10 ? "\n..." : "")) != MyMessageBoxResult.Yes)
                return;
            UpdateArchive(update);
        }

        int AskAddConflict(string name, ref int conflict_for_all)
        {
            switch (Messages.ShowMessage(string.Format("'{0}' already exists in this folder of the archive.", name), "", new ButtonContent("Replace", ModrenMessageBitmapImage.Default, 1), new ButtonContent("Replace all", ModrenMessageBitmapImage.Default, 2), new ButtonContent("Skip", ModrenMessageBitmapImage.Skip, 3), new ButtonContent("Skip all", ModrenMessageBitmapImage.Skip, 4), new ButtonContent("Keep both", ModrenMessageBitmapImage.Default, 5), new ButtonContent("Keep both for all", ModrenMessageBitmapImage.Default, 6), new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 7)))
            {
                case MyMessageBoxResult.Button1:
                    return 1;
                case MyMessageBoxResult.Button2:
                    conflict_for_all = 1;
                    return 1;
                case MyMessageBoxResult.Button3:
                    return 2;
                case MyMessageBoxResult.Button4:
                    conflict_for_all = 2;
                    return 2;
                case MyMessageBoxResult.Button5:
                    return 3;
                case MyMessageBoxResult.Button6:
                    conflict_for_all = 3;
                    return 3;
                default:
                    return 0;
            }
        }

        void RemoveFromArchive(List<CompressItem> nodes)
        {
            string reason = ModifyReason();
            if (reason != null)
            {
                Messages.ShowMessage("We can not remove items from this archive.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, reason);
                return;
            }
            ArchiveUpdate update = new ArchiveUpdate();
            foreach (var i in nodes)
                foreach (var j in Descendants(i))
                    foreach (int k in j.Indexes)
                        update.Changes[k] = null;
            if (update.Changes.Count == 0)
                return;
            if (Messages.ShowMessage(string.Format("Delete {0} item{1} from the archive?", nodes.Count, nodes.Count > 1 ? "s" : ""), ModrenMessageButton.YesNo, ModrenMessageImage.Delete, ModrenMessageSound.Question, true, string.Join("\n", nodes.Take(10).Select(x => x.FilePath).ToArray()) + (nodes.Count > 10 ? "\n..." : "") + "\nThis can not be undone.") != MyMessageBoxResult.Yes)
                return;
            UpdateArchive(update);
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (window_mode != CompressWindowMode.extract || list.SelectedItems.Count != 1)
                return;
            string reason = ModifyReason();
            if (reason != null)
            {
                Messages.ShowMessage("We can not rename items in this archive.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, reason);
                return;
            }
            CompressItem node = (list.SelectedItem as CompresserListView).Referance;
            string name = new Rename().ShowWindow(node.FileName, !node.IsDirectory);
            if (name == null)
                return;
            name = name.Trim();
            if (name == node.FileName)
                return;
            if (name == "" || name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Messages.ShowMessage("This name can not be used.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "A name can not be empty or contain any of these characters: \\ / : * ? \" < > |");
                return;
            }
            if (!name.Equals(node.FileName, StringComparison.OrdinalIgnoreCase) && node.Parent.Children.ContainsKey(name.ToLower()))
            {
                Messages.ShowMessage("Another item with this name already exists in this folder.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, name);
                return;
            }
            string new_path = node.Parent == root ? name : node.Parent.FilePath + "\\" + name;
            ArchiveUpdate update = new ArchiveUpdate();
            foreach (var i in Descendants(node))
                foreach (int k in i.Indexes)
                    update.Changes[k] = new_path + i.FilePath.Substring(node.FilePath.Length);
            if (update.Changes.Count > 0)
                UpdateArchive(update);
        }

        #endregion

        #region View

        static int Depth(CompressItem item)
        {
            int depth = 0;
            for (CompressItem i = item.Parent; i != null; i = i.Parent)
                depth++;
            return depth;
        }

        static IEnumerable<CompressItem> Ancestors(CompressItem item)
        {
            for (CompressItem i = item.Parent; i != null; i = i.Parent)
                yield return i;
        }

        static List<CompressItem> Descendants(CompressItem item)
        {
            List<CompressItem> items = new List<CompressItem>();
            Stack<CompressItem> stack = new Stack<CompressItem>();
            stack.Push(item);
            while (stack.Count > 0)
            {
                CompressItem i = stack.Pop();
                items.Add(i);
                if (i.Children != null)
                    foreach (var j in i.Children.Values)
                        stack.Push(j);
            }
            return items;
        }

        string NodePath(CompressItem item)
        {
            List<string> names = new List<string>();
            for (CompressItem i = item; i != null && i.Parent != null; i = i.Parent)
                names.Insert(0, i.FileName);
            names.Insert(0, "root");
            return string.Join("\\", names.ToArray());
        }

        CompressItem FindNode(string path)
        {
            if (path == null)
                return null;
            string[] parts = path.Trim().Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            CompressItem node = root;
            for (int i = parts.Length > 0 && parts[0].Equals("root", StringComparison.OrdinalIgnoreCase) ? 1 : 0; i < parts.Length; i++)
                if (node.Children == null || !node.Children.TryGetValue(parts[i].ToLower(), out node))
                    return null;
            return node;
        }

        private void RefreshView(CompressItem current)
        {
            if (current == null || current.Children == null)
                current = root;
            bool same = current == current_view && !search_mode;
            search_mode = false;
            current_path.Text = NodePath(current);
            int x = list.SelectedIndex;
            list_items.Clear();
            foreach (var i in current.Children.Values.OrderBy(y => y.IsDirectory ? 0 : 1).ThenBy(y => y.FileName, StringComparer.CurrentCultureIgnoreCase))
                list_items.Add(new CompresserListView(i));
            if (list.Items.Count > 0)
            {
                if (!same)
                    list.ScrollIntoView(list.Items[0]);
                else if (x > -1 && x < list.Items.Count)
                    list.ScrollIntoView(list_items[x]);
            }
            current_view = current;
            UpdateCommands();
        }

        private void GoUp_Click(object sender, RoutedEventArgs e)
        {
            if (search_mode)
                RefreshView(current_view);
            else if (current_view.Parent != null)
                RefreshView(current_view.Parent);
        }

        private void GoHome_Click(object sender, RoutedEventArgs e)
        {
            RefreshView(root);
        }

        private void CurrentPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                CompressItem target = FindNode(current_path.Text);
                if (target != null && target.IsDirectory)
                {
                    RefreshView(target);
                    list.Focus();
                }
                else
                    Messages.ShowMessage("This folder is not in the list.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, current_path.Text + "\nType a path like: root\\folder\\sub folder");
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                list.Focus();
            }
        }

        private void CurrentPath_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!search_mode)
                current_path.Text = NodePath(current_view);
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCommands();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            list.SelectAll();
            list.Focus();
        }

        private void Item_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && list.SelectedItems.Count == 1)
                OpenNode((list.SelectedItem as CompresserListView).Referance);
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItems.Count == 1)
                OpenNode((list.SelectedItem as CompresserListView).Referance);
        }

        void OpenNode(CompressItem item)
        {
            if (item.IsDirectory)
                RefreshView(item);
            else if (window_mode == CompressWindowMode.Compress)
            {
                try { Process.Start(item.Item.FullName); }
                catch (Exception ex)
                { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            }
            else
                OpenFromArchive(item);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (window_mode == CompressWindowMode.extract)
                LoadArchive(false, search_mode ? "root" : NodePath(current_view));
            else if (project_items.Count > 0)
                ScanProject(false, null);
        }

        private void SearchMenu_Click(object sender, RoutedEventArgs e)
        {
            search.Focus();
            search.SelectAll();
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            search_hint.Visibility = search.Text.Length == 0 ? Visibility.Visible : Visibility.Hidden;
        }

        private void Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                SearchButton_Click(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                search.Text = "";
                if (search_mode)
                    RefreshView(current_view);
                list.Focus();
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string text = search.Text.Trim();
            if (text == "")
            {
                if (search_mode)
                    RefreshView(current_view);
                return;
            }
            Regex pattern = null;
            if (text.Contains('*') || text.Contains('?'))
                pattern = new Regex("^" + Regex.Escape(text).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
            list_items.Clear();
            foreach (var i in root.AllFiles.OrderBy(y => NodePath(y), StringComparer.CurrentCultureIgnoreCase))
                if (i.FileName != null && (pattern != null ? pattern.IsMatch(i.FileName) : i.FileName.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0))
                    list_items.Add(new CompresserListView(i));
            search_mode = true;
            current_path.Text = string.Format("search results for '{0}' ({1})", text, list_items.Count);
            UpdateCommands();
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            string title;
            CompressItem node = list.SelectedItems.Count == 1 ? (list.SelectedItem as CompresserListView).Referance : null;
            if (node != null && window_mode == CompressWindowMode.extract)
            {
                title = node.FileName;
                sb.AppendLine("Path in the archive: " + node.FilePath);
                sb.AppendLine("Type: " + node.FileType);
                if (node.IsDirectory)
                {
                    List<CompressItem> items = Descendants(node);
                    sb.AppendLine(string.Format("Contains: {0} files, {1} folders", items.Count(x => !x.IsDirectory), items.Count(x => x.IsDirectory) - 1));
                }
                sb.AppendLine("Size: " + Functions.FileSize(node.RealSize.HasValue ? node.RealSize.Value : 0, 2) + (node.RealSize.HasValue ? string.Format(" ({0:N0} bytes)", node.RealSize.Value) : ""));
                if (node.Info.HasValue)
                {
                    ArchiveFileInfo info = node.Info.Value;
                    if (info.LastWriteTime > DateTime.MinValue)
                        sb.AppendLine("Modified: " + info.LastWriteTime);
                    if (info.CreationTime > DateTime.MinValue)
                        sb.AppendLine("Created: " + info.CreationTime);
                    sb.AppendLine("Attributes: " + (info.Attributes == 0 ? "-" : ((FileAttributes)info.Attributes).ToString()));
                    if (!node.IsDirectory)
                        sb.AppendLine("CRC: " + info.Crc.ToString("X8"));
                    sb.AppendLine("Encrypted: " + (info.Encrypted ? "yes" : "no"));
                    if (!string.IsNullOrEmpty(info.Comment))
                        sb.AppendLine("Comment: " + info.Comment);
                }
            }
            else if (window_mode == CompressWindowMode.extract)
            {
                title = Path.GetFileName(archive_path);
                sb.AppendLine("Path: " + archive_path);
                sb.AppendLine("Format: " + archive_format);
                sb.AppendLine(string.Format("Size on disk: {0} ({1:N0} bytes)", Functions.FileSize(archive_packed_size, 2), archive_packed_size));
                sb.AppendLine(string.Format("Size of the files: {0} ({1:N0} bytes)", Functions.FileSize(archive_unpacked_size, 2), archive_unpacked_size));
                if (archive_unpacked_size > 0)
                    sb.AppendLine(string.Format("Ratio: {0:0.#}%", archive_packed_size * 100.0 / archive_unpacked_size));
                sb.AppendLine(string.Format("Contains: {0} files, {1} folders", root.AllFiles.Count(x => !x.IsDirectory), root.AllFiles.Count(x => x.IsDirectory)));
                sb.AppendLine("Solid: " + (archive_solid ? "yes" : "no"));
                sb.AppendLine("Encrypted files: " + (archive_has_encrypted ? "yes" : "no") + (archive_names_encrypted ? ", file names encrypted" : ""));
                if (archive_volumes > 1)
                    sb.AppendLine("Volumes: " + archive_volumes);
                foreach (var i in archive_properties)
                    sb.AppendLine(i);
                string reason = ModifyReason();
                sb.AppendLine(reason == null ? "Files can be added, removed and renamed." : reason);
            }
            else if (node != null && node.Item != null)
            {
                title = node.FileName;
                sb.Append(Messages.GetItemInfo(node.Item));
                if (node.IsDirectory)
                {
                    List<CompressItem> items = Descendants(node);
                    sb.AppendLine(string.Format("In the archive: {0} files, {1} folders, {2}", items.Count(x => !x.IsDirectory), items.Count(x => x.IsDirectory) - 1, Functions.FileSize(node.RealSize.HasValue ? node.RealSize.Value : 0, 2)));
                }
            }
            else
            {
                title = "Archive project";
                if (!string.IsNullOrEmpty(last_saved_path))
                    sb.AppendLine("Project file: " + last_saved_path);
                sb.AppendLine(string.Format("Items: {0}, removed inner items: {1}", project_items.Count, project_excludes.Count));
                sb.AppendLine(string.Format("Contains: {0} files, {1} folders, {2}", root.AllFiles.Count(x => !x.IsDirectory), root.AllFiles.Count(x => x.IsDirectory), Functions.FileSize(root.RealSize.HasValue ? root.RealSize.Value : 0, 2)));
                sb.AppendLine("Saved: " + (project_saved ? "yes" : "no"));
            }
            Messages.ShowMessage(title, ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, sb.ToString());
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (busy || e.OriginalSource is MenuItem)
                return;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0, shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0, alt = (Keyboard.Modifiers & ModifierKeys.Alt) != 0;
            bool typing = Keyboard.FocusedElement is TextBox;
            FrameworkElement command = null;
            if (ctrl && !shift && key == Key.N)
                command = new_menu;
            else if (ctrl && !shift && key == Key.O)
                command = open_archive_menu;
            else if (ctrl && shift && key == Key.O)
                command = open_project_menu;
            else if (ctrl && !shift && key == Key.S)
                command = save_menu;
            else if (ctrl && shift && key == Key.S)
                command = save_as_menu;
            else if (ctrl && key == Key.E && window_mode == CompressWindowMode.extract)
                command = compress;
            else if (ctrl && key == Key.T)
                command = test_menu;
            else if (ctrl && key == Key.F)
                command = search_menu;
            else if (key == Key.F5)
                command = refresh_menu;
            else if (alt && key == Key.Enter)
                command = properties_menu;
            else if (alt && key == Key.Home)
                command = go_home;
            else if ((alt && key == Key.Up) || (!typing && key == Key.Back))
                command = go_up;
            else if (!typing && key == Key.F2)
                command = rename_menu;
            else if (!typing && key == Key.Delete)
                command = remove_menu;
            else if (!typing && !alt && key == Key.Enter && list.SelectedItems.Count == 1)
                command = context_open;
            if (command == null)
                return;
            e.Handled = true;
            if (!command.IsEnabled)
                return;
            if (command is MenuItem)
                command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            else
                command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        #endregion

        #region Files

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Add files to archive...";
            sfd.AddExtension = false;
            sfd.MultiSelect = true;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                if (window_mode == CompressWindowMode.extract)
                    AddToArchive(sfd.SelectedItems);
                else
                    AddToProject(sfd.SelectedItems);
            }
        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Add Directory to archive...";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                if (window_mode == CompressWindowMode.extract)
                    AddToArchive(new string[] { sfd.SelectedItem });
                else
                    AddToProject(new string[] { sfd.SelectedItem });
            }
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            if (window_mode != CompressWindowMode.Compress || project_items.Count == 0)
                return;
            project_items.Clear();
            project_excludes.Clear();
            root = new CompressItem(null, null, null, false, null, null);
            current_view = root;
            project_saved = false;
            UpdateTitle();
            RefreshView(root);
        }

        private void RemoveSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (list.SelectedItems.Count == 0)
            {
                Messages.ShowMessage("Please select at least one item to remove.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                return;
            }
            if (window_mode == CompressWindowMode.extract)
                RemoveFromArchive(SelectedNodes().Where(x => !Ancestors(x).Any(y => list.SelectedItems.Cast<CompresserListView>().Any(z => z.Referance == y))).ToList());
            else
                RemoveFromProject(SelectedNodes());
        }

        private void FilesList_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = !busy && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void FilesList_Drop(object sender, DragEventArgs e)
        {
            if (busy || !e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            string[] paths = ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(x => x != string.Empty).ToArray();
            if (paths.Length == 0)
                return;
            if (window_mode == CompressWindowMode.extract)
                AddToArchive(paths);
            else if (current_view != root || search_mode)
            {
                RefreshView(root);
                AddToProject(paths);
            }
            else
                AddToProject(paths);
        }

        #endregion

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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!ConfirmDiscardProject())
            {
                e.Cancel = true;
                return;
            }
            CloseWindow = true;
            foreach (var i in temp_folders)
                try
                {
                    if (Directory.Exists(i))
                        Directory.Delete(i, true);
                }
                catch (Exception) { }
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
            FileType = Item.FileType + (Item.Encrypted && !Item.IsDirectory ? " (encrypted)" : "");
            FileSize = Item.RealSize.HasValue ? Functions.FileSize(Item.RealSize.Value, 2) : "";
            CompressedFileSize = Item.CompressedSize.HasValue ? Functions.FileSize(Item.CompressedSize.Value, 2) : "";
            Modified = Item.Modified.HasValue && Item.Modified.Value > DateTime.MinValue ? Item.Modified.Value.ToString("g") : "";
            ItemToolTip = Item.Item != null ? Item.Item.FullName : Item.FilePath;
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

        string modified;
        public string Modified
        {
            get { return modified; }
            set { modified = value; }
        }

        string item_tool_tip;
        public string ItemToolTip
        {
            get { return item_tool_tip; }
            set { item_tool_tip = value; }
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
        [NonSerialized]
        internal List<int> Indexes = new List<int>();
        [NonSerialized]
        internal DateTime? Modified;
        [NonSerialized]
        internal bool Encrypted;
        [NonSerialized]
        internal ArchiveFileInfo? Info;

        public CompressItem(FileSystemInfo FileSystemItem, CompressItem Parent, CompressItem Root, bool add_sun, ulong? Size, ulong? CompressedSize)
        {
            Item = FileSystemItem;
            this.Parent = Parent;
            this.Root = this;
            RealSize = 0;
            this.CompressedSize = 0;
            Children = new Dictionary<string, CompressItem>();
            AllFiles = new List<CompressItem>();
            IsDirectory = true;
        }

        CompressItem(FileSystemInfo FileSystemItem, CompressItem Parent, CompressItem Root, HashSet<string> excluded, int[] counters)
        {
            Item = FileSystemItem;
            this.Parent = Parent;
            this.Root = Root;
            FileName = FileSystemItem.Name;
            FilePath = FileSystemItem.FullName;
            AllFiles = Root.AllFiles;
            try { Modified = FileSystemItem.LastWriteTime; }
            catch (Exception) { }
            if (FileSystemItem is FileInfo)
            {
                try { RealSize = (ulong)((FileInfo)FileSystemItem).Length; }
                catch (Exception) { RealSize = 0; }
                Extention = FileSystemItem.Extension;
                FileType = Functions.GetFileType(FileSystemItem.Extension);
                IsDirectory = false;
            }
            else
            {
                Extention = "directory";
                FileType = "Directory";
                Children = new Dictionary<string, CompressItem>();
                IsDirectory = true;
                RealSize = 0;
            }
            AllFiles.Add(this);
            Parent.Children[FileName.ToLower()] = this;
            if (IsDirectory)
                AddSuns((DirectoryInfo)FileSystemItem, excluded, counters);
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
        }

        internal static CompressItem BuildProject(List<string> items, List<string> excludes, List<string> missing, int[] counters)
        {
            CompressItem root = new CompressItem(null, null, null, false, null, null);
            HashSet<string> excluded = new HashSet<string>(excludes.Select(x => x.TrimEnd('\\').ToLower()));
            foreach (string i in items)
            {
                if (File.Exists(i))
                    new CompressItem(new FileInfo(i), root, root, excluded, counters);
                else if (Directory.Exists(i))
                    new CompressItem(new DirectoryInfo(i), root, root, excluded, counters);
                else
                    missing.Add(i);
            }
            ComputeSizes(root);
            return root;
        }

        internal static void AddArchiveEntry(CompressItem root, ArchiveFileInfo info, string archive, bool single)
        {
            string name = info.FileName;
            if (string.IsNullOrEmpty(name) || name == "[no name]")
            {
                if (single)
                {
                    string extention = Path.GetExtension(archive).ToLower();
                    name = Path.GetFileNameWithoutExtension(archive) + (extention == ".tgz" || extention == ".tbz" || extention == ".tbz2" || extention == ".txz" || extention == ".taz" ? ".tar" : "");
                }
                else
                    name = "[no name] " + info.Index;
            }
            string[] parts = name.Split(Functions.spliter, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                parts = new string[] { "[no name]" };
            CompressItem parent = root;
            string path = string.Empty;
            for (int i = 0; i < parts.Length; i++)
            {
                path = i == 0 ? parts[i] : path + "\\" + parts[i];
                bool last = i == parts.Length - 1, is_directory = !last || info.IsDirectory;
                string key = parts[i].ToLower();
                CompressItem node;
                if (parent.Children.TryGetValue(key, out node) && node.IsDirectory != is_directory)
                {
                    key = key + "\u0001";
                    if (!parent.Children.TryGetValue(key, out node) || node.IsDirectory != is_directory)
                        node = null;
                }
                if (node == null)
                {
                    node = new CompressItem(path, parts[i], parent, root, null, null, is_directory, false);
                    parent.Children.Add(key, node);
                    root.AllFiles.Add(node);
                }
                if (last)
                {
                    node.Indexes.Add(info.Index);
                    node.Info = info;
                    node.Modified = info.LastWriteTime;
                    node.Encrypted |= info.Encrypted;
                    if (!is_directory)
                        node.RealSize = info.Size;
                }
                parent = node;
            }
        }

        internal static ulong ComputeSizes(CompressItem item)
        {
            if (!item.IsDirectory || item.Children == null)
                return item.RealSize.HasValue ? item.RealSize.Value : 0;
            ulong size = 0;
            foreach (var i in item.Children.Values)
                size += ComputeSizes(i);
            item.RealSize = size;
            return size;
        }

        public List<CompressItem> GetSuns()
        {
            return AllFiles.Where(x => x.Parent == Parent).ToList();
        }

        private void AddSuns(DirectoryInfo di, HashSet<string> excluded, int[] counters)
        {
            DirectoryInfo[] dirs;
            FileInfo[] files;
            try
            {
                dirs = di.GetDirectories();
                files = di.GetFiles();
            }
            catch (Exception)
            {
                counters[1]++;
                return;
            }
            foreach (var i in dirs)
            {
                if (excluded.Contains(i.FullName.TrimEnd('\\').ToLower()))
                    continue;
                if (Functions.IsLink(i))
                {
                    counters[0]++;
                    continue;
                }
                new CompressItem(i, this, Root, excluded, counters);
            }
            foreach (var i in files)
                if (!excluded.Contains(i.FullName.ToLower()))
                    new CompressItem(i, this, Root, excluded, counters);
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