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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Subtitle_Option.xaml
    /// </summary>
    
    public partial class Subtitle_Option : Window
    {
        public Subtitle_Option()
        {
            InitializeComponent();
        }

        string[] BrushesNames = { "AliceBlue", "AntiqueWhite", "Aqua", "Aquamarine", "Azure", "Beige", "Bisque", "Black", "BlanchedAlmond", "Blue", "BlueViolet", "Brown", "BurlyWood", "CadetBlue", "Chartreuse", "Chocolate", "Coral", "CornflowerBlue", "Cornsilk", "Crimson", "Cyan", "DarkBlue", "DarkCyan", "DarkGoldenrod", "DarkGray", "DarkGreen", "DarkKhaki", "DarkMagenta", "DarkOliveGreen", "DarkOrange", "DarkOrchid", "DarkRed", "DarkSalmon", "DarkSeaGreen", "DarkSlateBlue", "DarkSlateGray", "DarkTurquoise", "DarkViolet", "DeepPink", "DeepSkyBlue", "DimGray", "DodgerBlue", "Firebrick", "FloralWhite", "ForestGreen", "Fuchsia", "Gainsboro", "GhostWhite", "Gold", "Goldenrod", "Gray", "Green", "GreenYellow", "Honeydew", "HotPink", "IndianRed", "Indigo", "Ivory", "Khaki", "Lavender", "LavenderBlush", "LawnGreen", "LemonChiffon", "LightBlue", "LightCoral", "LightCyan", "LightGoldenrodYellow", "LightGray", "LightGreen", "LightPink", "LightSalmon", "LightSeaGreen", "LightSkyBlue", "LightSlateGray", "LightSteelBlue", "LightYellow", "Lime", "LimeGreen", "Linen", "Magenta", "Maroon", "MediumAquamarine", "MediumBlue", "MediumOrchid", "MediumPurple", "MediumSeaGreen", "MediumSlateBlue", "MediumSpringGreen", "MediumTurquoise", "MediumVioletRed", "MidnightBlue", "MintCream", "MistyRose", "Moccasin", "NavajoWhite", "Navy", "OldLace", "Olive", "OliveDrab", "Orange", "OrangeRed", "Orchid", "PaleGoldenrod", "PaleGreen", "PaleTurquoise", "PaleVioletRed", "PapayaWhip", "PeachPuff", "Peru", "Pink", "Plum", "PowderBlue", "Purple", "Red", "RosyBrown", "RoyalBlue", "SaddleBrown", "Salmon", "SandyBrown", "SeaGreen", "SeaShell", "Sienna", "Silver", "SkyBlue", "SlateBlue", "SlateGray", "Snow", "SpringGreen", "SteelBlue", "Tan", "Teal", "Thistle", "Tomato", "Turquoise", "Violet", "Wheat", "White", "WhiteSmoke", "Yellow", "YellowGreen" };
        bool accept = false;
        FontWithColor old;

        internal FontWithColor ShowWindow(FontWithColor old_font)
        {
            old = old_font;
            ShowDialog();
            if (accept)
                return new FontWithColor(FontsNames.SelectedIndex, FontColors.SelectedIndex, FontSizes.Value, Italic.IsChecked.Value, Bold.IsChecked.Value);
            else
                return old;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var i in System.Drawing.FontFamily.Families)
            {
                ListBoxItem tmp = new ListBoxItem();
                tmp.Content = i.Name + "  (أبجد هوز حطي كلمن سعفص قرشت ثخذ ضظغ 0123456789)";
                tmp.FontFamily = tmp.FontFamily = new FontFamily(i.Name);
                tmp.FontSize = 14;
                FontsNames.Items.Add(tmp);
            }
            for (int i = 0; i < BrushesNames.Length; i++)
            {
                ListBoxItem tmp = new ListBoxItem();
                tmp.Foreground = FontWithColor.WMBrushes[i];
                tmp.Content = BrushesNames[i];
                FontColors.Items.Add(tmp);
            }
            if (old != null)
            {
                for (int i = 0; i < System.Drawing.FontFamily.Families.Length; i++)
                    if (old.font_name.Name.ToLower().Equals(System.Drawing.FontFamily.Families[i].Name.ToLower()))
                    {
                        FontsNames.SelectedIndex = i;
                        break;
                    }
                if (FontsNames.SelectedIndex == -1)
                    FontsNames.SelectedIndex = 0;
                FontsNames.ScrollIntoView(FontsNames.Items[FontsNames.SelectedIndex]);
                FontColors.SelectedIndex = FontWithColor.GetColorIndex(old.font_color);
                FontColors.ScrollIntoView(FontColors.Items[FontColors.SelectedIndex]);
                FontSizes.Value = old.font_size;
                TextTest.FontSize = FontSizes.Value;
                Bold.IsChecked = old.bold;
                Italic.IsChecked = old.italic;
            }
        }

        private void FontSizes_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextTest != null)
                TextTest.FontSize = e.NewValue;
        }

        private void FontsNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextTest.FontFamily = new System.Windows.Media.FontFamily(System.Drawing.FontFamily.Families[FontsNames.SelectedIndex].Name);
        }

        private void FontColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextTest.Foreground = FontWithColor.WMBrushes[FontColors.SelectedIndex];
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        private void Save_Setting(object sender, RoutedEventArgs e)
        {
            accept = true;
            Close();
        }

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
            ThicknessAnimation tag = new ThicknessAnimation(new Thickness(-450, 0, 0, 0), new Duration(new TimeSpan(1500000)));
            mother.BeginAnimation(Grid.MarginProperty, tag);
            rec.BeginAnimation(Rectangle.MarginProperty, ta);
            rec.Width = label2.ActualWidth;
            label1.Background = null;
            label1.Foreground = Brushes.White;
            label2.Background = Brushes.White;
            label2.Foreground = Brushes.Black;
        }

        #endregion

        private void TextTest_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }

    class FontWithColor
    {
       public System.Drawing.FontFamily font_name;
       public double font_size;
       public Brush font_color;
       public bool italic;
       public bool bold;
       public static Brush[] WMBrushes = { Brushes.AliceBlue, Brushes.AntiqueWhite, Brushes.Aqua, Brushes.Aquamarine, Brushes.Azure, Brushes.Beige, Brushes.Bisque, Brushes.Black, Brushes.BlanchedAlmond, Brushes.Blue, Brushes.BlueViolet, Brushes.Brown, Brushes.BurlyWood, Brushes.CadetBlue, Brushes.Chartreuse, Brushes.Chocolate, Brushes.Coral, Brushes.CornflowerBlue, Brushes.Cornsilk, Brushes.Crimson, Brushes.Cyan, Brushes.DarkBlue, Brushes.DarkCyan, Brushes.DarkGoldenrod, Brushes.DarkGray, Brushes.DarkGreen, Brushes.DarkKhaki, Brushes.DarkMagenta, Brushes.DarkOliveGreen, Brushes.DarkOrange, Brushes.DarkOrchid, Brushes.DarkRed, Brushes.DarkSalmon, Brushes.DarkSeaGreen, Brushes.DarkSlateBlue, Brushes.DarkSlateGray, Brushes.DarkTurquoise, Brushes.DarkViolet, Brushes.DeepPink, Brushes.DeepSkyBlue, Brushes.DimGray, Brushes.DodgerBlue, Brushes.Firebrick, Brushes.FloralWhite, Brushes.ForestGreen, Brushes.Fuchsia, Brushes.Gainsboro, Brushes.GhostWhite, Brushes.Gold, Brushes.Goldenrod, Brushes.Gray, Brushes.Green, Brushes.GreenYellow, Brushes.Honeydew, Brushes.HotPink, Brushes.IndianRed, Brushes.Indigo, Brushes.Ivory, Brushes.Khaki, Brushes.Lavender, Brushes.LavenderBlush, Brushes.LawnGreen, Brushes.LemonChiffon, Brushes.LightBlue, Brushes.LightCoral, Brushes.LightCyan, Brushes.LightGoldenrodYellow, Brushes.LightGray, Brushes.LightGreen, Brushes.LightPink, Brushes.LightSalmon, Brushes.LightSeaGreen, Brushes.LightSkyBlue, Brushes.LightSlateGray, Brushes.LightSteelBlue, Brushes.LightYellow, Brushes.Lime, Brushes.LimeGreen, Brushes.Linen, Brushes.Magenta, Brushes.Maroon, Brushes.MediumAquamarine, Brushes.MediumBlue, Brushes.MediumOrchid, Brushes.MediumPurple, Brushes.MediumSeaGreen, Brushes.MediumSlateBlue, Brushes.MediumSpringGreen, Brushes.MediumTurquoise, Brushes.MediumVioletRed, Brushes.MidnightBlue, Brushes.MintCream, Brushes.MistyRose, Brushes.Moccasin, Brushes.NavajoWhite, Brushes.Navy, Brushes.OldLace, Brushes.Olive, Brushes.OliveDrab, Brushes.Orange, Brushes.OrangeRed, Brushes.Orchid, Brushes.PaleGoldenrod, Brushes.PaleGreen, Brushes.PaleTurquoise, Brushes.PaleVioletRed, Brushes.PapayaWhip, Brushes.PeachPuff, Brushes.Peru, Brushes.Pink, Brushes.Plum, Brushes.PowderBlue, Brushes.Purple, Brushes.Red, Brushes.RosyBrown, Brushes.RoyalBlue, Brushes.SaddleBrown, Brushes.Salmon, Brushes.SandyBrown, Brushes.SeaGreen, Brushes.SeaShell, Brushes.Sienna, Brushes.Silver, Brushes.SkyBlue, Brushes.SlateBlue, Brushes.SlateGray, Brushes.Snow, Brushes.SpringGreen, Brushes.SteelBlue, Brushes.Tan, Brushes.Teal, Brushes.Thistle, Brushes.Tomato, Brushes.Turquoise, Brushes.Violet, Brushes.Wheat, Brushes.White, Brushes.WhiteSmoke, Brushes.Yellow, Brushes.YellowGreen };
       public static string[] BrushesNames = { "AliceBlue", "AntiqueWhite", "Aqua", "Aquamarine", "Azure", "Beige", "Bisque", "Black", "BlanchedAlmond", "Blue", "BlueViolet", "Brown", "BurlyWood", "CadetBlue", "Chartreuse", "Chocolate", "Coral", "CornflowerBlue", "Cornsilk", "Crimson", "Cyan", "DarkBlue", "DarkCyan", "DarkGoldenrod", "DarkGray", "DarkGreen", "DarkKhaki", "DarkMagenta", "DarkOliveGreen", "DarkOrange", "DarkOrchid", "DarkRed", "DarkSalmon", "DarkSeaGreen", "DarkSlateBlue", "DarkSlateGray", "DarkTurquoise", "DarkViolet", "DeepPink", "DeepSkyBlue", "DimGray", "DodgerBlue", "Firebrick", "FloralWhite", "ForestGreen", "Fuchsia", "Gainsboro", "GhostWhite", "Gold", "Goldenrod", "Gray", "Green", "GreenYellow", "Honeydew", "HotPink", "IndianRed", "Indigo", "Ivory", "Khaki", "Lavender", "LavenderBlush", "LawnGreen", "LemonChiffon", "LightBlue", "LightCoral", "LightCyan", "LightGoldenrodYellow", "LightGray", "LightGreen", "LightPink", "LightSalmon", "LightSeaGreen", "LightSkyBlue", "LightSlateGray", "LightSteelBlue", "LightYellow", "Lime", "LimeGreen", "Linen", "Magenta", "Maroon", "MediumAquamarine", "MediumBlue", "MediumOrchid", "MediumPurple", "MediumSeaGreen", "MediumSlateBlue", "MediumSpringGreen", "MediumTurquoise", "MediumVioletRed", "MidnightBlue", "MintCream", "MistyRose", "Moccasin", "NavajoWhite", "Navy", "OldLace", "Olive", "OliveDrab", "Orange", "OrangeRed", "Orchid", "PaleGoldenrod", "PaleGreen", "PaleTurquoise", "PaleVioletRed", "PapayaWhip", "PeachPuff", "Peru", "Pink", "Plum", "PowderBlue", "Purple", "Red", "RosyBrown", "RoyalBlue", "SaddleBrown", "Salmon", "SandyBrown", "SeaGreen", "SeaShell", "Sienna", "Silver", "SkyBlue", "SlateBlue", "SlateGray", "Snow", "SpringGreen", "SteelBlue", "Tan", "Teal", "Thistle", "Tomato", "Turquoise", "Violet", "Wheat", "White", "WhiteSmoke", "Yellow", "YellowGreen" };

       public FontWithColor(int font_index, int color_index, double font_size, bool italic, bool bold)
       {
           font_name = System.Drawing.FontFamily.Families[font_index];
           this.font_size = font_size;
           font_color = WMBrushes[color_index];
           this.italic = italic;
           this.bold = bold;
       }

       public FontWithColor(System.Drawing.FontFamily font, Brush color, double font_size, bool italic, bool bold)
       {
           font_name = font;
           this.font_size = font_size;
           font_color = color;
           this.italic = italic;
           this.bold = bold;
       }

       public FontWithColor(FontFamily font, Brush color, double font_size, bool italic, bool bold)
       {
           font_name = new System.Drawing.FontFamily(font.Source);
           this.font_size = font_size;
           font_color = color;
           this.italic = italic;
           this.bold = bold;

       }

       public static int GetColorIndex(Brush color)
       {
           for (int i = 0; i < BrushesNames.Length; i++)
               if (WMBrushes[i].ToString().ToLower().Equals(color.ToString().ToLower()))
                   return i;
           return -1;
       }
    }
}
