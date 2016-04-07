using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Compress_Files.xaml
    /// </summary>
    public partial class Compress_Files : Window
    {
        CompressItem root;
        string confirmed_path = null;

        OutArchiveFormat extention_item = OutArchiveFormat.SevenZip;
        string[] sold_block_size = { "off", "1m", "2m", "4m", "8m", "16m", "32m", "64m", "128m", "256m", "512m", "1g", "2g", "4g", "8g", "16g", "32g", "64g", "on" };
        static string[] archive_extentions = { ".tar.gz", ".tar.bz2", ".tar.xz", ".7z", ".zip", ".tar", ".gz", ".bz2", ".xz" };
        public Compress_Files()
        {
            InitializeComponent();
            format.SelectedIndex = 0;
            priority.SelectedIndex = 1;
            volume_size.SelectedIndex = 0;
        }

        internal void ShowWindow(CompressItem root, string suggested_name)
        {
            this.root = root;
            if (!string.IsNullOrEmpty(suggested_name))
                file_name.Text = suggested_name + "." + ((ComboBoxItem)format.SelectedItem).Tag;
            ShowDialog();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SaveFile;
            sfd.CreateFolderEnable = true;
            sfd.DialogTitle = "Select output file";
            sfd.AddExtension = true;
            sfd.MultiSelect = false;
            string extention = "." + ((ComboBoxItem)format.SelectedItem).Tag;
            string description = FileInfoCombonents.FilesInformation.ContainsKey(extention) ? FileInfoCombonents.FilesInformation[extention].Description : extention.ToUpper().Substring(1) + " File";
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = description + string.Format(" (*{0})", extention), extentions = SelectFileFilter.SetExtentions(extention) });
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            try
            {
                string folder = Path.GetDirectoryName(file_name.Text.Trim());
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                    sfd.InitialDirectory = folder;
            }
            catch (Exception) { }
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                string path = sfd.SelectedItem;
                if (!path.ToLower().EndsWith(extention))
                    path += extention;
                file_name.Text = path;
                confirmed_path = path;
            }
        }

        private void Format_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tag = ((ComboBoxItem)format.SelectedItem).Tag.ToString();
            level.Items.Clear();
            level.Items.Insert(0, "Just store");
            for (int i = 1; i < 6; i++)
                level.Items.Add(((CompressionLevel)i).ToString());
            level.SelectedIndex = 3;
            level.IsEnabled = true;
            solid_block.IsEnabled = false;
            password_box.IsEnabled = false;
            password_checkbox.IsChecked = false;
            encrypt_file_name.IsChecked = false;
            switch (tag)
            {
                case "7z":
                    extention_item = OutArchiveFormat.SevenZip;
                    password_box.IsEnabled = true;
                    solid_block.IsEnabled = true;
                    solid_block.SelectedIndex = 3;
                    break;
                case "zip":
                    extention_item = OutArchiveFormat.Zip;
                    password_box.IsEnabled = true;
                    break;
                case "tar":
                    extention_item = OutArchiveFormat.Tar;
                    level.SelectedIndex = 0;
                    level.IsEnabled = false;
                    break;
                case "gz":
                    extention_item = OutArchiveFormat.GZip;
                    break;
                case "bz2":
                    extention_item = OutArchiveFormat.BZip2;
                    break;
                case "xz":
                    extention_item = OutArchiveFormat.XZ;
                    break;
            }
            level.ToolTip = level.IsEnabled ? null : "tar only keeps the files together, it does not compress them.";
            solid_block.ToolTip = solid_block.IsEnabled ? null : "Solid blocks are an option of 7z archives.";
            password_box.ToolTip = password_box.IsEnabled ? null : "Only 7z and zip archives can have a password.";
            UpdatePasswordState();
            if (file_name.Text.Trim().Length > 0)
            {
                string path = file_name.Text.Trim();
                string old_extention = archive_extentions.FirstOrDefault(x => path.EndsWith(x, StringComparison.OrdinalIgnoreCase));
                if (old_extention != null)
                    path = path.Remove(path.Length - old_extention.Length);
                file_name.Text = path + "." + tag;
            }
        }

        void UpdatePasswordState()
        {
            bool on = password_box.IsEnabled && password_checkbox.IsChecked == true;
            password_enc.IsEnabled = password_textbox.IsEnabled = show_password.IsEnabled = on;
            password_enc.ToolTip = on ? null : "Tick 'Password' first.";
            encrypt_file_name.IsEnabled = on && extention_item == OutArchiveFormat.SevenZip;
            if (!encrypt_file_name.IsEnabled)
                encrypt_file_name.IsChecked = false;
            encrypt_file_name.ToolTip = encrypt_file_name.IsEnabled ? "Nobody can see the list of files without the password." : extention_item != OutArchiveFormat.SevenZip ? "Only 7z archives can encrypt file names." : "Tick 'Password' and enter a password first.";
        }

        private void PasswordCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePasswordState();
            if (password_checkbox.IsChecked == true)
            {
                if (show_password.IsChecked == true)
                    password_textbox.Focus();
                else
                    password_enc.Focus();
            }
        }

        static long ParseVolumeSize(string text)
        {
            text = text.Trim().ToUpper();
            if (text == "" || text.StartsWith("DON"))
                return 0;
            int k = 0;
            while (k < text.Length && (char.IsDigit(text[k]) || text[k] == '.' || text[k] == ','))
                k++;
            double number;
            if (k == 0 || !double.TryParse(text.Substring(0, k).Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number) || number <= 0)
                return -1;
            string unit = text.Substring(k).Trim();
            double factor;
            if (unit == "" || unit.StartsWith("M"))
                factor = 1048576;
            else if (unit.StartsWith("G"))
                factor = 1073741824;
            else if (unit.StartsWith("K"))
                factor = 1024;
            else if (unit.StartsWith("B"))
                factor = 1;
            else
                return -1;
            double size = number * factor;
            if (size < 65536 || size > 1099511627776.0)
                return -1;
            return (long)size;
        }

        private void Compress_Click(object sender, RoutedEventArgs e)
        {
            string text = file_name.Text.Trim();
            if (text.Equals(string.Empty))
            {
                Messages.ShowMessage("Some field is missing.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please enter file path.");
                return;
            }
            string output;
            try
            {
                if (text.Any(x => Path.GetInvalidPathChars().Any(y => y == x)) || !Path.IsPathRooted(text))
                    throw new ArgumentException("Please enter the full path of the archive, for example D:\\Backup\\photos.7z");
                output = Path.GetFullPath(text);
                if (Path.GetFileName(output).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || Path.GetFileName(output) == "")
                    throw new ArgumentException("The file name contains characters that can not be used.");
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, ex.Message);
                return;
            }
            string folder = Path.GetDirectoryName(output);
            if (folder == null || !Directory.Exists(folder))
            {
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "The directory of the archive does not exist:\n" + folder);
                return;
            }
            if (Directory.Exists(output))
            {
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "A directory with this name already exists:\n" + output);
                return;
            }
            if (root.AllFiles.Any(x => x.Item != null && (string.Equals(x.Item.FullName, output, StringComparison.OrdinalIgnoreCase) || (x.Item is DirectoryInfo && output.StartsWith(x.Item.FullName.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)))))
            {
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "The archive can not be saved inside the files or directories that are being compressed.");
                return;
            }
            long volume = ParseVolumeSize(volume_size.Text);
            if (volume < 0)
            {
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "The volume size is not valid, examples: 100 MB, 700M, 4 GB (at least 64 KB).");
                return;
            }
            if (volume > 0 && File.Exists(output + ".001"))
            {
                if (Messages.ShowMessage("Volumes of this archive already exist, overwrite them?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, output + ".001 ...\nThe old volumes are replaced only after the new archive is created successfully.") != MyMessageBoxResult.Yes)
                    return;
            }
            else if (volume == 0 && File.Exists(output) && !string.Equals(output, confirmed_path, StringComparison.OrdinalIgnoreCase))
            {
                if (Messages.ShowMessage("The file already exists, overwrite it?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, output + "\nThe old file is replaced only after the new archive is created successfully.") != MyMessageBoxResult.Yes)
                    return;
            }
            string password = "";
            bool has_password = password_box.IsEnabled && password_checkbox.IsChecked == true;
            if (has_password)
            {
                password = show_password.IsChecked.Value ? password_textbox.Text : password_enc.Password;
                if (password.Equals(string.Empty))
                {
                    Messages.ShowMessage("Some field is missing.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please enter a password.");
                    return;
                }
            }
            CompressOptions options = new CompressOptions();
            options.Format = extention_item;
            options.Output = output;
            options.Level = level.SelectedIndex < 0 ? 3 : level.SelectedIndex;
            options.SolidBlock = solid_block.IsEnabled && solid_block.SelectedIndex > -1 ? sold_block_size[solid_block.SelectedIndex] : null;
            options.Password = password;
            options.EncryptNames = has_password && encrypt_file_name.IsChecked == true;
            options.VolumeSize = volume;
            options.Priority = priority.SelectedIndex - 1;
            Close();
            new ArchiveOperation().StartCompressing(options, root);
        }

        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            password_textbox.Visibility = Visibility.Visible;
            password_enc.Visibility = Visibility.Hidden;
            password_textbox.Text = password_enc.Password;
            show_password.ToolTip = "Hide password";
        }

        private void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            password_textbox.Visibility = Visibility.Hidden;
            password_enc.Visibility = Visibility.Visible;
            password_enc.Password = password_textbox.Text;
            show_password.ToolTip = "Show password";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SFX_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void SFX_Unchecked(object sender, RoutedEventArgs e)
        {
        }
    }

    class CompressOptions
    {
        internal OutArchiveFormat Format = OutArchiveFormat.SevenZip;
        internal string Output;
        internal int Level = 3;
        internal string SolidBlock;
        internal string Password = "";
        internal bool EncryptNames;
        internal long VolumeSize;
        internal int Priority;
    }

    enum ArchiveType
    {
        SevenZip = 1,
        Arc = 2,

    }
}



