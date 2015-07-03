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
                Messages.ShowMessage("Please write a file name.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
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
                Messages.ShowMessage("File extension has some invalid characters.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, string.Format("File extension can't contain any of these characters:\n" + "'\"' ' < ' ' > ' ' | ' ':' ' * ' ' ? ' '\\' ' / '.\nAnd some other special chars:\n'\\t (tab)' '\\n (enter)' '\\r' '\\0' '\\a' '\\b' '\\v' '\\f'."));
                int pos = name.CaretIndex;
                foreach (char i in Path.GetInvalidFileNameChars())
                    name.Text = name.Text.Replace(i.ToString(), "");
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
