using IWshRuntimeLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for New_Shortcut.xaml
    /// </summary>
    public partial class New_Shortcut : Window
    {
        public New_Shortcut()
        {
            InitializeComponent();
        }

        #region Shortcut Section

        private void CreateShortcut_Click(object sender, RoutedEventArgs e)
        {
            bool replay = true;
            do
            {
                if (Directory.Exists(o_path.Text) || System.IO.File.Exists(o_path.Text))
                {
                    if (Directory.Exists(s_path.Text))
                    {
                        if (!System.IO.File.Exists(s_path.Text + "\\" + s_name.Text) && !Directory.Exists(s_path.Text + "\\" + s_name.Text))
                        {
                            try
                            {
                                IWshShortcut shortcut = new IWshShell_Class().CreateShortcut(s_path.Text + "\\" + s_name.Text);
                                shortcut.TargetPath = o_path.Text;
                                //shortcut.IconLocation = "Location of icon";
                                shortcut.Description = desc.Text;
                                shortcut.Save();
                                replay = false;
                            }
                            catch (Exception ex/* extra info*/)
                            {
                                OperationTree unsucsess = new OperationTree(null, false, "", OperationType.Attributes, null, false, "We can not create shortcut.");
                                if (System.IO.File.Exists(o_path.Text))
                                    unsucsess.children.Add(new OperationTree(new FileInfo(o_path.Text), false, "", OperationType.CreateShortcut, unsucsess, "We can not create shortcut."));
                                else if (Directory.Exists(o_path.Text))
                                    unsucsess.children.Add(new OperationTree(new DirectoryInfo(o_path.Text), false, "", OperationType.CreateShortcut, unsucsess, false, "We can not create shortcut."));
                                else
                                    unsucsess.children.Add(new OperationTree(new FileInfo(o_path.Text), false, "", OperationType.CreateShortcut, unsucsess, "We can not find original file."));
                                new File_s_Transferring_Error(unsucsess, "Error while creating shortcut.").ShowThisDialog();
                                if (unsucsess.children[0].user_option == OperationOption.Skip)
                                    replay = false;
                            }
                        }
                    }
                }
            } while (replay);
            Close();
        }

        private void SelectOrginalItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                o_path.Text = ofd.FileName;
                s_name.Text = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName) + ".lnk";
            }
        }

        private void SelectLinkDestination_Click(object sender, RoutedEventArgs e)
        {
            Folder_Picker fp = new Folder_Picker();
            string path;
            if ((path = fp.ShowThisDialog()) != null)
                o_path.Text = path;
        }

        #endregion


        #region Junction Section

        private void CreateJunction_Click(object sender, RoutedEventArgs e)
        {
            bool replay = true;
            do
            {
                if (Directory.Exists(d_path.Text))
                {
                    bool not_empty = false;
                    try
                    {
                        if (Directory.Exists(j_path.Text))
                            if (Directory.GetFiles(j_path.Text).Length == 0)
                                if (Directory.GetDirectories(j_path.Text).Length == 0)
                                {
                                    Classes.JunctionPoint.Create(j_path.Text, d_path.Text, false);
                                    replay = false;
                                }
                                else
                                    not_empty = true;
                            else
                                not_empty = true;
                        else
                        {
                            Classes.JunctionPoint.Create(j_path.Text, d_path.Text, create_direcory.IsChecked.Value);
                            replay = false;
                        }
                    }
                    catch (Exception ex/* extra info*/)
                    {
                        OperationTree unsucsess = new OperationTree(null, false, "", OperationType.Attributes, null, false, "We can not create reparse point.");
                        if (not_empty)
                            unsucsess.children.Add(new OperationTree(new DirectoryInfo(d_path.Text), false, "", OperationType.CreateShortcut, unsucsess, false, "Target directory is not empty."));
                        else if (Directory.Exists(d_path.Text))
                            unsucsess.children.Add(new OperationTree(new DirectoryInfo(d_path.Text), false, "", OperationType.CreateShortcut, unsucsess, false, "We can not create reparse point."));
                        else
                            unsucsess.children.Add(new OperationTree(new DirectoryInfo(d_path.Text), false, "", OperationType.CreateShortcut, unsucsess, false, "We can not find original directory."));
                        new File_s_Transferring_Error(unsucsess, "Error while creating reparse point.").ShowThisDialog();
                        if (unsucsess.children[0].user_option == OperationOption.Skip)
                            replay = false;
                    }
                }
            }
            while (replay);
            Close();
        }

        private void SelectJunctionTarget_Click(object sender, RoutedEventArgs e)
        {
            Folder_Picker fp = new Folder_Picker();
            string path;
            if ((path = fp.ShowThisDialog()) != null)
                j_path.Text = path;
        }

        private void SelectSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            Folder_Picker fp = new Folder_Picker();
            string path;
            if ((path = fp.ShowThisDialog()) != null)
                d_path.Text = path;
        }

        #endregion


        #region Apperance

        private void Label1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ThicknessAnimation tar = new ThicknessAnimation(new Thickness(0, 41, 0, 0), new Duration(new TimeSpan(1000000)));
            ThicknessAnimation tag = new ThicknessAnimation(new Thickness(0), new Duration(new TimeSpan(1500000)));
            mother.BeginAnimation(Grid.MarginProperty, tag);
            rec.BeginAnimation(Rectangle.MarginProperty, tar);
            rec.Width = label1.ActualWidth;
            label1.Background = Brushes.White;
            label1.Foreground = Brushes.Black;
            label2.Background = null;
            label2.Foreground = Brushes.White;

        }

        private void label2_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ThicknessAnimation ta = new ThicknessAnimation(new Thickness(label1.ActualWidth, 41, 0, 0), new Duration(new TimeSpan(1000000)));
            ThicknessAnimation tag = new ThicknessAnimation(new Thickness(-500, 0, 0, 0), new Duration(new TimeSpan(1500000)));
            mother.BeginAnimation(Grid.MarginProperty, tag);
            rec.BeginAnimation(Rectangle.MarginProperty, ta);
            rec.Width = label2.ActualWidth;
            label1.Background = null;
            label1.Foreground = Brushes.White;
            label2.Background = Brushes.White;
            label2.Foreground = Brushes.Black;
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.Width = label1.ActualWidth;
        }
    }
}
