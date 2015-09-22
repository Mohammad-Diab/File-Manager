using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static bool IsAdmin = false;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            IsAdmin = Functions.IsRunningAsAdmin();
            BinaryFormatter BF = new BinaryFormatter();
            if (File.Exists("FileInfo.dat"))
            {
                List<BasicFileInfoCombonents> info = null;
                Stream filestream = File.Open("FileInfo.dat", FileMode.Open);
                bool err = false;
                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        info = (List<BasicFileInfoCombonents>)BF.Deserialize(filestream);
                        err = false; break;
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(250);
                        err = true;
                    }
                }
                filestream.Close();
                if (err)
                    ShowErrorAndClose("Can not load 'FileInfo.dat' File.");

                if (info == null || !BasicFileInfoCombonents.ConvertToStandard(info))
                    ShowErrorAndClose("The file 'FileInfo.dat' is not valid.");
            }
            else
                ShowErrorAndClose("Can not find 'FileInfo.dat' File.");
        }

        void ShowErrorAndClose(string error)
        {
            Messages.ShowMessage("Something wrong, sorry app will close now.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, error);
            Shutdown();
        }
    }
}
