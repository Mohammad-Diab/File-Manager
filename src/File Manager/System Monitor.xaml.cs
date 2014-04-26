using System;
using System.Collections.Generic;
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

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for System_Monitor.xaml
    /// </summary>
    public partial class System_Monitor : Window
    {
        int selected_id = 0;
        BackgroundWorker worker;
        Point point = new Point(0.5, 0.5);
        RotateTransform angel = new RotateTransform(45);
        BitmapImage process = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/service.png"));
        BitmapImage app = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/window.png"));
        Thickness[] margins = { new Thickness(10, 10, 0, 0), new Thickness(105, 10, 30, 0), new Thickness(105, 34, 30, 0), new Thickness(105, 58, 30, 0), new Thickness(105, 82, 30, 0), new Thickness(0, 43, 10, 0), new Thickness(10, 0, 10, 3) };
        public System_Monitor()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            bool change_selected_item = ProcessList.SelectedItem != null;
            int selected_id = 0;
            if (change_selected_item)
                selected_id = Convert.ToInt32(((ListViewItem)ProcessList.SelectedItem).ContentStringFormat);
            ProcessList.Items.Clear();
            foreach (var i in Process.GetProcesses())
                ProcessList.Items.Add(MakeProcessItem(i));
            if (change_selected_item)
                foreach (ListViewItem i in ProcessList.Items)
                    if (i.ContentStringFormat.Equals(selected_id.ToString()))
                    {
                        ProcessList.SelectedItem = i;
                        break;
                    }
        }

        private ListViewItem MakeProcessItem(Process item)
        {
            Grid container = new Grid();
            Image image = new Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Width = image.Height = 90;
            image.Margin = margins[0];
            if (!item.MainWindowTitle.Equals(""))
                image.Source = app;
            else
                image.Source = process;

            TextBlock tx1 = new TextBlock();
            tx1.VerticalAlignment = VerticalAlignment.Top;
            tx1.FontSize = 14;
            tx1.Margin = margins[1];
            tx1.TextTrimming = TextTrimming.CharacterEllipsis;
            tx1.Text = "Process ID: " + item.Id;

            TextBlock tx2 = new TextBlock();
            tx2.VerticalAlignment = VerticalAlignment.Top;
            tx2.FontSize = 14;
            tx2.Margin = margins[2];
            tx2.TextTrimming = TextTrimming.CharacterEllipsis;
            tx2.Text = "Process Name: " + (item.MainWindowTitle.Equals("") ? (item.ProcessName) : (item.ProcessName + " (" + item.MainWindowTitle + ")"));

            TextBlock tx3 = new TextBlock();
            tx3.VerticalAlignment = VerticalAlignment.Top;
            tx3.FontSize = 14;
            tx3.Margin = margins[3];
            tx3.TextTrimming = TextTrimming.CharacterEllipsis;
            tx3.Text = "CPU: ";
            //try
            ////{ tx3.Text = "CPU: " + item.TotalProcessorTime.TotalSeconds; }
            //catch (Exception) { }

            TextBlock tx4 = new TextBlock();
            tx4.VerticalAlignment = VerticalAlignment.Top;
            tx4.FontSize = 14;
            tx4.Margin = margins[4];
            tx4.TextTrimming = TextTrimming.CharacterEllipsis;
            tx4.Text = "Memory usage: " + (item.PagedSystemMemorySize64 + item.PagedMemorySize64 + item.NonpagedSystemMemorySize64);
            if (!item.Responding)
            {
                TextBlock unrespond = new TextBlock();
                unrespond.Text = "Unresponding";
                unrespond.HorizontalAlignment = HorizontalAlignment.Right;
                unrespond.Margin = margins[5];
                unrespond.FontSize = 18;
                unrespond.RenderTransformOrigin = point;
                unrespond.FontWeight = FontWeights.Bold;
                unrespond.Foreground = Brushes.Red;
                unrespond.VerticalAlignment = VerticalAlignment.Top;
                unrespond.RenderTransform = angel;
                container.Children.Add(unrespond);
            }


            Rectangle separator = new Rectangle();
            separator.Fill = Brushes.Silver;
            separator.Height = 1;
            separator.Margin = margins[6];
            separator.VerticalAlignment = VerticalAlignment.Bottom;

            container.Height = 111;
            container.Width = ProcessList.ActualWidth - 40;
            container.Children.Add(image);
            container.Children.Add(tx1);
            container.Children.Add(tx2);
            container.Children.Add(tx3);
            container.Children.Add(tx4);
            container.Children.Add(separator);

            ListViewItem LVI = new ListViewItem();
            LVI.ToolTip = item.ProcessName;
            LVI.Tag = item;
            LVI.ContentStringFormat = item.Id.ToString();
            LVI.Content = container;

            return LVI;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            worker.ReportProgress(0);
            while (true)
            {
                Thread.Sleep(1000);
                worker.ReportProgress(2);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            worker.ReportProgress(0);
        }

        private void Window_SizeChanged_1(object sender, SizeChangedEventArgs e)
        {
            foreach (ListViewItem i in ProcessList.Items)
                ((Grid)i.Content).Width = ProcessList.ActualWidth - 40;
        }


        //private void ListProcess_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (ListProcess.SelectedIndex > -1)
        //    {
        //        Process i = Process.GetProcesses()[ListProcess.SelectedIndex];
        //        EndProcess.IsEnabled = true;
        //        id.Text = i.Id.ToString();
        //        name.Text = i.ProcessName;
        //        type.Text = i.NonpagedSystemMemorySize64 + " " + i.PagedMemorySize64 + " " + i.PagedSystemMemorySize64 + " " + i.PeakPagedMemorySize64 + " " + i.PeakVirtualMemorySize64 + " " + i.PeakWorkingSet64 + " " + i.PrivateMemorySize64 + " " + i.VirtualMemorySize64 + " " + i.WorkingSet64;
        //    }
        //}


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
