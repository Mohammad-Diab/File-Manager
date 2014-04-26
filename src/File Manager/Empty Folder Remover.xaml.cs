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
    /// Interaction logic for Empty_Folder_Remover.xaml
    /// </summary>
    public partial class Empty_Folder_Remover : Window
    {
        Tree directory_tree;
        Point point = new Point(0.5, 0.5);
        RotateTransform angel = new RotateTransform(45);
        //Thickness[] old_margins = { new Thickness(10, 10, 0, 0), new Thickness(30, 10, 0, 0), new Thickness(110, 10, 10, 0), new Thickness(110, 34, 10, 0), new Thickness(110, 58, 11, 0), new Thickness(10, 0, 10, 3), new Thickness(10, 0, 10, 3), new Thickness(0, 40, 10, 0) };
        Thickness[] margins = { new Thickness(10, 7, 0, 0), new Thickness(31, 3, 0, 0), new Thickness(60, 5, 10, 0), new Thickness(110, 34, 10, 0), new Thickness(110, 58, 11, 0), new Thickness(10, 0, 10, 3), new Thickness(10, 0, 10, 3), new Thickness(26, 8, 0, 0) };

        public Empty_Folder_Remover()
        { InitializeComponent(); }

        private void DeleteItems_Click(object sender, RoutedEventArgs e)
        {
            List<string> files = new List<string>();
            List<string> dires = new List<string>();
            foreach (TreeViewItem i in ItemsTree.Items)
            {
                AddItem(i, files, dires);
                if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && i.IsEnabled == true)
                    if (File.Exists(i.HeaderStringFormat))
                        files.Add(i.HeaderStringFormat);
                    else
                        dires.Add(i.HeaderStringFormat);
            }
            if (files.Count + dires.Count > 0)
            {
                My_MessageBox messagebox;
                if (files.Count + dires.Count > 1)
                    messagebox = new My_MessageBox(files.Count + dires.Count);
                else if (files.Count == 1)
                    messagebox = new My_MessageBox(new FileInfo(files[0]));
                else
                    messagebox = new My_MessageBox(new DirectoryInfo(dires[0]));
                if (messagebox.ShowThisDialog() == MyMessageBoxResult.Yes)
                    new File_Operation(files, dires).ShowDialog();
                foreach (TreeViewItem i in ItemsTree.Items)
                {
                    if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && (!File.Exists(i.HeaderStringFormat) && (!Directory.Exists(i.HeaderStringFormat))))
                    {
                        TextBlock del = new TextBlock();
                        del.Text = "Deleted";
                        del.HorizontalAlignment = HorizontalAlignment.Left;
                        del.Margin = margins[7];
                        del.FontSize = 9.5;
                        del.RenderTransformOrigin = point;
                        del.FontWeight = FontWeights.Bold;
                        del.Foreground = Brushes.Red;
                        del.VerticalAlignment = VerticalAlignment.Top;
                        del.RenderTransform = angel;
                        ((Grid)i.Header).Children.Add(del);
                        i.IsEnabled = false;
                    }
                    CheckBoxSun(i);
                }
            }
            else
                new My_MessageBox("Delete files", "You have not selected any file to been deleted!", new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-warning.png")), new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav")).ShowDialog();
        }

        private void AddItem(TreeViewItem item, List<string> files, List<string> dires)
        {
            foreach (TreeViewItem i in item.Items)
            {
                AddItem(i, files, dires);
                if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && i.IsEnabled == true)
                {
                    if (File.Exists(i.HeaderStringFormat))
                        files.Add(i.HeaderStringFormat);
                    else
                        dires.Add(i.HeaderStringFormat);
                }
            }
        }

        private void FindEmptey_Click(object sender, RoutedEventArgs e)
        {
            string path = new Folder_Picker().ShowThisDialog();
            if (path != null)
            {
                DirectoryInfo root_directory = new DirectoryInfo(path);
                directory_tree = new Tree(null, new BigItemElement(root_directory, 0, true));
                long full_size = GetAllFiles(root_directory, directory_tree);
                directory_tree.SetSize(full_size);
                PrintResult(directory_tree);
                if (ItemsTree.Items.Count > 0)
                    DeleteItems.IsEnabled = true;
                else
                {
                    DeleteItems.IsEnabled = false;
                    MessageBox.Show("no empety folder here");
                }
            }
        }

        private void PrintResult(Tree directory_tree)
        {
            foreach (Tree i in directory_tree.children)
                if (i.content.size == 0)
                    ItemsTree.Items.Add(MakeTreeViewItem(i, 40));
                else
                    PrintResult(i);
        }

        private TreeViewItem MakeTreeViewItem(Tree item, double level)
        {
            int file_index = 0;
            if (!item.content.is_directory)
                file_index = Functions.GetFileIndex(item.content.file_system.Extension);

            CheckBox ch = new CheckBox();
            ch.HorizontalAlignment = HorizontalAlignment.Left;
            ch.VerticalAlignment = VerticalAlignment.Top;
            ch.Margin = margins[0];
            ch.IsChecked = true;
            ch.Checked += Ch_Checked;
            ch.Unchecked += Ch_Unchecked;

            Image image = new Image();
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.VerticalAlignment = VerticalAlignment.Top;
            image.Width = image.Height = 24;
            image.Margin = margins[1];
            if (item.content.is_directory)
                image.Source = FileInformation.file_bitmap_24[FileInformation.file_bitmap_24.Count - 2];
            else
                image.Source = FileInformation.file_bitmap_24[file_index];

            TextBlock tx1 = new TextBlock();
            tx1.VerticalAlignment = VerticalAlignment.Top;
            tx1.FontSize = 14;
            tx1.Margin = margins[2];
            tx1.TextTrimming = TextTrimming.CharacterEllipsis;
            tx1.Text = item.content.file_system.FullName;

            //TextBlock tx2 = new TextBlock();
            //tx2.VerticalAlignment = VerticalAlignment.Top;
            //tx2.FontSize = 14;
            //tx2.Margin = margins[3];
            //tx2.TextTrimming = TextTrimming.CharacterEllipsis;
            //tx2.Text = "Type: " + (item.content.is_directory ? "Directory" : Functions.GetFileType(file_index, item.content.file_system.Extension));

            //TextBlock tx3 = new TextBlock();
            //tx3.VerticalAlignment = VerticalAlignment.Top;
            //tx3.FontSize = 14;
            //tx3.Margin = margins[4];
            //tx3.TextTrimming = TextTrimming.CharacterEllipsis;
            //tx3.Text = "Date Modified: " + item.content.file_system.LastWriteTimeUtc;

            //Rectangle separator = new Rectangle();
            //separator.Fill = Brushes.Silver;
            //separator.Height = 1;
            //separator.Margin = margins[5];
            //separator.VerticalAlignment = VerticalAlignment.Bottom;

            Grid container = new Grid();
            container.Height = 30;
            container.Width = ItemsTree.ActualWidth - level;
            container.Children.Add(ch);
            container.Children.Add(image);
            container.Children.Add(tx1);
            //container.Children.Add(tx2);
            //container.Children.Add(tx3);
            //container.Children.Add(separator);

            TreeViewItem TVI = new TreeViewItem();
            TVI.ToolTip = item.content.file_system.FullName;
            TVI.Tag = item.content.file_system;
            TVI.HeaderStringFormat = item.content.file_system.FullName;
            TVI.Header = container;
            foreach (Tree i in item.children)
                TVI.Items.Add(MakeTreeViewItem(i, level + 20));

            return TVI;
        }

        private void Ch_Unchecked(object sender, RoutedEventArgs e)
        {
            TreeViewItem checks = ((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent);
            foreach (TreeViewItem i in checks.Items)
                ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = false;
            CheckParent(checks);
        }

        private void Ch_Checked(object sender, RoutedEventArgs e)
        {
            TreeViewItem checks = ((TreeViewItem)((Grid)((CheckBox)sender).Parent).Parent);
            foreach (TreeViewItem i in checks.Items)
                ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = true;
            CheckParent(checks);
        }

        private void CheckParent(TreeViewItem checks)
        {
            while (checks.Parent != ItemsTree)
            {
                checks = ((TreeViewItem)checks.Parent);
                int count = 0, nullable = 0;
                foreach (TreeViewItem i in checks.Items)
                    if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true)
                        count++;
                    else if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == null)
                        nullable++;
                if (count == 0 && nullable == 0)
                    ((CheckBox)((Grid)checks.Header).Children[0]).IsChecked = false;
                else if (count == checks.Items.Count)
                    ((CheckBox)((Grid)checks.Header).Children[0]).IsChecked = true;
                else
                    ((CheckBox)((Grid)checks.Header).Children[0]).IsChecked = null;
            }
        }

        private long GetAllFiles(DirectoryInfo directory, Tree parent)
        {
            long size = 0;
            foreach (var i in directory.GetDirectories())
                try
                {
                    Tree sun = new Tree(parent, new BigItemElement(i, 0, true));
                    parent.AddChild(sun);
                    long dir_size = GetAllFiles(i, sun);
                    sun.SetSize(dir_size);
                    size += dir_size;
                }
                catch (Exception) { }
            foreach (FileInfo i in directory.GetFiles())
            {
                parent.AddChild(new BigItemElement(i, i.Length, false));
                size += i.Length;
            }
            parent.children.Sort();
            return size;
        }

        private void ClearList_Click(object sender, RoutedEventArgs e)
        {
            ItemsTree.Items.Clear();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (TreeViewItem i in ItemsTree.Items)
            {
                ((Grid)i.Header).Width = ItemsTree.ActualWidth - 40;
                ChangeSize(i, 60);
            }
        }

        private void ChangeSize(TreeViewItem big_item, double level)
        {
            foreach (TreeViewItem i in big_item.Items)
            {
                ((Grid)i.Header).Width = ItemsTree.ActualWidth - level;
                ChangeSize(i, level + 25);
            }
        }

        private void CheckBoxSun(TreeViewItem item)
        {
            foreach (TreeViewItem i in item.Items)
            {
                if (((CheckBox)((Grid)i.Header).Children[0]).IsChecked == true && (!File.Exists(i.HeaderStringFormat) && (!Directory.Exists(i.HeaderStringFormat))))
                {
                    TextBlock del = new TextBlock();
                    del.Text = "Deleted";
                    del.HorizontalAlignment = HorizontalAlignment.Left;
                    del.Margin = margins[7];
                    del.FontSize = 9.5;
                    del.RenderTransformOrigin = point;
                    del.FontWeight = FontWeights.Bold;
                    del.Foreground = Brushes.Red;
                    del.VerticalAlignment = VerticalAlignment.Top;
                    del.RenderTransform = angel;
                    ((Grid)i.Header).Children.Add(del);
                    i.IsEnabled = false;
                }
                CheckBoxSun(i);
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (TreeViewItem i in ItemsTree.Items)
                ((CheckBox)((Grid)i.Header).Children[0]).IsChecked = true;
        }
    }
}
