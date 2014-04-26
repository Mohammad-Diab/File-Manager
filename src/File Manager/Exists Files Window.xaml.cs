using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
    /// Interaction logic for Exists_Files_Window.xaml
    /// </summary>
    public partial class Exists_Files_Window : Window
    {
        List<UnCompleatOperation> uncomplete;
        bool accept = false, active = false;
        UIElement[] full;
        string paste_in;
        Thickness[] Margins = { new Thickness(10, 5, 0, 0), new Thickness(0, 95, 0, 0), new Thickness(70, 6, 0, 0), new Thickness(70, 27, 0, 0), new Thickness(70, 48, 0, 0), new Thickness(0, 5, 0, 0), new Thickness(10, 24, 0, 10), new Thickness(0, 24, 0, 10) };
        Brush my_grey = new SolidColorBrush(Color.FromArgb(255, 100, 100, 100));
        List<int> same, same_size, same_size_date;
        public Exists_Files_Window(List<UnCompleatOperation> uncomplete, string path)
        {
            paste_in = path;
            this.uncomplete = uncomplete;
            InitializeComponent();
            foreach (var i in uncomplete)
                if (i.error_message == "File is already exists.")
                    container.Children.Add(MakeGrid(i, paste_in));
            full = new UIElement[container.Children.Count];
            container.Children.CopyTo(full, 0);
        }

        private UIElement MakeGrid(UnCompleatOperation i, string paste_in)
        {
            FileInfo source = new FileInfo(i.files_full_name);
            FileInfo target = new FileInfo(paste_in + "\\" + i.file_short_name);
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
            separator.Margin = Margins[1];
            separator.VerticalAlignment = VerticalAlignment.Top;

            TextBlock from_size = new TextBlock();
            from_size.HorizontalAlignment = HorizontalAlignment.Left;
            from_size.Margin = Margins[2];
            from_size.TextTrimming = TextTrimming.CharacterEllipsis;
            from_size.VerticalAlignment = VerticalAlignment.Top;
            from_size.Text = Functions.FileSize(source.Length);

            TextBlock from_type = new TextBlock();
            from_type.HorizontalAlignment = HorizontalAlignment.Left;
            from_type.Margin = Margins[3];
            from_type.TextTrimming = TextTrimming.CharacterEllipsis;
            from_type.VerticalAlignment = VerticalAlignment.Top;
            from_type.Text = FileInformation.file_description[file_index];

            TextBlock from_date = new TextBlock();
            from_date.HorizontalAlignment = HorizontalAlignment.Left;
            from_date.Margin = Margins[4];
            from_date.TextTrimming = TextTrimming.CharacterEllipsis;
            from_date.VerticalAlignment = VerticalAlignment.Top;
            from_date.Text = source.LastWriteTime.ToString();

            Image from_image = new Image();
            from_image.HorizontalAlignment = HorizontalAlignment.Left;
            from_image.Margin = Margins[5];
            from_image.Width = from_image.Height = 64;
            from_image.VerticalAlignment = VerticalAlignment.Top;
            from_image.Source = FileInformation.file_bitmap_64[file_index];

            Grid from_grid = new Grid();
            from_grid.Children.Add(from_size);
            from_grid.Children.Add(from_type);
            from_grid.Children.Add(from_date);
            from_grid.Children.Add(from_image);

            CheckBox from = new CheckBox();
            from.ToolTip = source.FullName.Remove(source.FullName.Length - i.file_short_name.Length);
            from.HorizontalAlignment = HorizontalAlignment.Left;
            from.Margin = Margins[6];
            from.Width = 245;
            from.Content = from_grid;
            from.Checked += From_Checked;
            from.Unchecked += From_Checked;

            TextBlock to_size = new TextBlock();
            to_size.HorizontalAlignment = HorizontalAlignment.Left;
            to_size.Margin = Margins[2];
            to_size.TextTrimming = TextTrimming.CharacterEllipsis;
            to_size.VerticalAlignment = VerticalAlignment.Top;
            to_size.Text = Functions.FileSize(target.Length);

            TextBlock to_type = new TextBlock();
            to_type.HorizontalAlignment = HorizontalAlignment.Left;
            to_type.Margin = Margins[3];
            to_type.TextTrimming = TextTrimming.CharacterEllipsis;
            to_type.VerticalAlignment = VerticalAlignment.Top;
            to_type.Text = FileInformation.file_description[file_index];

            TextBlock to_date = new TextBlock();
            to_date.HorizontalAlignment = HorizontalAlignment.Left;
            to_date.Margin = Margins[4];
            to_date.TextTrimming = TextTrimming.CharacterEllipsis;
            to_date.VerticalAlignment = VerticalAlignment.Top;
            to_date.Text = target.LastWriteTime.ToString();

            Image to_image = new Image();
            to_image.HorizontalAlignment = HorizontalAlignment.Left;
            to_image.Margin = Margins[5];
            to_image.Width = from_image.Height = 64;
            to_image.VerticalAlignment = VerticalAlignment.Top;
            to_image.Source = FileInformation.file_bitmap_64[file_index];

            Grid to_grid = new Grid();
            to_grid.Children.Add(to_size);
            to_grid.Children.Add(to_type);
            to_grid.Children.Add(to_date);
            to_grid.Children.Add(to_image);

            CheckBox to = new CheckBox();
            to.ToolTip = paste_in;
            to.HorizontalAlignment = HorizontalAlignment.Right;
            to.Margin = Margins[7];
            to.Width = 245;
            to.Content = to_grid;
            to.Checked += To_Checked;
            to.Unchecked += To_Checked;

            Grid grid = new Grid();
            grid.Children.Add(name);
            grid.Children.Add(separator);
            grid.Children.Add(from);
            grid.Children.Add(to);
            return grid;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            accept = false;
            this.Close();
        }

        private void CheckSource_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, 2);
        }

        private void CheckSource_Unloaded(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, 2);
        }

        private void CheckTarget_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, 3);
        }

        private void CheckTarget_Unloaded(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, 3);
        }

        private void From_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckSource();
        }

        private void To_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckTarget();
        }

        private void CheckUncheckAll(bool p, int index)
        {
            foreach (Grid i in container.Children)
                ((CheckBox)i.Children[index]).IsChecked = p;
        }

        private void CheckUncheckSource()
        {
            int j = 0;
            foreach (Grid i in container.Children)
                if (((CheckBox)i.Children[2]).IsChecked == true)
                    j++;
            if (j == container.Children.Count)
                source_checkbox.IsChecked = true;
            else if (j == 0)
                source_checkbox.IsChecked = false;
            else
                source_checkbox.IsChecked = null;
        }

        private void CheckUncheckTarget()
        {
            int j = 0;
            foreach (Grid i in container.Children)
                if (((CheckBox)i.Children[3]).IsChecked == true)
                    j++;
            if (j == container.Children.Count)
                target_checkbox.IsChecked = true;
            else if (j == 0)
                target_checkbox.IsChecked = false;
            else
                target_checkbox.IsChecked = null;
        }

        private void RemoveSame()
        {
            if (same == null)
            {
                same = new List<int>();
                for (int i = 0; i < container.Children.Count; i++)
                {
                    if (uncomplete[i].files_size == new FileInfo(paste_in + uncomplete[i].file_short_name).Length)
                    {
                        SHA1 HashComputer = new SHA1Cng();
                        byte[] first = HashComputer.ComputeHash(File.OpenRead(uncomplete[i].files_full_name));
                        byte[] second = HashComputer.ComputeHash(File.OpenRead(paste_in + uncomplete[i].file_short_name));
                        bool equal = true;
                        for (int j = 0; j < first.Length; j++)
                            if (!(first[j] == second[j]))
                            {
                                equal = false;
                                break;
                            }
                        if (equal)
                            same.Add(i);
                    }
                }
                same_containt.Text = same.Count + "";
            }
        }

        private void RemoveSameSize()
        {
            if (same_size == null)
            {
                same_size = new List<int>();
                for (int i = 0; i < container.Children.Count; i++)
                    if (uncomplete[i].files_size == new FileInfo(paste_in + uncomplete[i].file_short_name).Length)
                        same_size.Add(i);
                same_file_size.Text = same_size.Count + "";
            }
        }

        private void RemoveSameSizeDate()
        {
            if (same_size_date == null)
            {
                same_size_date = new List<int>();
                for (int i = 0; i < container.Children.Count; i++)
                {
                    if (uncomplete[i].files_size == new FileInfo(paste_in + uncomplete[i].file_short_name).Length)
                        if (File.GetLastWriteTime(uncomplete[i].files_full_name).Ticks == File.GetLastWriteTime(paste_in + uncomplete[i].file_short_name).Ticks)
                            same_size_date.Add(i);
                }
                same_file_date.Text = same_size_date.Count + "";
            }
        }

        internal List<int> ShowThisDialog()
        {
            List<int> over = new List<int>();
            this.ShowDialog();
            if (!accept)
                foreach (var i in container.Children)
                    over.Add(0);
            else
            {
                foreach (Grid i in container.Children)
                    if (((CheckBox)i.Children[2]).IsChecked == true)
                        if (((CheckBox)i.Children[3]).IsChecked == true)
                            over.Add(2);
                        else
                            over.Add(1);
                    else
                        over.Add(0);
                switch (skip_items.SelectedIndex)
                {
                    case 1:
                        for (int i = 0; i < same.Count; i++)
                            over.Insert(same[i], 0);
                        break;
                    case 2:
                        for (int i = 0; i < same_size.Count; i++)
                            over.Insert(same_size[i], 0);
                        break;
                    case 3:
                        for (int i = 0; i < same_size_date.Count; i++)
                            over.Insert(same_size_date[i], 0);
                        break;
                    default:
                        break;
                }
            }
            return over;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            accept = true;
            this.Close();
        }

        private void skip_same_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (active)
            {
                container.Children.Clear();
                foreach (var i in full)
                    container.Children.Add(i);
                if (skip_items.SelectedIndex == 1)
                {
                    RemoveSame();
                    for (int i = same.Count - 1; i > -1; i--)
                        container.Children.RemoveAt(same[i]);
                }
                else if (skip_items.SelectedIndex == 2)
                {
                    RemoveSameSize();
                    for (int i = same_size.Count - 1; i > -1; i--)
                        container.Children.RemoveAt(same_size[i]);
                }
                else if (skip_items.SelectedIndex == 3)
                {
                    RemoveSameSizeDate();
                    for (int i = same_size_date.Count - 1; i > -1; i--)
                        container.Children.RemoveAt(same_size_date[i]);
                }
                else if (skip_items.SelectedIndex == 4)
                    for (int i = 0; i < container.Children.Count; i++)
                        if (File.GetLastWriteTime(uncomplete[i].files_full_name).Ticks >= File.GetLastWriteTime(paste_in + uncomplete[i].file_short_name).Ticks)
                        {
                            ((CheckBox)((Grid)container.Children[i]).Children[2]).IsChecked = true;
                            ((CheckBox)((Grid)container.Children[i]).Children[3]).IsChecked = false;
                        }
                        else
                        {
                            ((CheckBox)((Grid)container.Children[i]).Children[2]).IsChecked = false;
                            ((CheckBox)((Grid)container.Children[i]).Children[3]).IsChecked = true;
                        }
            }
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            active = true;
        }
    }
}