//#region Indeterminate ProcessBar

//private void Start_Indeterminate()
//{
//    go_again = true;
//    BackgroundWorker main_bw = new BackgroundWorker();
//    main_bw.DoWork += MainBW_DoWork;
//    main_bw.ProgressChanged += MainBW_ProgressChanged;
//    main_bw.WorkerReportsProgress = true;
//    main_bw.RunWorkerCompleted += MainBW_RunWorkerCompleted;
//    main_bw.RunWorkerAsync(eg);
//}

//void Stop_Indeterminate()
//{
//    go_again = false;
//}

//void MainBW_DoWork(object sender, DoWorkEventArgs e)
//{
//    Grid n = (Grid)e.Argument;
//    BackgroundWorker bw = (BackgroundWorker)sender;
//    for (int i = 0; i < 5; i++)
//    {
//        bw.ReportProgress(i);
//        Thread.Sleep(200);
//    }
//    Thread.Sleep(3500);
//}

//void MainBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
//{
//    BackgroundWorker stage1_bw = new BackgroundWorker();
//    stage1_bw.ProgressChanged += AllStagesBW_ProgressChanged;
//    stage1_bw.DoWork += Stage1BW_DoWork;
//    stage1_bw.WorkerReportsProgress = true;
//    stage1_bw.RunWorkerCompleted += Stage1BW_RunWorkerCompleted;
//    stage1_bw.RunWorkerAsync(eg.Children[e.ProgressPercentage]);
//}

