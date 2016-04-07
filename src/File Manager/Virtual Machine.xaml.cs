using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
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
    /// Interaction logic for Virtual_Machine.xaml
    /// </summary>
    public partial class Virtual_Machine : Window
    {
        Process vm_process;
        StringBuilder vm_output = new StringBuilder();
        string qemu_folder;
        bool Initialize = false, syncing = false, stop_requested = false, whpx_available = false;
        long max_memory = 2048;

        public Virtual_Machine()
        {
            InitializeComponent();
            SafetyTools.SetWindowIcon(this, "VirtualMachine");
            for (int i = 1; i <= Environment.ProcessorCount; i++)
                cores.Items.Add(i);
            cores.SelectedIndex = 0;
            long memory = (long)(ToolPerfSampler.TotalPhysicalMemory() / 1048576);
            if (memory == 0)
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
                    {
                        foreach (var share in searcher.Get())
                        {
                            long tmp;
                            long.TryParse(share.Properties["Capacity"].Value.ToString(), out tmp);
                            memory += tmp / 1048576;
                        }
                    }
                }
                catch (Exception) { }
            max_memory = memory <= 0 ? 2048 : memory > 4096 ? memory - 2048 : Math.Max(256, memory / 2);
            max_memory = Math.Max(128, max_memory / 64 * 64);
            memory_size.Maximum = max_memory;
            memory_size.TickFrequency = max_memory > 16384 ? 2048 : max_memory > 4096 ? 1024 : 256;
            memory_hint.Text = "64 MB - " + max_memory + " MB";
            try { whpx_available = File.Exists(System.IO.Path.Combine(Environment.SystemDirectory, "WinHvPlatform.dll")); }
            catch (Exception) { }
            DetectQemu();
            LoadSettings();
            Initialize = true;
            UpdateState();
        }

        void DetectQemu()
        {
            string selected = architecture.SelectedItem is ComboBoxItem ? (string)((ComboBoxItem)architecture.SelectedItem).Tag : Functions.ReadSetting("vm.arch");
            string found = null;
            foreach (var i in new string[] { "qemu-system-x86_64.exe", "qemu-system-i386.exe", "qemu-img.exe" })
            {
                found = Functions.FindProgram(i, "qemu", @"SOFTWARE\QEMU", "Install_Dir") ?? Functions.FindProgram(i, "qemu", @"SOFTWARE\QEMU", "InstallLocation");
                if (found != null)
                    break;
            }
            qemu_folder = found == null ? null : System.IO.Path.GetDirectoryName(found);
            architecture.Items.Clear();
            List<string> names = new List<string>();
            if (qemu_folder != null)
                try
                {
                    foreach (var i in Directory.GetFiles(qemu_folder, "qemu-system-*.exe"))
                    {
                        string name = System.IO.Path.GetFileNameWithoutExtension(i).Substring("qemu-system-".Length);
                        if (name.EndsWith("w") && File.Exists(System.IO.Path.Combine(qemu_folder, "qemu-system-" + name.Substring(0, name.Length - 1) + ".exe")))
                            continue;
                        names.Add(name);
                    }
                }
                catch (Exception) { }
            names = names.OrderBy(x => x == "x86_64" ? 0 : x == "i386" ? 1 : 2).ThenBy(x => x).ToList();
            List<string> others = names.Where(x => x != "x86_64" && x != "i386").ToList();
            foreach (var i in names)
            {
                bool pc = i == "x86_64" || i == "i386";
                ComboBoxItem item = new ComboBoxItem() { Content = i == "x86_64" ? "x86_64 (64-bit PC)" : i == "i386" ? "i386 (32-bit PC)" : i, Tag = i, IsEnabled = pc };
                if (!pc)
                {
                    item.ToolTip = "This emulator is installed, but it needs machine and firmware options this tool does not offer (only PC machines are supported here)";
                    ToolTipService.SetShowOnDisabled(item, true);
                }
                architecture.Items.Add(item);
            }
            ComboBoxItem select = architecture.Items.Cast<ComboBoxItem>().FirstOrDefault(x => (string)x.Tag == selected && x.IsEnabled) ?? architecture.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.IsEnabled);
            architecture.SelectedItem = select;
            if (qemu_folder == null)
                architecture.ToolTip = "QEMU is not installed";
            else if (select == null)
                architecture.ToolTip = "No QEMU PC emulator (qemu-system-x86_64.exe or qemu-system-i386.exe) was found in " + qemu_folder;
            else
                architecture.ToolTip = "QEMU emulators found in " + qemu_folder + (others.Count > 0 ? "\nAlso installed (not supported here): " + string.Join(", ", others.ToArray()) : "");
        }

        string QemuImg
        {
            get
            {
                if (qemu_folder == null)
                    return null;
                string path = System.IO.Path.Combine(qemu_folder, "qemu-img.exe");
                return File.Exists(path) ? path : null;
            }
        }

        void LoadSettings()
        {
            int value;
            if (int.TryParse(Functions.ReadSetting("vm.cores"), out value) && value >= 1 && value <= cores.Items.Count)
                cores.SelectedIndex = value - 1;
            long memory;
            if (!long.TryParse(Functions.ReadSetting("vm.memory"), out memory) || memory < 64)
                memory = Math.Min(2048, max_memory);
            SetMemory(Math.Min(memory, max_memory));
            accel_whpx.IsChecked = Functions.ReadSetting("vm.whpx") != "0";
            hdd_file.Text = Functions.ReadSetting("vm.hdd") ?? "";
            cd_file.Text = Functions.ReadSetting("vm.cd") ?? "";
            floppy_file.Text = Functions.ReadSetting("vm.floppy") ?? "";
            if (int.TryParse(Functions.ReadSetting("vm.boot"), out value) && value >= 0 && value < boot_from.Items.Count)
                boot_from.SelectedIndex = value;
            if (int.TryParse(Functions.ReadSetting("vm.vga"), out value) && value >= 0 && value < vga.Items.Count)
                vga.SelectedIndex = value;
            full_screen.IsChecked = Functions.ReadSetting("vm.fullscreen") == "1";
            network.IsChecked = Functions.ReadSetting("vm.network") != "0";
            sound.IsChecked = Functions.ReadSetting("vm.sound") == "1";
        }

        void SaveSettings()
        {
            if (architecture.SelectedItem is ComboBoxItem)
                Functions.SaveSetting("vm.arch", (string)((ComboBoxItem)architecture.SelectedItem).Tag);
            Functions.SaveSetting("vm.cores", (cores.SelectedIndex + 1).ToString());
            Functions.SaveSetting("vm.memory", ((long)memory_size.Value).ToString(System.Globalization.CultureInfo.InvariantCulture));
            Functions.SaveSetting("vm.whpx", accel_whpx.IsChecked == true ? "1" : "0");
            Functions.SaveSetting("vm.hdd", hdd_file.Text.Trim());
            Functions.SaveSetting("vm.cd", cd_file.Text.Trim());
            Functions.SaveSetting("vm.floppy", floppy_file.Text.Trim());
            Functions.SaveSetting("vm.boot", boot_from.SelectedIndex.ToString());
            Functions.SaveSetting("vm.vga", vga.SelectedIndex.ToString());
            Functions.SaveSetting("vm.fullscreen", full_screen.IsChecked == true ? "1" : "0");
            Functions.SaveSetting("vm.network", network.IsChecked == true ? "1" : "0");
            Functions.SaveSetting("vm.sound", sound.IsChecked == true ? "1" : "0");
        }

        void SetMemory(long value)
        {
            syncing = true;
            memory_size.Value = value;
            memory_text.Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            syncing = false;
        }

        private void MemorySize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (syncing || memory_text == null)
                return;
            long value = Math.Max(64, (long)Math.Round(e.NewValue / 64) * 64);
            syncing = true;
            memory_text.Text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (Math.Abs(memory_size.Value - value) > 0.5)
                memory_size.Value = value;
            syncing = false;
        }

        private void MemoryText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (syncing || memory_size == null)
                return;
            string digits = new string(memory_text.Text.Where(char.IsDigit).ToArray());
            if (digits != memory_text.Text)
            {
                int caret = memory_text.CaretIndex;
                memory_text.Text = digits;
                memory_text.CaretIndex = Math.Min(caret, digits.Length);
                return;
            }
            long value;
            if (long.TryParse(digits, out value) && value >= 64 && value <= max_memory)
            {
                syncing = true;
                memory_size.Value = value;
                syncing = false;
                memory_text.ClearValue(TextBox.ForegroundProperty);
            }
            else
                memory_text.Foreground = Brushes.Red;
        }

        private void MemoryText_LostFocus(object sender, RoutedEventArgs e)
        {
            long value;
            if (!long.TryParse(memory_text.Text, out value))
                value = (long)memory_size.Value;
            SetMemory(Math.Max(64, Math.Min(max_memory, value)));
            memory_text.ClearValue(TextBox.ForegroundProperty);
        }

        bool IsPC
        {
            get
            {
                ComboBoxItem item = architecture.SelectedItem as ComboBoxItem;
                return item != null && ((string)item.Tag == "x86_64" || (string)item.Tag == "i386");
            }
        }

        void UpdateState()
        {
            if (!Initialize)
                return;
            bool running = vm_process != null;
            qemu_bar.Visibility = qemu_folder == null ? Visibility.Visible : Visibility.Collapsed;
            bool media = hdd_file.Text.Trim().Length > 0 || cd_file.Text.Trim().Length > 0 || floppy_file.Text.Trim().Length > 0;
            string reason = qemu_folder == null ? "QEMU is not installed: use 'Download QEMU' below, then 'Check again'" : running ? "The virtual machine is running" : !IsPC ? "No QEMU PC emulator (qemu-system-x86_64.exe) was found" : !media ? "Choose a hard disk, CD/DVD or floppy image first" : null;
            SafetyTools.SetState(button, reason == null, reason);
            button.Content = running ? "Running" : "RUN";
            SafetyTools.SetState(stop_button, running, "The virtual machine is not running");
            options_grid.IsEnabled = !running;
            options_grid.ToolTip = running ? "Settings can not be changed while the virtual machine is running" : null;
            SafetyTools.SetState(create_hdd, QemuImg != null, qemu_folder == null ? "QEMU is not installed (qemu-img.exe creates the disk)" : "qemu-img.exe was not found in " + qemu_folder);
            SafetyTools.SetState(accel_whpx, whpx_available && IsPC, !whpx_available ? "Windows Hypervisor Platform is not turned on: Control Panel > Programs > Turn Windows features on or off > Windows Hypervisor Platform (then restart Windows). QEMU uses software emulation (TCG)." : "Only for x86 PC machines");
            textBlock5.Text = running ? "Running. Press \"Ctrl + Alt + G\" in the QEMU window to free the mouse." : "Press \"Ctrl + Alt + G\" in the QEMU window to free the mouse.";
        }

        private void Option_Changed(object sender, SelectionChangedEventArgs e)
        {
            UpdateState();
        }

        private void Media_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateState();
        }

        static string DriveFormat(string path)
        {
            switch (System.IO.Path.GetExtension(path).ToLower())
            {
                case ".vhd":
                    return "vpc";
                case ".vhdx":
                    return "vhdx";
                case ".qcow2":
                case ".qcow":
                    return System.IO.Path.GetExtension(path).ToLower().Substring(1);
                case ".qed":
                    return "qed";
                case ".vdi":
                    return "vdi";
                case ".vmdk":
                    return "vmdk";
                case ".hdd":
                    return "parallels";
                case ".img":
                case ".ima":
                case ".raw":
                case ".iso":
                case ".flp":
                case ".vfd":
                    return "raw";
            }
            return null;
        }

        static string DriveOption(string path, string options)
        {
            string format = DriveFormat(path);
            return " -drive \"file=" + path.Replace(",", ",,") + (format != null ? ",format=" + format : "") + options + "\"";
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            if (vm_process != null)
                return;
            string hdd = hdd_file.Text.Trim(), cd = cd_file.Text.Trim(), floppy = floppy_file.Text.Trim();
            List<string> missing = new List<string>();
            foreach (var i in new string[] { hdd, cd, floppy })
                if (i.Length > 0 && !File.Exists(i))
                    missing.Add(i);
            if (missing.Count > 0)
            {
                Messages.ShowMessage("We can not find the disk image.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Join("\n", missing.ToArray()));
                return;
            }
            string[] boot_media = { hdd, cd, floppy }, boot_names = { "hard disk", "CD/DVD", "floppy" };
            if (boot_media[boot_from.SelectedIndex].Length == 0)
            {
                Messages.ShowMessage("Choose a " + boot_names[boot_from.SelectedIndex] + " image to boot from.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Or choose another device in 'Boot from'.");
                return;
            }
            long memory;
            if (!long.TryParse(memory_text.Text, out memory) || memory < 64 || memory > max_memory)
            {
                Messages.ShowMessage("The memory size is not valid.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Type a size between 64 MB and " + max_memory + " MB.");
                return;
            }
            ComboBoxItem arch = architecture.SelectedItem as ComboBoxItem;
            if (qemu_folder == null || arch == null)
            {
                Functions.FindQemu("qemu-system-x86_64.exe");
                return;
            }
            string qemu = System.IO.Path.Combine(qemu_folder, "qemu-system-" + (string)arch.Tag + ".exe");
            if (!File.Exists(qemu))
            {
                Messages.ShowMessage("We can not find QEMU.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, qemu);
                return;
            }
            StringBuilder arguments = new StringBuilder();
            arguments.Append("-name \"File Manager VM\"");
            arguments.Append(" -m " + memory.ToString(System.Globalization.CultureInfo.InvariantCulture));
            arguments.Append(" -smp " + (cores.SelectedIndex + 1));
            arguments.Append(accel_whpx.IsEnabled && accel_whpx.IsChecked == true ? " -accel whpx,kernel-irqchip=off -accel tcg" : " -accel tcg");
            if (hdd.Length > 0)
                arguments.Append(DriveOption(hdd, ",media=disk,index=0"));
            if (cd.Length > 0)
                arguments.Append(DriveOption(cd, ",media=cdrom,index=2"));
            if (floppy.Length > 0)
                arguments.Append(DriveOption(floppy, ",if=floppy,index=0"));
            arguments.Append(" -boot order=" + (boot_from.SelectedIndex == 0 ? "c" : boot_from.SelectedIndex == 1 ? "d" : "a"));
            arguments.Append(" -vga " + ((ComboBoxItem)vga.SelectedItem).Content);
            if (full_screen.IsChecked == true)
                arguments.Append(" -full-screen");
            arguments.Append(network.IsChecked == true ? " -nic user,model=e1000" : " -nic none");
            if (sound.IsChecked == true)
                arguments.Append(" -audiodev dsound,id=snd0 -device AC97,audiodev=snd0");
            try
            {
                vm_output = new StringBuilder();
                stop_requested = false;
                Process process = new Process();
                ProcessStartInfo p = new ProcessStartInfo(qemu, arguments.ToString());
                p.WorkingDirectory = qemu_folder;
                p.CreateNoWindow = true;
                p.UseShellExecute = false;
                p.RedirectStandardError = true;
                p.RedirectStandardOutput = true;
                process.StartInfo = p;
                process.EnableRaisingEvents = true;
                StringBuilder output = vm_output;
                DataReceivedEventHandler read = (object s, DataReceivedEventArgs a) =>
                {
                    if (a.Data != null)
                        lock (output)
                            if (output.Length < 20000)
                                output.AppendLine(a.Data);
                };
                process.ErrorDataReceived += read;
                process.OutputDataReceived += read;
                process.Exited += (object s, EventArgs a) =>
                {
                    try { ((Process)s).WaitForExit(); }
                    catch (Exception) { }
                    Dispatcher.BeginInvoke(new Action(() => { VmExited(process, output); }));
                };
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                vm_process = process;
                SaveSettings();
            }
            catch (Exception ex)
            { Messages.ShowMessage("We can not start the virtual machine.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message + "\n\n" + qemu + " " + arguments); }
            UpdateState();
        }

        void VmExited(Process process, StringBuilder output)
        {
            if (vm_process != process)
                return;
            int code = 0;
            try { code = process.ExitCode; }
            catch (Exception) { }
            string text;
            lock (output)
                text = output.ToString().Trim();
            process.Dispose();
            vm_process = null;
            UpdateState();
            if (!stop_requested && code != 0)
                Messages.ShowMessage("The virtual machine stopped with an error.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, (text.Length > 0 ? text : "QEMU gave no message.") + "\n\nQEMU exit code: " + code);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (vm_process == null)
                return;
            if (Messages.ShowMessage("Turn the virtual machine off now?", ModrenMessageButton.YesNo, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "This is like pulling the power cable: anything not saved inside the virtual machine is lost. Shut the system inside it down first when you can.") != MyMessageBoxResult.Yes)
                return;
            StopVm();
        }

        void StopVm()
        {
            if (vm_process == null)
                return;
            stop_requested = true;
            try
            {
                if (!vm_process.HasExited)
                    vm_process.Kill();
            }
            catch (Exception ex)
            { Messages.ShowMessage("We can not stop the virtual machine.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
        }

        SelectFileDialog MakeDialog(string title, string filter_name, string extensions, string current)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.CreateFolderEnable = false;
            sfd.DialogTitle = title;
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = filter_name, extentions = SelectFileFilter.SetExtentions(extensions) });
            try
            {
                string folder = System.IO.Path.GetDirectoryName(current);
                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                    sfd.InitialDirectory = folder;
            }
            catch (Exception) { }
            return sfd;
        }

        private void BrowseHdd_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = MakeDialog("Select virtual hard disk file", "Virtual Hard Disk Image (*.vhd, *.vhdx, *.qcow2, *.qcow, *.qed, *.vdi, *.vmdk, *.img, *.raw, *.hdd)", ".vhd|.vhdx|.qcow2|.qcow|.qed|.vdi|.vmdk|.img|.raw|.hdd", hdd_file.Text);
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                hdd_file.Text = sfd.SelectedItem;
        }

        private void BrowseCd_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = MakeDialog("Select CD/DVD image file", "CD/DVD Image (*.iso, *.img)", ".iso|.img", cd_file.Text);
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
            {
                cd_file.Text = sfd.SelectedItem;
                if (hdd_file.Text.Trim().Length == 0 || !File.Exists(hdd_file.Text.Trim()))
                    boot_from.SelectedIndex = 1;
            }
        }

        private void BrowseFloppy_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = MakeDialog("Select floppy disk image file", "Floppy Disk Image (*.img, *.ima, *.flp, *.vfd)", ".img|.ima|.flp|.vfd", floppy_file.Text);
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                floppy_file.Text = sfd.SelectedItem;
        }

        private void CreateHdd_Click(object sender, RoutedEventArgs e)
        {
            Create_Virtual_Hard_Disk window = new Create_Virtual_Hard_Disk();
            window.Owner = this;
            window.ShowDialog();
            if (window.CreatedPath != null)
            {
                hdd_file.Text = window.CreatedPath;
                boot_from.SelectedIndex = cd_file.Text.Trim().Length > 0 ? 1 : 0;
            }
        }

        private void CreateFloppy_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SaveFile;
            sfd.CreateFolderEnable = true;
            sfd.AddExtension = true;
            sfd.MultiSelect = false;
            sfd.DialogTitle = "Save the new floppy image (1.44 MB) as";
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Floppy Disk Image (*.img)", extentions = SelectFileFilter.SetExtentions(".img") });
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (sfd.ShowWindow() != MyMessageBoxResult.OK || string.IsNullOrEmpty(sfd.SelectedItem))
                return;
            string target = sfd.SelectedItem, temp = target + ".partial";
            try
            {
                File.WriteAllBytes(temp, BlankFloppy());
                if (File.Exists(target))
                    File.Delete(target);
                File.Move(temp, target);
                floppy_file.Text = target;
            }
            catch (Exception ex)
            {
                try { File.Delete(temp); }
                catch (Exception) { }
                Messages.ShowMessage("We can not create the floppy image.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
            }
        }

        static byte[] BlankFloppy()
        {
            byte[] image = new byte[1474560];
            byte[] boot = { 0xEB, 0x3C, 0x90, (byte)'M', (byte)'S', (byte)'D', (byte)'O', (byte)'S', (byte)'5', (byte)'.', (byte)'0', 0x00, 0x02, 0x01, 0x01, 0x00, 0x02, 0xE0, 0x00, 0x40, 0x0B, 0xF0, 0x09, 0x00, 0x12, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x29 };
            Array.Copy(boot, image, boot.Length);
            byte[] serial = BitConverter.GetBytes((uint)Environment.TickCount ^ (uint)DateTime.Now.Ticks);
            Array.Copy(serial, 0, image, 39, 4);
            Array.Copy(Encoding.ASCII.GetBytes("NO NAME    FAT12   "), 0, image, 43, 19);
            byte[] code = { 0xFA, 0xF4, 0xEB, 0xFD };
            Array.Copy(code, 0, image, 62, code.Length);
            image[510] = 0x55;
            image[511] = 0xAA;
            foreach (int i in new int[] { 512, 512 * 10 })
            {
                image[i] = 0xF0;
                image[i + 1] = 0xFF;
                image[i + 2] = 0xFF;
            }
            return image;
        }

        private void AboutTool_Click(object sender, RoutedEventArgs e)
        {
            AboutTool window = new AboutTool();
            window.tool_vertion.Text = "Version: 1.0";
            window.tool_date.Text = "";
            window.tool_name.Text = "Virtual Machine";
            window.tool_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/Virtual-machine.png"));
            window.Owner = this;
            window.ShowDialog();
        }

        private void DownloadQemu_Click(object sender, RoutedEventArgs e)
        {
            Functions.OpenUrl(Functions.QemuDownloadLink);
        }

        private void CheckQemu_Click(object sender, RoutedEventArgs e)
        {
            DetectQemu();
            UpdateState();
            if (qemu_folder == null)
                Messages.ShowMessage("QEMU was not found.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, "Install QEMU for Windows (64-bit) from the official QEMU site into its default folder (C:\\Program Files\\qemu), then click 'Check again'.");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (vm_process != null)
                switch (Messages.ShowMessage("The virtual machine is still running.", "Turn it off now (anything not saved inside it is lost), or keep it running after this window closes?", new ButtonContent("Turn it off", ModrenMessageBitmapImage.Default, 1), new ButtonContent("Keep it running", ModrenMessageBitmapImage.Default, 2), new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 3)))
                {
                    case MyMessageBoxResult.Button1:
                        StopVm();
                        break;
                    case MyMessageBoxResult.Button2:
                        stop_requested = true;
                        break;
                    default:
                        e.Cancel = true;
                        return;
                }
            SaveSettings();
        }
    }
}
