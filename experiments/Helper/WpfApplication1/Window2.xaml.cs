using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FilesManager
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        public Window2()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string Path = @"D:\User\Desktop\New folder (2)";
            var dir = new DirectoryInfo(Path);

            if (!dir.Exists)
            {
                dir.Create();
            }

            if ((dir.Attributes & FileAttributes.Compressed) == 0)
            {
                try
                {
                    // Enable compression for the output folder
                    // (this will save a ton of disk space)

                    string objPath = "Win32_Directory.Name=" + "'" + dir.FullName.Replace("\\", @"\\").TrimEnd('\\') + "'";

                    using (ManagementObject obj = new ManagementObject(objPath))
                    {
                        using (obj.InvokeMethod("Compress", null, null))
                        {
                            // I don't really care about the return value, 
                            // if we enabled it great but it can also be done manually
                            // if really needed
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("Cannot enable compression for folder '" + dir.FullName + "': " + ex.Message, "WMI");
                }
            }


        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string Path = @"D:\User\Desktop\New folder (2)";
            var dir = new DirectoryInfo(Path);
            if ((dir.Attributes & FileAttributes.Compressed) == 0)
            {
                try
                {
                    string objPath = "Win32_Directory.Name=" + "'" + dir.FullName.Replace("\\", @"\\").TrimEnd('\\') + "'";
                    using (ManagementObject obj = new ManagementObject(objPath))
                    { using (obj.InvokeMethod("DeCompress", null, null)) { } }
                }
                catch (Exception ex)
                { System.Diagnostics.Trace.WriteLine("Cannot enable compression for folder '" + dir.FullName + "': " + ex.Message, "WMI"); }
            }
        }


    }
}
