using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for Exists_Files_Window.xaml
    /// </summary>
    public partial class Exists_Files_Window : Window
    {
        ObservableCollection<ExistsItemsListView> ExistsItemsList = new ObservableCollection<ExistsItemsListView>();
        ObservableCollection<ExistsItemsListView> ExistsItemsList_Copy;
        bool accept = false, user_active = false;
        string paste_in;
        List<int> same, same_size, same_size_date, unfinished;
        OperationTree root;

        public Exists_Files_Window(OperationTree root, string destination)
        {
            InitializeComponent();
            this.root = root;
            paste_in = destination;
            AddSuns(root);
            container.DataContext = ExistsItemsList;
        }

        private void AddSuns(OperationTree root)
        {
            foreach (var i in root.children)
            {
                if (i.children == null)
                    ExistsItemsList.Add(new ExistsItemsListView(i, new FileInfo(paste_in + "\\" + i.short_path)));
                else
                    AddSuns(i);
                i.error_message = "";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            user_active = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            accept = false;
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            accept = true;
            this.Close();
        }

        private void CheckSource_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, -1);
        }

        private void CheckSource_Unloaded(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, -1);
        }

        private void CheckTarget_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, 1);
        }

        private void CheckTarget_Unloaded(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, 1);
        }

        private void From_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckSource();
        }

        private void To_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckTarget();
        }

        private void CheckUncheckAll(bool p, int index)
        {
            foreach (ExistsItemsListView i in ExistsItemsList)
                if (index > 0)
                    i.TargetIsChecked = p;
                else if (index < 0)
                    i.SourceIsChecked = p;
            container.Items.Refresh();
        }

        private void CheckUncheckSource()
        {
            int j = 0;
            foreach (ExistsItemsListView i in ExistsItemsList)
                if (i.SourceIsChecked == true)
                    j++;
            if (j == ExistsItemsList.Count)
                source_checkbox.IsChecked = true;
            else if (j == 0)
                source_checkbox.IsChecked = false;
            else
                source_checkbox.IsChecked = null;
        }

        private void CheckUncheckTarget()
        {
            int j = 0;
            foreach (ExistsItemsListView i in ExistsItemsList)
                if (i.TargetIsChecked == true)
                    j++;
            if (j == ExistsItemsList.Count)
                target_checkbox.IsChecked = true;
            else if (j == 0)
                target_checkbox.IsChecked = false;
            else
                target_checkbox.IsChecked = null;
        }

        private void SkipSame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (user_active)
            {
                Functions.MarkAs(root, OperationOption.NoValue, true);
                if (ExistsItemsList_Copy == null)
                {
                    ExistsItemsList_Copy = new ObservableCollection<ExistsItemsListView>();
                    foreach (var i in ExistsItemsList)
                        ExistsItemsList_Copy.Add(i);
                }
                ExistsItemsList.Clear();
                foreach (var i in ExistsItemsList_Copy)
                    ExistsItemsList.Add(i);
                switch (skip_items.SelectedIndex)
                {
                    case 1:
                        if (same != null || Messages.ShowMessage("This operation maybe take long time according to files size and amount of files, do you want to continue?", ModrenMessageButton.YesNo, ModrenMessageImage.Exclamation, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.Yes)
                        {
                            RemoveSame();
                            for (int i = same.Count - 1; i > -1; i--)
                            {
                                ExistsItemsList[same[i]].source_item.user_option = OperationOption.Skip;
                                ExistsItemsList.RemoveAt(same[i]);
                            }
                        }
                        else
                            skip_items.SelectedIndex = 0;
                        break;
                    case 2:
                        RemoveSameSize();
                        for (int i = 0; i < same_size.Count; i++)
                        {
                            ExistsItemsList[same_size[i]].source_item.user_option = OperationOption.Skip;
                            ExistsItemsList.RemoveAt(same_size[i]);
                        }
                        break;
                    case 3:
                        RemoveSameSizeDate();
                        for (int i = 0; i < same_size_date.Count; i++)
                        {
                            ExistsItemsList[same_size_date[i]].source_item.user_option = OperationOption.Skip;
                            ExistsItemsList.RemoveAt(same_size_date[i]);
                        }
                        break;
                    case 5:
                        if (unfinished != null || Messages.ShowMessage("This operation maybe take long time according to files size and amount of files, do you want to continue?", ModrenMessageButton.YesNo, ModrenMessageImage.Exclamation, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.Yes)
                        {
                            FindUnfinished();
                            for (int i = unfinished.Count - 1; i > -1; i--)
                            {
                                ExistsItemsList[unfinished[i]].source_item.user_option = OperationOption.Continue;
                                ExistsItemsList.RemoveAt(unfinished[i]);
                            }
                        }
                        else
                            skip_items.SelectedIndex = 0;
                        break;
                    case 4:
                        for (int i = 0; i < ExistsItemsList.Count; i++)
                            if (ExistsItemsList[i].source_item.item.LastWriteTime.Ticks > ExistsItemsList[i].TargetFile.LastWriteTime.Ticks)
                            {
                                ExistsItemsList[i].TargetIsChecked = false;
                                ExistsItemsList[i].SourceIsChecked = true;
                            }
                            else
                            {
                                ExistsItemsList[i].TargetIsChecked = true;
                                ExistsItemsList[i].SourceIsChecked = false;
                            }
                        break;
                }

            }
        }

        private void RemoveSame()
        {
            if (same == null)
            {
                same = new List<int>();
                for (int i = 0; i < ExistsItemsList.Count; i++)
                {
                    if (ExistsItemsList[i].source_item.size == ExistsItemsList[i].TargetFile.Length)
                    {
                        SHA1 HashComputer = new SHA1Cng();
                        try
                        {
                            FileStream source_stream = File.OpenRead(ExistsItemsList[i].source_item.item.FullName);
                            FileStream target_stream = File.OpenRead(ExistsItemsList[i].TargetFile.FullName);
                            byte[] first = HashComputer.ComputeHash(source_stream);
                            byte[] second = HashComputer.ComputeHash(target_stream);
                            source_stream.Close();
                            target_stream.Close();
                            if (first.SequenceEqual(second))
                                same.Add(i);
                        }
                        catch (Exception) {  }
                    }
                }
                same_containt.Text = same.Count.ToString();
            }
        }

        private void FindUnfinished()
        {
            if (unfinished == null)
            {
                unfinished = new List<int>();
                for (int i = 0; i < ExistsItemsList.Count; i++)
                {
                    long target_length = ExistsItemsList[i].TargetFile.Length;
                    if (target_length > 0 && target_length < ExistsItemsList[i].source_item.size && IsStartOf(ExistsItemsList[i].TargetFile.FullName, ExistsItemsList[i].source_item.item.FullName, target_length))
                        unfinished.Add(i);
                }
                unfinished_count.Text = unfinished.Count.ToString();
            }
        }

        bool IsStartOf(string part_path, string full_path, long length)
        {
            try
            {
                using (FileStream part = new FileStream(part_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (FileStream full = new FileStream(full_path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] first = new byte[65536], second = new byte[65536];
                    long remain = length;
                    while (remain > 0)
                    {
                        int count = (int)Math.Min(first.Length, remain);
                        int read1 = 0, read2 = 0, k;
                        while (read1 < count && (k = part.Read(first, read1, count - read1)) > 0)
                            read1 += k;
                        while (read2 < count && (k = full.Read(second, read2, count - read2)) > 0)
                            read2 += k;
                        if (read1 != count || read2 != count)
                            return false;
                        for (int i = 0; i < count; i++)
                            if (first[i] != second[i])
                                return false;
                        remain -= count;
                    }
                    return true;
                }
            }
            catch (Exception) { return false; }
        }

        private void RemoveSameSize()
        {
            if (same_size == null)
            {
                same_size = new List<int>();
                for (int i = ExistsItemsList.Count - 1; i > -1; i--)
                    if (ExistsItemsList[i].source_item.size == ExistsItemsList[i].TargetFile.Length)
                        same_size.Add(i);   
                same_file_size.Text = same_size.Count.ToString();
            }
        }

        private void RemoveSameSizeDate()
        {
            if (same_size_date == null)
            {
                same_size_date = new List<int>();
                for (int i = ExistsItemsList.Count - 1; i >= 0; i--)
                    if ((ExistsItemsList[i].source_item.size == ExistsItemsList[i].TargetFile.Length) && (ExistsItemsList[i].source_item.item.LastWriteTime.Ticks == ExistsItemsList[i].TargetFile.LastWriteTime.Ticks))
                        same_size_date.Add(i);
                same_file_date.Text = same_size_date.Count.ToString();
            }
        }

        internal void ShowThisDialog()
        {
            Functions.MarkAs(root, OperationOption.NoValue, true);
            this.ShowDialog();
            if (!accept)
                Functions.MarkAs(root, OperationOption.Skip, true);
            else
            {
                foreach (ExistsItemsListView i in ExistsItemsList)
                    if (i.SourceIsChecked.Value)
                        if (i.TargetIsChecked.Value)
                            i.source_item.user_option = OperationOption.CopyWithNewName;
                        else
                            i.source_item.user_option = OperationOption.Over;
                    else
                        i.source_item.user_option = OperationOption.Skip;
                Functions.MarkAs(root, OperationOption.Skip, false);
            }
        }
    }

    public class ExistsItemsListView
    {
        public ExistsItemsListView(OperationTree source_file, FileInfo target_file)
        {
            FileName = source_file.item.Name;
            SourceSize = Functions.FileSize(source_file.size, 2);
            SourceDate = source_file.item.LastWriteTimeUtc.ToString();
            SourceTooltip = ((FileInfo)source_file.item).DirectoryName;
            TargetFile = target_file;
            TargetSize = Functions.FileSize(target_file.Length, 2);
            TargetDate = target_file.LastWriteTimeUtc.ToString();
            TargetTooltip = target_file.DirectoryName;
            TargetIcon = SourceIcon = Functions.GetFileImage64(source_file.item.Extension);
            SourceType = TargetType = Functions.GetFileType(source_file.item.Extension);
            target_ischecked = source_ischecked = false;
            source_item = source_file;
        }

        public OperationTree source_item;

        private String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        private BitmapImage source_icon;
        public BitmapImage SourceIcon
        {
            get { return source_icon; }
            set { source_icon = value; }
        }

        private String source_type;
        public string SourceType
        {
            get { return source_type; }
            set { source_type = value; }
        }

        private String source_size;
        public string SourceSize
        {
            get { return source_size; }
            set { source_size = value; }
        }

        private String source_date;
        public string SourceDate
        {
            get { return source_date; }
            set { source_date = value; }
        }

        private String source_tooltip;
        public string SourceTooltip
        {
            get { return source_tooltip; }
            set { source_tooltip = value; }
        }

        private bool? source_ischecked;
        public Boolean? SourceIsChecked
        {
            get { return source_ischecked; }
            set { source_ischecked = value; }
        }

        FileInfo target_file;
        public FileInfo TargetFile
        {
            get { return target_file; }
            set { target_file = value; }
        }

        private BitmapImage target_icon;
        public BitmapImage TargetIcon
        {
            get { return target_icon; }
            set { target_icon = value; }
        }

        private String target_type;
        public string TargetType
        {
            get { return target_type; }
            set { target_type = value; }
        }

        private String target_size;
        public string TargetSize
        {
            get { return target_size; }
            set { target_size = value; }
        }

        private String target_date;
        public string TargetDate
        {
            get { return target_date; }
            set { target_date = value; }
        }

        private String target_tooltip;
        public string TargetTooltip
        {
            get { return target_tooltip; }
            set { target_tooltip = value; }
        }

        private bool? target_ischecked;
        public Boolean? TargetIsChecked
        {
            get { return target_ischecked; }
            set { target_ischecked = value; }
        }
    }
}