using System;
using System.Collections.Generic;
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
            Initialize = false;
        }

        bool Initialize = true;
        Process vhd_process;

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SaveFile;
            sfd.DialogTitle = "select where to save virtual hard disk";
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Virtual Hard Disk (.vhd)", extentions = SelectFileFilter.SetExtentions(".vhd") });
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                disk_location.Text = sfd.SelectedItem;
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
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < disk_size_text.Text.Length; i++)
                if (char.IsDigit(disk_size_text.Text[i]) || disk_size_text.Text[i].Equals('.'))
                    sb.Append(disk_size_text.Text[i]);
            disk_size_text.Text = sb.ToString();
            double si = 0;
            double.TryParse(disk_size_text.Text, out si);
            if(si>4)
            disk_size.Value = si;

        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            if (Environment.OSVersion.Version.Major > 5)
            {
                if (disk_location.Text != "")
                {
                    if (File.Exists(disk_location.Text))
                        if (MessageBox.Show("File is already exist,Delete the existing file?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            try { File.Delete(disk_location.Text); }
                            catch (Exception)
                            { MessageBox.Show("We can not delete the file, please select other location."); return; }

                    //check if there is enough space
                    try
                    {
                        vhd_process = new Process();
                        double disk_size;
                        double.TryParse(disk_size_text.Text, out disk_size);
                        if (size_unit.SelectedIndex == 1)
                            disk_size *= 1024;
                        ProcessStartInfo p = new ProcessStartInfo("diskpart", "CREATE VDISK FILE=\"" + disk_location.Text + "\" MAXIMUM=" + disk_size + " TYPE=" + (dynamic_expand.IsChecked.Value ? "EXPANDABLE" : "FIXED"));
                        p.CreateNoWindow = true;
                        p.UseShellExecute = false;
                        p.Verb = "runas";
                        vhd_process.StartInfo = p;
                        vhd_process.Start();
                        vhd_process.WaitForExit();
                    }
                    catch (Exception) { }
                }
                else
                    MessageBox.Show("Please select disk location.");
            }

        }

        private void AttachVHD_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DetachVHD_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
