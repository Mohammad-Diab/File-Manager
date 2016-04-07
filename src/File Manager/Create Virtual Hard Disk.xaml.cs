using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Create_Virtual_Hard_Disk.xaml
    /// </summary>
    public partial class Create_Virtual_Hard_Disk : Window
    {
        public Create_Virtual_Hard_Disk()
        {
            InitializeComponent();
            SafetyTools.SetWindowIcon(this, "CreateVirtualHardDisk");
            Initialize = false;
            DiskFormat_SelectionChanged(null, null);
        }

        bool Initialize = true, creating = false;
        Process vhd_process;
        internal string CreatedPath;

        string Format
        {
            get { return disk_format.SelectedItem is ComboBoxItem ? (string)((ComboBoxItem)disk_format.SelectedItem).Tag : "vpc"; }
        }

        static string FormatExtension(string format)
        {
            switch (format)
            {
                case "vpc":
                    return ".vhd";
                case "raw":
                    return ".img";
            }
            return "." + format;
        }

        static readonly string[] disk_extensions = { ".vhd", ".vhdx", ".qcow2", ".vdi", ".vmdk", ".img" };

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string extension = FormatExtension(Format);
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SaveFile;
            sfd.CreateFolderEnable = true;
            sfd.AddExtension = true;
            sfd.MultiSelect = false;
            sfd.DialogTitle = "Select where to save the virtual hard disk";
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = ((ComboBoxItem)disk_format.SelectedItem).Content + " (*" + extension + ")", extentions = SelectFileFilter.SetExtentions(extension) });
            try
            {
                string folder = System.IO.Path.GetDirectoryName(disk_location.Text.Trim());
                sfd.InitialDirectory = !string.IsNullOrEmpty(folder) && Directory.Exists(folder) ? folder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            catch (Exception) { sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                disk_location.Text = sfd.SelectedItem;
        }

        private void DiskFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Initialize || disk_location == null)
                return;
            string format = Format, extension = FormatExtension(format), text = disk_location.Text.Trim();
            string current = System.IO.Path.GetExtension(text).ToLower();
            if (text.Length > 0 && disk_extensions.Contains(current) && current != extension)
                disk_location.Text = text.Substring(0, text.Length - current.Length) + extension;
            bool raw = format == "raw";
            SafetyTools.SetState(dynamic_expand, !raw, "A raw image always takes its full size");
            if (raw)
                fixed_size.IsChecked = true;
            size_hint.Text = format == "vpc" ? "VHD: up to 2040 GB" : format == "vhdx" ? "VHDX: up to 64 TB" : "";
            UpdateButtons();
        }

        private void DiskLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateButtons();
        }

        void UpdateButtons()
        {
            if (Initialize || attach_button == null)
                return;
            string path = disk_location.Text.Trim(), extension = System.IO.Path.GetExtension(path).ToLower();
            string reason = creating ? "Wait until the disk is created" : path.Length == 0 ? "Choose a .vhd or .vhdx file in 'File location' first" : extension != ".vhd" && extension != ".vhdx" ? "Windows can attach only .vhd and .vhdx files" : !File.Exists(path) ? "The file does not exist yet: create it first" : null;
            SafetyTools.SetState(attach_button, reason == null, reason);
            SafetyTools.SetState(detach_button, reason == null, reason);
            SafetyTools.SetState(create_button, !creating, "The disk is being created");
            options.IsEnabled = !creating;
        }

        private void SizeUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Initialize)
                return;
            DiskSizeText_TextChanged(null, null);
        }

        private void DiskSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!Initialize)
            {
                if (size_unit.SelectedIndex == 1)
                {
                    if (e.NewValue < 9)
                        disk_size_text.Text = Math.Round(e.NewValue, 2).ToString();
                    else if (e.NewValue < 31)
                        disk_size_text.Text = Math.Round(e.NewValue, 1).ToString();
                    else
                        disk_size_text.Text = (((int)(e.NewValue / 4)) * 4).ToString();
                }
                else if (e.NewValue > 1023)
                {
                    disk_size_text.Text = Math.Round(e.NewValue / 1024, 2).ToString();
                    disk_size.Value = Math.Round(e.NewValue / 1024, 2);
                    size_unit.SelectedIndex = 1;
                }
                else
                    disk_size_text.Text = Math.Round(e.NewValue, 2).ToString();
            }
        }

        private void DiskSizeText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (disk_size_text == null || disk_size == null)
                return;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < disk_size_text.Text.Length; i++)
                if (char.IsDigit(disk_size_text.Text[i]) || disk_size_text.Text[i].Equals('.'))
                    sb.Append(disk_size_text.Text[i]);
            if (sb.ToString() != disk_size_text.Text)
            {
                disk_size_text.Text = sb.ToString();
                disk_size_text.CaretIndex = disk_size_text.Text.Length;
                return;
            }
            double si = 0;
            double.TryParse(disk_size_text.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out si);
            if (si > 4 && si <= disk_size.Maximum)
            {
                bool old = Initialize;
                Initialize = true;
                disk_size.Value = si;
                Initialize = old;
            }

        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (creating)
                return;
            if (disk_location.Text.Trim() == "")
            {
                Messages.ShowMessage("Some field is missing.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please select disk location.");
                return;
            }
            double disk_size;
            double.TryParse(disk_size_text.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out disk_size);
            if (size_unit.SelectedIndex == 1)
                disk_size *= 1024;
            if (disk_size < 1)
            {
                Messages.ShowMessage("Some field has wrong values.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Please enter the disk size.");
                return;
            }
            string format = Format, extension = FormatExtension(format);
            if (format == "vpc" && disk_size > 2040 * 1024)
            {
                Messages.ShowMessage("A VHD file can not be bigger than 2040 GB.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Choose VHDX, QCOW2, VDI or VMDK for bigger disks.");
                return;
            }
            string target;
            try { target = System.IO.Path.GetFullPath(disk_location.Text.Trim()); }
            catch (Exception ex)
            {
                Messages.ShowMessage("The file location is not valid.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, ex.Message);
                return;
            }
            if (!target.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                target += extension;
            string folder = System.IO.Path.GetDirectoryName(target);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                Messages.ShowMessage("We can not find the folder.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, folder);
                return;
            }
            if (Directory.Exists(target))
            {
                Messages.ShowMessage("A folder with this name exists.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, target);
                return;
            }
            bool is_fixed = fixed_size.IsChecked == true;
            if (is_fixed)
                try
                {
                    DriveInfo drive = new DriveInfo(System.IO.Path.GetPathRoot(target));
                    if (drive.IsReady && drive.AvailableFreeSpace < (long)(disk_size * 1048576))
                    {
                        Messages.ShowMessage("There is not enough free space for a fixed size disk.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Free: " + Functions.FileSize(drive.AvailableFreeSpace, 2) + "\nNeeded: " + Functions.FileSize(disk_size * 1048576, 2) + "\nChoose 'Dynamically expanding' or another drive.");
                        return;
                    }
                }
                catch (Exception) { }
            if (File.Exists(target) && Messages.ShowMessage("File is already exist, overwrite?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "The old file is replaced only after the new disk is created successfully.") != MyMessageBoxResult.Yes)
                return;
            string qemu_img = Functions.FindQemu("qemu-img.exe");
            if (qemu_img == null)
                return;
            string options = "";
            switch (format)
            {
                case "vpc":
                case "vhdx":
                    options = " -o subformat=" + (is_fixed ? "fixed" : "dynamic");
                    break;
                case "qcow2":
                case "raw":
                    options = is_fixed ? " -o preallocation=full" : "";
                    break;
                case "vdi":
                    options = is_fixed ? " -o static=on" : "";
                    break;
                case "vmdk":
                    options = " -o subformat=" + (is_fixed ? "monolithicFlat" : "monolithicSparse");
                    break;
            }
            string temp_path = target.Substring(0, target.Length - extension.Length) + ".partial" + extension;
            string arguments = "create -f " + format + options + " \"" + temp_path + "\" " + ((long)disk_size).ToString(System.Globalization.CultureInfo.InvariantCulture) + "M";
            creating = true;
            progress.IsIndeterminate = true;
            progress.Visibility = Visibility.Visible;
            create_button.Content = "Creating...";
            Cursor = Cursors.AppStarting;
            UpdateButtons();
            string output = "";
            int exit_code = -1;
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (object s, DoWorkEventArgs a) =>
            {
                if (File.Exists(temp_path))
                    File.Delete(temp_path);
                ProcessStartInfo p = new ProcessStartInfo(qemu_img, arguments);
                p.CreateNoWindow = true;
                p.UseShellExecute = false;
                p.RedirectStandardError = true;
                p.RedirectStandardOutput = true;
                using (Process process = new Process())
                {
                    process.StartInfo = p;
                    process.Start();
                    vhd_process = process;
                    StringBuilder errors = new StringBuilder();
                    process.ErrorDataReceived += (object o, DataReceivedEventArgs d) => { if (d.Data != null) lock (errors) errors.AppendLine(d.Data); };
                    process.BeginErrorReadLine();
                    string standard = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    exit_code = process.ExitCode;
                    vhd_process = null;
                    lock (errors)
                        output = (errors.ToString() + standard).Trim();
                }
                if (exit_code == 0 && !cancel_create)
                {
                    if (File.Exists(target))
                        File.Delete(target);
                    File.Move(temp_path, target);
                }
            };
            worker.RunWorkerCompleted += (object s, RunWorkerCompletedEventArgs a) =>
            {
                creating = false;
                Cursor = null;
                progress.IsIndeterminate = false;
                progress.Visibility = Visibility.Hidden;
                create_button.Content = "CREATE";
                try
                {
                    if (File.Exists(temp_path))
                        File.Delete(temp_path);
                }
                catch (Exception) { }
                UpdateButtons();
                if (cancel_create)
                {
                    cancel_create = false;
                    return;
                }
                if (a.Error != null)
                    Messages.ShowMessage("We can not create the virtual hard disk.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, a.Error.Message);
                else if (exit_code == 0)
                {
                    CreatedPath = target;
                    disk_location.Text = target;
                    Messages.ShowMessage("The virtual hard disk has been created.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, target + "\n\nIt is empty: the system you install inside the virtual machine creates the partitions" + (format == "vpc" || format == "vhdx" ? ", or attach it here and use Disk Management." : "."));
                    if (Owner is Virtual_Machine)
                        Close();
                }
                else
                    Messages.ShowMessage("We can not create the virtual hard disk.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, output.Length > 0 ? output : "qemu-img exit code " + exit_code);
            };
            worker.RunWorkerAsync();
        }

        bool cancel_create = false;

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!creating)
                return;
            e.Cancel = true;
            if (Messages.ShowMessage("Stop creating the virtual hard disk?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, "The unfinished file is deleted.") != MyMessageBoxResult.Yes)
                return;
            cancel_create = true;
            try
            {
                Process process = vhd_process;
                if (process != null && !process.HasExited)
                    process.Kill();
            }
            catch (Exception) { }
        }

        void RunDiskPart(string command, string done_message)
        {
            if (!System.IO.File.Exists(disk_location.Text.Trim()))
            {
                Messages.ShowMessage("Please select an existing virtual hard disk (.vhd or .vhdx) file first.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                return;
            }
            string script = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "file-manager-diskpart-" + Guid.NewGuid().ToString("N") + ".txt");
            try
            {
                File.WriteAllText(script, "select vdisk file=\"" + System.IO.Path.GetFullPath(disk_location.Text.Trim()) + "\"\r\n" + command + "\r\n");
                ProcessStartInfo p = new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "diskpart.exe"), "/s \"" + script + "\"");
                p.Verb = "runas";
                p.UseShellExecute = true;
                p.WindowStyle = ProcessWindowStyle.Hidden;
                Cursor = Cursors.Wait;
                using (Process diskpart = Process.Start(p))
                {
                    diskpart.WaitForExit();
                    if (diskpart.ExitCode == 0)
                        Messages.ShowMessage(done_message, ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                    else
                        Messages.ShowMessage("Windows could not do it.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, "diskpart exit code " + diskpart.ExitCode + (command.StartsWith("attach") ? "\nIs it already attached, or used by a virtual machine?" : "\nIs it attached?"));
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.NativeErrorCode != 1223)
                    Messages.ShowMessage("We can not run diskpart.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
            }
            catch (Exception ex)
            { Messages.ShowMessage("We can not run diskpart.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            finally
            {
                Cursor = null;
                try { File.Delete(script); }
                catch (Exception) { }
            }
        }

        private void AttachVHD_Click(object sender, RoutedEventArgs e)
        {
            RunDiskPart("attach vdisk", "The virtual hard disk has been attached.");
        }

        private void DetachVHD_Click(object sender, RoutedEventArgs e)
        {
            RunDiskPart("detach vdisk", "The virtual hard disk has been detached.");
        }

    }
}
