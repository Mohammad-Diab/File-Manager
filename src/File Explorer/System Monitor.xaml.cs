using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Explorer
{
    /// <summary>
    /// Interaction logic for System_Monitor.xaml
    /// </summary>
    public partial class System_Monitor : Window
    {
        static ObservableCollection<ProcessItem> processes = new ObservableCollection<ProcessItem>();
        int selected_id = 0;
        public System_Monitor()
        {
            InitializeComponent();
            ListProcess.DataContext = processes;
            //Task b = new Task(() => {             
            //    Thread.Sleep(1000);
            //});
            //b.RunSynchronously();
            //Thread refresh = new Thread(() =>
            //{
            //    processes = new ObservableCollection<ProcessItem>();
            //    while (true)
            //    {
            //        processes.Clear();
            //        foreach (var i in Process.GetProcesses())
            //            processes.Add(new ProcessItem(i.Id, i.ProcessName + " (" + i.MainWindowTitle + ")", Functions.File_Size(i.NonpagedSystemMemorySize64 + i.PagedMemorySize64), i.SessionId.ToString()));
            //        Thread.Sleep(2000);
            //    }
            //});
            //refresh.IsBackground = true;
            //refresh.Start();
        }

        private void ListProcess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListProcess.SelectedIndex > -1)
            {
                Process i = Process.GetProcesses()[ListProcess.SelectedIndex];
                EndProcess.IsEnabled = true;
                id.Text = i.Id.ToString();
                name.Text = i.ProcessName;
                type.Text = i.NonpagedSystemMemorySize64 + " " + i.PagedMemorySize64 + " " + i.PagedSystemMemorySize64 + " " + i.PeakPagedMemorySize64 + " " + i.PeakVirtualMemorySize64 + " " + i.PeakWorkingSet64 + " " + i.PrivateMemorySize64 + " " + i.VirtualMemorySize64 + " " + i.WorkingSet64;
            }
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            processes.Clear();
            foreach (var i in Process.GetProcesses())
                processes.Add(new ProcessItem(i.Id.ToString(), i.ProcessName + " (" + i.MainWindowTitle + ")", Functions.File_Size(i.NonpagedSystemMemorySize64 + i.PagedMemorySize64), i.SessionId.ToString()));
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            processes.Clear();
            foreach (var i in Process.GetProcesses())
                processes.Add(new ProcessItem(i.Id.ToString(), i.ProcessName + " (" + i.MainWindowTitle + ")", Functions.File_Size(i.NonpagedSystemMemorySize64 + i.PagedMemorySize64), i.SessionId.ToString()));
        }



        //delegate void SetTextBoxTextDelegate(ListBox list, ICollection<ListBoxItem> items);

        /*private void Timer_Tick(object sender, EventArgs e)
        {
            task = new Task(Refresh);
            task.RunSynchronously(TaskScheduler.Current);
        }*/

        //void SetTextBoxTextDelegate(ListBox list, ItemCollection items)
        //{
        //    list.Items = items;
        //}
        //static public List<int> old_list_process = new List<int>();

        //private delegate void Refres(object sender, EventArgs e);

        /*private void Refresh()
        {
            System.Windows.Forms.Timer t1;
            int selectedItem = -1;
            if (ListProcess.SelectedIndex > -1)
                selectedItem = old_list_process[ListProcess.SelectedIndex];
            ListProcess.Items.Clear();
            old_list_process.Clear();
            foreach (var i in Process.GetProcesses())
            {
                string process = i.ProcessName;
                if (i.MainWindowTitle != String.Empty)
                    process += (" (" + i.MainWindowTitle + ")");
                ListProcess.Items.Add(process);
                old_list_process.Add(i.Id);
            }
            if (selectedItem > -1 && old_list_process.Contains(selectedItem))
                ListProcess.SelectedIndex = old_list_process.IndexOf(selectedItem);
            proccess_number.Text = "Process Numbers: " + old_list_process.Count;
            t1 = new System.Windows.Forms.Timer();
            t1.Tick += Timer_Tick;
            t1.Interval = 1000;
            t1.Enabled = true;
            t1.Start();
        }*/

        //private void jk(object state)
        //{
        //    ItemCollection u = ListProcess.Items;
        //    u.Clear();
        //    foreach (var i in Process.GetProcesses())
        //    {
        //        string process = i.ProcessName;
        //        if (i.MainWindowTitle != String.Empty)
        //            process += (" (" + i.MainWindowTitle + ")");
        //        u.Add(process);
        //    }
        //    //ListProcess.Dispatcher.RunAsync(
        //    //int selectedItem = -1;
        //    //if (ListProcess.SelectedIndex > -1)
        //    //    selectedItem = old_list_process[ListProcess.SelectedIndex];
        //    //ListProcess.Items.Clear();
        //    //old_list_process.Clear();

        //    //if (selectedItem > -1 && old_list_process.Contains(selectedItem))
        //    //    ListProcess.SelectedIndex = old_list_process.IndexOf(selectedItem);
        //}
    }
}
