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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;

namespace PreView
{
    /// <summary>
    /// Interaction logic for MusicPlayer.xaml
    /// </summary>
    public partial class MusicPlayer : Window
    {

        bool IsOpenned
        {
            get { return menu_is_openned; }
            set
            {
                if (value)
                {
                    file_button.IsEnabled = false;
                    //back_grid.Visibility = file_menu.Visibility = Visibility.Visible;

                    ThicknessAnimation f_rec_ani = new ThicknessAnimation(new Thickness(20, 4, 0, 0), new Duration(new TimeSpan(500000)));
                    f_rec_ani.Completed += (object sender, EventArgs e) =>
                    {
                        ThicknessAnimation s_rec_ani = new ThicknessAnimation(new Thickness(20, 9, 0, 0), new Duration(new TimeSpan(500000)));
                        s_rec_ani.Completed += (object sender2, EventArgs e2) =>
                        {
                            ThicknessAnimation t_rec_ani = new ThicknessAnimation(new Thickness(20, 14, 0, 0), new Duration(new TimeSpan(500000)));
                            t_rec_ani.Completed += (object sender3, EventArgs e3) =>
                            {
                                //for (int i = 1; i < hbrg_recs.Children.Count; i++)
                                //    hbrg_recs.Children[i].Visibility = Visibility.Collapsed;
                                //move arow
                                hbrg_recs.Children[0].Visibility = Visibility.Visible;
                                ThicknessAnimation arr_ani = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(750000)));
                                hbrg_recs.Children[0].BeginAnimation(MarginProperty, arr_ani);
                            };
                            hbrg_recs.Children[3].BeginAnimation(MarginProperty, t_rec_ani);
                        };
                        hbrg_recs.Children[2].BeginAnimation(MarginProperty, s_rec_ani);
                    };
                    hbrg_recs.Children[1].BeginAnimation(MarginProperty, f_rec_ani);

                    ThicknessAnimation menu_anim = new ThicknessAnimation(new Thickness(0), new Duration(new TimeSpan(1000000)));
                    menu_anim.Completed += (object sender, EventArgs e) => { file_button.IsEnabled = true; };
                    main_grid.BeginAnimation(MarginProperty, menu_anim);
                    //DoubleAnimation bb = new DoubleAnimation(40, 120, new Duration(new TimeSpan(10000)));
                    //hhh.BeginAnimation(WidthProperty, bb);
                    //content.BeginAnimation(MarginProperty, menu_anim);
                }
                else
                {
                    file_button.IsEnabled = false;
                    //menu_list.SelectedIndex = -1;
                    ThicknessAnimation arr_ani = new ThicknessAnimation(new Thickness(-40, 0, 0, 0), new Duration(new TimeSpan(500000)));
                    arr_ani.Completed += (object sender4, EventArgs e4) =>
                    {
                        hbrg_recs.Children[0].Visibility = Visibility.Hidden;
                        ThicknessAnimation f_rec_ani = new ThicknessAnimation(new Thickness(0, 14, 0, 0), new Duration(new TimeSpan(750000)));
                        f_rec_ani.Completed += (object sender, EventArgs e) =>
                        {
                            ThicknessAnimation s_rec_ani = new ThicknessAnimation(new Thickness(0, 9, 0, 0), new Duration(new TimeSpan(750000)));
                            s_rec_ani.Completed += (object sender2, EventArgs e2) =>
                            { hbrg_recs.Children[1].BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(0, 4, 0, 0), new Duration(new TimeSpan(750000)))); };
                            hbrg_recs.Children[2].BeginAnimation(MarginProperty, s_rec_ani);
                        };
                        //for (int i = 1; i < hbrg_recs.Children.Count; i++)
                        //    hbrg_recs.Children[i].Visibility = Visibility.Visible;
                        hbrg_recs.Children[3].BeginAnimation(MarginProperty, f_rec_ani);

                    };
                    hbrg_recs.Children[0].BeginAnimation(MarginProperty, arr_ani);



