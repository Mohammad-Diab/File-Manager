using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Preview.xaml
    /// </summary>
    public partial class Preview : Window
    {
        //DriveInfo dr;
        //FileInfo fi;
        //DirectoryInfo di;
        int main_menu_item_count = 7;
        List<FileInfo> neighbors;
        int current_item_in_viewer = 0;
        public Preview()
        {
            //delete after testing
            BinaryFormatter BF = new BinaryFormatter();
            List<BasicFileInfoCombonents> info = (List<BasicFileInfoCombonents>)BF.Deserialize(File.Open("FileInfo.dat", FileMode.OpenOrCreate));
            BasicFileInfoCombonents.ConvertToStandard(info);
            //
            InitializeComponent();
            ShowDialog(new FileInfo(@"C:\Users\pal\Documents\media-skip-backward.mp3"), ItemType.File);
        }

        #region Start Window

        internal void ShowDialog(object Item, ItemType type)
        {
            switch (type)
            {
                case ItemType.Drive:
                    Drive((DriveInfo)Item);
                    ShowDialog();
                    break;
                case ItemType.Directory:
                    Directory((DirectoryInfo)Item);
                    ShowDialog();
                    break;
                case ItemType.File:
                    InitializeForFile((FileInfo)Item);
                    controler.DoWork += controler_DoWork;
                    controler.WorkerReportsProgress = true;
                    controler.ProgressChanged += controler_ProgressChanged;
                    //ShowDialog();
                    break;
                default:
                    break;
            }
        }

        private void InitializeForFile(FileInfo file)
        {
            //Information Tab
            PrimeInitialize(file);
            change_name_expander.IsEnabled = attribute_tab.IsEnabled = security_tab.IsEnabled = !file.IsReadOnly;
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
            item_size.Text = Functions.FileSize(file.Length);
            item_ondisk_size.Text = "on disk: " + Functions.FileSize(file.Length);
            directory_content.Text = "This is a file.";
            item_image.Source = FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Bitmap256;

            //Atribute Tab
            apply_to_all.Visibility = Visibility.Collapsed;

            //Main menu and Tabs
            if ((!FileInfoCombonents.FilesInformation.ContainsKey(file.Extension.ToLower())) || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Unknown || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.CannotOpen)
            {
                preview_tab.IsEnabled = false;
                TabItem_MouseLeftButtonUp(information_tab, null);
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 99, 0, 0);
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
                    open_button.Margin = new Thickness(0, 99, 0, 0);
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
                    open_button.Margin = new Thickness(0, 67, 0, 0);
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
                    MakeRTFViewer();
                else if (file.Extension.ToLower() == ".xps" || file.Extension.ToLower() == ".oxps")
                    MakeXPSViewer();
                else
                {
                    preview_tab.IsEnabled = false;
                    TabItem_MouseLeftButtonUp(information_tab, null);
                }
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 99, 0, 0);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Text || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.TextCanRunAsApplication || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Html)
            {
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 67, 0, 0);
                if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Html)
                    MakeHtmlViewer();
                else
                    MakeTextViewer();
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
                    open_button.Margin = new Thickness(0, 15, 0, 0);
                }
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Audio || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Vedio || FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Image)
            {
                extract_button.Visibility = Visibility.Collapsed;
                explore_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 99, 0, 0);
                if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Image)
                    MakeImageViewer(file);
                else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Vedio)
                    MakeVedioViewer(file);
                else
                    MakeAudioViewer(file);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.Font)
            {
                MakeFontViewer();
                explore_button.ToolTip = "Install font";
                explore_button.Click += InstallFontButton_Click;
                explore_button_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Menu/font-install.png"));
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 67, 0, 0);
            }
            else if (FileInfoCombonents.FilesInformation[file.Extension.ToLower()].Category == FileCategory.DiskImage)
            {
                preview_tab.IsEnabled = false;
                TabItem_MouseLeftButtonUp(information_tab, null);
                extract_button.Visibility = Visibility.Collapsed;
                open_button.Margin = new Thickness(0, 67, 0, 0);
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
                    open_button.Margin = new Thickness(0, 99, 0, 0);
                }
            }
            main_menu_item_count = main_menu_stack.Children.Count;
        }

        private void ConvertToISOButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void InstallFontButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MakeFontViewer()
        {
            throw new NotImplementedException();
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
            slider_mover.WorkerReportsProgress = true;
            slider_mover.DoWork += AudioSliderMover_DoWork;
            slider_mover.ProgressChanged += AudioSliderMover_ProgressChanged;
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
            if (neighbors.Count > current_item_in_viewer)
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
            if (neighbors.Count == null)
            {
                neighbors = new List<FileInfo>();
                special_function.Visibility = Visibility.Visible;
                BackgroundWorker get_audio_neighbor = new BackgroundWorker();
                get_audio_neighbor.DoWork += GetAudioNeighbor_DoWork;
                get_audio_neighbor.WorkerReportsProgress = true;
                get_audio_neighbor.ProgressChanged += GetAudioNeighbor_ProgressChanged;
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
            slider_mover.WorkerReportsProgress = true;
            slider_mover.DoWork += VideoSliderMover_DoWork;
            slider_mover.ProgressChanged += VideoSliderMover_ProgressChanged;
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
        }

        private void VideoPlayPause_Click(object sender, RoutedEventArgs e)
        {
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void MakeTextViewer()
        {
            //throw new NotImplementedException();
        }

        private void MakeHtmlViewer()
        {
            throw new NotImplementedException();
        }

        private void MakeXPSViewer()
        {
            throw new NotImplementedException();
        }

        private void MakeRTFViewer()
        {
            throw new NotImplementedException();
        }

        private void ExtractArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ExplorerArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RunAsAdministratorButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
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
            //Create flow shart;
        }

        private void Directory(DirectoryInfo directoryInfo)
        {
            throw new NotImplementedException();
        }

        private void Drive(DriveInfo driveInfo)
        {
            throw new NotImplementedException();
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

        private void MainMenu_Open_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainMenu_OpenWith_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainMenu_MoveTo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainMenu_CopyTo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MainMenu_Delete_Click(object sender, RoutedEventArgs e)
        {

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
                            name_error.Visibility = Visibility.Visible;
                        }
                }
                else
                    rename_button.Visibility = Visibility.Hidden;
            }
        }

        private void Grid1_MouseOver(object sender, MouseEventArgs e)
        {

        }

        private void Grid1_MouseLeave(object sender, MouseEventArgs e)
        {

        }











        









    }
}

