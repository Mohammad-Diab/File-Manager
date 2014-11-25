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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Renamer.xaml
    /// </summary>
    public partial class Renamer : Window
    {
        Dictionary<string, bool> list1 = new Dictionary<string, bool>();
        Dictionary<string, bool> list2 = new Dictionary<string, bool>();
        ObservableCollection<RenamerListView> list_to_rename = new ObservableCollection<RenamerListView>();
        ObservableCollection<RenamerListView> list_to_rename2 = new ObservableCollection<RenamerListView>();

        public Renamer()
        {
            InitializeComponent();
            FilesList.DataContext = list_to_rename;
            FilesList2.DataContext = list_to_rename2;
        }

        void AddFiles(ObservableCollection<RenamerListView> add_in, string items_count, Dictionary<string, bool> add_in_dictionary)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                foreach (string i in OFD.FileNames)
                {
                    if (!add_in_dictionary.ContainsKey(i))
                    {
                        add_in.Add(new RenamerListView(new FileInfo(i)));
                        add_in_dictionary.Add(i, true);
                        items_count = add_in_dictionary.Count + (add_in_dictionary.Count > 1 ? " Item" : " Items");
                    }
                }
        }

        void AddDirectory(ObservableCollection<RenamerListView> add_in, string items_count, Grid rename_control, Dictionary<string, bool> add_in_dictionary)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                rename_control.IsEnabled = false;
                BackgroundWorker add_all_files_in_dir_bw = new BackgroundWorker();
                add_all_files_in_dir_bw.WorkerSupportsCancellation = true;
                add_all_files_in_dir_bw.WorkerReportsProgress = true;
                add_all_files_in_dir_bw.DoWork += (object sender, DoWorkEventArgs e) => { GetAllFiles(new DirectoryInfo(e.Argument.ToString()), add_in_dictionary, (BackgroundWorker)sender); };
                add_all_files_in_dir_bw.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    FileInfo i = (FileInfo)e.UserState;
                    try
                    { add_in.Add(new RenamerListView(i)); }
                    catch (Exception) { }
                };
                add_all_files_in_dir_bw.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    rename_control.IsEnabled = true;
                    if (add_in_dictionary.Count > 0)
                        items_count = add_in_dictionary.Count + (add_in_dictionary.Count > 1 ? " Items" : " Item");
                    else
                        items_count = "";
                };
                add_all_files_in_dir_bw.RunWorkerAsync(path);
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
            AddFiles(list_to_rename, items_count.Text, list1);
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)//cheack if item is in list or not
        {
            AddDirectory(list_to_rename, items_count.Text, rename_control, list1);
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            ClearList(list1, list_to_rename, items_count.Text);
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            if (new My_MessageBox("Confirmation", "Are you sure you want to rename all files in list?", "Caution: you can not reverse this operation!", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
            {
                List<FileOperationItem> to_rename = new List<FileOperationItem>();
                foreach (var i in list_to_rename)
                    to_rename.Add(new FileOperationItem(i.File, false, (remove_extension.IsChecked.Value ? i.File.Name.Substring(0, i.File.Name.Length - i.File.Extension.Length) : i.File.Name) + (extension.Text.Length > 0 ? ('.' + extension.Text) : "")));
                new Operation().CopyDialogShow(to_rename, Op.Rename, "", true);
                ClearList(list1, list_to_rename, items_count.Text);
            }
        }

        private void Extension_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (extension.Text.Contains("*") || extension.Text.Contains("/") || extension.Text.Contains("\\") || extension.Text.Contains("\"") || extension.Text.Contains("?") || extension.Text.Contains("<") || extension.Text.Contains(">") || extension.Text.Contains("|") || extension.Text.Contains("."))
            {
                extension.Text = extension.Text.Replace("*", "").Replace("/", "").Replace("\\", "").Replace("\"", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "").Replace(".", "");
                new My_MessageBox("FILE MANAGER", "File extension can't contain any of these characters:\n\\ / : * ? \" < > | .", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            RemoveItems(FilesList.SelectedItems, list_to_rename, list1, items_count.Text);
        }

        #endregion


        #region Suggest best possible extension

        private void AddFile2_Click(object sender, RoutedEventArgs e)
        {
            AddFiles(list_to_rename2, items_count2.Text, list2);
        }

        private void AddFolder2_Click(object sender, RoutedEventArgs e)
        {
            AddDirectory(list_to_rename2, items_count2.Text, rename2_control, list2);
        }

        private void RemoveSelected2_Click(object sender, RoutedEventArgs e)
        {
            RemoveItems(FilesList2.SelectedItems, list_to_rename2, list2, items_count2.Text);
        }

        private void ClearList2_Click(object sender, RoutedEventArgs e)
        {
            ClearList(list2, list_to_rename2, items_count2.Text);
        }

        private void Rename2_Click(object sender, RoutedEventArgs e)
        {
            if (new My_MessageBox("Confirmation", "Are you sure you want to rename all files in list?", "Caution: you can not reverse this operation!", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                Suggest_Click(null, null);
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
            rename2_control.IsEnabled = false;
            rename2_progressbar.Value = 0;
            rename2_progressbar.Visibility = Visibility.Visible;
        }

        private void SuggestBW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker this_bw = ((BackgroundWorker)sender);
            Parallel.For(0, list_to_rename2.Count, (i) =>//for (int i = 0; i < list_to_rename2.Count; i++)
            {
                if (!list_to_rename2[i].Done && list_to_rename2[i].PossibleExt.Count == 0 && list_to_rename2[i].File.Exists)
                {
                    try
                    {
                        BinaryReader BR = new BinaryReader(list_to_rename2[i].File.OpenRead());
                        List<byte> file_bytes = new List<byte>();
                        if (list_to_rename2[i].File.Length > 12)
                        {
                            file_bytes = BR.ReadBytes(12).ToList<byte>();
                            if (IsISO(list_to_rename2[i], this_bw))
                                this_bw.ReportProgress(i, "iso");
                            else if (IsWave(list_to_rename2[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "wav");
                            else if (IsAVI(list_to_rename2[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "avi");
                            else if (Is3GP(list_to_rename2[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "3gp");
                            else if (IsMP4(list_to_rename2[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "mp4");
                            else if (IsWebM(list_to_rename2[i], this_bw, file_bytes))
                                this_bw.ReportProgress(i, "webm");
                            else if (IsDOC(list_to_rename2[i], this_bw))
                                this_bw.ReportProgress(i, "doc");
                            else if (IsXLS(list_to_rename2[i], this_bw))
                                this_bw.ReportProgress(i, "xls");
                            else
                            {
                                foreach (var j in FileInfoCombonents.FilesInformation)
                                    foreach (var l in j.Value.IDs)
                                        if (list_to_rename2[i].File.Length >= l.Length + j.Value.Offset)
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
                                if (!list_to_rename2[i].PossibleExt.Contains("doc"))
                                    if (IsPPT(list_to_rename2[i], this_bw))
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
                list_to_rename2[e.ProgressPercentage].PossibleExt.Add(e.UserState.ToString());
            else
                rename2_progressbar.Value += 1000.0 / list_to_rename2.Count;
        }

        private void SuggestBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var i in list_to_rename2)
            {
                if (i.PossibleExt.Count == 0)
                    i.PossibleExt.Add("none");
                if (i.PossibleExt.Count > 0 && !i.Done && i.SelectedExt < 0)
                    i.SelectedExt = 0;
                i.Done = true;
            }
            rename2_progressbar.Visibility = Visibility.Hidden;
            rename2_control.IsEnabled = true;
            FilesList2.Items.Refresh();
        }

        private void SuggestAndRename_bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SuggestBW_RunWorkerCompleted(null, null);
            List<FileOperationItem> to_rename = new List<FileOperationItem>();
            foreach (var i in list_to_rename2)
                if (i.PossibleExt.Count > i.SelectedExt && i.Done && i.PossibleExt[i.SelectedExt] != "none" && i.File.Exists && ("." + i.PossibleExt[i.SelectedExt]) != i.File.Extension)
                    to_rename.Add(new FileOperationItem(i.File, false, (remove_extension2.IsChecked.Value ? i.File.Name.Substring(0, i.File.Name.Length - i.File.Extension.Length) : i.File.Name) + (i.PossibleExt[i.SelectedExt].Length > 0 ? (i.PossibleExt[i.SelectedExt]) : "")));
            new Operation().CopyDialogShow(to_rename, Op.Rename, "", true);
            ClearList(list2, list_to_rename2, items_count2.Text);
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
}


/*
        bool done;
        public Boolean Done
        {
            get { return done; }
            set
            {
                if (value)
                {
                    status_img = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/tick.png"));
                    status_txt = "Done";
                    status_color = Brushes.Green;
                }
                else
                {
                    status_img = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/error.png"));
                    status_txt = "Error";
                    status_color = Brushes.Red;
                }
                done = value;
            }
        }

        BitmapImage status_img;
        private BitmapImage StatusImg { get { return status_img; } set { status_img = value; } }

        private string status_txt;
        public String StatusTxt { get { return status_txt; } set { status_txt = value; } }

        Brush status_color;
        public Brush StatusColor { get { return status_color; } set { status_color = value; } }
 * */


//private ListViewItem MakeItemFile(FileInfo file)
//{
//    int file_index = 0;
//    file_index = Functions.GetFileIndex(file.Extension);

//    Image image = new Image();
//    image.HorizontalAlignment = HorizontalAlignment.Left;
//    image.VerticalAlignment = VerticalAlignment.Top;
//    image.Width = image.Height = 24;
//    image.Margin = margins[0];
//    image.Source = FileInformation.file_bitmap_24[file_index];

//    TextBlock tx1 = new TextBlock();
//    tx1.VerticalAlignment = VerticalAlignment.Top;
//    tx1.FontSize = 14;
//    tx1.Margin = margins[1];
//    tx1.TextTrimming = TextTrimming.CharacterEllipsis;
//    tx1.Text = file.FullName;

//    //TextBlock tx2 = new TextBlock();
//    //tx2.VerticalAlignment = VerticalAlignment.Top;
//    //tx2.FontSize = 14;
//    //tx2.Margin = margins[3];
//    //tx2.TextTrimming = TextTrimming.CharacterEllipsis;
//    //tx2.Text = "Old Extension: '" + file.Extension.ToUpper() + "'";

//    //TextBlock tx3 = new TextBlock();
//    //tx3.VerticalAlignment = VerticalAlignment.Top;
//    //tx3.FontSize = 14;
//    //tx3.Margin = margins[4];
//    //tx3.TextTrimming = TextTrimming.CharacterEllipsis;
//    //tx3.Text = "Type: " + Functions.GetFileType(file_index, file.Extension);

//    //Rectangle separator = new Rectangle();
//    //separator.Fill = Brushes.Silver;
//    //separator.Height = 1;
//    //separator.Margin = margins[5];
//    //separator.VerticalAlignment = VerticalAlignment.Bottom;

//    Grid container = new Grid();
//    container.Height = 30;
//    container.Width = FilesList.ActualWidth - 40;
//    container.Children.Add(image);
//    container.Children.Add(tx1);
//    //container.Children.Add(tx2);
//    //container.Children.Add(tx3);
//    //container.Children.Add(separator);

//    ListViewItem LVI = new ListViewItem();
//    LVI.ToolTip = file.FullName;
//    LVI.Tag = file;
//    LVI.ContentStringFormat = file.FullName;
//    LVI.Content = container;

//    return LVI;
//}



//    Parallel.ForEach(list_to_rename2, (i) =>
//    {
//        if (i.File.Length > 0)
//        {
//            FileStream fs = i.File.OpenRead();
//            int byt = fs.ReadByte();
//            switch (byt)
//            {
//                case 0:
//                    if (i.File.Length >= 4)
//                    {
//                        if (fs.ReadByte() == 0)
//                        {
//                            byt = fs.ReadByte();
//                            switch (byt)
//                            {
//                                case 0:
//                                    if (fs.ReadByte() == 0x18)
//                                        if (i.File.Length > 10 && fs.ReadByte() == 0x66 && fs.ReadByte() == 0x74 && fs.ReadByte() == 0x79 && fs.ReadByte() == 0x70 && fs.ReadByte() == 0x4D && fs.ReadByte() == 0x34 && fs.ReadByte() == 0x41)
//                                        { i.PossibleExt.Add("aac"); i.PossibleExt.Add("m4a"); }
//                                        else if (fs.ReadByte() == 0x1C)
//                                            if (i.File.Length > 10 && fs.ReadByte() == 0x66 && fs.ReadByte() == 0x74 && fs.ReadByte() == 0x79 && fs.ReadByte() == 0x70 && fs.ReadByte() == 0x4D && fs.ReadByte() == 0x34 && fs.ReadByte() == 0x41)
//                                            { i.PossibleExt.Add("aac"); i.PossibleExt.Add("m4a"); }
//                                    break;
//                                case 1:
//                                    break;
//                                case 2:
//                                    break;
//                                default:
//                                    break;
//                            }
//                        }
//                    }
//                    break;
//                case 66:
//                    break;
//                case 73:
//                    break;
//                case 77:
//                    break;
//                case 82:
//                    break;
//                default:
//                    break;

//            }
//        }
//    });