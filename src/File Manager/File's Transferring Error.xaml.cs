using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
    /// Interaction logic for File_s_Transferring_Error.xaml
    /// </summary>
    public partial class File_s_Transferring_Error : Window
    {
        ObservableCollection<OperationErrorListView> OperationErrorList = new ObservableCollection<OperationErrorListView>();
        bool user_active = false;
        int accept = 0;     //0 for cancel, 1 for OK, 2 for ratry all;
        OperationTree root;

        public File_s_Transferring_Error(OperationTree root, string header)
        {
            InitializeComponent();
            this.root = root;
            container.DataContext = OperationErrorList;
            if (header != null)
                sub_containt.Text = header;
            AddSuns(root, root.operation != Op.Copy);
        }

        private void AddSuns(OperationTree root, bool add_directory)
        {
            foreach (var i in root.children)
            {
                if (i.children == null)
                    OperationErrorList.Add(new OperationErrorListView(i));
                else
                {
                    if (add_directory)
                        OperationErrorList.Add(new OperationErrorListView(i));
                    AddSuns(i, i.operation == Op.Delete);
                }
                i.error_message = "";
            }
        }

        //public File_s_Transferring_Error(List<UnCompleatedRename> uncomplete, string header)
        //{
        //    InitializeComponent();
        //    foreach (var i in uncomplete)
        //        OperationErrorList.Add(new OperationErrorListView(i.Item, i.ErrorMessage));
        //    if (header != null)
        //        sub_containt.Text = header;
        //}

        private void Container_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            container.SelectedIndex = -1;
        }

        private void RetryAll_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, 1);
        }

        private void SkipAll_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, -1);
        }

        private void RetryAll_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, 1);
        }

        private void SkipAll_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, -1);
        }

        private void ItemRadio_Click(object sender, RoutedEventArgs e)
        {
            CheckUncheckSkip();     //to cheak or uncheack skip
            CheckUncheckRetry();    //to cheak or uncheack retry
        }

        private void CheckUncheckSkip()
        {
            if (!user_active)
            {
                user_active = true;
                int j = 0;
                foreach (OperationErrorListView i in OperationErrorList)
                    if (i.SkipIsChecked == true)
                        j++;
                if (j == OperationErrorList.Count)
                    skip_radiobutton.IsChecked = true;
                else if (j == 0)
                    skip_radiobutton.IsChecked = false;
                else
                    skip_radiobutton.IsChecked = null;
                user_active = false;
            }
        }

        private void CheckUncheckRetry()
        {
            if (!user_active)
            {
                user_active = true;
                int j = 0;
                foreach (OperationErrorListView i in OperationErrorList)
                    if (i.RetryIsChecked == true)
                        j++;
                if (j == OperationErrorList.Count)
                    retry_radiobutton.IsChecked = true;
                else if (j == 0)
                    retry_radiobutton.IsChecked = false;
                else
                    retry_radiobutton.IsChecked = null;
                user_active = false;
            }
        }

        private void CheckUncheckAll(bool p, int index)
        {
            user_active = true;
            if (index > 0)
            {
                foreach (OperationErrorListView i in OperationErrorList)
                    i.RetryIsChecked = p;
                if (p)
                    skip_radiobutton.IsChecked = false;
            }
            else
            {
                foreach (OperationErrorListView i in OperationErrorList)
                    i.SkipIsChecked = p;
                if (p)
                    retry_radiobutton.IsChecked = false;
            }
            container.Items.Refresh();
            user_active = false;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            accept = 1;
            Close();
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            accept = 2;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            accept = 0;
            Close();
        }

        internal void ShowThisDialog()
        {
            if (OperationErrorList.Count > 0)
                ShowDialog();
            else
                Cancel_Click(null, null);
            Functions.MarkAs(root, OperationOption.NoValue, true);
            if (accept == 0)
                Functions.MarkAs(root, OperationOption.Skip, true);
            else if (accept == 1)
                foreach (OperationErrorListView i in OperationErrorList)
                    if (i.RetryIsChecked == true)
                        i.Item.user_option = OperationOption.Retry;
                    else
                        i.Item.user_option = OperationOption.Skip;
            else
                Functions.MarkAs(root, OperationOption.Retry, true);
        }
    }

    public class OperationErrorListView
    {
        public OperationErrorListView(OperationTree item)
        {
            FileName = item.item.Name;
            Item = item;
            ErrorMsg = "Error: " + item.error_message;

            if (item.item is DirectoryInfo)
            {
                Icon = Functions.GetFileImage64("directory");
                FileType = "Directory";
            }
            else
            {
                Icon = Functions.GetFileImage64(item.item.Extension);
                FileType = Functions.GetFileType(item.item.Extension);
            }
            skip_ischecked = retry_ischecked = false;
        }

        private String file_name;
        public string FileName
        {
            get { return file_name; }
            set { file_name = value; }
        }

        OperationTree item;
        public OperationTree Item
        {
            get { return item; }
            set { item = value; }
        }

        private BitmapImage item_icon;
        public BitmapImage Icon
        {
            get { return item_icon; }
            set { item_icon = value; }
        }

        private String item_type;
        public string FileType
        {
            get { return item_type; }
            set { item_type = value; }
        }

        private String error_msg;
        public string ErrorMsg
        {
            get { return error_msg; }
            set { error_msg = value; }
        }

        private bool? retry_ischecked;
        public Boolean? RetryIsChecked
        {
            get { return retry_ischecked; }
            set
            {
                retry_ischecked = value;
                if (value == true)
                    skip_ischecked = false;
                else if (value == false)
                    skip_ischecked = true;
            }
        }

        private bool? skip_ischecked;
        public Boolean? SkipIsChecked
        {
            get { return skip_ischecked; }
            set
            {
                skip_ischecked = value;
                if (value == true)
                    retry_ischecked = false;
                else if (value == false)
                    retry_ischecked = true;
            }
        }
    }
}
