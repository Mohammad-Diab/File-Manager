using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        ArchiveType archive_type;
        CompressItem root;

        Process P;
        string compress_tool = "", type_extention = "";
        OutArchiveFormat extention_item = OutArchiveFormat.SevenZip;
        List<byte> compress_level = new List<byte>();
        string[] sold_block_size = { "off", "1m", "2m", "4m", "8m", "16m", "32m", "64m", "128m", "256m", "512m", "1g", "2g", "4g", "8g", "16g", "32g", "64g", "on" };
        bool go_again;
        int done = -1, exit_code = -999000;
        string error_message;
        public Compress_Files()
        {
            InitializeComponent();
            format.SelectedIndex = 0;
            priority.SelectedIndex = 1;
        }

        internal void ShowWindow(CompressItem root)
        {
            this.root = root;
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
            string extention = ".7z";
            if (sfx.IsChecked.Value)
                extention = ".exe";
            else
                extention = "." + ((ComboBoxItem)format.SelectedItem).Tag;
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = FileInfoCombonents.FilesInformation[extention].Description + string.Format(" (*{0})", extention), extentions = SelectFileFilter.SetExtentions(extention) });
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            //****if (file_name.Text != "")
            //    sfd.SelectedItem = file_name.Text;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                file_name.Text = sfd.SelectedItem;
        }

        private void Format_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!sfx.IsChecked.Value)
                SFX_Unchecked(null, null);
            switch (((ComboBoxItem)format.SelectedItem).Tag.ToString())
            {
                case "7z":
                    level.Items.Clear();
                    level.Items.Insert(0, "Just store");
                    for (int i = 1; i < 6; i++)
                        level.Items.Add(((CompressionLevel)i).ToString());
                    archive_type = ArchiveType.SevenZip;
                    password_box.IsEnabled = true;
                    password_checkbox.IsChecked = false;
                    sfx.IsEnabled = true;
                    solid_block.IsEnabled = true;
                    solid_block.SelectedIndex = 3;
                    level.SelectedIndex = 3;
                    level.IsEnabled = true;
                    encrypt_file_name.IsEnabled = true;
                    break;
                //case "ace":
                //    compress_tool = @"Tools\compress\ace\ace.exe";
                //    break;
                //case "arc":
                //    level.Items.Clear();
                //    level.Items.Insert(0, "Just store");
                //    for (int i = 0; i < 9; i++)
                //        level.Items.Add(i);
                //    sfx.IsEnabled = true;
                //    level.SelectedIndex = 5;
                //    level.IsEnabled = true;
                //    encrypt_file_name.IsChecked = encrypt_file_name.IsEnabled = false;
                //    archive_type = ArchiveType.Arc;
                //    break;
                //case 3:
                //    compress_tool = @"Tools\compress\ace\b1.exe";
                //    break;
                //case 4:
                //    if (compress_level.Count == 5)
                //    {
                //        compress_level.RemoveAt(0);
                //        level.Items.RemoveAt(0);
                //    }
                //    if (root.Children.Count == 1 && !root.Children[0].IsDirectory)
                //    { type_extention = "bzip2"; extention_item = OutArchiveFormat.BZip2; }
                //    else
                //    { type_extention = "tar"; extention_item = OutArchiveFormat.Tar; }
                //    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                //    solid_block.IsEnabled = false;
                //    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                //    sfx.IsChecked = sfx.IsEnabled = false;
                //    level.SelectedIndex = 1;
                //    level.IsEnabled = true;
                //    compress_tool = @"Tools\compress\7z\7z.exe";
                //    break;
                //case 5://cab
                //    compress_tool = @"cmd.exe";
                //    break;
                //case 6:
                //    if (compress_level.Count == 5)
                //    {
                //        compress_level.RemoveAt(0);
                //        level.Items.RemoveAt(0);
                //    }
                //    if (root.Children.Count == 1 && !root.Children[0].IsDirectory)
                //    { type_extention = "gzip"; extention_item = OutArchiveFormat.GZip; }
                //    else
                //    { type_extention = "tar"; extention_item = OutArchiveFormat.Tar; }
                //    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                //    solid_block.IsEnabled = false;
                //    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                //    sfx.IsChecked = sfx.IsEnabled = false;
                //    level.SelectedIndex = 1;
                //    level.IsEnabled = true;
                //    compress_tool = @"Tools\compress\7z\7z.exe";
                //    break;
                //case 7:
                //    if (compress_level.Count == 4)
                //    {
                //        compress_level.Insert(0, 0);
                //        level.Items.Insert(0, "Just store");
                //    }
                //    sfx.IsChecked = sfx.IsEnabled = false;
                //    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                //    level.SelectedIndex = 0;
                //    solid_block.IsEnabled = false;
                //    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                //    level.IsEnabled = false;
                //    compress_tool = @"Tools\compress\iso\mkisofs.exe";
                //    break;
                //case 8:
                //    level.SelectedIndex = 2;
                //    level.IsEnabled = true;
                //    solid_block.IsEnabled = false;
                //    encrypt_file_name.IsChecked = encrypt_file_name.IsEnabled = false;
                //    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                //    compress_tool = @"Tools\compress\kgb\kgb2_console.exe";
                //    break;
                //case 9:
                //    compress_tool = @"Tools\compress\mou\mou.exe";
                //    break;
                //case 10:
                //    level.SelectedIndex = 2;
                //    level.IsEnabled = true;
                //    compress_tool = @"Tools\compress\rar\rar.exe";
                //    break;
                //case 11:
                //    level.SelectedIndex = 2;
                //    level.IsEnabled = true;
                //    compress_tool = @"Tools\compress\rar\rar.exe";
                //    break;
                //case 12:
                //    compress_tool = @"Tools\compress\sit\sit.exe";
                //    break;
                //case 13:
                //    compress_tool = @"Tools\compress\sit\sitx.exe";
                //    break;
                //case 14:
                //    if (compress_level.Count == 4)
                //    {
                //        compress_level.Insert(0, 0);
                //        level.Items.Insert(0, "Just store");
                //    }
                //    type_extention = "tar";
                //    extention_item = OutArchiveFormat.Tar;
                //    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                //    sfx.IsChecked = sfx.IsEnabled = false;
                //    solid_block.IsEnabled = false;
                //    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                //    level.SelectedIndex = 0;
                //    level.IsEnabled = false;
                //    compress_tool = @"Tools\compress\7z\7z.exe";
                //    break;
                //case 15:
                //    sfx.IsEnabled = true;
                //    level.SelectedIndex = 2;
                //    level.IsEnabled = true;
                //    compress_tool = @"Tools\compress\uha\uha.exe";
                //    break;
                //case 16:
                //    if (compress_level.Count == 4)
                //    {
                //        compress_level.Insert(0, 0);
                //        level.Items.Insert(0, "Just store");
                //    }
                //    type_extention = "wim";
                //    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                //    sfx.IsChecked = sfx.IsEnabled = false;
                //    level.SelectedIndex = 0;
                //    solid_block.IsEnabled = false;
                //    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                //    level.IsEnabled = false;
                //    compress_tool = @"Tools\compress\7z\7z.exe";
                //    break;
                //case 17:
                //    if (compress_level.Count == 5)
                //    {
                //        compress_level.RemoveAt(0);
                //        level.Items.RemoveAt(0);
                //    }
                //    if (root.Children.Count == 1 && !root.Children[0].IsDirectory)
                //    { type_extention = "xz"; extention_item = OutArchiveFormat.XZ; }
                //    else
                //    { type_extention = "tar"; extention_item = OutArchiveFormat.Tar; }
                //    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                //    solid_block.IsEnabled = false;
                //    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                //    sfx.IsChecked = sfx.IsEnabled = false;
                //    level.SelectedIndex = 1;
                //    level.IsEnabled = true;
                //    compress_tool = @"Tools\compress\7z\7z.exe";
                //    break;
                //case 18:
                //    if (compress_level.Count == 5)
                //    {
                //        compress_level.RemoveAt(0);
                //        level.Items.RemoveAt(0);
                //    }
                //    type_extention = "zip";
                //    extention_item = OutArchiveFormat.Zip;
                //    solid_block.IsEnabled = false;
                //    encrypt_file_name.IsChecked = encrypt_file_name.IsEnabled = false;
                //    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                //    sfx.IsChecked = sfx.IsEnabled = false;
                //    level.SelectedIndex = 1;
                //    level.IsEnabled = true;
                //    compress_tool = @"Tools\compress\7z\7z.exe";
                //    break;
                //case 19:
                //    if (compress_level.Count == 5)
                //    {
                //        compress_level.RemoveAt(0);
                //        level.Items.RemoveAt(0);
                //    }
                //    type_extention = "zip";
                //    extention_item = OutArchiveFormat.Zip;
                //    solid_block.IsEnabled = false;
                //    encrypt_file_name.IsChecked = encrypt_file_name.IsEnabled = false;
                //    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                //    sfx.IsChecked = sfx.IsEnabled = false;
                //    level.SelectedIndex = 1;
                //    level.IsEnabled = true;
                //    compress_tool = @"Tools\compress\7z\7z.exe";
                //    break;
            }
        }

        private void Compress_Click(object sender, RoutedEventArgs e)
        {
            if (file_name.Text.Equals(string.Empty))
                MessageBox.Show("Enter path name.");
            else
            {
                if (!file_name.Text.Any(x => Path.GetInvalidPathChars().Any(y => y == x)))
                {
                    if (File.Exists(file_name.Text) && new FileInfo(file_name.Text).Length > 0)
                        if (new My_MessageBox("FILE MANAGER", "File already exist, Overwrite?", "", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                            try
                            {
                                File.Delete(file_name.Text);
                                File.Create(file_name.Text);
                            }
                            catch (Exception) { return; }
                        else
                            return;
                    ArchiveOperation op_window = new ArchiveOperation();
                    if (password_checkbox.IsChecked.Value)
                    {
                        string password = "";
                        if (show_password.IsChecked.Value)
                        {
                            if (password_textbox.Text.Equals(string.Empty))
                            {
                                MessageBox.Show("Please Enter Password.");
                                return;
                            }
                            else
                                password = password_textbox.Text;
                        }
                        else
                        {
                            if (password_enc.Password.Equals(string.Empty))
                            {
                                MessageBox.Show("Please Enter Password.");
                                return;
                            }
                            else
                                password = password_enc.Password;
                        }
                        op_window.StartCompressing(archive_type, file_name.Text, root, level.SelectedIndex, solid_block.SelectedItem.ToString(), sfx.IsChecked.Value, true, password, encrypt_file_name.IsChecked.Value, (priority.SelectedIndex - 1) * 2);
                        Close();
                    }
                    else
                    {
                        op_window.StartCompressing(archive_type, file_name.Text, root, level.SelectedIndex, solid_block.SelectedItem.ToString(), sfx.IsChecked.Value, false, "", false, (priority.SelectedIndex - 1) * 2);
                        Close();
                    }
                }
                else
                    MessageBox.Show("Path contains unlegal charectars.");
            }
        }

        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            password_textbox.Visibility = Visibility.Visible;
            password_enc.Visibility = Visibility.Hidden;
            password_textbox.Text = password_enc.Password;
        }

        private void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            password_textbox.Visibility = Visibility.Hidden;
            password_enc.Visibility = Visibility.Visible;
            password_enc.Password = password_textbox.Text;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SFX_Checked(object sender, RoutedEventArgs e)
        {
            if (file_name.Text.LastIndexOf('.') >= 0)
                file_name.Text = file_name.Text.Remove(file_name.Text.LastIndexOf('.')) + ".exe";
        }

        private void SFX_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Path.HasExtension(file_name.Text))
                file_name.Text = file_name.Text.Remove(file_name.Text.LastIndexOf('.')) + "." + ((ListBoxItem)format.SelectedItem).Tag;
        }
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