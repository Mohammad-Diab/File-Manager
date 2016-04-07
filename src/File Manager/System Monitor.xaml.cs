using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for System_Monitor.xaml
    /// </summary>
    public partial class System_Monitor : Window
    {
        ObservableCollection<ToolProcessItem> process_list = new ObservableCollection<ToolProcessItem>();
        ObservableCollection<ToolPerfSource> perf_sources = new ObservableCollection<ToolPerfSource>();
        Dictionary<string, ToolPerfSource> perf_by_key = new Dictionary<string, ToolPerfSource>();

        BackgroundWorker worker, perf_worker;
        bool closed = false, refresh_now = false, first_sample = true, performance_page_shown = false;
        string sort_by = "ProcessName";
        bool sort_descending = false;
        DispatcherTimer perf_timer;
        ToolPerfSampler sampler = new ToolPerfSampler();
        Dictionary<int, double[]> last_times = new Dictionary<int, double[]>();
        Dictionary<string, ImageSource> icon_cache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        List<Polyline> core_lines = new List<Polyline>();
        BitmapImage window_icon = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/window.png")), service_icon = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/service.png"));
        //Point point = new Point(0.5, 0.5);
        //RotateTransform angel = new RotateTransform(45);
        //BitmapImage process = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/service.png"));
        //BitmapImage app = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/window.png"));
        //Thickness[] margins = { new Thickness(10, 10, 0, 0), new Thickness(105, 10, 30, 0), new Thickness(105, 34, 30, 0), new Thickness(105, 58, 30, 0), new Thickness(105, 82, 30, 0), new Thickness(0, 43, 10, 0), new Thickness(10, 0, 10, 3) };
        public System_Monitor()
        {
            InitializeComponent();
            SafetyTools.SetWindowIcon(this, "SystemMonitor");
            window_icon.Freeze();
            service_icon.Freeze();
            ProcessList.ItemsSource = process_list;
            perf_list.ItemsSource = perf_sources;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.Width = label1.ActualWidth;
            perf_timer = new DispatcherTimer();
            perf_timer.Interval = TimeSpan.FromSeconds(1);
            perf_timer.Tick += PerfTimer_Tick;
            perf_timer.Start();
            StartWorker();
        }

        bool IsShowing
        {
            get { return !closed && IsVisible && WindowState != WindowState.Minimized; }
        }

        void StartWorker()
        {
            if (closed)
                return;
            worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(first_sample);
            first_sample = false;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (closed)
                return;
            if (e.Error == null && e.Result != null)
            {
                ToolProcessItem selected = ProcessList.SelectedItem as ToolProcessItem;
                Dictionary<int, ToolProcessItem> old_items = new Dictionary<int, ToolProcessItem>();
                foreach (var i in process_list)
                    old_items[i.Id] = i;
                List<ToolProcessItem> items = new List<ToolProcessItem>();
                foreach (var i in (List<ToolProcessItem>)e.Result)
                {
                    if (i.Icon == null)
                        i.Icon = i.HasWindow ? window_icon : service_icon;
                    ToolProcessItem old;
                    if (old_items.TryGetValue(i.Id, out old) && old.StartKey == i.StartKey)
                    {
                        old.Update(i);
                        items.Add(old);
                        old_items.Remove(i.Id);
                    }
                    else
                        items.Add(i);
                }
                foreach (var i in old_items.Values)
                    process_list.Remove(i);
                foreach (var i in items)
                    if (!process_list.Contains(i))
                        process_list.Add(i);
                ApplySort();
                if (selected != null && process_list.Contains(selected))
                    ProcessList.SelectedItem = selected;
                processes_count.Text = process_list.Count + " processes";
                ProcessList_SelectionChanged(null, null);
            }
            StartWorker();
        }

        //private ListViewItem MakeProcessItem(Process item)
        //{
        //    Grid container = new Grid();
        //    Image image = new Image();
        //    image.HorizontalAlignment = HorizontalAlignment.Left;
        //    image.VerticalAlignment = VerticalAlignment.Top;
        //    image.Width = image.Height = 90;
        //    image.Margin = margins[0];
        //    if (!item.MainWindowTitle.Equals(""))
        //        image.Source = app;
        //    else
        //        image.Source = process;

        //    TextBlock tx1 = new TextBlock();
        //    tx1.VerticalAlignment = VerticalAlignment.Top;
        //    tx1.FontSize = 14;
        //    tx1.Margin = margins[1];
        //    tx1.TextTrimming = TextTrimming.CharacterEllipsis;
        //    tx1.Text = "Process ID: " + item.Id;

        //    TextBlock tx2 = new TextBlock();
        //    tx2.VerticalAlignment = VerticalAlignment.Top;
        //    tx2.FontSize = 14;
        //    tx2.Margin = margins[2];
        //    tx2.TextTrimming = TextTrimming.CharacterEllipsis;
        //    tx2.Text = "Process Name: " + (item.MainWindowTitle.Equals("") ? (item.ProcessName) : (item.ProcessName + " (" + item.MainWindowTitle + ")"));

        //    TextBlock tx3 = new TextBlock();
        //    tx3.VerticalAlignment = VerticalAlignment.Top;
        //    tx3.FontSize = 14;
        //    tx3.Margin = margins[3];
        //    tx3.TextTrimming = TextTrimming.CharacterEllipsis;
        //    tx3.Text = "CPU: ";
        //    //try
        //    ////{ tx3.Text = "CPU: " + item.TotalProcessorTime.TotalSeconds; }
        //    //catch (Exception) { }

        //    TextBlock tx4 = new TextBlock();
        //    tx4.VerticalAlignment = VerticalAlignment.Top;
        //    tx4.FontSize = 14;
        //    tx4.Margin = margins[4];
        //    tx4.TextTrimming = TextTrimming.CharacterEllipsis;
        //    tx4.Text = "Memory usage: " + (item.PagedSystemMemorySize64 + item.PagedMemorySize64 + item.NonpagedSystemMemorySize64);
        //    if (!item.Responding)
        //    {
        //        TextBlock unrespond = new TextBlock();
        //        unrespond.Text = "Unresponding";
        //        unrespond.HorizontalAlignment = HorizontalAlignment.Right;
        //        unrespond.Margin = margins[5];
        //        unrespond.FontSize = 18;
        //        unrespond.RenderTransformOrigin = point;
        //        unrespond.FontWeight = FontWeights.Bold;
        //        unrespond.Foreground = Brushes.Red;
        //        unrespond.VerticalAlignment = VerticalAlignment.Top;
        //        unrespond.RenderTransform = angel;
        //        container.Children.Add(unrespond);
        //    }


        //    Rectangle separator = new Rectangle();
        //    separator.Fill = Brushes.Silver;
        //    separator.Height = 1;
        //    separator.Margin = margins[6];
        //    separator.VerticalAlignment = VerticalAlignment.Bottom;

        //    container.Height = 111;
        //    container.Width = ProcessList.ActualWidth - 40;
        //    container.Children.Add(image);
        //    container.Children.Add(tx1);
        //    container.Children.Add(tx2);
        //    container.Children.Add(tx3);
        //    container.Children.Add(tx4);
        //    container.Children.Add(separator);

        //    ListViewItem LVI = new ListViewItem();
        //    LVI.ToolTip = item.ProcessName;
        //    LVI.Tag = item;
        //    LVI.ContentStringFormat = item.Id.ToString();
        //    LVI.Content = container;

        //    return LVI;
        //}

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!(bool)e.Argument)
                for (int i = 0; i < 30 && !closed && !refresh_now; i++)
                    Thread.Sleep(100);
            while (!closed && !refresh_now && !processes_active)
                Thread.Sleep(200);
            refresh_now = false;
            if (closed)
                return;
            if (last_times.Count == 0)
            {
                last_times = ReadProcessTimes();
                Thread.Sleep(1000);
            }
            Process[] list = Process.GetProcesses();
            double now = ToolPerfSampler.Clock.Elapsed.TotalMilliseconds;
            int cores = Environment.ProcessorCount;
            Dictionary<int, double[]> new_times = new Dictionary<int, double[]>();
            List<ToolProcessItem> items = new List<ToolProcessItem>();
            foreach (var i in list)
            {
                if (closed)
                    return;
                try
                {
                    ToolProcessItem item = new ToolProcessItem(i);
                    double cpu_time = -1;
                    try { cpu_time = i.TotalProcessorTime.TotalMilliseconds; }
                    catch (Exception) { }
                    if (cpu_time >= 0)
                    {
                        new_times[i.Id] = new double[] { cpu_time, now };
                        double[] old;
                        if (last_times.TryGetValue(i.Id, out old) && now > old[1])
                            item.SetCPU(Math.Min(100, Math.Max(0, (cpu_time - old[0]) * 100.0 / ((now - old[1]) * cores))));
                        else
                            item.SetCPU(0);
                    }
                    else
                        item.SetCPU(-1);
                    item.Path = ToolProcessItem.GetProcessPath(i.Id);
                    if (item.Path != null)
                        item.Icon = GetProcessIcon(item.Path);
                    item.Details = item.ProcessName + "\nPID: " + item.PID + (item.Path != null ? "\n" + item.Path : "");
                    items.Add(item);
                }
                catch (Exception) { }
                finally { i.Dispose(); }
            }
            last_times = new_times;
            e.Result = items;
        }

        Dictionary<int, double[]> ReadProcessTimes()
        {
            Dictionary<int, double[]> times = new Dictionary<int, double[]>();
            double now = ToolPerfSampler.Clock.Elapsed.TotalMilliseconds;
            foreach (var i in Process.GetProcesses())
                try
                {
                    times[i.Id] = new double[] { i.TotalProcessorTime.TotalMilliseconds, now };
                }
                catch (Exception) { }
                finally { i.Dispose(); }
            return times;
        }

        ImageSource GetProcessIcon(string path)
        {
            ImageSource icon;
            if (icon_cache.TryGetValue(path, out icon))
                return icon;
            icon = null;
            try
            {
                using (System.Drawing.Icon file_icon = System.Drawing.Icon.ExtractAssociatedIcon(path))
                    if (file_icon != null)
                    {
                        BitmapSource source = Imaging.CreateBitmapSourceFromHIcon(file_icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        source.Freeze();
                        icon = source;
                    }
            }
            catch (Exception) { }
            icon_cache[path] = icon;
            return icon;
        }

        volatile bool processes_active = true;

        void UpdateActiveState()
        {
            processes_active = IsShowing && processes_page.Visibility == Visibility.Visible;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            UpdateActiveState();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            //worker.ReportProgress(0);
            refresh_now = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            closed = true;
            processes_active = false;
            if (perf_timer != null)
                perf_timer.Stop();
        }

        ToolProcessItem SelectedProcess
        {
            get { return ProcessList.SelectedItem as ToolProcessItem; }
        }

        private void ProcessList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ToolProcessItem selected = SelectedProcess;
            EndProcess.IsEnabled = selected != null && selected.HasWindow;
            EndProcess_Copy.IsEnabled = EndProcessTree.IsEnabled = selected != null;
            EndProcess.ToolTip = selected == null ? "Ask to close: select a program that has a window" : selected.HasWindow ? "Ask '" + selected.ShortName + "' to close its window (it can still ask to save)" : "Ask to close: '" + selected.ShortName + "' has no window, use End process";
            EndProcess_Copy.ToolTip = (selected == null ? "End process: select a process first" : "End process: stop '" + selected.ShortName + "' at once") + "\nWarning: anything not saved in it is lost, and ending a Windows process can make Windows stop working.";
            EndProcessTree.ToolTip = (selected == null ? "End process tree: select a process first" : "End process tree: stop '" + selected.ShortName + "' and every process it started") + "\nWarning: anything not saved in them is lost, and ending Windows processes can make Windows stop working.";
        }

        Process OpenSelected(ToolProcessItem selected)
        {
            try
            {
                Process p = Process.GetProcessById(selected.Id);
                if (string.Equals(p.ProcessName, selected.ShortName, StringComparison.OrdinalIgnoreCase))
                    return p;
                p.Dispose();
            }
            catch (Exception) { }
            Messages.ShowMessage("This process is not running any more.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, selected.ShortName + " (" + selected.PID + ")");
            refresh_now = true;
            return null;
        }

        private void EndProcess_Click(object sender, RoutedEventArgs e)
        {
            ToolProcessItem selected = SelectedProcess;
            if (selected == null)
                return;
            Process p = OpenSelected(selected);
            if (p == null)
                return;
            try
            {
                if (!p.CloseMainWindow())
                    Messages.ShowMessage("This process has no window to close.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, selected.ProcessName + " (" + selected.PID + ")");
            }
            catch (Exception ex)
            { Messages.ShowMessage("We can not ask this process to close.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            finally { p.Dispose(); }
            refresh_now = true;
        }

        private void KillProcess_Click(object sender, RoutedEventArgs e)
        {
            ToolProcessItem selected = SelectedProcess;
            if (selected == null)
                return;
            string warning = "The process ends at once: anything not saved in it is lost. Killing a Windows process can make Windows stop working.";
            if (selected.Id == Process.GetCurrentProcess().Id)
                warning = "This is File Manager itself: it closes at once and every running operation stops.\n\n" + warning;
            if (Messages.ShowMessage("End '" + selected.ShortName + "' (" + selected.PID + ")?", ModrenMessageButton.YesNo, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, warning) != MyMessageBoxResult.Yes)
                return;
            Process p = OpenSelected(selected);
            if (p == null)
                return;
            try
            {
                p.Kill();
                p.WaitForExit(3000);
            }
            catch (Exception ex)
            { Messages.ShowMessage("We can not end this process.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            finally { p.Dispose(); }
            refresh_now = true;
        }

        private void KillProcessTree_Click(object sender, RoutedEventArgs e)
        {
            ToolProcessItem selected = SelectedProcess;
            if (selected == null)
                return;
            Dictionary<int, List<int>> children = new Dictionary<int, List<int>>();
            Dictionary<int, string> names = new Dictionary<int, string>();
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId, Name FROM Win32_Process"))
                    foreach (ManagementObject i in searcher.Get())
                    {
                        int id = Convert.ToInt32(i["ProcessId"]), parent = Convert.ToInt32(i["ParentProcessId"]);
                        names[id] = Convert.ToString(i["Name"]);
                        if (id == parent)
                            continue;
                        if (!children.ContainsKey(parent))
                            children[parent] = new List<int>();
                        children[parent].Add(id);
                    }
            }
            catch (Exception ex)
            {
                Messages.ShowMessage("We can not read the process tree.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message);
                return;
            }
            List<int> tree = new List<int>();
            CollectTree(selected.Id, children, tree, 0);
            int self = Process.GetCurrentProcess().Id;
            StringBuilder list_text = new StringBuilder();
            for (int i = 0; i < tree.Count && i < 15; i++)
                list_text.AppendLine((names.ContainsKey(tree[i]) ? names[tree[i]] : "?") + " (" + tree[i] + ")");
            if (tree.Count > 15)
                list_text.AppendLine(string.Format("... and {0} more.", tree.Count - 15));
            list_text.AppendLine("\nThe processes end at once: anything not saved in them is lost. Ending Windows processes can make Windows stop working.");
            if (tree.Contains(self))
                list_text.AppendLine("File Manager itself is in this tree: it closes at once.");
            if (Messages.ShowMessage(string.Format("End '{0}' and {1} process(es) it started?", selected.ShortName, tree.Count - 1), ModrenMessageButton.YesNo, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, list_text.ToString()) != MyMessageBoxResult.Yes)
                return;
            Process root = OpenSelected(selected);
            if (root == null)
                return;
            root.Dispose();
            List<string> failed = new List<string>();
            for (int i = tree.Count - 1; i >= 0; i--)
            {
                if (tree[i] == self)
                    continue;
                try
                {
                    using (Process p = Process.GetProcessById(tree[i]))
                    {
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                }
                catch (ArgumentException) { }
                catch (Exception ex)
                { failed.Add((names.ContainsKey(tree[i]) ? names[tree[i]] : "?") + " (" + tree[i] + "): " + ex.Message); }
            }
            if (failed.Count > 0)
                Messages.ShowMessage("Some processes could not be ended.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, string.Join("\n", failed.Take(15).ToArray()));
            if (tree.Contains(self))
                Process.GetCurrentProcess().Kill();
            refresh_now = true;
        }

        void CollectTree(int id, Dictionary<int, List<int>> children, List<int> tree, int depth)
        {
            if (tree.Contains(id) || depth > 64)
                return;
            tree.Add(id);
            List<int> list;
            if (children.TryGetValue(id, out list))
                foreach (var i in list)
                    CollectTree(i, children, tree, depth + 1);
        }

        private void ProcessListHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;
            if (header == null || header.Column == null)
                return;
            string[] keys = { "PID", "ProcessName", "Memory", "CPU", "Threads" };
            int index = ((GridView)ProcessList.View).Columns.IndexOf(header.Column);
            if (index < 0 || index >= keys.Length)
                return;
            if (sort_by == keys[index])
                sort_descending = !sort_descending;
            else
            {
                sort_by = keys[index];
                sort_descending = index >= 2;
            }
            ApplySort();
        }

        void ApplySort()
        {
            string[] keys = { "PID", "ProcessName", "Memory", "CPU", "Threads" };
            string[] titles = { "PID", "Name", "Memory", "CPU", "Threads" };
            GridView view = (GridView)ProcessList.View;
            for (int i = 0; i < view.Columns.Count && i < keys.Length; i++)
                view.Columns[i].Header = titles[i] + (keys[i] == sort_by ? (sort_descending ? " ▼" : " ▲") : "");
            List<ToolProcessItem> sorted;
            switch (sort_by)
            {
                case "PID":
                    sorted = process_list.OrderBy(x => x.Id).ToList();
                    break;
                case "Memory":
                    sorted = process_list.OrderBy(x => x.MemoryBytes).ThenBy(x => x.ProcessName, StringComparer.OrdinalIgnoreCase).ToList();
                    break;
                case "CPU":
                    sorted = process_list.OrderBy(x => x.CPUValue).ThenBy(x => x.ProcessName, StringComparer.OrdinalIgnoreCase).ToList();
                    break;
                case "Threads":
                    sorted = process_list.OrderBy(x => x.ThreadCount).ThenBy(x => x.ProcessName, StringComparer.OrdinalIgnoreCase).ToList();
                    break;
                default:
                    sorted = process_list.OrderBy(x => x.ProcessName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Id).ToList();
                    break;
            }
            if (sort_descending)
                sorted.Reverse();
            for (int i = 0; i < sorted.Count; i++)
            {
                int index = process_list.IndexOf(sorted[i]);
                if (index != i)
                    process_list.Move(index, i);
            }
        }

        void TabChange_MouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            Label tab = (Label)sender;
            double rec_wid = tab == label1 ? 0 : label1.ActualWidth;
            rec.Width = tab.ActualWidth;
            rec.BeginAnimation(MarginProperty, new System.Windows.Media.Animation.ThicknessAnimation(new Thickness(rec_wid, 32, 0, 0), new Duration(new TimeSpan(2000000))));
            label1.Background = label2.Background = Brushes.Black;
            label1.Foreground = label2.Foreground = Brushes.White;
            tab.Background = Brushes.White;
            tab.Foreground = Brushes.Black;
            processes_page.Visibility = tab == label1 ? Visibility.Visible : Visibility.Collapsed;
            performance_page.Visibility = tab == label2 ? Visibility.Visible : Visibility.Collapsed;
            UpdateActiveState();
            if (tab == label2 && !performance_page_shown)
            {
                performance_page_shown = true;
                PerfTimer_Tick(null, null);
            }
            else if (tab == label1)
                refresh_now = true;
        }

        private void AboutTool_Click(object sender, RoutedEventArgs e)
        {
            AboutTool window = new AboutTool();
            window.tool_vertion.Text = "Version: 1.0";
            window.tool_date.Text = "";
            window.tool_name.Text = "System Monitor";
            window.tool_image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/system-monitor.png"));
            window.Owner = this;
            window.ShowDialog();
        }

        #region Performance

        private void PerfTimer_Tick(object sender, EventArgs e)
        {
            UpdateActiveState();
            if (!IsShowing || performance_page.Visibility != Visibility.Visible || (perf_worker != null && perf_worker.IsBusy))
                return;
            perf_worker = new BackgroundWorker();
            perf_worker.DoWork += (object s, DoWorkEventArgs a) => { a.Result = sampler.Sample(); };
            perf_worker.RunWorkerCompleted += PerfWorker_RunWorkerCompleted;
            perf_worker.RunWorkerAsync();
        }

        void PerfWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (closed || e.Error != null || e.Result == null)
                return;
            List<ToolPerfReading> readings = (List<ToolPerfReading>)e.Result;
            HashSet<string> seen = new HashSet<string>();
            foreach (var i in readings)
            {
                seen.Add(i.Key);
                ToolPerfSource source;
                if (!perf_by_key.TryGetValue(i.Key, out source))
                {
                    source = new ToolPerfSource(i);
                    perf_by_key[i.Key] = source;
                    perf_sources.Add(source);
                }
                source.Push(i);
            }
            foreach (var i in perf_sources.Where(x => !seen.Contains(x.Key)).ToList())
            {
                perf_sources.Remove(i);
                perf_by_key.Remove(i.Key);
            }
            if (perf_list.SelectedItem == null && perf_sources.Count > 0)
                perf_list.SelectedIndex = 0;
            DrawChart();
        }

        private void PerfList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawChart();
        }

        private void ShowCores_Changed(object sender, RoutedEventArgs e)
        {
            DrawChart();
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawChart();
        }

        void DrawChart()
        {
            ToolPerfSource source = perf_list.SelectedItem as ToolPerfSource;
            if (source == null)
            {
                chart_line1.Points = chart_line2.Points = new PointCollection();
                perf_title.Text = "";
                perf_details.Text = perf_sources.Count == 0 ? "Reading the performance counters..." : "";
                return;
            }
            perf_title.Text = source.Name;
            perf_details.Text = source.Details;
            legend1.Text = source.Label1;
            legend2.Text = source.Label2;
            legend2.Visibility = legend2_mark.Visibility = source.HasSecond ? Visibility.Visible : Visibility.Collapsed;
            double max = source.Percent ? 100 : Math.Max(source.Series1.Concat(source.Series2).DefaultIfEmpty(0).Max() * 1.15, source.Bits ? 8192 : 1024);
            chart_max.Text = source.Percent ? "100 %" : ToolPerfSampler.Rate(max, source.Bits);
            chart_line1.Points = MakePoints(source.Series1, max, chart_canvas.ActualWidth, chart_canvas.ActualHeight);
            chart_line2.Points = source.HasSecond ? MakePoints(source.Series2, max, chart_canvas.ActualWidth, chart_canvas.ActualHeight) : new PointCollection();
            show_cores.Visibility = source.Cores.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
            bool cores = source.Cores.Count > 1 && show_cores.IsChecked == true;
            cores_scroll.Visibility = cores ? Visibility.Visible : Visibility.Collapsed;
            if (!cores)
                return;
            while (core_lines.Count < source.Cores.Count)
            {
                Polyline line = new Polyline() { Stroke = chart_line1.Stroke, StrokeThickness = 1 };
                Canvas canvas = new Canvas() { Width = 110, Height = 46, ClipToBounds = true };
                canvas.Children.Add(line);
                Grid cell = new Grid();
                cell.Children.Add(canvas);
                cell.Children.Add(new TextBlock() { Text = "CPU " + core_lines.Count, FontSize = 10, Foreground = Brushes.Gray, Margin = new Thickness(3, 1, 0, 0) });
                cores_panel.Children.Add(new Border() { Child = cell, BorderBrush = chart_line1.Stroke, BorderThickness = new Thickness(1), Margin = new Thickness(0, 0, 4, 4), Background = Brushes.White });
                core_lines.Add(line);
            }
            for (int i = 0; i < core_lines.Count; i++)
                core_lines[i].Points = i < source.Cores.Count ? MakePoints(source.Cores[i], 100, 110, 46) : new PointCollection();
        }

        static PointCollection MakePoints(List<double> values, double max, double width, double height)
        {
            PointCollection points = new PointCollection();
            if (values.Count == 0 || width <= 0 || height <= 0 || max <= 0)
                return points;
            double step = width / (ToolPerfSource.HistoryLength - 1);
            for (int i = 0; i < values.Count; i++)
            {
                double x = width - (values.Count - 1 - i) * step;
                double y = height - Math.Min(1, Math.Max(0, values[i] / max)) * (height - 2) - 1;
                points.Add(new Point(x, y));
            }
            return points;
        }

        #endregion

        //private void Window_SizeChanged_1(object sender, SizeChangedEventArgs e)
        //{
        //    foreach (ListViewItem i in ProcessList.Items)
        //        ((Grid)i.Content).Width = ProcessList.ActualWidth - 40;
        //}


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

    public class ToolProcessItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        void Changed(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public ToolProcessItem(Process p)
        {
            Id = p.Id;
            PID = p.Id.ToString();
            ShortName = p.ProcessName;
            string title = "";
            try { title = p.MainWindowTitle; }
            catch (Exception) { }
            try { HasWindow = p.MainWindowHandle != IntPtr.Zero; }
            catch (Exception) { }
            ProcessName = title.Equals("") ? ShortName : ShortName + " (" + title + ")";
            try
            {
                MemoryBytes = p.PrivateMemorySize64;
                Memory = Functions.FileSize(MemoryBytes, 1);
            }
            catch (Exception)
            {
                MemoryBytes = -1;
                Memory = "-";
            }
            try
            {
                ThreadCount = p.Threads.Count;
                Threads = ThreadCount.ToString();
            }
            catch (Exception)
            {
                ThreadCount = -1;
                Threads = "-";
            }
            try { StartKey = ShortName + "|" + p.StartTime.Ticks; }
            catch (Exception) { StartKey = ShortName; }
        }

        public void SetCPU(double value)
        {
            CPUValue = value;
            CPU = value < 0 ? "-" : value.ToString(value >= 10 ? "0" : "0.0") + " %";
        }

        public void Update(ToolProcessItem item)
        {
            if (ProcessName != item.ProcessName) { ProcessName = item.ProcessName; Changed("ProcessName"); }
            if (Memory != item.Memory) { Memory = item.Memory; Changed("Memory"); }
            if (CPU != item.CPU) { CPU = item.CPU; Changed("CPU"); }
            if (Threads != item.Threads) { Threads = item.Threads; Changed("Threads"); }
            if (Details != item.Details) { Details = item.Details; Changed("Details"); }
            if (item.Icon != null && Icon != item.Icon) { Icon = item.Icon; Changed("Icon"); }
            MemoryBytes = item.MemoryBytes;
            CPUValue = item.CPUValue;
            ThreadCount = item.ThreadCount;
            HasWindow = item.HasWindow;
            Path = item.Path;
        }

        public int Id;
        public string StartKey, ShortName, Path;
        public bool HasWindow;
        public long MemoryBytes;
        public double CPUValue;
        public int ThreadCount;

        public string PID { get; set; }
        public string ProcessName { get; set; }
        public string Memory { get; set; }
        public string CPU { get; set; }
        public string Threads { get; set; }
        public string Details { get; set; }
        public ImageSource Icon { get; set; }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint access, bool inherit, int id);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr handle);

        internal static string GetProcessPath(int id)
        {
            if (id == 0 || id == 4)
                return null;
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = OpenProcess(0x1000, false, id);
                if (handle == IntPtr.Zero)
                    return null;
                StringBuilder name = new StringBuilder(1024);
                int size = name.Capacity;
                if (QueryFullProcessImageName(handle, 0, name, ref size))
                    return name.ToString();
            }
            catch (Exception) { }
            finally
            {
                if (handle != IntPtr.Zero)
                    CloseHandle(handle);
            }
            return null;
        }
    }

    public class ToolPerfReading
    {
        public string Key, Name, Value, Details, Label1, Label2;
        public double A, B;
        public bool HasSecond, Percent, Bits;
        public double[] Cores;
    }

    public class ToolPerfSource : INotifyPropertyChanged
    {
        public const int HistoryLength = 60;
        public event PropertyChangedEventHandler PropertyChanged;

        public ToolPerfSource(ToolPerfReading reading)
        {
            Key = reading.Key;
            Name = reading.Name;
            Label1 = reading.Label1;
            Label2 = reading.Label2;
            HasSecond = reading.HasSecond;
            Percent = reading.Percent;
            Bits = reading.Bits;
            Series1 = new List<double>();
            Series2 = new List<double>();
            Cores = new List<List<double>>();
        }

        public void Push(ToolPerfReading reading)
        {
            Add(Series1, reading.A);
            if (HasSecond)
                Add(Series2, reading.B);
            if (reading.Cores != null)
            {
                while (Cores.Count < reading.Cores.Length)
                    Cores.Add(new List<double>());
                for (int i = 0; i < reading.Cores.Length; i++)
                    Add(Cores[i], reading.Cores[i]);
            }
            Details = reading.Details;
            if (Value != reading.Value)
            {
                Value = reading.Value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Value"));
            }
        }

        static void Add(List<double> list, double value)
        {
            list.Add(double.IsNaN(value) || double.IsInfinity(value) ? 0 : value);
            while (list.Count > HistoryLength)
                list.RemoveAt(0);
        }

        public string Key, Label1, Label2, Details;
        public bool HasSecond, Percent, Bits;
        public List<double> Series1, Series2;
        public List<List<double>> Cores;
        public string Name { get; set; }
        public string Value { get; set; }
    }

    class ToolPerfSampler
    {
        internal static readonly Stopwatch Clock = Stopwatch.StartNew();

        [StructLayout(LayoutKind.Sequential)]
        struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemTimes(out long idle, out long kernel, out long user);

        [DllImport("kernel32.dll")]
        static extern ulong GetTickCount64();

        long last_idle = -1, last_kernel, last_user;
        PerformanceCounter[] core_counters;
        string cpu_name;
        string[] disk_names;
        Dictionary<string, PerformanceCounter[]> disk_counters = new Dictionary<string, PerformanceCounter[]>();
        int disk_check = 0;
        Dictionary<string, double[]> net_last = new Dictionary<string, double[]>();
        bool? gpu_available;
        Dictionary<string, CounterSample> gpu_last = new Dictionary<string, CounterSample>();
        string gpu_names;

        internal static ulong TotalPhysicalMemory()
        {
            MEMORYSTATUSEX status = new MEMORYSTATUSEX();
            status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            try
            {
                if (GlobalMemoryStatusEx(ref status))
                    return status.ullTotalPhys;
            }
            catch (Exception) { }
            return 0;
        }

        internal static string Rate(double value, bool bits)
        {
            if (bits)
            {
                string[] units = { "bps", "Kbps", "Mbps", "Gbps" };
                int i = 0;
                while (value >= 1000 && i < units.Length - 1)
                {
                    value /= 1000;
                    i++;
                }
                return Math.Round(value, value < 10 ? 1 : 0) + " " + units[i];
            }
            return Functions.FileSize(value, 1) + "/s";
        }

        internal List<ToolPerfReading> Sample()
        {
            List<ToolPerfReading> list = new List<ToolPerfReading>();
            try { SampleCPU(list); }
            catch (Exception) { }
            try { SampleMemory(list); }
            catch (Exception) { }
            try { SampleDisks(list); }
            catch (Exception) { }
            try { SampleNetwork(list); }
            catch (Exception) { }
            try { SampleGPU(list); }
            catch (Exception) { }
            return list;
        }

        void SampleCPU(List<ToolPerfReading> list)
        {
            if (cpu_name == null)
            {
                cpu_name = "";
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                        foreach (ManagementObject i in searcher.Get())
                        {
                            cpu_name = Convert.ToString(i["Name"]).Trim();
                            break;
                        }
                }
                catch (Exception) { }
            }
            if (core_counters == null)
            {
                List<PerformanceCounter> counters = new List<PerformanceCounter>();
                try
                {
                    string[] names = new PerformanceCounterCategory("Processor").GetInstanceNames().Where(x => x != "_Total").ToArray();
                    int number;
                    foreach (var i in names.OrderBy(x => int.TryParse(x, out number) ? number : int.MaxValue))
                    {
                        PerformanceCounter counter = new PerformanceCounter("Processor", "% Processor Time", i, true);
                        counter.NextValue();
                        counters.Add(counter);
                    }
                }
                catch (Exception) { counters.Clear(); }
                core_counters = counters.ToArray();
            }
            long idle, kernel, user;
            double usage = 0;
            if (GetSystemTimes(out idle, out kernel, out user))
            {
                if (last_idle >= 0)
                {
                    double total = (kernel - last_kernel) + (user - last_user);
                    if (total > 0)
                        usage = Math.Min(100, Math.Max(0, (total - (idle - last_idle)) * 100.0 / total));
                }
                last_idle = idle;
                last_kernel = kernel;
                last_user = user;
            }
            double[] cores = new double[core_counters.Length];
            for (int i = 0; i < cores.Length; i++)
                try { cores[i] = Math.Min(100, Math.Max(0, core_counters[i].NextValue())); }
                catch (Exception) { }
            string up_time = "";
            try
            {
                TimeSpan up = TimeSpan.FromMilliseconds(GetTickCount64());
                up_time = string.Format("Up time: {0}:{1:00}:{2:00}:{3:00}", (int)up.TotalDays, up.Hours, up.Minutes, up.Seconds);
            }
            catch (Exception) { }
            list.Add(new ToolPerfReading()
            {
                Key = "cpu",
                Name = "CPU",
                Value = Math.Round(usage) + " %",
                A = usage,
                Percent = true,
                Label1 = "Total use",
                Cores = cores.Length > 0 ? cores : null,
                Details = (cpu_name != "" ? cpu_name + "\n" : "") + "Use: " + Math.Round(usage, 1) + " %    Logical processors: " + Environment.ProcessorCount + (up_time != "" ? "    " + up_time : "")
            });
        }

        void SampleMemory(List<ToolPerfReading> list)
        {
            MEMORYSTATUSEX status = new MEMORYSTATUSEX();
            status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (!GlobalMemoryStatusEx(ref status) || status.ullTotalPhys == 0)
                return;
            double used = status.ullTotalPhys - status.ullAvailPhys;
            double percent = used * 100.0 / status.ullTotalPhys;
            list.Add(new ToolPerfReading()
            {
                Key = "mem",
                Name = "Memory",
                Value = Functions.FileSize(used, 1) + " / " + Functions.FileSize(status.ullTotalPhys, 1) + " (" + Math.Round(percent) + " %)",
                A = percent,
                Percent = true,
                Label1 = "In use",
                Details = "In use: " + Functions.FileSize(used, 2) + "    Available: " + Functions.FileSize(status.ullAvailPhys, 2) + "    Total: " + Functions.FileSize(status.ullTotalPhys, 2) + "\nCommitted: " + Functions.FileSize(status.ullTotalPageFile - status.ullAvailPageFile, 2) + " of " + Functions.FileSize(status.ullTotalPageFile, 2)
            });
        }

        void SampleDisks(List<ToolPerfReading> list)
        {
            if (disk_names == null || disk_check++ % 15 == 0)
            {
                string[] names;
                try { names = new PerformanceCounterCategory("PhysicalDisk").GetInstanceNames().Where(x => x != "_Total").OrderBy(x => x).ToArray(); }
                catch (Exception) { names = new string[0]; }
                if (disk_names == null || !names.SequenceEqual(disk_names))
                {
                    foreach (var i in disk_counters.Values)
                        foreach (var j in i)
                            j.Dispose();
                    disk_counters.Clear();
                    foreach (var i in names)
                        try
                        {
                            PerformanceCounter[] counters = { new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", i, true), new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", i, true), new PerformanceCounter("PhysicalDisk", "% Idle Time", i, true) };
                            foreach (var j in counters)
                                j.NextValue();
                            disk_counters[i] = counters;
                        }
                        catch (Exception) { }
                    disk_names = names;
                }
            }
            foreach (var i in disk_counters)
            {
                double read = 0, write = 0, active = 0;
                try
                {
                    read = i.Value[0].NextValue();
                    write = i.Value[1].NextValue();
                    active = Math.Min(100, Math.Max(0, 100 - i.Value[2].NextValue()));
                }
                catch (Exception) { continue; }
                list.Add(new ToolPerfReading()
                {
                    Key = "disk:" + i.Key,
                    Name = "Disk " + i.Key,
                    Value = "Active " + Math.Round(active) + " %",
                    A = read,
                    B = write,
                    HasSecond = true,
                    Label1 = "Read",
                    Label2 = "Write",
                    Details = "Read: " + Rate(read, false) + "    Write: " + Rate(write, false) + "    Active time: " + Math.Round(active, 1) + " %"
                });
            }
        }

        void SampleNetwork(List<ToolPerfReading> list)
        {
            double now = Clock.Elapsed.TotalSeconds;
            foreach (var i in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (i.OperationalStatus != OperationalStatus.Up || i.NetworkInterfaceType == NetworkInterfaceType.Loopback || i.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    continue;
                double sent, received;
                try
                {
                    IPv4InterfaceStatistics statistics = i.GetIPv4Statistics();
                    sent = statistics.BytesSent;
                    received = statistics.BytesReceived;
                }
                catch (Exception) { continue; }
                double send_rate = 0, receive_rate = 0;
                double[] old;
                if (net_last.TryGetValue(i.Id, out old) && now > old[2])
                {
                    send_rate = Math.Max(0, (sent - old[0]) * 8 / (now - old[2]));
                    receive_rate = Math.Max(0, (received - old[1]) * 8 / (now - old[2]));
                }
                net_last[i.Id] = new double[] { sent, received, now };
                string address = "";
                try
                {
                    foreach (var j in i.GetIPProperties().UnicastAddresses)
                        if (j.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            address = j.Address.ToString();
                            break;
                        }
                }
                catch (Exception) { }
                list.Add(new ToolPerfReading()
                {
                    Key = "net:" + i.Id,
                    Name = i.Name,
                    Value = "S: " + Rate(send_rate, true) + "  R: " + Rate(receive_rate, true),
                    A = send_rate,
                    B = receive_rate,
                    HasSecond = true,
                    Bits = true,
                    Label1 = "Send",
                    Label2 = "Receive",
                    Details = i.Description + "\nSend: " + Rate(send_rate, true) + "    Receive: " + Rate(receive_rate, true) + (i.Speed > 0 ? "    Link speed: " + Rate(i.Speed, true) : "") + (address != "" ? "    IPv4: " + address : "")
                });
            }
        }

        void SampleGPU(List<ToolPerfReading> list)
        {
            if (gpu_available == null)
            {
                try { gpu_available = PerformanceCounterCategory.Exists("GPU Engine"); }
                catch (Exception) { gpu_available = false; }
                if (gpu_available == true)
                    try
                    {
                        List<string> names = new List<string>();
                        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                            foreach (ManagementObject i in searcher.Get())
                                names.Add(Convert.ToString(i["Name"]));
                        gpu_names = string.Join(", ", names.ToArray());
                    }
                    catch (Exception) { }
            }
            if (gpu_available != true)
                return;
            InstanceDataCollectionCollection data = new PerformanceCounterCategory("GPU Engine").ReadCategory();
            InstanceDataCollection utilization = data["Utilization Percentage"];
            if (utilization == null)
                return;
            Dictionary<string, Dictionary<string, double>> adapters = new Dictionary<string, Dictionary<string, double>>();
            Dictionary<string, CounterSample> current = new Dictionary<string, CounterSample>();
            foreach (InstanceData i in utilization.Values)
            {
                string name = i.InstanceName;
                int luid = name.IndexOf("luid_"), eng = name.IndexOf("_eng_"), type = name.IndexOf("engtype_");
                if (luid < 0 || eng < luid || type < 0)
                    continue;
                string adapter = name.Substring(luid, eng - luid), engine = name.Substring(type + 8);
                current[name] = i.Sample;
                CounterSample old;
                double value = 0;
                if (gpu_last.TryGetValue(name, out old))
                    try { value = CounterSample.Calculate(old, i.Sample); }
                    catch (Exception) { }
                if (!adapters.ContainsKey(adapter))
                    adapters[adapter] = new Dictionary<string, double>();
                Dictionary<string, double> engines = adapters[adapter];
                engines[engine] = (engines.ContainsKey(engine) ? engines[engine] : 0) + Math.Max(0, value);
            }
            gpu_last = current;
            Dictionary<string, long> dedicated = new Dictionary<string, long>();
            try
            {
                if (PerformanceCounterCategory.Exists("GPU Adapter Memory"))
                {
                    InstanceDataCollection memory = new PerformanceCounterCategory("GPU Adapter Memory").ReadCategory()["Dedicated Usage"];
                    if (memory != null)
                        foreach (InstanceData i in memory.Values)
                        {
                            int luid = i.InstanceName.IndexOf("luid_");
                            if (luid >= 0)
                                dedicated[i.InstanceName.Substring(luid)] = i.RawValue;
                        }
                }
            }
            catch (Exception) { }
            int index = 0;
            foreach (var i in adapters.OrderBy(x => x.Key))
            {
                double usage = Math.Min(100, i.Value.Values.DefaultIfEmpty(0).Max());
                long memory_used;
                bool has_memory = dedicated.TryGetValue(i.Key, out memory_used);
                StringBuilder engines = new StringBuilder();
                foreach (var j in i.Value.OrderByDescending(x => x.Value).Take(4))
                    engines.Append(j.Key + ": " + Math.Round(Math.Min(100, j.Value)) + " %    ");
                list.Add(new ToolPerfReading()
                {
                    Key = "gpu:" + i.Key,
                    Name = "GPU " + index,
                    Value = Math.Round(usage) + " %" + (has_memory ? "  " + Functions.FileSize(memory_used, 1) : ""),
                    A = usage,
                    Percent = true,
                    Label1 = "Use (busiest engine)",
                    Details = (string.IsNullOrEmpty(gpu_names) ? "" : "Adapters: " + gpu_names + "\n") + engines.ToString().Trim() + (has_memory ? "\nDedicated memory in use: " + Functions.FileSize(memory_used, 2) : "")
                });
                index++;
            }
        }
    }
}