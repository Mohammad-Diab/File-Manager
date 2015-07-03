using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.Windows.Media;


namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Files_Encrepter.xaml
    /// </summary>
    public partial class Files_Encrepter : Window
    {
        bool CloseWindow = false, starting_window = true;

        EncryptPages current_page = EncryptPages.FilesEncrypt;
        EncryptPages CurrentPage
        {
            get { return current_page; }
            set { current_page = value; }
        }

        public Files_Encrepter()
        {
            InitializeComponent();
        }

        public Files_Encrepter(string file, bool encrypt)//true for encrypt and false foredcrypt
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.Width = label1.ActualWidth;
            starting_window = false;
            encrypt_list.DataContext = encrypt_data;
            decrypt_list.DataContext = decrypt_data;
        }

        private byte[] PreparePassword(string pass)
        {
            MD5 hasher1 = new MD5Cng();
            SHA256 hasher2 = new SHA256Cng();
            var st1 = hasher1.ComputeHash(UnicodeEncoding.Unicode.GetBytes(pass));
            for (int i = 0; i < st1.Length; i++)
                st1[i]++;
            var st2 = hasher2.ComputeHash(st1);
            List<byte> tt1 = new List<byte>();
            for (int i = st2.Length; i <= 65536; i += st2.Length)
                tt1.AddRange(st2);
            return tt1.ToArray<byte>();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Select files to encrypt";
            sfd.AddExtension = false;
            sfd.MultiSelect = true;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                AddFiles(sfd.SelectedItems.ToArray(), sender == encrypt_add_button ? encrypt_data : decrypt_data, sender == encrypt_add_button ? encrypt_list : decrypt_list);
        }

        private void AddFiles(string[] files, List<DestroyListView> data_list, ListView list)
        {
            foreach (string i in files)
                data_list.Add(new DestroyListView(new FileInfo(i)));
            list.Items.Refresh();
        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Add all files in this directory.";
            sfd.AddExtension = false;
            List<DestroyListView> add_in = sender == encrypt_add_folder_button ? encrypt_data : decrypt_data;
            ListView list = sender == encrypt_add_folder_button ? encrypt_list : decrypt_list;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                AddDirectory(sfd.SelectedItem, sender == encrypt_add_folder_button ? encrypt_data : decrypt_data, sender == encrypt_add_folder_button ? encrypt_list : decrypt_list);
        }

        private void AddDirectory(string path, List<DestroyListView> data_list, ListView list)
        {
            main_grid.IsEnabled = false;
            BackgroundWorker add_files_in_directory = new BackgroundWorker();
            add_files_in_directory.WorkerReportsProgress = true;
            add_files_in_directory.DoWork += (object bw_sender, DoWorkEventArgs e_bw) =>
            { GetAllFiles(new DirectoryInfo(path), add_files_in_directory); };

            add_files_in_directory.ProgressChanged += (object bw_sender, ProgressChangedEventArgs bw_e) =>
            { data_list.Add(new DestroyListView((FileInfo)bw_e.UserState)); };

            add_files_in_directory.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
            {
                list.Items.Refresh();
                main_grid.IsEnabled = true;
            };

            add_files_in_directory.RunWorkerAsync();
        }

        private void GetAllFiles(DirectoryInfo dir, BackgroundWorker worker)
        {
            try
            {
                var sub_dirs = dir.GetDirectories();
                foreach (var i in sub_dirs)
                {
                    if (CloseWindow)
                        return;
                    try { GetAllFiles(i, worker); }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            FileInfo[] files = null;
            try
            {
                files = dir.GetFiles();
                foreach (var i in files)
                {
                    if (CloseWindow)
                        return;
                    try { worker.ReportProgress(0, i); }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (sender == encrypt_remove_button)
            {
                encrypt_data.RemoveAll(x => encrypt_list.SelectedItems.Contains(x));
                encrypt_list.Items.Refresh();
            }
            else
            {
                decrypt_data.RemoveAll(x => encrypt_list.SelectedItems.Contains(x));
                decrypt_list.Items.Refresh();
            }
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            if (sender == encrypt_clear_button)
            {
                encrypt_data.Clear();
                encrypt_list.Items.Refresh();
            }
            else
            {
                decrypt_data.Clear();
                decrypt_list.Items.Refresh();
            }

        }

        private void ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            PasswordBox password = sender == enc_show_password ? enc_password : dec_password;
            TextBox password_text = sender == enc_show_password ? enc_password_text : dec_password_text;
            if (password.Visibility == Visibility.Visible)
            {
                ((Button)sender).Content = "Hide";
                password.Visibility = Visibility.Hidden;
                password_text.Visibility = Visibility.Visible;
                password_text.Text = password.Password;
            }
            else
            {
                ((Button)sender).Content = "Show";
                password.Visibility = Visibility.Visible;
                password_text.Visibility = Visibility.Hidden;
                password.Password = password_text.Text;
            }
        }

        private void Browser_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = true;
            sfd.DialogTitle = "Save output files in:";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                if (sender == encrypt_browser_button)
                    enc_output.Text = sfd.SelectedItem;
                else
                    dec_output.Text = sfd.SelectedItem;
            }
        }

        #region ENCRYPT

        List<DestroyListView> encrypt_data = new List<DestroyListView>();

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            bool encrypt = false;
            string password = enc_password.Visibility == Visibility.Hidden ? enc_password_text.Text : enc_password.Password;
            if (encrypt_data.Count < 1)
                Messages.ShowMessage("Some field is missing.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "List is empty.");
            else if (password.Length < 8)
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Password must be at least 8 charectars.");
            else if (enc_output.Text == string.Empty)
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please select output directory.");
            else if (enc_output.Text.Any(x => Path.GetInvalidPathChars().Any(y => y.Equals(x))))
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "We have found invalid charectars in your selected path.");
            else if (!Directory.Exists(enc_output.Text))
            {
                if (Messages.ShowMessage("We can not find directory '" + enc_output.Text + "', try to create it?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.Yes)
                    encrypt = true;
            }
            else
                encrypt = true;
            if (encrypt && Messages.ShowMessage("We have got all information we needed, start encription?",ModrenMessageButton.YesNo,ModrenMessageImage.Question,ModrenMessageSound.Question,false,"", "all files in list will encrypt with same password, make sure to keep it in safe place.") == MyMessageBoxResult.Yes)
            {
                bool delete;
                string save_in = enc_output.Text;
                if (!Directory.Exists(save_in))
                {
                    try { Directory.CreateDirectory(save_in); }
                    catch (Exception ex)
                    {
                        Messages.ShowMessage("We can not create directory '" + save_in + "'", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                        return;
                    }
                }
                delete = enc_delete_files.IsChecked.Value;
                List<FileInfo> files_list = new List<FileInfo>();
                foreach (var i in encrypt_data)
                    files_list.Add(i.File);
                byte[] bytes = new byte[password.Length * sizeof(char)];
                Buffer.BlockCopy(password.ToCharArray(), 0, bytes, 0, bytes.Length);
                bytes = new MD5Cng().ComputeHash(bytes);
                new Operation().StartEncrept(files_list, bytes, delete, save_in, true);
                
                ClearList_Click(encrypt_clear_button, null);
                Messages.ShowMessage("Eencription done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
            }
        }

        private void EncryptList_Drop(object sender, DragEventArgs e)
        {
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                if ((data = (string[])e.Data.GetData(DataFormats.FileDrop)).Length == 1 && Directory.Exists(data[0]))
                    AddDirectory(data[0], encrypt_data, encrypt_list);
                else if (data.Length > 0)
                    AddFiles(data, encrypt_data, encrypt_list);
        }

        private void EncOutputText_Drop(object sender, DragEventArgs e)
        {
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                if ((data = (string[])e.Data.GetData(DataFormats.FileDrop)).Length == 1 && Directory.Exists(data[0]))
                    enc_output.Text = data[0];
        }

        #endregion

        #region DECRYPT

        List<DestroyListView> decrypt_data = new List<DestroyListView>();

        private void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            bool decrypt = false;
            string password = dec_password.Visibility == Visibility.Hidden ? dec_password_text.Text : dec_password.Password;
            if (decrypt_data.Count < 1)
                Messages.ShowMessage("Some field is missing.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "List is empty.");
            else if (password.Length < 8)
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Password must be at least 8 charectars.");
            else if (dec_output.Text == string.Empty)
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please select output directory.");
            else if (dec_output.Text.Any(x => Path.GetInvalidPathChars().Any(y => y.Equals(x))))
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "We have found invalid charectars in your selected path.");
            else if (!Directory.Exists(dec_output.Text))
            {
                if (Messages.ShowMessage("We can not find directory '" + dec_output.Text + "', try to create it?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.Yes)
                    decrypt = true;
            }
            else
                decrypt = true;
            if (decrypt && Messages.ShowMessage("We have got all information we needed, start de-encription?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "", "make sure that all files in the list has same password.") == MyMessageBoxResult.Yes)
            {
                string save_in = dec_output.Text;
                if (!Directory.Exists(save_in))
                {
                    try { Directory.CreateDirectory(save_in); }
                    catch (Exception ex)
                    {
                        Messages.ShowMessage("We can not create directory '" + save_in + "'", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                        return;
                    }
                }
                List<FileInfo> files_list = new List<FileInfo>();
                foreach (var i in decrypt_data)
                    files_list.Add(i.File);
                byte[] bytes = new byte[password.Length * sizeof(char)];
                Buffer.BlockCopy(password.ToCharArray(), 0, bytes, 0, bytes.Length);
                bytes = new MD5Cng().ComputeHash(bytes);
                new Operation().StartDecrept(files_list, bytes, save_in, true);
                ClearList_Click(null, null);
                Messages.ShowMessage("De-encription done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
            }
        }

        private void DecryptList_Drop(object sender, DragEventArgs e)
        {
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                if ((data = (string[])e.Data.GetData(DataFormats.FileDrop)).Length == 1 && Directory.Exists(data[0]))
                    AddDirectory(data[0], decrypt_data, decrypt_list);
                else if (data.Length > 0)
                    AddFiles(data, decrypt_data, decrypt_list);
        }

        private void DecOutputText_Drop(object sender, DragEventArgs e)
        {
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                if ((data = (string[])e.Data.GetData(DataFormats.FileDrop)).Length == 1 && Directory.Exists(data[0]))
                    dec_output.Text = data[0];
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
            CurrentPage = (EncryptPages)tab_panal.Children.IndexOf((Label)sender) + 1;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!starting_window)
            {
                //if(current_page==DestroyPages.DiskDestroy)//****
                double t = (-1 * ((int)CurrentPage - 1)) * e.NewSize.Width;
                ThicknessAnimation ta = new ThicknessAnimation(new Thickness(t, 0, 0, 0), new Duration(new TimeSpan(0)));
                mother.BeginAnimation(MarginProperty, ta);
                for (int i = 0; i < mother.Children.Count; i++)
                {
                    ((Grid)mother.Children[i]).Width = ActualWidth - 5;
                    ((Grid)mother.Children[i]).Margin = new Thickness(i * e.NewSize.Width, 0, 0, 0);
                }
            }
        }

    }

    enum EncryptPages
    {
        FilesEncrypt = 1,
        FilesDecrypt = 2
    }
}