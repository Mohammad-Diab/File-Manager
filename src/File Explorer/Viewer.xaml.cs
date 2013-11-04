using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace File_Explorer
{
    /// <summary>
    /// Interaction logic for Viewer.xaml
    /// </summary>
    public partial class Viewer : Window
    {
        private string path;
        private ItemType itemType;
        private int p1;
        private int p2;

        public Viewer()
        {
            InitializeComponent();
        }

        public Viewer(string path, ItemType itemType, int p1, int p2)
        {
            // TODO: Complete member initialization
            this.path = path;
            this.itemType = itemType;
            this.p1 = p1;
            this.p2 = p2;
        }
    }
}