//void MainBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
//{
//    if (go_again)
//        Start_Indeterminate();
//}


//void Stage1BW_DoWork(object sender, DoWorkEventArgs e)
//{
//    BackgroundWorker bw = (BackgroundWorker)sender;
//    for (int i = 1; i < 66; i++)
//    {
//        bw.ReportProgress(2 * i, e.Argument);
//        Thread.Sleep(5);
//    }
//    e.Result = e.Argument;
//}

//void Stage1BW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
//{
//    BackgroundWorker stage2_bw = new BackgroundWorker();
//    stage2_bw.ProgressChanged += AllStagesBW_ProgressChanged;
//    stage2_bw.DoWork += Stage2BW_DoWork;
//    stage2_bw.WorkerReportsProgress = true;
//    stage2_bw.RunWorkerCompleted += Stage2BW_RunWorkerCompleted;
//    stage2_bw.RunWorkerAsync(e.Result);
//}


//private void Stage2BW_DoWork(object sender, DoWorkEventArgs e)
//{
//    BackgroundWorker bw = (BackgroundWorker)sender;
//    for (int i = 1; i < 66; i++)
//    {
//        bw.ReportProgress(132 + 2 * i, e.Argument);
//        Thread.Sleep(25);
//    }
//    e.Result = e.Argument;
//}

//private void Stage2BW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
//{
//    BackgroundWorker stage3_bw = new BackgroundWorker();
//    stage3_bw.ProgressChanged += AllStagesBW_ProgressChanged;
//    stage3_bw.DoWork += Stage3BW_DoWork;
//    stage3_bw.WorkerReportsProgress = true;
//    stage3_bw.RunWorkerAsync(e.Result);
//}


//private void Stage3BW_DoWork(object sender, DoWorkEventArgs e)
//{
//    BackgroundWorker bw = (BackgroundWorker)sender;
//    for (int i = 1; i < 66; i++)
//    {
//        bw.ReportProgress(264 + 2 * i, e.Argument);
//        Thread.Sleep(5);
//    }
//}


//void AllStagesBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
//{
//    ((Rectangle)e.UserState).Margin = new Thickness(e.ProgressPercentage, 0, 0, 0);
//}

//#endregion