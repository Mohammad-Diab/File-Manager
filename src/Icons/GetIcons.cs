using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Icons
{
    public static class GetIcons
    {
        public static BitmapImage GetIcon(string path, int width)
        {
            switch (width)
            {
                case 16:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Icons;component/Images" + "/16/" + path));
                case 24:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Icons;component/Images" + "/24/" + path));
                case 64:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Icons;component/Images" + "/64/" + path));
                case 256:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Icons;component/Images" + "/256/" + path));
                default:
                    return new BitmapImage(new Uri(@"pack://application:,,,/Icons;component/Images" + "/24/" + path));
            }
        }
    }
}
