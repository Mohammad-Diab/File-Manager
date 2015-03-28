using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
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
            creation_time.Text = "Creation: " + file.CreationTimeUtc.ToString();
            modified_time.Text = "Modified: " + file.LastWriteTimeUtc.ToString();
            last_accessed_time.Text = "Last accessed: " + file.LastAccessTime.ToString();
            //Create flowshart;
            GetAttributes(file);
            reverse.Visibility = Visibility.Collapsed;
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
        }

        #region File Region

        private void InitializeForFile(FileInfo file)
        {
            //Information Tab
            PrimeInitialize(file);
            change_name_expander.IsEnabled = attribute_grid.IsEnabled = security_tab.IsEnabled = !file.IsReadOnly;
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

            //Atribute Tab
            apply_to_all.Visibility = Visibility.Collapsed;

            //Main menu and Tabs
            if ((!FileInfoCombonents.FilesInformation.ContainsKey(file.Extension.ToLower())) || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Unknown || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.CannotOpen)
            {
                preview_tab.IsEnabled = false;
                TabItem_MouseLeftButtonUp(information_tab, null);
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 89, 0, 0);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Application)
            {
                preview_tab.IsEnabled = false;
                TabItem_MouseLeftButtonUp(information_tab, null);
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
                preview_tab.IsEnabled = false;
                TabItem_MouseLeftButtonUp(information_tab, null);                    //may then remove it to standalone condition to explore archive file
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
                    MakeRTFViewer(file);
                else if (file.Extension.ToLower() == ".xps" || file.Extension.ToLower() == ".oxps")
                    MakeXPSViewer(file);
                else
                {
                    preview_tab.IsEnabled = false;
                    TabItem_MouseLeftButtonUp(information_tab, null);
                }
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 89, 0, 0);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Text || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.TextCanRunAsApplication || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Html)
            {
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 57, 0, 0);
                if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Html)
                    MakeHtmlViewer(file);
                else
                    MakeTextViewer(file);
                if (file.Extension.ToLower() == ".mht" || file.Extension.ToLower() == ".mhtml")
                {
                    explore_button.ToolTip = "Extract files from archive";
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/extract-archive.png"));
                    explore_button.Click += ExtractArchiveButton_Click;
                }
                else
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
                    MakeImageViewer(file);
                else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Vedio)
                    MakeVedioViewer(file);
                else
                    MakeAudioViewer(file);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Font)
            {
                MakeFontViewer(file);
                explore_button.ToolTip = "Install font";
                explore_button.Click += InstallFontButton_Click;
                explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/font-install.png"));
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 57, 0, 0);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.DiskImage)
            {
                preview_tab.IsEnabled = false;
                TabItem_MouseLeftButtonUp(information_tab, null);
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 57, 0, 0);
                if (file.Extension.ToLower() == ".isz")
                {
                    explore_button.ToolTip = "Convert to ISO";
                    explore_button.Click += ConvertToISOButton_Click;
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/covert-to-iso.png"));
                }
                else if (file.Extension.ToLower() == ".mou")
                {
                    explore_button.ToolTip = "Extract files from archive";
                    explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/extract-archive.png"));
                    explore_button.Click += ExtractArchiveButton_Click;
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
                special_function.Visibility = Visibility.Visible;
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
            ((Ellipse)((Grid)sender).Children[1]).Opacity = 1;
            ((Ellipse)((Grid)sender).Children[2]).Opacity = 1;
        }

        private void SliderMoverElement_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Ellipse)((Grid)sender).Children[1]).Opacity = 0.8;
            ((Ellipse)((Grid)sender).Children[2]).Opacity = 0.8;
            slider_old_point = e.GetPosition(null);
            allow_move_slider = true;
        }

        private void SliderMoverElement_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            allow_move_slider = false;
            Grid g = ((Grid)((Grid)sender).Children[2]);
            ((Ellipse)g.Children[2]).Opacity = 1;
            ((Ellipse)g.Children[2]).Opacity = 1;
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
                System.Threading.Thread.Sleep(100);
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
                image_view.Source = new BitmapImage(new Uri(file.FullName));
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

        void get_image_neighbor_DoWork(object sender, DoWorkEventArgs e)
        {
            neighbors = new List<FileInfo>();
            FileInfo file = ((FileInfo)e.Argument);
            neighbors.Add(file);
            foreach (var i in file.Directory.GetFiles())
                if (i.FullName != file.FullName && FileInfoCombonents.FilesInformation.ContainsKey(i.Extension.ToLower()) && FileInfoCombonents.FilesInformation[i.Extension.ToLower()].Category == FileCategory.Image)
                {
                    neighbors.Add(i);
                    Thread.Sleep(2);
                }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                DoubleAnimation grid_opacity = new DoubleAnimation(1, show_duration);
                image_control.BeginAnimation(Grid.OpacityProperty, grid_opacity);
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

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {

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
            if (e.Key == Key.NumPad4 && current_item_in_viewer > 0)
            {
                current_item_in_viewer--;
                try
                {
                    image_view.Source = new BitmapImage(new Uri(neighbors[current_item_in_viewer].FullName));
                    ThicknessAnimation ta = new ThicknessAnimation(new Thickness(-798, 2, 2, 2), new Thickness(2, 2, 2, 2), show_duration);
                    image_view.BeginAnimation(MarginProperty, ta);
                }
                catch (Exception) { }
            }
            else if (e.Key == Key.NumPad6 && current_item_in_viewer < neighbors.Count - 2)
            {
                current_item_in_viewer++;
                try
                {
                    image_view.Source = new BitmapImage(new Uri(neighbors[current_item_in_viewer].FullName));
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

        private void MakeXPSViewer(FileInfo file)
        {
            DocumentViewer doc_viewer = new DocumentViewer();
            doc_viewer.Margin = new Thickness(0, 0, 12, 0);
            try
            {
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
                preview_grid.Children.Add(doc_viewer);
                XpsDocument XPS = new XpsDocument(file.FullName, FileAccess.Read);
                doc_viewer.Document = XPS.GetFixedDocumentSequence();
                TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            }
            catch (Exception ex)
            { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
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
            { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void MakeTextViewer(FileInfo file)
        {
            RichTextBox rich = new RichTextBox();
            rich.Margin = new Thickness(0, 0, 12, 0);
            rich.IsReadOnly = true;
            try
            {
                StreamReader SR = new StreamReader(file.FullName);
                rich.Document = new FlowDocument(new Paragraph(new Run(SR.ReadToEnd())));
                SR.Close();
            }
            catch (Exception ex)
            { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
            rich.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            preview_grid.Children.Insert(0, rich);
        }

        private void MakeFontViewer(FileInfo file)
        {
            //font name and version
            font_viewer.Visibility = Visibility.Visible;
            ((ListBox)font_viewer.Children[0]).FontFamily = new FontFamily(new Uri(file.FullName), "Name"); //wrong
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

            app_can_open_this_type.Items.Add("File Manager");
            app_can_open_this_type.Items.Add("File Explorer");
            app_can_open_this_type.Items.Add("Windows Explorer");

            item_image.Source = FileInfoCombonents.FilesInformation["directory"].Bitmap256;

            //Atribute Tab
            directory_attribute.Text = "Directory";

            preview_tab.IsEnabled = false;
            hash_tab.IsEnabled = false;
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
            //catch (Exception ex) {/* new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();*/ }
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
            var d = directory.GetDirectories();
            foreach (var i in d)
            {
                directory_list.Add(new FileOperationItem(i, false, null));
                try
                { GetSubFilesAndDirectories(i); }
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
            app_can_open_this_type.Items.Add("File Manager");
            app_can_open_this_type.Items.Add("File Explorer");
            app_can_open_this_type.Items.Add("Windows Explorer");
            time_region.Visibility = Visibility.Hidden;
            item_image.Source = Functions.GetDriveImage(driveInfo.DriveType, driveInfo.Name.ToLower().Equals(Functions.SystemDrive), driveInfo.IsReady, 256);

            //main menu and tabs
            move_to_button.Visibility = delete_button.Visibility = Visibility.Collapsed;
            preview_tab.IsEnabled = false;
            hash_tab.IsEnabled = false;
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
            { Process.Start(new ProcessStartInfo("Tools\\notepad++.exe", fi.FullName)); }
            catch (Exception ex) { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void ConvertToISOButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //password and exit code message box (event 'exited')
                System.Windows.Forms.SaveFileDialog SFD = new System.Windows.Forms.SaveFileDialog();
                SFD.Filter = "ISO File (*.iso)|*.iso";
                if (SFD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Process.Start(new ProcessStartInfo("Tools\\unisz.exe", " \"" + fi.FullName + "\""));
                    File.Move(fi.FullName.Remove(fi.FullName.Length - 1) + "o", SFD.FileName);
                }
            }
            catch (Exception ex) { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void InstallFontButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo PSI = new ProcessStartInfo("Tools\\InstallFonts.exe", "\"" + fi.FullName + "\"");
                PSI.Verb = "runas";
                var P = Process.Start(PSI);
                P.WaitForExit(int.MaxValue);
                if (P.ExitCode == 1)
                    new My_MessageBox("Install font", "Font has been installed successfully", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav")).ShowDialog();
                else
                    new My_MessageBox("Install Fonts", "Something wrong while installing font.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            }
            catch (Exception ex) { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
        }

        private void ExtractArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                ProcessStartInfo PSI;
                Dictionary<int, string> exit_code = new Dictionary<int, string>(6);
                switch (fi.Extension.ToLower())
                {
                    case ".uha":
                        PSI = new ProcessStartInfo("Tools\\UHARC.exe", " x \"" + path + "\" \"" + path + "\"");
                        exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                        break;
                    case ".kgb":
                        PSI = new ProcessStartInfo("Tools\\KGB.exe", " x \"" + path + "\" \"" + path + "\"");
                        exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                        break;
                    case ".mht":
                        PSI = new ProcessStartInfo("Tools\\extractMHT.exe", " \"" + path + "\" \"" + path + "\"");
                        exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                        break;
                    case ".mhtml":
                        PSI = new ProcessStartInfo("Tools\\extractMHT.exe", " \"" + path + "\" \"" + path + "\"");
                        exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                        break;
                    case ".mou":
                        PSI = new ProcessStartInfo("Tools\\WinMount\\winmount", " -e \"" + path + "\" -s:\"" + path + "\"");
                        exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                        break;
                    default:
                        PSI = new ProcessStartInfo("Tools\\7z.exe", " x \"" + path + "\" -o\"" + path + "\"");
                        exit_code.Add(0, "Complete"); exit_code.Add(1, "Waring: Something maybe not complete successfuly."); exit_code.Add(2, "Fatal error"); exit_code.Add(7, "Command line error"); exit_code.Add(8, "No enough memory "); exit_code.Add(255, "You stopped the process");
                        break;
                }
                try
                {
                    Process process = System.Diagnostics.Process.Start(PSI);
                    process.WaitForExit(int.MaxValue);
                    exit_code = new Dictionary<int, string>(6);
                    new My_MessageBox("Extract archive", exit_code[process.ExitCode], new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
                }
                catch (Exception ex)
                {
                    new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
                    Close();
                }
            }
        }

        private void ExplorerArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            //catch Error massages
            try
            {
                if (fi.Extension.ToLower() == ".uha")
                    Process.Start(new ProcessStartInfo("Tools\\WinUha.exe", "\"" + fi.FullName + "\""));
                else if (fi.Extension.ToLower() == ".kgb")
                    Process.Start(new ProcessStartInfo("Tools\\KGB.exe", "\"" + fi.FullName + "\""));
                else
                    Process.Start(new ProcessStartInfo("Tools\\7zFM.exe", "\"" + fi.FullName + "\""));
            }
            catch (Exception ex)
            { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
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
            ThicknessAnimation an = new ThicknessAnimation(new Thickness(tab_control.Children.IndexOf(((Label)sender)) * -800, 0, 0, 0), new Duration(new TimeSpan(1000000)));
            mother_of_all_grid.BeginAnimation(Grid.MarginProperty, an);
        }

        #endregion

        #region Main Menu Apperance

        void MainMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            if (main_menu.Margin.Left < -60)
            {
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
                new My_MessageBox("File Manager", "Drive is not ready!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
            else if (type == ItemType.File)
                try
                { System.Diagnostics.Process.Start(new ProcessStartInfo(fi.FullName)); }
                catch (Exception ex)
                { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
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
            { System.Diagnostics.Process.Start(PSI); }
            catch (Exception ex)
            { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
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
                new Operation().CopyDialogShow(list, Op.Copy, copy_to, false);
        }

        private void MainMenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (new My_MessageBox((type == ItemType.Directory) ? (FileSystemInfo)di : (FileSystemInfo)fi).ShowThisDialog() == MyMessageBoxResult.Yes)
                try
                {
                    if (type == ItemType.Directory)
                        di.Delete(true);
                    else
                        if (!fi.IsReadOnly && fi.Exists)
                            fi.Delete();
                }
                catch (Exception ex)
                { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
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
                if (type == ItemType.Directory)
                    di.MoveTo(di.Parent.FullName + "\\" + rename_textbox.Text);
                else if (type == ItemType.File)
                    fi.MoveTo(fi.Directory.FullName + "\\" + rename_textbox.Text);
                name_error.Visibility = Visibility.Collapsed;
                rename_button.Visibility = Visibility.Hidden;
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
            bool?[] attributes = { read_only.IsChecked, hidden.IsChecked, system.IsChecked, archive.IsChecked, compressed.IsChecked, encrypted.IsChecked, not_content_indexed.IsChecked, normal.IsChecked };
            List<FileOperationItem> list = new List<FileOperationItem>();
            if (fi != null)
                list.Add(new FileOperationItem(new FileInfo(fi.FullName), false, CalcFileAttributes(FileAttributes.Directory, new FileInfo(fi.FullName).Attributes, attributes)));
            else
                list.Add(new FileOperationItem(new DirectoryInfo(di == null ? dri.Name : di.FullName), false, CalcFileAttributes(FileAttributes.Directory, new DirectoryInfo(di == null ? dri.Name : di.FullName).Attributes, attributes)));
            new Operation().CopyDialogShow(list, Op.Attributes, "", true);
        }

        private void ApplyAttreputeToAllContent_Click(object sender, RoutedEventArgs e)
        {
            if (calc_directory_size != null)
                if (calc_directory_size.IsBusy)
                    new My_MessageBox("Apply attributes", "App is busy now please try again later.", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
                else
                {
                    bool?[] attributes = { read_only.IsChecked, hidden.IsChecked, system.IsChecked, archive.IsChecked, compressed.IsChecked, encrypted.IsChecked, not_content_indexed.IsChecked, normal.IsChecked };
                    List<FileOperationItem> list = new List<FileOperationItem>();
                    list.Add(new FileOperationItem(new DirectoryInfo(di == null ? dri.Name : di.FullName), false, CalcFileAttributes(0, new DirectoryInfo(di == null ? dri.Name : di.FullName).Attributes, attributes)));
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
                    new Operation().CopyDialogShow(list, Op.Attributes, "", true);
                }
        }

        FileAttributes CalcFileAttributes(FileAttributes start, FileAttributes default_attributes, bool?[] selected_attributes)
        {
            FileAttributes new_attributes = start;
            if (selected_attributes[0] == true)
                new_attributes += 1;
            if (selected_attributes[1] == true)
                new_attributes += 2;
            if (selected_attributes[2] == true)
                new_attributes += 4;
            if (selected_attributes[3] == true)
                new_attributes += 32;
            if (default_attributes.HasFlag(FileAttributes.Temporary))
                new_attributes += 256;
            if (default_attributes.HasFlag(FileAttributes.SparseFile))
                new_attributes += 512;
            if (default_attributes.HasFlag(FileAttributes.ReparsePoint))
                new_attributes += 1024;
            if (selected_attributes[4] == true)
                new_attributes += 2048;
            if (default_attributes.HasFlag(FileAttributes.Offline))
                new_attributes += 4096;
            if (selected_attributes[5] == true)
                new_attributes += 16384;
            if (selected_attributes[6] == true)
                new_attributes += 8192;
            if (selected_attributes[7].Value)
                new_attributes = FileAttributes.Normal;
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
                            creation_time.Text = "Creation: " + di.CreationTimeUtc.ToString();
                        }
                        catch (Exception ex)
                        { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
                }
                else
                {
                    DateTime? date = new ChangeDate().ShowDialog(fi.CreationTimeUtc);
                    if (date.HasValue)
                        try
                        {
                            fi.CreationTimeUtc = date.Value;
                            creation_time.Text = "Creation: " + fi.CreationTimeUtc.ToString();
                        }
                        catch (Exception ex)
                        { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
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
                            modified_time.Text = "Modified: " + di.LastWriteTimeUtc.ToString();
                        }
                        catch (Exception ex)
                        { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
                }
                else
                {
                    DateTime? date = new ChangeDate().ShowDialog(fi.LastWriteTimeUtc);
                    if (date.HasValue)
                        try
                        {
                            fi.LastWriteTimeUtc = date.Value;
                            modified_time.Text = "Modified: " + fi.LastWriteTimeUtc.ToString();
                        }
                        catch (Exception ex)
                        { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
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
                            last_accessed_time.Text = "Last accessed: " + di.LastAccessTimeUtc.ToString();
                        }
                        catch (Exception ex)
                        { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
                }
                else
                {
                    DateTime? date = new ChangeDate().ShowDialog(fi.LastAccessTimeUtc);
                    if (date.HasValue)
                        try
                        {
                            fi.LastAccessTimeUtc = date.Value;
                            last_accessed_time.Text = "Last accessed: " + fi.LastAccessTimeUtc.ToString();
                        }
                        catch (Exception ex)
                        { new My_MessageBox("File Manager", ex.Message, new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog(); }
                }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            CancelEveryThinig = true;
            foreach (BackgroundWorker bw in background_workers)
            {
                if (bw.IsBusy)
                    bw.CancelAsync();
            }
        }

        #region APP Bar

        private void Window_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
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

        #endregion

        private void SubtitleOption_Click(object sender, RoutedEventArgs e)
        {
            if (video_player_element.CanPause)
            {
                video_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-play.png"));
                video_paly_button_image.ToolTip = "Play";
                video_player_element.Pause();
            }
            FontWithColor k = new FontWithColor(subtitle_text.FontFamily, subtitle_text.Foreground, subtitle_text.FontSize, subtitle_text.FontStyle.Equals(FontStyles.Italic), subtitle_text.FontWeight.Equals(FontWeights.Bold));
            var val = new Subtitle_Option().ShowWindow(k);
            subtitle_text.FontFamily = subtitle_shadow.FontFamily = new System.Windows.Media.FontFamily(val.font_name.Name);
            subtitle_text.FontSize = subtitle_shadow.FontSize = val.font_size;
            subtitle_text.Foreground = val.font_color;
            subtitle_text.FontStyle = subtitle_shadow.FontStyle = val.italic ? FontStyles.Italic : FontStyles.Normal;
            subtitle_text.FontWeight = subtitle_shadow.FontWeight = val.bold ? FontWeights.Bold : FontWeights.Normal;
            VideoPlayPause_Click(sender, null);
        }

        private void MuteVolume_Click(object sender, RoutedEventArgs e)
        {
            if (video_player_element.Volume == 0)
                video_player_element.Volume = 75;
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
}