                    ThicknessAnimation menu_anim = new ThicknessAnimation(new Thickness(-100, 0, 0, 0), new Duration(new TimeSpan(1000000)));
                    menu_anim.Completed += (object sender, EventArgs e) =>
                    {
                        //back_grid.Visibility = file_menu.Visibility = Visibility.Collapsed;
                        file_button.IsEnabled = true;
                    };
                    main_grid.BeginAnimation(MarginProperty, menu_anim);
                    //content.BeginAnimation(MarginProperty, menu_anim);
                }
                menu_is_openned = value;
            }
        }

        MedaiState state = MedaiState.Unknown;
        MedaiState State
        {
            get { return state; }
            set
            {
                state = value;
                switch (value)
                {
                    case MedaiState.Play:
                        music.Play();
                        break;
                    case MedaiState.Pause:
                        music.Pause();
                        break;
                    case MedaiState.Stop:
                        music.Stop();
                        break;
                    case MedaiState.Failed:
                        caution.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        bool window_closed = false, stay_paused;

        bool video_is_on = true, allow_slider_to_move = true;

        bool menu_is_openned = false;

        public MusicPlayer()
        {
            InitializeComponent();
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = !IsOpenned;
        }

        private void Video_MediaOpened(object sender, RoutedEventArgs e)
        {
            //if have video: auto swich to video player
            state = MedaiState.Play;
            //Height = music.NaturalVideoHeight < SystemParameters.PrimaryScreenHeight - 96 ? music.NaturalVideoHeight + 64 : SystemParameters.PrimaryScreenHeight - 64;
            //Width = music.NaturalVideoWidth < SystemParameters.PrimaryScreenWidth - 64 ? music.NaturalVideoWidth + 24 : SystemParameters.PrimaryScreenWidth - 64;

            if (music.NaturalDuration.HasTimeSpan)
            {
                txt_total.Text = txt_total_shadow.Text = CovertTimeSpanToText(music.NaturalDuration.TimeSpan);

                BackgroundWorker slider_mover = new BackgroundWorker();
                slider_mover.WorkerReportsProgress = true;
                slider_mover.DoWork += (object sender1, DoWorkEventArgs arg) =>
                {
                    while (video_is_on && !window_closed)
                    {
                        Thread.Sleep(100);
                        while (State != MedaiState.Play)
                        {
                            Thread.Sleep(500);
                            if (window_closed)
                                break;
                        }
                        if (!window_closed)
                            slider_mover.ReportProgress(0);
                    }
                };
                slider_mover.ProgressChanged += (object sender1, ProgressChangedEventArgs arg) =>
                {
                    if (allow_slider_to_move)
                        slider.Value = (music.Position.TotalMilliseconds * 1000) / music.NaturalDuration.TimeSpan.TotalMilliseconds;
                    txt_position.Text = txt_position_shadow.Text = CovertTimeSpanToText(music.Position);

                  /*  if (Subtitle_Active)
                    {
                        int subtitle_id = SearchSubtitle(music.Position.TotalMilliseconds, 0, subtitles.Count - 1);
                        if (subtitle_id > -1 && subtitle_id < subtitles.Count)
                            subtitle_text.Text = subtitle_shadow.Text = subtitles[subtitle_id].text;
                        else
                            subtitle_text.Text = subtitle_shadow.Text = string.Empty;
                    }*/
                };
                slider_mover.RunWorkerAsync();
            }
        }

        private string CovertTimeSpanToText(TimeSpan time)
        {
            string text = "";
            if (time.Days > 0)
                text += time.Days + (time.Days == 1 ? " day  " : " days  ");
            if (time.Hours > 0)
                text += time.Hours + ":";
            text += (time.Minutes < 10 ? ("0" + time.Minutes) : time.Minutes.ToString()) + ":" + (time.Seconds < 10 ? ("0" + time.Seconds) : time.Seconds.ToString());
            return text;
        }

        private void Video_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            video_is_on = false;
            State = MedaiState.Failed;
        }

        private void Video_MediaEnded(object sender, RoutedEventArgs e)
        {
            State = MedaiState.Stop;
        }

        private void Btm_PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (State == MedaiState.Play)
            {
                if (music.CanPause)
                    State = MedaiState.Pause;
            }
            else if (State == MedaiState.Pause || State == MedaiState.Stop)
                State = MedaiState.Play;
        }

        private void Btm_Stop_Click(object sender, RoutedEventArgs e)
        {
            if (State == MedaiState.Play || State == MedaiState.Pause)
                State = MedaiState.Stop;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            window_closed = true;
        }

        private void Slider_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            allow_slider_to_move = false;
            stay_paused = State != MedaiState.Play;
            music.Pause();
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!allow_slider_to_move)
                if (music.NaturalDuration.HasTimeSpan)
                    music.Position = new TimeSpan(Convert.ToInt64(music.NaturalDuration.TimeSpan.Ticks * slider.Value / 1000));
        }


        private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            allow_slider_to_move = true;
            if (!stay_paused)
                music.Play();
        }

        internal void MakeMedaiPlayer(FileInfo file)
        {
            try
            {
                music.Source = new Uri(file.FullName);
                music.LoadedBehavior = MediaState.Manual;
                music.Play();
                int index;
                if ((index = file.Name.LastIndexOf(".")) > -1)
                    playing_song_name.Text = file.Name.Substring(0, index);
                else
                    playing_song_name.Text = file.Name;
                subtitle_active = false;
                try
                {
                    var files = file.Directory.GetFiles();
                    //int index_of_dot;
                    string media_name = Path.GetFileNameWithoutExtension(file.Name);
                    //if ((index_of_dot = file.Name.LastIndexOf('.')) > -1)
                    //    video_name = file.Name.Substring(0, index_of_dot);
                    //else
                    //    video_name = file.Name;

                    foreach (var i in files)
                    {
                        //search for other music
                        if (!i.Name.ToLower().Equals(file.Name.ToLower()))
                            if (IsSubtitle(i, media_name.ToLower()))
                            {
                                if (LoadSubtitle(i))
                                    break;
                            }
                    }
                }
                catch (Exception) { }
            }
            catch (Exception)
            { caution.Visibility = Visibility.Visible; }
        }


        #region SubTitle

        List<SubtitleItem> subtitles;

        bool subtitle_active;
        bool Subtitle_Active
        {
            get { return subtitle_active; }
            /*set
            {
                if (value)
                    subtitle_text.Visibility = subtitle_shadow.Visibility = Visibility.Visible;
                else
                    subtitle_text.Visibility = subtitle_shadow.Visibility = Visibility.Collapsed;
                subtitle_active = value;
            }*/
        }

        int subtitle_id;
        int SubtitleId
        {
            get { return subtitle_id; }
            set { subtitle_id = value; }
        }

        private bool LoadSubtitle(FileInfo subtitle_file)
        {
            
            System.Data.DataTable h = new System.Data.DataTable();
            try
            {
                XmlTextReader reader = new XmlTextReader(subtitle_file.OpenRead());
                subtitles = new List<SubtitleItem>();
                while (reader.Read())
                {
                    if (!reader.IsEmptyElement)
                        if(reader.NodeType==XmlNodeType.Text)

                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                while (reader.MoveToNextAttribute())
                                    MessageBox.Show(reader.Value, "att");
                                break;
                            case XmlNodeType.Text:
                                    MessageBox.Show(reader.Value, "text");
                                    MessageBox.Show(reader.Name, "text");
                                    break;
                            case XmlNodeType.EndElement:
                                break;
                        }
                }
                h.ReadXmlSchema(subtitle_file.OpenRead());
                h.ReadXml(subtitle_file.OpenRead());
                return true;
            }
            catch (Exception ex) { string f = ex.Message; return false; }
            //State = VideoState.Pause;
            //subtitles = new List<SubtitleItem>();
            //try
            //{
            //    StreamReader stream = new StreamReader(subtitle_file.OpenRead(), Encoding.UTF8);
            //    if (stream.BaseStream.Length < 1048577)      //1 MB
            //    {
            //        SubtitleItem.RestCount();
            //        List<string> item = new List<string>();
            //        while (!stream.EndOfStream)
            //        {
            //            string s1 = stream.ReadLine();
            //            if (s1.Equals(string.Empty))
            //            {
            //                if ((!stream.EndOfStream))
            //                {
            //                    string s2 = stream.ReadLine();
            //                    int m;
            //                    if (int.TryParse(s2, out m))
            //                    {
            //                        subtitles.Add(new SubtitleItem(item));
            //                        item.Clear();
            //                        item.Add(s2);
            //                    }
            //                    else
            //                        item.Add(s2);
            //                }
            //            }
            //            else
            //                item.Add(s1);
            //        }
            //        stream.Close();
            //        subtitles.Sort(SubtitlesSorter);
            //       /* Subtitle_Active = true;*/
            //        return true;
            //    }
            //    return false;
            //}
            //catch (Exception) { return false; }
            //State = VideoState.Play;
        }

        private int SubtitlesSorter(SubtitleItem x, SubtitleItem y)
        {
            return x.start.CompareTo(y.start);
        }

        double subtitle_sync = 0;
        Encoding subtitle_encode = Encoding.UTF8;

        private void SubtitleOption_Click(object sender, RoutedEventArgs e)
        {
            //if (video_player_element.CanPause)
            //{
            //    video_paly_button_image.Source = new BitmapImage(new Uri(@"pack://application:,,,/File Manager;component/Images/Menu/media-play.png"));
            //    video_paly_button_image.ToolTip = "Play";
            //    video_player_element.Pause();
            //}
            //FontWithColor current_font = new FontWithColor(subtitle_text.FontFamily, text_foreground, subtitle_text.FontSize, subtitle_text.FontStyle.Equals(FontStyles.Italic), subtitle_text.FontWeight.Equals(FontWeights.Bold), Encoding.UTF8, subtitle_sync, subtitle_text.Margin.Left, subtitle_text.Margin.Bottom, text_background);
            //var val = new SubtitleSetting().ShowWindow(current_font);
            //subtitle_text.FontFamily = subtitle_shadow.FontFamily = new FontFamily(val.font_name.Name);
            //subtitle_text.FontSize = subtitle_shadow.FontSize = val.font_size;
            //text_foreground.Color = val.font_color.Color;
            //text_background.Color = val.background_color.Color;
            //subtitle_text.FontStyle = subtitle_shadow.FontStyle = val.italic ? FontStyles.Italic : FontStyles.Normal;
            //subtitle_text.FontWeight = subtitle_shadow.FontWeight = val.bold ? FontWeights.Bold : FontWeights.Normal;
            //VideoPlayPause_Click(sender, null);
            //subtitle_sync = val.async_millisecond;
            //subtitle_text.Margin = new Thickness(val.position.X, 0, val.position.X, val.position.Y);
            //subtitle_shadow.Margin = new Thickness(val.position.X - 1.5, 0, val.position.X + 1.5, val.position.Y + 1.5);
        }

        private void TextBox_FindInSubtitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).Text == "")
                txt_placeholder.Visibility = Visibility.Visible;
            else
                txt_placeholder.Visibility = Visibility.Hidden; ;
        }

        private bool IsSubtitle(FileInfo subtitle_file, string media_file_name)
        {
            //all subtitle format
            //".srt", ".sub", ".ssa", ".ass", ".smi", ".psb", ".idx",".usf",".xss",".rt",".sup",".cdg",".utf",".aqt",".jss",".txt",".smil",".stl",".dks",".pjs",".mpl2",".mks",".vtt" 

            //common subtitle format
            //".srt", ".ass", 

            string[] subtitle_format = { ".xml"/*".srt", ".ass"*/ };
            XmlDataProvider m = new XmlDataProvider();
            foreach (string i in subtitle_format)
                if (subtitle_file.Name.ToLower().Equals(media_file_name + i))
                    return true;
            return false;
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

        #endregion
    }
}
