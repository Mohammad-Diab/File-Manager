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

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Rename.xaml
    /// </summary>
    public partial class Rename : Window
    {
        bool accept = false;
        public Rename()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            name.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (name.Text == string.Empty)
                MessageBox.Show("You must write a file name!", "FILE MANAGER");
            else
            {
                accept = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            accept = false;
            Close();
        }

        private void Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            var InvalidChars = Path.GetInvalidFileNameChars();
            if (name.Text.Any(x => InvalidChars.Any(y => x == y)))
            {
                new My_MessageBox("FILE MANAGER", string.Format("File name can't contain any of these characters:\n{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15} {16} {17} {18} {19} {20} {21} {22} {32} {24} {25} {26} {27} {28} {29} {30} {31} {32} {33} {34} {35} {36} {37} {38} {39} {40} {41}", InvalidChars), new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav")).ShowDialog();
                int pos = name.CaretIndex;
                foreach (char i in Path.GetInvalidFileNameChars())
                    name.Text.Replace(i.ToString(), "");
                name.CaretIndex = pos - 1;
            }
        }

        internal string ShowWindow(string name, bool is_file)
        {
            if (is_file)
            {
                this.name.Text = Path.GetFileName(name);
                if (this.name.Text.Contains('.'))
                    this.name.Select(0, this.name.Text.LastIndexOf('.'));
                else
                    this.name.SelectAll();
            }
            else
            {
                this.name.Text = name;
                this.name.SelectAll();
            }
            ShowDialog();
            if (accept)
                return this.name.Text;
            else
                return null;
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Cancel_Click(null, null);
        }
    }
}
