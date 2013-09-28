using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;
using System.Windows.Media.Animation;

namespace File_Explorer
{
    /// <summary>
    /// Interaction logic for Viewer.xaml
    /// </summary>

    public partial class Viewer : Window
    {
        MediaElement media;
        FileInfo file;
        DirectoryInfo dir;
        FileSystemInfo file_system;
        DriveInfo drive;
        ItemType file_type;
        int sub_dir_count, file_count, wait_for = 3000;
        bool isOn = false, isplay = true, mouse_move = false;
        string path;
        ScrollViewer scr;
        Point old_point;
        Image img;
        BitmapImage source;
        BackgroundWorker worker = new BackgroundWorker();
        Thread thread;

        public Viewer(string path, ItemType type, int sub_dirs, int sub_files)
        {
            InitializeComponent();
            this.file_type = type;
            sub_dir_count = sub_dirs;
            file_count = sub_files;
            if (type == ItemType.Directory)
            {
                dir = new DirectoryInfo(path);
                file_system = new DirectoryInfo(path);
            }
            else if (type == ItemType.Drive)
                drive = new DriveInfo(path);
            else if (type == ItemType.File)
            {
                file = new FileInfo(path);
                file_system = new FileInfo(path);
            }
            this.path = path;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FileName.Text = path;
            FileName.ToolTip = path;
            if (file_type == ItemType.Directory)
                MakeGridForDirectory();
            else if (file_type == ItemType.Drive)
                MakeGridForDrive();
            else if (file_type == ItemType.File)
                MakeGridForFile(Functions.GetFileIndex(file.Extension));
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
            thread = new Thread(HideControl);
            thread.IsBackground = true;
            thread.Start();
        }

