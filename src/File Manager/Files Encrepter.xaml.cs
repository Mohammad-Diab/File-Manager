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
using System.Windows.Controls.Primitives;

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

        public Files_Encrepter(string[] files, bool encrypt)//true for encrypt and false for decrypt
        {
            InitializeComponent();
            if (encrypt)
                AddFiles(files, encrypt_data, encrypt_list);
            else
            {
                AddFiles(files, decrypt_data, decrypt_list);
                CurrentPage = EncryptPages.FilesDecrypt;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.Width = label1.ActualWidth;
            starting_window = false;
            encrypt_list.DataContext = encrypt_data;
            decrypt_list.DataContext = decrypt_data;
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
            if (((ToggleButton)sender).IsChecked.Value)
            {
                ((ContentControl)sender).ToolTip = "Hide password";
                password.Visibility = Visibility.Hidden;
                password_text.Visibility = Visibility.Visible;
                password_text.Text = password.Password;
            }
            else
            {
                ((ContentControl)sender).ToolTip = "Show password";
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
            else if (overwrite_file.IsChecked == false)
            {
                if (enc_output.Text == string.Empty)
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
            }
            else
                encrypt = true;
            if (encrypt && Messages.ShowMessage("We have got all information we needed, start encryption?", ModrenMessageButton.YesNo,ModrenMessageImage.Question,ModrenMessageSound.Question,false,"", "all files in list will encrypt with same password, make sure to keep it in safe place.") == MyMessageBoxResult.Yes)
            {
                bool delete, enc_file_name, enc_pack_files;
                string save_in = enc_output.Text;
                if (overwrite_file.IsChecked == false)
                {
                    if (!Directory.Exists(save_in))
                    {
                        try { Directory.CreateDirectory(save_in); }
                        catch (Exception ex)
                        {
                            Messages.ShowMessage("We can not create directory '" + save_in + "'", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                            return;
                        }
                    }
                }
                delete = enc_delete_files.IsChecked.Value;
                enc_file_name = this.enc_file_name.IsChecked.Value;
                enc_pack_files = this.enc_pack_files.IsChecked.Value;
                List<FileInfo> files_list = new List<FileInfo>();
                foreach (var i in encrypt_data)
                    files_list.Add(i.File);
                byte[] bytes = new byte[password.Length * sizeof(char)];
                Buffer.BlockCopy(password.ToCharArray(), 0, bytes, 0, bytes.Length);
                bytes = new MD5Cng().ComputeHash(bytes);
                new Operation().StartEncrept(files_list, bytes, delete, overwrite_file.IsChecked.Value, save_in, enc_file_name, enc_pack_files, true);
                
                ClearList_Click(encrypt_clear_button, null);
                Messages.ShowMessage("Encryption done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
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

        private void EncryptOverwriteFile_Checked(object sender, RoutedEventArgs e)
        {
            enc_output_grid.IsEnabled = false;
            enc_delete_files.IsChecked = enc_delete_files.IsEnabled = false;
            enc_pack_files.IsChecked = encrypt_add_button.IsEnabled = false;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (enc_output_grid != null)
            {
                enc_output_grid.IsEnabled = true;
                enc_delete_files.IsEnabled = true;
                encrypt_add_button.IsEnabled = true;
            }
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
            if (decrypt && Messages.ShowMessage("We have got all information we needed, start de-encryption?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "", "make sure that all files in the list has same password.") == MyMessageBoxResult.Yes)
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
                Messages.ShowMessage("De-encryption done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
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
            ThicknessAnimation ta = new ThicknessAnimation(new Thickness((-1 * tab_panal.Children.IndexOf((Label)sender)) * ActualWidth, 40, 0, 0), new Duration(new TimeSpan(2000000)));
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

        private void AboutTool_Click(object sender, RoutedEventArgs e)
        {
            //version 1:
            //first relese
            //1.1:
            //imrove speed
            //improve interface
            //2:
            //new interface
            //new encrype method (old files will not dencrypt in this version)
          // new not published //2.1:
            //add method(fast-mid-slow)
            //encrypt on same file or encrypt to other file
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!starting_window)
            {
                //if(current_page==DestroyPages.DiskDestroy)//****
                double t = (-1 * ((int)CurrentPage - 1)) * e.NewSize.Width;
                ThicknessAnimation ta = new ThicknessAnimation(new Thickness(t, 40, 0, 0), new Duration(new TimeSpan(0)));
                mother.BeginAnimation(MarginProperty, ta);
                for (int i = 0; i < mother.Children.Count; i++)
                {
                    ((Grid)mother.Children[i]).Width = ActualWidth - 5;
                    ((Grid)mother.Children[i]).Margin = new Thickness(i * e.NewSize.Width, 0, 0, 0);
                }
            }
        }




        static SHA256Cng sha1 = new SHA256Cng();
        static MD5 md5 = new MD5Cng();
        public static byte[] general_password = { 3, 130, 64, 104, 54, 111, 89, 162, 66, 76, 93, 113, 180, 185, 75, 222, 104, 214, 161, 4, 79, 124, 48, 122, 155, 200, 210, 132, 170, 19, 234, 188, 148, 158, 168, 128, 36, 98, 152, 195, 226, 194, 25, 190, 6, 26, 15, 144, 254, 9, 112, 108, 101, 219, 241, 3, 88, 9, 228, 135, 230, 148, 145, 223, 184, 8, 14, 15, 188, 186, 24, 190, 154, 211, 127, 17, 16, 84, 41, 172, 189, 31, 4, 31, 122, 236, 252, 236, 213, 99, 163, 99, 193, 49, 185, 49, 176, 207, 223, 207, 40, 69, 49, 69, 232, 248, 121, 248, 102, 75, 83, 75, 72, 14, 133, 14, 165, 20, 190, 20, 238, 10, 100, 10, 175, 178, 153, 178 };
        public static byte[] general_secret_number = { 3, 130, 64, 104, 54, 111, 89, 162, 66, 76, 93, 113, 180, 185, 75, 222, 104, 214, 161, 4, 79, 124, 48, 122, 155, 200, 210, 132, 170, 19, 234, 188, 148, 158, 168, 128, 36, 98, 152, 195, 226, 194, 25, 190, 6, 26, 15, 144, 254, 9, 112, 108, 101, 219, 241, 3, 88, 9, 228, 135, 230, 148, 145, 223, 184, 8, 14, 15, 188, 186, 24, 190, 154, 211, 127, 17, 16, 84, 41, 172, 189, 31, 4, 31, 122, 236, 252, 236, 213, 99, 163, 99, 193, 49, 185, 49, 176, 207, 223, 207, 40, 69, 49, 69, 232, 248, 121, 248, 102, 75, 83, 75, 72, 14, 133, 14, 165, 20, 190, 20, 238, 10, 100, 10, 175, 178, 153, 178 };

        internal static byte[] EncryptBytes(byte[] word, byte[] key)
        {
            byte[] encrypted_bytes = new byte[word.Length];
            Parallel.For(0, word.Length, (i) =>
            {
                encrypted_bytes[i] = (byte)(word[i] ^ key[i % key.Length]);
                encrypted_bytes[i]--;
            });
            return encrypted_bytes;
        }

        internal static byte[] DecryptBytes(byte[] word, byte[] key)
        {
            byte[] decrypted_bytes = new byte[word.Length];
            Parallel.For(0, word.Length, (i) =>
            {
                word[i]++;
                decrypted_bytes[i] = (byte)(word[i] ^ key[i % key.Length]);
            });
            return decrypted_bytes;
        }

        internal static byte[] PreparePassword(byte[] password, byte[] secret_number, int length)
        {
            var sha_hash = new SHA512Cng();
            var code1 = sha_hash.ComputeHash(password);
            var code2 = sha_hash.ComputeHash(secret_number);
            byte[] secret_code = new byte[code2.Length];
            Parallel.For(0, code1.Length, (i) => { secret_code[i] = (byte)((code1[i] + code2[i]) / 2 + 8); });
            byte[] encryption_code = new byte[length];
            int iteration = 1;
            for (int i = 0; i < length; i++)
            {
                encryption_code[i] = secret_code[iteration];
                iteration++;
                if (iteration == code1.Length)
                    iteration = 0;
            }
            return encryption_code;
        }

        //SecretNumber must generate depend on password
        internal static byte[] GetSecretNumber(int length)
        {
            byte[] secret_number = new byte[length];
            new Random((int)(0/*DateTime.Now.Ticks / 34553786873*/)).NextBytes(secret_number);
            return secret_number;
            //byte[] secret_number = new byte[length];
            //var bytes = BitConverter.GetBytes(key);
            //for (int i = 0; i < length; i++)
            //    secret_number[i] = bytes[i % bytes.Length];
            //return secret_number;
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            var b1 = new byte[16];
            new Random(new Random(DateTime.Now.Millisecond).Next()).NextBytes(b1);
            byte[] pass = new byte[enc_password_text.Text.Length * 2];
            Buffer.BlockCopy(enc_password_text.Text.ToCharArray(), 0, pass, 0, pass.Length);
            var sec_num = GetSecretNumber(128);
            byte[] encrypt_key = PreparePassword(pass, sec_num, 16);
            var enc_word = EncryptBytes(b1, encrypt_key);

            byte[] dec_word = DecryptBytes(enc_word, encrypt_key);

            MessageBox.Show(StringMaker(pass, sec_num, encrypt_key, enc_word, dec_word, b1), "Result");
        }

        string StringMaker(byte[] pass, byte[] sec_num, byte[] enc_key, byte[] enc_word, byte[] dec_word, byte[] real_word)
        {
            StringBuilder mm = new StringBuilder();
            mm.Append("Password\t\t\t: ");
            foreach (var i in pass)
                mm.Append(i + ", ");
            mm.Append("\n");

            mm.Append("Secret Number\t: ");
            foreach (var i in sec_num)
                mm.Append(i + ", ");
            mm.Append("\n");

            mm.Append("Encrypt Key\t\t: ");
            foreach (var i in enc_key)
                mm.Append(i + ", ");
            mm.Append("\n");

            mm.Append("Enc Word\t\t\t: ");
            foreach (var i in enc_word)
                mm.Append(i + ", ");
            mm.Append("\n");
            mm.Append("\n");
            mm.Append("\n");

            mm.Append("read Word\t\t\t: ");
            foreach (var i in real_word)
                mm.Append(i + ", ");
            mm.Append("\n");

            mm.Append("Dec Word\t\t\t: ");
            foreach (var i in dec_word)
                mm.Append(i + ", ");
            mm.Append("\n");

            return mm.ToString();
        }

        internal static byte[] HashBytes(byte[] bytes, int length)
        {
            var password = md5.ComputeHash(bytes);
            for (int i = 0; i < password.Length; i++)
                password[i]++;
            password = sha1.ComputeHash(password);
            if (length > 0)
            {
                int iteration = 1;
                byte[] password_hash = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    password_hash[i] = password[iteration];
                    iteration++;
                    if (iteration == password.Length)
                        iteration = 0;
                }
                return password_hash;
            }
            return password;
        }

    }

    enum EncryptPages
    {
        FilesEncrypt = 1,
        FilesDecrypt = 2
    }

    class EncryptFileHeader
    {
        internal bool is_package;
        internal bool encrypt_files_name;
        internal string tool_version;
        internal float version;
        internal byte[] secret_number;
        internal byte[] password_hash;
        long first_file_index;
        internal long first_file_length;
        internal int files_count;
        internal List<EncryptFileHeaderIndex> indices;

        internal EncryptFileHeader(bool IsPackage,bool EncryptFilesName, float ToolVersion, byte[] SecretNumber, byte[] PasswordHash, int FilesCount)
        {
            is_package = IsPackage;
            encrypt_files_name = EncryptFilesName;
            tool_version = ToolVersion.ToString();
            version = ToolVersion;
            secret_number = SecretNumber;
            password_hash = PasswordHash;
            files_count = FilesCount;
            indices = new List<EncryptFileHeaderIndex>();
        }

        internal long GetFirstFileIndex()
        {
            first_file_index = 4 + 38 + 4 + 128 + 128 + 8 + 8 + 4;
            foreach (var i in indices)
                first_file_index += (i.file_name_length * 2);
            return first_file_index + 128;
            // 300(288 for each file indises and 12 byte for extra info) + 8 for id + 38 file manager version + 4 version + 128 secrut number + 128 password + 8 first file start + 8 first file length + 8 files count;
        }

        internal void SetFirstFileInfo(long index, long length)
        {
            first_file_index = index;
            first_file_length = length;
        }
    }

    class EncryptFileHeaderIndex
    {
        internal int id;
        internal byte file_name_length;
        internal byte[] fileName;
        internal string file_name;
        internal long start_index;
        internal long length;
        internal byte[] crc;

        internal EncryptFileHeaderIndex(int ID, FileInfo file, byte[] encrypter)
        {
            id = ID;
            file_name_length = (byte)file.Name.Length;//(byte)file.Name.Length;
            byte[] bytes = new byte[file_name_length * sizeof(char)];
            //var name_byte = Encoding.Unicode.GetBytes(file.Name);
            //string name2 = Encoding.Unicode.GetString(name_byte);
            Buffer.BlockCopy(file.Name.ToCharArray(), 0, bytes, 0, file_name_length * sizeof(char));
            //
            //string jjj = "real name:\n";
            //foreach (var i in bytes)
            //    jjj += (i + " ,");
            //MessageBox.Show(jjj);
            //
            //string name = Encoding.Unicode.GetString(bytes);
            fileName = Files_Encrepter.EncryptBytes(bytes, encrypter);

            //
            //string kkk = "enc name:\n";
            //foreach (var i in fileName)
            //    kkk += (i + " ,");
            //MessageBox.Show(kkk);
            //

            length = file.Length;
            crc = new byte[8];//encrypt with user or general password
        }

        internal EncryptFileHeaderIndex(byte[] ID, byte[] encrypted_name, byte[] start_index, byte[] length, byte[] encrypted_CRC, byte[] decrypter)
        {
            //
            //string kkk = "enc name:\n";
            //foreach (var i in encrypted_name)
            //    kkk += (i + " ,");
            //MessageBox.Show(kkk);
            //

            id = BitConverter.ToInt32(ID, 0);
            file_name_length = (byte)(encrypted_name.Length / sizeof(char));
            file_name = Encoding.Unicode.GetString(Files_Encrepter.DecryptBytes(encrypted_name, decrypter));
            //
            //string jjj = "decryptez name:\n";
            //foreach (var i in Files_Encrepter.DecryptBytes(encrypted_name, decrypter))
            //    jjj += (i + " ,");
            //MessageBox.Show(jjj);
            //
            this.start_index = BitConverter.ToInt64(start_index, 0);
            this.length = BitConverter.ToInt64(length, 0);
            crc = Files_Encrepter.DecryptBytes(encrypted_CRC, decrypter);
        }

        internal void SetCRC(byte[] crc, byte[] encrypter)
        {
            this.crc = Files_Encrepter.EncryptBytes(crc, encrypter);
        }
    }
}

//Gnerat Password
//private void button_Click(object sender, RoutedEventArgs e)
//{
//    string password = "FILE MANAGER";
//    byte[] bytes = new byte[password.Length * sizeof(char)];
//    Buffer.BlockCopy(password.ToCharArray(), 0, bytes, 0, bytes.Length);
//    var a1 = new SHA512Cng().ComputeHash(bytes);
//    var a2 = new MD5Cng().ComputeHash(bytes);
//    byte[] t1 = new byte[32];
//    new Random(DateTime.Now.Millisecond).NextBytes(t1);
//    for (int i = 0; i < a2.Length; i++)
//        t1[i] = a2[i];
//    var a3 = new SHA1Cng().ComputeHash(bytes);
//    byte[] t2 = new byte[32];
//    new Random(DateTime.Now.Millisecond).NextBytes(t2);
//    for (int i = 0; i < a3.Length; i++)
//        t2[i] = a3[i];
//    string p = "";
//    for (int i = 0; i < 128; i++)
//    {
//        if (i % 2 == 0)
//            p += (a1[i / 2] + ", ");
//        else
//            if ((i / 2) % 2 == 0)
//            p += (t1[i / 4] + ", ");
//        else
//            p += (t2[i / 4] + ", ");
//    }
//    MessageBox.Show(p);
//}