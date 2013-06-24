using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
    /// Interaction logic for Folder_Picker.xaml
    /// </summary>
    public partial class Folder_Picker : Window
    {
        public Folder_Picker()
        {
            InitializeComponent();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            foreach (var i in DriveInfo.GetDrives())
            {
                TreeViewItem TreeItem = new TreeViewItem();
                TreeItem.Header = i.Name;
                FolderExplorer.Items.Add(TreeItem);
            }
        }
    }
}
