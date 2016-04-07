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
            DispatcherUnhandledException += (object sender_ex, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e_ex) =>
            {
                Functions.LogError(e_ex.Exception);
                e_ex.Handled = true;
                try { Messages.ShowMessage("Something went wrong.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, e_ex.Exception.Message); }
                catch (Exception) { }
            };
            AppDomain.CurrentDomain.UnhandledException += (object sender_ex, UnhandledExceptionEventArgs e_ex) =>
            {
                if (e_ex.ExceptionObject is Exception)
                    Functions.LogError((Exception)e_ex.ExceptionObject);
            };
            TaskScheduler.UnobservedTaskException += (object sender_ex, UnobservedTaskExceptionEventArgs e_ex) =>
            {
                Functions.LogError(e_ex.Exception);
                e_ex.SetObserved();
            };
            IsAdmin = Functions.IsRunningAsAdmin();
            string error = Functions.LoadFileInfo();
            if (error != null)
            {
                ShowErrorAndClose(error);
                return;
            }
            new Thread(() => { Operation.CleanWipeFolders(); }) { IsBackground = true }.Start();
            if (!SafetyTools.OpenToolFromArguments(e.Args))
                new Directory_Explorer().Show();
        }

        void ShowErrorAndClose(string error)
        {
            Messages.ShowMessage("Something wrong, sorry app will close now.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, error);
            Shutdown();
        }
    }
}
