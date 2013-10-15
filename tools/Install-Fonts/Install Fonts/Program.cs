using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Install_Fonts
{
    public class Program
    {
        static int Main(string[] arg)
        {
            try
            {
                File.Copy(arg[0], Environment.GetFolderPath(Environment.SpecialFolder.Fonts) + "\\" + new FileInfo(arg[0]).Name, false);
                return 1;
            }
            catch (Exception) { return 0; }
        }
    }
}
