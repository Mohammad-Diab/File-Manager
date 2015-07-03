using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for SubtitleSetting.xaml
    /// </summary>
    public partial class SubtitleSetting : Window
    {
        List<FontItem> font_data = new List<FontItem>(), font_data_backup = new List<FontItem>();
        List<ColorItem> color_data = new List<ColorItem>(), bg_color_data = new List<ColorItem>(), color_data_backup = new List<ColorItem>();
        bool accept = false, starting_window = true;
        FontWithColor old;
        FontSettingPages CurrentPage = FontSettingPages.Font;


        public SubtitleSetting()
        {
            InitializeComponent();
        }

        internal FontWithColor ShowWindow(FontWithColor old_font)
        {
            old = old_font;
            ShowDialog();
            if (accept)
            {
                double left, bottom;
                if (!double.TryParse(bottom_textbox.Text, out bottom) || bottom > 250 || bottom < 10)
                    bottom = old.position.Y;
                if (!double.TryParse(left_textbox.Text, out left) || left > 200 || left < 10)
                    left = old.position.X;
                return new FontWithColor((fonts_list.SelectedItem as FontItem).Font, (colors_list.SelectedItem as ColorItem).Color, (bg_colors_list.SelectedItem as ColorItem).Color, font_size_slider.Value, Italic.IsChecked.Value, Bold.IsChecked.Value, Encoding.GetEncoding(Convert.ToInt32(((ComboBoxItem)encode_comboBox.SelectedItem).Tag)), (sync_multiple.SelectedIndex * 59 + 1) * sync_slider.Value * 1000, left, bottom, text_opacity_slider.Value, background_opacity_slider.Value);
            }
            else
                return old;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            starting_window = false;
            rec.Width = label1.ActualWidth;
            bg_colors_list.DataContext = bg_color_data;
            colors_list.DataContext = color_data;
            fonts_list.DataContext = font_data;
            var fonts_array = System.Drawing.FontFamily.Families;
            int selected_font = -1;

            BackgroundWorker get_fonts = new BackgroundWorker();
            get_fonts.WorkerReportsProgress = true;

            get_fonts.DoWork += (object sender2, DoWorkEventArgs e2) =>
            {
                for (int i = 0; i < fonts_array.Length; i++)
                {
                    get_fonts.ReportProgress(i);
                    bool _continue = true;
                    if (_continue && old != null)
                        if (old.font_name.Name.ToLower().Equals(fonts_array[i].Name.ToLower()))
                        {
                            selected_font = i;
                            _continue = false;
                        }
                }
            };

            get_fonts.ProgressChanged += (object sender2, ProgressChangedEventArgs g) =>
            {
                var item = new FontItem(new FontFamily(fonts_array[g.ProgressPercentage].Name), fonts_array[g.ProgressPercentage].Name);
                font_data.Add(item);
                font_data_backup.Add(item);
            };

            get_fonts.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
            {
                fonts_list.Items.Refresh();
                if (old != null)
                {
                    fonts_list.SelectedIndex = selected_font;
                    if (fonts_list.SelectedIndex > -1)
                        fonts_list.ScrollIntoView(fonts_list.Items[fonts_list.SelectedIndex]);
                }
            };
            get_fonts.RunWorkerAsync();

            BackgroundWorker get_colors = new BackgroundWorker();
            get_colors.WorkerReportsProgress = true;

            get_colors.DoWork += (object sender2, DoWorkEventArgs e2) =>
            {
                for (int i = 0; i < FontWithColor.BrushesNames.Length; i++)
                    get_colors.ReportProgress(i);
            };

            get_colors.ProgressChanged += (object sender2, ProgressChangedEventArgs g) =>
            {
                var item = new ColorItem((SolidColorBrush)FontWithColor.WMBrushes[g.ProgressPercentage], FontWithColor.BrushesNames[g.ProgressPercentage]);
                color_data.Add(item);
                color_data_backup.Add(item);
                bg_color_data.Add(item);
            };

            get_colors.RunWorkerCompleted += (object sender2, RunWorkerCompletedEventArgs e2) =>
            {
                colors_list.Items.Refresh();
                bg_colors_list.Items.Refresh();
                colors_list.SelectedIndex = FontWithColor.GetColorIndex(old.font_color);
                if (colors_list.SelectedIndex > -1)
                    colors_list.ScrollIntoView(colors_list.Items[colors_list.SelectedIndex]);
                bg_colors_list.SelectedIndex = FontWithColor.GetColorIndex(old.background_color);
                if (bg_colors_list.SelectedIndex > -1)
                    bg_colors_list.ScrollIntoView(bg_colors_list.Items[bg_colors_list.SelectedIndex]);
            };
            get_colors.RunWorkerAsync();

            bool continue_search = true;
            foreach (var i in Encoding.GetEncodings())
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = i.DisplayName;
                item.Tag = i.CodePage;
                encode_comboBox.Items.Add(item);
                if (continue_search && i.CodePage == old.text_encode.CodePage)
                {
                    encode_comboBox.SelectedItem = item;
                    continue_search = false;
                }
            }
            if (old != null)
            {
                font_size_slider.Value = old.font_size;
                Bold.IsChecked = old.bold;
                Italic.IsChecked = old.italic;
                if (old.async_millisecond <= 60000 && old.async_millisecond >= -60000)
                    sync_slider.Value = old.async_millisecond / 1000;
                else
                {
                    sync_slider.Value = old.async_millisecond / 60000;
                    sync_multiple.SelectedIndex = 1;
                }
                if (encode_comboBox.SelectedIndex == -1)
                    encode_comboBox.SelectedIndex = 0;
                left_textbox.Text = old.position.X.ToString();
                bottom_textbox.Text = old.position.Y.ToString();
                background_opacity_slider.Value = old.background_opacity;
                text_opacity_slider.Value = old.text_opacity >= 0.5 ? old.text_opacity : 100;
            }
        }


        #region Font

        private void FontsNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fonts_list.SelectedIndex > -1)
                preview_text.FontFamily = new FontFamily((fonts_list.SelectedItem as FontItem).FontName);
        }

        private void FontColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (colors_list.SelectedItem != null)
            {
                preview_text.Foreground = (colors_list.SelectedItem as ColorItem).Color;
                if (AreSimilar((colors_list.SelectedItem as ColorItem).Color.Color, Colors.White, 15))
                    preview_text.Background = Brushes.Black;
                if (AreSimilar((colors_list.SelectedItem as ColorItem).Color.Color, Colors.Black, 20))
                    preview_text.Background = Brushes.White;
            }
        }

        private bool AreSimilar(Color c1, Color c2, int tolerance)
        {
            return Math.Abs(c1.R - c2.R) + Math.Abs(c1.G - c2.G) + Math.Abs(c1.B - c2.B) <= 3 * tolerance;
        }

        private void FontSizes_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (preview_text != null)
                preview_text.FontSize = e.NewValue;
        }

        private void Bold_Checked(object sender, RoutedEventArgs e)
        {
            preview_text.FontWeight = FontWeights.Bold;
        }

        private void Bold_Unchecked(object sender, RoutedEventArgs e)
        {
            preview_text.FontWeight = FontWeights.Normal;
        }

        private void Italic_Checked(object sender, RoutedEventArgs e)
        {
            preview_text.FontStyle = FontStyles.Italic;
        }

        private void Italic_Unchecked(object sender, RoutedEventArgs e)
        {
            preview_text.FontStyle = FontStyles.Normal;
        }

        private void FontFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            font_data.Clear();
            foreach (var i in font_data_backup)
                if (i.FontName.ToLower().Contains(font_filter.Text.ToLower()))
                    font_data.Add(i);
            fonts_list.Items.Refresh();
        }

        private void FColorFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            color_data.Clear();
            foreach (var i in color_data_backup)
                if (i.ColorName.ToLower().Contains(fcolor_filter.Text.ToLower()))
                    color_data.Add(i);
            colors_list.Items.Refresh();
        }

        #endregion Font


        #region Options

        private void SyncSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!starting_window)
                syncvalue_textblock.Text = Math.Round(sync_slider.Value * (1 + 59 * sync_multiple.SelectedIndex), 2) + " seconds";
        }

        private void SyncMultiple_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!starting_window)
                syncvalue_textblock.Text = Math.Round(sync_slider.Value * (1 + 59 * sync_multiple.SelectedIndex), 2) + " second" + (Math.Round(sync_slider.Value * (1 + 59 * sync_multiple.SelectedIndex), 2) > 1 ? "s" : "");
        }

        private void SyncRest_Click(object sender, RoutedEventArgs e)
        {
            sync_slider.Value = 0;
        }

        private void BColorFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            bg_color_data.Clear();
            foreach (var i in color_data_backup)
                if (i.ColorName.ToLower().Contains(bcolor_filter.Text.ToLower()))
                    bg_color_data.Add(i);
            bg_colors_list.Items.Refresh();
        }

        private void BackgroundOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!starting_window)
                background_opacity_text.Text = Math.Round(background_opacity_slider.Value, 0) + " %";
        }

        private void TextOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!starting_window)
                text_opacity_text.Text = Math.Round(text_opacity_slider.Value, 0) + " %";
        }

        #endregion Options


        private void Save_Setting(object sender, RoutedEventArgs e)
        {
            //غير الصفحة الى مكان الخطأ (move taps)
            if (fonts_list.SelectedItem != null)
            {
                if (bg_colors_list.SelectedItem != null)
                {
                    if (colors_list.SelectedItem != null)
                    {
                        accept = true;
                        Close();
                    }
                    else
                        Messages.ShowMessage("Please select font color from colors list.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
                }
                else
                    Messages.ShowMessage("Please select text background color from colors list.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
            }
            else
                Messages.ShowMessage("Please select font from fonts list.", ModrenMessageButton.OK, ModrenMessageImage.Exclamation, ModrenMessageSound.Exclamation, false, "");
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }


        #region Apperance

        private void Label1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ThicknessAnimation tar = new ThicknessAnimation(new Thickness(0, 32, 0, 0), new Duration(new TimeSpan(1000000)));
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
            ThicknessAnimation ta = new ThicknessAnimation(new Thickness(label1.ActualWidth, 32, 0, 0), new Duration(new TimeSpan(1000000)));
            ThicknessAnimation tag = new ThicknessAnimation(new Thickness(-500, 0, 0, 0), new Duration(new TimeSpan(1500000)));
            mother.BeginAnimation(Grid.MarginProperty, tag);
            rec.BeginAnimation(Rectangle.MarginProperty, ta);
            rec.Width = label2.ActualWidth;
            label1.Background = null;
            label1.Foreground = Brushes.White;
            label2.Background = Brushes.White;
            label2.Foreground = Brushes.Black;
        }

        private void AboutTool_Click(object sender, RoutedEventArgs e)
        {
            //subtitle with font and color and size
            //bold and italic options
            //1.1
            //synchronize subtitle
            //Text position
            //subtitle encode
            //v 2.0
            //redesign dialog and improve speed
            //background color and opacity
            //fonts and color filter
            //you can resize dialog now
        }

        private void SetBackgroundWhite_Click(object sender, RoutedEventArgs e)
        {
            preview_text.Background = Brushes.White;
        }

        private void SetBackgroundBlack_Click(object sender, RoutedEventArgs e)
        {
            preview_text.Background = Brushes.Black;
        }

        void TabChange_MouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            ThicknessAnimation ta = new ThicknessAnimation(new Thickness((-1 * tab_panal.Children.IndexOf((Label)sender)) * ActualWidth, 0, 0, 0), new Duration(new TimeSpan(2000000)));
            double rec_wid = 0;
            foreach (Label i in tab_panal.Children)
                if (i != sender)
                    rec_wid += i.ActualWidth;
                else
                    break;
            ThicknessAnimation ta2 = new ThicknessAnimation(new Thickness(rec_wid, 32, 0, 0), new Duration(new TimeSpan(2000000)));
            rec.Width = ((Label)sender).ActualWidth;
            rec.BeginAnimation(MarginProperty, ta2);
            mother.BeginAnimation(MarginProperty, ta);
            label1.Background = Brushes.Black; label1.Foreground = Brushes.White;
            label2.Background = Brushes.Black; label2.Foreground = Brushes.White;
            ((Label)sender).Background = Brushes.White;
            ((Label)sender).Foreground = Brushes.Black;
            CurrentPage = (FontSettingPages)tab_panal.Children.IndexOf((Label)sender) + 1;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!starting_window)
            {
                double t = (-1 * ((int)CurrentPage - 1)) * e.NewSize.Width;
                ThicknessAnimation ta = new ThicknessAnimation(new Thickness(t, 0, 0, 0), new Duration(new TimeSpan(0)));
                mother.BeginAnimation(MarginProperty, ta);
                mother.Margin = new Thickness(t, 0, 0, 0);
                for (int i = 0; i < mother.Children.Count; i++)
                {
                    ((Grid)mother.Children[i]).Width = ActualWidth - 5;
                    ((Grid)mother.Children[i]).Margin = new Thickness(i * e.NewSize.Width, 0, 0, 0);
                }
            }
        }


        private void FilterMask_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender == font_filter)
                font_filter_mask.Visibility = Visibility.Collapsed;
            else if (sender == fcolor_filter)
                fcolor_filter_mask.Visibility = Visibility.Collapsed;
            else if (sender == bcolor_filter)
                bcolor_filter_mask.Visibility = Visibility.Collapsed;
        }

        private void FilterMask_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender == font_filter && font_filter.Text == string.Empty)
                font_filter_mask.Visibility = Visibility.Visible;
            else if (sender == fcolor_filter && fcolor_filter.Text == string.Empty)
                fcolor_filter_mask.Visibility = Visibility.Visible;
            else if (sender == bcolor_filter && bcolor_filter.Text == string.Empty)
                bcolor_filter_mask.Visibility = Visibility.Visible;
        }

        #endregion

    }

    class FontWithColor
    {
        public System.Drawing.FontFamily font_name;
        public double font_size;
        public SolidColorBrush font_color;
        public bool italic;
        public bool bold;
        public Point position;
        public double async_millisecond;
        public Encoding text_encode;
        public double text_opacity;
        public double background_opacity;
        public SolidColorBrush background_color;

        public static SolidColorBrush[] WMBrushes = { Brushes.AliceBlue, Brushes.AntiqueWhite, Brushes.Aqua, Brushes.Aquamarine, Brushes.Azure, Brushes.Beige, Brushes.Bisque, Brushes.Black, Brushes.BlanchedAlmond, Brushes.Blue, Brushes.BlueViolet, Brushes.Brown, Brushes.BurlyWood, Brushes.CadetBlue, Brushes.Chartreuse, Brushes.Chocolate, Brushes.Coral,
            Brushes.CornflowerBlue, Brushes.Cornsilk, Brushes.Crimson, Brushes.Cyan, Brushes.DarkBlue, Brushes.DarkCyan, Brushes.DarkGoldenrod, Brushes.DarkGray, Brushes.DarkGreen, Brushes.DarkKhaki, Brushes.DarkMagenta, Brushes.DarkOliveGreen, Brushes.DarkOrange, Brushes.DarkOrchid, Brushes.DarkRed, Brushes.DarkSalmon, Brushes.DarkSeaGreen,
            Brushes.DarkSlateBlue, Brushes.DarkSlateGray, Brushes.DarkTurquoise, Brushes.DarkViolet, Brushes.DeepPink, Brushes.DeepSkyBlue, Brushes.DimGray, Brushes.DodgerBlue, Brushes.Firebrick, Brushes.FloralWhite, Brushes.ForestGreen, Brushes.Fuchsia, Brushes.Gainsboro, Brushes.GhostWhite, Brushes.Gold, Brushes.Goldenrod, Brushes.Gray,
            Brushes.Green, Brushes.GreenYellow, Brushes.Honeydew, Brushes.HotPink, Brushes.IndianRed, Brushes.Indigo, Brushes.Ivory, Brushes.Khaki, Brushes.Lavender, Brushes.LavenderBlush, Brushes.LawnGreen, Brushes.LemonChiffon, Brushes.LightBlue, Brushes.LightCoral, Brushes.LightCyan, Brushes.LightGoldenrodYellow, Brushes.LightGray,
            Brushes.LightGreen, Brushes.LightPink, Brushes.LightSalmon, Brushes.LightSeaGreen, Brushes.LightSkyBlue, Brushes.LightSlateGray, Brushes.LightSteelBlue, Brushes.LightYellow, Brushes.Lime, Brushes.LimeGreen, Brushes.Linen, Brushes.Magenta, Brushes.Maroon, Brushes.MediumAquamarine, Brushes.MediumBlue, Brushes.MediumOrchid,
            Brushes.MediumPurple, Brushes.MediumSeaGreen, Brushes.MediumSlateBlue, Brushes.MediumSpringGreen, Brushes.MediumTurquoise, Brushes.MediumVioletRed, Brushes.MidnightBlue, Brushes.MintCream, Brushes.MistyRose, Brushes.Moccasin, Brushes.NavajoWhite, Brushes.Navy, Brushes.OldLace, Brushes.Olive, Brushes.OliveDrab, Brushes.Orange,
            Brushes.OrangeRed, Brushes.Orchid, Brushes.PaleGoldenrod, Brushes.PaleGreen, Brushes.PaleTurquoise, Brushes.PaleVioletRed, Brushes.PapayaWhip, Brushes.PeachPuff, Brushes.Peru, Brushes.Pink, Brushes.Plum, Brushes.PowderBlue, Brushes.Purple, Brushes.Red, Brushes.RosyBrown, Brushes.RoyalBlue, Brushes.SaddleBrown, Brushes.Salmon,
            Brushes.SandyBrown, Brushes.SeaGreen, Brushes.SeaShell, Brushes.Sienna, Brushes.Silver, Brushes.SkyBlue, Brushes.SlateBlue, Brushes.SlateGray, Brushes.Snow, Brushes.SpringGreen, Brushes.SteelBlue, Brushes.Tan, Brushes.Teal, Brushes.Thistle, Brushes.Tomato,Brushes.Transparent, Brushes.Turquoise, Brushes.Violet, Brushes.Wheat, Brushes.White,
            Brushes.WhiteSmoke, Brushes.Yellow, Brushes.YellowGreen };

        public static string[] BrushesNames = { "Alice Blue", "Antique White", "Aqua", "Aquamarine", "Azure", "Beige", "Bisque", "Black", "Blanched Almond", "Blue", "Blue Violet", "Brown", "Burly Wood", "Cadet Blue", "Chartreuse", "Chocolate", "Coral", "Cornflower Blue", "Cornsilk", "Crimson", "Cyan", "Dark Blue", "Dark Cyan", "Dark Goldenrod",
            "Dark Gray", "Dark Green", "Dark Khaki", "Dark Magenta", "Dark Olive Green", "Dark Orange", "Dark Orchid", "Dark Red", "Dark Salmon", "Dark Sea Green", "Dark Slate Blue", "Dark Slate Gray", "Dark Turquoise", "Dark Violet", "Deep Pink", "Deep Sky Blue", "Dim Gray", "Dodger Blue", "Firebrick", "Floral White", "Forest Green", "Fuchsia",
            "Gainsboro", "Ghost White", "Gold", "Goldenrod", "Gray", "Green", "Green Yellow", "Honeydew", "Hot Pink", "Indian Red", "Indigo", "Ivory", "Khaki", "Lavender", "Lavender Blush", "Lawn Green", "Lemon Chiffon", "Light Blue", "Light Coral", "Light Cyan", "Light Goldenrod Yellow", "Light Gray", "Light Green", "Light Pink",
            "Light Salmon", "Light Sea Green", "Light Sky Blue", "Light Slate Gray", "Light Steel Blue", "Light Yellow", "Lime", "Lime Green", "Linen", "Magenta", "Maroon", "Medium Aquamarine", "Medium Blue", "Medium Orchid", "Medium Purple", "Medium Sea Green", "Medium Slate Blue", "Medium Spring Green", "Medium Turquoise", "Medium Violet Red",
            "Midnight Blue", "Mint Cream", "Misty Rose", "Moccasin", "Navajo White", "Navy", "Old Lace", "Olive", "Olive Drab", "Orange", "Orange Red", "Orchid", "Pale Goldenrod", "Pale Green", "Pale Turquoise", "Pale Violet Red", "Papaya Whip", "Peach Puff", "Peru", "Pink", "Plum", "Powder Blue", "Purple", "Red", "Rosy Brown", "Royal Blue",
            "Saddle Brown", "Salmon", "Sandy Brown", "Sea Green", "Sea Shell", "Sienna", "Silver", "Sky Blue", "Slate Blue", "Slate Gray", "Snow", "Spring Green", "Steel Blue", "Tan", "Teal", "Thistle", "Tomato", "Transparent", "Turquoise", "Violet", "Wheat", "White", "White Smoke", "Yellow", "Yellow Green" };

        public FontWithColor(FontFamily font, SolidColorBrush color, SolidColorBrush background_color, double font_size, bool italic, bool bold, Encoding encode, double async, double left, double bottom, double text_opacity, double background_opacity)
        {
            font_name = new System.Drawing.FontFamily(font.Source);
            this.font_size = font_size;
            this.italic = italic;
            this.bold = bold;
            async_millisecond = async;
            text_encode = encode;
            position = new Point(left, bottom);
            this.text_opacity = text_opacity;
            this.background_opacity = background_opacity;
            font_color = new SolidColorBrush(Color.FromArgb(Convert.ToByte(255 * text_opacity / 100), color.Color.R, color.Color.G, color.Color.B));
            this.background_color = new SolidColorBrush(Color.FromArgb(Convert.ToByte(255 * background_opacity / 100), background_color.Color.R, background_color.Color.G, background_color.Color.B));
        }

        public FontWithColor(FontFamily font, SolidColorBrush color, double font_size, bool italic, bool bold, Encoding encode, double async, double left, double bottom, SolidColorBrush background_color)
        {
            font_name = new System.Drawing.FontFamily(font.Source);
            this.font_size = font_size;
            font_color = WMBrushes[GetColorIndex(color)];
            this.italic = italic;
            this.bold = bold;
            async_millisecond = async;
            text_encode = encode;
            position = new Point(left, bottom);
            this.background_color = WMBrushes[GetColorIndex(background_color)];
            text_opacity = color.Color.A * 100.0 / 255;
            background_opacity = background_color.Color.A * 100.0 / 255;
        }

        public static int GetColorIndex(SolidColorBrush color)
        {
            if (color.Color.A == 0 && color.Color.B == 255 && color.Color.G == 255 && color.Color.R == 255)
                return 133; //Brushes.Transparent
            if (color != null)
            {
                for (int i = 0; i < WMBrushes.Length; i++)
                {
                    if (i == 133)
                        continue;
                    if (WMBrushes[i].Color.B == color.Color.B && WMBrushes[i].Color.G == color.Color.G && WMBrushes[i].Color.R == color.Color.R)
                        return i;
                }
            }
            return 7;       //Brushes.Black
        }
    }

    public class ColorItem
    {
        string color_name;
        public string ColorName
        {
            get { return color_name; }
            set { color_name = value; }
        }

        SolidColorBrush color;
        public SolidColorBrush Color
        {
            get { return color; }
            set { color = value; }
        }

        public ColorItem(SolidColorBrush color, string color_name)
        {
            Color = color;
            ColorName = color_name;
        }
    }

    public class FontItem
    {
        string font_name;
        public string FontName
        {
            get { return font_name; }
            set { font_name = value; }
        }

        string display_name;
        public string DisplayName
        {
            get { return display_name; }
            set { display_name = value; }
        }

        FontFamily font;
        public FontFamily Font
        {
            get { return font; }
            set { font = value; }
        }

        public FontItem(FontFamily font, string font_name)
        {
            Font = font;
            FontName = font_name;
            DisplayName = font_name + " \t(أبجد هوز حطي كلمن سعفص قرشت ثخذ ضظغ 0123456789)";
        }
    }

    enum FontSettingPages
    {
        Font = 1,
        Setting = 2
    }
}