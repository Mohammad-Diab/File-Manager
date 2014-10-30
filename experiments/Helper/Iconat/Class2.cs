using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Iconat
{
    public class Class1
    {
        public Class1() { }
        public Class1(int x) { }
        private static Dictionary<string, ImageSource> hh;
        public ImageSource getIcon(string ext)
        {
            if (hh == null)
                BuildDictionary();
            return hh[ext];
        }

        public void BuildDictionary()
        {
            hh = new Dictionary<string, ImageSource>();
            hh.Add("exe", new BitmapImage(new Uri("pack://application:,,,/Iconat;component/Icons/x-exe.png")));
            hh.Add("png", new BitmapImage(new Uri("pack://application:,,,/Iconat;component/Icons/computer.png")));
            hh.Add("avi", new BitmapImage(new Uri("pack://application:,,,/Iconat;component/Icons/multimedia-audio.png")));
            hh.Add("txt", new BitmapImage(new Uri("pack://application:,,,/Iconat;component/Icons/text-x.png")));
            hh.Add("msi", new BitmapImage(new Uri("pack://application:,,,/Iconat;component/Icons/text-help.png")));
        }

        public void CallWindow()
        {
            new Window1().Show();
        }
    }
}
