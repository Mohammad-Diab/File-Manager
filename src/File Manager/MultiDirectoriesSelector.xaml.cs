using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        ObservableCollection<GeneralPurpose> list_to_add = new ObservableCollection<GeneralPurpose>();
        bool accept = false;
        public MultiDirectoriesSelector()
        {
            InitializeComponent();
            list_of_directories.DataContext = list_to_add;
        }

        internal bool ShowDialog(List<string> paths)
        {
            ShowDialog();
            if (accept)
            {
                foreach (var i in list_to_add)
                    paths.Add(i.FullPath);
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
                bw.DoWork += (object dowork_sender, DoWorkEventArgs dowork_e) => { Thread.Sleep(280); };
                bw.RunWorkerCompleted += (object completed_sender, RunWorkerCompletedEventArgs completed_e) => { Done.IsEnabled = true; };
                bw.RunWorkerAsync();
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)   //check if user click ok in folder picker
                AddDirectoryToList(path);
        }

        private void AddDirectoryToList(string path)
        {
            if (Directory.Exists(path))
            {
                bool? isExist = false;  //true:Exist  false:notExist  null:is a father of one of directory in the list
                if (path[path.Length - 1] != '\\') //add slash to end of the path, then we can check if directory is exist or its a child of any existing directory
                    path = path + "\\";
                foreach (GeneralPurpose i in list_to_add)
                    if (path.ToLower().Contains(i.FullPath.ToLower()))
                    {
                        isExist = true;
                        break;
                    }
                    else if (i.FullPath.ToLower().Contains(path.ToLower()))
                    {
                        isExist = null;
                        break;
                    }
                if (isExist == false)
                    list_to_add.Add(new GeneralPurpose(path, null));
                //list_to_add.Items.Add(MakeGridForDirectoriesItem(path));
                else if (isExist == true)
                    new My_MessageBox("FILE MANAGER", "Directory is already Exist in the list or in a directory in the list!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
                else
                    new My_MessageBox("FILE MANAGER", "you have first to remove all \"" + path + "\" suns from the list", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
            }
            else
                new My_MessageBox("FILE MANAGER", "Path '" + path + "' has not been found!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            list_to_add.Clear();
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            for (int i = list_of_directories.SelectedItems.Count - 1; i > -1; i--)
                list_to_add.Remove((GeneralPurpose)list_of_directories.SelectedItems[i]);
        }

        private void ListOfDirectories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list_of_directories.SelectedItems.Count > 0)
                remove_selected.IsEnabled = true;
            else
                remove_selected.IsEnabled = false;
        }

        private void Search_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                AddDirectoryToList(to_add_path.Text);
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
            AddDirectoryToList(to_add_path.Text);
        }
    }
}