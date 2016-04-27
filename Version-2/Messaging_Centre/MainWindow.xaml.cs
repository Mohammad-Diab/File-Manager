using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Messaging_Centre
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool has_addition_info = false, is_costum_button = false;
        public bool HasAdditionInfo
        {
            set { has_addition_info = value; }
        }
        int returned_value = 0;
        MyMessageBoxResult to_return_value = MyMessageBoxResult.Cancel;
        // '0' for cancel or no
        // '1' for ok or yes
        public ModrenMessageBox()
        {
            InitializeComponent();
        }

        internal MyMessageBoxResult ShowMessage(ModrenMessageButton buttons)
        {
            is_costum_button = true;
            ShowDialog();
            //buttons_panal.DataContext = content;
            //buttons_panal.Items.Refresh();
            return Value(buttons);
        }

        internal MyMessageBoxResult ShowMessage(ModrenMessageButton buttons, out bool show_message_again)
        {
            ShowDialog();
            show_message_again = show_again.IsChecked.HasValue ? !show_again.IsChecked.Value : true;
            return Value(buttons);
        }

        private MyMessageBoxResult Value(ModrenMessageButton buttons)
        {
            if (buttons == ModrenMessageButton.OK)
                return MyMessageBoxResult.OK;
            else if (buttons == ModrenMessageButton.Costum)
                return to_return_value;
            switch (returned_value)
            {
                case 0:
                    if (buttons == ModrenMessageButton.OKCancel)
                        return MyMessageBoxResult.Cancel;
                    if (buttons == ModrenMessageButton.YesNo)
                        return MyMessageBoxResult.No;
                    break;
                case 1:
                    if (buttons == ModrenMessageButton.OKCancel)
                        return MyMessageBoxResult.OK;
                    if (buttons == ModrenMessageButton.YesNo)
                        return MyMessageBoxResult.Yes;
                    break;
            }
            return MyMessageBoxResult.Cancel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //MaxHeight = SystemParameters.PrimaryScreenHeight - 35;
            //MaxWidth = SystemParameters.PrimaryScreenWidth - 15;
            stack_panal.MaxWidth = SystemParameters.PrimaryScreenWidth - 50;
            //extra_info.MaxHeight = MaxHeight - 200;
            stack_panal.MaxHeight = SystemParameters.PrimaryScreenHeight - 75;
            Width = stack_panal.ActualWidth + (ok.Visibility == Visibility.Visible ? 75 : 25);
            Height = stack_panal.ActualHeight + (ok.Visibility == Visibility.Visible ? 75 : is_costum_button ? 50 : 25);
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
            ////////buttons_panal.Items.Refresh();
            //if (extra_info.Visibility == Visibility.Visible)
            //{
            //    if (Caution_grid.Visibility == Visibility.Visible)
            //        Width = Math.Max(title.ActualWidth, Math.Max(extra_info.ActualWidth, caution_text.ActualWidth)) + 140;
            //    else
            //        Width = Math.Max(title.ActualWidth, extra_info.ActualWidth) + 140;
            //}
            //else
            //{
            //    if (Caution_grid.Visibility == Visibility.Visible)
            //        Width = Math.Max(title.ActualWidth, caution_text.ActualWidth) + 140;
            //    else
            //        Width = title.ActualWidth + 140;
            //}
            //if (password_grid.Visibility == Visibility.Visible && ActualWidth < 500)
            //    Width = 500;
            //Height = title.ActualHeight + 100 + (extra_info.Visibility == Visibility.Visible ? 17 + extra_info.ActualHeight : 0) + (rectangle.Visibility == Visibility.Visible ? 10 : 0) + (Caution_grid.Visibility == Visibility.Visible ? caution_text.ActualHeight : 0) + (password_grid.Visibility == Visibility.Visible ? 22 : 0);
            //if (extra_info.Visibility != Visibility.Visible && Caution_grid.Visibility == Visibility.Visible)
            //    Height -= 10;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if ((password_grid.Visibility == Visibility.Visible && (show_password_bt.IsChecked.Value ? password_view.Text : password_pw.Password) != string.Empty) || password_grid.Visibility == Visibility.Collapsed)
            {
                returned_value = 1;
                Close();
            }
            else
                Messages.ShowMessage("Passwrd field is empty.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            returned_value = 0;
            Close();
        }

        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            password_view.Text = password_pw.Password;
            password_pw.Visibility = Visibility.Collapsed;
            password_view.Visibility = Visibility.Visible;
            show_password_bt.ToolTip = "Hide password";
        }

        private void show_password_bt_Unchecked(object sender, RoutedEventArgs e)
        {
            password_pw.Password = password_view.Text;
            password_pw.Visibility = Visibility.Visible;
            password_view.Visibility = Visibility.Collapsed;
            show_password_bt.ToolTip = "Show password";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            to_return_value = (MyMessageBoxResult)((Button)sender).Tag;
            Close();
        }

        internal void SetSound(ModrenMessageSound sound)
        {
            switch (sound)
            {
                case ModrenMessageSound.Exclamation:
                    sound_player.Source = new Uri(Environment.CurrentDirectory + @"\Media\dialog-warning.wav");
                    break;
                case ModrenMessageSound.Information:
                    sound_player.Source = new Uri(Environment.CurrentDirectory + @"\Media\dialog-information.wav");
                    break;
                case ModrenMessageSound.Question:
                    sound_player.Source = new Uri(Environment.CurrentDirectory + @"\Media\dialog-question.wav");
                    break;
                case ModrenMessageSound.Stop:
                    sound_player.Source = new Uri(Environment.CurrentDirectory + @"\Media\dialog-error.wav");
                    break;
            }
        }

        internal void SetImage(ModrenMessageImage icon)
        {
            switch (icon)
            {
                case ModrenMessageImage.Delete:
                    Topbar.Fill = ok.Background = Brushes.Tomato;
                    Thickness tmp = ok.Margin;
                    ok.Margin = cancel.Margin;
                    cancel.Margin = tmp;
                    ok.Foreground = ok.BorderBrush = Brushes.WhiteSmoke;
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/delete.png"));
                    break;
                case ModrenMessageImage.Exclamation:
                    ok.BorderBrush = Topbar.Fill = Brushes.Peru;
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-Warning.png"));
                    break;
                case ModrenMessageImage.Information:
                    ok.BorderBrush = Topbar.Fill = Brushes.Green;
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-information.png"));
                    break;
                case ModrenMessageImage.None:
                    image.Source = null;
                    break;
                case ModrenMessageImage.Question:
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-question.png"));
                    break;
                case ModrenMessageImage.Stop:
                    ok.BorderBrush = Topbar.Fill = Brushes.Tomato;
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-error.png"));
                    break;
                case ModrenMessageImage.Tick:
                    Topbar.Fill = Brushes.Green;
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-tick.png"));
                    break;
                case ModrenMessageImage.Cross:
                    Topbar.Fill = Brushes.Green;
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-cross.png"));
                    break;
                case ModrenMessageImage.Sad:
                    ok.BorderBrush = Topbar.Fill = Brushes.Peru;
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/dialog-sad.png"));
                    break;
            }
        }

        internal void SetButtons(ModrenMessageButton buttons)
        {
            switch (buttons)
            {
                case ModrenMessageButton.OK:
                    cancel.Visibility = Visibility.Collapsed;
                    break;
                case ModrenMessageButton.YesNo:
                    cancel.Content = "NO";
                    ok.Content = "YES";
                    break;
            }
        }
    }

    public class Messages
    {
        //support link in message (link to file directory and web image and tool)
        //hint and extra info
        //Ctrl + C to copy message
        public static MyMessageBoxResult ShowMessage(string Message, ModrenMessageButton buttons, ModrenMessageImage image, ModrenMessageSound sound, bool has_addition_info, string extra_info)
        {
            ModrenMessageBox message = new ModrenMessageBox();
            message.HasAdditionInfo = has_addition_info;
            message.title.Text = Message;
            message.SetButtons(buttons);
            message.SetImage(image);
            message.SetSound(sound);
            //if (message.title.Width > message.ActualWidth - 90)
            //    message.title.TextWrapping = TextWrapping.Wrap;
            if (has_addition_info)
                message.extra_info.Text = extra_info;
            else
                message.rectangle.Visibility = message.addition_info.Visibility = message.extra_info.Visibility = Visibility.Collapsed;
            message.Caution_grid.Visibility = message.show_again.Visibility = message.password_grid.Visibility = Visibility.Collapsed;
            return message.ShowMessage(buttons);
        }

        public static MyMessageBoxResult ShowMessage(string Message, ModrenMessageButton buttons, ModrenMessageImage image, ModrenMessageSound sound, bool has_addition_info, string extra_info, string caution)
        {
            ModrenMessageBox message = new ModrenMessageBox();
            message.HasAdditionInfo = has_addition_info;
            message.title.Text = Message;
            message.SetButtons(buttons);
            message.SetImage(image);
            message.SetSound(sound);
            //if (message.title.Width > message.ActualWidth - 90)
            //    message.title.TextWrapping = TextWrapping.Wrap;
            if (has_addition_info)
                message.extra_info.Text = extra_info;
            else
                message.addition_info.Visibility = message.extra_info.Visibility = Visibility.Collapsed;
            message.show_again.Visibility = message.password_grid.Visibility = Visibility.Collapsed;
            message.caution_text.Text = caution;
            return message.ShowMessage(buttons);
        }

        public static MyMessageBoxResult ShowMessageWithPassword(string Message, ModrenMessageButton buttons, ModrenMessageImage image, ModrenMessageSound sound, bool has_addition_info, string extra_info, string caution, out string password)
        {
            ModrenMessageBox message = new ModrenMessageBox();
            message.HasAdditionInfo = has_addition_info;
            message.title.Text = Message;
            message.SetButtons(buttons);
            message.SetImage(image);
            message.SetSound(sound);
            //if (message.title.Width > message.ActualWidth - 90)
            //    message.title.TextWrapping = TextWrapping.Wrap;
            if (has_addition_info)
                message.extra_info.Text = extra_info;
            else
                message.addition_info.Visibility = message.extra_info.Visibility = Visibility.Collapsed;
            message.show_again.Visibility = Visibility.Collapsed;
            if (caution == string.Empty)
                message.Caution_grid.Visibility = Visibility.Collapsed;
            else
                message.caution_text.Text = caution;
            var return_value = message.ShowMessage(buttons);
            password = message.show_password_bt.IsChecked.Value ? message.password_view.Text : message.password_pw.Password;
            return return_value;
        }

        public static MyMessageBoxResult ShowMessage(string Message, string addition_info, params ButtonContent[] buttons_content)
        {
            List<ButtonContent> content = new List<ButtonContent>();
            ModrenMessageBox message = new ModrenMessageBox();
            message.title.Text = Message;
            message.HasAdditionInfo = true;
            message.show_again.Visibility = message.Caution_grid.Visibility = message.password_grid.Visibility = message.extra_info.Visibility = message.ok.Visibility = message.cancel.Visibility = Visibility.Collapsed;
            message.buttons_panal.Visibility = Visibility.Visible;
            if (addition_info == null || addition_info == "")
                message.addition_info.Visibility = Visibility.Collapsed;
            else
                message.addition_info.Text = addition_info;
            message.SetImage(ModrenMessageImage.Question);
            message.SetSound(ModrenMessageSound.Question);
            //message.buttons_panal.DataContext = content;
            foreach (var i in buttons_content)
                message.buttons_panal.Items.Add(i/*MakeButton(i, message.button_template)*/);
            //message.buttons_panal.Items.Refresh();
            //message.buttons_panal.Items.Refresh();
            //message.buttons_panal.Items.Refresh();
            return message.ShowMessage(ModrenMessageButton.Costum);
        }

        public static MyMessageBoxResult ShowMessage(string Message, ModrenMessageButton buttons, ModrenMessageImage image, ModrenMessageSound sound, bool has_addition_info, string extra_info, out bool show_message_again)
        {
            ModrenMessageBox message = new ModrenMessageBox();
            message.HasAdditionInfo = has_addition_info;
            message.title.Text = Message;
            message.SetButtons(buttons);
            message.SetImage(image);
            message.SetSound(sound);
            if (has_addition_info)
                message.extra_info.Text = extra_info;
            else
                message.rectangle.Visibility = message.addition_info.Visibility = message.extra_info.Visibility = Visibility.Collapsed;
            message.Caution_grid.Visibility = message.password_grid.Visibility = Visibility.Collapsed;
            return message.ShowMessage(buttons, out show_message_again);
        }

        internal static string GetItemInfo(FileSystemInfo item)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("path: " + item.FullName);
            sb.AppendLine("type: " + Functions.GetFileType(item is DirectoryInfo ? "directory" : item.Extension));
            if (item is FileInfo && item.Exists)
                sb.AppendLine("size: " + Functions.FileSize(((FileInfo)item).Length, 2));
            sb.AppendLine("last modified date: " + item.LastWriteTime.ToString());
            return sb.ToString();
        }

    }

    public enum ModrenMessageButton
    {
        OK = 1,
        OKCancel = 2,
        YesNo = 3,
        Costum = 100
    }

    public enum ModrenMessageImage
    {
        None = 0,
        Information = 1,
        Question = 2,
        Exclamation = 3,
        Stop = 4,
        Delete = 5,
        Rename = 6,
        Password = 7,
        Tick = 8,
        Cross = 9,
        Sad = 10,
        Custum = 100
    }

    public enum ModrenMessageSound
    {
        None = 0,
        Information = 1,
        Question = 2,
        Exclamation = 3,
        Stop = 4
    }

    public enum ModrenMessageBitmapImage
    {
        Retry = 1,
        Skip = 2,
        Cancel = 3,
        Default = 100
    }

    public class ButtonContent
    {
        public ButtonContent(string text, ModrenMessageBitmapImage image, int id)
        {
            Text = text;
            switch (image)
            {
                case ModrenMessageBitmapImage.Skip:
                    Image = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/button-skip.png"));
                    break;
                case ModrenMessageBitmapImage.Retry:
                    Image = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/button-retry.png"));
                    break;
                case ModrenMessageBitmapImage.Cancel:
                    Image = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/button-cancel.png"));
                    break;
                default:
                    Image = new BitmapImage(new Uri("pack://application:,,,/File Manager;component/Images/Appearance/button-default.png"));
                    break;
            }
            ID = (MyMessageBoxResult)(id + 100);
        }

        string text;
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        BitmapImage image;
        public BitmapImage Image
        {
            get { return image; }
            set { image = value; }
        }

        MyMessageBoxResult id;
        public MyMessageBoxResult ID
        {
            get { return id; }
            set { id = value; }
        }
    };
}

