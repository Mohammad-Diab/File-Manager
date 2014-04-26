using System;
using System.Collections.Generic;
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
    /// Interaction logic for File_Compresser.xaml
    /// </summary>
    public partial class File_Compresser : Window
    {
        //ObservableCollection<ListItem> list_items;
        public File_Compresser()
        {
            InitializeComponent();
            //list_items = new ObservableCollection<ListItem>();
            //list.DataContext = list_items;
        }

        //private void AddFolder_Click(object sender, RoutedEventArgs e)
        //{
        //    string path = new Folder_Picker().ShowThisDialog();
        //    if (path != null)
        //        list_items.Add(new ListItem(path/*, ItemType.Directory*/));
        //}

        //private void AddFiles_Click(object sender, RoutedEventArgs e)
        //{
        //    var OFD = new Microsoft.Win32.OpenFileDialog();
        //    OFD.Multiselect = true;
        //    if (OFD.ShowDialog() == true)
        //        foreach (var i in OFD.FileNames)
        //            list_items.Add(new ListItem(i/*, ItemType.File*/));
        //}
    }
}
