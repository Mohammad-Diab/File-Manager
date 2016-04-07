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
using System.Windows.Shapes;
//using Xceed.Wpf.Toolkit;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for ChangeDate.xaml
    /// </summary>
    public partial class ChangeDate : Window
    {
        public ChangeDate()
        {
            InitializeComponent();
            //DateTimePicker h = new DateTimePicker();
            //(Content as Grid).Children.Add(h);
        }

        bool accept = false;

        internal DateTime? ShowDialog(DateTime elementary_date)
        {
            if (elementary_date.Kind == DateTimeKind.Utc)
                elementary_date = elementary_date.ToLocalTime();
            date_picker.SelectedDate = elementary_date;
            hour.SelectedIndex = elementary_date.Hour;
            minute.SelectedIndex = elementary_date.Minute;
            second.SelectedIndex = elementary_date.Second;
            ShowDialog();
            if (!accept || !date_picker.SelectedDate.HasValue)
                return null;
            else
            {
                DateTime date = new DateTime(date_picker.SelectedDate.Value.Year, date_picker.SelectedDate.Value.Month, date_picker.SelectedDate.Value.Day, hour.SelectedIndex, minute.SelectedIndex, second.SelectedIndex, 0, DateTimeKind.Local);
                return date;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            accept = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            date_picker.SelectedDate = null;
            Close();
        }

        private void Now_Click(object sender, RoutedEventArgs e)
        {
            date_picker.SelectedDate = DateTime.Now.Date;
            hour.SelectedIndex = DateTime.Now.Hour;
            minute.SelectedIndex = DateTime.Now.Minute;
            second.SelectedIndex = DateTime.Now.Second;
        }
    }
}
