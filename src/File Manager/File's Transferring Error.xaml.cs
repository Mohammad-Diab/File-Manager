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
    /// Interaction logic for File_s_Transferring_Error.xaml
    /// </summary>
    public partial class File_s_Transferring_Error : Window

    {
        List<UnCompleatOperation> uncomplete;
        bool wait = false;
        int accept = 0;//0 for cancel, 1 for OK, 2 for ratry all;
        Thickness[] Margins = { new Thickness(10, 0, 0, 0), new Thickness(10, 24, 0, 0), new Thickness(80, 24, 0, 0), new Thickness(80, 40, 0, 0), new Thickness(80, 56, 0, 0), new Thickness(80, 72, 0, 0), new Thickness(0, 41, 115, 0), new Thickness(0, 41, 25, 0), new Thickness(0, 95, 0, 0) };
        Brush my_grey = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));

        public File_s_Transferring_Error(List<UnCompleatOperation> uncomplete, string header)
        {
            this.uncomplete = uncomplete;
            InitializeComponent();
            foreach (var i in uncomplete)
                if (i.error_message != "File is already exists.")
                    container.Children.Add(MakeGrid(i));
            if (header != null)
                sub_containt.Text = header;
        }

        private UIElement MakeGrid(UnCompleatOperation i)
        {
            FileInfo source = new FileInfo(i.files_full_name);
            int file_index = Functions.GetFileIndex(source.Extension);

            TextBlock name = new TextBlock();
            name.FontSize = 14;
            name.HorizontalAlignment = HorizontalAlignment.Left;
            name.Margin = Margins[0];
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            name.VerticalAlignment = VerticalAlignment.Top;
            name.Text = source.Name;
            name.Foreground = my_grey;

            Rectangle separator = new Rectangle();
            separator.Fill = Brushes.YellowGreen;
            separator.Height = 1;
            separator.Margin = Margins[8];
            separator.VerticalAlignment = VerticalAlignment.Top;

            TextBlock size = new TextBlock();
            size.HorizontalAlignment = HorizontalAlignment.Left;
            size.Margin = Margins[4];
            size.TextTrimming = TextTrimming.CharacterEllipsis;
            size.VerticalAlignment = VerticalAlignment.Top;
            size.Text = Functions.FileSize(i.files_size);

            TextBlock type = new TextBlock();
            type.HorizontalAlignment = HorizontalAlignment.Left;
            type.Margin = Margins[2];
            type.TextTrimming = TextTrimming.CharacterEllipsis;
            type.VerticalAlignment = VerticalAlignment.Top;
            type.Text = FileInformation.file_description[file_index];

            TextBlock date = new TextBlock();
            date.HorizontalAlignment = HorizontalAlignment.Left;
            date.Margin = Margins[3];
            date.TextTrimming = TextTrimming.CharacterEllipsis;
            date.VerticalAlignment = VerticalAlignment.Top;
            date.Text = source.LastWriteTime.ToString();

            TextBlock error_message = new TextBlock();
            error_message.HorizontalAlignment = HorizontalAlignment.Left;
            error_message.Margin = Margins[5];
            error_message.TextTrimming = TextTrimming.CharacterEllipsis;
            error_message.VerticalAlignment = VerticalAlignment.Top;
            error_message.Foreground = Brushes.Tomato;
            error_message.Text = "Error Message: " + i.error_message;

            Image image = new Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.Margin = Margins[1];
            image.Width = image.Height = 64;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Source = FileInformation.file_bitmap_64[file_index];

            RadioButton skip = new RadioButton();
            skip.VerticalAlignment = VerticalAlignment.Top;
            skip.HorizontalAlignment = HorizontalAlignment.Right;
            skip.Margin = Margins[7];
            skip.Content = "Skip";
            skip.FontSize = 14;
            skip.Checked += From_Checked;

            RadioButton retry = new RadioButton();
            retry.HorizontalAlignment = HorizontalAlignment.Right;
            retry.VerticalAlignment = VerticalAlignment.Top;
            retry.Margin = Margins[6];
            retry.Content = "Retry";
            retry.FontSize = 14;
            retry.Checked += From_Checked;

            if (i.IsDirectory)
            {
                image.Source = FileInformation.file_bitmap_64[FileInformation.file_bitmap_64.Count - 2];
                type.Text = "Directory";
            }

            if (i.files_size == -1)
                size.Text = "";

            Grid grid = new Grid();
            grid.ToolTip = i.files_full_name;
            grid.Children.Add(name);
            grid.Children.Add(separator);
            grid.Children.Add(skip);
            grid.Children.Add(retry);
            grid.Children.Add(size);
            grid.Children.Add(type);
            grid.Children.Add(date);
            grid.Children.Add(error_message);
            grid.Children.Add(image);

            return grid;
        }

        private void From_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckSkip(2, skip_radiobutton);
            CheckUncheckSkip(3, retry_radiobutton);
        }

        private void CheckUncheckSkip(int index, RadioButton radio)
        {
            if (!wait)
            {
                int j = 0;
                foreach (Grid i in container.Children)
                    if (((RadioButton)i.Children[index]).IsChecked == true)
                        j++;
                if (j == container.Children.Count)
                    radio.IsChecked = true;
                else if (j == 0)
                    radio.IsChecked = false;
                else
                    radio.IsChecked = null;
            }
        }

        private void CheckUncheckAll(bool p, int index)
        {
            wait = true;
            foreach (Grid i in container.Children)
                ((RadioButton)i.Children[index]).IsChecked = p;
            wait = false;
        }

        private void RetryAll_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, 3);
        }

        private void SkipAll_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, 2);
        }

        private void RetryAll_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, 3);
        }

        private void SkipAll_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, 2);
        }

        internal List<bool> ShowThisDialog()
        {
            List<bool> retry = new List<bool>();
            this.ShowDialog();
            if (accept == 0)
                foreach (var i in container.Children)
                    retry.Add(false);
            else if (accept == 1)
                foreach (Grid i in container.Children)
                    if (((RadioButton)i.Children[3]).IsChecked == true)
                        retry.Add(true);
                    else
                        retry.Add(false);
            else
                foreach (var i in container.Children)
                    retry.Add(true);
            return retry;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            accept = 1;
            Close();
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            accept = 2;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            accept = 0;
            Close();
        }
    }
}
