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
        }

        internal DateTime? ShowDialog(DateTime elementary_date)
        {
            date_picker.SelectedDate = elementary_date;
            hour.SelectedIndex = elementary_date.Hour;
            minute.SelectedIndex = elementary_date.Minute;
            second.SelectedIndex = elementary_date.Second;
            ShowDialog();
            if (!date_picker.SelectedDate.HasValue)
                return null;
            else
            {
                DateTime date = new DateTime(date_picker.SelectedDate.Value.Year, date_picker.SelectedDate.Value.Month, date_picker.SelectedDate.Value.Day, hour.SelectedIndex, minute.SelectedIndex, second.SelectedIndex, 0);
                return date;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            date_picker.SelectedDate = null;
            Close();
        }
    }
}
