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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Compress_Files.xaml
    /// </summary>
    public partial class Compress_Files : Window
    {
        CompresserListView root;
        Process P;
        string compress_tool = "", type_extention = "";
        List<byte> compress_level = new List<byte>();
        string[] sold_block_size = { "off", "1m", "2m", "4m", "8m", "16m", "32m", "64m", "128m", "256m", "512m", "1g", "2g", "4g", "8g", "16g", "32g", "64g", "on" };
        bool go_again;
        int done = -1, exit_code = -999000;
        string error_message;
        public Compress_Files()
        {
            InitializeComponent();
            compress_level.Add(0);
            compress_level.Add(1);
            compress_level.Add(5);
            compress_level.Add(7);
            compress_level.Add(9);
            format.SelectedIndex = 0;
            priority.SelectedIndex = 1;
        }

        internal void ShowDialog(CompresserListView root)
        {
            this.root = root;
            ShowDialog();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            if (sfx.IsChecked.Value)
                sfd.Filter = FileInfoCombonents.FilesInformation[".exe"].Description + "|*.exe";
            else
                switch (format.SelectedIndex)
                {
                    case 0:
                        sfd.Filter = Functions.GetFileType(".7z") + "|*.7z";
                        break;
                    case 1:
                        sfd.Filter = Functions.GetFileType(".ace") + "|*.ace";
                        break;
                    case 2:
                        sfd.Filter = Functions.GetFileType(".arc") + "|*.arc";
                        break;
                    case 3:
                        sfd.Filter = Functions.GetFileType(".b1") + "|*.b1";
                        break;
                    case 4:
                        sfd.Filter = Functions.GetFileType(".bzip2") + "|*.bzip";
                        break;
                    case 5:
                        sfd.Filter = Functions.GetFileType(".cab") + "|*.cab";
                        break;
                    case 6:
                        sfd.Filter = Functions.GetFileType(".gzip") + "|*.gzip";
                        break;
                    case 7:
                        sfd.Filter = Functions.GetFileType(".iso") + "|*.iso";
                        break;
                    case 8:
                        sfd.Filter = Functions.GetFileType(".kgb") + "|*.kgb";
                        break;
                    case 9:
                        sfd.Filter = Functions.GetFileType(".mou") + "|*.mou";
                        break;
                    case 10:
                        sfd.Filter = Functions.GetFileType(".rar") + "|*.rar";
                        break;
                    case 11:
                        sfd.Filter = Functions.GetFileType(".rar") + "|*.rar";
                        break;
                    case 12:
                        sfd.Filter = Functions.GetFileType(".sit") + "|*.sit";
                        break;
                    case 13:
                        sfd.Filter = Functions.GetFileType(".sitx") + "|*.sitx";
                        break;
                    case 14:
                        sfd.Filter = Functions.GetFileType(".tar") + "|*.tar";
                        break;
                    case 15:
                        sfd.Filter = Functions.GetFileType(".uha") + "|*.uha";
                        break;
                    case 16:
                        sfd.Filter = Functions.GetFileType(".wim") + "|*.wim";
                        break;
                    case 17:
                        sfd.Filter = Functions.GetFileType(".xz") + "|*.xz";
                        break;
                    case 18:
                        sfd.Filter = Functions.GetFileType(".zip") + "|*.zip";
                        break;
                    case 19:
                        sfd.Filter = Functions.GetFileType(".zipx") + "|*.zipx";
                        break;
                }
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (file_name.Text != "")
                sfd.FileName = file_name.Text;
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                file_name.Text = sfd.FileName;
        }

        private void Format_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!sfx.IsChecked.Value)
                SFX_Unchecked(null, null);
            switch (format.SelectedIndex)
            {
                case 0:
                    if (compress_level.Count == 4)
                    {
                        compress_level.Insert(0, 0);
                        level.Items.Insert(0, "Just store");
                    }
                    type_extention = "7z";
                    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = true;
                    sfx.IsEnabled = true;
                    solid_block.IsEnabled = true;
                    solid_block.SelectedIndex = 3;
                    level.SelectedIndex = 2;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\7z\7z.exe";
                    break;
                case 1:
                    compress_tool = @"Tools\compress\ace\ace.exe";
                    break;
                case 2:
                    sfx.IsEnabled = true;
                    level.SelectedIndex = 2;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\arc\arc.exe";
                    break;
                case 3:
                    compress_tool = @"Tools\compress\ace\b1.exe";
                    break;
                case 4:
                    if (compress_level.Count == 5)
                    {
                        compress_level.RemoveAt(0);
                        level.Items.RemoveAt(0);
                    }
                    if (root.Children.Count == 1 && !root.Children[0].IsDirectory)
                        type_extention = "bzip2";
                    else
                        type_extention = "tar";
                    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                    solid_block.IsEnabled = false;
                    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                    sfx.IsChecked = sfx.IsEnabled = false;
                    level.SelectedIndex = 1;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\7z\7z.exe";
                    break;
                case 5://cab
                    compress_tool = @"cmd.exe";
                    break;
                case 6:
                    if (compress_level.Count == 5)
                    {
                        compress_level.RemoveAt(0);
                        level.Items.RemoveAt(0);
                    }
                    if (root.Children.Count == 1 && !root.Children[0].IsDirectory)
                        type_extention = "gzip";
                    else
                        type_extention = "tar";
                    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                    solid_block.IsEnabled = false;
                    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                    sfx.IsChecked = sfx.IsEnabled = false;
                    level.SelectedIndex = 1;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\7z\7z.exe";
                    break;
                case 7:
                    if (compress_level.Count == 4)
                    {
                        compress_level.Insert(0, 0);
                        level.Items.Insert(0, "Just store");
                    }
                    sfx.IsChecked = sfx.IsEnabled = false;
                    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                    level.SelectedIndex = 0;
                    solid_block.IsEnabled = false;
                    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                    level.IsEnabled = false;
                    compress_tool = @"Tools\compress\iso\mkisofs.exe";
                    break;
                case 8:
                    level.SelectedIndex = 2;
                    level.IsEnabled = true;
                    solid_block.IsEnabled = false;
                    encrypt_file_name.IsChecked = encrypt_file_name.IsEnabled = false;
                    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                    compress_tool = @"Tools\compress\kgb\kgb2_console.exe";
                    break;
                case 9:
                    compress_tool = @"Tools\compress\mou\mou.exe";
                    break;
                case 10:
                    level.SelectedIndex = 2;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\rar\rar.exe";
                    break;
                case 11:
                    level.SelectedIndex = 2;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\rar\rar.exe";
                    break;
                case 12:
                    compress_tool = @"Tools\compress\sit\sit.exe";
                    break;
                case 13:
                    compress_tool = @"Tools\compress\sit\sitx.exe";
                    break;
                case 14:
                    if (compress_level.Count == 4)
                    {
                        compress_level.Insert(0, 0);
                        level.Items.Insert(0, "Just store");
                    }
                    type_extention = "tar";
                    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                    sfx.IsChecked = sfx.IsEnabled = false;
                    solid_block.IsEnabled = false;
                    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                    level.SelectedIndex = 0;
                    level.IsEnabled = false;
                    compress_tool = @"Tools\compress\7z\7z.exe";
                    break;
                case 15:
                    sfx.IsEnabled = true;
                    level.SelectedIndex = 2;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\uha\uha.exe";
                    break;
                case 16:
                    if (compress_level.Count == 4)
                    {
                        compress_level.Insert(0, 0);
                        level.Items.Insert(0, "Just store");
                    }
                    type_extention = "wim";
                    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                    sfx.IsChecked = sfx.IsEnabled = false;
                    level.SelectedIndex = 0;
                    solid_block.IsEnabled = false;
                    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                    level.IsEnabled = false;
                    compress_tool = @"Tools\compress\7z\7z.exe";
                    break;
                case 17:
                    if (compress_level.Count == 5)
                    {
                        compress_level.RemoveAt(0);
                        level.Items.RemoveAt(0);
                    }
                    if (root.Children.Count == 1 && !root.Children[0].IsDirectory)
                        type_extention = "xz";
                    else
                        type_extention = "tar";
                    password.IsEnabled = password_retry.IsEnabled = show_password.IsEnabled = encrypt_file_name.IsEnabled = password_textbox.IsEnabled = false;
                    solid_block.IsEnabled = false;
                    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                    sfx.IsChecked = sfx.IsEnabled = false;
                    level.SelectedIndex = 1;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\7z\7z.exe";
                    break;
                case 18:
                    if (compress_level.Count == 5)
                    {
                        compress_level.RemoveAt(0);
                        level.Items.RemoveAt(0);
                    }
                    type_extention = "zip";
                    solid_block.IsEnabled = false;
                    encrypt_file_name.IsChecked = encrypt_file_name.IsEnabled = false;
                    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                    sfx.IsChecked = sfx.IsEnabled = false;
                    level.SelectedIndex = 1;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\7z\7z.exe";
                    break;
                case 19:
                    if (compress_level.Count == 5)
                    {
                        compress_level.RemoveAt(0);
                        level.Items.RemoveAt(0);
                    }
                    type_extention = "zip";
                    solid_block.IsEnabled = false;
                    encrypt_file_name.IsChecked = encrypt_file_name.IsEnabled = false;
                    solid_block.SelectedIndex = solid_block.Items.Count - 1;
                    sfx.IsChecked = sfx.IsEnabled = false;
                    level.SelectedIndex = 1;
                    level.IsEnabled = true;
                    compress_tool = @"Tools\compress\7z\7z.exe";
                    break;
            }
        }

        private void Copress_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(file_name.Text))
                if (new My_MessageBox("Compressed File Manager", "File already exist, Overwrite?", "", "", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog_warning.png")), null, null).ShowThisDialog() == MyMessageBoxResult.Yes)
                    try { File.Delete(file_name.Text); }
                    catch (Exception) { return; }
                else
                    return;
            main.IsEnabled = false;
            if (!show_password.IsChecked.Value && password.Password != password_retry.Password)
                new My_MessageBox("File Manager", "Password doesn't! match.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            else if (file_name.Text == String.Empty)
                new My_MessageBox("File Manager", "Please select file to save", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            else
            {
                bool encrypt = encrypt_file_name.IsChecked.Value, sfx_value = sfx.IsChecked.Value, encrypt_headers = encrypt_file_name.IsChecked.Value;
                string password_st = show_password.IsChecked.Value ? password_textbox.Text : password.Password, file_path = file_name.Text;
                int solid = solid_block.SelectedIndex, level_value = level.SelectedIndex, format_value = format.SelectedIndex;
                ThicknessAnimation animator = new ThicknessAnimation(new Thickness(-500, 0, 0, 0), new Duration(new TimeSpan(2000000)));
                main_grid.BeginAnimation(Grid.MarginProperty, animator);
                Start_Indeterminate();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += bw_DoWork;
                bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                bw.WorkerSupportsCancellation = true;
                done = -1;
                error_message = "";
                bw.RunWorkerAsync();
                Thread tt = new Thread(() =>
                {
                    try
                    {
                        string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Temp\archive.tmp";
                        StreamWriter st = new StreamWriter(File.Open(path, FileMode.OpenOrCreate));
                        StringBuilder files_paths = new StringBuilder();
                        foreach (var i in root.Children)
                            st.WriteLine(i.FileFullName);
                        st.Close();
                        string compress_method = " -p\"";
                        if (encrypt)
                            compress_method = " -hp\"";
                        foreach (var i in root.Children)
                            files_paths.Append("\"" + i.FileFullName + "\" ");
                        switch (compress_tool)
                        {
                            case @"Tools\compress\7z\7z.exe":
                                Compress(" a \"" + file_path + "\" @\"" + path + "\" " + (type_extention == "" ? "" : ("-t" + type_extention)) + (sfx_value ? " -sfx7z.sfx" : "") + (password_st == string.Empty ? "" : (" -p\"" + password_st + "\"")) + (encrypt_headers ? " -mhe" : String.Empty) + (type_extention != "wim" && type_extention != "tar" ? " -mx=" + compress_level[level_value] : "") + (format_value == 0 ? " -ms=" + sold_block_size[solid] : ""));
                                break;
                            case @"Tools\compress\rar\rar.exe":
                                Compress("a -m" + (level_value > 1 ? level_value + 1 : level_value) + (solid > 0 ? " -s" : "") + (" -ma" + (format_value - 6)) + (sfx_value ? " -sfx" : "") + (password_st == string.Empty ? "" : (compress_method + password_st + "\"")) + " \"" + file_path + "\" " + files_paths + "");
                                break;
                            case @"Tools\compress\arc\arc.exe":
                                Compress("a -m" + compress_level[level_value] + (" -s" + sold_block_size[solid]) + (sfx_value ? " -sfx" : "") + (password_st == String.Empty ? "" : (" -p\"" + password_st + "\"")) + " \"" + file_path + "\" @\"" + path + "\" ");
                                break;
                            case @"Tools\compress\kgb\kgb2_console.exe":
                                foreach (var i in root.Children)
                                    files_paths.Append("\"" + i.FileFullName + "\" ");
                                Compress("-a7 -m" + compress_level[level_value] /*+ (sfx.IsChecked.Value ? " -sfx" : "")*/ + (password_st == String.Empty ? "" : (" -p\"" + password_st + "\"")) + " \"" + file_path + "\" " + files_paths.ToString());
                                break;
                            case @"Tools\compress\uha\uha.exe":
                                Compress("a -r+ -ed- -pr m" + (level_value < 4 ? level_value : 3) + (level_value == 4 ? " -md32768" : "") + " -mm+ -md+ -b32768" + (password_st == String.Empty ? "" : (" -pw\"" + password_st + "\"")) + (encrypt_headers ? " -ph" : String.Empty) + " \"" + file_path + "\" " + files_paths);
                                break;
                        }
                        File.Delete(path);
                        done = 0;
                        if (root.Children.Count == 1 && root.Children[0].IsDirectory || root.Children.Count > 1)
                            try
                            {
                                switch (format_value)
                                {
                                    case 4:
                                        MultiStageCompress("bzip2");
                                        break;
                                    case 6:
                                        MultiStageCompress("gzip");
                                        break;
                                    case 17:
                                        MultiStageCompress("xz");
                                        break;
                                }
                            }
                            catch (Exception ex) { done = 1; error_message = ex.Message; }
                    }
                    catch (Exception ex) { done = 1; error_message = ex.Message; }
                });
                tt.Start();
            }
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Stop_Indeterminate();
            if (done == 0)
            {
                if (exit_code == 0)
                    new My_MessageBox("File Manager", "Done", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
                else if (exit_code == 1)
                    new My_MessageBox("File Manager", "Done but some files have not been compressed.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
                Close();
            }
            else if (done == 1 || exit_code == 2 || exit_code == 8)
            {
                ThicknessAnimation animator = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(2000000)));
                main_grid.BeginAnimation(Grid.MarginProperty, animator);
                new My_MessageBox("File Manager", done == 1 ? error_message : exit_code == 2 ? "Fatal error." : "Not enough memory for operation", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
                main_grid.IsEnabled = true;
            }
            else if (exit_code==255)
                new My_MessageBox("File Manager", "User stopped the process.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); 
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (done < 0)
                Thread.Sleep(100);
            exit_code = P.ExitCode;
        }

        void Compress(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo(compress_tool, arguments);
            P = new Process();
            P.StartInfo = psi;
            P.Start();
            //p.PriorityClass = ProcessPriorityClass.Normal;
            P.WaitForExit();
        }

        void MultiStageCompress(string format)
        {
            FileInfo n = new FileInfo(file_name.Text);
            n.MoveTo(n.FullName.Remove(n.FullName.Length - n.Extension.Length) + ".tar");
            Compress(" a \"" + file_name.Text + "\" \"" + n.FullName + "\" -t" + format + " -mx=" + compress_level[level.SelectedIndex]);
            n.Delete();
        }

        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            password_retry.Visibility = password.Visibility = Visibility.Hidden;
            password_textbox.Visibility = Visibility.Visible;
            password_textbox.Text = password.Password;
        }

        private void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
        {
            password_retry.Visibility = password.Visibility = Visibility.Visible;
            password_textbox.Visibility = Visibility.Hidden;
            password.Password = password_textbox.Text;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Indeterminate ProcessBar

        private void Start_Indeterminate()
        {
            go_again = true;
            BackgroundWorker main_bw = new BackgroundWorker();
            main_bw.DoWork += MainBW_DoWork;
            main_bw.ProgressChanged += MainBW_ProgressChanged;
            main_bw.WorkerReportsProgress = true;
            main_bw.RunWorkerCompleted += MainBW_RunWorkerCompleted;
            main_bw.RunWorkerAsync(eg);
        }

        void Stop_Indeterminate()
        {
            go_again = false;
        }

        void MainBW_DoWork(object sender, DoWorkEventArgs e)
        {
            Grid n = (Grid)e.Argument;
            BackgroundWorker bw = (BackgroundWorker)sender;
            for (int i = 0; i < 5; i++)
            {
                bw.ReportProgress(i);
                Thread.Sleep(200);
            }
            Thread.Sleep(3500);
        }

        void MainBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            BackgroundWorker stage1_bw = new BackgroundWorker();
            stage1_bw.ProgressChanged += AllStagesBW_ProgressChanged;
            stage1_bw.DoWork += Stage1BW_DoWork;
            stage1_bw.WorkerReportsProgress = true;
            stage1_bw.RunWorkerCompleted += Stage1BW_RunWorkerCompleted;
            stage1_bw.RunWorkerAsync(eg.Children[e.ProgressPercentage]);
        }

        void MainBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (go_again)
                Start_Indeterminate();
        }


        void Stage1BW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            for (int i = 1; i < 66; i++)
            {
                bw.ReportProgress(2 * i, e.Argument);
                Thread.Sleep(5);
            }
            e.Result = e.Argument;
        }

        void Stage1BW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker stage2_bw = new BackgroundWorker();
            stage2_bw.ProgressChanged += AllStagesBW_ProgressChanged;
            stage2_bw.DoWork += Stage2BW_DoWork;
            stage2_bw.WorkerReportsProgress = true;
            stage2_bw.RunWorkerCompleted += Stage2BW_RunWorkerCompleted;
            stage2_bw.RunWorkerAsync(e.Result);
        }


        private void Stage2BW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            for (int i = 1; i < 66; i++)
            {
                bw.ReportProgress(132 + 2 * i, e.Argument);
                Thread.Sleep(25);
            }
            e.Result = e.Argument;
        }

        private void Stage2BW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker stage3_bw = new BackgroundWorker();
            stage3_bw.ProgressChanged += AllStagesBW_ProgressChanged;
            stage3_bw.DoWork += Stage3BW_DoWork;
            stage3_bw.WorkerReportsProgress = true;
            stage3_bw.RunWorkerAsync(e.Result);
        }


        private void Stage3BW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            for (int i = 1; i < 66; i++)
            {
                bw.ReportProgress(264 + 2 * i, e.Argument);
                Thread.Sleep(5);
            }
        }


        void AllStagesBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ((Rectangle)e.UserState).Margin = new Thickness(e.ProgressPercentage, 0, 0, 0);
        }

        #endregion

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (P != null && !P.HasExited)
                try
                { P.Close(); }
                catch (Exception) { }
            ThicknessAnimation animator = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(2000000)));
            main_grid.BeginAnimation(Grid.MarginProperty, animator);
            main_grid.IsEnabled = true;
            if (!P.HasExited)
                P.Kill();
            if (File.Exists(file_name.Text))
                try
                { File.Delete(file_name.Text); }
                catch (Exception) { }
        }

        private void SFX_Checked(object sender, RoutedEventArgs e)
        {
            if (file_name.Text.LastIndexOf('.') >= 0)
                file_name.Text = file_name.Text.Remove(file_name.Text.LastIndexOf('.')) + ".exe";
        }

        private void SFX_Unchecked(object sender, RoutedEventArgs e)
        {
            if (file_name.Text.LastIndexOf('.') >= 0)
                file_name.Text = file_name.Text.Remove(file_name.Text.LastIndexOf('.')) + "." + ((ListBoxItem)format.SelectedItem).Content;
        }
    }
}
