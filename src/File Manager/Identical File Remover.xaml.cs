using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// Interaction logic for Identical_File_Remover.xaml
    /// </summary>
    public partial class Identical_File_Remover : Window
    {
        ObservableCollection<DublicateFiles_ListView> list_to_view = new ObservableCollection<DublicateFiles_ListView>();
        SHA1 Hasher = new SHA1Cng();
        int files_count = 0;
        List<string> paths;
        bool sun_bridle = true, bridle_father = true, show_message_again = true, cancel = false;
        double dublicate_size = 0;
        double deleted_size = 0;
        double total_size = 0;
        List<BasicFileInfo> file_list;
        List<string> sha1_string;
        BackgroundWorker find_duplicate_BackgroundWorker;

        public Identical_File_Remover()
        {
            InitializeComponent();
            item_group_viewer.DataContext = list_to_view;
        }

        private void FindDuplicate_Click(object sender, RoutedEventArgs e)
        {
            paths = new List<string>();
            if (new MultiDirectoriesSelector().ShowDialog(paths) && paths.Count > 0)
            {
                ClearList_Click(null, null);
                files_count = 0;
                process.Visibility = Visibility.Visible;
                find_dublicate.IsEnabled = false;
                TaskbarItemInfo.ProgressValue = 0;
                find_duplicate_BackgroundWorker = new BackgroundWorker();
                find_duplicate_BackgroundWorker.WorkerReportsProgress = true;
                find_duplicate_BackgroundWorker.WorkerSupportsCancellation = true;
                find_duplicate_BackgroundWorker.ProgressChanged += FindDuplicate_ProgressChanged;
                find_duplicate_BackgroundWorker.DoWork += FindDuplicate_DoWork;
                find_duplicate_BackgroundWorker.RunWorkerCompleted += FindDuplicate_RunWorkerCompleted;
                find_duplicate_BackgroundWorker.RunWorkerAsync();
            }
        }

        void FindDuplicate_DoWork(object sender, DoWorkEventArgs e)
        {
            file_list = new List<BasicFileInfo>();
            sha1_string = new List<string>();
            foreach (string i in paths)
                try
                { GetAllFiles(new DirectoryInfo(i)); }
                catch (Exception) { }
            file_list.Sort();
            files_count = file_list.Count;
            for (int i = file_list.Count - 2; i > 0; i--)
            {
                CalcHash(file_list, i, -1, +1);
                find_duplicate_BackgroundWorker.ReportProgress(file_list.Count - i - 2);
                if (cancel) return;
            }
            if (file_list.Count > 1)
            {
                CalcHash(file_list, 0, +1, +1);
                find_duplicate_BackgroundWorker.ReportProgress(file_list.Count - 1);
                CalcHash(file_list, file_list.Count - 1, -1, -1);
                find_duplicate_BackgroundWorker.ReportProgress(file_list.Count);
            }
            file_list.Sort(CompareStrings);
            for (int i = 0; i < file_list.Count - 1; i++)
            {
                if (cancel) return;
                if (file_list[i].SHA1.Equals(file_list[i + 1].SHA1))
                {
                    int count = 1;
                    for (int j = i + 1; j < file_list.Count; j++)
                        if (file_list[i].SHA1.Equals(file_list[j].SHA1))
                            count++;
                        else
                            break;
                    find_duplicate_BackgroundWorker.ReportProgress(i, count);
                    i += count - 1;
                }
            }
        }

        private void GetAllFiles(DirectoryInfo directoryInfo)
        {
            if (cancel)
                return;
            try
            {
                var files = directoryInfo.GetFiles();
                foreach (var i in files)
                    file_list.Add(new BasicFileInfo(i));
            }
            catch (Exception) { }
            try
            {
                var sub_dirs = directoryInfo.GetDirectories();
                foreach (var i in sub_dirs)
                    GetAllFiles(i);
            }
            catch (Exception) { }
        }

        private int CompareStrings(BasicFileInfo x, BasicFileInfo y)
        {
            return x.SHA1.CompareTo(y.SHA1);
        }

        void FindDuplicate_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                TaskbarItemInfo.ProgressValue += 3.0 / (4.0 * files_count);
                process.Value = TaskbarItemInfo.ProgressValue * 100.0;
            }
            else
            {
                int i = (int)e.UserState;
                TaskbarItemInfo.ProgressValue += (i - 0.0) / (4.0 * files_count);
                process.Value = TaskbarItemInfo.ProgressValue * 100.0;
                list_to_view.Add(new DublicateFiles_ListView(Functions.GetFileImage24(file_list[e.ProgressPercentage].extention), Functions.GetFileType(file_list[e.ProgressPercentage].extention), file_list[e.ProgressPercentage].size, false, new Point()));
                for (int j = e.ProgressPercentage; j < e.ProgressPercentage + i; j++)
                    list_to_view[list_to_view.Count - 1].Dublicats.Add(new FilePath_ListView(file_list[j].fullpath, file_list[j].extention));
                list_to_view[list_to_view.Count - 1].FullPath = list_to_view[list_to_view.Count - 1].Dublicats.Count + (list_to_view[list_to_view.Count - 1].Dublicats.Count > 1 ? " dublicats " : " dublicat");
                list_to_view[list_to_view.Count - 1].TotalFileSize = Functions.FileSize(file_list[e.ProgressPercentage].size * list_to_view[list_to_view.Count - 1].Dublicats.Count, 2);
            }
        }

        void FindDuplicate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!cancel)
            {
                SelectAll_Click(null, null);
                TaskbarItemInfo.ProgressValue = 0;
                if (list_to_view.Count > 0)
                    delete.IsEnabled = select_all.IsEnabled = select_none.IsEnabled = clear_list.IsEnabled = true;
                find_dublicate.IsEnabled = true;
                items_label.Text = list_to_view.Count > 1 ? "items" : "item";
                number.Text = " " + list_to_view.Count;
                process.Value = 100;
                new My_MessageBox("FILE MANAGER", "Search done.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
                process.Visibility = Visibility.Hidden;
                flow_shart.Visibility = Visibility.Visible;
            }
        }

        private void CalcHash(List<BasicFileInfo> file_list, int i, int p1, int p2)
        {
            if (file_list[i].size == file_list[i + p1].size || file_list[i].size == file_list[i + p2].size)
                try
                { file_list[i].SetHash(Hasher.ComputeHash(File.OpenRead(file_list[i].fullpath))); }
                catch (Exception) { }
            else
                file_list.RemoveAt(i);
        }

        private void Sun_Unchecked(object sender, RoutedEventArgs e)
        {
            if (bridle_father)
                sun_bridle = false;
            CheckAllSun(list_to_view[item_group_viewer.SelectedIndex], -1);
            sun_bridle = true;
        }

        private void Sun_Checked(object sender, RoutedEventArgs e)
        {
            if (bridle_father)
                sun_bridle = false;
            if (list_to_view[item_group_viewer.SelectedIndex].cheched_count == list_to_view[item_group_viewer.SelectedIndex].Dublicats.Count - 1)
                if (show_message_again)
                {
                    if (new My_MessageBox("File Manager", "You are about to remove all copies of this file, do you want to do that?", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-Warning.wav"), true).ShowThisDialog(out show_message_again) == MyMessageBoxResult.Yes)
                        CheckAllSun(list_to_view[item_group_viewer.SelectedIndex], 1);
                    else
                    {
                        sun_bridle = true;
                        bridle_father = false;
                        (sender as CheckBox).IsChecked = false;
                        bridle_father = true;
                    }
                }
                else
                    CheckAllSun(list_to_view[item_group_viewer.SelectedIndex], 1);
            else
                CheckAllSun(list_to_view[item_group_viewer.SelectedIndex], 1);
            sun_bridle = true;
        }

        private void CheckAllSun(DublicateFiles_ListView d, int dif)
        {
            if (!sun_bridle)
            {
                list_to_view[item_group_viewer.SelectedIndex].cheched_count += dif;
                deleted_size += dif * list_to_view[item_group_viewer.SelectedIndex].size;
                deleted_grid.Width = (deleted_size / total_size) * total.ActualWidth + 4;
                deleted_text.Text = Functions.FileSize(deleted_size, 2) + " will be deleted";
                int count = 0;
                foreach (var i in d.Dublicats)
                    if (i.IsChecked) count++;
                if (count == 0)
                    d.ChangeIsCheakedValue(false);
                else if (count == d.Dublicats.Count)
                    d.ChangeIsCheakedValue(true);
                else
                    d.ChangeIsCheakedValue(null);
                item_group_viewer.Items.Refresh();
                item_viewer.Items.Refresh();
            }
        }

        private void Item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (item_viewer.SelectedIndex > -1)
                try { System.Diagnostics.Process.Start(list_to_view[item_group_viewer.SelectedIndex].Dublicats[item_viewer.SelectedIndex].FullPath); }
                catch (Exception ex) { new My_MessageBox("Error", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void Item_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (File.Exists(list_to_view[item_group_viewer.SelectedIndex].Dublicats[item_viewer.SelectedIndex].FullPath))
                    new Preview().ShowDialog(new FileInfo(list_to_view[item_group_viewer.SelectedIndex].Dublicats[item_viewer.SelectedIndex].FullPath), ItemType.File);
                if (MainWindow.GoToPath)
                {
                    MainWindow.GoToPath = false;
                    new MainWindow(MainWindow.to_go_path).ShowDialog();
                }
            }
            else if (e.Key == Key.Enter)
                Item_MouseDoubleClick(null, null);
            else if (e.Key == Key.Delete)
                Delete_Click(delete, null);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            List<FileOperationItem> files = new List<FileOperationItem>();
            foreach (var i in list_to_view)
                foreach (var j in i.Dublicats)
                    if (j.IsChecked)
                        files.Add(new FileOperationItem(new FileInfo(j.FullPath), false, ""));
            if (files.Count > 0)
            {
                My_MessageBox messagebox;
                if (files.Count > 1)
                    messagebox = new My_MessageBox(files.Count);
                else
                    messagebox = new My_MessageBox(files[0].Item);
                if (messagebox.ShowThisDialog() == MyMessageBoxResult.Yes)
                {
                    new Operation().CopyDialogShow(files, Op.Delete, "", true);
                    for (int i = list_to_view.Count - 1; i > -1; i--)
                    {
                        for (int j = list_to_view[i].Dublicats.Count - 1; j > -1; j--)
                            if (list_to_view[i].Dublicats[j].IsChecked && !File.Exists(list_to_view[i].Dublicats[j].FullPath))
                            { list_to_view[i].Dublicats.RemoveAt(j); list_to_view[i].cheched_count--; }
                        if (list_to_view[i].Dublicats.Count < 2)
                            list_to_view.RemoveAt(i);
                        else
                            list_to_view[i].FullPath = list_to_view[i].Dublicats.Count + " dublicats ";
                    }
                    number.Text = list_to_view.Count + " ";
                    items_label.Text = list_to_view.Count > 1 ? "items" : "item";
                    new My_MessageBox("File Manager", "Files deleted done.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
                }
            }
            else
                new My_MessageBox("File Manager", "You have not selected any file to been deleted!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            dublicate_size = 0;
            deleted_size = 0;
            total_size = 0;
            foreach (DublicateFiles_ListView i in list_to_view)
            {
                i.IsChecked = true;
                total_size += (i.size * i.Dublicats.Count);
                dublicate_size += (i.size * (i.Dublicats.Count - 1));
            }
            item_viewer.Items.Refresh();
            item_group_viewer.Items.Refresh();
            deleted_size = dublicate_size;
            duplicate.Width = (dublicate_size / total_size) * total.ActualWidth - 2;
            deleted_grid.Width = (deleted_size / total_size) * total.ActualWidth + 4;
            deleted_text.Text = Functions.FileSize(deleted_size, 2) + " will be deleted";
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            process.Value = 0;
            delete.IsEnabled = select_all.IsEnabled = select_none.IsEnabled = clear_list.IsEnabled = false;
            flow_shart.Visibility = Visibility.Hidden;
            list_to_view.Clear();
            item_viewer.DataContext = null;
        }

        private void SortBy_Click(object sender, RoutedEventArgs e)
        {
            ((Button)sender).ContextMenu.IsOpen = !((Button)sender).ContextMenu.IsOpen;
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (DublicateFiles_ListView i in list_to_view)
                i.IsChecked = false;
            item_group_viewer.Items.Refresh();
            item_viewer.Items.Refresh();
            deleted_grid.Width = 4;
            deleted_text.Text = "0 byte will be deleted";
        }

        private void ItemGroupViewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (item_group_viewer.SelectedIndex > -1)
                item_viewer.DataContext = list_to_view[item_group_viewer.SelectedIndex].Dublicats;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            cancel = true;
        }
    }

    class BasicFileInfo : IComparable
    {
        public string name;
        public string extention;
        public string fullpath;
        public long size;
        public string SHA1;

        public BasicFileInfo(FileInfo file)
        {
            name = file.Name;
            extention = file.Extension;
            fullpath = file.FullName;
            size = file.Length;
        }

        public int CompareTo(object obj)
        {
            long s = ((BasicFileInfo)obj).size;
            if (s > this.size)
                return 1;
            else if (s < this.size)
                return -1;
            else
                return 0;
        }

        internal void SetHash(byte[] p)
        {
            StringBuilder h = new StringBuilder();
            foreach (byte i in p)
                h.Append(i.ToString());
            SHA1 = h.ToString();
        }
    }

    class DublicateFiles_ListView : GeneralPurpose
    {
        internal int cheched_count = 0;
        public long size = 0;
        public DublicateFiles_ListView() { Dublicats = new ObservableCollection<FilePath_ListView>(); }
        public DublicateFiles_ListView(BitmapImage img_source, string file_type, long size, bool? is_checked, Point Tag)
        {
            Image = img_source;
            FileType = file_type;
            this.size = size;
            FileSize = Functions.FileSize(size, 2);
            Dublicats = new ObservableCollection<FilePath_ListView>();
            Point = Tag;
        }

        private bool? is_cheched;
        public Boolean? IsChecked
        {
            get { return is_cheched; }
            set
            {
                if (value.HasValue)
                {
                    foreach (var i in Dublicats)
                        i.IsChecked = value.Value;
                    if (value.Value)
                        Dublicats[0].IsChecked = false;
                    cheched_count = Dublicats.Count - 1;
                }
                if (value.Value)
                    is_cheched = null;
                else
                    is_cheched = value;
                //refresh list view;
            }
        }

        public void ChangeIsCheakedValue(bool? value)
        {
            is_cheched = value;
        }

        ObservableCollection<FilePath_ListView> dublicats;
        public ObservableCollection<FilePath_ListView> Dublicats
        {
            get { return dublicats; }
            set { dublicats = value; }
        }

        private String file_type;
        public string FileType
        {
            get { return file_type; }
            set { file_type = value; }
        }


        private String total_file_size;
        public string TotalFileSize
        {
            get { return total_file_size; }
            set { total_file_size = value; }
        }

        private String file_size;
        public string FileSize
        {
            get { return file_size; }
            set { file_size = value; }
        }

        Point point;
        public Point Point
        {
            get { return point; }
            set { point = value; }
        }
    }

    class FilePath_ListView : GeneralPurpose
    {
        public FilePath_ListView(string path, string ext)
        {
            FullPath = path;
            IsChecked = false;
            Image = Functions.GetFileImage24(ext);
        }

        bool is_checked;
        public bool IsChecked
        {
            get { return is_checked; }
            set { is_checked = value; }
        }
    }
}

/*
            //MenuItem view_file = new MenuItem();
            //view_file.Header = "View file";
            //view_file.Click += ViewFile_Click;
            //MenuItem view_directory = new MenuItem();
            //view_directory.Header = "View in directory";
            //view_directory.Click += ViewDirectory_Click;
*/

//path = new Folder_Picker().ShowThisDialog();
//    if (path != null)
//    {
//        //delete.IsEnabled = false;
//        //TaskbarItemInfo.ProgressValue = 0;
//        //files_name = new List<string>();
//        //SHA1s = new List<List<byte[]>>();
//        //file_size = new List<long>();
//        //List<List<string>> Identical_Files = new List<List<string>>();
//        //Get_all_files(path);
//        //Quick_Sort(file_size, 0, file_size.Count - 1);
//        //List<string> fileNode = null;
//        //long current_size = -1;
//        for (int i = 0; i < file_size.Count; i++)
//        {
//            if (current_size != file_size[i])
//            {
//                current_size = file_size[i]; //create edge
//                if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
//                    Identical_Files.Add(fileNode);
//                fileNode = new List<string>();
//            }
//            fileNode.Add(files_name[i]);
//        } //create edge for last node
//        if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
//            Identical_Files.Add(fileNode);
//        files_size = Identical_Files.Count;
//        SHA1 HashComputer = new SHA1Cng();
//        for (int i = 0; i < Identical_Files.Count; i++)
//        {
//            List<byte[]> tmp = new List<byte[]>();
//            for (int j = 0; j < Identical_Files[i].Count; j++)
//                tmp.Add(HashComputer.ComputeHash(File.OpenRead(Identical_Files[i][j])));
//            SHA1s.Add(tmp);
//            TaskbarItemInfo.ProgressValue = (2 * (i + 0.0)) / (3 * files_size);
//        }
//        bool equal;
//        for (int i = 0; i < Identical_Files.Count; i++)
//        {
//            for (int j = 0; j < Identical_Files[i].Count - 1; j++)
//            {
//                TreeViewItem father = MakeTreeViewItemFather(Identical_Files[i][j], Identical_Files[i].Count);
//                if (father != null)
//                {
//                    TreeViewItem sun1 = MakeTreeViewItemSun(Identical_Files[i][j], i);
//                    if (sun1 != null)
//                        father.Items.Add(sun1);
//                    for (int k = j + 1; k < Identical_Files[i].Count; k++)
//                    {
//                        equal = Equals(SHA1s[i][j], SHA1s[i][k]);
//                        if (equal)
//                        {
//                            TreeViewItem sun = MakeTreeViewItemSun(Identical_Files[i][k], i);
//                            if (sun != null)
//                                father.Items.Add(sun);
//                            files_name.Remove(files_name[k]);
//                            SHA1s[i].Remove(SHA1s[i][k]);
//                            Identical_Files[i].Remove(Identical_Files[i][k]);
//                            k--;
//                        }
//                    }
//                    if (father.Items.Count > 1)
//                        ItemsTree.Items.Add(father);
//                }
//            }
//            TaskbarItemInfo.ProgressValue += 1.0 / (3 * files_size);
//        }
//        for (int i = 0; i < ItemsTree.Items.Count; i++)
//            for (int j = 1; j < ((TreeViewItem)ItemsTree.Items[i]).Items.Count; j++)
//                ((CheckBox)((Grid)((TreeViewItem)((TreeViewItem)ItemsTree.Items[i]).Items[j]).Header).Children[0]).IsChecked = true;
//        TaskbarItemInfo.ProgressValue = 1;
//    }
//    delete.IsEnabled = true;





//ItemsTree.IsHitTestVisible = false;
//TaskbarItemInfo.ProgressValue = 0;
//files_name = new List<string>();
//SHA1s = new List<List<byte[]>>();
//file_size = new List<long>();
//List<List<string>> Identical_Files = new List<List<string>>();
//foreach (string i in paths)
//    try
//    { Get_all_files(new DirectoryInfo(i)); }
//    catch (Exception) { }
//Quick_Sort(file_size, 0, file_size.Count - 1);
//List<string> fileNode = null;
//long current_size = -1;
//for (int i = 0; i < file_size.Count; i++)
//{
//    if (current_size != file_size[i])
//    {
//        current_size = file_size[i]; //create edge
//        if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
//            Identical_Files.Add(fileNode);
//        fileNode = new List<string>();
//    }
//    fileNode.Add(files_name[i]);
//} //create edge for last node
//if (fileNode != null && fileNode.Count > 1) //contains at least the file itself
//    Identical_Files.Add(fileNode);
//files_size = Identical_Files.Count;
//SHA1 HashComputer = new SHA1Cng();
//for (int i = 0; i < Identical_Files.Count; i++)
//{
//    List<byte[]> tmp = new List<byte[]>();
//    for (int j = 0; j < Identical_Files[i].Count; j++)
//        tmp.Add(HashComputer.ComputeHash(File.OpenRead(Identical_Files[i][j])));
//    SHA1s.Add(tmp);
//    TaskbarItemInfo.ProgressValue = (2 * (i + 0.0)) / (3 * files_size);
//}
//for (int i = 0; i < Identical_Files.Count; i++)
//{
//    for (int j = 0; j < Identical_Files[i].Count - 1; j++)
//    {
//        TreeViewItem father = MakeTreeViewItemFather(Identical_Files[i][j], Identical_Files[i].Count);
//        if (father != null)
//        {
//            TreeViewItem sun1 = MakeTreeViewItemSun(Identical_Files[i][j], i);
//            if (sun1 != null)
//                father.Items.Add(sun1);
//            for (int k = j + 1; k < Identical_Files[i].Count; k++)
//            {
//                if (Equals(SHA1s[i][j], SHA1s[i][k]))
//                {
//                    TreeViewItem sun = MakeTreeViewItemSun(Identical_Files[i][k], i);
//                    if (sun != null)
//                        father.Items.Add(sun);
//                    files_name.Remove(files_name[k]);
//                    SHA1s[i].Remove(SHA1s[i][k]);
//                    Identical_Files[i].Remove(Identical_Files[i][k]);
//                    k--;
//                }
//            }
//            if (father.Items.Count > 1)
//                ItemsTree.Items.Add(father);
//        }
//    }
//    TaskbarItemInfo.ProgressValue += 1.0 / (3 * files_size);
//}


//TreeViewItem father = MakeTreeViewItemFather(dublicate_Files[i][j], dublicate_Files[i].Count);
//if (father != null)
//{
//TreeViewItem sun1 = MakeTreeViewItemSun(dublicate_Files[i][j], i);
//if (sun1 != null)
//    father.Items.Add(sun1);
//for (int k = j + 1; k < dublicate_Files[i].Count; k++)
//{
//    if (Equals(SHA1s[i][j], SHA1s[i][k]))
//    {
//        TreeViewItem sun = MakeTreeViewItemSun(dublicate_Files[i][k], i);
//        if (sun != null)
//            father.Items.Add(sun);
//        files_name.Remove(files_name[k]);
//        SHA1s[i].Remove(SHA1s[i][k]);
//        dublicate_Files[i].Remove(dublicate_Files[i][k]);
//        k--;
//    }
//}
//if (father.Items.Count > 1)
//    ItemsTree.Items.Add(father);
//}



//private TreeViewItem MakeTreeViewItemSun(string file_name, int father_index)
//{
//    try
//    {
//        FileInfo file = new FileInfo(file_name);
//        int file_index = Functions.GetFileIndex(file.Extension);

//        CheckBox ch1 = new CheckBox();
//        ch1.HorizontalAlignment = HorizontalAlignment.Left;
//        ch1.VerticalAlignment = VerticalAlignment.Top;
//        ch1.Margin = margins[0];
//        ch1.FontSize = 14;
//        ch1.Content = file_name;
//        ch1.IsChecked = true;
//        ch1.Checked += Sun_Checked;
//        ch1.Unchecked += Sun_Unchecked;

//        //TextBlock t1 = new TextBlock();
//        //t1.Text = "File Path: " + file_name;
//        //t1.TextTrimming = TextTrimming.CharacterEllipsis;
//        //t1.Margin = margins[1];
//        //t1.VerticalAlignment = VerticalAlignment.Top;
//        //t1.FontSize = 14;

//        //TextBlock t2 = new TextBlock();
//        //t2.Text = "Type: " + Functions.GetFileType(file_index, file.Extension);
//        //t2.Margin = margins[2];
//        //t2.VerticalAlignment = VerticalAlignment.Top;
//        //t2.FontSize = 14;

//        //TextBlock t3 = new TextBlock();
//        //t3.Text = "Date Modified: " + file.LastWriteTimeUtc;
//        //t3.Margin = margins[3];
//        //t3.VerticalAlignment = VerticalAlignment.Top;
//        //t3.FontSize = 14;

//        //Rectangle separator = new Rectangle();
//        //separator.Fill = Brushes.Silver;
//        //separator.Height = 1;
//        //separator.Margin = margins[4];
//        //separator.VerticalAlignment = VerticalAlignment.Bottom;

//        Grid grid = new Grid();
//        grid.Height = 30;
//        grid.Width = ItemsTree.ActualWidth - 60;
//        //grid.ToolTip = file_name;
//        grid.Children.Add(ch1);
//        //grid.Children.Add(t1);
//        //grid.Children.Add(t2);
//        //grid.Children.Add(t3);
//        //grid.Children.Add(separator);
//        grid.Background = colors[father_index % 7];

//        TreeViewItem tree_item = new TreeViewItem();
//        tree_item.ToolTip = file_name;
//        tree_item.ContextMenu = context_menu;
//        tree_item.Header = grid;
//        //tree_item.Background = colors[father_index % 7];
//        tree_item.HeaderStringFormat = file_name;
//        return tree_item;
//    }
//    catch (Exception)
//    { return null; }
//}

//private TreeViewItem MakeTreeViewItemFather(string file_name, int duplicates)
//{
//    try
//    {
//        FileInfo file = new FileInfo(file_name);
//        int file_index = Functions.GetFileIndex(file.Extension);

//        CheckBox ch1 = new CheckBox();
//        ch1.Content = "Check All Copies";
//        ch1.HorizontalAlignment = HorizontalAlignment.Left;
//        ch1.VerticalAlignment = VerticalAlignment.Top;
//        ch1.Margin = margins[5];
//        ch1.FontSize = 14;
//        ch1.IsChecked = true;
//        ch1.Unchecked += Father_Unchecked;
//        ch1.Checked += Father_Checked;

//        TextBlock t1 = new TextBlock();
//        t1.Text = "File size: " + Functions.FileSize(file.Length);
//        t1.Margin = margins[6];
//        t1.VerticalAlignment = VerticalAlignment.Top;
//        t1.FontSize = 14;

//        TextBlock t2 = new TextBlock();
//        t2.Text = "duplicate's number: " + duplicates + " Copies";
//        t2.Margin = margins[7];
//        t2.VerticalAlignment = VerticalAlignment.Top;
//        t2.FontSize = 14;

//        Image image = new Image();
//        image.Margin = margins[8];
//        image.HorizontalAlignment = HorizontalAlignment.Right;
//        image.Width = 67;
//        image.Source = FileInformation.file_bitmap_64[file_index];

//        Grid grid = new Grid();
//        grid.Height = 87;
//        grid.Width = ItemsTree.ActualWidth - 45;
//        grid.Children.Add(ch1);
//        grid.Children.Add(t1);
//        grid.Children.Add(t2);
//        grid.Children.Add(image);
//        if (ItemsTree.Items.Count > 0)
//        {
//            Rectangle separator = new Rectangle();
//            separator.Fill = Brushes.YellowGreen;
//            separator.Height = 2;
//            separator.Margin = margins[1];
//            separator.VerticalAlignment = VerticalAlignment.Top;
//            grid.Children.Add(separator);
//        }

//        TreeViewItem tree_item = new TreeViewItem();
//        tree_item.Tag = new Point(file.Length, duplicates);
//        //tree_item.HeaderStringFormat = "";
//        tree_item.Header = grid;
//        return tree_item;
//    }
//    catch (Exception)
//    { return null; }
//}




//private void CheckUncheckAll(TreeViewItem father, bool check)
//{
//    sun_bridle = true;
//    foreach (TreeViewItem i in father.Items)
//        ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = check;
//    sun_bridle = false;
//}

//private void Get_all_files(DirectoryInfo dir)
//{
//    DirectoryInfo[] sub_dirs = dir.GetDirectories();
//    foreach (var i in dir.GetFiles())
//    {
//        try
//        {
//            files_name.Add(i.FullName);
//            file_size.Add(i.Length);
//        }
//        catch (Exception) { }
//    }
//    foreach (var i in sub_dirs)
//        try
//        { Get_all_files(i); }
//        catch (Exception) { }
//}

//private bool Equals(byte[] first, byte[] socend)
//{
//    bool res = true;
//    Parallel.For(0, first.Length, (i) =>
//    {
//        if (first[i] != socend[i])
//            res = false;
//    });
//    return res;
//}

//private void Quick_Sort(List<long> files_size, int left, int right)
//{
//    int pivot;
//    if (right > left)
//    {
//        pivot = Partition(files_size, left, right);
//        Quick_Sort(files_size, left, pivot - 1);
//        Quick_Sort(files_size, pivot + 1, right);
//    }
//}

//private int Partition(List<long> files_size, int left, int right)
//{
//    int swapPos;
//    int pivotPos = left + (right - left) / 2;
//    Swap(files_name, left, pivotPos);
//    Swap(files_size, left, pivotPos);
//    swapPos = left + 1;
//    for (int i = swapPos; i <= right; i++)
//        if (files_size[i] < files_size[left])
//        {
//            Swap(files_size, i, swapPos);
//            Swap(files_name, i, swapPos);
//            swapPos++;
//        }
//    Swap(files_name, swapPos - 1, left);
//    Swap(files_size, swapPos - 1, left);
//    return swapPos - 1;
//}

//private void Swap(List<long> A, int first, int last)
//{
//    long tmp = A[last];
//    A[last] = A[first];
//    A[first] = tmp;
//}

//private void Swap(List<string> A, int first, int last)
//{
//    string tmp = A[last];
//    A[last] = A[first];
//    A[first] = tmp;
//}







//Thickness[] margins = { new Thickness(10, 10, 0, 0), new Thickness(35, 10, 10, 0), new Thickness(35, 34, 10, 0), new Thickness(35, 58, 10, 0), new Thickness(10, 0, 10, 2),/*Socend Part*/ new Thickness(5, 10, 0, 0), new Thickness(10, 33, 82, 0), new Thickness(10, 57, 82, 0), new Thickness(0, 10, 10, 0), new Thickness(0, 30, 10, 0) };
//Thickness[] margins = { new Thickness(10, 7, 0, 0), new Thickness(5, 2, 0, 0), new Thickness(35, 34, 10, 0), new Thickness(35, 58, 10, 0), new Thickness(10, 0, 10, 2),/*Socend Part*/ new Thickness(5, 10, 0, 0), new Thickness(10, 33, 82, 0), new Thickness(10, 57, 82, 0), new Thickness(0, 10, 10, 0), new Thickness(26, 8, 10, 0) };
//Brush[] colors = { new SolidColorBrush(Color.FromArgb(125, 135, 206, 235)), new SolidColorBrush(Color.FromArgb(125, 255, 161, 251)), new SolidColorBrush(Color.FromArgb(125, 187, 255, 161)), new SolidColorBrush(Color.FromArgb(125, 255, 161, 161)), new SolidColorBrush(Color.FromArgb(125, 161, 255, 251)), new SolidColorBrush(Color.FromArgb(125, 255, 217, 161)), new SolidColorBrush(Color.FromArgb(125, 182, 161, 255)) };
//List<string> files_name;
//Point point = new Point(0.5, 0.5);
//RotateTransform angel = new RotateTransform(45);
//List<long> file_size;




//string path;

//List<List<byte[]>> SHA1s;
//List<List<string>> dublicate_Files;



//private void Father_Unchecked(object sender, RoutedEventArgs e)
//{
//    if (!bridle_father)
//        CheckUncheckAll((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent, false);
//}

//private void Father_Checked(object sender, RoutedEventArgs e)
//{
//    if (!bridle_father)
//        if (show_message_again)
//            if (new My_MessageBox("Remove copies", "You are about to remove all copies of this file?", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-Warning.wav"), true).ShowThisDialog(out show_message_again) == MyMessageBoxResult.Yes)
//                CheckUncheckAll((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent, true);
//            else
//                ((CheckBox)sender).IsChecked = false;
//        else
//            CheckUncheckAll((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent, true);

//}
