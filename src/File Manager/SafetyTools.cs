using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace File_Manager
{
    static class SafetyTools
    {
        internal static void SetWindowIcon(Window window, string name)
        {
            try { window.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/Window/" + name + ".ico")); }
            catch (Exception) { }
        }

        internal static void SetState(FrameworkElement element, bool enabled, string reason)
        {
            element.IsEnabled = enabled;
            ToolTipService.SetShowOnDisabled(element, true);
            string description = element.Tag as string;
            element.ToolTip = enabled || reason == null ? description : description == null ? reason : description + "\n" + reason;
        }

        internal static void DigitsOnly(TextBox box)
        {
            box.PreviewTextInput += (object sender, TextCompositionEventArgs e) => { e.Handled = !e.Text.All(char.IsDigit); };
            DataObject.AddPastingHandler(box, (object sender, DataObjectPastingEventArgs e) =>
            {
                string text = e.DataObject.GetDataPresent(DataFormats.UnicodeText) ? e.DataObject.GetData(DataFormats.UnicodeText) as string : null;
                if (text == null || !text.All(char.IsDigit))
                    e.CancelCommand();
            });
        }

        internal static bool RestartAsAdmin(string tool, IEnumerable<string> paths)
        {
            StringBuilder arguments = new StringBuilder(tool);
            foreach (string i in paths)
            {
                arguments.Append(" \"");
                arguments.Append(i.EndsWith("\\") ? i + "\\" : i);
                arguments.Append('"');
            }
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(System.Reflection.Assembly.GetEntryAssembly().Location, arguments.ToString());
                info.UseShellExecute = true;
                info.Verb = "runas";
                Process.Start(info);
                return true;
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode != 1223)
                    Messages.ShowMessage("We can not restart File Manager as administrator.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not restart File Manager as administrator.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
            }
            return false;
        }

        internal static bool OpenToolFromArguments(string[] args)
        {
            if (args == null || args.Length < 1)
                return false;
            List<string> paths = args.Skip(1).Where(x => Directory.Exists(x) || File.Exists(x)).ToList();
            switch (args[0].ToLower())
            {
                case "--empty-folder-remover":
                    if (paths.Count > 0)
                        Functions.ShowTool(new Empty_Folder_Remover(paths));
                    else
                        Functions.ShowTool(new Empty_Folder_Remover());
                    return true;
                case "--identical-file-remover":
                    if (paths.Count > 0)
                        Functions.ShowTool(new Identical_File_Remover(paths));
                    else
                        Functions.ShowTool(new Identical_File_Remover());
                    return true;
                case "--directories-analyzer":
                    Functions.ShowTool(new Big_File_Finder(paths.Where(x => Directory.Exists(x)).ToList()));
                    return true;
            }
            return false;
        }

        internal static void OpenItem(string path)
        {
            if (Directory.Exists(path))
                Directory_Explorer.ExplorePath(path);
            else if (File.Exists(path))
            {
                try { Process.Start(path); }
                catch (Exception ex)
                { Messages.ShowMessage("We can not open '" + path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            }
            else
                Messages.ShowMessage("We can not find '" + path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        internal static void OpenLocation(string path)
        {
            string parent = null;
            try { parent = Path.GetDirectoryName(path.TrimEnd('\\')); }
            catch (Exception) { }
            if (parent != null && Directory.Exists(parent))
                Directory_Explorer.ExplorePath(parent);
            else
                Messages.ShowMessage("We can not find the folder of '" + path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        internal static void PreviewItem(string path)
        {
            MainWindow.GoToPath = false;
            if (File.Exists(path))
                new Preview().ShowDialog(new FileInfo(path), ItemType.File);
            else if (Directory.Exists(path))
                new Preview().ShowDialog(new DirectoryInfo(path), ItemType.Directory);
            else
            {
                Messages.ShowMessage("We can not find '" + path + "'.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                return;
            }
            if (MainWindow.GoToPath)
            {
                MainWindow.GoToPath = false;
                Directory_Explorer.ExplorePath(MainWindow.to_go_path);
            }
        }

        internal static void CopyPaths(IEnumerable<string> paths)
        {
            try { Clipboard.SetText(string.Join("\r\n", paths.ToArray())); }
            catch (Exception ex)
            { Messages.ShowMessage("We can not copy to the clipboard.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }
    }
}
