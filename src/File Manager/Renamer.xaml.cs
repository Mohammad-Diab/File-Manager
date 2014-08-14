using System;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Renamer.xaml
    /// </summary>
    public partial class Renamer : Window
    {
        //Thickness[] margins = { new Thickness(10, 10, 0, 0), new Thickness(31, 3, 0, 0), new Thickness(110, 10, 10, 0), new Thickness(110, 34, 10, 0), new Thickness(110, 58, 11, 0), new Thickness(10, 0, 10, 3) };
        Dictionary<string, byte[]> files_identify = new Dictionary<string, byte[]>();
        public TimeSpan timespan = new TimeSpan(10);
        List<FileInfo> Undone;
        bool? remove_old_extension = true;
        string new_extension;
        List<FileIDInfo> list_of_FilesID;
        ObservableCollection<RenamerListView> list_to_rename = new ObservableCollection<RenamerListView>();
        ObservableCollection<Renamer2ListView> list_to_rename2 = new ObservableCollection<Renamer2ListView>();
        Thickness[] margins = { new Thickness(10, 3, 0, 0), new Thickness(40, 5, 10, 0) };
        public Renamer()
        {
            InitializeComponent();
            FilesList.DataContext = list_to_rename;
            FilesList2.DataContext = list_to_rename2;
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            items_count.Text = list_to_rename.Count + (list_to_rename.Count > 1 ? " Items" : "item");
        }

        void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileInfo i = (FileInfo)e.UserState;
            try
            { list_to_rename.Add(new RenamerListView(i.FullName, FileInformation.file_bitmap_24[Functions.GetFileIndex(i.Extension)], i)); }
            catch (Exception) { }
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            GetAllFiles(e.Argument.ToString(), (BackgroundWorker)sender);
        }

        private void GetAllFiles(string dir, BackgroundWorker bw)
        {
            string[] sub_dirs = null;
            try { sub_dirs = Directory.GetDirectories(dir); }
            catch (Exception) { }
            DirectoryInfo directory = new DirectoryInfo(dir);
            FileInfo[] files = null;
            try { files = directory.GetFiles(); }
            catch (Exception) { }
            if (files != null)
                foreach (var i in files)
                {
                    bw.ReportProgress(0, i);
                    System.Threading.Thread.Sleep(timespan);
                }
            //FilesList.Items.Add(MakeItemFile(i));
            if (sub_dirs == null)
                return;
            else
                foreach (var i in sub_dirs)
                    GetAllFiles(i, bw);
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog OFD = new Microsoft.Win32.OpenFileDialog();
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == true)
                foreach (string i in OFD.FileNames)
                {
                    //FilesList.Items.Add(MakeItemFile(new FileInfo(i)));
                    //if (list_to_rename.Contains(i))
                    //{
                    FileInfo file = new FileInfo(i);
                    list_to_rename.Add(new RenamerListView(i, FileInformation.file_bitmap_24[Functions.GetFileIndex(file.Extension)], file));
                    items_count.Text = list_to_rename.Count + (list_to_rename.Count > 1 ? " Item" : "items");
                    //}
                }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)//cheack if item is in list or not
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                BackgroundWorker add_all_files_in_dir_bw = new BackgroundWorker();
                add_all_files_in_dir_bw.WorkerSupportsCancellation = true;
                add_all_files_in_dir_bw.WorkerReportsProgress = true;
                add_all_files_in_dir_bw.DoWork += bw_DoWork;
                add_all_files_in_dir_bw.ProgressChanged += bw_ProgressChanged;
                add_all_files_in_dir_bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                add_all_files_in_dir_bw.RunWorkerAsync(path);
            }
            //GetAllFiles(path);
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            list_to_rename.Clear();
            items_count.Text = "";
            //FilesList.Items.Clear();
        }

        private void Rename_Click(object sender, RoutedEventArgs e)//Move To Thread and pocesspar to background worker 
        {
            if (new My_MessageBox("Confirmation", "Are you sure you want to rename all files in list?", "Caution: you can not reverse this operation or cancel it!", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
            {
                Undone = new List<FileInfo>();
                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerSupportsCancellation = true;
                bw.WorkerReportsProgress = true;
                bw.DoWork += RenameBW_DoWork;
                bw.ProgressChanged += RenameBW_ProgressChanged;
                bw.RunWorkerCompleted += RenameBW_RunWorkerCompleted;
                remove_old_extension = remove_extension.IsChecked;
                process.Visibility = Visibility.Visible;
                process.Value = 0;
                process.Maximum = list_to_rename.Count;
                new_extension = extension.Text;
                bw.RunWorkerAsync();
            }
        }

        private void RenameBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            list_to_rename.Clear();
            foreach (FileInfo i in Undone)
                //FilesList.Items.Add(MakeItemFile(i));
                list_to_rename.Add(new RenamerListView(i.FullName, FileInformation.file_bitmap_24[Functions.GetFileIndex(i.Extension)], i));
            items_count.Text = list_to_rename.Count + (list_to_rename.Count > 1 ? " Item" : "items");
            new My_MessageBox("Renamer", "Done." + (Undone.Count > 0 ? "\n But some files have not been renamed successfully, this files have been readded to the list" : ""), new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
            process.Visibility = Visibility.Hidden;
        }

        private void RenameBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            process.Value = e.ProgressPercentage + 1;
        }

        private void RenameBW_DoWork(object sender, DoWorkEventArgs e)
        {
            //bool show_message_again = true;
            for (int i = 0; i < list_to_rename.Count; i++)
                try
                {
                    if (File.Exists(list_to_rename[i].FullPath))
                    {
                        string file_path;
                        if (remove_old_extension == false)
                            //file_path = i.ContentStringFormat;
                            file_path = list_to_rename[i].FullPath;
                        else
                            //file_path = i.ContentStringFormat.Substring(0, i.ContentStringFormat.Length - ((FileInfo)i.Tag).Extension.Length);
                            file_path = list_to_rename[i].FullPath.Substring(0, list_to_rename[i].FullPath.Length - list_to_rename[i].File.Extension.Length);
                        if (!File.Exists(file_path + "." + new_extension))
                            //File.Move(i.ContentStringFormat, file_path + "." + extension.Text);
                            File.Move(list_to_rename[i].FullPath, file_path + "." + new_extension);
                        else
                        {
                            int k = 1;
                            while (File.Exists(file_path + " (" + k + ")" + "." + new_extension)) k++;
                            //if (!show_message_again || (show_message_again && new My_MessageBox("Rename file", "Do you want to name file '" + file_path + " (" + k + ")" + "." + new_extension + "' instat of '" + file_path + "." + new_extension + "'", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-Warning.wav"), true).ShowThisDialog(out show_message_again) == MyMessageBoxResult.Yes))
                            //File.Move(i.ContentStringFormat, file_path + " (" + k + ")" + "." + extension.Text);
                            File.Move(list_to_rename[i].FullPath, file_path + " (" + k + ")" + "." + new_extension);
                            //else
                            //    Undone.Add(list_to_rename[i].File);
                        }
                    }
                    else
                        Undone.Add(list_to_rename[i].File);
                    ((BackgroundWorker)sender).ReportProgress(i);
                }
                catch (Exception) { Undone.Add(list_to_rename[i].File); }
        }

        private void extension_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (extension.Text.Contains("*") || extension.Text.Contains("/") || extension.Text.Contains("\\") || extension.Text.Contains("\"") || extension.Text.Contains("?") || extension.Text.Contains("<") || extension.Text.Contains(">") || extension.Text.Contains("|") || extension.Text.Contains("."))
            {
                extension.Text = extension.Text.Replace("*", "").Replace("/", "").Replace("\\", "").Replace("\"", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "").Replace(".", "");
                new My_MessageBox("Renamer", "File extension can't contain any of these characters:\n\\ / : * ? \" < > | .", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            }
        }

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

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            for (int i = FilesList.SelectedItems.Count - 1; i >= 0; i--)
                //FilesList.Items.RemoveAt(FilesList.Items.IndexOf(FilesList.SelectedItems[i]));
                list_to_rename.RemoveAt(FilesList.Items.IndexOf(FilesList.SelectedItems[i]));
            items_count.Text = list_to_rename.Count + (list_to_rename.Count > 1 ? " Item" : "items");
        }

        #region Suggest best possible extension

        private void AddFile2_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OFD = new System.Windows.Forms.OpenFileDialog();
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                foreach (string i in OFD.FileNames)
                {
                    FileInfo file = new FileInfo(i);
                    list_to_rename2.Add(new Renamer2ListView(i, FileInformation.file_bitmap_24[Functions.GetFileIndex(file.Extension)], file));
                    items_count2.Text = list_to_rename2.Count + (list_to_rename2.Count > 1 ? " Item" : "items");
                }
        }

        private void AddFolder2_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                BackgroundWorker add_all_files_in_dir_bw2 = new BackgroundWorker();
                add_all_files_in_dir_bw2.WorkerSupportsCancellation = true;
                add_all_files_in_dir_bw2.WorkerReportsProgress = true;
                add_all_files_in_dir_bw2.DoWork += bw2_DoWork;
                add_all_files_in_dir_bw2.ProgressChanged += bw2_ProgressChanged;
                add_all_files_in_dir_bw2.RunWorkerCompleted += bw2_RunWorkerCompleted;
                add_all_files_in_dir_bw2.RunWorkerAsync(path);
            }
        }

        private void bw2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            items_count2.Text = list_to_rename2.Count + (list_to_rename2.Count > 1 ? " Items" : "item");
        }

        private void bw2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileInfo i = (FileInfo)e.UserState;
            try
            { list_to_rename2.Add(new Renamer2ListView(i.FullName, FileInformation.file_bitmap_24[Functions.GetFileIndex(i.Extension)], i)); }
            catch (Exception) { }
        }

        private void bw2_DoWork(object sender, DoWorkEventArgs e)
        {
            GetAllFiles(e.Argument.ToString(), (BackgroundWorker)sender);
        }

        private void RemoveSelected2_Click(object sender, RoutedEventArgs e)
        {
            for (int i = FilesList2.SelectedItems.Count - 1; i >= 0; i--)
                list_to_rename2.RemoveAt(FilesList2.Items.IndexOf(FilesList2.SelectedItems[i]));
            items_count.Text = list_to_rename2.Count + (list_to_rename2.Count > 1 ? " Item" : "items");
        }

        private void ClearList2_Click(object sender, RoutedEventArgs e)
        {
            list_to_rename2.Clear();
        }

        private void Rename2_Click(object sender, RoutedEventArgs e)
        {
            if (new My_MessageBox("Confirmation", "Are you sure you want to rename all files in list?", "Caution: you can not reverse this operation or cancel it!", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
            {
                Suggest_Click(null, null);
            }
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
            suggest_bw.RunWorkerAsync(list_of_FilesID);
            rename2_progressbar.Value = 0;
            rename2_progressbar.Visibility = Visibility.Visible;
            FilesList2.Margin = new Thickness(1500, 1500, 1500, 1500);
        }

        private void SuggestAndRename_bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SuggestBW_RunWorkerCompleted(null, null);
            BackgroundWorker renamer2_bw = new BackgroundWorker();
            renamer2_bw.DoWork += Rename2BW_DoWork;
            renamer2_bw.ProgressChanged += Rename2BW_ProgressChanged;
            renamer2_bw.WorkerSupportsCancellation = renamer2_bw.WorkerReportsProgress = true;
            renamer2_bw.RunWorkerCompleted += Rename2BW_RunWorkerCompleted;
            renamer2_bw.RunWorkerAsync(remove_extension2.IsChecked);
            rename2_progressbar.Value = 0;
            rename2_progressbar.Visibility = Visibility.Visible;
            FilesList2.Margin = new Thickness(1500, 1500, 1500, 1500);
        }

        private void Rename2BW_DoWork(object sender, DoWorkEventArgs e)
        {
            bool? is_checked = (bool?)e.Argument;
            //bool show_message_again = true;
            int process = -1;
            foreach (var i in list_to_rename2)
            {
                process++;
                if (i.PossibleExt.Count > i.SelectedExt && i.Done == false && i.PossibleExt[i.SelectedExt] != "none")
                    try
                    {
                        if (i.File.Exists && ("." + i.PossibleExt[i.SelectedExt]) != i.File.Extension)
                        {
                            string file_path;
                            if (is_checked == false)
                                file_path = i.FullPath;
                            else
                                file_path = i.FullPath.Substring(0, i.FullPath.Length - i.File.Extension.Length);
                            if (!File.Exists(file_path + "." + i.PossibleExt[i.SelectedExt]))
                                i.File.MoveTo(file_path + "." + i.PossibleExt[i.SelectedExt]);
                            else
                            {
                                int k = 1;
                                while (File.Exists(file_path + " (" + k + ")" + "." + i.PossibleExt[i.SelectedExt])) k++;
                                //if (!show_message_again || (show_message_again && new My_MessageBox("Rename file", "Do you want to name file '" + file_path + " (" + k + ")" + "." + new_extension + "' instat of '" + file_path + "." + new_extension + "'", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-Warning.wav"), true).ShowThisDialog(out show_message_again) == MyMessageBoxResult.Yes))
                                i.File.MoveTo(file_path + " (" + k + ")" + "." + i.PossibleExt[i.SelectedExt]);
                                //else
                                //    Undone.Add(list_to_rename[i].File);
                            }
                            ((BackgroundWorker)sender).ReportProgress(process);
                        }
                    }
                    catch (Exception) { }
            }
        }

        private void Rename2BW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            rename2_progressbar.Value = 1050;
            rename2_progressbar.Visibility = Visibility.Hidden;
            FilesList2.Margin = new Thickness(10, 42, 10, 66);
        }

        private void Rename2BW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            rename2_progressbar.Value += 1000 / list_to_rename2.Count;
            list_to_rename2[e.ProgressPercentage].Done = true;
        }

        private void SuggestBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            foreach (var i in list_to_rename2)
            {
                if (i.PossibleExt.Count == 0)
                    i.PossibleExt.Add("none");
                if (i.PossibleExt.Count > 0 && i.Done == false)
                    i.SelectedExt = 0;
            }
            rename2_progressbar.Value = 1050;
            rename2_progressbar.Visibility = Visibility.Hidden;
            FilesList2.Margin = new Thickness(10, 42, 10, 66);
        }

        private void SuggestBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage > -1)
            {
                list_to_rename2[e.ProgressPercentage].PossibleExt.Add(e.UserState.ToString());
                list_to_rename2[e.ProgressPercentage].SelectedExt = 0;
            }
            else
                rename2_progressbar.Value += 1000 / list_to_rename2.Count;
        }

        private void SuggestBW_DoWork(object sender, DoWorkEventArgs e)
        {
            List<FileIDInfo> list = (List<FileIDInfo>)e.Argument;
            Parallel.For(0, list_to_rename2.Count, (i) =>// (int i = 0; i < list_to_rename2.Count; i++)
            {
                BackgroundWorker this_bw = ((BackgroundWorker)sender);
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
                                foreach (var j in list)
                                    if (list_to_rename2[i].File.Length >= j.IDs.Count)
                                    {
                                        try
                                        {
                                            int k;
                                            while (j.IDs.Count > file_bytes.Count)
                                                file_bytes.Add(BR.ReadByte());
                                            for (k = 0; k < j.IDs.Count; k++)
                                                if (j.IDs[k] != file_bytes[k])
                                                    break;
                                            if (k == j.IDs.Count)
                                                this_bw.ReportProgress(i, j.Extention);
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

        private bool IsXLS(Renamer2ListView i, BackgroundWorker this_bw)
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

        private bool IsDOC(Renamer2ListView i, BackgroundWorker this_bw)
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

        private bool IsWebM(Renamer2ListView i, BackgroundWorker this_bw, List<byte> file_bytes)
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

        private bool IsMP4(Renamer2ListView i, BackgroundWorker this_bw, List<byte> file_bytes)
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

        private bool Is3GP(Renamer2ListView i, BackgroundWorker this_bw, List<byte> file_bytes)
        {
            if (file_bytes[4] == 0x66 && file_bytes[5] == 0x74 && file_bytes[6] == 0x79 && file_bytes[7] == 0x70 && file_bytes[8] == 0x33 && file_bytes[9] == 0x67 && file_bytes[10] == 0x70)
                return true;
            return false;
        }

        private bool IsAVI(Renamer2ListView i, BackgroundWorker this_bw, List<byte> file_bytes)
        {
            if (file_bytes[0] == 0x52 && file_bytes[1] == 0x49 && file_bytes[2] == 0x46 && file_bytes[3] == 0x46 && file_bytes[8] == 0x41 && file_bytes[9] == 0x56 && file_bytes[10] == 0x49)
                return true;
            return false;
        }

        private bool IsPPT(Renamer2ListView i, BackgroundWorker sender)
        {
            if (i.File.Length > 44)
            {
                BinaryReader BR = new BinaryReader(i.File.OpenRead());
                if (BR.ReadByte() == 0xD0 && BR.ReadByte() == 0xCF && BR.ReadByte() == 0x11 && BR.ReadByte() == 0xE0 && BR.ReadByte() == 0xA1 && BR.ReadByte() == 0xB1 && BR.ReadByte() == 0x1A && BR.ReadByte() == 0xE1 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x3E && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x03 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0xFE && BR.ReadByte() == 0xFF && BR.ReadByte() == 0x09 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x06 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00 && BR.ReadByte() == 0x00)
                    return true;
            }
            return false;
        }

        private bool IsWave(Renamer2ListView i, BackgroundWorker sender, List<byte> file_bytes)
        {
            if (file_bytes[0] == 0x52 && file_bytes[1] == 0x49 && file_bytes[2] == 0x46 && file_bytes[3] == 0x46 && file_bytes[8] == 0x57 && file_bytes[9] == 0x41 && file_bytes[10] == 0x56 && file_bytes[11] == 0x45)
                return true;
            return false;
        }

        private bool IsISO(Renamer2ListView i, BackgroundWorker sender)
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

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list_of_FilesID == null)
            {
                list_of_FilesID = new List<FileIDInfo>();
                Stream st = null;
                bool error = false;
                if (File.Exists("ExtIDs.dat"))
                    try
                    {
                        FileInfo file = new FileInfo("ExtIDs.dat");
                        if (file.Length < 256)
                            error = true;
                        else
                        {
                            BinaryReader BR = new BinaryReader(File.OpenRead("ExtIDs.dat"));
                            int count = BR.ReadInt32();
                            if (BR.ReadByte() == 68 && BR.ReadByte() == 114 && BR.ReadByte() == 46 && BR.ReadByte() == 77 && BR.ReadByte() == 111 && BR.ReadByte() == 104 && BR.ReadByte() == 97 && BR.ReadByte() == 109 && BR.ReadByte() == 109 && BR.ReadByte() == 97 && BR.ReadByte() == 100)
                            {
                                BR.ReadString();
                                BR.ReadInt32();
                                BR.ReadString();
                                BR.BaseStream.Position = 1024;
                                for (int i = 0; i < count; i++)
                                {
                                    string ext = BR.ReadString();
                                    BR.ReadByte();
                                    string discription = BR.ReadString();
                                    int length = BR.ReadInt32();
                                    List<int> IDs = new List<int>();
                                    for (int j = 0; j < length; j++)
                                        IDs.Add(BR.ReadInt32());
                                    list_of_FilesID.Add(new FileIDInfo(ext, discription, IDs));
                                }
                            }
                            else
                                error = true;
                        }
                    }
                    catch (Exception) { error = true; }
                else
                    error = true;
                if (error)
                {
                    MessageBox.Show("Error while loading file \"ExtIDs.dat\"");
                    ((TabControl)sender).SelectedIndex = 0;
                    list_of_FilesID = null;
                }
                if (st != null)
                    st.Close();
            }
        }
    }
}



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