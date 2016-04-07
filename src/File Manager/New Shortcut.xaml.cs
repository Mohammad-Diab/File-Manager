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
        private New_Shortcut()
        {
            InitializeComponent();
        }

        #region Shortcut Section

        private void CreateShortcut_Click(object sender, RoutedEventArgs e)
        {
            string original = o_path.Text.Trim(), folder = s_path.Text.Trim(), name = s_name.Text.Trim(), icon = icon_path.Text.Trim();
            if (original.Length > 0 && folder.Length == 0)
                try
                {
                    folder = System.IO.Path.GetDirectoryName(original.TrimEnd('\\')) ?? "";
                    s_path.Text = folder;
                }
                catch (Exception) { }
            if (original.Length > 0 && name.Length == 0)
                name = DefaultName(original);
            if (name.Length > 0 && !name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                name += ".lnk";
            s_name.Text = name;
            if (original != string.Empty && name != string.Empty && folder != string.Empty)
            {
                if (name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
                {
                    Messages.ShowMessage("The shortcut name has characters a file name can not have.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "\\ / : * ? \" < > |");
                    return;
                }
                if (Directory.Exists(original) || System.IO.File.Exists(original))
                {
                    if (Directory.Exists(folder))
                    {
                        string shortcut_path = System.IO.Path.Combine(folder, name);
                        if (!System.IO.File.Exists(shortcut_path) && !Directory.Exists(shortcut_path))
                        {
                            string icon_file = icon;
                            int comma = icon.LastIndexOf(',');
                            int icon_index = 0;
                            if (comma > 0 && int.TryParse(icon.Substring(comma + 1), out icon_index))
                                icon_file = icon.Substring(0, comma);
                            if (icon_file != string.Empty && System.IO.File.Exists(icon_file) || icon_file == string.Empty)
                            {
                                bool replay = false;
                                do
                                {
                                    replay = false;
                                    try
                                    {
                                        IWshShortcut shortcut = new IWshShell_Class().CreateShortcut(shortcut_path);
                                        shortcut.TargetPath = original;
                                        if (Directory.Exists(original))
                                            shortcut.WorkingDirectory = original;
                                        else
                                            shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(original);
                                        if (icon_file != string.Empty)
                                            shortcut.IconLocation = icon_file + "," + icon_index;
                                        shortcut.Description = desc.Text;
                                        shortcut.Save();
                                        Messages.ShowMessage("The shortcut has been created.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, shortcut_path);
                                        Close();
                                    }
                                    catch (Exception ex)
                                    {
                                        switch (Messages.ShowMessage("We can not create the shortcut.", ex.Message, new ButtonContent("Retry creating the shortcut", ModrenMessageBitmapImage.Retry, 1), new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 2)))
                                        {
                                            case MyMessageBoxResult.Button1:
                                                replay = true;
                                                break;
                                            default:
                                                replay = false;
                                                break;
                                        }
                                    }
                                } while (replay);
                            }
                            else
                                Messages.ShowMessage("We can not find icon file '" + icon_file + "'.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, false, "");
                        }
                        else
                            Messages.ShowMessage("A file or folder with the name of the shortcut already exists in this folder.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, shortcut_path);
                    }
                    else
                    {
                        if (Messages.ShowMessage("The shortcut folder does not exist, do you want to create it?", ModrenMessageButton.YesNo, ModrenMessageImage.Question, ModrenMessageSound.Question, true, folder) == MyMessageBoxResult.Yes)
                            try
                            {
                                Directory.CreateDirectory(folder);
                                CreateShortcut_Click(null, null);
                            }
                            catch (Exception ex)
                            { Messages.ShowMessage("Sorry, we can not create the folder.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, ex.Message); }
                    }
                }
                else
                    Messages.ShowMessage("The original file or folder does not exist.", ModrenMessageButton.OK, ModrenMessageImage.Stop, ModrenMessageSound.Stop, true, original);
            }
            else
                Messages.ShowMessage("Please fill all necessary fields.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Necessary fields are:\nOriginal path.\nShortcut path.\nShortcut name.");
        }

        static string DefaultName(string original)
        {
            string trimmed = original.TrimEnd('\\');
            string name = Directory.Exists(original) ? System.IO.Path.GetFileName(trimmed) : System.IO.Path.GetFileNameWithoutExtension(trimmed);
            if (string.IsNullOrEmpty(name))
                name = trimmed.Replace(":", "").Replace("\\", "") + " drive";
            return name + ".lnk";
        }

        string auto_folder = "", auto_name = "";

        private void OriginalPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            string original = o_path.Text.Trim();
            if (original.Length == 0 || (!Directory.Exists(original) && !System.IO.File.Exists(original)))
                return;
            string folder = "";
            try { folder = System.IO.Path.GetDirectoryName(original.TrimEnd('\\')) ?? ""; }
            catch (Exception) { }
            if (s_path.Text.Trim().Length == 0 || s_path.Text == auto_folder)
                s_path.Text = auto_folder = folder;
            if (s_name.Text.Trim().Length == 0 || s_name.Text == auto_name)
                s_name.Text = auto_name = DefaultName(original);
        }

        private void SelectOriginalFile_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Select the file the shortcut opens";
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                o_path.Text = sfd.SelectedItem;
        }

        private void SelectOriginalFolder_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Select the folder the shortcut opens";
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                o_path.Text = sfd.SelectedItem;
        }

        private void SelectOrginalItem_Click(object sender, RoutedEventArgs e)
        {
            o_browse.ContextMenu.PlacementTarget = o_browse;
            o_browse.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            o_browse.ContextMenu.IsOpen = true;
        }

        private void SelectLinkDestination_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = true;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Select the folder for the shortcut";
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                s_path.Text = sfd.SelectedItem;
        }

        private void SelectIcon_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.OpenFile;
            sfd.FilterMenu.Add(new SelectFileFilter() { display_name = "Icon, program or library (*.ico, *.exe, *.dll)", extentions = SelectFileFilter.SetExtentions(".ico|.exe|.dll") });
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Select the icon of the shortcut";
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
                                    Messages.ShowMessage("You do not have permission to access '" + j_path.Text + "'.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, ex.Message);
                                    return;
                                }
                            }
                            if ((Directory.Exists(j_path.Text) && content_count == 0) || (!Directory.Exists(j_path.Text) && create_direcory.IsChecked.Value))
                            {
                                bool replay = false;
                                do
                                {
                                    replay = false;
                                    try
                                    {
                                        Classes.JunctionPoint.Create(j_path.Text, d_path.Text, create_direcory.IsChecked.Value);
                                        Messages.ShowMessage("The junction has been created.", ModrenMessageButton.OK, ModrenMessageImage.Information, ModrenMessageSound.Information, true, j_path.Text + "  ->  " + d_path.Text);
                                        Close();
                                    }
                                    catch (Exception ex)
                                    {
                                        switch (Messages.ShowMessage("We can not create the junction.", ex.Message, new ButtonContent("Retry creating the junction", ModrenMessageBitmapImage.Retry, 1), new ButtonContent("Cancel", ModrenMessageBitmapImage.Cancel, 2)))
                                        {
                                            case MyMessageBoxResult.Button1:
                                                replay = true;
                                                break;
                                            default:
                                                replay = false;
                                                break;
                                        }
                                    }
                                } while (replay);
                            }
                            else
                                Messages.ShowMessage("The junction target folder is not empty.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Choose an empty folder, or a folder that does not exist yet.");
                        }
                        else
                            Messages.ShowMessage("The junction target folder does not exist.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, "Tick 'Create the folder if it does not exist.' or choose an existing empty folder.");
                    }
                    else
                        Messages.ShowMessage("Sorry, we can not find the source folder.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, true, d_path.Text);
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
            sfd.DialogTitle = "Select where the junction is created (an empty folder)";
            if (sfd.ShowWindow() == MyMessageBoxResult.OK)
                j_path.Text = sfd.SelectedItem;
        }

        private void SelectSourceDirectory_Click(object sender, RoutedEventArgs e)
        {
            SelectFileDialog sfd = new SelectFileDialog();
            sfd.dialogMode = SelectFileDialogMode.SelectDirectory;
            sfd.CreateFolderEnable = false;
            sfd.AddExtension = false;
            sfd.DialogTitle = "Select the folder the junction points to";
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

        static internal void CreateShortcut(string path, bool show_dialog)
        {
            New_Shortcut window = new New_Shortcut();
            if (path != null && path != "")
                window.o_path.Text = path;
            if (show_dialog)
                window.ShowDialog();
            else
                window.Show();
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
