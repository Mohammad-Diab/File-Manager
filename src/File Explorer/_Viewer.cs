using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace File_Explorer
{
    public partial class _Viewer : Form
    {
        FileInfo file;
        public _Viewer(string path)
        {
            InitializeComponent();
            InitializeComponent();
            file = new FileInfo(path);
        }

        private void Viewer_Load(object sender, EventArgs e)
        {
            FileName.Text = file.FullName;
            if (file.Extension.ToLower().Equals(".pdf"))
            { }
            axFoxitCtl1.OpenFile(file.FullName);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
/*
 * using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace File_Manager
{
    public partial class _Viewer : Form
    {
        FileInfo file;
        public _Viewer(string path)
        {
            InitializeComponent();
            file = new FileInfo(path);
        }

        private void Viewer_Load(object sender, EventArgs e)
        {
            FileName.Text = file.FullName;
            if (file.Extension.ToLower().Equals(".pdf"))
            { }
            PdfDoc.OpenFile(file.FullName);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
*/