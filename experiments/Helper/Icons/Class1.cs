using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Icons
{
    public static class Class1
    {
        private static Dictionary<string, ImageSource> hh;
        public static ImageSource getIcon(string ext)
        {
            if (hh == null)
                BuildDictionary();
            return hh[ext];
        }

        private static void BuildDictionary()
        {
            hh = new Dictionary<string, ImageSource>();
            hh.Add("exe", new BitmapImage(new Uri("pack://application:,,,/Icons;Icons/x-exe.png")));
            hh.Add("png", new BitmapImage(new Uri("pack://application:,,,/Icons;component/Icons/computer.png")));
            hh.Add("avi", new BitmapImage(new Uri("pack://application:,,,/Icons;component/Icons/multimedia-audio.png")));
            hh.Add("txt", new BitmapImage(new Uri("pack://application:,,,/Icons;component/Icons/text-ini.png")));
            hh.Add("msi", new BitmapImage(new Uri("pack://application:,,,/Icons;component/Icons/text-help.png")));
        }
    }
}
