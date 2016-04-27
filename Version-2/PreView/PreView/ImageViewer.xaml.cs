using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using Core;

namespace PreView
{
    /// <summary>
    /// Interaction logic for ImageViewer.xaml
    /// </summary>
    public partial class ImageViewer : Window
    {
        bool menu_is_openned = false;
        bool IsOpenned
        {
            get { return menu_is_openned; }
            set
            {
                if (value)
                {
                    file_button.IsEnabled = false;
                    //back_grid.Visibility = file_menu.Visibility = Visibility.Visible;

                    ThicknessAnimation f_rec_ani = new ThicknessAnimation(new Thickness(20, 4, 0, 0), new Duration(new TimeSpan(500000)));
                    f_rec_ani.Completed += (object sender, EventArgs e) =>
                    {
                        ThicknessAnimation s_rec_ani = new ThicknessAnimation(new Thickness(20, 9, 0, 0), new Duration(new TimeSpan(500000)));
                        s_rec_ani.Completed += (object sender2, EventArgs e2) =>
                        {
                            ThicknessAnimation t_rec_ani = new ThicknessAnimation(new Thickness(20, 14, 0, 0), new Duration(new TimeSpan(500000)));
                            t_rec_ani.Completed += (object sender3, EventArgs e3) =>
                            {
                                //for (int i = 1; i < hbrg_recs.Children.Count; i++)
                                //    hbrg_recs.Children[i].Visibility = Visibility.Collapsed;
                                //move arow
                                hbrg_recs.Children[0].Visibility = Visibility.Visible;
                                ThicknessAnimation arr_ani = new ThicknessAnimation(new Thickness(0, 0, 0, 0), new Duration(new TimeSpan(750000)));
                                hbrg_recs.Children[0].BeginAnimation(MarginProperty, arr_ani);
                            };
                            hbrg_recs.Children[3].BeginAnimation(MarginProperty, t_rec_ani);
                        };
                        hbrg_recs.Children[2].BeginAnimation(MarginProperty, s_rec_ani);
                    };
                    hbrg_recs.Children[1].BeginAnimation(MarginProperty, f_rec_ani);
                   
                    ThicknessAnimation menu_anim = new ThicknessAnimation(new Thickness(0), new Duration(new TimeSpan(1000000)));
                    menu_anim.Completed += (object sender, EventArgs e) => { file_button.IsEnabled = true; };
                    main_grid.BeginAnimation(MarginProperty, menu_anim);
                    //DoubleAnimation bb = new DoubleAnimation(40, 120, new Duration(new TimeSpan(10000)));
                    //hhh.BeginAnimation(WidthProperty, bb);
                    //content.BeginAnimation(MarginProperty, menu_anim);
                }
                else
                {
                    file_button.IsEnabled = false;
                    //menu_list.SelectedIndex = -1;
                    ThicknessAnimation arr_ani = new ThicknessAnimation(new Thickness(-40, 0, 0, 0), new Duration(new TimeSpan(500000)));
                    arr_ani.Completed += (object sender4, EventArgs e4) =>
                    {
                        hbrg_recs.Children[0].Visibility = Visibility.Hidden;
                        ThicknessAnimation f_rec_ani = new ThicknessAnimation(new Thickness(0, 14, 0, 0), new Duration(new TimeSpan(750000)));
                        f_rec_ani.Completed += (object sender, EventArgs e) =>
                        {
                            ThicknessAnimation s_rec_ani = new ThicknessAnimation(new Thickness(0, 9, 0, 0), new Duration(new TimeSpan(750000)));
                            s_rec_ani.Completed += (object sender2, EventArgs e2) =>
                            { hbrg_recs.Children[1].BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(0, 4, 0, 0), new Duration(new TimeSpan(750000)))); };
                            hbrg_recs.Children[2].BeginAnimation(MarginProperty, s_rec_ani);
                        };
                        //for (int i = 1; i < hbrg_recs.Children.Count; i++)
                        //    hbrg_recs.Children[i].Visibility = Visibility.Visible;
                        hbrg_recs.Children[3].BeginAnimation(MarginProperty, f_rec_ani);

                    };
                    hbrg_recs.Children[0].BeginAnimation(MarginProperty, arr_ani);



                    ThicknessAnimation menu_anim = new ThicknessAnimation(new Thickness(-100, 0, 0, 0), new Duration(new TimeSpan(1000000)));
                    menu_anim.Completed += (object sender, EventArgs e) =>
                    {
                        //back_grid.Visibility = file_menu.Visibility = Visibility.Collapsed;
                        file_button.IsEnabled = true;
                    };
                    main_grid.BeginAnimation(MarginProperty, menu_anim);
                    //content.BeginAnimation(MarginProperty, menu_anim);
                }
                menu_is_openned = value;
            }
        }

        int hide_controls_after = 3000;
        bool is_mouse_on_controls = false;

        public ImageViewer()
        {
            InitializeComponent();
        }

        private void File_Click(object sender, RoutedEventArgs e)
        {
            IsOpenned = !IsOpenned;
        }


        bool isOn = false;
        Point old_point;

        internal void MakeImageViewer(FileInfo file)
        {
            try
            {
                ViewImage(file);
                try
                { Class1.GetFiles(file.Directory, FileType.Image, false, false); }
                catch (Exception) { }
            }
            catch (Exception)
            { caution.Visibility = Visibility.Visible; }
        }

        void ViewImage(FileInfo image_file)
        {
            BitmapImage bi = new BitmapImage(new Uri(image_file.FullName));
            Height = bi.PixelHeight < SystemParameters.PrimaryScreenHeight - 64 ? bi.PixelHeight + 48 : SystemParameters.PrimaryScreenHeight - 64;
            Width = bi.PixelWidth < SystemParameters.PrimaryScreenWidth - 64 ? bi.PixelWidth + 24 : SystemParameters.PrimaryScreenWidth - 64;
            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2 - 10;
            Top = (SystemParameters.PrimaryScreenHeight - Height) / 2 - 10;
            image.Source = bi;
            DoubleAnimation da = new DoubleAnimation(-45, 0, new Duration(TimeSpan.FromMilliseconds(150)));
            RotateTransform rt = new RotateTransform();
            image.RenderTransform = rt;
            image.RenderTransformOrigin = new Point(0, 1);
            rt.BeginAnimation(RotateTransform.AngleProperty, da);
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (controls.Visibility == Visibility.Hidden)
                {
                    controls.Visibility = Visibility.Visible;
                    DoubleAnimation controls_animation = new DoubleAnimation(1, new Duration(new TimeSpan(0, 0, 0, 0, 300)));
                    controls.BeginAnimation(OpacityProperty, controls_animation);
                }
                hide_controls_after = 3000;
            }
        }

        private void ImageScrol_MouseLeave(object sender, MouseEventArgs e)
        {
            isOn = false;
            old_point = e.GetPosition(null);
        }

        private void ImageScrol_MouseDown(object sender, MouseButtonEventArgs e)
        {
            old_point = e.GetPosition(null);
            isOn = true;
        }

        private void ImageScrol_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isOn = false;
            old_point = e.GetPosition(null);
        }

        private void ImageScrol_MouseMove(object sender, MouseEventArgs e)
        {
            if (isOn)
            {
                ScrollViewer scr = ((ScrollViewer)sender);
                Point new_point = e.GetPosition(null);
                scr.ScrollToHorizontalOffset(scr.HorizontalOffset + (old_point.X - new_point.X));
                scr.ScrollToVerticalOffset(scr.VerticalOffset + (old_point.Y - new_point.Y));
                old_point = e.GetPosition(null);
            }
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ImageControl_MouseLeave(object sender, MouseEventArgs e)
        {
            is_mouse_on_controls = false;
        }

    }


}
