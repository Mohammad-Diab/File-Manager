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
        bool accept = false, is_file_name = false;
        static string english_keys = "`qwertyuiop[]asdfghjkl;'zxcvnm,", arabic_keys = "ذضصثقفغعهخحجدشسيبلاتنمكطئءؤرىةو";
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
            is_file_name = is_file;
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

        private void SwitchLanguage_Click(object sender, RoutedEventArgs e)
        {
            string text = name.Text;
            bool to_english = text.Any(x => arabic_keys.IndexOf(x) >= 0);
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (to_english)
                {
                    if (c == 'ل' && i + 1 < text.Length && text[i + 1] == 'ا')
                    {
                        result.Append('b');
                        i++;
                    }
                    else
                        result.Append(arabic_keys.IndexOf(c) >= 0 ? english_keys[arabic_keys.IndexOf(c)] : c);
                }
                else if (char.ToLower(c) == 'b')
                    result.Append("لا");
                else
                    result.Append(english_keys.IndexOf(char.ToLower(c)) >= 0 ? arabic_keys[english_keys.IndexOf(char.ToLower(c))] : c);
            }
            name.Text = result.ToString();
            name.Focus();
            name.CaretIndex = name.Text.Length;
        }

        private void RandomName_Click(object sender, RoutedEventArgs e)
        {
            string random = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            int dot = is_file_name ? name.Text.LastIndexOf('.') : -1;
            name.Text = random + (dot > 0 ? name.Text.Substring(dot) : "");
            name.Focus();
            name.Select(0, random.Length);
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Cancel_Click(null, null);
        }
    }
}
