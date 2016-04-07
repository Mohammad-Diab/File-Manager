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
            PrepareControls();
        }

        public File_Destroyer(object list, bool destroy_files)//true file destroyer false disk destroyer
        {
            InitializeComponent();
            PrepareControls();
            if (destroy_files)
            {
                if (list is string[])
                    AddFiles((string[])list);
                else if (list is IEnumerable<string>)
                    AddFiles(((IEnumerable<string>)list).ToArray());
                else if (list is IEnumerable<FileInfo>)
                    AddFiles(((IEnumerable<FileInfo>)list).Select(x => x.FullName).ToArray());
            }
            else
            {
                CurrentPage = DestroyPages.DiskDestroy;
                selectdrive = list as DriveInfo;
            }
        }

        public File_Destroyer(List<string> files, string reason)
        {
            InitializeComponent();
            PrepareControls();
            AddFiles(files.ToArray());
            if (!string.IsNullOrEmpty(reason))
            {
                adding_text.Text = reason;
                adding_text.ToolTip = reason;
                adding_text.Visibility = Visibility.Visible;
            }
        }

        void PrepareControls()
        {
            SafetyTools.DigitsOnly(repete_tb);
            foreach (ComboBox combo in new ComboBox[] { method_cb, disk_method_cb })
            {
                combo.ToolTip = SafetyFileShredder.MethodsHelp();
                for (int i = 0; i < combo.Items.Count; i++)
                {
                    ComboBoxItem item = (ComboBoxItem)combo.Items[i];
                    item.ToolTip = SafetyFileShredder.MethodHelp((DestroyMethod)(i + 1));
                    ToolTipService.SetShowDuration(item, 60000);
                }
            }
            destroy_files_button.Tag = "Destroy the files of the list\n\n" + SafetyFileShredder.Caveat;
            destroy_button.Tag = "Fill the free space of the drive to destroy its deleted files\n\n" + SafetyFileShredder.Caveat;
            ToolTipService.SetShowDuration(destroy_files_button, 60000);
            ToolTipService.SetShowDuration(destroy_button, 60000);
            UpdateButtons();
            UpdateDiskButton();
        }

        void UpdateButtons()
        {
            bool empty = destroy_list_data.Count == 0;
            int selected = files_list.SelectedItems.Count;
            SafetyTools.SetState(add_files, !adding, adding ? "Please wait, files are being added." : null);
            SafetyTools.SetState(add_folder, !adding, adding ? "Please wait, files are being added." : null);
            SafetyTools.SetState(clear_list, !empty && !adding, adding ? "Please wait, files are being added." : "The list is empty.");
            SafetyTools.SetState(remove_selected, selected > 0 && !adding, adding ? "Please wait, files are being added." : empty ? "The list is empty." : "Select files in the list first.");
            SafetyTools.SetState(destroy_files_button, !empty && !adding, adding ? "Please wait, files are being added." : "Add files to the list first.");
        }

        void UpdateDiskButton()
        {
            SafetyTools.SetState(destroy_button, drive_list.IsEnabled && drive_list.SelectedIndex > -1, drive_list.IsEnabled ? "Choose a drive first." : "Please wait, the drives are being read.");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        #region Destroy Files

        List<DestroyListView> destroy_list_data = new List<DestroyListView>();
        HashSet<string> listed_files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool adding = false;

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
            if (listed_files.Count != destroy_list_data.Count)
                listed_files = new HashSet<string>(destroy_list_data.Select(x => x.FullPath), StringComparer.OrdinalIgnoreCase);
            List<string> links = new List<string>(), folders = new List<string>();
            foreach (string i in files)
            {
                if (Directory.Exists(i))
                {
                    folders.Add(i);
                    continue;
                }
                if (!File.Exists(i))
                    continue;
                FileInfo file = new FileInfo(i);
                if (Functions.IsLink(file))
                    links.Add(i);
                else if (listed_files.Add(file.FullName))
                    destroy_list_data.Add(new DestroyListView(file));
            }
            files_list.Items.Refresh();
            UpdateButtons();
            if (links.Count > 0)
                Messages.ShowMessage(string.Format("{0} link{1} not added.", links.Count, links.Count > 1 ? "s were" : " was"), ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "Links (symbolic links) are never followed or destroyed:\n" + string.Join("\n", links.Take(30).ToArray()));
            if (folders.Count > 0)
                AddDirectory(folders);
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Add all files in this directory.";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                AddDirectory(new List<string>() { sfd.SelectedItem });
        }

        private void AddDirectory(List<string> paths)
        {
            if (adding)
                return;
            adding = true;
            adding_text.Text = "Adding files...";
            adding_text.ToolTip = string.Join("\n", paths.ToArray());
            adding_text.Visibility = Visibility.Visible;
            UpdateButtons();
            List<string> found = new List<string>(), skipped = new List<string>();
            BackgroundWorker add_files_in_directory = new BackgroundWorker();
            add_files_in_directory.DoWork += (object bw_sender, DoWorkEventArgs e_bw) =>
            {
                foreach (string path in paths)
                {
                    DirectoryInfo directory = new DirectoryInfo(path);
                    if (!directory.Exists)
                        continue;
                    if (Functions.IsLink(directory))
                        skipped.Add(path + "\n    (link, not followed)");
                    else
                        GetAllFiles(directory, found, skipped);
                }
            };
            add_files_in_directory.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
            {
                adding = false;
                adding_text.Visibility = Visibility.Collapsed;
                if (CloseWindow)
                    return;
                AddFiles(found.ToArray());
                if (skipped.Count > 0)
                    Messages.ShowMessage(string.Format("{0} item{1} not added.", skipped.Count, skipped.Count > 1 ? "s were" : " was"), ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", skipped.Take(30).ToArray()) + (skipped.Count > 30 ? string.Format("\n... and {0} more.", skipped.Count - 30) : ""));
            };
            add_files_in_directory.RunWorkerAsync();
        }

        private void GetAllFiles(DirectoryInfo dir, List<string> found, List<string> skipped)
        {
            DirectoryInfo[] sub_dirs;
            FileInfo[] files;
            try
            {
                sub_dirs = dir.GetDirectories();
                files = dir.GetFiles();
            }
            catch (Exception ex)
            {
                skipped.Add(dir.FullName + "\n    (could not be read: " + ex.Message + ")");
                return;
            }
            foreach (var i in sub_dirs)
            {
                if (CloseWindow)
                    return;
                if (Functions.IsLink(i))
                    skipped.Add(i.FullName + "\n    (link, not followed)");
                else
                    GetAllFiles(i, found, skipped);
            }
            foreach (var i in files)
            {
                if (CloseWindow)
                    return;
                if (Functions.IsLink(i))
                    skipped.Add(i.FullName + "\n    (link, not destroyed)");
                else
                    found.Add(i.FullName);
            }
        }

        private void FilesList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && !adding)
            {
                string[] data = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (data != null && data.Length > 0)
                    AddFiles(data);
            }
        }

        int RepeatCount()
        {
            int repete;
            int.TryParse(repete_tb.Text, out repete);
            if (repete < 1) repete = 1;
            else if (repete > 35) repete = 35;
            return repete;
        }

        private void Repete_LostFocus(object sender, RoutedEventArgs e)
        {
            repete_tb.Text = RepeatCount().ToString();
        }

        private void Destroy_Click(object sender, RoutedEventArgs e)
        {
            if (destroy_list_data.Count > 0 && !adding)
            {
                int repete = RepeatCount();
                bool delete;
                DestroyMethod method;
                repete_tb.Text = repete.ToString();
                delete = delete_files.IsChecked.Value;
                method = (DestroyMethod)method_cb.SelectedIndex + 1;
                StringBuilder names = new StringBuilder();
                names.AppendLine(string.Format("Method: {0}, {1} time{2}{3}.", ((ComboBoxItem)method_cb.SelectedItem).Content, repete, repete > 1 ? "s" : "", delete ? ", then renamed, emptied and deleted" : ", the files are kept with their names and sizes, only their content is overwritten"));
                for (int i = 0; i < destroy_list_data.Count && i < 15; i++)
                    names.AppendLine(destroy_list_data[i].FullPath);
                if (destroy_list_data.Count > 15)
                    names.AppendLine(string.Format("... and {0} more.", destroy_list_data.Count - 15));
                names.AppendLine();
                names.Append(SafetyFileShredder.Caveat);
                if (Messages.ShowMessage(string.Format("Are you sure you want to destroy {0} file{1}?", destroy_list_data.Count, destroy_list_data.Count > 1 ? "s" : ""), ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, names.ToString(), "These files will be lost permanently, they can not be recovered!") == MyMessageBoxResult.Yes && Functions.ConfirmRiskyItems(destroy_list_data.Select(x => x.FullPath), "destroy"))
                {
                    List<FileInfo> files = new List<FileInfo>();
                    foreach (var i in destroy_list_data)
                        files.Add(new FileInfo(i.FullPath));
                    Operation operation = new Operation();
                    operation.StartSafetyDestroyFiles(files, method, repete, delete, true);
                    HashSet<string> finished = new HashSet<string>(operation.done_files, StringComparer.OrdinalIgnoreCase);
                    destroy_list_data.RemoveAll(x => finished.Contains(x.FullPath) || !File.Exists(x.FullPath));
                    files_list.Items.Refresh();
                    UpdateButtons();
                    long not_done = files.Count - operation.CompletedCount;
                    string notes = operation.result_notes.Count > 0 ? "\nNotes:\n" + string.Join("\n", operation.result_notes.ToArray()) : "";
                    string done_text = delete ? "destroyed and deleted" : "overwritten (kept with their names and sizes)";
                    if (operation.result_errors.Count > 0 || not_done > 0)
                        Messages.ShowMessage(string.Format("{0} of {1} file{2} {3}.", operation.CompletedCount, files.Count, files.Count > 1 ? "s" : "", done_text), ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, (operation.Stopped ? "The operation was stopped, the files that were not finished keep their names and are still in the list.\n" : "") + (not_done > 0 ? not_done + " file(s) were not finished or failed, they are still in the list if they exist.\n" : "") + string.Join("\n", operation.result_errors.ToArray()) + notes);
                    else
                        Messages.ShowMessage("Destroying done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, string.Format("{0} file{1} {2}.", files.Count, files.Count > 1 ? "s" : "", done_text) + notes);
                }
            }
            else
                UpdateButtons();
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (adding || files_list.SelectedItems.Count == 0)
                return;
            HashSet<DestroyListView> selected = new HashSet<DestroyListView>(files_list.SelectedItems.Cast<DestroyListView>());
            destroy_list_data.RemoveAll(x => selected.Contains(x));
            files_list.Items.Refresh();
            UpdateButtons();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            if (adding)
                return;
            destroy_list_data.Clear();
            files_list.Items.Refresh();
            UpdateButtons();
        }

        private void FilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtons();
        }

        DestroyListView SelectedFile()
        {
            return files_list.SelectedItems.Count == 1 ? files_list.SelectedItem as DestroyListView : null;
        }

        private void FilesList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && files_list.SelectedItems.Count > 0)
            {
                RemoveSelected_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Space && SelectedFile() != null)
            {
                SafetyTools.PreviewItem(SelectedFile().FullPath);
                e.Handled = true;
            }
        }

        private void FilesList_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            int selected = files_list.SelectedItems.Count;
            string one = selected == 0 ? "Select a file first." : selected > 1 ? "Select only one file." : null;
            SafetyTools.SetState(menu_open_location, one == null, one);
            SafetyTools.SetState(menu_preview, one == null, one);
            SafetyTools.SetState(menu_copy_path, selected > 0, "Select a file first.");
            SafetyTools.SetState(menu_remove, selected > 0 && !adding, adding ? "Please wait, files are being added." : "Select a file first.");
        }

        private void MenuOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile() != null)
                SafetyTools.OpenLocation(SelectedFile().FullPath);
        }

        private void MenuPreview_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedFile() != null)
                SafetyTools.PreviewItem(SelectedFile().FullPath);
        }

        private void MenuCopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (files_list.SelectedItems.Count > 0)
                SafetyTools.CopyPaths(files_list.SelectedItems.Cast<DestroyListView>().Select(x => x.FullPath));
        }

        #endregion

        #region Destroy Disk

        DriveInfo[] drivers;

        private void GetDrivers()
        {
            drive_list.Items.Clear();
            BackgroundWorker get_drivers = new BackgroundWorker();
            drive_list.IsEnabled = refresh.IsEnabled = false;
            UpdateDiskButton();
            get_drivers.DoWork += (object senderw, DoWorkEventArgs ew) => { try { drivers = DriveInfo.GetDrives(); } catch (Exception) { } };
            get_drivers.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                drive_list.Items.Clear();
                int select_index = -1;
                if (drivers != null)
                    for (int i = 0; i < drivers.Length; i++)
                        try
                        {
                            if (drivers[i].DriveType != DriveType.CDRom && drivers[i].DriveType != DriveType.Network && drivers[i].IsReady && drivers[i].TotalFreeSpace > 0)
                            {
                                if (selectdrive != null && drivers[i].Name.ToLower() == selectdrive.Name.ToLower())
                                    select_index = drive_list.Items.Count;
                                drive_list.Items.Add(drivers[i].Name);
                            }
                        }
                        catch (Exception) { }
                drive_list.IsEnabled = refresh.IsEnabled = true;
                drive_list.SelectedIndex = select_index;
                UpdateDiskButton();
            };
            get_drivers.RunWorkerAsync();
        }

        private void DriveList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDiskButton();
            if (!starting_window)
                if (drive_list.SelectedIndex > -1)
                {
                    DriveInfo drive = new DriveInfo(drive_list.SelectedItem.ToString());
                    if (drive.IsReady)
                    {
                        drive_icon.Source = Functions.GetDriveImage(drive, 256);
                        drive_name.Text = Functions.GetDriveDescription(drive);
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
                    bool system_drive = drive.Name.ToLower() == Functions.SystemDrive;
                    if (Messages.ShowMessage("Are you sure you want to destroy all deleted files in '" + drive.Name + "'?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "The free space of the drive is filled with temporary files in the folder '" + System.IO.Path.Combine(drive.Name, Operation.WipeFolderName) + "', they are deleted when the operation finishes or is stopped." + (system_drive ? "\n2 GiB are kept free on the system drive." : "") + "\nMethod: " + SafetyFileShredder.MethodDescription((DestroyMethod)(disk_method_cb.SelectedIndex + 1)) + "\n\n" + SafetyFileShredder.Caveat, "Deleted files will be unrecoverable!") == MyMessageBoxResult.Yes)
                    {
                        if (system_drive && Messages.ShowMessage("Warning: this is the system drive.", "Filling the free space of the drive Windows runs from can slow down or disturb Windows and other programs until the operation finishes.", new ButtonContent("Cancel (recommended)", ModrenMessageBitmapImage.Cancel, 1), new ButtonContent("Continue at my own risk", ModrenMessageBitmapImage.Default, 2)) != MyMessageBoxResult.Button2)
                            return;
                        DestroyMethod method;
                        method = (DestroyMethod)(disk_method_cb.SelectedIndex + 1);
                        Operation operation = new Operation();
                        operation.StartSafetyDestroyDisk(drive, method, true);
                        if (operation.result_errors.Count > 0)
                            Messages.ShowMessage("Destroying finished with problems.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", operation.result_errors.ToArray()));
                        else if (operation.Stopped)
                            Messages.ShowMessage("Destroying was stopped, the temporary files were deleted.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                        else
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
            if (CurrentPage == DestroyPages.DiskDestroy)
            {
                current_page = DestroyPages.FilesDestroy;
                TabChange_MouseButtonUp(label2, null);
            }
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
            SafetyTools.SetWindowIcon(this, CurrentPage == DestroyPages.DiskDestroy ? "DestroyDisk" : "FileDestroyer");
            Title = CurrentPage == DestroyPages.DiskDestroy ? "Destroy Disk" : "Files Destroyer";
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
            AboutTool window = new AboutTool();
            window.tool_vertion.Text = "Version: 2.1";
            window.tool_date.Text = "";
            window.tool_name.Text = "Files destroyer";
            window.tool_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/files-destroyer.png"));
            window.ShowDialog();
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