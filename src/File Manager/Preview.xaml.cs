using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;
using System.Data.SqlClient;
using System.Configuration;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Preview.xaml
    /// </summary>
    public partial class Preview : Window
    {
        bool CancelEveryThinig = false;
        DriveInfo dri, directory_root;
        FileInfo fi;
        DirectoryInfo di;
        ItemType type;
        int main_menu_item_count = 7;
        List<FileInfo> neighbors;
        int current_item_in_viewer = 0, clusterSize;
        List<BackgroundWorker> background_workers = new List<BackgroundWorker>();

        public Preview()
        {
            InitializeComponent();
        }

        #region Start Window
        BackgroundWorker calc_directory_size;

        internal void ShowDialog(object Item, ItemType type)
        {
            this.type = type;
            switch (type)
            {
                case ItemType.Drive:
                    dri = (DriveInfo)Item;
                    InitializeForDrive(dri);
                    ShowDialog();
                    break;
                case ItemType.Directory:
                    di = (DirectoryInfo)Item;
                    InitializeForDirectory(di);
                    ShowDialog();
                    break;
                case ItemType.File:
                    fi = (FileInfo)Item;
                    InitializeForFile(fi);
                    if (start_with_hash)
                    {
                        TabItem_MouseLeftButtonUp(hash_tab, null);
                        Loaded += (object sender, RoutedEventArgs e) => { HashCalculate_Click(null, null); };
                    }
                    controler.DoWork += controler_DoWork;
                    controler.WorkerReportsProgress = true;
                    controler.ProgressChanged += controler_ProgressChanged;
                    ShowDialog();
                    break;
                default:
                    break;
            }
        }

        private void PrimeInitialize(FileSystemInfo file)
        {
            Title += " '" + file.FullName + "'";
            item_path.Text = file.FullName;
            rename_textbox.Text = file.Name;
            item_name.Text = "Name: " + file.Name;
            item_type.Text = file.Extension;
            creation_time.Text = "Creation: " + file.CreationTime.ToString();
            modified_time.Text = "Modified: " + file.LastWriteTime.ToString();
            last_accessed_time.Text = "Last accessed: " + file.LastAccessTime.ToString();
            //Create flowshart;
            GetAttributes(file);
            reverse.Visibility = Visibility.Collapsed;
            FillMoreInformation(file);
        }

        private void GetAttributes(FileSystemInfo f)
        {
            normal.IsChecked = read_only.IsChecked = hidden.IsChecked = archive.IsChecked = compressed.IsChecked = encrypted.IsChecked = system.IsChecked = not_content_indexed.IsChecked = false;
            if (f.Attributes.HasFlag(FileAttributes.Normal) || (f is DirectoryInfo && ((int)f.Attributes == 8208)))
                normal.IsChecked = true;
            else
            {
                other.IsChecked = true;
                if (f.Attributes.HasFlag(FileAttributes.ReadOnly))
                    read_only.IsChecked = true;
                if (f.Attributes.HasFlag(FileAttributes.Hidden))
                    hidden.IsChecked = true;
                if (f.Attributes.HasFlag(FileAttributes.Archive))
                    archive.IsChecked = true;
                if (f.Attributes.HasFlag(FileAttributes.Compressed))
                    compressed.IsChecked = true;
                if (f.Attributes.HasFlag(FileAttributes.Encrypted))
                    encrypted.IsChecked = true;
                if (encrypted.IsChecked == false && compressed.IsChecked == false)
                    not_encrypted_compressed.IsChecked = true;
                if (f.Attributes.HasFlag(FileAttributes.System))
                    system.IsChecked = true;
                if (f.Attributes.HasFlag(FileAttributes.NotContentIndexed))
                    not_content_indexed.IsChecked = true;
            }
            loaded_attributes = SelectedAttributes();
        }

        bool?[] loaded_attributes;

        void RefreshAttributes()
        {
            try
            {
                if (fi != null)
                    GetAttributes(new FileInfo(fi.FullName));
                else
                    GetAttributes(new DirectoryInfo(di == null ? dri.Name : di.FullName));
                reverse.Visibility = Visibility.Collapsed;
            }
            catch (Exception) { }
        }

        #region File Region

        private void InitializeForFile(FileInfo file)
        {
            //Information Tab
            cloud_only = IsCloudOnly(file);
            PrimeInitialize(file);
            string extension = file.Extension.ToLower();
            Dispatcher.BeginInvoke(new Action(() => { FillApps(extension); }), System.Windows.Threading.DispatcherPriority.Background);
            item_location.Text = "Path: " + file.DirectoryName;
            if (FileInfoCombonents.FilesInformation.ContainsKey(file.Extension.ToLower()))
            {
                item_type_discriotion.Text = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Description;
                item_category.Text = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category.ToString();
            }
            else
            {
                item_type_discriotion.Text = file.Extension.ToUpper() + " File";
                item_category.Text = "UnKnown";
            }
            item_size.Text = Functions.FileSize(file.Length, 2);
            item_ondisk_size.Text = "on disk: " + Functions.FileSize(SizeOnDisk(file), 2);
            directory_content.Text = "This is a file.";
            item_image.Source = Functions.GetFileImage256(file.Extension);
            if (file.Extension.ToLower() == ".enf" || file.Extension.ToLower() == ".enp")
            {
                List<string> names = Operation.ReadPlainNames(file.FullName);
                if (names != null)
                    directory_content.Text = "Encrypted " + (names.Count > 1 ? "package of " + names.Count + " files: " : "file, original name: ") + string.Join(", ", names.Take(5).ToArray()) + (names.Count > 5 ? ", ..." : "");
                else
                    directory_content.Text = "Encrypted file, its name is encrypted too.";
            }

            //Atribute Tab
            apply_to_all.Visibility = Visibility.Collapsed;

            //Main menu and Tabs
            if ((!FileInfoCombonents.FilesInformation.ContainsKey(file.Extension.ToLower())) || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Unknown || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.CannotOpen)
            {
                PreviewWithShell(file);
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 89, 0, 0);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Application)
            {
                PreviewWithShell(file);
                if (file.Extension.ToLower().Equals(".msi"))
                {
                    open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/install-package.png"));
                    open_button.ToolTip = "Install package";
                    explore_button.Visibility = Visibility.Collapsed;
                    open_button.Margin = new Thickness(0, 89, 0, 0);
                }
                else
                {
                    if (file.Exists)
                        try
                        {
                            open_button_image.Source = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.Icon.ExtractAssociatedIcon(file.FullName).Handle, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        }
                        catch (Exception) { open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/run-application.png")); }
                    else
                        open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/run-application.png"));
                    explore_button.ToolTip = "Run as administrator";
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/run-as-adimstrator.png"));
                    open_button.Margin = new Thickness(0, 57, 0, 0);
                    explore_button.Click += RunAsAdministratorButton_Click;
                }
                extract_button.Visibility = Visibility.Collapsed;
                //after mack an antivirous depend it scan veiw it or not
                //caution.Visibility = Visibility.Visible; 
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Archive)
            {
                PreviewWithShell(file);                    //may then remove it to standalone condition to explore archive file
                explore_button.ToolTip = "Explorer archive";                         //may then remove it to standalone condition to explore archive file
                explore_button.Click += ExplorerArchiveButton_Click;
                explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/explore-archive.png"));
                extract_button.ToolTip = "Extract files from archive";
                extract_button.Click += ExtractArchiveButton_Click;
                extract_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/extract-archive.png"));
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Document)
            {
                if (file.Extension.ToLower() == ".rtf")
                    PreviewWith(() => { MakeRTFViewer(file); });
                else if (file.Extension.ToLower() == ".xps" || file.Extension.ToLower() == ".oxps")
                    PreviewWith(() => { MakeXPSViewer(file); });
                else
                    PreviewWithShell(file);
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 89, 0, 0);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Text || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.TextCanRunAsApplication || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Html)
            {
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 57, 0, 0);
                if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Html)
                    PreviewWith(() => { MakeHtmlViewer(file); });
                else
                    PreviewWith(() => { MakeTextViewer(file); });
                {
                    explore_button.ToolTip = "Edit";
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/document-edit.png"));
                    explore_button.Click += EditButton_Click;
                }
                if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.TextCanRunAsApplication)
                {
                    open_button.ToolTip = "Run commands";
                    open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/run-command.png"));
                    //antivirus else show be carfull using these files
                    caution.Visibility = Visibility.Visible;

                    extract_button.ToolTip = "Run as administrator";
                    extract_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/run-as-adimstrator.png"));
                    extract_button.Click += RunAsAdministratorButton_Click;
                    extract_button.Visibility = Visibility.Visible;
                    open_button.Margin = new Thickness(0, 05, 0, 0);
                }
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Audio || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Vedio || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Image)
            {
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 89, 0, 0);
                if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Image)
                    PreviewWith(() => { MakeImageViewer(file); });
                else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Vedio)
                    PreviewWith(() => { MakeVedioViewer(file); });
                else
                    PreviewWith(() => { MakeAudioViewer(file); });
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Font)
            {
                PreviewWith(() => { MakeFontViewer(file); });
                explore_button.ToolTip = "Install font";
                explore_button.Click += InstallFontButton_Click;
                explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/font-install.png"));
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 57, 0, 0);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.DiskImage)
            {
                PreviewWithShell(file);
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 57, 0, 0);
                if (file.Extension.ToLower() == ".isz")
                {
                    explore_button.ToolTip = "Convert to ISO is not available: it needs 'unisz.exe', which has no official installer or download.";
                    ToolTipService.SetShowOnDisabled(explore_button, true);
                    explore_button.IsEnabled = false;
                    explore_button.Opacity = 0.5;
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/covert-to-iso.png"));
                }
                else if (file.Extension.ToLower() == ".iso" || file.Extension.ToLower() == ".img" || file.Extension.ToLower() == ".vhd" || file.Extension.ToLower() == ".wim")
                {
                    explore_button.ToolTip = "Explorer archive";
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/explore-archive.png"));
                    explore_button.Click += ExplorerArchiveButton_Click;
                }
                else
                {
                    explore_button.Visibility = Visibility.Collapsed;
                    open_button.Margin = new Thickness(0, 89, 0, 0);
                }
            }
            main_menu_item_count = main_menu_stack.Children.Count;
        }

        #region Audio Section

        bool is_on = true, allow_move_slider = false, is_play = false;
        FileInfo audio_file;
        Point slider_old_point;
        Thickness from_animation = new Thickness(180, 0, 0, 0);
        Thickness to_animation = new Thickness(0, 0, 0, 0);

        private void MakeAudioViewer(FileInfo file)
        {
            audio_file = file;
            audio_player.Visibility = Visibility.Visible;
            audio_player_element.Source = new Uri(file.FullName);
            audio_player_element.LoadedBehavior = MediaState.Manual;
            audio_player_element.Play();
            BackgroundWorker slider_mover = new BackgroundWorker();
            slider_mover.WorkerReportsProgress = slider_mover.WorkerSupportsCancellation = true;
            slider_mover.DoWork += AudioSliderMover_DoWork;
            slider_mover.ProgressChanged += AudioSliderMover_ProgressChanged;
            background_workers.Add(slider_mover);
            slider_mover.RunWorkerAsync();
        }

        private void AudioSliderMover_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!allow_move_slider && audio_player_element.NaturalDuration.HasTimeSpan)
            {
                slider_mover_element.Margin = new Thickness((slider_background.ActualWidth - 10) * (audio_player_element.Position.TotalSeconds / audio_player_element.NaturalDuration.TimeSpan.TotalSeconds), 0, 0, 0);
                slider_forground.Width = (slider_background.ActualWidth - 2) * (audio_player_element.Position.TotalSeconds / audio_player_element.NaturalDuration.TimeSpan.TotalSeconds);
            }
            if (audio_player_element.Position.Days > 0)
                audio_current_position_time.Text = audio_player_element.Position.Days + (audio_player_element.Position.Days == 1 ? " day  " : " days  ") + audio_player_element.Position.Hours + ":" + (audio_player_element.Position.Minutes < 10 ? ("0" + audio_player_element.Position.Minutes) : audio_player_element.Position.Minutes.ToString()) + ":" + (audio_player_element.Position.Seconds < 10 ? ("0" + audio_player_element.Position.Seconds) : audio_player_element.Position.Seconds.ToString());
            else if (audio_player_element.Position.Hours > 0)
                audio_current_position_time.Text = audio_player_element.Position.Hours + ":" + (audio_player_element.Position.Minutes < 10 ? ("0" + audio_player_element.Position.Minutes) : audio_player_element.Position.Minutes.ToString()) + ":" + (audio_player_element.Position.Seconds < 10 ? ("0" + audio_player_element.Position.Seconds) : audio_player_element.Position.Seconds.ToString());
            else
                audio_current_position_time.Text = (audio_player_element.Position.Minutes < 10 ? ("0" + audio_player_element.Position.Minutes) : audio_player_element.Position.Minutes.ToString()) + ":" + (audio_player_element.Position.Seconds < 10 ? ("0" + audio_player_element.Position.Seconds) : audio_player_element.Position.Seconds.ToString());
        }

        private void AudioSliderMover_DoWork(object sender, DoWorkEventArgs e)
        {
            while (is_on)
            {
                System.Threading.Thread.Sleep(100);
                ((BackgroundWorker)sender).ReportProgress(0);
            }
        }

        private void AudioPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            audio_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-pause.png"));
            audio_paly_button_image.ToolTip = "Pause";
            is_play = true;
            audio_play_list.SelectedIndex = current_item_in_viewer;
            audio_current_position_time.Text = "00:00";
            if (neighbors != null && neighbors.Count > current_item_in_viewer)
                playing_file_name.Text = neighbors[current_item_in_viewer].Name.Remove(neighbors[current_item_in_viewer].Name.Length - neighbors[current_item_in_viewer].Extension.Length);//name of song
            else
                playing_file_name.Text = audio_file.Name.Remove(audio_file.Name.Length - audio_file.Extension.Length);
            if (audio_player_element.NaturalDuration.HasTimeSpan)
                if (audio_player_element.NaturalDuration.TimeSpan.Days > 0)
                    audio_total_time.Text = audio_player_element.NaturalDuration.TimeSpan.Days + (audio_player_element.NaturalDuration.TimeSpan.Days == 1 ? " day  " : " days  ") + audio_player_element.NaturalDuration.TimeSpan.Hours + ":" + (audio_player_element.NaturalDuration.TimeSpan.Minutes < 10 ? ("0" + audio_player_element.NaturalDuration.TimeSpan.Minutes) : audio_player_element.NaturalDuration.TimeSpan.Minutes.ToString()) + ":" + (audio_player_element.NaturalDuration.TimeSpan.Seconds < 10 ? ("0" + audio_player_element.NaturalDuration.TimeSpan.Seconds) : audio_player_element.NaturalDuration.TimeSpan.Seconds.ToString());
                else if (audio_player_element.NaturalDuration.TimeSpan.Hours > 0)
                    audio_total_time.Text = audio_player_element.NaturalDuration.TimeSpan.Hours + ":" + (audio_player_element.NaturalDuration.TimeSpan.Minutes < 10 ? ("0" + audio_player_element.NaturalDuration.TimeSpan.Minutes) : audio_player_element.NaturalDuration.TimeSpan.Minutes.ToString()) + ":" + (audio_player_element.NaturalDuration.TimeSpan.Seconds < 10 ? ("0" + audio_player_element.NaturalDuration.TimeSpan.Seconds) : audio_player_element.NaturalDuration.TimeSpan.Seconds.ToString());
                else
                    audio_total_time.Text = (audio_player_element.NaturalDuration.TimeSpan.Minutes < 10 ? ("0" + audio_player_element.NaturalDuration.TimeSpan.Minutes) : audio_player_element.NaturalDuration.TimeSpan.Minutes.ToString()) + ":" + (audio_player_element.NaturalDuration.TimeSpan.Seconds < 10 ? ("0" + audio_player_element.NaturalDuration.TimeSpan.Seconds) : audio_player_element.NaturalDuration.TimeSpan.Seconds.ToString());
            if (neighbors == null)
            {
                neighbors = new List<FileInfo>();
                BackgroundWorker get_audio_neighbor = new BackgroundWorker();
                get_audio_neighbor.DoWork += GetAudioNeighbor_DoWork;
                get_audio_neighbor.WorkerReportsProgress = get_audio_neighbor.WorkerSupportsCancellation = true;
                get_audio_neighbor.ProgressChanged += GetAudioNeighbor_ProgressChanged;
                background_workers.Add(get_audio_neighbor);
                get_audio_neighbor.RunWorkerAsync();
            }
        }

        private void GetAudioNeighbor_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = (BackgroundWorker)sender;
            neighbors.Add(audio_file);
            bw.ReportProgress(0, audio_file);
            foreach (var i in audio_file.Directory.GetFiles())
                if (i.FullName != audio_file.FullName && FileInfoCombonents.FilesInformation.ContainsKey(i.Extension.ToLower()) && FileInfoCombonents.FilesInformation[i.Extension.ToLower()].Category == FileCategory.Audio)
                {
                    neighbors.Add(i);
                    bw.ReportProgress(0, i);
                    Thread.Sleep(40);
                }
        }

        void GetAudioNeighbor_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ListBoxItem item = new ListBoxItem();
            FileInfo f = ((FileInfo)e.UserState);
            item.Content = f.Name;
            item.ToolTip = f.FullName;
            audio_play_list.Items.Add(item);
            ThicknessAnimation an = new ThicknessAnimation(from_animation, to_animation, new Duration(new TimeSpan(500000)));
            item.BeginAnimation(ListBoxItem.MarginProperty, an);
        }

        private void AudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            is_play = false;
            if (current_item_in_viewer < neighbors.Count - 1)
            {
                current_item_in_viewer++;
                audio_player_element.Source = new Uri(neighbors[current_item_in_viewer].FullName);
            }
        }

        private void AudioPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            is_play = false;
            caution.Visibility = Visibility.Visible;
            failed.Text = "Can't read this media file, make sure that you install all codec required and the file is a correct media file.";
        }

        private void AudioPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (audio_paly_button_image.ToolTip.ToString() == "Play")
            {
                audio_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-pause.png"));
                audio_paly_button_image.ToolTip = "Pause";
                audio_player_element.Play();
            }
            else
            {
                audio_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-play.png"));
                audio_paly_button_image.ToolTip = "Play";
                audio_player_element.Pause();
            }
        }

        private void NextAudio_Click(object sender, RoutedEventArgs e)
        {
            if (neighbors != null && current_item_in_viewer < neighbors.Count - 1)
            {
                current_item_in_viewer++;
                audio_player_element.Source = new Uri(neighbors[current_item_in_viewer].FullName);
            }
        }

        private void PreviousAudio_Click(object sender, RoutedEventArgs e)
        {
            if (neighbors != null && current_item_in_viewer < neighbors.Count + 1 && current_item_in_viewer > 0)
            {
                current_item_in_viewer--;
                audio_player_element.Source = new Uri(neighbors[current_item_in_viewer].FullName);
            }
        }

        private void AudioPlayList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (audio_play_list.SelectedIndex > -1)
            {
                current_item_in_viewer = audio_play_list.SelectedIndex;
                audio_player_element.Source = new Uri(neighbors[current_item_in_viewer].FullName);
            }
        }

        private void AudioPlayerGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //volum up and down
        }


        //Slider
        private void SliderMoverElement_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Ellipse)((Grid)sender).Children[1]).Opacity = 0.9;
            ((Ellipse)((Grid)sender).Children[2]).Opacity = 0.9;
        }

        private void SliderMoverElement_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Grid)sender).Children[1].Opacity = 1;
            ((Grid)sender).Children[2].Opacity = 1;
        }

        private void SliderMoverElement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Grid)sender).Children[1].Opacity = 0.8;
            ((Grid)sender).Children[2].Opacity = 0.8;
            slider_old_point = e.GetPosition(null);
            allow_move_slider = true;
        }

        private void SliderMoverElement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            allow_move_slider = false;
            Grid g = ((Grid)((Grid)sender).Children[2]);
            g.Children[2].Opacity = 1;
            g.Children[2].Opacity = 1;
            if (is_play && (audio_file == null ? video_player_element.NaturalDuration.HasTimeSpan : audio_player_element.NaturalDuration.HasTimeSpan))
                if (audio_file != null)
                    audio_player_element.Position = new TimeSpan((long)(audio_player_element.NaturalDuration.TimeSpan.TotalSeconds * 10000000.0 * (slider_forground.ActualWidth / slider_background.ActualWidth)));
                else
                    video_player_element.Position = new TimeSpan((long)(video_player_element.NaturalDuration.TimeSpan.TotalSeconds * 10000000.0 * (v_slider_forground.ActualWidth / v_slider_background.ActualWidth)));
        }

        private void AudioPlayerGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (allow_move_slider && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point new_point = e.GetPosition(null);
                double diff = new_point.X - slider_old_point.X;
                if (Math.Abs(diff) > 2)
                    if ((slider_forground.Width + diff) > slider_background.ActualWidth - 2)
                    {
                        slider_mover_element.Margin = new Thickness(slider_background.ActualWidth - 10, 0, 0, 0);
                        slider_forground.Width = slider_background.ActualWidth - 2;
                    }
                    else if (slider_forground.Width + diff < 0)
                    {
                        slider_mover_element.Margin = new Thickness(0, 0, 0, 0);
                        slider_forground.Width = 0;
                    }
                    else
                    {
                        slider_forground.Width = slider_forground.Width + diff;
                        slider_mover_element.Margin = new Thickness(slider_forground.Width, 0, 0, 0);
                    }
                slider_old_point.X = new_point.X;
            }
        }

        #endregion

        #region Video Section

        private void MakeVedioViewer(FileInfo file)
        {
            video_player.Visibility = Visibility.Visible;
            video_player_element.Source = new Uri(file.FullName);
            video_player_element.LoadedBehavior = MediaState.Manual;
            video_player_element.Play();
            BackgroundWorker slider_mover = new BackgroundWorker();
            slider_mover.WorkerReportsProgress = slider_mover.WorkerSupportsCancellation = true;
            slider_mover.DoWork += VideoSliderMover_DoWork;
            slider_mover.ProgressChanged += VideoSliderMover_ProgressChanged;
            background_workers.Add(slider_mover);
            Subtitle_Active = false;
            slider_mover.RunWorkerAsync();
        }

        private void VideoSliderMover_DoWork(object sender, DoWorkEventArgs e)
        {
            while (is_on)
            {
                Thread.Sleep(100);
                ((BackgroundWorker)sender).ReportProgress(0);
            }
        }

        private void VideoSliderMover_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (!allow_move_slider && video_player_element.NaturalDuration.HasTimeSpan)
            {
                v_slider_mover_element.Margin = new Thickness((v_slider_background.ActualWidth - 10) * (video_player_element.Position.TotalSeconds / video_player_element.NaturalDuration.TimeSpan.TotalSeconds), 0, 0, 0);
                v_slider_forground.Width = (v_slider_background.ActualWidth - 2) * (video_player_element.Position.TotalSeconds / video_player_element.NaturalDuration.TimeSpan.TotalSeconds);
            }
            if (video_player_element.Position.Days > 0)
                video_current_position_time.Text = video_player_element.Position.Days + (video_player_element.Position.Days == 1 ? " day  " : " days  ") + video_player_element.Position.Hours + ":" + (video_player_element.Position.Minutes < 10 ? ("0" + video_player_element.Position.Minutes) : video_player_element.Position.Minutes.ToString()) + ":" + (video_player_element.Position.Seconds < 10 ? ("0" + video_player_element.Position.Seconds) : video_player_element.Position.Seconds.ToString());
            else if (video_player_element.Position.Hours > 0)
                video_current_position_time.Text = video_player_element.Position.Hours + ":" + (video_player_element.Position.Minutes < 10 ? ("0" + video_player_element.Position.Minutes) : video_player_element.Position.Minutes.ToString()) + ":" + (video_player_element.Position.Seconds < 10 ? ("0" + video_player_element.Position.Seconds) : video_player_element.Position.Seconds.ToString());
            else
                video_current_position_time.Text = (video_player_element.Position.Minutes < 10 ? ("0" + video_player_element.Position.Minutes) : video_player_element.Position.Minutes.ToString()) + ":" + (video_player_element.Position.Seconds < 10 ? ("0" + video_player_element.Position.Seconds) : video_player_element.Position.Seconds.ToString());
            if (Subtitle_Active)
            {
                int subtitle_id = SearchSubtitle(video_player_element.Position.TotalMilliseconds, 0, subtitles.Count - 1);
                if (subtitle_id > -1 && subtitle_id < subtitles.Count)
                    subtitle_text.Text = subtitle_shadow.Text = subtitles[subtitle_id].text;
                else
                    subtitle_text.Text = subtitle_shadow.Text = string.Empty;
            }
        }

        private int SearchSubtitle(double pos, int start, int end)
        {
            if (subtitles.Count == 0 || start > subtitles.Count || end > subtitles.Count || start > end)
                return -1;
            int mid = (start + end) / 2;
            if (subtitles[mid].start < pos && subtitles[mid].end > pos)
                return mid;
            else if (start != end)
            {
                if (subtitles[mid].start < pos)
                    return SearchSubtitle(pos, mid + 1, end);
                else
                    return SearchSubtitle(pos, start, mid - 1);
            }
            return -1;
        }

        private void VideoPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (video_player_element.CanPause)
                if (video_paly_button_image.ToolTip.ToString() == "Play")
                {
                    video_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-pause.png"));
                    video_paly_button_image.ToolTip = "Pause";
                    video_player_element.Play();
                }
                else
                {
                    video_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-play.png"));
                    video_paly_button_image.ToolTip = "Play";
                    video_player_element.Pause();
                    is_play = true;
                }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            app_bar_background.Visibility = app_bar_dots.Visibility = app_bar_grid.Visibility = app_bar_stack.Visibility = Visibility.Visible;
            video_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-pause.png"));
            video_paly_button_image.ToolTip = "Pause";
            is_play = true;
            controler.RunWorkerAsync(video_control_grid);
            video_current_position_time.Text = "00:00";
            if (video_player_element.NaturalDuration.HasTimeSpan)
                if (video_player_element.NaturalDuration.TimeSpan.Days > 0)
                    video_total_time.Text = video_player_element.NaturalDuration.TimeSpan.Days + (video_player_element.NaturalDuration.TimeSpan.Days == 1 ? " day  " : " days  ") + video_player_element.NaturalDuration.TimeSpan.Hours + ":" + (video_player_element.NaturalDuration.TimeSpan.Minutes < 10 ? ("0" + video_player_element.NaturalDuration.TimeSpan.Minutes) : video_player_element.NaturalDuration.TimeSpan.Minutes.ToString()) + ":" + (video_player_element.NaturalDuration.TimeSpan.Seconds < 10 ? ("0" + video_player_element.NaturalDuration.TimeSpan.Seconds) : video_player_element.NaturalDuration.TimeSpan.Seconds.ToString());
                else if (video_player_element.NaturalDuration.TimeSpan.Hours > 0)
                    video_total_time.Text = video_player_element.NaturalDuration.TimeSpan.Hours + ":" + (video_player_element.NaturalDuration.TimeSpan.Minutes < 10 ? ("0" + video_player_element.NaturalDuration.TimeSpan.Minutes) : video_player_element.NaturalDuration.TimeSpan.Minutes.ToString()) + ":" + (video_player_element.NaturalDuration.TimeSpan.Seconds < 10 ? ("0" + video_player_element.NaturalDuration.TimeSpan.Seconds) : video_player_element.NaturalDuration.TimeSpan.Seconds.ToString());
                else
                    video_total_time.Text = (video_player_element.NaturalDuration.TimeSpan.Minutes < 10 ? ("0" + video_player_element.NaturalDuration.TimeSpan.Minutes) : video_player_element.NaturalDuration.TimeSpan.Minutes.ToString()) + ":" + (video_player_element.NaturalDuration.TimeSpan.Seconds < 10 ? ("0" + video_player_element.NaturalDuration.TimeSpan.Seconds) : video_player_element.NaturalDuration.TimeSpan.Seconds.ToString());
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            caution.Visibility = Visibility.Visible;
            failed.Text = "Can't read this media file, make sure that you install all codec required and the file is a correct media file.";
        }

        private void VideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            video_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-play.png"));
            video_paly_button_image.ToolTip = "Play";
            is_play = false;
            video_player_element.Stop();
        }

        private void VedioPlayerGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                video_control_grid.Visibility = Visibility.Visible;
                DoubleAnimation grid_opacity = new DoubleAnimation(0.75, show_duration);
                video_control_grid.BeginAnimation(Grid.OpacityProperty, grid_opacity);
                hide_after = 2500;
            }
            else if (allow_move_slider && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point new_point = e.GetPosition(null);
                double diff = new_point.X - slider_old_point.X;
                if (Math.Abs(diff) > 2)
                    if ((v_slider_forground.Width + diff) > v_slider_background.ActualWidth - 2)
                    {
                        v_slider_mover_element.Margin = new Thickness(v_slider_background.ActualWidth - 10, 0, 0, 0);
                        v_slider_forground.Width = v_slider_background.ActualWidth - 2;
                    }
                    else if (v_slider_forground.Width + diff < 0)
                    {
                        v_slider_mover_element.Margin = new Thickness(0, 0, 0, 0);
                        v_slider_forground.Width = 0;
                    }
                    else
                    {
                        v_slider_forground.Width = v_slider_forground.Width + diff;
                        v_slider_mover_element.Margin = new Thickness(v_slider_forground.Width, 0, 0, 0);
                    }
                slider_old_point.X = new_point.X;
            }
        }

        private void VedioPlayerGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //vedio volume control
        }

        private void VideoControlGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            is_mouse_on_controls = true;
        }

        private void VideoControlGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            is_mouse_on_controls = false;
        }

        #endregion

        #region Image Section

        bool isOn = false;
        Point old_point;

        private void MakeImageViewer(FileInfo file)
        {
            try
            {
                image_viewer_grid.Visibility = Visibility.Visible;
                image_view.Source = LoadImage(file.FullName);
                controler.RunWorkerAsync(image_control);
                BackgroundWorker get_image_neighbor = new BackgroundWorker();
                get_image_neighbor.DoWork += get_image_neighbor_DoWork;
                get_image_neighbor.RunWorkerAsync(file);
            }
            catch (Exception)
            {
                caution.Visibility = Visibility.Visible;
                failed.Text = "Can't open this image file, make sure that the file is not too large and it is a correct image file.";
            }
        }

        BitmapImage LoadImage(string path)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }

        void get_image_neighbor_DoWork(object sender, DoWorkEventArgs e)
        {
            List<FileInfo> list = new List<FileInfo>();
            FileInfo file = ((FileInfo)e.Argument);
            list.Add(file);
            try
            {
                foreach (var i in file.Directory.GetFiles())
                    if (i.FullName != file.FullName && FileInfoCombonents.FilesInformation.ContainsKey(i.Extension.ToLower()) && FileInfoCombonents.FilesInformation[i.Extension.ToLower()].Category == FileCategory.Image)
                        list.Add(i);
            }
            catch (Exception) { }
            neighbors = list;
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                DoubleAnimation grid_opacity = new DoubleAnimation(1, show_duration);
                image_control.BeginAnimation(OpacityProperty, grid_opacity);
                hide_after = 2500;
            }
        }

        private void ImageScrol_MouseLeave(object sender, MouseEventArgs e)
        {
            isOn = false;
            old_point = e.GetPosition(null);
        }

        private void ImageScrol_MouseDown(object sender, MouseButtonEventArgs e)
        {
            old_point = e.GetPosition(null);
            isOn = true;
        }

        private void ImageScrol_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isOn = false;
            old_point = e.GetPosition(null);
        }

        private void ImageScrol_MouseMove(object sender, MouseEventArgs e)
        {
            if (isOn)
            {
                ScrollViewer scr = ((ScrollViewer)sender);
                Point new_point = e.GetPosition(null);
                scr.ScrollToHorizontalOffset(scr.HorizontalOffset + (old_point.X - new_point.X));
                scr.ScrollToVerticalOffset(scr.VerticalOffset + (old_point.Y - new_point.Y));
                old_point = e.GetPosition(null);
            }
        }

        int image_angle = 0;

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            image_angle = (image_angle + 90) % 360;
            image_view.LayoutTransform = new RotateTransform(image_angle);
        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            if (image_view.Source == null)
                return;
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SaveFile;
            sfd.CreateFolderEnable = true;
            sfd.DialogTitle = "Save a copy of the image as (PNG)";
            sfd.AddExtension = true;
            sfd.MultiSelect = false;
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "PNG Image File (*.png)", extentions = SelectFileFilter.SetExtentions(".png") });
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                string path = sfd.SelectedItem;
                if (!path.ToLower().EndsWith(".png"))
                    path += ".png";
                if (File.Exists(path) && Messages.ShowMessage("File is already exist, overwrite?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") != MyMessageBoxResult.Yes)
                    return;
                try
                {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(new TransformedBitmap((BitmapSource)image_view.Source, new RotateTransform(image_angle))));
                    using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                        encoder.Save(stream);
                    Messages.ShowMessage("The image has been saved.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, path);
                }
                catch (Exception ex)
                { Messages.ShowMessage("We can not save the image.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            }
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            image_angle = (image_angle + 270) % 360;
            image_view.LayoutTransform = new RotateTransform(image_angle);
        }

        private void ImageControl_MouseLeave(object sender, MouseEventArgs e)
        {
            is_mouse_on_controls = false;
        }

        private void ImageControl_MouseEnter(object sender, MouseEventArgs e)
        {
            is_mouse_on_controls = true;
        }

        private void ImageViewer_KeyDown(object sender, KeyEventArgs e)
        {
            if (neighbors == null)
                return;
            if (e.Key == Key.NumPad4 && current_item_in_viewer > 0)
            {
                current_item_in_viewer--;
                try
                {
                    image_angle = 0;
                    image_view.LayoutTransform = null;
                    image_view.Source = LoadImage(neighbors[current_item_in_viewer].FullName);
                    ThicknessAnimation ta = new ThicknessAnimation(new Thickness(-798, 2, 2, 2), new Thickness(2, 2, 2, 2), show_duration);
                    image_view.BeginAnimation(MarginProperty, ta);
                }
                catch (Exception) { }
            }
            else if (e.Key == Key.NumPad6 && current_item_in_viewer < neighbors.Count - 1)
            {
                current_item_in_viewer++;
                try
                {
                    image_angle = 0;
                    image_view.LayoutTransform = null;
                    image_view.Source = LoadImage(neighbors[current_item_in_viewer].FullName);
                    ThicknessAnimation ta = new ThicknessAnimation(new Thickness(802, 2, 2, 2), new Thickness(2, 2, 2, 2), show_duration);
                    image_view.BeginAnimation(MarginProperty, ta);
                }
                catch (Exception) { }
            }
        }

        #endregion

        private void MakeHtmlViewer(FileInfo file)
        {
            WebBrowser web = new WebBrowser();
            web.Navigate(new Uri(file.FullName));
            web.Margin = new Thickness(0, 0, 12, 0);
            preview_grid.Children.Add(web);
        }

        XpsDocument xps_document;

        private void MakeXPSViewer(FileInfo file)
        {
            DocumentViewer doc_viewer = new DocumentViewer();
            doc_viewer.Margin = new Thickness(0, 0, 12, 0);
            try
            {
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                preview_grid.Children.Add(doc_viewer);
                xps_document = new XpsDocument(file.FullName, FileAccess.Read);
                doc_viewer.Document = xps_document.GetFixedDocumentSequence();
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            }
            catch (Exception ex)
            { Messages.ShowMessage("We can not view this file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void MakeRTFViewer(FileInfo file)
        {
            RichTextBox rich = new RichTextBox();
            rich.Margin = new Thickness(0, 0, 12, 0);
            rich.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            preview_grid.Children.Add(rich);
            rich.IsReadOnly = true;
            try
            {
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                FileStream FS = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                TextRange TR = new TextRange(rich.Document.ContentStart, rich.Document.ContentEnd);
                TR.Load(FS, DataFormats.Rtf);
                FS.Close();
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            }
            catch (Exception ex)
            { Messages.ShowMessage("We can not view this file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void MakeTextViewer(FileInfo file)
        {
            RichTextBox rich = new RichTextBox();
            rich.Margin = new Thickness(0, 0, 12, 0);
            rich.IsReadOnly = true;
            try
            {
                using (StreamReader SR = new StreamReader(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    char[] buffer = new char[4 * 1024 * 1024];
                    int count = SR.ReadBlock(buffer, 0, buffer.Length);
                    string text = new string(buffer, 0, count);
                    if (!SR.EndOfStream)
                        text += "\n\n... (only the first 4 MB of the file are shown)";
                    rich.Document = new FlowDocument(new Paragraph(new Run(text)));
                }
            }
            catch (Exception ex)
            { Messages.ShowMessage("We can not view this file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            rich.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            preview_grid.Children.Insert(0, rich);
        }

        private void MakeFontViewer(FileInfo file)
        {
            //font name and version
            font_viewer.Visibility = Visibility.Visible;
            try
            {
                var families = Fonts.GetFontFamilies(new Uri(file.FullName));
                if (families.Count > 0)
                    ((ListBox)font_viewer.Children[0]).FontFamily = families.First();
            }
            catch (Exception) { }
        }

        private  void MakeDataBaseViewer(FileInfo file)
        {
            //var connectionString = ConfigurationManager.ConnectionStrings["WingtipToys"].ConnectionString;
            //string queryString = "SELECT Id, ProductName FROM dbo.Products;";
            //using (var connection = new SqlConnection(connectionString))
            //{
            //    var command = new SqlCommand(queryString, connection);
            //    connection.Open();
            //    using (var reader = command.ExecuteReader())
            //    {
            //        while (reader.Read())
            //        {
            //            Console.WriteLine(String.Format("{0}, {1}", reader[0], reader[1]));
            //        }
            //    }
            //} 
        }

        #region Size on disk

        long SizeOnDisk(FileInfo info)
        {
            if (clusterSize == 0)
                GetClusterSize(info.Directory.Root.FullName);
            uint hosize;
            uint losize = GetCompressedFileSizeW(info.FullName, out hosize);
            long size;
            size = (long)hosize << 32 | losize;
            if (clusterSize > 0)
                return ((size + (clusterSize) - 1) / (clusterSize)) * clusterSize;
            else
                return info.Length;
        }

        void GetClusterSize(string drive)
        {
            clusterSize = 0;
            using (var searcher = new ManagementObjectSearcher("select BlockSize,NumberOfBlocks from Win32_Volume WHERE DriveLetter = '" + drive.TrimEnd('\\') + "'"))
            {
                foreach (ManagementObject i in searcher.Get())
                {
                    string bb = (i["BlockSize"]).ToString();
                    int.TryParse(bb, out clusterSize);
                    if (clusterSize == 0)
                        clusterSize = 4096;
                    break;
                }
            }
        }

        [DllImport("kernel32.dll")]
        static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName, [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        #endregion

        #endregion


        #region Directory Region

        List<FileOperationItem> files_list;
        List<FileOperationItem> directory_list;
        long total_size, total_on_disk_size;

        private void InitializeForDirectory(DirectoryInfo directory)
        {
            //Main Menu
            open_with_button.ToolTip = "Open in new window";
            open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/folder.png"));
            open_with_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/window-new.png"));
            copy_to_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/folder-copy.png"));
            move_to_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/folder-move.png"));

            //Information Tab
            PrimeInitialize(directory);
            //change_name_expander.IsEnabled = attribute_tab.IsEnabled = security_tab.IsEnabled = !directory.IsReadOnly;
            item_location.Text = "Path: " + directory.Parent.FullName;
            item_type_discriotion.Text = "Files Directory";
            item_category.Text = "Directory";
            calc_directory_size = new BackgroundWorker();
            calc_directory_size.WorkerReportsProgress = true;
            calc_directory_size.WorkerSupportsCancellation = true;
            calc_directory_size.ProgressChanged += CalcDirectorySize_ProgressChanged;
            calc_directory_size.DoWork += CalcDirectorySize_DoWork;
            background_workers.Add(calc_directory_size);
            calc_directory_size.RunWorkerAsync(di);

            //BackgroundWorker calc_directory_change_value = new BackgroundWorker();
            //calc_directory_change_value.WorkerReportsProgress = true;
            //calc_directory_change_value.WorkerSupportsCancellation = true;
            //calc_directory_change_value.ProgressChanged += CalcDirectorySizeChangeValue_ProgressChanged;
            //calc_directory_change_value.DoWork += CalcDirectorySizeChangeValue_DoWork;
            //calc_directory_change_value.RunWorkerAsync(calc_directory_size);

            FillFolderApps();

            item_image.Source = FileInfoCombonents.FilesInformation["directory"].Bitmap256;

            //Atribute Tab
            directory_attribute.Text = "Directory";

            preview_tab.IsEnabled = false;
            hash_tab.IsEnabled = false;
            preview_tab.ToolTip = "Preview is for files, use Open to browse this directory";
            hash_tab.ToolTip = "Hashes are calculated for files only";
            ToolTipService.SetShowOnDisabled(preview_tab, true);
            ToolTipService.SetShowOnDisabled(hash_tab, true);
            TabItem_MouseLeftButtonUp(information_tab, null);
            extract_button.Visibility = Visibility.Collapsed;
            explore_button.Visibility = Visibility.Collapsed;
            open_button.Margin = new Thickness(0, 84, 0, 0);

        }

        private void CalcDirectorySize_DoWork(object sender, DoWorkEventArgs e)
        {
            files_list = new List<FileOperationItem>();
            directory_list = new List<FileOperationItem>();
            BackgroundWorker bw = (BackgroundWorker)sender;
            Thread m = new Thread(() =>
            {
                if (di == null)
                    directory_root = dri;
                else
                    directory_root = new DriveInfo(di.Root.FullName);
                while (bw.IsBusy)
                {
                    try { bw.ReportProgress(0); }
                    catch (Exception) { }
                    Thread.Sleep(250);
                }
            });
            m.Start();
            //try
            { GetSubFilesAndDirectories((DirectoryInfo)e.Argument); }
            //catch (Exception ex) {/* Messages.ShowMessage("ex.Message", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);*/ }
            bw.ReportProgress(0);
        }

        private void CalcDirectorySize_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                item_size.Text = Functions.FileSize(total_size, 2);
                item_ondisk_size.Text = "on disk: " + Functions.FileSize(total_on_disk_size, 2) + " " + Math.Round(total_on_disk_size * 100.0 / (directory_root.TotalSize - directory_root.TotalFreeSpace), 3) + " % of used space on disk";
                directory_content.Text = "Directory contains " + directory_list.Count + (directory_list.Count > 1 ? " Directories" : " Directory") + " & " + files_list.Count + (files_list.Count > 1 ? " Files" : " File");
            }
            catch (Exception) { }
        }

        private void GetSubFilesAndDirectories(DirectoryInfo directory)
        {
            if (CancelEveryThinig)
                return;
            if (Functions.IsLink(directory) && directory_list.Count > 0)
                return;
            var d = directory.GetDirectories();
            foreach (var i in d)
            {
                directory_list.Add(new FileOperationItem(i, false, null));
                try
                {
                    if (!Functions.IsLink(i))
                        GetSubFilesAndDirectories(i);
                }
                catch (Exception) { }
            }
            var f = directory.GetFiles();
            foreach (var i in f)
            {
                files_list.Add(new FileOperationItem(i, false, ""));
                total_size += i.Length;
                total_on_disk_size += SizeOnDisk(i);
            }
        }

        #endregion


        #region Drive

        private void InitializeForDrive(DriveInfo driveInfo)
        {
            //Information Tab
            PrimeInitialize(new DirectoryInfo(driveInfo.Name));
            Title += " '" + driveInfo.Name + "'";
            item_path.Text = driveInfo.Name;
            item_location.Text = "Path: root";
            change_name_expander.Header = "Change Label: ";
            item_type.Text = driveInfo.DriveType.ToString();
            FillFolderApps();
            time_region.Visibility = Visibility.Hidden;
            item_image.Source = Functions.GetDriveImage(driveInfo, 256);

            //main menu and tabs
            move_to_button.Visibility = delete_button.Visibility = Visibility.Collapsed;
            preview_tab.IsEnabled = false;
            hash_tab.IsEnabled = false;
            preview_tab.ToolTip = "Preview is for files, use Open to browse this drive";
            hash_tab.ToolTip = "Hashes are calculated for files only";
            ToolTipService.SetShowOnDisabled(preview_tab, true);
            ToolTipService.SetShowOnDisabled(hash_tab, true);
            open_with_button.ToolTip = "Open in new window";
            open_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/harddisk.png"));
            open_with_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/window-new.png"));
            copy_to_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/folder-copy.png"));


            if (driveInfo.IsReady)
            {
                try
                {
                    item_name.Text = "Name: " + driveInfo.VolumeLabel;
                    rename_textbox.Text = driveInfo.VolumeLabel;
                    item_type_discriotion.Text = driveInfo.DriveFormat;
                    item_size.Text = "Drive size: " + Functions.FileSize(driveInfo.TotalSize, 2) + " with " + Functions.FileSize(driveInfo.TotalFreeSpace, 2) + " Free";
                    item_ondisk_size.Text = "Used space: " + Functions.FileSize(driveInfo.TotalSize - driveInfo.TotalFreeSpace, 2) + " " + Math.Round((driveInfo.TotalSize - driveInfo.TotalFreeSpace) * 100.0 / driveInfo.TotalSize, 3) + " %";
                }
                catch (Exception) { }
                calc_directory_size = new BackgroundWorker();
                calc_directory_size.WorkerReportsProgress = true;
                calc_directory_size.WorkerSupportsCancellation = true;
                calc_directory_size.ProgressChanged += CalcDriveSize_ProgressChanged;
                calc_directory_size.DoWork += CalcDirectorySize_DoWork;
                background_workers.Add(calc_directory_size);
                calc_directory_size.RunWorkerAsync(dri.RootDirectory);
                //Create flowshart;
                //change_name_expander.IsEnabled = attribute_tab.IsEnabled = security_tab.IsEnabled = !directory.IsReadOnly;

                //Atribute Tab
                if (driveInfo.DriveType == DriveType.CDRom)
                    attribute_grid.IsEnabled = false;
                normal.IsEnabled = read_only.IsEnabled = hidden.IsEnabled = archive.IsEnabled = encrypted.IsEnabled = system.IsEnabled = false;
                other.IsChecked = true;
                directory_content.Text = "Directory";
                reverse.Visibility = Visibility.Collapsed;
            }
            else
            {
                item_name.Text = "Name: " + driveInfo.Name;
                attribute_grid.IsEnabled = false;
                security_tab.IsEnabled = false;
                security_tab.ToolTip = "The drive is not ready";
                ToolTipService.SetShowOnDisabled(security_tab, true);
                change_name_expander.IsEnabled = false;
                item_type_discriotion.Text = "Drive is not ready!";
                item_size.Text = "0 byte";
                item_ondisk_size.Text = "0 byte";
                directory_content.Text = "Drive is not ready!";
                item_category.Text = "";
            }
            TabItem_MouseLeftButtonUp(information_tab, null);
            extract_button.Visibility = Visibility.Collapsed;
            explore_button.Visibility = Visibility.Collapsed;
            open_button.Margin = new Thickness(0, 143, 0, 0);
        }

        private void CalcDriveSize_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            directory_content.Text = "Drive contains " + directory_list.Count + (directory_list.Count > 1 ? " Directories" : " Directory") + " & " + files_list.Count + (files_list.Count > 1 ? " Files" : " File") + (" with " + Functions.FileSize(total_size, 2) + " size.");
        }

        #endregion

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string editor = Functions.FindProgram("notepad++.exe", "Notepad++", @"SOFTWARE\Notepad++", "");
                if (editor != null)
                    Process.Start(new ProcessStartInfo(editor, "\"" + fi.FullName + "\""));
                else
                    Process.Start(new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "notepad.exe"), "\"" + fi.FullName + "\""));
            }
            catch (Exception ex) { Messages.ShowMessage("We found error while starting the editor.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void ConvertToISOButton_Click(object sender, RoutedEventArgs e)
        {
            Messages.ShowMessage("Convert to ISO is not available.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "It needs the tool 'unisz.exe', which has no official installer or download.");
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        static extern int AddFontResource(string lpszFilename);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        private void InstallFontButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fonts_folder = System.IO.Path.Combine(System.IO.Path.Combine(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft"), "Windows"), "Fonts");
                string target = System.IO.Path.Combine(fonts_folder, fi.Name);
                string system_font = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), fi.Name);
                if (File.Exists(target) || File.Exists(system_font))
                {
                    Messages.ShowMessage("This font is already installed.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                    return;
                }
                string font_name = System.IO.Path.GetFileNameWithoutExtension(fi.Name);
                try
                {
                    var families = Fonts.GetFontFamilies(new Uri(fi.FullName));
                    if (families.Count > 0)
                        font_name = families.First().Source.Split('#').Last();
                }
                catch (Exception) { }
                Directory.CreateDirectory(fonts_folder);
                File.Copy(fi.FullName, target);
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Fonts"))
                    key.SetValue(font_name + (fi.Extension.ToLower() == ".otf" ? " (OpenType)" : " (TrueType)"), target);
                AddFontResource(target);
                IntPtr result;
                SendMessageTimeout(new IntPtr(0xffff), 0x001D, IntPtr.Zero, IntPtr.Zero, 0x0002, 1000, out result);
                Messages.ShowMessage("Font has been installed successfully.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "The font was installed for your user account only.");
            }
            catch (Exception ex) { Messages.ShowMessage("We found error while trying to install the font.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void ExtractArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            File_Compresser window = new File_Compresser();
            Functions.ShowTool(window);
            window.OpenArchive(fi.FullName, true);
        }

        private void ExplorerArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            File_Compresser window = new File_Compresser();
            Functions.ShowTool(window);
            window.OpenArchive(fi.FullName, false);
        }



        #endregion

        #region Shell preview and messages

        bool cloud_only = false, start_with_hash = false;
        ShellPreviewHost shell_host;

        internal void ShowHashDialog(FileInfo file)
        {
            start_with_hash = true;
            ShowDialog(file, ItemType.File);
        }

        static bool IsCloudOnly(FileInfo file)
        {
            try
            {
                return ((int)file.Attributes & (0x400000 | 0x40000 | 0x1000)) != 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        void PreviewWith(Action make)
        {
            if (!cloud_only && !start_with_hash)
            {
                make();
                return;
            }
            Button show = new Button() { Content = cloud_only ? "Download and preview" : "Show preview" };
            Button open = new Button() { Content = "Open" };
            Grid panel = ShowPreviewMessage(Functions.GetFileImage256(fi.Extension), cloud_only ? "This file is stored only in the cloud" : "Preview is paused", cloud_only ? "Previewing it downloads it from OneDrive (or your cloud provider) to this PC." : "Press 'Show preview' to load the preview of this file.", show, open);
            show.Click += (object sender, RoutedEventArgs e) =>
            {
                cloud_only = start_with_hash = false;
                preview_grid.Children.Remove(panel);
                make();
            };
            open.Click += MainMenuOpen_Click;
        }

        void PreviewWithShell(FileInfo file)
        {
            Guid handler;
            if (!ShellPreviewHost.FindHandler(file.Extension, out handler))
            {
                ShowNoPreview(file, null);
                TabItem_MouseLeftButtonUp(information_tab, null);
                return;
            }
            PreviewWith(() =>
            {
                ShellPreviewHost host = new ShellPreviewHost();
                host.Margin = new Thickness(6, 0, 12, 0);
                shell_host = host;
                preview_grid.Children.Add(host);
                bool opened = false;
                host.Loaded += (object sender, RoutedEventArgs e) =>
                {
                    if (opened)
                        return;
                    opened = true;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (CancelEveryThinig)
                            return;
                        string error = host.Open(file.FullName, handler);
                        if (error != null)
                        {
                            preview_grid.Children.Remove(host);
                            host.Dispose();
                            if (shell_host == host)
                                shell_host = null;
                            ShowNoPreview(file, "The preview handler of this file type failed: " + error);
                        }
                    }), System.Windows.Threading.DispatcherPriority.Background);
                };
            });
        }

        void ShowNoPreview(FileInfo file, string reason)
        {
            Button open = new Button() { Content = "Open" };
            open.Click += MainMenuOpen_Click;
            ShowPreviewMessage(Functions.GetFileImage256(file.Extension), "No preview available for " + (file.Extension.Length > 0 ? file.Extension.ToLower() : "this file"), reason ?? "Windows has no preview handler for this file type.", open);
        }

        Grid ShowPreviewMessage(ImageSource icon, string title, string detail, params Button[] buttons)
        {
            Grid panel = new Grid() { Background = new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF4)), Margin = new Thickness(0, 0, 10, 0) };
            StackPanel stack = new StackPanel() { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 600 };
            stack.Children.Add(new Image() { Source = icon, Width = 128, Height = 128 });
            stack.Children.Add(new TextBlock() { Text = title, FontSize = 22, Foreground = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 0) });
            if (!string.IsNullOrEmpty(detail))
                stack.Children.Add(new TextBlock() { Text = detail, FontSize = 14, Foreground = Brushes.Gray, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
            StackPanel row = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 0) };
            foreach (var i in buttons)
            {
                i.MinWidth = 150;
                i.Height = 32;
                i.Margin = new Thickness(5, 0, 5, 0);
                i.FontSize = 14;
                i.Background = null;
                i.BorderBrush = i.Foreground = new SolidColorBrush(Color.FromRgb(0x2A, 0xB0, 0x48));
                row.Children.Add(i);
            }
            stack.Children.Add(row);
            panel.Children.Add(stack);
            preview_grid.Children.Add(panel);
            return panel;
        }

        #endregion

        #region More information

        void FillMoreInformation(FileSystemInfo item)
        {
            info_owner.Text = "Owner: " + OwnerName(item);
            info_owner.ToolTip = info_owner.Text;
            FileInfo file = item as FileInfo;
            if (file == null || cloud_only)
                return;
            string extension = file.Extension.ToLower();
            if (extension == ".exe" || extension == ".dll" || extension == ".sys" || extension == ".ocx" || extension == ".scr" || extension == ".cpl")
                try
                {
                    FileVersionInfo version = FileVersionInfo.GetVersionInfo(file.FullName);
                    if (!string.IsNullOrEmpty(version.FileVersion))
                    {
                        info_version.Text = "Version: " + version.FileVersion + (string.IsNullOrEmpty(version.ProductName) ? "" : " (" + version.ProductName + ")");
                        info_version.ToolTip = info_version.Text + (string.IsNullOrEmpty(version.CompanyName) ? "" : "\n" + version.CompanyName);
                        info_version.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception) { }
            if (FileInfoCombonents.FilesInformation.ContainsKey(extension) && FileInfoCombonents.FilesInformation[extension].Category == FileCategory.Image)
                try
                {
                    using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        BitmapDecoder decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation | BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None);
                        info_dimensions.Text = "Dimensions: " + decoder.Frames[0].PixelWidth + " x " + decoder.Frames[0].PixelHeight + " pixels";
                        info_dimensions.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception) { }
        }

        static string OwnerName(FileSystemInfo item)
        {
            try
            {
                FileSystemSecurity security = item is FileInfo ? (FileSystemSecurity)((FileInfo)item).GetAccessControl(AccessControlSections.Owner) : ((DirectoryInfo)item).GetAccessControl(AccessControlSections.Owner);
                return IdentityName(security.GetOwner(typeof(SecurityIdentifier)));
            }
            catch (UnauthorizedAccessException)
            {
                return "(no permission to read)";
            }
            catch (Exception)
            {
                return "(unknown)";
            }
        }

        static string IdentityName(IdentityReference identity)
        {
            if (identity == null)
                return "(unknown)";
            try
            {
                return identity.Translate(typeof(NTAccount)).Value;
            }
            catch (Exception)
            {
                return identity.Value;
            }
        }

        void FillApps(string extension)
        {
            ClearApps();
            string default_name = ShellPreviewHost.QueryAssociation(4, extension, null), default_exe = ShellPreviewHost.QueryAssociation(2, extension, null), default_id = ShellPreviewHost.QueryAssociation(21, extension, null);
            List<OpenWithApp> apps = ShellAssociations.Handlers(extension, default_name, default_exe, default_id);
            if (!apps.Any(x => x.IsDefault) && !string.IsNullOrEmpty(default_name))
                apps.Insert(0, new OpenWithApp() { Name = default_name, Id = default_exe, Kind = OpenWithKind.Default, IsDefault = true, Icon = ShellAssociations.LoadIcon(default_exe, 0), Tip = default_name + (string.IsNullOrEmpty(default_exe) ? "" : "\n" + default_exe) });
            foreach (var i in apps)
                app_can_open_this_type.Items.Add(CreateAppItem(i, extension));
            if (apps.Count == 0)
                app_can_open_this_type.Items.Add(new ListBoxItem() { Content = new TextBlock() { Text = "No app is registered for this type", TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 13 }, Foreground = Brushes.Gray, IsEnabled = false });
            app_can_open_this_type.Items.Add(CreateAppItem(new OpenWithApp() { Name = "Other app...", Kind = OpenWithKind.Other, Icon = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/open-with.png")), Tip = "Choose another app in the Windows \"Open with\" dialog" }, extension));
        }

        void FillFolderApps()
        {
            ClearApps();
            app_can_open_this_type.Items.Add(CreateAppItem(new OpenWithApp() { Name = "File Manager", Kind = OpenWithKind.FileManager, Icon = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/App Icon.png")), Tip = "Open in a new File Manager window" }, null));
            app_can_open_this_type.Items.Add(CreateAppItem(new OpenWithApp() { Name = "File Explorer", Kind = OpenWithKind.FileExplorer, Icon = ShellAssociations.LoadIcon(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), 0), Tip = "Open in Windows File Explorer" }, null));
        }

        void ClearApps()
        {
            foreach (var i in app_can_open_this_type.Items.OfType<ListBoxItem>())
                if (i.Tag is OpenWithApp)
                    ShellAssociations.Release(((OpenWithApp)i.Tag).Handler);
            app_can_open_this_type.Items.Clear();
        }

        ListBoxItem CreateAppItem(OpenWithApp app, string extension)
        {
            DockPanel row = new DockPanel() { Height = 20 };
            row.Children.Add(new Image() { Source = app.Icon, Width = 16, Height = 16, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock() { Text = app.Title, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, FontSize = 13 });
            ListBoxItem item = new ListBoxItem() { Content = row, Tag = app, ToolTip = app.Tip };
            item.MouseDoubleClick += (object sender, MouseButtonEventArgs e) =>
            {
                if (e.ChangedButton != MouseButton.Left)
                    return;
                e.Handled = true;
                OpenApp(app);
            };
            if (app.Kind == OpenWithKind.Other)
                return item;
            ContextMenu menu = new ContextMenu();
            MenuItem open = new MenuItem() { Header = "Open with this app", FontWeight = FontWeights.Bold };
            open.Click += (object sender, RoutedEventArgs e) => { OpenApp(app); };
            menu.Items.Add(open);
            if (app.Kind == OpenWithKind.Handler || app.Kind == OpenWithKind.Default)
            {
                MenuItem set_default = new MenuItem() { Header = "Set as default..." };
                ToolTipService.SetShowOnDisabled(set_default, true);
                if (app.IsDefault)
                {
                    set_default.IsEnabled = false;
                    set_default.ToolTip = "This is already the default app for " + extension + " files";
                }
                else
                {
                    set_default.ToolTip = "Opens the Windows \"Open with\" dialog with \"Always use this app\" ticked: choose " + app.Name + " there (the file opens with it)";
                    set_default.Click += (object sender, RoutedEventArgs e) =>
                    {
                        string error = ShellAssociations.OpenWithDialog(new WindowInteropHelper(this).Handle, fi.FullName, true);
                        if (error != null)
                            Messages.ShowMessage("We can not open the Windows \"Open with\" dialog.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, error);
                        else
                            FillApps(extension);
                    };
                }
                menu.Items.Add(set_default);
            }
            menu.Items.Add(new Separator());
            MenuItem copy = new MenuItem() { Header = "Copy name" };
            copy.Click += (object sender, RoutedEventArgs e) =>
            {
                try
                {
                    Clipboard.SetText(app.Name);
                }
                catch (Exception ex)
                {
                    Messages.ShowMessage("We can not copy to the clipboard.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, ex.Message);
                }
            };
            menu.Items.Add(copy);
            item.ContextMenu = menu;
            return item;
        }

        void OpenApp(OpenWithApp app)
        {
            string path = fi != null ? fi.FullName : dri != null ? dri.Name : di.FullName, error = null;
            try
            {
                if (app.Kind == OpenWithKind.FileManager)
                    Directory_Explorer.ExplorePath(path);
                else if (app.Kind == OpenWithKind.FileExplorer)
                    Process.Start(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), "\"" + path + "\"");
                else if (app.Kind == OpenWithKind.Other)
                    error = ShellAssociations.OpenWithDialog(new WindowInteropHelper(this).Handle, path, false);
                else if (app.IsDefault || app.Handler == null)
                    Process.Start(path);
                else
                    error = ShellAssociations.Invoke(app.Handler, path);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            if (error != null)
                Messages.ShowMessage(string.Format("Sorry we can not open '{0}' with {1}.", path, app.Name), ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, error);
        }

        private void AppsList_KeyDown(object sender, KeyEventArgs e)
        {
            ListBoxItem item = app_can_open_this_type.SelectedItem as ListBoxItem;
            if (e.Key == Key.Enter && item != null && item.Tag is OpenWithApp)
            {
                e.Handled = true;
                OpenApp((OpenWithApp)item.Tag);
            }
        }

        private void CopyDetails_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine(item_path.Text);
            foreach (var i in new TextBlock[] { item_name, item_location, item_type, item_type_discriotion, item_category, item_size, item_ondisk_size, directory_content, creation_time, modified_time, last_accessed_time, info_owner, info_version, info_dimensions })
                if (i.Visibility == Visibility.Visible && !string.IsNullOrEmpty(i.Text))
                    text.AppendLine(i.Text);
            try
            {
                Clipboard.SetText(text.ToString());
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not copy to the clipboard.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, ex.Message);
            }
        }

        #endregion

        #region Hash

        BackgroundWorker hash_worker;
        string[] hash_results;

        private void HashCalculate_Click(object sender, RoutedEventArgs e)
        {
            if (fi == null || (hash_worker != null && hash_worker.IsBusy))
                return;
            string path = fi.FullName;
            string[] results = null;
            string error = null;
            hash_md5.Text = hash_sha1.Text = hash_sha256.Text = hash_sha512.Text = "";
            hash_results = null;
            hash_copy_md5.IsEnabled = hash_copy_sha1.IsEnabled = hash_copy_sha256.IsEnabled = hash_copy_sha512.IsEnabled = false;
            HashCompare_TextChanged(null, null);
            hash_worker = new BackgroundWorker();
            hash_worker.WorkerReportsProgress = hash_worker.WorkerSupportsCancellation = true;
            background_workers.Add(hash_worker);
            hash_worker.DoWork += (object sender2, DoWorkEventArgs e2) =>
            {
                BackgroundWorker worker = (BackgroundWorker)sender2;
                try
                {
                    using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1024 * 1024, FileOptions.SequentialScan))
                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    using (SHA1 sha1 = new SHA1CryptoServiceProvider())
                    using (SHA256 sha256 = new SHA256Managed())
                    using (SHA512 sha512 = new SHA512Managed())
                    {
                        HashAlgorithm[] algorithms = { md5, sha1, sha256, sha512 };
                        byte[] buffer = new byte[1024 * 1024];
                        long done = 0, length = Math.Max(1, stream.Length);
                        int read, last_percent = -1;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (worker.CancellationPending)
                            {
                                e2.Cancel = true;
                                return;
                            }
                            foreach (var i in algorithms)
                                i.TransformBlock(buffer, 0, read, null, 0);
                            done += read;
                            int percent = (int)(done * 100 / length);
                            if (percent != last_percent)
                            {
                                last_percent = percent;
                                worker.ReportProgress(percent);
                            }
                        }
                        foreach (var i in algorithms)
                            i.TransformFinalBlock(buffer, 0, 0);
                        results = algorithms.Select(x => BitConverter.ToString(x.Hash).Replace("-", "").ToLower()).ToArray();
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            };
            hash_worker.ProgressChanged += (object sender2, ProgressChangedEventArgs e2) =>
            {
                hash_progress.Value = e2.ProgressPercentage;
                hash_status.Text = "Calculating... " + e2.ProgressPercentage + "%";
            };
            hash_worker.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
            {
                if (CancelEveryThinig)
                    return;
                hash_progress.Visibility = Visibility.Hidden;
                hash_calculate_btm.IsEnabled = true;
                hash_calculate_btm.ToolTip = null;
                hash_cancel_btm.IsEnabled = false;
                hash_cancel_btm.ToolTip = "Nothing is being calculated";
                if (e2.Cancelled)
                    hash_status.Text = "Canceled.";
                else if (error != null)
                {
                    hash_status.Text = "We can not calculate the hash of this file: " + error;
                    Messages.ShowMessage("We can not calculate the hash of this file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, error);
                }
                else if (results != null)
                {
                    hash_results = results;
                    hash_md5.Text = results[0];
                    hash_sha1.Text = results[1];
                    hash_sha256.Text = results[2];
                    hash_sha512.Text = results[3];
                    hash_copy_md5.IsEnabled = hash_copy_sha1.IsEnabled = hash_copy_sha256.IsEnabled = hash_copy_sha512.IsEnabled = true;
                    hash_copy_md5.ToolTip = hash_copy_sha1.ToolTip = hash_copy_sha256.ToolTip = hash_copy_sha512.ToolTip = "Copy to the clipboard";
                    hash_status.Text = "Done.";
                    HashCompare_TextChanged(null, null);
                }
            };
            hash_progress.Value = 0;
            hash_progress.Visibility = Visibility.Visible;
            hash_status.Text = "Calculating... 0%";
            hash_calculate_btm.IsEnabled = false;
            hash_calculate_btm.ToolTip = "Already calculating";
            hash_cancel_btm.IsEnabled = true;
            hash_cancel_btm.ToolTip = null;
            hash_worker.RunWorkerAsync();
        }

        private void HashCancel_Click(object sender, RoutedEventArgs e)
        {
            if (hash_worker != null && hash_worker.IsBusy)
                hash_worker.CancelAsync();
        }

        private void HashCopy_Click(object sender, RoutedEventArgs e)
        {
            TextBox box = FindName("hash_" + (sender as Button).Tag) as TextBox;
            if (box != null && box.Text.Length > 0)
                try
                {
                    Clipboard.SetText(box.Text);
                }
                catch (Exception ex)
                {
                    Messages.ShowMessage("We can not copy to the clipboard.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, ex.Message);
                }
        }

        private void HashCompare_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (hash_compare == null || hash_compare_result == null)
                return;
            string value = new string(hash_compare.Text.Where(x => !char.IsWhiteSpace(x) && x != '-').ToArray()).ToLower();
            if (value.Length == 0)
            {
                hash_compare.ClearValue(TextBox.BackgroundProperty);
                hash_compare_result.Text = "";
            }
            else if (hash_results == null)
            {
                hash_compare.ClearValue(TextBox.BackgroundProperty);
                hash_compare_result.Foreground = Brushes.Gray;
                hash_compare_result.Text = "Press Calculate to compare.";
            }
            else
            {
                int index = Array.IndexOf(hash_results, value);
                string[] names = { "MD5", "SHA-1", "SHA-256", "SHA-512" };
                hash_compare.Background = new SolidColorBrush(index >= 0 ? Color.FromRgb(0xD4, 0xF5, 0xD9) : Color.FromRgb(0xFA, 0xD4, 0xD2));
                hash_compare_result.Foreground = index >= 0 ? Brushes.ForestGreen : Brushes.Tomato;
                hash_compare_result.Text = index >= 0 ? "Matches the " + names[index] + " of this file." : "Does not match any hash of this file.";
            }
        }

        #endregion

        #region Security

        bool security_loaded = false;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, string pszObjectName, string pszPropertyPage);

        string ItemPath()
        {
            return fi != null ? fi.FullName : di != null ? di.FullName : dri.Name;
        }

        void LoadSecurity()
        {
            security_loaded = true;
            security_list.Items.Clear();
            security_error.Visibility = Visibility.Collapsed;
            security_list.Visibility = Visibility.Visible;
            try
            {
                FileSystemSecurity security = fi != null ? (FileSystemSecurity)new FileInfo(fi.FullName).GetAccessControl() : new DirectoryInfo(ItemPath()).GetAccessControl();
                security_owner.Text = IdentityName(security.GetOwner(typeof(SecurityIdentifier)));
                foreach (FileSystemAccessRule i in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                    security_list.Items.Add(new SecurityRuleItem(IdentityName(i.IdentityReference), i.AccessControlType == AccessControlType.Allow ? "Allow" : "Deny", RightsName(i.FileSystemRights), i.IsInherited ? "Yes" : "No"));
                if (security_list.Items.Count == 0)
                {
                    security_error.Text = "This item has no permission entries.";
                    security_error.Visibility = Visibility.Visible;
                }
            }
            catch (UnauthorizedAccessException)
            {
                security_owner.Text = "(no permission to read)";
                security_list.Visibility = Visibility.Hidden;
                security_error.Text = "You do not have permission to read the security settings of this item.\nPress 'Advanced...' to open the Windows security properties, it can ask for administrator rights.";
                security_error.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                security_list.Visibility = Visibility.Hidden;
                security_error.Text = "We can not read the security settings of this item: " + ex.Message;
                security_error.Visibility = Visibility.Visible;
            }
        }

        static string RightsName(FileSystemRights rights)
        {
            int value = (int)rights;
            List<string> names = new List<string>();
            if ((value & 0x10000000) != 0)
                names.Add("Full control");
            if ((value & unchecked((int)0x80000000)) != 0)
                names.Add("Read");
            if ((value & 0x40000000) != 0)
                names.Add("Write");
            if ((value & 0x20000000) != 0)
                names.Add("Execute");
            FileSystemRights specific = (FileSystemRights)(value & 0x0FFFFFFF);
            if ((specific & FileSystemRights.FullControl) == FileSystemRights.FullControl)
                names.Add("Full control");
            else if ((specific & FileSystemRights.Modify) == FileSystemRights.Modify)
                names.Add("Modify");
            else if ((specific & FileSystemRights.ReadAndExecute) == FileSystemRights.ReadAndExecute)
            {
                names.Add("Read & execute");
                if ((specific & FileSystemRights.Write) == FileSystemRights.Write)
                    names.Add("Write");
            }
            else if ((specific & ~FileSystemRights.Synchronize) != 0)
                names.Add((specific & ~FileSystemRights.Synchronize).ToString());
            return names.Count == 0 ? "Special" : string.Join(", ", names.Distinct().ToArray());
        }

        private void SecurityAdvanced_Click(object sender, RoutedEventArgs e)
        {
            if (!SHObjectProperties(new WindowInteropHelper(this).Handle, 2, ItemPath(), "Security"))
                Messages.ShowMessage("We can not open the Windows security properties of this item.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        #endregion

        #region TabItem Stack Apperance

        private void TabItem_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Label)sender).Foreground = Brushes.Blue;
        }

        private void TabItem_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Label)sender).Foreground = new SolidColorBrush(Color.FromArgb(255, 64, 64, 64));
        }

        private void TabItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            foreach (Label i in tab_control.Children)
                i.Background = null;
            ((Label)sender).Background = Brushes.White;
            if (sender == security_tab && !security_loaded)
                LoadSecurity();
            ThicknessAnimation an = new ThicknessAnimation(new Thickness(tab_control.Children.IndexOf(((Label)sender)) * -800, 0, 0, 0), new Duration(new TimeSpan(1000000)));
            mother_of_all_grid.BeginAnimation(Grid.MarginProperty, an);
        }

        #endregion

        #region Main Menu Apperance

        void MainMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            if (main_menu.Margin.Left < -60)
            {
                if (shell_host != null)
                    shell_host.Visibility = Visibility.Hidden;
                ThicknessAnimation animation = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(1000000)));
                main_menu.BeginAnimation(Grid.MarginProperty, animation);
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += ShowMenu_DoWork;
                bw.ProgressChanged += ShowMenu_ProgressChanged;
                bw.WorkerReportsProgress = true;
                bw.RunWorkerAsync();
            }
        }

        void ShowMenu_DoWork(object sender, DoWorkEventArgs e)
        {
            int i = 0;
            while (i < main_menu_item_count)
            {
                ((BackgroundWorker)sender).ReportProgress(i++);
                Thread.Sleep(55);
            }
        }

        void ShowMenu_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ((Button)main_menu_stack.Children[e.ProgressPercentage]).Background = null;
            ThicknessAnimation animation = new ThicknessAnimation(new Thickness(0, ((Button)main_menu_stack.Children[e.ProgressPercentage]).Margin.Top, 0, 0), new Duration(new TimeSpan(500000)));
            main_menu_stack.Children[e.ProgressPercentage].BeginAnimation(Button.MarginProperty, animation);
        }

        private void MainMenu_MouseLeave(object sender, MouseEventArgs e)
        {
            if (main_menu.Margin.Left > -4)
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += HideMenu_DoWork;
                bw.ProgressChanged += HideMenu_ProgressChanged;
                bw.WorkerReportsProgress = true;
                bw.RunWorkerAsync();
                bw.RunWorkerCompleted += HideMenu_RunWorkerCompleted;
            }
        }

        private void HideMenu_DoWork(object sender, DoWorkEventArgs e)
        {
            int i = main_menu_item_count;
            Thread.Sleep(200);
            while (i > 0)
            {
                ((BackgroundWorker)sender).ReportProgress(--i);
                Thread.Sleep(55);
            }
        }

        private void HideMenu_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ThicknessAnimation animation = new ThicknessAnimation(new Thickness(-128, ((Button)main_menu_stack.Children[e.ProgressPercentage]).Margin.Top, 0, 0), new Duration(new TimeSpan(750000)));
            main_menu_stack.Children[e.ProgressPercentage].BeginAnimation(Button.MarginProperty, animation);
        }

        private void HideMenu_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ThicknessAnimation animation = new ThicknessAnimation(new Thickness(-64, 0, 0, 0), new Duration(new TimeSpan(750000)));
            main_menu.BeginAnimation(Grid.MarginProperty, animation);
            if (shell_host != null)
                shell_host.Visibility = Visibility.Visible;
        }



        void RoundButton_MouseOver(object sender, MouseEventArgs e)
        {
            BackgroundWorker BW = new BackgroundWorker();
            BW.DoWork += RoundButtonColor_DoWork;
            BW.ProgressChanged += RoundButtonColor_ProgressChanged;
            BW.WorkerReportsProgress = true;
            BW.RunWorkerAsync(((Grid)sender).Children[0]);
        }

        void RoundButtonColor_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker BW = (BackgroundWorker)sender;
            for (int b = 0; b < 200; b += 10)
            {
                BW.ReportProgress(b, e.Argument);
                Thread.Sleep(10);
            }
        }

        void RoundButtonColor_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            byte i = (byte)e.ProgressPercentage;
            ((Ellipse)e.UserState).Fill = new SolidColorBrush(Color.FromArgb((byte)(5 * (i / 4)), 0, (byte)(3 * i / 4), i));
        }



        void RoundButton_MouseLeave(object sender, MouseEventArgs e)
        {
            BackgroundWorker BW = new BackgroundWorker();
            BW.DoWork += RoundButtonRecolor_DoWork;
            BW.ProgressChanged += RoundButtonRecolor_ProgressChanged;
            BW.WorkerReportsProgress = true;
            BW.RunWorkerAsync((((Grid)sender).Children[0]));
        }

        void RoundButtonRecolor_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker BW = (BackgroundWorker)sender;
            for (int b = 200; b > 0; b -= 10)
            {
                BW.ReportProgress(b, e.Argument);
                Thread.Sleep(10);
            }
        }

        void RoundButtonRecolor_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            byte i = (byte)e.ProgressPercentage;
            ((Ellipse)e.UserState).Fill = new SolidColorBrush(Color.FromArgb((byte)(5 * (i / 4)), 0, (byte)(3 * i / 4), i));
        }



        private void RoundButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((Ellipse)((Grid)sender).Children[0]).Fill = new SolidColorBrush(Color.FromArgb(255, 15, 100, 150));
        }

        private void RoundButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ((Ellipse)((Grid)sender).Children[0]).Fill = new SolidColorBrush(Color.FromArgb(255, 0, 150, 200));
        }

        #endregion

        #region Main Menu Command

        private void MainMenuOpen_Click(object sender, RoutedEventArgs e)
        {
            Close();
            if (type == ItemType.Directory || (type == ItemType.Drive && dri.IsReady))
            {
                MainWindow.GoToPath = true;
                MainWindow.to_go_path = dri == null ? di.FullName : dri.Name;
            }
            else if (type == ItemType.Drive && !dri.IsReady)
                Messages.ShowMessage("Drive is not ready.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            else if (type == ItemType.File)
                try
                { Process.Start(new ProcessStartInfo(fi.FullName)); }
                catch (Exception ex)
                { Messages.ShowMessage("Sorry, we can not open the file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void MainMenuOpenWith_Click(object sender, RoutedEventArgs e)
        {
            Functions.OpenWith(fi != null ? fi.FullName : dri != null ? dri.Name : di.FullName, type);
        }

        private void RunAsAdministratorButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo PSI = new ProcessStartInfo(fi.FullName);
            PSI.Verb = "runas";
            try
            { Process.Start(PSI); }
            catch (Exception ex)
            { Messages.ShowMessage("Sorry, we can not open the file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        private void MainMenuMoveTo_Click(object sender, RoutedEventArgs e)
        {
            string copy_to = new Folder_Picker("Select directory to move to", true).ShowThisDialog();
            if (copy_to != null)
                CollectFiles(true, copy_to);
        }

        private void MainMenuCopyTo_Click(object sender, RoutedEventArgs e)
        {
            string copy_to = new Folder_Picker("Select directory to copy to", true).ShowThisDialog();
            if (copy_to != null)
                CollectFiles(false, copy_to);
        }

        private void CollectFiles(bool remove, string copy_to)
        {
            //////////List<FileOperationItem> to_copy = new List<FileOperationItem>();
            //////////if (type == ItemType.File)
            //////////    to_copy.Add(new FileOperationItem(fi, remove, false, fi.FullName.Remove(0, fi.DirectoryName.Length - 1)));
            //////////else if (type == ItemType.Directory)
            //////////    to_copy.Add(new FileOperationItem(di, ""));
            //////////else if (type == ItemType.Drive)
            //////////    to_copy.Add(new FileOperationItem(new DirectoryInfo(dri.Name), ""));
            //////////File_Operation window = new File_Operation();
            //////////window.CopyDialogShow(copy_to, to_copy);
            List<FileOperationItem> list = new List<FileOperationItem>();
            if (type == ItemType.Directory)
                list.Add(new FileOperationItem(di, remove, di.Name));
            else if (type == ItemType.Drive)
                list.Add(new FileOperationItem(dri.RootDirectory, remove, dri.RootDirectory.Name));
            else if (type == ItemType.File)
                list.Add(new FileOperationItem(fi, remove, fi.Name));
            if (list.Count > 0)
                new Operation().CopyDialogShow(list, OperationType.Copy, copy_to, false);
        }

        private void MainMenuDelete_Click(object sender, RoutedEventArgs e)
        {
            //Move to operation
            bool permanent = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            if (type != ItemType.Directory && type != ItemType.File)
                return;
            List<FileSystemInfo> items = new List<FileSystemInfo>() { (type == ItemType.Directory) ? di : (FileSystemInfo)fi };
            if (xps_document != null)
                try { xps_document.Close(); }
                catch (Exception) { }
            if (Functions.ConfirmDelete(items, permanent) && Functions.DeleteItems(items, permanent, this))
            {
                items[0].Refresh();
                if (!items[0].Exists)
                    Close();
            }
        }

        #endregion

        #region Controls Button Hide

        Duration hide_duration = new Duration(new TimeSpan(2500000));
        Duration show_duration = new Duration(new TimeSpan(1000000));
        int hide_after = 2500;
        bool is_mouse_on_controls = false;
        BackgroundWorker controler = new BackgroundWorker();

        void controler_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (!is_mouse_on_controls)
                    hide_after -= 500;
                else
                {
                    hide_after = 250;
                    controler.ReportProgress(1, e.Argument);
                }
                if (hide_after < 10)
                    controler.ReportProgress(-1, e.Argument);
                Thread.Sleep(499);
            }
        }

        void controler_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Grid g = (Grid)e.UserState;
            if (e.ProgressPercentage < 0 && g.Opacity > 0.1)
            {
                DoubleAnimation grid_opacity = new DoubleAnimation(0, hide_duration);
                g.BeginAnimation(Grid.OpacityProperty, grid_opacity);
                g.Visibility = Visibility.Hidden;
            }
            if (e.ProgressPercentage > 0 && g.Opacity < 0.1)
            {
                DoubleAnimation grid_opacity = new DoubleAnimation(1, show_duration);
                g.BeginAnimation(Grid.OpacityProperty, grid_opacity);
                g.Visibility = Visibility.Visible;
            }
        }

        #endregion

        private void RenameTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rename_button != null)
            {
                name_error.Visibility = Visibility.Hidden;
                if (rename_textbox.Text != String.Empty && rename_textbox.Text != item_name.Text)
                {
                    rename_button.Visibility = Visibility.Visible;
                    string[] forbidden_letters = { "*", "/", "\\", "\"", "?", "<", ">", "|", ":" };
                    foreach (string i in forbidden_letters)
                        if (rename_textbox.Text.Contains(i))
                        {
                            rename_textbox.Text = rename_textbox.Text.Replace(i, "");
                            name_error.Text = "File name can't contain any of these characters:\n" + @"'\' , '/' , ':' , '*' , '?' , '\' , '<' , '>' , '|'";
                            name_error.Visibility = Visibility.Visible;
                        }
                }
                else
                    rename_button.Visibility = Visibility.Hidden;
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (rename_textbox.Text.Length > 0 && rename_textbox.Text != "." && rename_textbox.Text[rename_textbox.Text.Length - 1] != '.' /*&& !File.Exists(dir.Parent.FullName + "\\" + name.Text) && !Directory.Exists(dir.Parent.FullName + "\\" + name.Text)*/)
            {
                try
                {
                    if (type == ItemType.Drive)
                        dri.VolumeLabel = rename_textbox.Text;
                    else if (!Functions.ConfirmRiskyItems(new string[] { type == ItemType.Directory ? di.FullName : fi.FullName }, "rename"))
                        return;
                    else if (type == ItemType.Directory)
                        di.MoveTo(di.Parent.FullName + "\\" + rename_textbox.Text);
                    else if (type == ItemType.File)
                        fi.MoveTo(fi.Directory.FullName + "\\" + rename_textbox.Text);
                    name_error.Visibility = Visibility.Collapsed;
                    rename_button.Visibility = Visibility.Hidden;
                    item_name.Text = "Name: " + rename_textbox.Text;
                }
                catch (Exception ex)
                {
                    name_error.Text = ex.Message;
                    name_error.Visibility = Visibility.Visible;
                }
            }
            else
            {
                name_error.Text = "You can not use this name!";
                name_error.Visibility = Visibility.Visible;
            }
        }

        private void Grid1_MouseOver(object sender, MouseEventArgs e)
        {

        }

        private void Grid1_MouseLeave(object sender, MouseEventArgs e)
        {

        }

        #region Attributes

        private void ReverseAttribute_Click(object sender, RoutedEventArgs e)
        {
            if (dri == null)
                if (fi == null) GetAttributes(di);
                else GetAttributes(fi);
            reverse.Visibility = Visibility.Collapsed;
        }

        private void NormalAttribute_Checked(object sender, RoutedEventArgs e)
        {
            reverse.Visibility = Visibility.Visible;
            read_only.IsChecked = false;
            hidden.IsChecked = false;
            archive.IsChecked = false;
            compressed.IsChecked = false;
            encrypted.IsChecked = false;
            system.IsChecked = false;
        }

        private void OtherAttributes_Checked(object sender, RoutedEventArgs e)
        {
            reverse.Visibility = Visibility.Visible;
            //archive.IsChecked = true;
        }

        private void AnyAttributes_Checked(object sender, RoutedEventArgs e)
        {
            reverse.Visibility = Visibility.Visible;
            other.IsChecked = true;
        }

        private void ApplyAttrepute_Click(object sender, RoutedEventArgs e)
        {
            bool?[] attributes = SelectedAttributes();
            List<FileOperationItem> list = new List<FileOperationItem>();
            if (fi != null)
                list.Add(new FileOperationItem(new FileInfo(fi.FullName), false, CalcFileAttributes(0, new FileInfo(fi.FullName).Attributes, attributes)));
            else
                list.Add(new FileOperationItem(new DirectoryInfo(di == null ? dri.Name : di.FullName), false, CalcFileAttributes(FileAttributes.Directory, new DirectoryInfo(di == null ? dri.Name : di.FullName).Attributes, attributes)));
            if (Functions.ConfirmRiskyItems(new string[] { ItemPath() }, "change the attributes of"))
                new Operation().CopyDialogShow(list, OperationType.Attributes, "", true);
            RefreshAttributes();
        }

        private void ApplyAttreputeToAllContent_Click(object sender, RoutedEventArgs e)
        {
            if (calc_directory_size != null)
                if (calc_directory_size.IsBusy)
                    Messages.ShowMessage("Sorry, the app is busy now, please try again later.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                else
                {
                    bool?[] attributes = SelectedAttributes();
                    if (loaded_attributes != null)
                    {
                        for (int i = 0; i < 7; i++)
                            if (attributes[i] == loaded_attributes[i])
                                attributes[i] = null;
                        if (attributes[7] == loaded_attributes[7])
                            attributes[7] = false;
                    }
                    if (!Functions.ConfirmRiskyItems(new string[] { ItemPath() }, "change the attributes of all content of"))
                        return;
                    List<FileOperationItem> list = new List<FileOperationItem>();
                    list.Add(new FileOperationItem(new DirectoryInfo(di == null ? dri.Name : di.FullName), false, CalcFileAttributes(FileAttributes.Directory, new DirectoryInfo(di == null ? dri.Name : di.FullName).Attributes, attributes)));
                    foreach (var i in directory_list)
                    {
                        i.short_path = CalcFileAttributes(FileAttributes.Directory, i.Item.Attributes, attributes);
                        list.Add(i);
                    }
                    foreach (var i in files_list)
                    {
                        i.short_path = CalcFileAttributes(0, i.Item.Attributes, attributes);
                        list.Add(i);
                    }
                    new Operation().CopyDialogShow(list, OperationType.Attributes, "", true);
                    RefreshAttributes();
                }
        }

        bool?[] SelectedAttributes()
        {
            bool? compressed_value = compressed.IsChecked, encrypted_value = encrypted.IsChecked;
            if (compressed_value != true && encrypted_value != true && not_encrypted_compressed.IsChecked != true)
                compressed_value = encrypted_value = null;
            bool?[] attributes = { read_only.IsChecked, hidden.IsChecked, system.IsChecked, archive.IsChecked, compressed_value, encrypted_value, not_content_indexed.IsChecked, normal.IsChecked };
            return attributes;
        }

        FileAttributes SetAttribute(FileAttributes attributes, FileAttributes flag, bool? value)
        {
            if (value == true)
                return attributes | flag;
            if (value == false)
                return attributes & ~flag;
            return attributes;
        }

        FileAttributes CalcFileAttributes(FileAttributes start, FileAttributes default_attributes, bool?[] selected_attributes)
        {
            if (selected_attributes[7] == true)
                return FileAttributes.Normal;
            FileAttributes new_attributes = start | (default_attributes & (FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive | FileAttributes.Temporary | FileAttributes.SparseFile | FileAttributes.ReparsePoint | FileAttributes.Compressed | FileAttributes.Offline | FileAttributes.NotContentIndexed | FileAttributes.Encrypted));
            new_attributes = SetAttribute(new_attributes, FileAttributes.ReadOnly, selected_attributes[0]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.Hidden, selected_attributes[1]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.System, selected_attributes[2]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.Archive, selected_attributes[3]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.Compressed, selected_attributes[4]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.Encrypted, selected_attributes[5]);
            new_attributes = SetAttribute(new_attributes, FileAttributes.NotContentIndexed, selected_attributes[6]);
            return new_attributes;
        }

        #endregion

        private void CreationChange_Click(object sender, RoutedEventArgs e)
        {
            if (dri == null)
                if (fi == null)
                {
                    DateTime? date = new ChangeDate().ShowDialog(di.CreationTimeUtc);
                    if (date.HasValue)
                        try
                        {
                            di.CreationTimeUtc = date.Value;
                            creation_time.Text = "Creation: " + di.CreationTime.ToString();
                        }
                        catch (Exception ex)
                        { Messages.ShowMessage("Sorry, we can not change creation time for this file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
                else
                {
                    DateTime? date = new ChangeDate().ShowDialog(fi.CreationTimeUtc);
                    if (date.HasValue)
                        try
                        {
                            fi.CreationTimeUtc = date.Value;
                            creation_time.Text = "Creation: " + fi.CreationTime.ToString();
                        }
                        catch (Exception ex)
                        { Messages.ShowMessage("Sorry, we can not change creation time for this directory.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
        }

        private void ModifiedChange_Click(object sender, RoutedEventArgs e)
        {
            if (dri == null)
                if (fi == null)
                {
                    DateTime? date = new ChangeDate().ShowDialog(di.LastWriteTimeUtc);
                    if (date.HasValue)
                        try
                        {
                            di.LastWriteTimeUtc = date.Value;
                            modified_time.Text = "Modified: " + di.LastWriteTime.ToString();
                        }
                        catch (Exception ex)
                        { Messages.ShowMessage("Sorry, we can not change last write time for this file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
                else
                {
                    DateTime? date = new ChangeDate().ShowDialog(fi.LastWriteTimeUtc);
                    if (date.HasValue)
                        try
                        {
                            fi.LastWriteTimeUtc = date.Value;
                            modified_time.Text = "Modified: " + fi.LastWriteTime.ToString();
                        }
                        catch (Exception ex)
                        { Messages.ShowMessage("Sorry, we can not change last write time for this directory.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
        }

        private void AccessedChange_Click(object sender, RoutedEventArgs e)
        {
            if (dri == null)
                if (fi == null)
                {
                    DateTime? date = new ChangeDate().ShowDialog(di.LastAccessTimeUtc);
                    if (date.HasValue)
                        try
                        {
                            di.LastAccessTimeUtc = date.Value;
                            last_accessed_time.Text = "Last accessed: " + di.LastAccessTime.ToString();
                        }
                        catch (Exception ex)
                        { Messages.ShowMessage("Sorry, we can not change last access time for this file.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
                else
                {
                    DateTime? date = new ChangeDate().ShowDialog(fi.LastAccessTimeUtc);
                    if (date.HasValue)
                        try
                        {
                            fi.LastAccessTimeUtc = date.Value;
                            last_accessed_time.Text = "Last accessed: " + fi.LastAccessTime.ToString();
                        }
                        catch (Exception ex)
                        { Messages.ShowMessage("Sorry, we can not change last access time for this directory.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CancelEveryThinig = true;
            if (xps_document != null)
                try { xps_document.Close(); }
                catch (Exception) { }
            if (shell_host != null)
                shell_host.Unload();
            ClearApps();
            foreach (BackgroundWorker bw in background_workers)
            {
                if (bw.IsBusy)
                    bw.CancelAsync();
            }
        }

        #region APP Bar

        private void Window_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (app_bar_stack.Visibility == Visibility.Visible && !(e.OriginalSource is Visual && app_can_open_this_type.IsAncestorOf((Visual)e.OriginalSource)))
                OpenCloseAPPBar();
        }

        private void OpenCloseAPPBar()
        {
            if (app_bar.Margin.Bottom > -30)
            {
                ThicknessAnimation ta = new ThicknessAnimation(new Thickness(0, 0, 0, -80), new Duration(new TimeSpan(1000000)));
                app_bar.Height = 110;
                app_bar.BeginAnimation(MarginProperty, ta);
            }
            else if (app_bar.Margin.Bottom < 0)
            {
                ThicknessAnimation ta = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(1000000)));
                app_bar.Height = 550;
                app_bar.BeginAnimation(MarginProperty, ta);
            }
        }

        private void app_bar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenCloseAPPBar();
            //تحذير رفع 10 بكسل
        }

        #endregion


        #region SubTitle

        List<SubtitleItem> subtitles;
        bool subtitle_active;
        bool Subtitle_Active
        {
            get { return subtitle_active; }
            set
            {
                if (value)
                    subtitle_text.Visibility = subtitle_shadow.Visibility = Visibility.Visible;
                else
                    subtitle_text.Visibility = subtitle_shadow.Visibility = Visibility.Collapsed;
                subtitle_active = value;
            }
        }
        int subtitle_id
        {
            get { return subtitle_id; }
            set { subtitle_id = value; }
        }

        private void LoadSubtitle_Click(object sender, RoutedEventArgs e)
        {
            if (video_player_element.CanPause)
            {
                video_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-play.png"));
                video_paly_button_image.ToolTip = "Play";
                video_player_element.Pause();
            }
            subtitles = new List<SubtitleItem>();
            System.Windows.Forms.OpenFileDialog j = new System.Windows.Forms.OpenFileDialog();
            if (j.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                StreamReader k = new StreamReader(j.OpenFile(), Encoding.UTF8);
                if (k.BaseStream.Length < 1048577)      //1 MB
                {
                    SubtitleItem.RestCount();
                    List<string> item = new List<string>();
                    while (!k.EndOfStream)
                    {
                        string s1 = k.ReadLine();
                        if (s1.Equals(string.Empty))
                        {
                            if ((!k.EndOfStream))
                            {
                                string s2 = k.ReadLine();
                                int m;
                                if (int.TryParse(s2, out m))
                                {
                                    subtitles.Add(new SubtitleItem(item));
                                    item.Clear();
                                    item.Add(s2);
                                }
                                else
                                    item.Add(s2);
                            }
                        }
                        else
                            item.Add(s1);
                    }
                    k.Close();
                    subtitles.Sort(SubtitlesSorter);
                    Subtitle_Active = true;
                }
            }
            VideoPlayPause_Click(sender, null);
        }

        private int SubtitlesSorter(SubtitleItem x, SubtitleItem y)
        {
            return x.start.CompareTo(y.start);
        }

        double subtitle_sync = 0;
        Encoding subtitle_encode = Encoding.UTF8;
        private void SubtitleOption_Click(object sender, RoutedEventArgs e)
        {
            if (video_player_element.CanPause)
            {
                video_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-play.png"));
                video_paly_button_image.ToolTip = "Play";
                video_player_element.Pause();
            }
            FontWithColor current_font = new FontWithColor(subtitle_text.FontFamily, text_foreground, subtitle_text.FontSize, subtitle_text.FontStyle.Equals(FontStyles.Italic), subtitle_text.FontWeight.Equals(FontWeights.Bold), Encoding.UTF8, subtitle_sync, subtitle_text.Margin.Left, subtitle_text.Margin.Bottom, text_background);
            var val = new SubtitleSetting().ShowWindow(current_font);
            subtitle_text.FontFamily = subtitle_shadow.FontFamily = new FontFamily(val.font_name.Name);
            subtitle_text.FontSize = subtitle_shadow.FontSize = val.font_size;
            text_foreground.Color = val.font_color.Color;
            text_background.Color = val.background_color.Color;
            subtitle_text.FontStyle = subtitle_shadow.FontStyle = val.italic ? FontStyles.Italic : FontStyles.Normal;
            subtitle_text.FontWeight = subtitle_shadow.FontWeight = val.bold ? FontWeights.Bold : FontWeights.Normal;
            VideoPlayPause_Click(sender, null);
            subtitle_sync = val.async_millisecond;
            subtitle_text.Margin = new Thickness(val.position.X, 0, val.position.X, val.position.Y);
            subtitle_shadow.Margin = new Thickness(val.position.X - 1.5, 0, val.position.X + 1.5, val.position.Y + 1.5);
        }

        #endregion



        private void MuteVolume_Click(object sender, RoutedEventArgs e)
        {
            if (video_player_element.Volume == 0)
                video_player_element.Volume = 0.75;
            else
                video_player_element.Volume = 0;
        }

    }

    class SubtitleItem
    {
        public static int count;
        public int ID;
        public long start;
        public long end;
        public string text;
        public bool error;

        public SubtitleItem(List<string> str)
        {
            string[] splitter = { "-->" };
            count++;
            ID = count;
            var time = str[1].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            if (time.Length > 1)
            {
                time[0] = time[0].Replace(" ", "");
                time[1] = time[1].Replace(" ", "");
                int u1, s1, m1, h1, u2, s2, m2, h2;
                int.TryParse(time[0].Substring(0, time[0].IndexOf(':')), out h1);
                int.TryParse(time[0].Substring(time[0].IndexOf(':') + 1, 2), out m1);
                int.TryParse(time[0].Substring(time[0].IndexOf(':') + 4, 2), out s1);
                int.TryParse(time[0].Substring(time[0].IndexOf(':') + 7, 3), out u1);

                int.TryParse(time[1].Substring(0, time[1].IndexOf(':')), out h2);
                int.TryParse(time[1].Substring(time[1].IndexOf(':') + 1, 2), out m2);
                int.TryParse(time[1].Substring(time[1].IndexOf(':') + 4, 2), out s2);
                int.TryParse(time[1].Substring(time[1].IndexOf(':') + 7, 3), out u2);
                start = (s1 + (m1 * 60) + (h1 * 3600)) * 1000 + u1;
                end = (s2 + (m2 * 60) + (h2 * 3600)) * 1000 + u2;
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 2; i < str.Count; i++)
                if (!str[i].Replace(" ", "").Equals(string.Empty) && !str.Equals("\r\n"))
                    sb.AppendLine(str[i]);
            text = sb.ToString(0, sb.Length - 2);
        }

        public static void RestCount()
        {
            count = 0;
        }
    }

    class SecurityRuleItem
    {
        public SecurityRuleItem(string identity, string type, string rights, string inherited)
        {
            Identity = identity;
            Type = type;
            Rights = rights;
            Inherited = inherited;
        }

        string identity;
        public string Identity
        {
            get { return identity; }
            set { identity = value; }
        }

        string type;
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        string rights;
        public string Rights
        {
            get { return rights; }
            set { rights = value; }
        }

        string inherited;
        public string Inherited
        {
            get { return inherited; }
            set { inherited = value; }
        }
    }

    class ShellPreviewHost : HwndHost
    {
        [ComImport, Guid("8895b1c6-b41f-4c1c-a562-0d564250836f"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IPreviewHandler
        {
            void SetWindow(IntPtr hwnd, ref RECT rect);
            void SetRect(ref RECT rect);
            void DoPreview();
            void Unload();
            void SetFocus();
            void QueryFocus(out IntPtr phwnd);
            [PreserveSig]
            int TranslateAccelerator(IntPtr pmsg);
        }

        [ComImport, Guid("b7d14566-0509-4cce-a71f-0a554233bd9b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IInitializeWithFile
        {
            void Initialize([MarshalAs(UnmanagedType.LPWStr)] string pszFilePath, uint grfMode);
        }

        [ComImport, Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IInitializeWithStream
        {
            void Initialize(System.Runtime.InteropServices.ComTypes.IStream pstream, uint grfMode);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int left, top, right, bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hwnd, out RECT rect);

        [DllImport("ole32.dll")]
        static extern int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        static extern int SHCreateStreamOnFileEx(string pszFile, uint grfMode, uint dwAttributes, bool fCreate, IntPtr pstmTemplate, out System.Runtime.InteropServices.ComTypes.IStream ppstm);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        static extern int AssocQueryString(uint flags, uint str, string pszAssoc, string pszExtra, StringBuilder pszOut, ref uint pcchOut);

        IntPtr host_window = IntPtr.Zero;
        IPreviewHandler handler;
        System.Runtime.InteropServices.ComTypes.IStream stream;

        internal static string QueryAssociation(uint what, string extension, string extra)
        {
            if (string.IsNullOrEmpty(extension))
                return null;
            uint length = 1024;
            StringBuilder result = new StringBuilder((int)length);
            try
            {
                if (AssocQueryString(0x4, what, extension, extra, result, ref length) == 0)
                    return result.ToString();
            }
            catch (Exception) { }
            return null;
        }

        internal static bool FindHandler(string extension, out Guid clsid)
        {
            clsid = Guid.Empty;
            string value = QueryAssociation(16, extension, "{8895b1c6-b41f-4c1c-a562-0d564250836f}");
            if (string.IsNullOrEmpty(value))
                return false;
            try
            {
                clsid = new Guid(value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            host_window = CreateWindowEx(0, "static", "", 0x40000000 | 0x10000000 | 0x02000000, 0, 0, Math.Max(1, (int)ActualWidth), Math.Max(1, (int)ActualHeight), hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            return new HandleRef(this, host_window);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            Unload();
            DestroyWindow(hwnd.Handle);
            host_window = IntPtr.Zero;
        }

        internal string Open(string path, Guid clsid)
        {
            if (host_window == IntPtr.Zero)
                return "the preview window is not ready.";
            object instance = null;
            try
            {
                Guid iid = new Guid("00000000-0000-0000-C000-000000000046");
                if (CoCreateInstance(ref clsid, IntPtr.Zero, 0x4, ref iid, out instance) != 0 || instance == null)
                    if (CoCreateInstance(ref clsid, IntPtr.Zero, 0x1, ref iid, out instance) != 0 || instance == null)
                        return "the preview handler could not be started.";
                if (instance is IInitializeWithFile)
                    ((IInitializeWithFile)instance).Initialize(path, 0);
                else if (instance is IInitializeWithStream)
                {
                    if (SHCreateStreamOnFileEx(path, 0x40, 0, false, IntPtr.Zero, out stream) != 0 || stream == null)
                        return "the file could not be opened.";
                    ((IInitializeWithStream)instance).Initialize(stream, 0);
                }
                else
                    return "the preview handler does not support files.";
                handler = instance as IPreviewHandler;
                if (handler == null)
                    return "the preview handler is not valid.";
                RECT rect;
                GetClientRect(host_window, out rect);
                handler.SetWindow(host_window, ref rect);
                handler.DoPreview();
                return null;
            }
            catch (Exception ex)
            {
                handler = null;
                ReleaseAll(instance);
                return ex.Message;
            }
        }

        void UpdateRect()
        {
            if (handler == null || host_window == IntPtr.Zero)
                return;
            try
            {
                RECT rect;
                GetClientRect(host_window, out rect);
                handler.SetRect(ref rect);
            }
            catch (Exception) { }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            Dispatcher.BeginInvoke(new Action(UpdateRect), System.Windows.Threading.DispatcherPriority.Background);
        }

        internal void Unload()
        {
            object instance = handler;
            handler = null;
            if (instance != null)
                try
                {
                    ((IPreviewHandler)instance).Unload();
                }
                catch (Exception) { }
            ReleaseAll(instance);
        }

        void ReleaseAll(object instance)
        {
            if (instance != null && Marshal.IsComObject(instance))
                try
                {
                    Marshal.FinalReleaseComObject(instance);
                }
                catch (Exception) { }
            if (stream != null)
            {
                try
                {
                    Marshal.FinalReleaseComObject(stream);
                }
                catch (Exception) { }
                stream = null;
            }
        }
    }

    enum OpenWithKind
    {
        Handler,
        Default,
        Other,
        FileManager,
        FileExplorer
    }

    class OpenWithApp
    {
        internal string Name, Id, Tip;
        internal ImageSource Icon;
        internal OpenWithKind Kind;
        internal bool IsDefault, IsStore;
        internal object Handler;

        internal string Title
        {
            get { return Name + (IsStore ? " (Store)" : "") + (IsDefault ? " (default)" : ""); }
        }
    }

    static class ShellAssociations
    {
        [ComImport, Guid("F04061AC-1659-4a3f-A954-775AA57FC083"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAssocHandler
        {
            [PreserveSig]
            int GetName([MarshalAs(UnmanagedType.LPWStr)] out string ppsz);
            [PreserveSig]
            int GetUIName([MarshalAs(UnmanagedType.LPWStr)] out string ppsz);
            [PreserveSig]
            int GetIconLocation([MarshalAs(UnmanagedType.LPWStr)] out string ppszPath, out int pIndex);
            [PreserveSig]
            int IsRecommended();
            [PreserveSig]
            int MakeDefault([MarshalAs(UnmanagedType.LPWStr)] string pszDescription);
            [PreserveSig]
            int Invoke(IntPtr pdo);
            [PreserveSig]
            int CreateInvoker(IntPtr pdo, out IntPtr ppInvoker);
        }

        [ComImport, Guid("973810ae-9599-4b88-9e4d-6ee98c9552da"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IEnumAssocHandlers
        {
            [PreserveSig]
            int Next(int celt, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IAssocHandler[] rgelt, out int pceltFetched);
        }

        [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IShellItem
        {
            [PreserveSig]
            int BindToHandler(IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct OPENASINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcszFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pcszClass;
            public int oaifInFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHAssocEnumHandlers(string pszExtra, int afFilter, out IEnumAssocHandlers ppEnumHandler);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHCreateItemFromParsingName(string pszPath, IntPtr pbc, [In] ref Guid riid, out IShellItem ppv);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHOpenWithDialog(IntPtr hwndParent, ref OPENASINFO poainfo);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern uint ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

        [DllImport("user32.dll")]
        static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

        internal static List<OpenWithApp> Handlers(string extension, string default_name, string default_exe, string default_id)
        {
            List<OpenWithApp> apps = new List<OpenWithApp>();
            if (string.IsNullOrEmpty(extension))
                return apps;
            IEnumAssocHandlers list = null;
            try
            {
                if (SHAssocEnumHandlers(extension, 1, out list) != 0 || list == null)
                    return apps;
                IAssocHandler[] one = new IAssocHandler[1];
                int fetched;
                while (apps.Count < 30 && list.Next(1, one, out fetched) == 0 && fetched == 1)
                {
                    IAssocHandler handler = one[0];
                    one[0] = null;
                    if (handler == null)
                        continue;
                    string name, ui_name, icon_path;
                    int icon_index;
                    if (handler.GetName(out name) != 0)
                        name = null;
                    if (handler.GetUIName(out ui_name) != 0 || string.IsNullOrEmpty(ui_name))
                        ui_name = name;
                    if (string.IsNullOrEmpty(ui_name))
                    {
                        Release(handler);
                        continue;
                    }
                    if (handler.GetIconLocation(out icon_path, out icon_index) != 0)
                    {
                        icon_path = null;
                        icon_index = 0;
                    }
                    OpenWithApp app = new OpenWithApp() { Name = ui_name, Id = name, Kind = OpenWithKind.Handler, Handler = handler };
                    app.IsStore = (icon_path != null && icon_path.StartsWith("@{")) || (name != null && name.IndexOf('\\') < 0 && name.IndexOf('!') > 0);
                    app.Icon = LoadIcon(string.IsNullOrEmpty(icon_path) ? name : icon_path, icon_index);
                    app.Tip = ui_name + (app.IsStore ? " (Microsoft Store app)" : "") + (string.IsNullOrEmpty(name) ? "" : "\n" + name);
                    apps.Add(app);
                }
            }
            catch (Exception) { }
            finally
            {
                Release(list);
            }
            OpenWithApp found = null;
            foreach (var i in new string[] { default_id, default_exe })
                if (found == null && !string.IsNullOrEmpty(i))
                    found = apps.FirstOrDefault(x => string.Equals(x.Id, i, StringComparison.OrdinalIgnoreCase));
            if (found == null && !string.IsNullOrEmpty(default_name))
                found = apps.FirstOrDefault(x => string.Equals(x.Name, default_name, StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                found.IsDefault = true;
                apps.Remove(found);
                apps.Insert(0, found);
            }
            return apps;
        }

        internal static ImageSource LoadIcon(string path, int index)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            try
            {
                if (path.StartsWith("@"))
                {
                    StringBuilder resolved = new StringBuilder(1024);
                    if (SHLoadIndirectString(path, resolved, resolved.Capacity, IntPtr.Zero) != 0 || !File.Exists(resolved.ToString()))
                        return null;
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.DecodePixelWidth = 32;
                    image.UriSource = new Uri(resolved.ToString());
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
                path = Environment.ExpandEnvironmentVariables(path.Trim('"'));
                IntPtr large, small;
                if (ExtractIconEx(path, index, out large, out small, 1) == 0)
                    return null;
                try
                {
                    IntPtr icon = small != IntPtr.Zero ? small : large;
                    if (icon == IntPtr.Zero)
                        return null;
                    BitmapSource source = Imaging.CreateBitmapSourceFromHIcon(icon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    return source;
                }
                finally
                {
                    if (large != IntPtr.Zero)
                        DestroyIcon(large);
                    if (small != IntPtr.Zero)
                        DestroyIcon(small);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string Invoke(object handler, string file)
        {
            IShellItem item = null;
            IntPtr data = IntPtr.Zero;
            try
            {
                Guid item_id = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), data_handler = new Guid("b8c0bd9f-ed24-455c-83e6-d5390c4fe8c4"), data_id = new Guid("0000010e-0000-0000-C000-000000000046");
                int result = SHCreateItemFromParsingName(file, IntPtr.Zero, ref item_id, out item);
                if (result != 0 || item == null)
                    return Marshal.GetExceptionForHR(result != 0 ? result : -2147467259).Message;
                result = item.BindToHandler(IntPtr.Zero, ref data_handler, ref data_id, out data);
                if (result != 0 || data == IntPtr.Zero)
                    return Marshal.GetExceptionForHR(result != 0 ? result : -2147467259).Message;
                result = ((IAssocHandler)handler).Invoke(data);
                return result == 0 ? null : Marshal.GetExceptionForHR(result).Message;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                if (data != IntPtr.Zero)
                    Marshal.Release(data);
                Release(item);
            }
        }

        internal static string OpenWithDialog(IntPtr owner, string file, bool set_default)
        {
            try
            {
                OPENASINFO info = new OPENASINFO() { pcszFile = file, pcszClass = null, oaifInFlags = set_default ? 0x1 | 0x2 | 0x4 | 0x8 : 0x1 | 0x2 | 0x4 };
                int result = SHOpenWithDialog(owner, ref info);
                return result == 0 || result == -2147023673 ? null : Marshal.GetExceptionForHR(result).Message;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        internal static void Release(object instance)
        {
            if (instance != null && Marshal.IsComObject(instance))
                try
                {
                    Marshal.ReleaseComObject(instance);
                }
                catch (Exception) { }
        }
    }
}