        private void MakeGridForFile(int file_index)
        {
            MakeGridForFileSystem();
            //name.IsEnabled = !file.IsReadOnly;
            type.Text = Functions.GetFileType(file_index, file.Extension);
            size.Text = Functions.File_Size(file.Length);
            ItemImage.Source = item_image.Source = FileInformation.file_bitmap[file_index];
            if (FileInformation.file_type[file_index] == FileCategory.Unknown || FileInformation.file_type[file_index] == FileCategory.CannotOpen)
            {
                Container.Items.RemoveAt(0);
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 79, 0, 0);
            }
            else if (FileInformation.file_type[file_index] == FileCategory.Application)
            {
                Container.Items.RemoveAt(0);
                open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/run-application.png"));
                explore_button.ToolTip = "Run as administrator";
                explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/run-as-adimstrator.png"));
                explore_button.Click += RunAsAdministrator_Click;
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 47, 0, 0);
                caution.Visibility = Visibility.Visible;
            }
            else if (FileInformation.file_type[file_index] == FileCategory.Archive)
            {
                Container.Items.RemoveAt(0);                    //may then remove it to standalon condition to explore archive file
                explore_button.ToolTip = "Explorer archive";   //may then remove it to standalon condition to explore archive file
                explore_button.Click += ExplorerArchive_Click;
                explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/explore-archive.png"));
                extract_button.ToolTip = "Extract files from archive";
                extract_button.Click += ExtractArchive_Click;
                extract_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/extract-archive.png"));
            }
            else if (FileInformation.file_type[file_index] == FileCategory.Document)
            {
                if (file.Extension.ToLower() == ".rtf")
                    MakeRTFViewer();
                else if (file.Extension.ToLower() == ".xps" || file.Extension.ToLower() == ".oxps")
                    MakeXPSViewer();
                else
                    Container.Items.RemoveAt(0);
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 79, 0, 0);
            }
            else if (FileInformation.file_type[file_index] == FileCategory.Text || FileInformation.file_type[file_index] == FileCategory.TextCanRunAsApplication || FileInformation.file_type[file_index] == FileCategory.Html)
            {
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 47, 0, 0);
                if (FileInformation.file_type[file_index] == FileCategory.Html)
                    MakeHtmlViewer();
                else
                    MakeTextViewer();
                if (file.Extension.ToLower() == ".mht" || file.Extension.ToLower() == ".mhtml")
                {
                    explore_button.ToolTip = "Extract files from archive";
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/extract-archive.png"));
                    explore_button.Click += ExtractArchive_Click;
                }
                else
                {
                    explore_button.ToolTip = "Edit";
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/document-edit.png"));
                    explore_button.Click += Edit_Click;
                }
                if (FileInformation.file_type[file_index] == FileCategory.TextCanRunAsApplication)
                {
                    open_button.ToolTip = "Run commands";
                    open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/run-command.png"));
                    caution.Visibility = Visibility.Visible;
                    extract_button.Visibility = Visibility.Collapsed;
                    open_button.Margin = new Thickness(0, 47, 0, 0);
                }
            }
            else if (FileInformation.file_type[file_index] == FileCategory.Audio || FileInformation.file_type[file_index] == FileCategory.Vedio || FileInformation.file_type[file_index] == FileCategory.Image)
            {
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 79, 0, 0);
                if (FileInformation.file_type[file_index] == FileCategory.Image)
                    MakeImageViewer();
                else
                    MakeMediaViewer();
            }
            else if (FileInformation.file_type[file_index] == FileCategory.Font)
            {
                MakeFontViewer();
                explore_button.ToolTip = "Install font";
                explore_button.Click += InstallFont_Click;
                explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/font-install.png"));
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 47, 0, 0);
            }
            else if (FileInformation.file_type[file_index] == FileCategory.DiskImage)
            {
                Container.Items.RemoveAt(0);
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 47, 0, 0);
                if (file.Extension.ToLower() == ".isz")
                {
                    explore_button.ToolTip = "Convert to ISO";
                    explore_button.Click += ConvertToISO_Click;
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/covert-to-iso.png"));
                }
                else if (file.Extension.ToLower() == ".mou")
                {
                    explore_button.ToolTip = "Extract files from archive";
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/extract-archive.png"));
                    explore_button.Click += ExtractArchive_Click;
                }
                else
                {
                    explore_button.Visibility = Visibility.Collapsed;
                    open_button.Margin = new Thickness(0, 79, 0, 0);
                }
            }
        }

        private void ConvertToISO_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.SaveFileDialog SFD = new System.Windows.Forms.SaveFileDialog();
                SFD.Filter = "ISO File (*.iso)|*.iso";
                if (SFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Process.Start(new ProcessStartInfo("Tools\\unisz.exe", " \"" + path + "\""));
                    File.Move(path.Remove(path.Length - 1) + "o", SFD.FileName);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            try
            { Process.Start(new ProcessStartInfo("Tools\\notepad++.exe", file.FullName)); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void MakeGridForDrive()
        {
            Container.Items.RemoveAt(0);
            open_with_button.ToolTip = "Open in new window";
            open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/harddisk.png"));
            open_with_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/window-new.png"));
            copy_to_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/folder-copy.png"));
            extract_button.Visibility = Visibility.Collapsed;
            explore_button.Visibility = Visibility.Collapsed;
            delete_button.Visibility = Visibility.Collapsed;
            move_to_button.Visibility = Visibility.Collapsed;
            open_button.Margin = new Thickness(0, 143, 0, 0);
            ItemImage.Source = item_image.Source = new BitmapImage(new Uri(Functions.GetDriveImage(path, MainWindow.StandardImageSource)));
            modified_all.Visibility = Visibility.Collapsed;
            accessed_all.Visibility = Visibility.Collapsed;
            if (drive.IsReady)
            {
                name.Text = drive.VolumeLabel;
                type_title.Text = "File System: ";
                type.Text = drive.DriveFormat;
                size_title.Text = "Disk Size: ";
                size.Text = Functions.File_Size(drive.TotalFreeSpace) + " Free of " + Functions.File_Size(drive.TotalSize);
                created_title.Text = "Used Space: ";
                created.Text = Functions.File_Size(drive.TotalSize - drive.TotalFreeSpace);
                normal.Visibility = Visibility.Collapsed;
                read_only.Visibility = Visibility.Collapsed;
                hidden.Visibility = Visibility.Collapsed;
                archive.Visibility = Visibility.Collapsed;
                encrypted.Visibility = Visibility.Collapsed;
                TextBlock1.Visibility = Visibility.Collapsed;
                Rectangle1.Visibility = Visibility.Collapsed;
                system.Visibility = Visibility.Collapsed;
                TextBlock2.Visibility = Visibility.Collapsed;
                Rectangle3.Visibility = Visibility.Collapsed;
                directory.Visibility = Visibility.Collapsed;
                TextBlock3.Visibility = Visibility.Collapsed;
                Rectangle4.Visibility = Visibility.Collapsed;
                other.IsChecked = true;
                drive_usage.Value = 100 - (drive.TotalFreeSpace * 100) / drive.TotalSize;
                if (drive_usage.Value > 90)
                    drive_usage.Foreground = System.Windows.Media.Brushes.Crimson;
                else
                    drive_usage.Foreground = System.Windows.Media.Brushes.RoyalBlue;
            }
            else
            {
                Container.Items.RemoveAt(1);
                name.Text = drive.Name;
                name.IsEnabled = false;
                type.Text = drive.DriveType.ToString();
                size_title.Text = "";
                size.Text = "Drive is not ready!";
                created_all.Visibility = Visibility.Collapsed;
                drive_usage.Visibility = Visibility.Collapsed;
            }
            Change_Name.Visibility = Visibility.Hidden;

        }

        private void MakeGridForDirectory()
        {
            Container.Items.RemoveAt(0);
            open_with_button.ToolTip = "Open in new window";
            open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/folder.png"));
            open_with_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/window-new.png"));
            copy_to_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/folder-copy.png"));
            move_to_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/folder-move.png"));
            extract_button.Visibility = Visibility.Collapsed;
            explore_button.Visibility = Visibility.Collapsed;
            open_button.Margin = new Thickness(0, 79, 0, 0);
            type.Text = "Directory";
            size_title.Text = "Contains: ";
            size.Text = sub_dir_count + " Directories & " + file_count + " Files";
            ItemImage.Source = item_image.Source = FileInformation.file_bitmap[FileInformation.file_bitmap.Count - 2];
            MakeGridForFileSystem();
        }

        private void MakeGridForFileSystem()
        {
            GetAttributes();
            name.Text = file_system.Name;
            created.Text = file_system.CreationTimeUtc.ToString();
            modified.Text = file_system.LastWriteTimeUtc.ToString();
            accessed.Text = file_system.LastAccessTimeUtc.ToString();
            drive_usage.Visibility = Visibility.Collapsed;
            Change_Name.Visibility = Visibility.Hidden;
        }

        private void MakeXPSViewer()
        {
            DocumentViewer doc_viewer = new DocumentViewer();
            try
            {
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                viewer.Children.Insert(0, doc_viewer);
                XpsDocument XPS = new XpsDocument(path, FileAccess.Read);
                doc_viewer.Document = XPS.GetFixedDocumentSequence();
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void MakeRTFViewer()
        {
            RichTextBox rich = new RichTextBox();
            rich.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            viewer.Children.Insert(0, rich);
            rich.IsReadOnly = true;
            try
            {
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                FileStream FS = new FileStream(path, FileMode.Open, FileAccess.Read);
                TextRange TR = new TextRange(rich.Document.ContentStart, rich.Document.ContentEnd);
                TR.Load(FS, DataFormats.Rtf);
                FS.Close();
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            }
            catch (Exception ex)
            { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void MakeFontViewer()
        {
            ListView font_list = new ListView();
            FontFamily this_font = new System.Windows.Media.FontFamily(new Uri(path), "Name");
            font_list.Background = null;
            font_list.Foreground = Brushes.WhiteSmoke;
            font_list.FontSize = 18;
            font_list.FontFamily = this_font;
            ListViewItem item = new ListViewItem();
            item.FontSize = 14;
            item.Content = "Font Name: " + this_font.ToString();
            font_list.Items.Add(item);
            item = new ListViewItem();
            item.Content = "__________________________________________________________";
            font_list.Items.Add(item);
            item = new ListViewItem();
            item.FontFamily = this_font;
            item.Content = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            font_list.Items.Add(item);
            item = new ListViewItem();
            item.FontFamily = this_font;
            item.Content = "0123456789.:,;(!?)+-*/=";
            font_list.Items.Add(item);
            item = new ListViewItem();
            item.FontFamily = this_font;
            item.Content = "أبجد هوز حطي كلمن سعفص قرشت ثخذ ضظغ";
            font_list.Items.Add(item);
            item = new ListViewItem();
            item.Content = "__________________________________________________________";
            font_list.Items.Add(item);
            int[] font_size = { 12, 18, 24, 36, 48, 60, 72 };
            foreach (var i in font_size)
            {
                item = new ListViewItem();
                item.Content = i + "-  The Quick brown fox jumps over the lazy dog. 0123456789" + "\t" + "هيف الخصور نواعم الأبدان" + "\t" + "فلج الثغور إذا ابتسمن ضواحكا";
                item.FontSize = i;
                font_list.Items.Add(item);
            }
            viewer.Children.Insert(0, font_list);
        }

        private void MakeMediaViewer()
        {
            media = new MediaElement();
            media.Source = new Uri(path);
            media.LoadedBehavior = MediaState.Manual;
            media.MediaFailed += media_MediaFailed;
            media.MediaEnded += media_MediaEnded;
            viewer.Children.Insert(0, media);
            media.Play();
            play.Click += PlayMedia_Click;
            stop.Click += stop_Click;
            BackgroundWorker moveSlider = new BackgroundWorker();
            moveSlider.WorkerReportsProgress = true;
            moveSlider.DoWork+=moveSlider_DoWork;
            moveSlider.ProgressChanged += moveSlider_ProgressChanged;
            moveSlider.RunWorkerAsync();
            Controls.Visibility = Visibility.Visible;
            volume.Value = 50;
            volume.ValueChanged += volume_ValueChanged;
            isOn = true;
        }

        void HideControl()
        {
            while (true)
            {
                mouse_move = false;
                Thread.Sleep(wait_for);
                if (!mouse_move)
                    worker.ReportProgress(10);
            }
        }

        private void media_MediaEnded(object sender, RoutedEventArgs e)
        {
            play_content.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/copy-start.png"));
            media.Stop();
            isOn = false;
        }

        private void media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            caution.Visibility = Visibility.Visible;
            failed.Text = "Can't open this media file, make sure that you install all codec required and the file is a correct media file.";
            Controls.Visibility = Visibility.Collapsed;
        }

        private void moveSlider_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (media.NaturalDuration.HasTimeSpan)
            {
                Slider1.Value = (media.Position.TotalSeconds / media.NaturalDuration.TimeSpan.TotalSeconds) * Slider1.Maximum;
                second.Text = Functions.ToString(media.Position.Seconds);
                minute.Text = Functions.ToString(media.Position.Minutes);
                hour.Text = Functions.ToString(media.Position.Hours);
                day.Text = Functions.ToString(media.Position.Days);
            }
        }

        private void moveSlider_DoWork(object sender, DoWorkEventArgs e)
        {
            while (isplay)
            {
                System.Threading.Thread.Sleep(500);
                ((BackgroundWorker)sender).ReportProgress(0);
            }
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
            isOn = false;
            media.Stop();
            play_content.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/copy-start.png"));
        }

        private void MakeImageViewer()
        {
            Controls.Visibility = Visibility.Visible;
            //StackPanel 
            scr = new ScrollViewer();
            img = new Image();
            scr.Margin = new Thickness(2, 2, 2, 2);
            try
            { source = new BitmapImage(new Uri(path)); }
            catch (Exception)
            {
                caution.Visibility = Visibility.Visible;
                failed.Text = "Can't open this image file, make sure that the file is not too large and it is a correct image file.";
                Controls.Visibility = Visibility.Collapsed;
            }
            img.Source = source;
            scr.Content = img;
            scr.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            scr.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            img.Stretch = Stretch.None;
            scr.MouseMove += scr_MouseMove;
            scr.PreviewMouseDown += scr_MouseLeftButtonDown;
            scr.PreviewMouseUp += scr_MouseLeftButtonUp;
            scr.MouseLeave += scr_MouseLeave;
            play.IsEnabled = false;
            play_content.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/image-save.png"));
            pause_content.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/rotate-left.png"));
            stop_content.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/rotate-right.png"));
            play.Click += SaveImage_Click;
            pause.Click += RotateLeft_Click;
            stop.Click += RotateRight_Click;
            play.Margin = new Thickness(240, 10, 0, 10);
            pause.Margin = new Thickness(150, 10, 0, 10);
            stop.Margin = new Thickness(330, 10, 0, 10);
            volume.Margin = Margin = new Thickness(0, 5, 150, 5);
            worker.RunWorkerAsync();
            Slider1.Visibility = Visibility.Collapsed;
            Timer1.Visibility = Visibility.Collapsed;
            pause.Visibility = Visibility.Visible;
            volume.ValueChanged += Zoom_ValueChanged;
            viewer.Children.Insert(0, scr);
            //thread.Start();
        }

        private void Zoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //throw new NotImplementedException();
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage o = new BitmapImage(new Uri(path));
            o.Rotation = Rotation.Rotate90;
            img.Source = o;
            //source.Rotation = PhotoOrientation.Rotate90;
            //img.Source = source;
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            //source.Rotation = Rotation.Rotate90;
            //img.Source = source;
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            //PhotoOrientation.Rotate90
        }

        private void PlayMedia_Click(object sender, RoutedEventArgs e)
        {
            if (isOn)
            {
                media.Pause();
                play_content.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/copy-start.png"));
            }
            else
            {
                media.Play();
                play_content.Source = new BitmapImage(new Uri("pack://application:,,,/File Explorer;component/Images/Menu/copy-pause.png"));
            }
            isOn = !isOn;
        }

        private void volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            media.Volume = e.NewValue / 100;
        }

        private void Slider1_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (media.NaturalDuration.HasTimeSpan)
            {
                long pos = (long)Math.Floor(media.NaturalDuration.TimeSpan.Ticks * (Slider1.Value / 10));
                media.Position = new TimeSpan(pos);
            }  
        }

        private void viewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (Controls.Opacity < 0.1)
            {
                wait_for = 3500;
                mouse_move = true;
                DoubleAnimation animation = new DoubleAnimation(0, 1, new Duration(new TimeSpan(2000000)));
                Controls.BeginAnimation(Grid.OpacityProperty, animation);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (Controls.Opacity > 0.99)
            {
                wait_for = 100;
                DoubleAnimation m = new DoubleAnimation(1, 0, new Duration(new TimeSpan(10000000)));
                Controls.BeginAnimation(Grid.OpacityProperty, m);
            }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
                System.Threading.Thread.Sleep(1000);
        }

        private void MakeHtmlViewer()
        {
            Frame web = new Frame();
            web.Navigate(new Uri(path));
            viewer.Children.Insert(0, web);
        }

        private void MakeTextViewer()
        {
            RichTextBox rich = new RichTextBox();
            rich.IsReadOnly = true;
            StreamReader SR = new StreamReader(path);
            rich.Document = new FlowDocument(new Paragraph(new Run(SR.ReadToEnd())));
            SR.Close();
            rich.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            viewer.Children.Insert(0, rich);
        }

        private void scr_MouseLeave(object sender, MouseEventArgs e)
        {
            isOn = false;
            old_point = e.GetPosition(null);
        }

        private void scr_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isOn = false;
            old_point = e.GetPosition(null);
        }

        private void scr_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            old_point = e.GetPosition(null);
            isOn = true;
        }

        private void scr_MouseMove(object sender, MouseEventArgs e)
        {
            if (isOn)
            {
                Point new_point = e.GetPosition(null);
                scr.ScrollToHorizontalOffset(scr.HorizontalOffset + (old_point.X - new_point.X));
                scr.ScrollToVerticalOffset(scr.VerticalOffset + (old_point.Y - new_point.Y));
                old_point = e.GetPosition(null);
            }
        }

        private void Reverse_Click(object sender, RoutedEventArgs e)
        {
            GetAttributes();
        }

        private void GetAttributes()
        {
            normal.IsChecked = false;
            read_only.IsChecked = false;
            hidden.IsChecked = false;
            archive.IsChecked = false;
            compressed.IsChecked = false;
            encrypted.IsChecked = false;
            system.IsChecked = false;
            directory.IsChecked = false;
            not_content_indexed.IsChecked = false;
            if (file_system.Attributes.HasFlag(FileAttributes.Normal))
                normal.IsChecked = true;
            else
            {
                if (file_system.Attributes.HasFlag(FileAttributes.ReadOnly))
                    read_only.IsChecked = true;
                if (file_system.Attributes.HasFlag(FileAttributes.Hidden))
                    hidden.IsChecked = true;
                if (file_system.Attributes.HasFlag(FileAttributes.Archive))
                    archive.IsChecked = true;
                if (file_system.Attributes.HasFlag(FileAttributes.Compressed))
                    compressed.IsChecked = true;
                if (file_system.Attributes.HasFlag(FileAttributes.Encrypted))
                    encrypted.IsChecked = true;
                if (file_system.Attributes.HasFlag(FileAttributes.System))
                    system.IsChecked = true;
                if (file_system.Attributes.HasFlag(FileAttributes.Directory))
                    directory.IsChecked = true;
                if (file_system.Attributes.HasFlag(FileAttributes.NotContentIndexed))
                    not_content_indexed.IsChecked = true;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (drive == null || (drive != null && drive.IsReady))
            {
                error.Visibility = Visibility.Collapsed;
                if (name.Text != String.Empty)
                {
                    Change_Name.Visibility = Visibility.Visible;
                    string[] forbidden_letters = { "*", "/", "\\", "\"", "?", "<", ">", "|" };
                    foreach (string i in forbidden_letters)
                        if (name.Text.Contains(i))
                        {
                            name.Text = name.Text.Replace(i, "");
                            error.Text = "File name can't contain any of these characters:\n'\\' , '/' , ':' , '*' , '?' , '\"' , '<' , '>' , '|'";
                            error.Visibility = Visibility.Visible;
                        }
                }
                else
                    Change_Name.Visibility = Visibility.Hidden;
            }
            //if (name.Text.Contains("*") || name.Text.Contains("/") || name.Text.Contains("\\") || name.Text.Contains("\"") || name.Text.Contains("?") || name.Text.Contains("<") || name.Text.Contains(">") || name.Text.Contains("|"))
            //{
            //    int i = name.CaretIndex;
            //    name.Text = name.Text.Replace("*", "").Replace("/", "").Replace("\\", "").Replace("\"", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "");
            //    name.CaretIndex = i - 1;
            //    error.Visibility = Visibility.Visible;
            //}
        }

        private void SaveAttributes_Click(object sender, RoutedEventArgs e)
        {
            if (normal.IsChecked == true)
            {
                int attributes = (int)file_system.Attributes;
                for (int i = 131072; i >= 1; i /= 2)
                    if (attributes >= i)
                    {
                        attributes -= i;
                        file_system.Attributes -= i;
                    }
                file_system.Attributes += 128;
            }
            else
            {
                if (read_only.IsChecked == true && !file_system.Attributes.HasFlag(FileAttributes.ReadOnly))
                    file_system.Attributes += 1;
                else if (read_only.IsChecked == false && file_system.Attributes.HasFlag(FileAttributes.ReadOnly))
                    file_system.Attributes -= 1;
                if (hidden.IsChecked == true && !file_system.Attributes.HasFlag(FileAttributes.Hidden))
                    file_system.Attributes += 2;
                else if (hidden.IsChecked == false && file_system.Attributes.HasFlag(FileAttributes.Hidden))
                    file_system.Attributes -= 2;
                if (archive.IsChecked == true && !file_system.Attributes.HasFlag(FileAttributes.Archive))
                    file_system.Attributes += 32;
                else if (archive.IsChecked == false && file_system.Attributes.HasFlag(FileAttributes.Archive))
                    file_system.Attributes -= 32;
                if (compressed.IsChecked == true && !file_system.Attributes.HasFlag(FileAttributes.Compressed))
                    file_system.Attributes += 2048;
                else if (compressed.IsChecked == false && file_system.Attributes.HasFlag(FileAttributes.Compressed))
                    file_system.Attributes -= 2048;
                if (encrypted.IsChecked == true && !file_system.Attributes.HasFlag(FileAttributes.Encrypted))
                    file_system.Attributes += 16384;
                else if (encrypted.IsChecked == false && file_system.Attributes.HasFlag(FileAttributes.Encrypted))
                    file_system.Attributes -= 16384;
                if (system.IsChecked == true && !file_system.Attributes.HasFlag(FileAttributes.System))
                    file_system.Attributes += 4;
                else if (system.IsChecked == false && file_system.Attributes.HasFlag(FileAttributes.System))
                    file_system.Attributes -= 4;
                if (directory.IsChecked == true && !file_system.Attributes.HasFlag(FileAttributes.Directory))
                    file_system.Attributes += 16;
                else if (directory.IsChecked == false && file_system.Attributes.HasFlag(FileAttributes.Directory))
                    file_system.Attributes -= 16;
                if (not_content_indexed.IsChecked == true && !file_system.Attributes.HasFlag(FileAttributes.NotContentIndexed))
                    file_system.Attributes += 131072;
                else if (not_content_indexed.IsChecked == false && file_system.Attributes.HasFlag(FileAttributes.NotContentIndexed))
                    file_system.Attributes -= 131072;
            }
        }

        private void Attributes_Checked(object sender, RoutedEventArgs e)
        {
            reverse.Visibility = Visibility.Visible;
            other.IsChecked = true;
        }

        private void Attributes_Unchecked(object sender, RoutedEventArgs e)
        {
            if (read_only.IsChecked == false && hidden.IsChecked == false && archive.IsChecked == false && compressed.IsChecked == false && encrypted.IsChecked == false && system.IsChecked == false && directory.IsChecked == false)
                normal.IsChecked = true;
        }

        private void normal_Checked(object sender, RoutedEventArgs e)
        {
            reverse.Visibility = Visibility.Visible;
            read_only.IsChecked = false;
            hidden.IsChecked = false;
            archive.IsChecked = false;
            compressed.IsChecked = false;
            encrypted.IsChecked = false;
            system.IsChecked = false;
            directory.IsChecked = false;
        }

        private void ChangeName_Click(object sender, RoutedEventArgs e)
        {
            if (name.Text != "." /*&& !File.Exists(dir.Parent.FullName + "\\" + name.Text) && !Directory.Exists(dir.Parent.FullName + "\\" + name.Text)*/)
            {
                if (file_type == ItemType.Directory)
                    dir.MoveTo(dir.Parent.FullName + "\\" + name.Text);
                else if (file_type == ItemType.File)
                    file.MoveTo(file.Directory.FullName + "\\" + name.Text);
                error.Visibility = Visibility.Collapsed;
                Change_Name.Visibility = Visibility.Hidden;
            }
            else
            {
                error.Text = "You can not use this name!";
                error.Visibility = Visibility.Visible;
            }
        }

        private void other_Unchecked(object sender, RoutedEventArgs e)
        {
            read_only.IsEnabled = false;
            hidden.IsEnabled = false;
            archive.IsEnabled = false;
            compressed.IsEnabled = false;
            encrypted.IsEnabled = false;
            system.IsEnabled = false;
            directory.IsEnabled = false;
        }

        private void other_Checked(object sender, RoutedEventArgs e)
        {
            read_only.IsEnabled = true;
            hidden.IsEnabled = true;
            archive.IsEnabled = true;
            compressed.IsEnabled = true;
            encrypted.IsEnabled = true;
            system.IsEnabled = true;
            directory.IsEnabled = true;
        }

        private void Zoom_In(object sender, RoutedEventArgs e)
        {
            //ImageViewer.
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            Close();
            if (file_type == ItemType.Directory || (file_type == ItemType.Drive && drive.IsReady))
            {
                MainWindow.go_to_path = true;
                MainWindow.to_go_path = path;
            }
            else if (file_type == ItemType.Drive && !drive.IsReady)
                MessageBox.Show("Drive is not ready", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
            else if (file_type == ItemType.File)
                System.Diagnostics.Process.Start(new ProcessStartInfo(path));
        }

        private void OpenWith_Click(object sender, RoutedEventArgs e)
        {
            Functions.OpenWith(path, file_type);
        }

        private void ExtractArchive_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog FBD = new System.Windows.Forms.FolderBrowserDialog();
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ProcessStartInfo PSI;
                Dictionary<int, string> exit_code = new Dictionary<int, string>(6);
                if (file.Extension.ToLower() == ".uha")
                {
                    PSI = new ProcessStartInfo("Tools\\UHARC.exe", " x \"" + path + "\" \"" + FBD.SelectedPath + "\"");
                    exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                }
                else if (file.Extension.ToLower() == ".kgb")
                {
                    PSI = new ProcessStartInfo("Tools\\KGB.exe", " x \"" + path + "\" \"" + FBD.SelectedPath + "\"");
                    exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                }
                else if (file.Extension.ToLower() == ".mht" || file.Extension.ToLower() == ".mhtml")
                {
                    PSI = new ProcessStartInfo("Tools\\extractMHT.exe", " \"" + path + "\" \"" + FBD.SelectedPath + "\"");
                    exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                }
                else if (file.Extension.ToLower() == ".mou")
                {
                    PSI = new ProcessStartInfo("Tools\\WinMount\\winmount", " -e \"" + path + "\" -s:\"" + FBD.SelectedPath + "\"");
                    exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                }
                else
                {
                    PSI = new ProcessStartInfo("Tools\\7z.exe", " x \"" + path + "\" -o\"" + FBD.SelectedPath + "\"");
                    exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                }
                try
                {
                    Process process = System.Diagnostics.Process.Start(PSI);
                    process.WaitForExit(int.MaxValue);
                    exit_code = new Dictionary<int, string>(6);
                    MessageBox.Show(exit_code[process.ExitCode]);
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); Close(); }
            }
        }

        private void ExplorerArchive_Click(object sender, RoutedEventArgs e)
        {
            if (file.Extension.ToLower() == ".uha")
                Process.Start(new ProcessStartInfo("Tools\\WinUha.exe", "\"" + file.FullName + "\""));
            else if (file.Extension.ToLower() == ".kgb")
                Process.Start(new ProcessStartInfo("Tools\\KGB.exe", "\"" + file.FullName + "\""));
            else
                Process.Start(new ProcessStartInfo("Tools\\7zFM.exe", "\"" + file.FullName + "\""));
        }

        private void RunAsAdministrator_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo PSI = new ProcessStartInfo(path);
            PSI.Verb = "runas";
            try
            { System.Diagnostics.Process.Start(PSI); }
            catch (Exception err)
            { MessageBox.Show(err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void MoveTo_Click(object sender, RoutedEventArgs e)
        {
            string copy_to = new Folder_Picker("Select directory to copy to").ShowThisDialog();
            if (copy_to != null)
                CollectFiles(true, copy_to);
        }

        private void CopyTo_Click(object sender, RoutedEventArgs e)
        {
            string copy_to = new Folder_Picker("Select directory to copy to").ShowThisDialog();
            if (copy_to != null)
                CollectFiles(false, copy_to);
        }

        private void CollectFiles(bool remove,string copy_to)
        {
            List<FileElement> to_copy = new List<FileElement>();
            if (file_type == ItemType.File)
                to_copy.Add(new FileElement(file.FullName, file.Length, file.FullName.Remove(0, file.DirectoryName.Length - 1), remove));
            else if (file_type == ItemType.Directory)
                Functions.AddAllFiles(dir.FullName, dir.Parent.FullName, remove, to_copy);
            else if (file_type == ItemType.Drive)
                Functions.AddAllFiles(drive.Name, "", remove, to_copy);
            new File_Operation(to_copy).Show(copy_to);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you realy want to permanently delete '" + path + "':", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                if (file_type == ItemType.Directory)
                    dir.Delete(true);
                else
                    file.Delete();
        }

        private void InstallFont_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo PSI = new ProcessStartInfo("Tools\\InstallFonts.exe", file.FullName);
                PSI.Verb = "runas";
                var P = Process.Start(PSI);
                P.WaitForExit(int.MaxValue);
                if (P.ExitCode == 1)
                    MessageBox.Show("Done.", "Install Fonts", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Something wrong while installing font.", "Install Fonts", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        


        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            //Encoding encode;
            //Text_Encoding(out encode);
            //StreamReader SR = new StreamReader(file.FullName, encode);
            //TextViewer.Document = Document(SR.ReadToEnd(), file.Extension);
            //SR.Close();
        }

        private void Text_Encoding(out Encoding encode)
        {
            //if (TextEncoding.SelectedIndex == 0)
            //    encode = Encoding.ASCII;
            //else if (TextEncoding.SelectedIndex == 1)
            //    encode = Encoding.BigEndianUnicode;
            //else if (TextEncoding.SelectedIndex == 2)
            //    encode = Encoding.Unicode;
            //else if (TextEncoding.SelectedIndex == 3)
            //    encode = Encoding.UTF32;
            //else if (TextEncoding.SelectedIndex == 4)
            //    encode = Encoding.UTF7;
            //else
            encode = Encoding.UTF8;
        }

        private void Save_Text(object sender, RoutedEventArgs e)
        {
            //Encoding encode;
            //Text_Encoding(out encode);
            //StreamWriter SW = new StreamWriter(file.FullName, false, encode);
            //SW.Write(TextViewer.Document);
            //SW.Close();
        }

        private void Text_Editing(object sender, RoutedEventArgs e)
        {
            //Process.Start(new ProcessStartInfo("Tools\\notepad++.exe", file.FullName));
        }

        private void Mute_Media(object sender, RoutedEventArgs e)
        {
            //MediaViewer.IsMuted = !MediaViewer.IsMuted;
        }

        private void Video_Scroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //MediaViewer.Position = new TimeSpan((long)(MediaSlider.Value / 1000000));
        }

        private void Pause_Media(object sender, RoutedEventArgs e)
        {
            //if (isOn)
            //    MediaViewer.Pause();
            //else
            //    MediaViewer.Play();
            //isOn = !isOn;
        }

        private void Open_File(object sender, RoutedEventArgs e)
        {
            //if (file_type == ItemType.File)
            //{
            //    if (MainWindow.IsArchive(file.Extension))
            //    {
            //        ProcessStartInfo PSI = new ProcessStartInfo("Tools\\7zFM.exe", "\"" + file.FullName + "\"");
            //        System.Diagnostics.Process.Start(PSI);
            //    }
            //    else
            //    {
            //        ProcessStartInfo PSI = new ProcessStartInfo(file.FullName);
            //        System.Diagnostics.Process.Start(PSI);
            //    }
            //}
            //else if (file_type == ItemType.Directory)
            //{
            //    MainWindow.go_to_path = true;
            //    MainWindow.to_go_path = directory.FullName;
            //}
            //else if (file_type == null)
            //{
            //    MainWindow.go_to_path = true;
            //    MainWindow.to_go_path = drive.Name;
            //}
            //this.Close();
        }

        private string Archive_Type(string Extintion)
        {
            //if (file.Extension.Length > 0 && file.Extension[file.Extension.Length - 1] == '_')
            //    return "Cabinet Archive";
            //switch (Extintion.ToLower())
            //{
            //    case ".rar":
            //        return "WinRar Archive";
            //    case ".7z":
            //        return "7zip Archive";
            //    case ".ace":
            //        return "WinAce Archive";
            //    case ".mou":
            //        return "WinMount Archive";
            //    case ".cab":
            //        return "Cabinet Archive";
            //    default:
            return "(" + Extintion.ToUpper() + ") Archive";
            //}
        }

        private void Extract(object sender, RoutedEventArgs e)
        {
            //System.Windows.Forms.FolderBrowserDialog FBD = new System.Windows.Forms.FolderBrowserDialog();
            //if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            //{
            //    ProcessStartInfo PSI = new ProcessStartInfo("Tools\\7z.exe", " x \"" + file.FullName + "\" -o\"" + FBD.SelectedPath + "\"");
            //    System.Diagnostics.Process.Start(PSI);
            //}
        }

        private void Window_KeyUp_1(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //if (e.Key.Equals(Key.Escape))
            //{
            //    MediaViewer.Source = null;
            //    WebPageViewer.Source = null;
            //    this.Close();
            //}
        }

        private FlowDocument Document(string document, string extension)
        {
            ////char[] separator = { ';' };
            FlowDocument doc = new FlowDocument();
            ////if (extension.Equals(".cs"))
            ////{
            ////    var instructions = document.Split(';');
            ////    for (int i = 0; i < instructions.Length; i++)
            ////    {
            ////        var words = instructions[i].Split(' ');
            ////        for (int j = 0; j < words.Length; j++)
            ////        {
            ////            Run block = new Run(words[j]);
            ////            if (Contains(words[j], blue))
            ////                block.Foreground = Brushes.Blue;
            ////            else if (Contains(words[j], cyan))
            ////                block.Foreground = Brushes.Cyan;
            ////            Paragraph par = new Paragraph(block);
            ////            doc.Blocks.Add(par);
            ////            //int i1 = words[i].IndexOf("\"");
            ////            //int i2 = words[i].IndexOf("\"", words[i].IndexOf("\""));
            ////        }
            ////    }
            ////}
            ////else
            ////{
            //text_ = document;
            //System.Windows.Documents.Run block = new System.Windows.Documents.Run(document);
            //System.Windows.Documents.Paragraph par = new System.Windows.Documents.Paragraph(block);
            //doc.Blocks.Add(par);
            ////}
            return doc;
        }

        private bool Contains(string value, string[] In)
        {
            foreach (var i in In)
                if (i.Equals(value))
                    return true;
            return false;
        }

        private void Search_Text_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            ////if (text_ != null)
            ////{
            //    Search_Text.Background = Brushes.White;
            //    if (!txt.Contains(Search_Text.Text))
            //        Search_Text.Background = Brushes.Crimson;
            //    var runs = paragraph.Inlines.ToList<Inline>();
            //    for (int i = 0; i < runs.Count; i++)
            //        runs[i].Background = Brushes.Transparent;
            //    for (int i = 0; i < runs.Count - Search_Text.Text.Length + 1; i++)
            //    {
            //        string str = string.Empty;
            //        int j;
            //        for (j = i; j < i + Search_Text.Text.Length; j++)
            //            str = str + ((Run)(runs[j])).Text;
            //        if (str == Search_Text.Text)
            //            for (j = i; j < i + Search_Text.Text.Length; j++)
            //                runs[j].Background = Brushes.LimeGreen;
            //    }
            //    paragraph.Inlines.Clear();
            //    foreach (var i in runs)
            //        paragraph.Inlines.Add(i);
            ////}
        }

        private void SlidTraker()
        {
            //while (isOn)
            //    Sett(MediaViewer.Position.Ticks);
        }

        private void Sett(long n)
        {
            //MediaSlider.Value = n;
        }

        private void v(object sender, MouseButtonEventArgs e)
        {
            //MediaViewer.Position = new TimeSpan((long)MediaSlider.Value * 1000000);
        }

        private void MediaViewer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            //Error.Text = "Can't open this media file, make sure that you install all codec required and the file is a correct media file.";
            //MediaControl.Visibility = Visibility.Collapsed;
        }

        private void Rotate_Right(object sender, RoutedEventArgs e)
        {
            //RotateTransform m_transform;
            //m_userRotation = Add90DegreesCW(m_userRotation);
            //UpdateImageRotation(m_userRotation, out m_transform);
            ////ImageViewer.
        }

        private void Slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void Grid_MouseOver(object sender, MouseEventArgs e)
        {
            ((Grid)sender).Background = new SolidColorBrush(Color.FromArgb(255, 65, 64, 59));
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Grid)sender).Background = null;
            ((Grid)sender).Opacity = 1;
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).Background = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).Background = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));
        }

        private void ShowMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            if (main_menu.Width < 4)
            {
                DoubleAnimation animation2 = new DoubleAnimation(0, 64, new Duration(new TimeSpan(1000000)));
                main_menu.BeginAnimation(Grid.WidthProperty, animation2);
                Thread.Sleep(100);
                DoubleAnimation animation1 = new DoubleAnimation(0, 1, new Duration(new TimeSpan(2500000)));
                main_menu_stack.BeginAnimation(StackPanel.OpacityProperty, animation1);
            }
        }

        private void HideMenu_MouseLeave(object sender, MouseEventArgs e)
        {
            if (main_menu.Width > 60)
            {
                DoubleAnimation animation1 = new DoubleAnimation(1, 0, new Duration(new TimeSpan(1000000)));
                main_menu_stack.BeginAnimation(StackPanel.OpacityProperty, animation1);
                Thread.Sleep(100);
                DoubleAnimation animation2 = new DoubleAnimation(64, 0, new Duration(new TimeSpan(2500000)));
                main_menu.BeginAnimation(Grid.WidthProperty, animation2);
            }
        }

        private void Grid1_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Ellipse)((Grid)((Button)sender).Content).Children[0]).Fill = null;
            ((Ellipse)((Grid)((Button)sender).Content).Children[0]).Opacity = 1;
        }

        private void Grid1_MouseOver(object sender, MouseEventArgs e)
        {
            ((Ellipse)((Grid)((Button)sender).Content).Children[0]).Fill = Brushes.SkyBlue;
            ((Ellipse)((Grid)((Button)sender).Content).Children[0]).Opacity = 0.85;
        }
    }
}

