using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for MultiDirectoriesSelector.xaml
    /// </summary>
    public partial class MultiDirectoriesSelector : Window
    {
        bool accept = false;
        Thickness[] margins = { new Thickness(5, 3, 0, 0), new Thickness(35, 5, 0, 0) };
        public MultiDirectoriesSelector()
        {
            InitializeComponent();
        }

        internal bool ShowDialog(List<string> paths)
        {
            ShowDialog();
            if (accept)
            {
                foreach (Grid i in list_of_directories.Items)
                    paths.Add(i.ToolTip.ToString());
            }
            return accept;
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            if (list_of_directories.Items.Count > 0)
            {
                accept = true;
                Close();
            }
            else
            {
                Functions.ShuckWindow(this, list_of_directories.Items.Count == 0, "List is empty!");
                Done.IsEnabled = false;
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += bw_DoWork;
                bw.RunWorkerCompleted += bw_RunWorkerCompleted;
                bw.RunWorkerAsync();
            }
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(280);
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Done.IsEnabled = true;
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)   //check if user chick ok in folder picker
                AddDirectoryToList(path);
        }

        private void AddDirectoryToList(string path)
        {
            bool? isExist = false;  //true:Exist  false:notExist  null:is afather of one of directory in the list
            if (path[path.Length - 1] != '\\') //add slash to end of the path, then we can check if directory is exist or its a child of any existing directory
                path = path + "\\";
            foreach (Grid i in list_of_directories.Items)
                if (path.ToLower().Contains(i.ToolTip.ToString().ToLower()))
                {
                    isExist = true;
                    break;
                }
                else if (i.ToolTip.ToString().ToLower().Contains(path.ToLower()))
                {
                    isExist = null;
                    break;
                }
            if (isExist==false)
                list_of_directories.Items.Add(MakeGridForDirectoriesItem(path));
            else if (isExist == true)
                MessageBox.Show("Directory is already Exist in list");
            else
                MessageBox.Show("you have first to remove all \"" + path + "\" suns from the list");
        }

        private Grid MakeGridForDirectoriesItem(string path)
        {
            Image image = new Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Width = image.Height = 24;
            image.Margin = margins[0];
            image.Source = FileInformation.file_bitmap_24[FileInformation.file_bitmap_24.Count - 2];

            TextBlock tx1 = new TextBlock();
            tx1.VerticalAlignment = VerticalAlignment.Top;
            tx1.FontSize = 14;
            tx1.Margin = margins[1];
            tx1.TextTrimming = TextTrimming.CharacterEllipsis;
            tx1.Text = path;

            Grid container = new Grid();
            container.Height = 30;
            container.Width = list_of_directories.ActualWidth - 45;
            container.Children.Add(image);
            container.Children.Add(tx1);
            container.ToolTip = path;

            return container;
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            list_of_directories.Items.Clear();
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            for (int i = list_of_directories.SelectedItems.Count - 1; i > -1; i--)
                list_of_directories.Items.Remove(list_of_directories.SelectedItems[i]);
        }

        private void ListOfDirectories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list_of_directories.SelectedItems.Count > 0)
                remove_selected.IsEnabled = true;
            else
                remove_selected.IsEnabled = false;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (Grid i in list_of_directories.Items)
                i.Width = list_of_directories.ActualWidth - 45;
        }

        private void search_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Directory.Exists(to_add_path.Text))
                    AddDirectoryToList(to_add_path.Text);
                else
                    MessageBox.Show("path not found");
            }
        }

        private void to_add_path_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            path_box_label.Visibility = Visibility.Collapsed;
        }

        private void to_add_path_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (to_add_path.Text == string.Empty)
                path_box_label.Visibility = Visibility.Visible;
        }

        private void AddFolderFromPath_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(to_add_path.Text))
                AddDirectoryToList(to_add_path.Text);
            else
                MessageBox.Show("path not found");
        }
    }
}