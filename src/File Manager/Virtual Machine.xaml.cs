using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
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
        public Virtual_Machine()
        {
            InitializeComponent();
            for (int i = 1; i < Environment.ProcessorCount; i++)
            {
                ComboBoxItem it = new ComboBoxItem();
                it.Content = i + 1;
                cores.Items.Add(it);
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory");
            long memory = 0;
            foreach (var share in searcher.Get())
            {
                long tmp;
                long.TryParse(share.Properties["Capacity"].Value.ToString(), out tmp);
                memory = memory + tmp;
            }
            memory /= 1048576;
            if (memory == 0)
                memory_size.IsEnabled = false;
            else
                memory_size.Maximum = memory > 1800 ? memory - 896 : memory > 896 ? Environment.OSVersion.Version.Major > 5 ? 256 : memory - 256 : Environment.OSVersion.Version.Major > 5 ? 128 : 256;
            Initialize = true;
        }

        bool Initialize = false;

        SelectFileDialog sfd;

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            vm_process = new Process();
            ProcessStartInfo p = new ProcessStartInfo(@"Tools\qemu\qemu-system-x86_64.exe", " -L . -smp cores=" + (cores.SelectedIndex + 1) + (floppy.IsChecked.Value ? " -fda" : hard_disk.IsChecked.Value ? " -hda" : " -cdrom") + " \"" + disk_file.Text + "\" -m " + memory_size.Value + " -vga " + ((ComboBoxItem)vga.SelectedItem).Content + (full_screen.IsChecked.Value ? " -full-screen" : ""));
            p.CreateNoWindow = true;
            p.UseShellExecute = false;
            vm_process.StartInfo = p;
            vm_process.Start();
            vm_process.WaitForExit();
        }

        private void MemorySize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Initialize)
                memory_size_text.Text = Functions.FileSize(((((long)memory_size.Value / 16) * 16) * 1024.0 * 1024), 2);
        }

        private void HardDisk_Checked(object sender, RoutedEventArgs e)
        {
            sfd = new SelectFileDialog();
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Virtual Hard Disk Image (*.hdd, *.qcow, *.qed, *.vdi, *.vhd, *.vmdk)", extentions = SelectFileFilter.SetExtentions(".hdd|.qcow|.qed|.vdi|.vhd|.vmdk") });
            sfd.DialogTitle = "Select virtual hard disk file";
            disk_file.Text = "";
        }

        private void CDRom_Checked(object sender, RoutedEventArgs e)
        {
            sfd = new SelectFileDialog();
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Virtual CD Rom Image (*.iso, *.img)", extentions = SelectFileFilter.SetExtentions(".iso|.img") });
            sfd.DialogTitle = "Select CD rom image file";
            disk_file.Text = "";
        }

        private void FloppyDisk_Checked(object sender, RoutedEventArgs e)
        {
            sfd = new SelectFileDialog();
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Virtual Floppy Disk Image (*.vfd)", extentions = SelectFileFilter.SetExtentions(".vfd") });
            sfd.DialogTitle = "Select virtual floppy disk file";
            disk_file.Text = "";
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var filter = sfd.FilterMenu;
            var title = sfd.DialogTitle;
            sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.DialogTitle = title;
            sfd.FilterMenu = filter;
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                disk_file.Text = sfd.SelectedItem;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (vm_process != null)
                try { vm_process.Close(); }
                catch (Exception)
                {
                    try { vm_process.Kill(); }
                    catch (Exception) { }
                }
        }

        private void CreateVHD_Click(object sender, RoutedEventArgs e)
        {
            if (Functions.IsRunningAsAdmin())
                new Create_Virtual_Hard_Disk().ShowDialog();
            else
                System.Windows.MessageBox.Show("You must run this app as administrator.");
        }
    }
}