//else
//    if (file_type == ItemType.File)
//    {
//        FileName.Text = file.FullName;
//        if (MainWindow.IsImage(file.Extension))
//        {
//            ImageElement.Visibility = Visibility.Visible;
//            ImageViewer.Source = new BitmapImage(new Uri(file.FullName));
//        }
//        else if (MainWindow.IsVedio(file.Extension) || MainWindow.IsAudio(file.Extension))
//        {
//            MediaElement.Visibility = Visibility.Visible;
//            MediaViewer.Source = new Uri(file.FullName);
//            MediaViewer.Play();
//            isOn = true;
//            if (MediaViewer.NaturalDuration.HasTimeSpan)
//                MediaSlider.Maximum = MediaViewer.NaturalDuration.TimeSpan.Ticks / 1000000;
//            Thread t1 = new Thread(SlidTraker);
//            ThreadStart bb = new ThreadStart(SlidTraker);
//            t1.Start(MediaSlider);
//            MediaViewer.Position = new TimeSpan((long)MediaSlider.Value);
//        }
//        else if (MainWindow.IsHtml(file.Extension))
//        {
//            WebPageViewer.Visibility = Visibility.Visible;
//            WebPageViewer.Source = new Uri(file.FullName);
//            Grid1.Background = null;
//        }
//        else if (MainWindow.IsText(file.Extension))
//        {
//            Text.Visibility = Visibility.Visible;
//            StreamReader SR = new StreamReader(file.FullName, Encoding.Default);
//            txt = SR.ReadToEnd();
//            SR.Close();
//            paragraph = new Paragraph();
//            foreach (var i in txt)
//            {
//                Run run = new Run(i.ToString());
//                paragraph.Inlines.Add(run);
//            }
//            TextViewer.Document = new FlowDocument(paragraph);
//            if (file.IsReadOnly)
//                EditText.IsEnabled = false;
//        }
//        else if (MainWindow.IsArchive(file.Extension))
//        {
//            Archive.Visibility = Visibility.Visible;
//            ArchiveImage.Source = new BitmapImage(new Uri(MainWindow.GetImageSource(imagesource, file.Extension, true)));
//            ArchiveName.Text = "Name: " + file.Name;
//            ArchiveSize.Text = "Size: " + MainWindow.File_Size(file.Length);
//            ArchiveType.Text = "Type: " + Archive_Type(file.Extension);
//        }
//        else if (file.Extension.ToLower().Equals(".docx") || file.Extension.ToLower().Equals(".xps") || file.Extension.ToLower().Equals(".odt"))
//        {
//            Docx Viewer
//            XpsDocument Doc;
//            ComponentInfo.SetLicense("FREE-LIMITED-KEY");
//            DocumentModel DM = new DocumentModel();
//            var section = new GemBox.Document.Section(DM);
//            DM.Sections.Add(section);
//            var paragraph = new GemBox.Document.Paragraph(DM);
//            section.Blocks.Add(paragraph);
//            var run = new GemBox.Document.Run(DM,"Hello Word");
//            paragraph.Inlines.Add(run);
//            Doc = DM.ConvertToXpsDocument(SaveOptions.XpsDefault);
//            XPSViewer.Document = Doc.GetFixedDocumentSequence();
            

