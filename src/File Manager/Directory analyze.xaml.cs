using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Directory_analyze.xaml
    /// </summary>
    public partial class Directory_analyze : Window
    {
        bool starting_window = true;
        public Directory_analyze()
        {
            InitializeComponent();
        }

        #region What is Deferent

        private void MakeAnimation(TextBlock textblock, Grid content)
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                for (int i = 0; i < 50; i++)
                {
                    bw.ReportProgress(i);
                    Thread.Sleep(5);
                }
            };
            bw.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                textblock.FontSize -= (textblock.FontSize - 22) / 50;
                textblock.Margin = new Thickness(10, textblock.Margin.Top - (textblock.Margin.Top - 44) / 50, 0, 0);
            };
            bw.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                textblock.Margin = new Thickness(10, 44, 0, 0);
                textblock.FontSize = 22;
                content.Visibility = Visibility.Visible;
            };
            bw.RunWorkerAsync();
        }


        private void DirectoryGrid_Drop(object sender, DragEventArgs e)
        {
            string file_name = "-";
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                file_name = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            else if (e.Data.GetDataPresent(DataFormats.Text))
                file_name = (string)e.Data.GetData(DataFormats.Text);
            if (file_name != "-" && Directory.Exists(file_name))
            {
                if (sender == fd_grid)
                    FillFirstDirectoryGrid(file_name);
                else
                    FillSecondDirectoryGrid(file_name);
            }
        }

        private void FillSecondDirectoryGrid(string p)
        {
            if (p != sd_filepath.Text)
            {
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(p);
                    fd_filename.Text = dir.Name;
                    fd_filepath.Text = dir.FullName;
                    fd_date.Text = "Date: " + string.Format("{0}/{1}/{2}  ", Functions.FormalDigits(dir.LastWriteTimeUtc.Day, 2), Functions.FormalDigits(dir.LastWriteTimeUtc.Month, 2), Functions.FormalDigits(dir.LastWriteTimeUtc.Year, 4)) + string.Format("{0}:{1}", Functions.FormalDigits(dir.LastWriteTimeUtc.Hour, 2), Functions.FormalDigits(dir.LastWriteTimeUtc.Minute, 2));
                    fd_image.Source = Functions.GetFileImage256("directory");
                    fd_size.Text = string.Format("Contains: {0} directories, {1} files", dir.GetDirectories().Length, dir.GetFiles().Length);
                    fd_type.Text = "Type: Directory";
                }
                catch (Exception ex)
                {
                    RestValue(fd_grid);
                    fd_filepath.Text = "-";
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("Directories can not be same.");
        }

        private void FillFirstDirectoryGrid(string p)
        {
            if (p != fd_filepath.Text)
            {
                try
                {
                    DirectoryInfo dir = new DirectoryInfo(p);
                    sd_filename.Text = dir.Name;
                    sd_filepath.Text = dir.FullName;
                    sd_date.Text = "Date: " + string.Format("{0}/{1}/{2}  ", Functions.FormalDigits(dir.LastWriteTimeUtc.Day, 2), Functions.FormalDigits(dir.LastWriteTimeUtc.Month, 2), Functions.FormalDigits(dir.LastWriteTimeUtc.Year, 4)) + string.Format("{0}:{1}", Functions.FormalDigits(dir.LastWriteTimeUtc.Hour, 2), Functions.FormalDigits(dir.LastWriteTimeUtc.Minute, 2));
                    sd_image.Source = Functions.GetFileImage256("directory");
                    sd_size.Text = string.Format("Contains: {0} directories, {1} files", dir.GetDirectories().Length, dir.GetFiles().Length);
                    sd_type.Text = "Type: Directory";
                }
                catch (Exception ex)
                {
                    RestValue(fd_grid);
                    sd_filepath.Text = "-";
                    MessageBox.Show(ex.Message);
                }
            }
            else
                MessageBox.Show("Directories can not be same.");
        }

        private void FirstDirectoryPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!starting_window)
            {
                begin_analyze_button.IsEnabled = false;
                if (Directory.Exists(fd_filepath.Text))
                {
                    if (fd_filepath.Text.Equals(sd_filepath.Text))
                    {
                        fd_filepath.Text = "-";
                        return;
                    }
                    FillFirstDirectoryGrid(fd_filepath.Text);
                    if (Directory.Exists(sd_filepath.Text))
                        begin_analyze_button.IsEnabled = true;
                }
                else
                    RestValue(fd_grid);
            }
        }

        private void SecondDirectoryPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!starting_window)
            {
                begin_analyze_button.IsEnabled = false;
                if (File.Exists(sd_filepath.Text))
                {
                    if (fd_filepath.Text.Equals(sd_filepath.Text))
                    {
                        sd_filepath.Text = "-";
                        return;
                    }
                    FillSecondDirectoryGrid(sd_filepath.Text);
                    if (File.Exists(fd_filepath.Text))
                        begin_analyze_button.IsEnabled = true;
                }
                else
                    RestValue(sd_grid);
            }
        }

        private void RestValue(Grid grid)
        {
            if (grid == fd_grid)
            {
                fd_filename.Text = "First file";
                fd_date.Text = "-";
                fd_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/warning-grey.png"));
                fd_size.Text = "-";
                fd_type.Text = "-";
            }
            else if (grid == sd_grid)
            {
                sd_filename.Text = "First file";
                sd_date.Text = "-";
                sd_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/warning-grey.png"));
                sd_size.Text = "-";
                sd_type.Text = "-";
            }
        }

        private void Browser_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.DialogTitle = string.Format("Select the {0} directory to compare:", sender == fd_browse ? "first" : "second");
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.MultiSelect = false;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                if (Directory.Exists(sfd.SelectedItem))
                {
                    if (sender == fd_browse)
                        FillFirstDirectoryGrid(sfd.SelectedItem);
                    else
                        FillSecondDirectoryGrid(sfd.SelectedItem);
                }
            }
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            starting_window = false;
        }


    }
}
