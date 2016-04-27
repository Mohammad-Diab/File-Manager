using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PreView
{
    public class Viewer
    {
        public static void ViewImage(string ImagePath)
        {
            if (File.Exists(ImagePath))
            {
                ImageViewer window = new ImageViewer();
                window.Show();
                window.MakeImageViewer(new FileInfo(ImagePath));
            }
        }

        public static void ViewImages(List<string> ImagesPath)
        {

        }

        public static void ViewVideo(string VideoPath)
        {
            if (File.Exists(VideoPath))
            {
                VideoPlayer window = new VideoPlayer();
                window.Show();
                window.MakeVideoPlayer(new FileInfo(VideoPath));
            }
        }

        public static void ViewMusic(string MusicPath)
        {
            if (File.Exists(MusicPath))
            {
                MusicPlayer window = new MusicPlayer();
                window.Show();
                window.MakeMedaiPlayer(new FileInfo(MusicPath));
            }
        }

    }

    internal class SubtitleItem
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

    internal enum MedaiState
    {
        Unknown = 0,
        Failed = 1,
        Play = 2,
        Pause = 3,
        Stop = 4
    }
}
