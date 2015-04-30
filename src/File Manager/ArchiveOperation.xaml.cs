using SevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for ArchiveOperation.xaml
    /// </summary>
    public partial class ArchiveOperation : Window
    {
        Brush progress_brush;
        double progress_value = 0, old_progress = 0;
        int current_index = -1;
        long time, last_time;
        bool stop_operation = false;
        public ArchiveOperation()
        {
            InitializeComponent();
        }

        internal void StartCompressing(ArchiveType type, string file_name, CompressItem files_names, int compress_level, string solid_block_size, bool is_sfx, bool has_password, string password, bool encrypt_file_name, int priority)
        {
            progress_brush = Brushes.MediumPurple;
            Title = "Compressing " + Functions.FileSize(files_names.RealSize.Value, 2);
            speed_text.Text = "";
            time_text.Text = "";
            archive_name.ToolTip = archive_name.Text = file_name;
            //if (archive_name.ActualWidth > Width - 160)
            //    archive_name.ToolTip = file_name;
            switch (type)
            {
                case ArchiveType.SevenZip:
                    SevenZipCompressFunction(file_name, files_names, compress_level, solid_block_size, is_sfx, false, password, encrypt_file_name, priority);
                    break;
            }
            ShowDialog();
        }

        private void SevenZipCompressFunction(string file_name, CompressItem root_item, int compress_level, string solid_block_size, bool is_sfx, bool password_protection, string password, bool encrypt_file_name, int priority)
        {
            string library_path = Path.Combine(Environment.CurrentDirectory + @"\Tools\compress\7z\7z.dll");
            if (File.Exists(library_path))
            {
                SevenZipBase.SetLibraryPath(library_path);
                try
                {
                    Stream file_stream = File.OpenWrite(file_name);
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.WorkerReportsProgress = true;
                    bw.DoWork += (object sender, DoWorkEventArgs e) =>
                    {
                        SevenZipCompressor szc = new SevenZipCompressor();
                        szc.ArchiveFormat = OutArchiveFormat.SevenZip;
                        szc.CompressionLevel = (CompressionLevel)compress_level;
                        szc.CompressionMode = CompressionMode.Create;
                        szc.EncryptHeaders = encrypt_file_name;
                        szc.IncludeEmptyDirectories = true;
                        szc.Compressing += (object compress_sender, ProgressEventArgs compress_e) =>
                        {
                            if (stop_operation)
                                e.Cancel = true;
                            else
                                bw.ReportProgress(compress_e.PercentDone);
                        };
                        szc.FilesFound += (object found_sender, IntEventArgs found_e) =>
                        {
                            //bw.ReportProgress(0);
                        };
                        szc.FileCompressionStarted += (object com_sender, FileNameEventArgs com_e) =>
                        {
                            old_progress = progress_value;
                            MessageBox.Show(current_index.ToString());
                            if (current_index > 0)
                            {
                                progress_value += root_item.AllFiles[current_index].RealSize.Value;
                                time = DateTime.UtcNow.Ticks - last_time;
                            }
                            last_time = DateTime.UtcNow.Ticks;
                            current_index++;
                        };
                        if (password_protection)
                        {
                            szc.CompressFilesEncrypted(file_stream, password, CompressItem.GetFilsName(root_item.AllFiles));
                        }
                        else
                        {
                            szc.CompressFiles(file_stream, CompressItem.GetFilsName(root_item.AllFiles));
                        }
                    };
                    bw.ProgressChanged += (object compress_sender, ProgressChangedEventArgs compress_e) =>
                    {
                        progress_bar.Value = progress_value;
                        double val = progress_value * 100.0 / (progress_bar.Maximum + double.Epsilon);
                        percent.Text = Math.Round(val, 0) + "%";
                        ChangeRemain((root_item.RealSize.Value + 0.0) - progress_value, false);
                        TaskbarItemInfo.ProgressValue = val / 100;
                        double speed = (progress_value - old_progress) / (time / 10000000);
                        speed_text.Text = Functions.FileSize(speed, 1) + " per second";
                        double time_in_sec = (root_item.RealSize.Value + 0.0 - progress_value) / (speed + double.Epsilon);
                        //time_text.Text = Convert.ToDateTime(time_in_sec).ToString();
                    };

                    bw.RunWorkerCompleted += (object compress_sender, RunWorkerCompletedEventArgs compress_e) =>
                    {
                        file_stream.Close();
                        Close();
                    };
                    progress_bar.IsIndeterminate = false;
                    bw.RunWorkerAsync();
                }
                catch (Exception) { }

            }
            else
                MessageBox.Show("Fatal Error", "We can not find 7-Zip library file.");
        }
        void ChangeRemain(double size, bool is_items)
        {
            string remain_number;
            string remain_unti;
            remain_number = Functions.FileSize1000(size, 3);
            if (is_items)
                remain_unti = size > 1 ? "Items" : "Item";
            remain.Text = remain_number;
            remain_unit.Text = (is_items ? "item" : "byte") + (size > 1 ? "s" : "");
            remain_unit.Margin = new Thickness(100 - (10 * (4 - remain.Text.Length)), 40, 0, 0);
        }

        internal void StartExtracting(ArchiveType type, string file_name, string extact_path, bool extract_all, CompressItem files_names, int priority)
        {
            Show();
            progress_brush = Brushes.DarkGoldenrod;
        }

        private void Start_ThumbButtonInfo_Click(object sender, EventArgs e)
        {

        }

        private void Stop_ThumbButtonInfo_Click(object sender, EventArgs e)
        {

        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