//            BinaryReader n=new BinaryReader(new FileStream(file.FullName,FileMode.Open));
//            DocumentModel DM = new DocumentModel();
//            DM.ConvertToXpsDocument(SaveOptions.XpsDefault);

//            XpsDocument XPS = new XpsDocument(file.FullName, FileAccess.Read);
//            XPSViewer.Document = XPS.GetFixedDocumentSequence();
//            XPSViewer.Visibility = Visibility.Visible;


//            StreamReader SR = new StreamReader(file.FullName, Encoding.ASCII);
//            TextViewer.Document = ((Block)SR.ReadToEnd());
//            if (file.IsReadOnly)
//            EditText.IsEnabled = false;
//        }
//        else if (MainWindow.IsFont(file.Extension))
//        {
//            FileName.Text = file.FullName;
//            FontFamily This_Font = new System.Windows.Media.FontFamily(new Uri("file://" + file.FullName), "Name");
//            Font_Viewer.Visibility = Visibility.Visible;
//            ListViewItem temp = new ListViewItem();
//            temp.FontSize = 14;
//            temp.Content = "Font Name: " + This_Font.ToString();
//            temp.Foreground = Brushes.WhiteSmoke;
//            Font_List.Items.Add(temp);

//            temp = new ListViewItem();
//            temp.Content = "__________________________________________________________";
//            temp.Foreground = Brushes.WhiteSmoke;
//            Font_List.Items.Add(temp);

