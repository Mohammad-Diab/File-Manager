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
        //List<UnCompleatedOperation> uncomplete;
        bool wait = false;
        int accept = 0;//0 for cancel, 1 for OK, 2 for ratry all;
        //private List<UnCompleatedRename> un_done;

        public File_s_Transferring_Error(List<UnCompleatedOperation> uncomplete, string header)
        {
            InitializeComponent();
            container.DataContext = OperationErrorList;
            foreach (var i in uncomplete)
                if (i.ErrorMessage != "File is already exists.")
                    OperationErrorList.Add(new OperationErrorListView(i.Item.Item, i.ErrorMessage));
            if (header != null)
                sub_containt.Text = header;
        }

        public File_s_Transferring_Error(List<UnCompleatedRename> uncomplete, string header)
        {
            InitializeComponent();
            foreach (var i in uncomplete)
                OperationErrorList.Add(new OperationErrorListView(i.Item, i.ErrorMessage));
            if (header != null)
                sub_containt.Text = header;
        }

        //private void From_Checked(object sender, RoutedEventArgs e)
        //{
        //    CheckUncheckSkip(2, skip_radiobutton);
        //    CheckUncheckSkip(3, retry_radiobutton);
        //}

        private void CheckUncheckSkip()
        {
            if (!wait)
            {
                wait = true;
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
                wait = false;
            }
        }

        private void CheckUncheckRetry()
        {
            if (!wait)
            {
                wait = true;
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
                wait = false;
            }
        }

        private void CheckUncheckAll(bool p, int index)
        {
            wait = true;
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
            wait = false;
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

        internal List<bool> ShowThisDialog()
        {
            List<bool> retry = new List<bool>();
            this.ShowDialog();
            if (accept == 0)
                foreach (var i in OperationErrorList)
                    retry.Add(false);
            else if (accept == 1)
                foreach (OperationErrorListView i in OperationErrorList)
                    if (i.RetryIsChecked == true)
                        retry.Add(true);
                    else
                        retry.Add(false);
            else
                foreach (var i in OperationErrorList)
                    retry.Add(true);
            return retry;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
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

        private void container_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            container.SelectedIndex = -1;
        }

        private void ItemRadio_Click(object sender, RoutedEventArgs e)
        {
            CheckUncheckSkip();     //to cheak or uncheack skip
            CheckUncheckRetry();    //to cheak or uncheack retry
        }
    }
}
