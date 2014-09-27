using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace ConsoleApplication6
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(GetFileSizeOnDisk("D:\\Games\\War Chess\\data1.pak"));
            Console.Read();
        }

        public static long GetFileSizeOnDisk(string file)
        {
            FileInfo info = new FileInfo(file);
            uint clusterSize = 0;
            using (var searcher = new ManagementObjectSearcher("select BlockSize,NumberOfBlocks from Win32_Volume WHERE DriveLetter = '" + info.Directory.Root.FullName.TrimEnd('\\') + "'"))
            {
                foreach (ManagementObject i in searcher.Get())
                {
                    string bb = (i["BlockSize"]).ToString();
                    uint.TryParse(bb, out clusterSize);
                    break;
                }
            }
            uint hosize;
            uint losize = GetCompressedFileSizeW(file, out hosize);
            long size;
            size = (long)hosize << 32 | losize;
            return ((size + ((long)clusterSize) - 1) / ((long)clusterSize)) * (long)clusterSize;
        }

        [DllImport("kernel32.dll")]
        static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,[Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);
    }
}
