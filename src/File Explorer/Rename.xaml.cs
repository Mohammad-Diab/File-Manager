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
    /// Interaction logic for Rename.xaml
    /// </summary>
    public partial class Rename : Window
    {
        public Rename()
        {
            InitializeComponent();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            TabIndex = 10;
            name.SelectAll();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (name.Text.Contains("*") || name.Text.Contains("/") || name.Text.Contains("\\") || name.Text.Contains("\"") || name.Text.Contains("?") || name.Text.Contains("<") || name.Text.Contains(">") || name.Text.Contains("|"))
            {
                _name.Height = 160;
                Label1.Text = "File name can't contain any of these characters:\n\\ / : * ? \" < > |";
                Label1.Visibility = Visibility.Visible;
            }
            else
            {
                MainWindow.new_Name = name.Text;
                this.Close();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MainWindow.new_Name = null;
            this.Close();
        }
    }
}
