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
            if (o_path.Text != string.Empty && s_name.Text != string.Empty && s_path.Text != string.Empty)
            {
                if (Directory.Exists(o_path.Text) || System.IO.File.Exists(o_path.Text))
                {
                    if (Directory.Exists(s_path.Text))
                    {
                        if (!System.IO.File.Exists(s_path.Text + "\\" + s_name.Text) && !Directory.Exists(s_path.Text + "\\" + s_name.Text))
                        {
                            if (icon_path.Text != string.Empty && System.IO.File.Exists(icon_path.Text) || icon_path.Text == string.Empty)
                            {
                                bool replay = false;
                                do
                                {
                                    try
                                    {
                                        IWshShortcut shortcut = new IWshShell_Class().CreateShortcut(s_path.Text + "\\" + s_name.Text);
                                        shortcut.TargetPath = o_path.Text;
                                        if (icon_path.Text != string.Empty)
                                            shortcut.IconLocation = icon_path.Text;
                                        shortcut.Description = desc.Text;
                                        shortcut.Save();
                                        Messages.ShowMessage("Creating shortcut done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                                        Close();
                                    }
                                    catch (Exception)
                                    {
                                        switch (Messages.ShowMessage("We can not create shortcut.", new ButtonContent("Retry creating shortcut", ModrenMessageBitmapImage.Retry, 1), new ButtonContent("Skip creating shortcut for this item.", ModrenMessageBitmapImage.Skip, 2), new ButtonContent("Cancel operation", ModrenMessageBitmapImage.Cancel, 3)))
                                        {
                                            case MyMessageBoxResult.Button1:
                                                replay = true;
                                                break;
                                            case MyMessageBoxResult.Button2:
                                            case MyMessageBoxResult.Button3:
                                                replay = false;
                                                break;
                                        }
                                    }
                                } while (replay);
                            }
                            else
                                Messages.ShowMessage("We can not find icon file '" + System.IO.Path.Combine(icon_path.Text) + "'.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
                        }
                        else
                            Messages.ShowMessage("We find an element in output directory has same name of shortcut.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
                    }
                    else
                    {
                        if (Messages.ShowMessage("Output path is not exist, do you want to create it?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.Yes)
                            try
                            {
                                Directory.CreateDirectory(s_path.Text);
                                CreateShortcut_Click(null, null);
                            }
                            catch (Exception ex)
                            { Messages.ShowMessage("Sorry, we can not create directory.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                    }
                }
                else
                    Messages.ShowMessage("File or directory is not exist.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
            }
            else
                Messages.ShowMessage("Please fill all nesessary fields.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Nesessary fields are:\nOrginal path.\nShortcut path.\nShortcut name.");
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
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = true;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Select Link Destination";
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                s_path.Text = sfd.SelectedItem;
        }

        private void SelectIcon_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = Functions.GetFileType("ico"), extentions = SelectFileFilter.SetExtentions(".ico") });
            sfd.CreateFolderEnable = true;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Select Link Destination";
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                icon_path.Text = sfd.SelectedItem;
        }

        #endregion


        #region Junction Section

        private void CreateJunction_Click(object sender, RoutedEventArgs e)
        {
            if (d_path.Text != string.Empty)
            {
                if (j_path.Text != string.Empty)
                {
                    if (Directory.Exists(d_path.Text))
                    {
                        if (Directory.Exists(j_path.Text) || (!Directory.Exists(j_path.Text) && create_direcory.IsChecked.Value))
                        {
                            long content_count = 50;
                            if (Directory.Exists(j_path.Text))
                            {
                                try
                                {  content_count = Directory.GetFiles(j_path.Text).Length + Directory.GetDirectories(j_path.Text).Length; }
                                catch (Exception ex)
                                {
                                    Messages.ShowMessage("You do not have permition to access '" + j_path.Text + "'.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, ex.Message);
                                    return;
                                }
                            }
                            if ((Directory.Exists(j_path.Text) && content_count == 0) || (!Directory.Exists(j_path.Text) && create_direcory.IsChecked.Value))
                            {
                                bool replay = false;
                                do
                                {
                                    try
                                    {
                                        Classes.JunctionPoint.Create(j_path.Text, d_path.Text, create_direcory.IsChecked.Value);
                                        Messages.ShowMessage("Creating junction point done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
                                        Close();
                                    }
                                    catch (Exception)
                                    {
                                        switch (Messages.ShowMessage("We can not create junction point.", new ButtonContent("Retry creating shortcut", ModrenMessageBitmapImage.Retry, 1), new ButtonContent("Skip creating shortcut for this item.", ModrenMessageBitmapImage.Skip, 2), new ButtonContent("Cancel operation", ModrenMessageBitmapImage.Cancel, 3)))
                                        {
                                            case MyMessageBoxResult.Button1:
                                                replay = true;
                                                break;
                                            case MyMessageBoxResult.Button2:
                                            case MyMessageBoxResult.Button3:
                                                replay = false;
                                                break;
                                        }
                                    }
                                } while (replay);
                            }
                            else
                                Messages.ShowMessage("Target directory is not empty.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                        }
                        else
                            Messages.ShowMessage("Directory is not exists and create parameter is false.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                    }
                    else
                        Messages.ShowMessage("Sorry we can not find source directory.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                }
                else
                    Messages.ShowMessage("Please fill all field first.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            }
            else
                Messages.ShowMessage("Please fill all field first.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");




            //            if (!System.IO.File.Exists(s_path.Text + "\\" + s_name.Text) && !Directory.Exists(s_path.Text + "\\" + s_name.Text))
            //            {
            //                if (icon_path.Text != string.Empty && System.IO.File.Exists(icon_path.Text) || icon_path.Text == string.Empty)
            //                {
            //                    bool replay = false;
            //                    do
            //                    {
            //                        try
            //                        {
            //                            IWshShortcut shortcut = new IWshShell_Class().CreateShortcut(s_path.Text + "\\" + s_name.Text);
            //                            shortcut.TargetPath = o_path.Text;
            //                            if (icon_path.Text != string.Empty)
            //                                shortcut.IconLocation = icon_path.Text;
            //                            shortcut.Description = desc.Text;
            //                            shortcut.Save();
            //                            Messages.ShowMessage("Creating shortcut done.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, false, "");
            //                            Close();
            //                        }
            //                        catch (Exception)
            //                        {
            //                            switch (Messages.ShowMessage("We can not create shortcut.", new ButtonContent("Retry creating shortcut", ModrenMessageBitmapImage.Retry, 1), new ButtonContent("Skip creating shortcut for this item.", ModrenMessageBitmapImage.Skip, 2), new ButtonContent("Cancel operation", ModrenMessageBitmapImage.Cancel, 3)))
            //                            {
            //                                case MyMessageBoxResult.Button1:
            //                                    replay = true;
            //                                    break;
            //                                case MyMessageBoxResult.Button2:
            //                                case MyMessageBoxResult.Button3:
            //                                    replay = false;
            //                                    break;
            //                            }
            //                        }
            //                    } while (replay);
            //                }
            //                else
            //                    Messages.ShowMessage("We can not find icon file '" + System.IO.Path.Combine(icon_path.Text) + "'.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
            //            }
            //            else
            //                Messages.ShowMessage("We find an element in output directory has same name of shortcut.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
            //        }
            //        else
            //        {
            //            if (Messages.ShowMessage("Output path is not exist, do you want to create it?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, false, "") == MyMessageBoxResult.Yes)
            //                try
            //                {
            //                    Directory.CreateDirectory(s_path.Text);
            //                    CreateShortcut_Click(null, null);
            //                }
            //                catch (Exception ex)
            //                { Messages.ShowMessage("Sorry, we can not create directory.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
            //        }
            //    }
            //    else
            //        Messages.ShowMessage("File or directory is not exist.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
            //}
            //else
            //    Messages.ShowMessage("Please fill all nesessary fields.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Nesessary fields are:\nOrginal path.\nShortcut path.\nShortcut name.");




            //bool replay = true;
            //do
            //{
            //    if (Directory.Exists(d_path.Text))
            //    {
            //        bool not_empty = false;
            //        try
            //        {
            //            if (Directory.Exists(j_path.Text))
            //                if (Directory.GetFiles(j_path.Text).Length == 0)
            //                    if (Directory.GetDirectories(j_path.Text).Length == 0)
            //                    {
            //                        Classes.JunctionPoint.Create(j_path.Text, d_path.Text, false);
            //                        replay = false;
            //                    }
            //                    else
            //                        not_empty = true;
            //                else
            //                    not_empty = true;
            //            else
            //            {
            //                Classes.JunctionPoint.Create(j_path.Text, d_path.Text, create_direcory.IsChecked.Value);
            //                replay = false;
            //            }
            //        }
            //        catch (Exception ex/* extra info*/)
            //        {
            //            OperationTree unsucsess = new OperationTree(null, false, "", OperationType.Attributes, null, false, "We can not create reparse point.");
            //            if (not_empty)
            //                unsucsess.children.Add(new OperationTree(new DirectoryInfo(d_path.Text), false, "", OperationType.CreateShortcut, unsucsess, false, "Target directory is not empty."));
            //            else if (Directory.Exists(d_path.Text))
            //                unsucsess.children.Add(new OperationTree(new DirectoryInfo(d_path.Text), false, "", OperationType.CreateShortcut, unsucsess, false, "We can not create reparse point."));
            //            else
            //                unsucsess.children.Add(new OperationTree(new DirectoryInfo(d_path.Text), false, "", OperationType.CreateShortcut, unsucsess, false, "We can not find original directory."));
            //            new File_s_Transferring_Error(unsucsess, "Error while creating reparse point.").ShowThisDialog();
            //            if (unsucsess.children[0].user_option == OperationOption.Skip)
            //                replay = false;
            //        }
            //    }
            //}
            //while (replay);
            //Close();
        }

        private void SelectJunctionTarget_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = true;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Select Link Destination";
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                j_path.Text = sfd.SelectedItem;
        }

        private void SelectSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = true;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Select Link Destination";
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                d_path.Text = sfd.SelectedItem;
        }

        #endregion


        #region Apperance

        private void Label1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ThicknessAnimation tar = new ThicknessAnimation(new Thickness(0, 30, 0, 0), new Duration(new TimeSpan(1000000)));
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
            ThicknessAnimation ta = new ThicknessAnimation(new Thickness(label1.ActualWidth, 30, 0, 0), new Duration(new TimeSpan(1000000)));
            ThicknessAnimation tag = new ThicknessAnimation(new Thickness(-500, 0, 0, 0), new Duration(new TimeSpan(1500000)));
            mother.BeginAnimation(MarginProperty, tag);
            rec.BeginAnimation(MarginProperty, ta);
            rec.Width = label2.ActualWidth;
            label1.Background = null;
            label1.Foreground = label2.Background = Brushes.White;
            label2.Foreground = Brushes.Black;
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rec.Width = label1.ActualWidth;
        }
    }

    //OperationTree unsucsess = new OperationTree(null, false, "", OperationType.Attributes, null, false, "We can not create shortcut.");
    //if (System.IO.File.Exists(o_path.Text))
    //    unsucsess.children.Add(new OperationTree(new FileInfo(o_path.Text), false, "", OperationType.CreateShortcut, unsucsess, "We can not create shortcut."));
    //else if (Directory.Exists(o_path.Text))
    //    unsucsess.children.Add(new OperationTree(new DirectoryInfo(o_path.Text), false, "", OperationType.CreateShortcut, unsucsess, false, "We can not create shortcut."));
    //else
    //    unsucsess.children.Add(new OperationTree(new FileInfo(o_path.Text), false, "", OperationType.CreateShortcut, unsucsess, "We can not find original file."));
    //new File_s_Transferring_Error(unsucsess, "Error while creating shortcut.").ShowThisDialog();
    //if (unsucsess.children[0].user_option == OperationOption.Skip)
    //    replay = false;
    //}
}
