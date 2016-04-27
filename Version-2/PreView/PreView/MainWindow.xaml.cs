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

namespace PreView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Viewer.ViewImage(textBox.Text);
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Viewer.ViewVideo(textBox.Text);
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            Viewer.ViewMusic(textBox.Text);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            new Subtitle_Maker().Show();
        }
    }
}
