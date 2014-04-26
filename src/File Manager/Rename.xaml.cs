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
        bool accept = false;
        public Rename()
        {
            InitializeComponent();
        }

        public Rename(string name)
        {
            InitializeComponent();
            this.name.Text = name;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            TabIndex = 10;
            name.Focus();
            name.SelectAll();
            Label1.Visibility = Visibility.Visible;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (name.Text == string.Empty)
            {
                _name.Height = 160;
                Label1.Visibility = Visibility.Visible;
                Label1.Text = "You must write a name.";
            }
            else
            {
                accept = true;
                this.Close();
            }
        }

        private void Name_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (name.Text.Contains("*") || name.Text.Contains("/") || name.Text.Contains("\\") || name.Text.Contains("\"") || name.Text.Contains("?") || name.Text.Contains("<") || name.Text.Contains(">") || name.Text.Contains("|"))
            {
                int i = name.CaretIndex;
                name.Text = name.Text.Replace("*", "").Replace("/", "").Replace("\\", "").Replace("\"", "").Replace("?", "").Replace("<", "").Replace(">", "").Replace("|", "").Replace(".", "");
                Label1.Text = "File name can't contain any of these characters:\n\\ / : * ? \" > |";
                Label1.Visibility = Visibility.Visible;
                _name.Height = 160;
                name.CaretIndex = i - 1;
            }
            else
                _name.Height = 120;
        }

        internal string ShowthisDialog()
        {
            this.ShowDialog();
            if (accept)
                return name.Text;
            else
                return null;
        }

        private void _name_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                accept = false;
                Close();
            }
        }
    }
}
