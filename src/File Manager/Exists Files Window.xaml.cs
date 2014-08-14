using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        ObservableCollection<ExistsItemsListView> ExistsItemsList = new ObservableCollection<ExistsItemsListView>();
        List<UnCompleatedOperation> uncomplete;
        bool accept = false, user_active = false;
        string paste_in;
        List<int> same, same_size, same_size_date;
        public Exists_Files_Window(List<UnCompleatedOperation> uncomplete, string path)
        {
            paste_in = path;
            this.uncomplete = uncomplete;
            InitializeComponent();
            foreach (var i in uncomplete)
                if (i.ErrorMessage == "File is already exists.")
                    ExistsItemsList.Add(new ExistsItemsListView((FileInfo)i.Item.Item, new FileInfo(paste_in + "\\" + i.Item.Item.Name)));
            container.DataContext = ExistsItemsList;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            accept = false;
            this.Close();
        }

        private void CheckSource_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, -1);
        }

        private void CheckSource_Unloaded(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, -1);
        }

        private void CheckTarget_Checked(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(true, 1);
        }

        private void CheckTarget_Unloaded(object sender, RoutedEventArgs e)
        {
            CheckUncheckAll(false, 1);
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
            foreach (ExistsItemsListView i in ExistsItemsList)
                if (index > 0)
                    i.TargetIsChecked = p;
                else if (index < 0)
                    i.SourceIsChecked = p;
        }





        private void CheckUncheckSource()
        {
            int j = 0;
            foreach (ExistsItemsListView i in ExistsItemsList)
                if (i.SourceIsChecked == true)
                    j++;
            if (j == ExistsItemsList.Count)
                source_checkbox.IsChecked = true;
            else if (j == 0)
                source_checkbox.IsChecked = false;
            else
                source_checkbox.IsChecked = null;
        }

        private void CheckUncheckTarget()
        {
            int j = 0;
            foreach (ExistsItemsListView i in ExistsItemsList)
                if (i.TargetIsChecked == true)
                    j++;
            if (j == ExistsItemsList.Count)
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
                for (int i = 0; i < ExistsItemsList.Count; i++)
                {
                    if (uncomplete[i].Item.Size == new FileInfo(paste_in + uncomplete[i].Item.Item.Name).Length)
                    {
                        SHA1 HashComputer = new SHA1Cng();
                        bool equal = true;
                        try
                        {
                            byte[] first = HashComputer.ComputeHash(File.OpenRead(uncomplete[i].Item.Item.FullName));
                            byte[] second = HashComputer.ComputeHash(File.OpenRead(paste_in + uncomplete[i].Item.Item.Name));
                            for (int j = 0; j < first.Length; j++)
                                if (!(first[j] == second[j]))
                                {
                                    equal = false;
                                    break;
                                }
                        }
                        catch (Exception) { equal = false; }
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
                for (int i = 0; i < ExistsItemsList.Count; i++)
                    if (uncomplete[i].Item.Size == new FileInfo(paste_in + uncomplete[i].Item.Item.Name).Length)
                        same_size.Add(i);
                same_file_size.Text = same_size.Count + "";
            }
        }

        private void RemoveSameSizeDate()
        {
            if (same_size_date == null)
            {
                same_size_date = new List<int>();
                for (int i = 0; i < ExistsItemsList.Count; i++)
                {
                    try
                    {
                        if (uncomplete[i].Item.Size == new FileInfo(paste_in + uncomplete[i].Item.Item.Name).Length)
                            if (File.GetLastWriteTime(uncomplete[i].Item.Item.FullName).Ticks == File.GetLastWriteTime(paste_in + uncomplete[i].Item.Item.Name).Ticks)
                                same_size_date.Add(i);
                    }
                    catch (Exception) { }
                }
                same_file_date.Text = same_size_date.Count + "";
            }
        }

        internal List<int> ShowThisDialog()
        {
            List<int> over = new List<int>();
            this.ShowDialog();
            if (!accept)
                foreach (var i in uncomplete)
                    over.Add(0);
            else
            {
                foreach (ExistsItemsListView i in ExistsItemsList)
                    if (i.SourceIsChecked == true)
                        if (i.TargetIsChecked == true)
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

        private void skip_same_SelectionChanged(object sender, SelectionChangedEventArgs e)//dont clear
        {
            //important
            //if (user_active)
            //{
            //    container.Children.Clear();
            //    foreach (var i in full)
            //        container.Children.Add(i);
            //    if (skip_items.SelectedIndex == 1)
            //    {
            //        RemoveSame();
            //        for (int i = same.Count - 1; i > -1; i--)
            //            container.Children.RemoveAt(same[i]);
            //    }
            //    else if (skip_items.SelectedIndex == 2)
            //    {
            //        RemoveSameSize();
            //        for (int i = same_size.Count - 1; i > -1; i--)
            //            container.Children.RemoveAt(same_size[i]);
            //    }
            //    else if (skip_items.SelectedIndex == 3)
            //    {
            //        RemoveSameSizeDate();
            //        for (int i = same_size_date.Count - 1; i > -1; i--)
            //            container.Children.RemoveAt(same_size_date[i]);
            //    }
            //    else if (skip_items.SelectedIndex == 4)
            //        for (int i = 0; i < container.Children.Count; i++)
            //            if (File.GetLastWriteTime(uncomplete[i].files_full_name).Ticks >= File.GetLastWriteTime(paste_in + uncomplete[i].file_short_name).Ticks)
            //            {
            //                ((CheckBox)((Grid)container.Children[i]).Children[2]).IsChecked = true;
            //                ((CheckBox)((Grid)container.Children[i]).Children[3]).IsChecked = false;
            //            }
            //            else
            //            {
            //                ((CheckBox)((Grid)container.Children[i]).Children[2]).IsChecked = false;
            //                ((CheckBox)((Grid)container.Children[i]).Children[3]).IsChecked = true;
            //            }
            //}
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            user_active = true;
        }

        private void container_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            container.SelectedIndex = -1;
        }
    }
}