//            temp = new ListViewItem();
//            temp.FontSize = 18;
//            temp.FontFamily = This_Font;
//            temp.Foreground = Brushes.WhiteSmoke;
//            temp.Content = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ";
//            Font_List.Items.Add(temp);

//            temp = new ListViewItem();
//            temp.FontSize = 18;
//            temp.FontFamily = This_Font;
//            temp.Foreground = Brushes.WhiteSmoke;
//            temp.Content = "0123456789.:,;(!?)+-*/=";
//            Font_List.Items.Add(temp);

//            temp = new ListViewItem();
//            temp.FontSize = 18;
//            temp.FontFamily = This_Font;
//            temp.Foreground = Brushes.WhiteSmoke;
//            temp.Content = "أبجد هوز حطي كلمن سعفص قرشت ثخذ ضظغ";
//            Font_List.Items.Add(temp);

//            temp = new ListViewItem();
//            temp.Content = "__________________________________________________________";
//            temp.Foreground = Brushes.WhiteSmoke;
//            Font_List.Items.Add(temp);

//            Font_List.Items.Add("ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz");
//            Font_List.Items.Add("0123456789.:,;(!?)+-*/=");
//            Font_List.Items.Add("أبجد هوز حطي كلمن سعفص قرشت ثخذ ضظغ");

