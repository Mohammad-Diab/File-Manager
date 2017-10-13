using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.IO;

namespace Music_Player
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, EventArgs e)
        {
            text01.Visibility = Visibility.Hidden;
            text02.Visibility = Visibility.Hidden;
            MoveElement(int.Parse(text.Text) * 1000, "", "");
        }

        private void MoveElement(double period,string text1,string text2)
        {
            ThicknessAnimation ta = new ThicknessAnimation(new Thickness(76, 120, 0, 0),new Thickness(86, 40, 0, 0),new Duration(TimeSpan.FromMilliseconds(period)));
            grid.BeginAnimation(MarginProperty, ta);

            BackgroundWorker all = new BackgroundWorker();
            all.DoWork += (object send2, DoWorkEventArgs args) =>
            {
                for (int i = 0; i < period / 5; i++)
                {
                    all.ReportProgress(i);
                    Thread.Sleep(5);
                }
            };
            all.ProgressChanged += (object send2, ProgressChangedEventArgs args) =>
            {
                double percent = (args.ProgressPercentage * 5.0) / period;
                if (percent < 0.25)
                    grid.Opacity = percent * 4;
                else if (percent > 0.75)
                    grid.Opacity = (1 - percent) * 4;
                grid.Margin = new Thickness(86, 120 - percent * 100, 0, 0);
            };
            all.WorkerReportsProgress = true;
            all.RunWorkerAsync();

            BackgroundWorker first = new BackgroundWorker();
            first.DoWork += (object send, DoWorkEventArgs args) => 
            {
                for (int i = 0; i < 109; i++)
                {
                    first.ReportProgress(i);
                    Thread.Sleep(4);
                }
                Thread.Sleep((int)period / 2 - 200);
            };
            first.ProgressChanged += (object send, ProgressChangedEventArgs args) =>
              {
                  text01.Margin = new Thickness(0, 109 - args.ProgressPercentage, 28, 0);
                  if (args.ProgressPercentage == 1)
                  {
                      text01.Text = text1;
                      text01.Visibility = Visibility.Visible;
                  }
              };
            first.RunWorkerCompleted += (object send, RunWorkerCompletedEventArgs args) =>
              {
                  BackgroundWorker second = new BackgroundWorker();
                  second.DoWork += (object send2, DoWorkEventArgs args2) =>
                  {
                      for (int i = 0; i < 100; i++)
                      {
                          second.ReportProgress(i);
                          Thread.Sleep(4);
                      }
                  };
                  second.ProgressChanged += (object send2, ProgressChangedEventArgs args2) =>
                  {
                      text02.Margin = new Thickness(0, 160 - args2.ProgressPercentage, 0, 0);
                      if (args2.ProgressPercentage == 1)
                      {
                          text02.Text = text2;
                          text02.Visibility = Visibility.Visible;
                      }
                  };
                  second.WorkerReportsProgress = true;
                  second.RunWorkerAsync();
              };
            first.WorkerReportsProgress = true;
            first.RunWorkerAsync();
        }

        private void AddSubtitle_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileInfo fi = new FileInfo(ofd.FileName);
                if (fi.Length > 1048577)        //1 MB
                {
                    MessageBox.Show("File is too large!");
                    return;
                }
                var lines = File.ReadAllLines(fi.FullName);
                LoudSubtitle(lines);
            }
        }

        private void LoudSubtitle(string[] lines)
        {
            Subtitle.RestID();

            List<string> item = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string s1 = lines[i];
                if (s1.Equals(string.Empty))
                {
                    if (lines.Length > i + 1)
                    {
                        i++;
                        string s2 = lines[i];
                        int m;
                        if (int.TryParse(s2, out m))
                        {
                            subtitles.Add(new Subtitle(item));
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
            
            subtitles.Sort();
        }


        #region SubTitle

        List<Subtitle> subtitles = new List<Subtitle>();

        Encoding subtitle_encode = Encoding.UTF8;

        #endregion
        bool is_playing;
        bool subtitle_on = true;
        int subtitle_id = -1;

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    media.Source = new Uri(ofd.FileName);
                    media.LoadedBehavior = MediaState.Manual;
                    media.Play();
                }
                catch (Exception) { }
            }
        }


        private void Audio_MediaOpened(object sender, RoutedEventArgs e)
        {

            BackgroundWorker slider_mover = new BackgroundWorker();
            slider_mover.WorkerReportsProgress = slider_mover.WorkerSupportsCancellation = true;
            slider_mover.DoWork += (object send, DoWorkEventArgs args)=> 
            {
                while (is_playing)
                {
                    Thread.Sleep(100);
                    slider_mover.ReportProgress(0);
                }
            };
            slider_mover.ProgressChanged += (object send, ProgressChangedEventArgs args) => 
            {
                if (subtitle_on)
                {
                    int current = SearchSubtitle(media.Position.TotalMilliseconds, 0, subtitles.Count - 1);
                    if (current > -1 && current < subtitles.Count)
                    {
                        if (current != subtitle_id)
                        {
                            subtitle_id = current;
                            MoveElement(subtitles[current].Period, subtitles[current].Text1, subtitles[current].Text2);
                        }
                    }
                    else
                    {
                        text01.Text = "";
                        text02.Text = "";
                    }
                }
            };
            is_playing = true;
            slider_mover.RunWorkerAsync();
        }

        private int SearchSubtitle(double pos, int start, int end)
        {
            if (subtitles.Count == 0 || start > subtitles.Count || end > subtitles.Count || start > end)
                return -1;
            int mid = (start + end) / 2;
            if (subtitles[mid].From.TotalMilliseconds < pos && subtitles[mid].To.TotalMilliseconds > pos)
                return mid;
            else if (start != end)
            {
                if (subtitles[mid].From.TotalMilliseconds < pos)
                    return SearchSubtitle(pos, mid + 1, end);
                else
                    return SearchSubtitle(pos, start, mid - 1);
            }
            return -1;
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }


    class Subtitle : IComparable
    {
        static string[] splitter = { "-->" };
        static int id = 0;
        public static int ID { get => id; }
        TimeSpan from;
        public TimeSpan From { get => from; set => from = value; }
        TimeSpan to;
        public TimeSpan To { get => to; set => to = value; }
        double period;
        public double Period { get => period; }
        string text1;
        public string Text1 { get => text1; set => text1 = value; }
        string text2;
        public string Text2 { get => text2; set => text2 = value; }

        public Subtitle(List<string> str)
        {
            id++;
            var time = str[1].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            if (time.Length > 1)
            {
                time[0] = time[0].Replace(" ", "").Replace(",", ".");
                time[1] = time[1].Replace(" ", "").Replace(",", ".");
                TimeSpan tmp;
                if (TimeSpan.TryParseExact(time[0], @"h\:m\:ss\.fff", null, out tmp))
                    From = tmp;
                if (TimeSpan.TryParseExact(time[1], @"h\:m\:ss\.fff", null, out tmp))
                    To = tmp;
                else
                    To = From - TimeSpan.FromSeconds(3);
            }
            Text1 = str[2];
            if (str.Count > 3)
                Text2 = str[3];
            period = To.TotalMilliseconds - from.TotalMilliseconds;
        }


        public static void RestID()
        {
            id = 0;
        }

        public int CompareTo(object x)
        {
            return From.CompareTo((x as Subtitle).From);
        }

    }
}