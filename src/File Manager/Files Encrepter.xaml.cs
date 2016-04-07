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
            PrepareControls();
        }

        public Files_Encrepter(string[] files, bool encrypt)//true for encrypt and false for decrypt
        {
            InitializeComponent();
            PrepareControls();
            if (encrypt)
                AddFiles(files, encrypt_data, encrypt_list);
            else
            {
                AddFiles(files, decrypt_data, decrypt_list);
                CurrentPage = EncryptPages.FilesDecrypt;
            }
        }

        void PrepareControls()
        {
            encrypt_list.ContextMenu = ListMenu(encrypt_list, encrypt_data);
            decrypt_list.ContextMenu = ListMenu(decrypt_list, decrypt_data);
            UpdateButtons();
        }

        ContextMenu ListMenu(ListView list, List<DestroyListView> data)
        {
            ContextMenu menu = new ContextMenu();
            MenuItem open_location = new MenuItem() { Header = "Open location" };
            MenuItem preview = new MenuItem() { Header = "Preview", InputGestureText = "Space" };
            MenuItem copy_path = new MenuItem() { Header = "Copy path" };
            MenuItem remove = new MenuItem() { Header = "Remove from list", InputGestureText = "Del" };
            open_location.Click += (object sender, RoutedEventArgs e) => { if (list.SelectedItems.Count == 1) SafetyTools.OpenLocation(((DestroyListView)list.SelectedItem).FullPath); };
            preview.Click += (object sender, RoutedEventArgs e) => { if (list.SelectedItems.Count == 1) SafetyTools.PreviewItem(((DestroyListView)list.SelectedItem).FullPath); };
            copy_path.Click += (object sender, RoutedEventArgs e) => { if (list.SelectedItems.Count > 0) SafetyTools.CopyPaths(list.SelectedItems.Cast<DestroyListView>().Select(x => x.FullPath)); };
            remove.Click += (object sender, RoutedEventArgs e) => { RemoveSelected(list, data); };
            menu.Items.Add(open_location);
            menu.Items.Add(preview);
            menu.Items.Add(copy_path);
            menu.Items.Add(new Separator());
            menu.Items.Add(remove);
            menu.Opened += (object sender, RoutedEventArgs e) =>
            {
                int selected = list.SelectedItems.Count;
                string one = selected == 0 ? "Select a file first." : selected > 1 ? "Select only one file." : null;
                SafetyTools.SetState(open_location, one == null, one);
                SafetyTools.SetState(preview, one == null, one);
                SafetyTools.SetState(copy_path, selected > 0, "Select a file first.");
                SafetyTools.SetState(remove, selected > 0 && !adding, adding ? "Please wait, files are being added." : "Select a file first.");
            };
            return menu;
        }

        string Password(PasswordBox box, TextBox text)
        {
            return box.Visibility == Visibility.Hidden ? text.Text : box.Password;
        }

        void UpdateButtons()
        {
            if (pack_extract_button == null || decrypt_button == null)
                return;
            string wait = adding ? "Please wait, files are being added." : null;
            SafetyTools.SetState(encrypt_add_button, !adding, wait);
            SafetyTools.SetState(encrypt_add_folder_button, !adding, wait);
            SafetyTools.SetState(decrypt_add_button, !adding, wait);
            SafetyTools.SetState(decrypt_add_folder_button, !adding, wait);
            SafetyTools.SetState(encrypt_clear_button, encrypt_data.Count > 0 && !adding, wait ?? "The list is empty.");
            SafetyTools.SetState(decrypt_clear_button, decrypt_data.Count > 0 && !adding, wait ?? "The list is empty.");
            SafetyTools.SetState(encrypt_remove_button, encrypt_list.SelectedItems.Count > 0 && !adding, wait ?? (encrypt_data.Count == 0 ? "The list is empty." : "Select files in the list first."));
            SafetyTools.SetState(decrypt_remove_button, decrypt_list.SelectedItems.Count > 0 && !adding, wait ?? (decrypt_data.Count == 0 ? "The list is empty." : "Select files in the list first."));
            string enc_reason = wait ?? (encrypt_data.Count == 0 ? "Add files to the list first." : Password(enc_password, enc_password_text).Length < 8 ? "The password must have at least 8 characters." : null);
            SafetyTools.SetState(encrypt_button, enc_reason == null, enc_reason);
            string dec_reason = wait ?? (decrypt_data.Count == 0 ? "Add encrypted files to the list first." : Password(dec_password, dec_password_text).Length == 0 ? "Enter the password first." : null);
            SafetyTools.SetState(decrypt_button, dec_reason == null, dec_reason);
            UpdatePackButtons();
        }

        void UpdatePackButtons()
        {
            string show_reason = pack_path.Text.Length == 0 ? "Open an encrypted package first." : pack_password.Password.Length == 0 ? "Enter the password first." : null;
            SafetyTools.SetState(pack_show_button, show_reason == null, show_reason);
            bool shown = pack_list.ItemsSource != null && pack_list.Items.Count > 0;
            SafetyTools.SetState(pack_extract_button, shown, "Click 'Show content' first.");
            pack_extract_button.Content = shown && pack_list.SelectedItems.Count == 0 ? "Extract all" : "Extract selected";
        }

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            UpdateButtons();
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtons();
        }

        private void List_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ListView list = (ListView)sender;
            if (e.Key == Key.Delete && list.SelectedItems.Count > 0)
            {
                RemoveSelected(list, list == encrypt_list ? encrypt_data : decrypt_data);
                e.Handled = true;
            }
            else if (e.Key == Key.Space && list.SelectedItems.Count == 1)
            {
                SafetyTools.PreviewItem(((DestroyListView)list.SelectedItem).FullPath);
                e.Handled = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.Width = label1.ActualWidth;
            starting_window = false;
            encrypt_list.DataContext = encrypt_data;
            decrypt_list.DataContext = decrypt_data;
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!Directory.Exists(enc_output.Text))
                enc_output.Text = documents;
            if (!Directory.Exists(dec_output.Text))
                dec_output.Text = documents;
            if (!Directory.Exists(pack_output.Text))
                pack_output.Text = documents;
            UpdateButtons();
            if (CurrentPage == EncryptPages.FilesDecrypt)
                TabChange_MouseButtonUp(label2, null);
            else if (CurrentPage == EncryptPages.ViewPackContent)
                TabChange_MouseButtonUp(label3, null);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CloseWindow = true;
        }

        bool adding = false;

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            bool encrypt = sender == encrypt_add_button;
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = encrypt ? "Select files to encrypt" : "Select encrypted files to decrypt";
            sfd.AddExtension = false;
            sfd.MultiSelect = true;
            if (!encrypt)
            {
                sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Encrypted files (*.enf, *.enp, *.fme)", extentions = SelectFileFilter.SetExtentions(".enf|.enp|.fme") });
                sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "All files", extentions = SelectFileFilter.SetExtentions("") });
            }
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                AddFiles(sfd.SelectedItems.ToArray(), encrypt ? encrypt_data : decrypt_data, encrypt ? encrypt_list : decrypt_list);
        }

        private void AddFiles(string[] files, List<DestroyListView> data_list, ListView list)
        {
            HashSet<string> listed = new HashSet<string>(data_list.Select(x => x.FullPath), StringComparer.OrdinalIgnoreCase);
            List<string> folders = new List<string>(), links = new List<string>();
            foreach (string i in files)
            {
                if (Directory.Exists(i))
                {
                    folders.Add(i);
                    continue;
                }
                if (!File.Exists(i))
                    continue;
                FileInfo file = new FileInfo(i);
                if (Functions.IsLink(file))
                    links.Add(i);
                else if (listed.Add(file.FullName))
                    data_list.Add(new DestroyListView(file));
            }
            list.Items.Refresh();
            UpdateButtons();
            if (links.Count > 0)
                Messages.ShowMessage(string.Format("{0} link{1} not added.", links.Count, links.Count > 1 ? "s were" : " was"), ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "Links (symbolic links) are never followed:\n" + string.Join("\n", links.Take(30).ToArray()));
            if (folders.Count > 0)
                AddDirectory(folders, data_list, list);
        }

        private void AddDirectory_Click(object sender, RoutedEventArgs e)
        {
            bool encrypt = sender == encrypt_add_folder_button;
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = encrypt ? "Add all files in this directory." : "Add all encrypted files in this directory.";
            sfd.AddExtension = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                AddDirectory(new List<string>() { sfd.SelectedItem }, encrypt ? encrypt_data : decrypt_data, encrypt ? encrypt_list : decrypt_list);
        }

        private void AddDirectory(List<string> paths, List<DestroyListView> data_list, ListView list)
        {
            if (adding)
                return;
            bool only_encrypted = data_list == decrypt_data;
            adding = true;
            Cursor = Cursors.AppStarting;
            UpdateButtons();
            List<string> found = new List<string>(), skipped = new List<string>();
            int not_encrypted = 0;
            BackgroundWorker add_files_in_directory = new BackgroundWorker();
            add_files_in_directory.DoWork += (object bw_sender, DoWorkEventArgs e_bw) =>
            {
                foreach (string path in paths)
                {
                    DirectoryInfo directory = new DirectoryInfo(path);
                    if (!directory.Exists)
                        continue;
                    if (Functions.IsLink(directory))
                        skipped.Add(path + "\n    (link, not followed)");
                    else
                        GetAllFiles(directory, found, skipped);
                }
                if (only_encrypted)
                    not_encrypted = found.RemoveAll(x => !IsEncryptedFile(x));
            };
            add_files_in_directory.RunWorkerCompleted += (object bw_sender, RunWorkerCompletedEventArgs bw_e) =>
            {
                adding = false;
                Cursor = null;
                if (CloseWindow)
                    return;
                AddFiles(found.ToArray(), data_list, list);
                if (skipped.Count > 0)
                    Messages.ShowMessage(string.Format("{0} item{1} not added.", skipped.Count, skipped.Count > 1 ? "s were" : " was"), ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", skipped.Take(30).ToArray()) + (skipped.Count > 30 ? string.Format("\n... and {0} more.", skipped.Count - 30) : ""));
                else if (only_encrypted && found.Count == 0)
                    Messages.ShowMessage("No file encrypted by File Manager was found in '" + string.Join("', '", paths.ToArray()) + "'.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, not_encrypted > 0, not_encrypted > 0 ? not_encrypted + " other file(s) were not added." : "");
            };
            add_files_in_directory.RunWorkerAsync();
        }

        static bool IsEncryptedFile(string path)
        {
            try
            {
                using (FileStream input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int version = Operation.ReadFmeHeader(input, null).version;
                    return version == 1 || version == 3 || version == 4;
                }
            }
            catch (Exception) { return false; }
        }

        private void GetAllFiles(DirectoryInfo dir, List<string> found, List<string> skipped)
        {
            DirectoryInfo[] sub_dirs;
            FileInfo[] files;
            try
            {
                sub_dirs = dir.GetDirectories();
                files = dir.GetFiles();
            }
            catch (Exception ex)
            {
                skipped.Add(dir.FullName + "\n    (could not be read: " + ex.Message + ")");
                return;
            }
            foreach (var i in sub_dirs)
            {
                if (CloseWindow)
                    return;
                if (Functions.IsLink(i))
                    skipped.Add(i.FullName + "\n    (link, not followed)");
                else
                    GetAllFiles(i, found, skipped);
            }
            foreach (var i in files)
            {
                if (CloseWindow)
                    return;
                if (Functions.IsLink(i))
                    skipped.Add(i.FullName + "\n    (link, not added)");
                else
                    found.Add(i.FullName);
            }
        }

        void RemoveSelected(ListView list, List<DestroyListView> data)
        {
            if (adding || list.SelectedItems.Count == 0)
                return;
            HashSet<DestroyListView> selected = new HashSet<DestroyListView>(list.SelectedItems.Cast<DestroyListView>());
            data.RemoveAll(x => selected.Contains(x));
            list.Items.Refresh();
            UpdateButtons();
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (sender == encrypt_remove_button)
                RemoveSelected(encrypt_list, encrypt_data);
            else
                RemoveSelected(decrypt_list, decrypt_data);
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            if (adding)
                return;
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
            UpdateButtons();
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
                else if (sender == pack_browser_button)
                    pack_output.Text = sfd.SelectedItem;
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
            string plan = overwrite_file.IsChecked == true ? "Every file is encrypted to '<name>.enf' next to it, the original is overwritten with zeros and deleted only after its encrypted copy is verified." : enc_delete_files.IsChecked == true ? "After the encryption, File Destroyer opens with the original files whose encrypted copy was verified, you choose the method there." : "The original files are kept.";
            if (encrypt && Messages.ShowMessage("We have got all information we needed, start encryption?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, plan, "all files in list will encrypt with same password, make sure to keep it in safe place.") == MyMessageBoxResult.Yes)
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
                delete = enc_delete_files.IsChecked.Value && overwrite_file.IsChecked == false;
                enc_file_name = this.enc_file_name.IsChecked.Value;
                enc_pack_files = this.enc_pack_files.IsChecked.Value;
                List<FileInfo> files_list = new List<FileInfo>();
                foreach (var i in encrypt_data)
                    files_list.Add(new FileInfo(i.FullPath));
                if (overwrite_file.IsChecked == true && !Functions.ConfirmRiskyItems(files_list.Select(x => x.FullName), "encrypt and overwrite"))
                    return;
                Operation operation = new Operation();
                operation.StartSafetyEncrypt(files_list, password, overwrite_file.IsChecked.Value, save_in, enc_file_name, enc_pack_files, true);
                HashSet<string> verified = new HashSet<string>(operation.verified_files.Select(x => x.FullName), StringComparer.OrdinalIgnoreCase);
                encrypt_data.RemoveAll(x => verified.Contains(x.FullPath));
                encrypt_list.Items.Refresh();
                UpdateButtons();
                ShowResult(operation, "Encryption");
                if (delete && verified.Count > 0)
                {
                    List<string> originals = operation.verified_files.Select(x => x.FullName).Where(x => File.Exists(x)).ToList();
                    if (originals.Count > 0 && (!operation.Stopped || Messages.ShowMessage(string.Format("The encryption was stopped. Open File Destroyer for the {0} original file{1} whose encrypted copy was verified?", originals.Count, originals.Count > 1 ? "s" : ""), ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.Yes))
                        Functions.ShowTool(new File_Destroyer(originals, string.Format("{0} original file{1}, encrypted and verified", originals.Count, originals.Count > 1 ? "s" : "")));
                }
            }
        }

        private void EncryptList_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && !adding && (data = e.Data.GetData(DataFormats.FileDrop) as string[]) != null && data.Length > 0)
                AddFiles(data, encrypt_data, encrypt_list);
        }

        private void EncOutputText_Drop(object sender, DragEventArgs e)
        {
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && overwrite_file.IsChecked == false)
                if ((data = (string[])e.Data.GetData(DataFormats.FileDrop)).Length == 1 && Directory.Exists(data[0]))
                    enc_output.Text = data[0];
        }

        private void EncryptOverwriteFile_Checked(object sender, RoutedEventArgs e)
        {
            enc_output_grid.IsEnabled = false;
            enc_delete_files.IsChecked = enc_delete_files.IsEnabled = false;
            enc_pack_files.IsChecked = enc_pack_files.IsEnabled = false;
            enc_output_grid.ToolTip = "Not used: the encrypted files are saved next to the originals.";
            enc_delete_files.ToolTip = "Not needed: 'Overwrite original files' already destroys every original after its encrypted copy is verified.";
            enc_pack_files.ToolTip = "Not available with 'Overwrite original files': every file gets its own encrypted file next to it.";
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (enc_output_grid != null)
            {
                enc_output_grid.IsEnabled = true;
                enc_delete_files.IsEnabled = true;
                enc_pack_files.IsEnabled = true;
                enc_output_grid.ToolTip = null;
                enc_delete_files.ToolTip = "When the encryption ends, File Destroyer opens with only the original files whose encrypted copy was written and verified.\nYou choose the destroy method there and confirm before anything is destroyed.";
                enc_pack_files.ToolTip = null;
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
                    files_list.Add(new FileInfo(i.FullPath));
                Operation operation = new Operation();
                operation.StartDecrept(files_list, password, save_in, dec_package_directory.IsChecked == true, null, true);
                if (operation.result_errors.Count == 0 && !operation.Stopped)
                    ClearList_Click(decrypt_clear_button, null);
                ShowResult(operation, "De-encryption");
            }
        }

        private void DecryptList_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && !adding && (data = e.Data.GetData(DataFormats.FileDrop) as string[]) != null && data.Length > 0)
                AddFiles(data, decrypt_data, decrypt_list);
        }

        private void DecOutputText_Drop(object sender, DragEventArgs e)
        {
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if ((data = (string[])e.Data.GetData(DataFormats.FileDrop)).Length == 1 && Directory.Exists(data[0]))
                    ((TextBox)sender).Text = data[0];
                e.Handled = true;
            }
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
            label3.Background = Brushes.Black; label3.Foreground = Brushes.White;
            ((Label)sender).Background = Brushes.White;
            ((Label)sender).Foreground = Brushes.Black;
            CurrentPage = (EncryptPages)tab_panal.Children.IndexOf((Label)sender) + 1;
            Title = CurrentPage == EncryptPages.FilesEncrypt ? "Encrypt files" : CurrentPage == EncryptPages.FilesDecrypt ? "Decrypt files" : "View package content";
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
            AboutTool window = new AboutTool();
            window.tool_vertion.Text = "Version: 3";
            window.tool_date.Text = "";
            window.tool_name.Text = "Files encrypter";
            window.tool_image.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/encrypt.png"));
            window.ShowDialog();
        }

        void ShowResult(Operation operation, string action)
        {
            StringBuilder details = new StringBuilder();
            foreach (var i in operation.result_errors)
                details.AppendLine(i);
            if (operation.result_notes.Count > 0)
            {
                if (details.Length > 0)
                    details.AppendLine();
                details.AppendLine("Output:");
                foreach (var i in operation.result_notes)
                    details.AppendLine(i);
            }
            if (operation.result_errors.Count > 0)
                Messages.ShowMessage(string.Format("{0} finished with {1} problem{2}.", action, operation.result_errors.Count, operation.result_errors.Count > 1 ? "s" : ""), ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, details.ToString());
            else if (operation.Stopped)
                Messages.ShowMessage(action + " was stopped, unfinished output files were removed.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, details.Length > 0, details.ToString());
            else
                Messages.ShowMessage(action + " done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, details.Length > 0, details.ToString());
        }

        #region VIEW PACK CONTENT

        private void PackOpen_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = "Select encrypted package or file";
            sfd.AddExtension = false;
            sfd.MultiSelect = false;
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Encrypted package or file (*.enp, *.enf)", extentions = SelectFileFilter.SetExtentions(".enp|.enf") });
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "All files", extentions = SelectFileFilter.SetExtentions("") });
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                pack_path.Text = sfd.SelectedItem;
                pack_list.ItemsSource = null;
            }
        }

        private void PackPath_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void PackPath_Drop(object sender, DragEventArgs e)
        {
            string[] data;
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && (data = (string[])e.Data.GetData(DataFormats.FileDrop)).Length > 0 && File.Exists(data[0]))
            {
                pack_path.Text = data[0];
                pack_list.ItemsSource = null;
            }
            e.Handled = true;
        }

        internal void OpenPackage(string path)
        {
            pack_path.Text = path;
            CurrentPage = EncryptPages.ViewPackContent;
        }

        private void Pack_Changed(object sender, RoutedEventArgs e)
        {
            if (pack_list == null || pack_extract_button == null)
                return;
            if (sender == pack_path)
                pack_list.ItemsSource = null;
            UpdatePackButtons();
        }

        private void PackPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && pack_show_button.IsEnabled)
            {
                PackShow_Click(pack_show_button, null);
                e.Handled = true;
            }
        }

        private void PackList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePackButtons();
        }

        private void PackShow_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(pack_path.Text))
                Messages.ShowMessage("Some field is missing.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please select an encrypted package.");
            else if (pack_password.Password.Length == 0)
                Messages.ShowMessage("Some field is missing.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please enter the password.");
            else
            {
                string error;
                Cursor = Cursors.Wait;
                List<FmeItem> items = Operation.ReadPackageContent(pack_path.Text, pack_password.Password, out error);
                Cursor = null;
                if (items == null)
                    Messages.ShowMessage("We can not show the package content.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, error);
                else
                {
                    List<PackItemView> views = new List<PackItemView>();
                    foreach (var i in items)
                        views.Add(new PackItemView(i));
                    pack_list.ItemsSource = views;
                }
                UpdatePackButtons();
            }
        }

        private void PackExtract_Click(object sender, RoutedEventArgs e)
        {
            if (pack_list.ItemsSource == null || pack_list.Items.Count == 0)
                Messages.ShowMessage("Click 'Show content' first.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else if (pack_output.Text == string.Empty || pack_output.Text.Any(x => Path.GetInvalidPathChars().Any(y => y.Equals(x))))
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please select a valid output directory.");
            else
            {
                List<int> ids = new List<int>();
                foreach (PackItemView i in pack_list.SelectedItems)
                    ids.Add(i.id);
                if (ids.Count == 0)
                {
                    if (Messages.ShowMessage("No file is selected, extract all files?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") != MyMessageBoxResult.Yes)
                        return;
                    foreach (PackItemView i in pack_list.Items)
                        ids.Add(i.id);
                }
                try { Directory.CreateDirectory(pack_output.Text); }
                catch (Exception ex)
                {
                    Messages.ShowMessage("We can not create directory '" + pack_output.Text + "'", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                    return;
                }
                Operation operation = new Operation();
                operation.StartDecrept(new List<FileInfo>() { new FileInfo(pack_path.Text) }, pack_password.Password, pack_output.Text, false, ids, true);
                ShowResult(operation, "Extraction");
            }
        }

        #endregion

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
        FilesDecrypt = 2,
        ViewPackContent = 3
    }

    class PackItemView
    {
        internal int id;

        public PackItemView(FmeItem item)
        {
            id = item.id;
            Name = item.name;
            Size = Functions.FileSize(item.plain_length, 2);
        }

        public string Name { get; set; }

        public string Size { get; set; }
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