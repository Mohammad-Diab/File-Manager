using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Security.Cryptography;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security;
using System.Threading;
using System.Threading.Tasks;


namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Files_Encrepter.xaml
    /// </summary>
    public partial class Files_Encrepter : Window
    {
        public Files_Encrepter()
        {
            InitializeComponent();
            files_list.DataContext = list_to_encrypt;
            files_list2.DataContext = list_to_decrypt;
        }

        //private byte[] GetPasswordBytes(SecureString pass)
        //{
        //    //SHA1Cng sha1 = new SHA1Cng();
        //    //return sha1.ComputeHash(Encoding.UTF8.GetBytes(pass));
        //    byte[] ba = null;

        //    if (pass.Length == 0)
        //        ba = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        //    else
        //    {
        //        IntPtr unmanagedBytes = Marshal.SecureStringToGlobalAllocAnsi(pass);
        //        try
        //        {
        //            unsafe
        //            {
        //                byte* byteArray = (byte*)unmanagedBytes.ToPointer();
        //                byte* pEnd = byteArray;
        //                while (*pEnd++ != 0) { }
        //                int length = (int)((pEnd - byteArray) - 1);
        //                ba = new byte[length];
        //                for (int i = 0; i < length; ++i)
        //                {
        //                    byte dataAtIndex = *(byteArray + i);
        //                    ba[i] = dataAtIndex;
        //                }
        //            }
        //        }
        //        finally
        //        { Marshal.ZeroFreeGlobalAllocAnsi(unmanagedBytes); }
        //    }
        //    return System.Security.Cryptography.SHA256.Create().ComputeHash(ba);
        //}

        private string PrepareFile(FileInfo fileInfo, string output_directory)
        {
            if (!Directory.Exists(output_directory))
            {
                string[] s = output_directory.Split(Functions.spliter, StringSplitOptions.RemoveEmptyEntries);
                string pp = s[0];
                for (int i = 1; i < s.Length; i++)
                {
                    pp += ("\\" + s[i]);
                    if (!Directory.Exists(pp))
                        try { Directory.CreateDirectory(pp); }
                        catch (Exception) { }
                }
            }
            string new_path = output_directory + "\\" + fileInfo.Name;
            int k = 1;
            string name = fileInfo.Name.Remove(fileInfo.Name.Length - fileInfo.Extension.Length);
            while (File.Exists(new_path) || Directory.Exists(new_path))
            { new_path = output_directory + "\\" + name + " (" + k + ")" + fileInfo.Extension; k++; }
            try
            { File.Create(new_path).Close(); }
            catch (Exception) { }
            return new_path;
        }

        #region ENCRYPT
        private byte[] enc_password_byte;
        ObservableCollection<DestroyListView> list_to_encrypt = new ObservableCollection<DestroyListView>();
        int complete;
        bool delete;
        string enc_output_directory;

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

        //private byte[] AES_Encrypt(byte[] bytesToBeEncrypted)
        //{
        //    byte[] encryptedBytes = null;
        //    byte[] saltBytes = new byte[] { 12, 22, 255, 124, 78, 63, 23, 89, 54, 35, 89, 125, 225, 101, 3, 88, 4, 2, 86, 12, 32, 1, 47, 20, 14, 232, 17, 3, 1, 7, 89, 66 };
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        using (RijndaelManaged AES = new RijndaelManaged())
        //        {
        //            AES.KeySize = 256;
        //            AES.BlockSize = 128;
        //            var key = new Rfc2898DeriveBytes(enc_password_bytes, saltBytes, 10);
        //            AES.Key = key.GetBytes(32);
        //            AES.IV = key.GetBytes(16);
        //            AES.Mode = CipherMode.CBC;
        //            using (CryptoStream cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
        //            {
        //                cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
        //                cs.Close();
        //            }
        //            encryptedBytes = ms.ToArray();
        //        }
        //    }
        //    return encryptedBytes;
        //}

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                foreach (string i in OFD.FileNames)
                {
                    FileInfo file = new FileInfo(i);
                    list_to_encrypt.Add(new DestroyListView(i, Functions.GetFileImage24(file.Extension), file));
                    items_count.Text = list_to_encrypt.Count + (list_to_encrypt.Count > 1 ? " Item" : " Items");
                }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                destroy_control.IsEnabled = false;
                BackgroundWorker add_all_files_in_dir_bw = new BackgroundWorker();
                add_all_files_in_dir_bw.WorkerSupportsCancellation = true;
                add_all_files_in_dir_bw.WorkerReportsProgress = true;
                add_all_files_in_dir_bw.DoWork += AddFilesBW_DoWork;
                add_all_files_in_dir_bw.ProgressChanged += AddFilesBW_ProgressChanged;
                add_all_files_in_dir_bw.RunWorkerCompleted += AddFilesBW_RunWorkerCompleted;
                add_all_files_in_dir_bw.RunWorkerAsync(path);
            }
        }

        void AddFilesBW_DoWork(object sender, DoWorkEventArgs e)
        {
            GetAllFiles(e.Argument.ToString(), (BackgroundWorker)sender);
        }

        private void GetAllFiles(string dir, BackgroundWorker bw)
        {
            string[] sub_dirs = null;
            try { sub_dirs = Directory.GetDirectories(dir); }
            catch (Exception) { }
            FileInfo[] files = null;
            try
            {
                DirectoryInfo directory = new DirectoryInfo(dir);
                files = directory.GetFiles();
            }
            catch (Exception) { }
            if (files != null)
                foreach (var i in files)
                    bw.ReportProgress(0, i);
            if (sub_dirs == null)
                return;
            else
                foreach (var i in sub_dirs)
                    GetAllFiles(i, bw);
        }

        void AddFilesBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileInfo i = (FileInfo)e.UserState;
            try
            { list_to_encrypt.Add(new DestroyListView(i.FullName, Functions.GetFileImage24(i.Extension), i)); }
            catch (Exception) { }
        }

        void AddFilesBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (list_to_encrypt.Count > 0)
                items_count.Text = list_to_encrypt.Count + (list_to_encrypt.Count > 1 ? " Items" : " Item");
            else
                items_count.Text = "";
            destroy_control.IsEnabled = true;
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            for (int i = files_list.SelectedItems.Count - 1; i >= 0; i--)
                list_to_encrypt.RemoveAt(files_list.Items.IndexOf(files_list.SelectedItems[i]));
            if (list_to_encrypt.Count > 0)
                items_count.Text = list_to_encrypt.Count + (list_to_encrypt.Count == 1 ? " Item" : " Items");
            else
                items_count.Text = string.Empty;
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            list_to_encrypt.Clear();
            items_count.Text = "";
        }

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            //message box question???????????????????????????????????????????????
            if (list_to_encrypt.Count > 0)
                if (new My_MessageBox("Confirmation", "Start Encription?", "Caution: do not loose your password.", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                {
                    complete = 0;
                    destroy_control.IsEnabled = false;
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.WorkerSupportsCancellation = true;
                    bw.WorkerReportsProgress = true;
                    bw.DoWork += EncryptBW_DoWork;
                    bw.ProgressChanged += EncryptBW_ProgressChanged;
                    bw.RunWorkerCompleted += EncryptBW_RunWorkerCompleted;
                    delete = delete_files.IsChecked.Value;
                    total_progress.Visibility = Visibility.Visible;
                    total_progress.Value = 0;
                    total_progress.Maximum = list_to_encrypt.Count;
                    enc_output_directory = output.Text;
                    enc_password_byte = PreparePassword(enc_password.Password);
                    bw.RunWorkerAsync();
                }
        }

        private void EncryptBW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            Thread process_thread = new Thread(() =>
            {
                while (bw.IsBusy)
                {
                    bw.ReportProgress(0);
                    Thread.Sleep(200);
                }
            });
            process_thread.Start();
            for (int index = 0; index < list_to_encrypt.Count; index++)
            {
                list_to_encrypt[index].CurrentProgress = 0;
                string write_file_in = PrepareFile(list_to_encrypt[index].File, enc_output_directory);
                if (!File.Exists(write_file_in) || !list_to_encrypt[index].File.Exists)
                    continue;
                BinaryReader BR = new BinaryReader(list_to_encrypt[index].File.OpenRead());
                BinaryWriter BW = new BinaryWriter(File.Open(write_file_in, FileMode.OpenOrCreate));
                bool done = false;
                try
                {
                    long k;
                    BW.Write("THIS FILE HAD ENCRYPTED WITH FILE MANAGER");
                    for (int i = 0; i < 256; i++)
                        BW.Write(enc_password_byte[i]);
                    for (k = 65536; k <= list_to_encrypt[index].File.Length; k += 65536)
                    {
                        BW.Write(Encrypt(BR.ReadBytes(65536)));
                        list_to_encrypt[index].CurrentProgress += 65536;
                    }
                    int remain = (int)(list_to_encrypt[index].File.Length - (k - 65536));
                    BW.Write(Encrypt(BR.ReadBytes(remain)));
                    list_to_encrypt[index].CurrentProgress += remain;
                    done = true;
                    list_to_encrypt[index].TextBlockVisibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    list_to_encrypt[index].CurrentProgress = 0;
                    list_to_encrypt[index].TextBlockVisibility = Visibility.Visible;
                    list_to_decrypt[index].ProgressBarVisibility = Visibility.Collapsed;
                    list_to_encrypt[index].ErrorMassege = ex.Message;
                    BR.Close();
                    BW.Close();
                    if (File.Exists(write_file_in))
                        try { File.Delete(write_file_in); }
                        catch (Exception) { } continue;
                }
                BR.Close();
                BW.Close();
                //list_to_destroy[index].CurrentProgress = list_to_destroy[index].MaximumValue;
                try
                {
                    if (done)
                    {
                        complete++;
                        if (delete)
                            list_to_encrypt[index].File.Delete();
                    }
                }
                catch (Exception) { }
            }
        }

        private byte[] Encrypt(byte[] bytes)
        {
            byte[] encrept = new byte[bytes.Length];
            Parallel.For(0, bytes.Length, (i) => { encrept[i] = (byte)(bytes[i] + enc_password_byte[i]); });
            return encrept;
        }

        private void EncryptBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            files_list.Items.Refresh();
            total_progress.Value = complete;
        }

        private void EncryptBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            destroy_control.IsEnabled = true;
            total_progress.Visibility = Visibility.Hidden;
            files_list.Items.Refresh();
            new My_MessageBox("FILE MANAGER", "Operation done.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
        }

        #endregion

        #region DECRYPT
        private byte[] dec_password_byte;
        ObservableCollection<DestroyListView> list_to_decrypt = new ObservableCollection<DestroyListView>();
        int de_complete;
        string dec_output_directory;

        private void AddFile2_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OFD = new OpenFileDialog();
            OFD.Multiselect = true;
            if (OFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                foreach (string i in OFD.FileNames)
                {
                    FileInfo file = new FileInfo(i);
                    list_to_decrypt.Add(new DestroyListView(i, Functions.GetFileImage24(file.Extension), file));
                    items_count2.Text = list_to_decrypt.Count + (list_to_decrypt.Count > 1 ? " Item" : " Items");
                }
        }

        private void AddFolder2_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                destroy_control.IsEnabled = false;
                BackgroundWorker add_all_files_in_dir_bw = new BackgroundWorker();
                add_all_files_in_dir_bw.WorkerSupportsCancellation = true;
                add_all_files_in_dir_bw.WorkerReportsProgress = true;
                add_all_files_in_dir_bw.DoWork += AddFilesBW_DoWork;
                add_all_files_in_dir_bw.ProgressChanged += AddFilesBW2_ProgressChanged;
                add_all_files_in_dir_bw.RunWorkerCompleted += AddFilesBW2_RunWorkerCompleted;
                add_all_files_in_dir_bw.RunWorkerAsync(path);
            }
        }

        void AddFilesBW2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileInfo i = (FileInfo)e.UserState;
            try
            { list_to_decrypt.Add(new DestroyListView(i.FullName, Functions.GetFileImage24(i.Extension), i)); }
            catch (Exception) { }
        }

        void AddFilesBW2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (list_to_decrypt.Count > 0)
                items_count2.Text = list_to_decrypt.Count + (list_to_decrypt.Count > 1 ? " Items" : " Item");
            else
                items_count2.Text = "";
            destroy_control2.IsEnabled = true;
        }

        private void RemoveSelected2_Click(object sender, RoutedEventArgs e)
        {
            for (int i = files_list2.SelectedItems.Count - 1; i >= 0; i--)
                list_to_decrypt.RemoveAt(files_list2.Items.IndexOf(files_list2.SelectedItems[i]));
            if (list_to_decrypt.Count > 0)
                items_count2.Text = list_to_decrypt.Count + (list_to_decrypt.Count == 1 ? " Item" : " Items");
            else
                items_count.Text = string.Empty;
        }

        private void ClearList2_Click(object sender, RoutedEventArgs e)
        {
            list_to_decrypt.Clear();
            items_count2.Text = "";
        }

        private void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            //message box question???????????????????????????????????????????????
            if (list_to_decrypt.Count > 0)
                if (new My_MessageBox("Confirmation", "Start Decription?", "Caution: make sure that all these files have the same password.", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                {
                    de_complete = 0;
                    destroy_control2.IsEnabled = false;
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.WorkerSupportsCancellation = true;
                    bw.WorkerReportsProgress = true;
                    bw.DoWork += DecryptBW_DoWork;
                    bw.ProgressChanged += DecryptBW_ProgressChanged;
                    bw.RunWorkerCompleted += DecryptBW_RunWorkerCompleted;
                    total_progress2.Visibility = Visibility.Visible;
                    total_progress2.Value = 0;
                    total_progress2.Maximum = list_to_encrypt.Count;
                    dec_password_byte = PreparePassword(dec_password.Password);
                    dec_output_directory = dec_output.Text;
                    bw.RunWorkerAsync();
                }
        }

        private void DecryptBW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            Thread process_thread = new Thread(() =>
            {
                while (bw.IsBusy)
                {
                    bw.ReportProgress(0);
                    Thread.Sleep(200);
                }
            });
            process_thread.Start();
            for (int index = 0; index < list_to_decrypt.Count; index++)
            {
                list_to_decrypt[index].CurrentProgress = 0;
                string write_file_in = PrepareFile(list_to_decrypt[index].File, dec_output_directory);
                if (!File.Exists(write_file_in) || !list_to_decrypt[index].File.Exists)
                    continue;
                BinaryReader BR = new BinaryReader(list_to_decrypt[index].File.OpenRead());
                BinaryWriter BW = new BinaryWriter(File.Open(write_file_in, FileMode.OpenOrCreate));
                bool done = false;
                try
                {
                    long k;
                    if (BR.ReadString() == "THIS FILE HAD ENCRYPTED WITH FILE MANAGER")
                    {
                        BR.ReadBytes(256);
                        //if (Compare(BR.ReadBytes(256), password_byte))
                        {
                            for (k = 65536; k <= list_to_decrypt[index].File.Length; k += 65536)
                            {
                                BW.Write(Decrypt(BR.ReadBytes(65536)));
                                list_to_decrypt[index].CurrentProgress += 65536;
                            }
                            int remain = (int)(list_to_decrypt[index].File.Length - (k - 65536));
                            BW.Write(Decrypt(BR.ReadBytes(remain)));
                            list_to_decrypt[index].CurrentProgress += remain;
                            list_to_decrypt[index].TextBlockVisibility = Visibility.Collapsed;
                            done = true;
                        }
                        //else
                        //    throw new Exception("Wrong Password");
                    }
                    else
                        throw new Exception("Invaled File"); 
                }
                catch (Exception ex)
                {
                    list_to_decrypt[index].CurrentProgress = 0;
                    list_to_decrypt[index].TextBlockVisibility = Visibility.Visible;
                    list_to_decrypt[index].ProgressBarVisibility = Visibility.Collapsed;
                    list_to_decrypt[index].ErrorMassege = ex.Message;
                    BR.Close();
                    BW.Close();
                    if (File.Exists(write_file_in))
                        try { File.Delete(write_file_in); }
                        catch (Exception) { } continue;
                }
                BR.Close();
                BW.Close();
                if (done)
                    de_complete++;
            }
        }

        private bool Compare(byte[] p, byte[] password_byte)
        {
            for (int i = 0; i < p.Length; i++)
                if (p[i] != password_byte[i])
                    return false;
            return true;
        }

        private byte[] Decrypt(byte[] bytes)
        {
            byte[] decrept = new byte[bytes.Length];
            Parallel.For(0, bytes.Length, (i) => { decrept[i] = (byte)(bytes[i] - dec_password_byte[i]); });
            return decrept;
        }

        private void DecryptBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            files_list2.Items.Refresh();
            total_progress.Value = de_complete;
        }

        private void DecryptBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            destroy_control2.IsEnabled = true;
            total_progress2.Visibility = Visibility.Hidden;
            files_list2.Items.Refresh();
            new My_MessageBox("FILE MANAGER", "Operation done.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
        }

        #endregion
    }
}
