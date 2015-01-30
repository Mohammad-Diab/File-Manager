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
            k = true;
        }

        bool k = false;

        OpenFileDialog ofd = new OpenFileDialog();
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            Process m = new Process();
            ProcessStartInfo p = new ProcessStartInfo(@"Tools\qemu\qemu.exe", "-L . -smp cores=" + (cores.SelectedIndex + 1) + (floppy.IsChecked.Value ? " -fda" : hard_disk.IsChecked.Value ? " -hda" : " -cdrom") + " \"" + disk_file.Text + "\" -m " + memory_size.Value + " -vga " + ((ComboBoxItem)vga.SelectedItem).Content + (full_screen.IsChecked.Value ? " -full-screen" : ""));
            p.CreateNoWindow = true;
            p.UseShellExecute = false;
            m.StartInfo = p;
            m.Start();
            m.WaitForExit();
        }

        private void MemorySize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (k)
                memory_size_text.Text = Functions.FileSize(((((long)memory_size.Value / 16) * 16) * 1024.0 * 1024), 2);
        }

        private void HardDisk_Checked(object sender, RoutedEventArgs e)
        {
            ofd.Filter = "*.hdd|*.hdd|*.qcow|*.qcow|*.qed|*.qed|*.vdi|*.vdi|*.vhd|*.vhd|*.vmdk|*.vmdk";
            disk_file.Text = "";
        }

        private void CDRom_Checked(object sender, RoutedEventArgs e)
        {
            ofd.Filter = "*.iso|*.iso|*.img|*.img";
            disk_file.Text = "";
        }

        private void FloppyDisk_Checked(object sender, RoutedEventArgs e)
        {
            ofd.Filter = "*.vfd|*.vfd";
            disk_file.Text = "";
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                disk_file.Text = ofd.FileName;
        }
    }
}
