using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
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
        bool starting_window = true, CloseWindow = false;

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

        Dictionary<string, bool> list1 = new Dictionary<string, bool>();
        Dictionary<string, bool> list2 = new Dictionary<string, bool>();
        ObservableCollection<RenamerListView> renamer_data = new ObservableCollection<RenamerListView>();
        ObservableCollection<RenamerListView> suggester_data = new ObservableCollection<RenamerListView>();

        public Renamer()
        {
            InitializeComponent();
        }

        void BrowseForFiles(string title, ObservableCollection<RenamerListView> add_in, string items_count, Dictionary<string, bool> add_in_dictionary)
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

        void AddFiles(ObservableCollection<RenamerListView> add_in, string items_count, Dictionary<string, bool> add_in_dictionary, List<string> list)
        {
            foreach (string i in list)
            {
                if (!add_in_dictionary.ContainsKey(i))
                {
                    add_in.Add(new RenamerListView(new FileInfo(i)));
                    add_in_dictionary.Add(i, true);
                }
            }
            if (add_in_dictionary.Count > 0)
                items_count = add_in_dictionary.Count + (add_in_dictionary.Count > 1 ? " Items" : " Item");
            else
                items_count = "";
        }

        void AddDirectory(ObservableCollection<RenamerListView> add_in, string items_count, Dictionary<string, bool> add_in_dictionary)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Add all files in this directory.";
            sfd.AddExtension = false;

            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                string path = sfd.SelectedItem;
                main_grid.IsEnabled = false;
                BackgroundWorker add_all_files_in_dir_bw = new BackgroundWorker();
                add_all_files_in_dir_bw.WorkerSupportsCancellation = true;
                add_all_files_in_dir_bw.WorkerReportsProgress = true;
                add_all_files_in_dir_bw.DoWork += (object sender, DoWorkEventArgs e) => { GetAllFiles(new DirectoryInfo(path), add_in_dictionary, (BackgroundWorker)sender); };
                add_all_files_in_dir_bw.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    FileInfo i = (FileInfo)e.UserState;
                    try
                    { add_in.Add(new RenamerListView(i)); }
                    catch (Exception) { }
                };
                add_all_files_in_dir_bw.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    main_grid.IsEnabled = true;
                    if (add_in_dictionary.Count > 0)
                        items_count = add_in_dictionary.Count + (add_in_dictionary.Count > 1 ? " Items" : " Item");
                    else
                        items_count = "";
                };
                add_all_files_in_dir_bw.RunWorkerAsync();
            }
        }

        private void GetAllFiles(DirectoryInfo dir, Dictionary<string, bool> list, BackgroundWorker bw)
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
                    if (!list.ContainsKey(i.FullName))
                    {
                        list.Add(i.FullName, true);
                        bw.ReportProgress(0, i);
                    }
            if (sub_dirs == null)
                return;
            else
                foreach (var i in sub_dirs)
                    GetAllFiles(i, list, bw);
        }

        void ClearList(Dictionary<string, bool> l1, ObservableCollection<RenamerListView> l2, string t)
        {
            l1.Clear();
            l2.Clear(); 
            t = "";
        }

        private void RemoveItems(IList selected_list, ObservableCollection<RenamerListView> remove_from, Dictionary<string, bool> list1, string p)
        {
            for (int i = selected_list.Count - 1; i >= 0; i--)
            {
                list1.Remove((selected_list[i] as RenamerListView).FullPath);
                remove_from.Remove(selected_list[i] as RenamerListView);
            }
            if (list1.Count > 0)
                p = list1.Count + (list1.Count == 1 ? " Item" : " Items");
            else
                p = string.Empty;
        }


        #region Extension Changer

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFiles("Select files to change thier extentions...", renamer_data, items_count.Text, list1);
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)//cheack if item is in list or not
        {
            AddDirectory(renamer_data, items_count.Text, list1);
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            ClearList(list1, renamer_data, items_count.Text);
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (renamer_data.Count > 0)
            {
                bool accept = false;
                if (extension.Text.Length == 0 && remove_extension.IsChecked.Value)
                    accept = Messages.ShowMessage("Are you sure you want to rename all files in list?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, extension.Text.Length == 0, "You have not enter any extention so we will anly remove old extention.", "Caution: you can not reverse this operation!") == MyMessageBoxResult.Yes;
                else if (extension.Text.Length == 0 && !remove_extension.IsChecked.Value)
                {
                    Messages.ShowMessage("Extension field seems empty.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please enter extention first or check 'Remove old extention'.");
                    accept = false;
                }
                else
                    accept = Messages.ShowMessage("Are you sure you want to rename all files in list?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "", "Caution: you can not reverse this operation!") == MyMessageBoxResult.Yes;
                if (accept)
                {
                    List<FileOperationItem> to_rename = new List<FileOperationItem>();
                    foreach (var i in renamer_data)
                        to_rename.Add(new FileOperationItem(i.File, false, (remove_extension.IsChecked.Value ? i.File.Name.Substring(0, i.File.Name.Length - i.File.Extension.Length) : i.File.Name) + (extension.Text.Length > 0 ? ('.' + extension.Text) : "")));
                    new Operation().CopyDialogShow(to_rename, OperationType.Rename, "", true);
                    ClearList(list1, renamer_data, items_count.Text);
                }
            }
            else
                Messages.ShowMessage("List is empty!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "First you have to add files then click 'Rename'");
        }

        private void Extension_TextChanged(object sender, TextChangedEventArgs e)
        {
            var InvalidChars = Path.GetInvalidFileNameChars();
            if (extension.Text.Any(x => InvalidChars.Any(y => y == x)))
            {
                foreach (char i in InvalidChars)
                    extension.Text = extension.Text.Replace(i.ToString(), "");
                Messages.ShowMessage("File extension has some invalid characters.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Format("File extension can't contain any of these characters:\n" + "'\"' ' < ' ' > ' ' | ' ':' ' * ' ' ? ' '\\' ' / '.\nAnd some other special chars:\n'\\t (tab)' '\\n (enter)' '\\r' '\\0' '\\a' '\\b' '\\v' '\\f'."));
            }
            else if(extension.Text.Contains('.'))
                {
                extension.Text.Replace(".", "");
                Messages.ShowMessage("File extension has some invalid characters.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "File extension can't contain dots '.'.");
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            RemoveItems(FilesList.SelectedItems, renamer_data, list1, items_count.Text);
        }

        #endregion


        #region Suggest best possible extension

        private void AddFile2_Click(object sender, RoutedEventArgs e)
        {
            BrowseForFiles("Select files...", suggester_data, items_count2.Text, list2);
        }

        private void AddFolder2_Click(object sender, RoutedEventArgs e)
        {
            AddDirectory(suggester_data, items_count2.Text, list2);
        }

        private void RemoveSelected2_Click(object sender, RoutedEventArgs e)
        {
            RemoveItems(FilesList2.SelectedItems, suggester_data, list2, items_count2.Text);
        }

        private void ClearList2_Click(object sender, RoutedEventArgs e)
        {
            ClearList(list2, suggester_data, items_count2.Text);
        }

        private void Rename2_Click(object sender, RoutedEventArgs e)
        {
            if (suggester_data.Count > 0)
            {
                if (Messages.ShowMessage("Are you sure you want to rename all files in list?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "", "Caution: you can not reverse this operation!") == MyMessageBoxResult.Yes)
                    Suggest_Click(null, null);
            }
            else
                Messages.ShowMessage("List is empty!", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "First you have to add files then click 'Rename'");
        }

        private void Suggest_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker suggest_bw = new BackgroundWorker();
            suggest_bw.DoWork += SuggestBW_DoWork;
            suggest_bw.ProgressChanged += SuggestBW_ProgressChanged;
            suggest_bw.WorkerSupportsCancellation = suggest_bw.WorkerReportsProgress = true;
            if (sender == null)
                suggest_bw.RunWorkerCompleted += SuggestAndRename_bw_RunWorkerCompleted;
            else
                suggest_bw.RunWorkerCompleted += SuggestBW_RunWorkerCompleted;
            suggest_bw.RunWorkerAsync();
            main_grid.IsEnabled = false;
            rename2_progressbar.Value = 0;
            rename2_progressbar.Visibility = Visibility.Visible;
        }

        private void SuggestBW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker this_bw = ((BackgroundWorker)sender);
            Parallel.For(0, suggester_data.Count, (i) =>//for (int i = 0; i < list_to_rename2.Count; i++)
            {
                if (!suggester_data[i].Done && suggester_data[i].PossibleExt.Count == 0 && suggester_data[i].File.Exists)
                {
                    try
                    {
                        BinaryReader BR = new BinaryReader(suggester_data[i].File.OpenRead());
                        List<byte> file_bytes = new List<byte>();
                        if (suggester_data[i].File.Length > 12)
                        {
                            file_bytes = BR.ReadBytes(12).ToList<byte>();
                            if (IsISO(suggester_data[i], this_bw))
                                this_bw.ReportProgress(i, "iso");
                            else if (IsWave(suggester_data[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "wav");
                            else if (IsAVI(suggester_data[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "avi");
                            else if (Is3GP(suggester_data[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "3gp");
                            else if (IsMP4(suggester_data[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "mp4");
                            else if (IsWebM(suggester_data[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "webm");
                            else if (IsDOC(suggester_data[i], this_bw))
                                this_bw.ReportProgress(i, "doc");
                            else if (IsXLS(suggester_data[i], this_bw))
                                this_bw.ReportProgress(i, "xls");
                            else
                            {
                                foreach (var j in FileInfoCombonents.FilesInformation)
                                    foreach (var l in j.Value.IDs)
                                        if (suggester_data[i].File.Length >= l.Length + j.Value.Offset)
                                        {
                                            try
                                            {
                                                int k;
                                                while (l.Length + j.Value.Offset > file_bytes.Count)
                                                    file_bytes.Add(BR.ReadByte());
                                                for (k = j.Value.Offset; k < l.Length; k++)
                                                    if (l[k] != file_bytes[k])
                                                        break;
                                                if (k == l.Length)
                                                {
                                                    this_bw.ReportProgress(i, j.Value.Extention);
                                                    break;
                                                }
                                            }
                                            catch (Exception) { }
                                        }
                                if (!suggester_data[i].PossibleExt.Contains("doc"))
                                    if (IsPPT(suggester_data[i], this_bw))
                                    {
                                        this_bw.ReportProgress(i, "ppt");
                                        this_bw.ReportProgress(i, "pps");
                                    }
                            }
                        }
                    }
                    catch (Exception) { }
                }
                this_bw.ReportProgress(-1);
            });
        }

        private void SuggestBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > -1)
                suggester_data[e.ProgressPercentage].PossibleExt.Add(e.UserState.ToString());
            else
                rename2_progressbar.Value += 1000.0 / suggester_data.Count;
        }

        private void SuggestBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var i in suggester_data)
            {
                if (i.PossibleExt.Count == 0)
                    i.PossibleExt.Add("none");
                if (i.PossibleExt.Count > 0 && !i.Done && i.SelectedExt < 0)
                    i.SelectedExt = 0;
                i.Done = true;
            }
            rename2_progressbar.Visibility = Visibility.Hidden;
            main_grid.IsEnabled = true;
            FilesList2.Items.Refresh();
        }

        private void SuggestAndRename_bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SuggestBW_RunWorkerCompleted(null, null);
            List<FileOperationItem> to_rename = new List<FileOperationItem>();
            foreach (var i in suggester_data)
                if (i.PossibleExt.Count > i.SelectedExt && i.Done && i.PossibleExt[i.SelectedExt] != "none" && i.File.Exists && ("." + i.PossibleExt[i.SelectedExt]) != i.File.Extension)
                    to_rename.Add(new FileOperationItem(i.File, false, (remove_extension2.IsChecked.Value ? i.File.Name.Substring(0, i.File.Name.Length - i.File.Extension.Length) : i.File.Name) + (i.PossibleExt[i.SelectedExt].Length > 0 ? (i.PossibleExt[i.SelectedExt]) : "")));
            new Operation().CopyDialogShow(to_rename, OperationType.Rename, "", true);
            ClearList(list2, suggester_data, items_count2.Text);
        }

        private bool IsXLS(RenamerListView i, BackgroundWorker this_bw)
        {
            if (i.File.Length > 1043)
            {
                BinaryReader BR = new BinaryReader(i.File.OpenRead());
                BR.BaseStream.Position = 545;
                if (BR.ReadByte() == 0x52 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x6F && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x6F && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x74 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x20 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x45 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x6E && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x74 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x72 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x79)
                    return true;
            }
            return false;
        }

        private bool IsDOC(RenamerListView i, BackgroundWorker this_bw)
        {
            if (i.File.Length > 550)
            {
                BinaryReader BR = new BinaryReader(i.File.OpenRead());
                BR.BaseStream.Position = 545;
                if (BR.ReadByte() == 0x62 && BR.ReadByte() == 0x6A && BR.ReadByte() == 0x62 && BR.ReadByte() == 0x6A)
                    return true;
            }
            return false;
        }

        private bool IsWebM(RenamerListView i, BackgroundWorker this_bw, List<byte> file_bytes)
        {
            if (i.File.Length > 35)
                if (file_bytes[0] == 0x1A && file_bytes[1] == 0x45 && file_bytes[2] == 0xDF && file_bytes[3] == 0xA3)
                {
                    BinaryReader BR = new BinaryReader(i.File.OpenRead());
                    BR.BaseStream.Position = 28;
                    if (BR.ReadByte() == 0x42 && BR.ReadByte() == 0x82 && BR.ReadByte() <= 0xFF && BR.ReadByte() == 0x77 && BR.ReadByte() == 0x65 && BR.ReadByte() == 0x62 && BR.ReadByte() == 0x6D)
                        return true;
                }
            return false;
        }

        private bool IsMP4(RenamerListView i, BackgroundWorker this_bw, List<byte> file_bytes)
        {
            if (file_bytes[4] == 0x66 && file_bytes[5] == 0x74 && file_bytes[6] == 0x79 && file_bytes[7] == 0x70 && file_bytes[8] == 0x46 && file_bytes[9] == 0x41 && file_bytes[10] == 0x43 && file_bytes[11] == 0x45)
                return true;
            else if (file_bytes[4] == 0x66 && file_bytes[5] == 0x74 && file_bytes[6] == 0x79 && file_bytes[7] == 0x70 && file_bytes[8] == 0x6D && file_bytes[9] == 0x70 && file_bytes[10] == 0x34 && file_bytes[11] == 0x32)
                return true;
            else if (file_bytes[4] == 0x66 && file_bytes[5] == 0x74 && file_bytes[6] == 0x79 && file_bytes[7] == 0x70 && file_bytes[8] == 0x69 && file_bytes[9] == 0x73 && file_bytes[10] == 0x6F && file_bytes[11] == 0x6D)
                return true;
            else if (file_bytes[4] == 0x66 && file_bytes[5] == 0x74 && file_bytes[6] == 0x79 && file_bytes[7] == 0x70 && file_bytes[8] == 0x64 && file_bytes[9] == 0x61 && file_bytes[10] == 0x73 && file_bytes[11] == 0x68)
                if (i.File.Length > 27)
                {
                    BinaryReader BR = new BinaryReader(i.File.OpenRead());
                    BR.BaseStream.Position = 20;
                    if (BR.ReadByte() == 0x61 && BR.ReadByte() == 0x76 && BR.ReadByte() == 0x63 && BR.ReadByte() == 0x31 && BR.ReadByte() == 0x6D && BR.ReadByte() == 0x70 && BR.ReadByte() == 0x34)
                        return true;
                }
            return false;
        }

        private bool Is3GP(RenamerListView i, BackgroundWorker this_bw, List<byte> file_bytes)
        {
            if (file_bytes[4] == 0x66 && file_bytes[5] == 0x74 && file_bytes[6] == 0x79 && file_bytes[7] == 0x70 && file_bytes[8] == 0x33 && file_bytes[9] == 0x67 && file_bytes[10] == 0x70)
                return true;
            return false;
        }

        private bool IsAVI(RenamerListView i, BackgroundWorker this_bw, List<byte> file_bytes)
        {
            if (file_bytes[0] == 0x52 && file_bytes[1] == 0x49 && file_bytes[2] == 0x46 && file_bytes[3] == 0x46 && file_bytes[8] == 0x41 && file_bytes[9] == 0x56 && file_bytes[10] == 0x49)
                return true;
            return false;
        }

        private bool IsPPT(RenamerListView i, BackgroundWorker sender)
        {
            if (i.File.Length > 44)
            {
                BinaryReader BR = new BinaryReader(i.File.OpenRead());
                if (BR.ReadByte() == 0xD0 && BR.ReadByte() == 0xCF && BR.ReadByte() == 0x11 && BR.ReadByte() == 0xE0 && BR.ReadByte() == 0xA1 && BR.ReadByte() == 0xB1 && BR.ReadByte() == 0x1A && BR.ReadByte() == 0xE1 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x3E && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x03 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0xFE && BR.ReadByte() == 0xFF && BR.ReadByte() == 0x09 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x06 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00)
                    return true;
            }
            return false;
        }

        private bool IsWave(RenamerListView i, BackgroundWorker sender, List<byte> file_bytes)
        {
            if (file_bytes[0] == 0x52 && file_bytes[1] == 0x49 && file_bytes[2] == 0x46 && file_bytes[3] == 0x46 && file_bytes[8] == 0x57 && file_bytes[9] == 0x41 && file_bytes[10] == 0x56 && file_bytes[11] == 0x45)
                return true;
            return false;
        }

        private bool IsISO(RenamerListView i, BackgroundWorker sender)
        {
            if (i.File.Length > 32776)
            {
                BinaryReader BR = new BinaryReader(i.File.OpenRead());
                BR.BaseStream.Position = 32768;
                if (BR.ReadByte() == 0x01 && BR.ReadByte() == 0x43 && BR.ReadByte() == 0x44 && BR.ReadByte() == 0x30 && BR.ReadByte() == 0x30 && BR.ReadByte() == 0x31 && BR.ReadByte() == 0x01 && BR.ReadByte() == 0x00)
                    return true;
            }
            return false;
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
            FilesList.DataContext = renamer_data;
            FilesList2.DataContext = suggester_data;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        internal bool ShowWindow(List<string> files,string Extention, ExtentionChanger type, bool showDialog)
        {
            CurrentPage = type;
            if (type == ExtentionChanger.ChangeExtention)
            {
                AddFiles(renamer_data, items_count.Text, list1, files);
                extension.Text = Extention;
            }
            else
            {
                AddFiles(suggester_data, items_count2.Text, list2, files);
                Suggest_Click(suggest_button, null);
            }
            
            if (showDialog)
                ShowDialog();
            else
                Show();
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
            done = false;
        }

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
}