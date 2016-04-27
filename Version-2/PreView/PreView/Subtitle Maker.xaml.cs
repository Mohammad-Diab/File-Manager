using Microsoft.Win32;
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

namespace PreView
{
    /// <summary>
    /// Interaction logic for Subtitle_Maker.xaml
    /// </summary>
    public partial class Subtitle_Maker : Window
    {
        List<SubtitleViewItem> list = new List<SubtitleViewItem>();
        public Subtitle_Maker()
        {
            InitializeComponent();
            listView.DataContext = list;
        }

        private void AddLine_Click(object sender, RoutedEventArgs e)
        {
            list.Add(new SubtitleViewItem(list.Count + 1, text1.Text, text2.Text, show1.Text, show2.Text, hide.Text));
            listView.Items.Refresh();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            System.Data.DataTable dt = new System.Data.DataTable("Subtitle");
            dt.Columns.Add(new System.Data.DataColumn("id", typeof(string)));
            dt.Columns.Add(new System.Data.DataColumn("Text1", typeof(string)));
            dt.Columns.Add(new System.Data.DataColumn("Text2", typeof(string)));
            dt.Columns.Add(new System.Data.DataColumn("From1", typeof(string)));
            dt.Columns.Add(new System.Data.DataColumn("From2", typeof(string)));
            dt.Columns.Add(new System.Data.DataColumn("To", typeof(string)));
            foreach (var i in list)
            {
                dt.Rows.Add(dt.NewRow());
                string[] tmp = { i.ID.ToString(), i.Text1, i.Text2, i.From1.ToString(), i.From2.ToString(), i.To.ToString() };
                dt.Rows[dt.Rows.Count - 1].ItemArray = tmp;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "XML File|*.XML";
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    dt.WriteXml(sfd.FileName);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }
    }

    class SubtitleViewItem
    {
        int id;
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        string text1;
        public string Text1
        {
            get { return text1; }
            set { text1 = value; }
        }

        string text2;
        public string Text2
        {
            get { return text2; }
            set { text2 = value; }
        }

        long from1;
        public long From1
        {
            get { return from1; }
            set { from1 = value; }
        }

        long from2;
        public long From2
        {
            get { return from2; }
            set { from2 = value; }
        }

        long to;
        public long To
        {
            get { return to; }
            set { to = value; }
        }

        public SubtitleViewItem(int id, string t1, string t2, string f1, string f2, string to)
        {
            ID = id;
            Text1 = t1;
            Text2 = t2;
            From1 = StringToLong(f1);
            From2 = StringToLong(f2);
            To = StringToLong(to);
        }

        private long StringToLong(string str)
        {
            long time = 0;
            string[] sp = { ":" };
            var s = str.Split(sp, StringSplitOptions.RemoveEmptyEntries);
            int[] facts = { 3600000, 60000, 1000, 1 };
            for (int i = s.Length - 1; i >= 0; i--)
            {
                int tmp;
                int.TryParse(s[i], out tmp);
                time += (tmp * facts[i]);
            }
            return time;
        }
    }
}