//            for (int i = 8; i < 73; i += 8)
//            {
//                temp = new ListViewItem();
//                temp.FontFamily = This_Font;
//                temp.Foreground = Brushes.WhiteSmoke;
//                temp.Content = i + "-  The Quick brown fox jumps over the lazy dog. 0123456789" + "\t" + "أنا غصة في حلق من عاداني" + "\t" + "أنا تمرة الأحباب حنظلة العدى";
//                temp.FontSize = i;
//                Font_List.Items.Add(temp);
//            }
//        }
//        else if (false)
//        {
//            System.Windows.Xps;
//        }
//        else
//        {
//            if (file.Extension.ToLower().Equals(".msi"))
//                OpenUnknoun.Content = "Install Package";
//            else if (file.Extension.ToLower().Equals(".exe"))
//                OpenUnknoun.Content = "Run Application";
//            Unknown.Visibility = Visibility.Visible;
//            ItemImage.Source = new BitmapImage(new Uri(MainWindow.GetImageSource(imagesource, file.Extension, true)));
//            name.Text = "Name: " + file.Name;
//            size.Text = "Size: " + MainWindow.File_Size(file.Length);
//            type.Text = "Type: " + file.Extension + " (file)";
//            created.Text = "Data Created: " + file.CreationTime.ToString();
//            modified.Text = "Data Modified: " + file.LastWriteTime.ToString();
//            Accessed.Text = "Last Access: " + file.LastAccessTime.ToString();
//        }
//    }
//    else if (file_type == ItemType.Directory)
//    {
//        OpenUnknoun.Content = "Open";
//        FileName.Text = directory.FullName;
//        Unknown.Visibility = Visibility.Visible;
//        ItemImage.Source = new BitmapImage(new Uri(MainWindow.GetImageSource(imagesource, "Folder", true)));
//        name.Text = "Name: " + directory.Name;
//        size.Text = "Contains " + (sub_dir_count + file_count) + " Items (" + sub_dir_count + " Directories & " + file_count + " Files)";
//        type.Text = "Type: Directory";
//        created.Text = "Data Created: " + directory.CreationTime.ToString();
//        modified.Text = "Data Modified: " + directory.LastWriteTime.ToString();
//        Accessed.Text = "Last Access: " + directory.LastAccessTime.ToString();
//    }
//    else
//    {
//        FileName.Text = drive.Name;
//        Unknown.Visibility = Visibility.Visible;
//        ItemImage.Source = new BitmapImage(new Uri(MainWindow.GetImageSource(imagesource, drive.DriveType.ToString(), drive.IsReady)));
//        size.Text = "Contains " + (sub_dir_count + file_count) + " Items (" + sub_dir_count + " Directories & " + file_count + " Files)";
//        if (drive.IsReady)
//        {
//            name.Text = "Name: " + drive.VolumeLabel + " (" + drive.Name + ")";
//            type.Text = "File System: " + drive.DriveFormat.ToString();
//            created.Text = "Total Size: " + MainWindow.File_Size(drive.TotalSize);
//            modified.Text = "Free Space: " + MainWindow.File_Size(drive.AvailableFreeSpace);
//            driveusage.Visibility = Visibility.Visible;
//            driveusage.Value = 100 - (drive.AvailableFreeSpace * 100 / drive.TotalSize);
//            OpenUnknoun.Content = "Open";
//        }
//        else
//        {
//            name.Text = "Name: " + drive.Name;
//            type.Text = "Type: " + drive.DriveType;
//            created.Text = "Drive is not Ready";
//            modified.Text = "";
//            OpenUnknoun.Visibility = Visibility.Collapsed;
//        }
//        Accessed.Visibility = Visibility.Collapsed;
//    }