using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;
using System.Windows.Media;
using System.Runtime.InteropServices;
namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Point old_point;
        bool go = false;
        public MainWindow()
        {
            InitializeComponent();
        }
        public static string[] file_extentions = { "directory", ".txt", ".log", ".sh", ".bsh", ".bat", ".cmd", ".nt", ".c", ".cpp", ".cs", ".html", ".mht", ".htm", ".shtml", ".shtm", ".xhtml", ".hta", ".ini", ".inf", ".reg", ".url", ".java", ".js", ".nfo", ".pas", ".inc", ".pl", ".pm", ".plx", ".php", ".php3", ".phtml", ".ps", ".ps1", ".py", ".pyw", ".sql", ".tex", ".vb", ".vbs", ".v", ".vhdl", ".xml", ".xsml", ".xsl", ".xsd", ".kml", ".wsdl", ".yml", ".docx", ".doc", ".odt", ".rtf", ".docm", ".dot", ".dotx", ".ppsx", ".pptx", ".pps", ".ppt", ".pptm", ".potx", ".pot", ".odp", ".xlsx", ".xlsm", ".xlsb", ".xls", ".xltx", ".xltm", ".xlt", ".ods", ".accdb", ".mdl", ".odbc", ".accdt", ".mdb", ".pdf", ".xps", ".oxps", ".djvu", ".comic", ".chm", ".mobi", ".epub", ".vdi", ".vhd", ".hdd", ".iso", ".bin", ".isz", ".img", ".dmg", ".nrg", ".mou", ".zip", ".cab", ".7z", ".rar", ".tar", ".deb", ".xz", ".txz", ".lzma", ".cpio", ".bz2", ".bzip2", ".gz", ".taz", ".fat", ".ntfs", ".hfs", ".rpm", ".wim", ".ace", ".jar", ".uha", ".squashfs", ".mp3", ".3gp", ".3ggp2", ".3gpp", ".acc", ".ac3", ".adt", ".adts", ".amr", ".amv", ".avi", ".flv", ".hdmov", ".mid", ".midi", ".mka", ".mkv", ".mp2", ".mp4", ".mp4v", ".mpeg", ".mpg", ".ogg", ".ogm", ".ogv", ".opus", ".ra", ".ram", ".rm", ".wav", ".wma", ".wmv", ".wv", ".vob", ".mov", ".swf", ".aac", ".jpg", ".png", ".ico", ".bmp", ".gif", ".tif", ".tiff", ".dib", ".jpe", ".jpeg", ".jfif", ".svg", ".jxr", ".wdp", ".psd", ".pcx", ".tga", ".fon", ".ttc", ".ttf", ".dll", ".exe", ".cpl", ".h", ".drv", ".msc", ".com", ".scr", ".msi", ".sys", ".db", ".pnf", ".dat", ".001", ".tbz2", ".tbz", ".gzip", ".tgz", ".tpz", ".z", ".lzh", ".lha", ".arj", ".xar", ".kgp", ".kge", ".", "unknown" };
        public static string[] file_image = { "folder.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-x.png", "text-x.png", "text-x.png", "text-c.png", "text-c++.png", "text-cs.png", "text-html.png", "text-html.png", "text-html.png", "text-html.png", "text-html.png", "text-html.png", "text-html.png", "text-script.png", "text-script.png", "text-reg.png", "text-html.png", "text-java.png", "text-script.png", "text-script.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-file.png", "text-python.png", "text-python.png", "text-file.png", "text-file.png", "text-script.png", "text-script.png", "text-file.png", "text-file.png", "text-script.png", "text-script.png", "text-script.png", "text-script.png", "text-script.png", "text-script.png", "text-file.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "text-richtext.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-presentation.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-spreadsheet.png", "text-database.png", "text-database.png", "text-database.png", "text-database.png", "text-database.png", "text-pdf.png", "text-xps.png", "text-xps.png", "text-file.png", "text-file.png", "text-help.png", "text-file.png", "text-file.png", "disk-vdi.png", "disk-vhd.png", "disk-hdd.png", "disk-iso.png", "disk-iso.png", "disk-iso.png", "disk-iso.png", "archive-file.png", "disk-iso.png", "disk-mou.png", "archive-zip.png", "archive-cab.png", "archive-7z.png", "archive-rar.png", "archive-tar.png", "archive-deb.png", "archive-file.png", "archive-file.png", "archive-lzma.png", "archive-file.png", "archive-bz2.png", "archive-bz2.png", "archive-gz.png", "archive-file.png", "archive-file.png", "archive-file.png", "archive-file.png", "archive-rpm.png", "archive-file.png", "archive-ace.png", "archive-jar.png", "archive-file.png", "archive-file.png", "multimedia-mp3.png", "multimedia-video.png", "multimedia-video.png", "multimedia-video.png", "multimedia-audio.png", "multimedia-audio.png", "multimedia-video.png", "multimedia-video.png", "multimedia-audio.png", "multimedia-video.png", "multimedia-video.png", "multimedia-flv.png", "multimedia-video.png", "multimedia-audio.png", "multimedia-audio.png", "multimedia-video.png", "multimedia-audio.png", "multimedia-mp3.png", "multimedia-video.png", "multimedia-video.png", "multimedia-video.png", "multimedia-video.png", "multimedia-ogg.png", "multimedia-video.png", "multimedia-video.png", "multimedia-audio.png", "multimedia-audio.png", "multimedia-audio.png", "multimedia-video.png", "multimedia-wav.png", "multimedia-wma.png", "multimedia-video.png", "multimedia-wav.png", "multimedia-video.png", "multimedia-video.png", "multimedia-flv.png", "multimedia-audio.png", "multimedia-jpg.png", "multimedia-png.png", "multimedia-ico.png", "multimedia-bmp.png", "multimedia-gif.png", "multimedia-tif.png", "multimedia-tif.png", "multimedia-image.png", "multimedia-jpg.png", "multimedia-jpg.png", "multimedia-image.png", "multimedia-image.png", "multimedia-image.png", "multimedia-image.png", "multimedia-psd.png", "multimedia-image.png", "multimedia-image.png", "x-fon.png", "x-fon.png", "x-fon.png", "x-dll.png", "x-exe.png", "x-dll.png", "text-script.png", "x-dll.png", "x-exe.png", "text-x.png", "x-scr.png", "x-package.png", "x-sys.png", "x-sys.png", "x-sys.png", "file.png", "archive-file.png", "archive-file.png", "archive-file.png", "archive-gz.png", "archive-gz.png", "archive-file.png", "archive-file.png", "archive-file.png", "archive-file.png", "archive-file.png", "archive-file.png", "archive-file.png", "archive-file.png", "file.png", "unknown-file.png" };
        public static string[] file_description = { "Files Directory", "Plan Text File", "Log File", "Unix Script File", "Unix Script File", "Batch File", "Command Script", ".nt", "'C' Source File", "'C++' Source File", "'C#' Source File", "HTML (Hyper Text Markup Language) Document", "HTML Archive File", "HTML (Hyper Text Markup Language) Document", "HTML Document", "HTML Document", "XHTML Document", "HTML Application", "Configuration Settings", "Setup Information", "Registration Entries", "Internet Shortcut", "java Source File", "java Script File", "System Information File", "Pascal Source File", "Pascal Source File", "Perl Source File", "Perl Source File", "Perl Source File", "PHP Hypertext Preprocessor File", "PHP Hypertext Preprocessor File", "PHP Hypertext Preprocessor File", "Post Script File", "Windows PowerShell Script", "Python File", "Python File", "Structured Query Language File", "TEX File", "Visual Basic File", "Visual Basic File", "Verilog file", "VHSIC Hardware Description Language File", "Extensible Markup Language File", "Extensible Markup Language File", "Extensible Markup Language File", "Extensible Markup Language File", "Extensible Markup Language File", "Extensible Markup Language File", "YAML Ain't Markup Language", "Microsoft Word Document", "Microsoft Word 97 - 2003 Document", "Open Document Text", "Rich Text Format", "Microsoft Word Macro-Enabled Document", "Microsoft Word 97 - 2003 Template", "Microsoft Word Template", "Microsoft PowerPoint Slide Show", "Microsoft PowerPoint Presentation", "Microsoft PowerPoint 97-2003 Slide Show", "Microsoft PowerPoint 97-2003 Presentation", "Microsoft PowerPoint Macro-Enabled Presentation", "Microsoft PowerPoint Template", "Microsoft PowerPoint 97-2003 Template", "Open Document Presentation", "Microsoft Excel Worksheet", "Microsoft Excel Macro-Enabled Worksheet", "Microsoft Excel Binary Worksheet", "Microsoft Excel 97-2003 Worksheet", "Microsoft Excel Template", "Microsoft Excel Macro-Enabled Template", ".Microsoft Excel Template", "Open Document Spreadsheet", "Microsoft Access Database", ".mdl", ".odbc", "Microsoft Access Template", "Microsoft Access Database", "Portable Document Format File", "XPS Document", "Open XPS Document", "Djvu Document", "Comic Document", "Compiled HTML Help File", ".mobi", ".epub", "Virtual Disk Image", "Virtual Hard Disk", "Virtual Hard Disk", "Disc Image File", "BIN File", "Compressed Disc Image File", "Disc Image File", "Mac Application Package", "Nero Burning ROM", "WinMount archiver", "Compressed Zipped File", "Cabinet File", "7Z Archive File", "Winrar Archive File", "TAR Archive File", "Debian Application Package", "XZ Archive File", "TXZ Archive File", "LZMA Archive File", "CPIO Archive File", "BZ2 Archive File", "BZIP2 Archive File", "GZ Archive File", "TAZ Archive File", "Fat Archive File", "NTFS Archive File", "HFS Archive File", "Red Hat Application Package", "Wim Image File", "Winace Archive File", "Executable Jar File", "Uha Archive File", "Squashfs Image File", "MP3 Format Sound", "3GPP Audio/Video", "3GPP2 Audio/Video", "3GPP Audio/Video", "ACC File", "AC3 Audio File", "ADTS Audio File", "ADTS Audio File", "AMR Audio File", "AMV Video File", "Video Clip File", "Flash Video File", "HDMOV Video File", "MIDI Sequence File", "MIDI Sequence File", "Matroska Audio File", "Matroska Video File", "MP3 Format Sound", "MP4 Video File", "MP4 Video File", "Movie Clip File", "Movie Clip File", ".ogg", "Ogg Video File", "Ogg Video File", "Opus Audio File", "RealMedia Audio File", "RealMedia File", "RealMedia Video File", "Wave Sound File", "Windows Media Audio File", "Windows Media Audio/Video File", "WavPack Audio File", ".DVD VOB File", "QuickTime Movie", "Shockwave Flash Object", "ADTS Audio File", "JPEG Image File", "PNG Image File", "Icon File", "Bitmap Image File", "GIF Image File", "TIFF Image File", "TIFF Image File", "Bitmap Image File", "JPEG Image File", "JPEG Image File", "JPEG Image File", "Standard Vector Graphics", "Windows Media Photo File", "Windows Media Photo File", ".psd", ".pcx", ".tga", "Font File", "TrueType Collection Font File", "TrueType Font File", "Application Extension", "Windows Application", "Windows Control Panel Item", "C++ Library Source File", "Device Driver", "Microsoft Common Console Document", "MS-DOS Application", "Screen Saver File", "Windows Installer Package", "System File", "Data Base File", "Precompiled Setup Information", "Data File", "7z Split Archive File", "TBZ2 Archive File", "TPZ Archive File", "GZ Archive File", "TGZ Archive File", "TPZ Archive File", "Z Archive File", "LZH Archive File", "LHA Archive File", "ARJ Archive File", "XAR Archive File", "KGP Archive File", "KGE Archive Files", "File", "Unknown File Type" };
        public static FileCategory[] file_type = { FileCategory.Directory, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.TextCanRunAsApplication, FileCategory.TextCanRunAsApplication, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Html, FileCategory.Html, FileCategory.Html, FileCategory.Html, FileCategory.Html, FileCategory.Html, FileCategory.Application, FileCategory.Text, FileCategory.Text, FileCategory.TextCanRunAsApplication, FileCategory.TextCanRunAsApplication, FileCategory.Text, FileCategory.TextCanRunAsApplication, FileCategory.TextCanRunAsApplication, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Html, FileCategory.Html, FileCategory.Html, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Html, FileCategory.Html, FileCategory.Html, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Text, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Unknown/*".mdl"*/, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Document, FileCategory.Unknown/*".mobi"*/, FileCategory.Unknown/*".epub"*/, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Application, FileCategory.Archive, FileCategory.Archive, FileCategory.Audio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio,/*".acc"*/FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Audio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Audio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Audio, FileCategory.Vedio, FileCategory.Vedio, FileCategory.Html, FileCategory.Audio, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Image, FileCategory.Html, FileCategory.Image, FileCategory.Image, FileCategory.Unknown/*".psd"*/, FileCategory.Unknown/*".pcx"*/, FileCategory.Unknown/*".tga"*/, FileCategory.Font, FileCategory.Font, FileCategory.Font, FileCategory.CannotOpen, FileCategory.Application, FileCategory.Application, FileCategory.Text, FileCategory.CannotOpen, FileCategory.Application, FileCategory.Application, FileCategory.Application, FileCategory.Application, FileCategory.CannotOpen, FileCategory.CannotOpen, FileCategory.CannotOpen, FileCategory.CannotOpen, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Archive, FileCategory.Unknown/*"."*/, FileCategory.Unknown };

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            long h = DateTime.Now.Ticks;
            BinaryReader f = new BinaryReader(File.OpenRead(@"D:\Disks Images\Computer.mou"));
            BinaryWriter f2 = new BinaryWriter(File.Open(@"D:\User\Desktop\Quran 1432.mou", FileMode.OpenOrCreate));
            FileInfo m = new FileInfo(@"D:\Disks Images\Computer.mou");
            int buffer_size = 65536;
            byte[] n = new byte[buffer_size];
            long i;
            for (i = 0; i < m.Length; i += buffer_size)
            {
                f.Read(n, 0, buffer_size);
                f2.Write(n);
            }
            f2.Write(f.ReadBytes((int)(m.Length - (i - buffer_size))));
            f.Close();
            f2.Close();
            //File.Copy(@"D:\Disks Images\Quran 1432.isz", @"D:\User\Desktop\Quran 1432.isz");
            System.Windows.Forms.MessageBox.Show((DateTime.Now.Ticks - h).ToString(), n.Length.ToString());
            //File.WriteAllBytes("D:\\User\\Desktop\\Quran 1432.isz", File.ReadAllBytes("D:\\Disks Images\\Quran 1432.isz"));
            //var s1 = m.Open(FileMode.Open);
            //FileInfo n = new FileInfo();
            //var s2=n.Create();
            //for (int i = 0; i < m.Length; i++)
            //    s2.WriteByte((Byte)s1.Read());
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            StreamReader j = new StreamReader(File.OpenRead(@"D:\User\Desktop\gfhgfj.txt"));
            string h = j.ReadToEnd();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            RTF.SelectionBrush = Brushes.Tomato;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            Quick_Sort(0, file_extentions.Length - 1);
            StringBuilder h = new StringBuilder("");
            h.Append("{");
            foreach (var i in file_extentions)
            {
                h.Append("\"");
                h.Append(i);
                h.Append("\"");
                h.Append(",");
            }
            h.Append("}\r\n");
            h.Append("{");
            foreach (var i in file_image)
            {
                h.Append("\"");
                h.Append(i);
                h.Append("\"");
                h.Append(",");
            }
            h.Append("}\r\n");
            h.Append("{");
            foreach (var i in file_description)
            {
                h.Append("\"");
                h.Append(i);
                h.Append("\"");
                h.Append(",");
            }
            h.Append("}\r\n");
            h.Append("{");
            foreach (var i in file_type)
            {
                h.Append("FileCategory.");
                h.Append(i);
                h.Append(",");
            }
            h.Append("}");
            System.Windows.Clipboard.SetText(h.ToString());
        }

        private void Quick_Sort(int left, int right)
        {
            int pivot;
            if (right > left)
            {
                pivot = Partition(left, right);
                Quick_Sort(left, pivot - 1);
                Quick_Sort(pivot + 1, right);
            }
        }

        private int Partition(int left, int right)
        {
            int swapPos;
            int pivotPos = left + (right - left) / 2;
            Swap(left, pivotPos);
            swapPos = left + 1;
            for (int i = swapPos; i <= right; i++)
                if (String.Compare(file_extentions[i], file_extentions[left]) < 0)
                {
                    Swap(i, swapPos);
                    swapPos++;
                }
            Swap(left, swapPos - 1);
            return swapPos - 1;
        }

        private void Swap(int first, int last)
        {
            string t1 = file_extentions[first];
            file_extentions[first] = file_extentions[last];
            file_extentions[last] = t1;

            string t2 = file_image[first];
            file_image[first] = file_image[last];
            file_image[last] = t2;

            string t3 = file_description[first];
            file_description[first] = file_description[last];
            file_description[last] = t3;

            FileCategory t4 = file_type[first];
            file_type[first] = file_type[last];
            file_type[last] = t4;
        }

        public enum FileCategory
        {
            Directory = 0,
            Text = 1,
            TextCanRunAsApplication = 2,
            Image = 3,
            Audio = 4,
            Vedio = 5,
            Html = 6,
            Archive = 7,
            Font = 8,
            Document = 9,
            Application = 10,
            CannotOpen = 11,
            Unknown = 100
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            Rectangle j = new Rectangle();
            j.Width = j.Height = 100;
            j.Fill = dddddddddd.Fill;
            jjjjj.ToolTip = j;
        }

        private void TextBox_PreviewKeyUp_1(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                try
                { frame.Navigate(new Uri(((System.Windows.Controls.TextBox)sender).Text)); }
                catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void frame_Navigating(object sender, NavigationEventArgs e)
        {
            if (frame.Source != null)
                ggg.Text = frame.Source.LocalPath;
        }

        private void Button_Click_010(object sender, RoutedEventArgs e)
        {
            int j = 0;
            Thread m1 = new Thread(() => { while (true) j++; });
            Thread m2 = new Thread(() => { while (true) j++; });
            Thread m3 = new Thread(() => { while (true) j++; });
            Thread m4 = new Thread(() => { while (true) j++; });
            Thread m5 = new Thread(() => { while (true) j++; });
            m1.Start();
            m2.Start();
            m3.Start();
            m4.Start();
            m5.Start();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            List<FileInfo> list = new List<FileInfo>();
            OpenFileDialog open = new OpenFileDialog();
            SaveFileDialog save = new SaveFileDialog();
            while (open.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                list.Add(new FileInfo(open.FileName));
            save.ShowDialog();
            BinaryWriter BW = new BinaryWriter(File.Open(save.FileName, FileMode.OpenOrCreate, FileAccess.Write));
            foreach (var i in list)
            {
                BinaryReader BR = new BinaryReader(File.OpenRead(i.FullName));
                long k;
                for (k = 65536; k <= i.Length; k += 65536)
                    BW.Write(BR.ReadBytes(65536));
                BW.Write(BR.ReadBytes((int)(i.Length - (k - 65536))));
                BR.Close();
            }
            BW.Close();
        }

        private void Button_Click6(object sender, RoutedEventArgs e)
        {
            var fdh = new FolderBrowserDialog();
            fdh.ShowDialog();
            foreach (var i in new DirectoryInfo(fdh.SelectedPath).GetDirectories())
            {
                if (i.Attributes.HasFlag(FileAttributes.System))
                    i.Attributes -= FileAttributes.System;
                if (i.Attributes.HasFlag(FileAttributes.Hidden))
                    i.Attributes -= 2;
            }
        }

        //Slider
        private void Grid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Ellipse)((Grid)sender).Children[1]).Opacity = 0.9;
            ((Ellipse)((Grid)sender).Children[2]).Opacity = 0.9;
        }

        private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ((Ellipse)((Grid)sender).Children[1]).Opacity = 1;
            ((Ellipse)((Grid)sender).Children[2]).Opacity = 1;
        }

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Ellipse)((Grid)sender).Children[1]).Opacity = 0.8;
            ((Ellipse)((Grid)sender).Children[2]).Opacity = 0.8;
            old_point = e.GetPosition(null);
            go = true;
        }

        private void Grid_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (go && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point new_point = e.GetPosition(null);
                double diff = new_point.X - old_point.X;
                if (Math.Abs(diff) > 2)
                    if ((rec.Width + diff) > container.ActualWidth - 2)
                    {
                        mo.Margin = new Thickness(container.ActualWidth - 10, 0, 0, 0);
                        rec.Width = container.ActualWidth - 2;
                    }
                    else if (rec.Width + diff < 0)
                    {
                        mo.Margin = new Thickness(0, 0, 0, 0);
                        rec.Width = 0;
                    }
                    else
                    {
                        rec.Width = rec.Width + diff;
                        mo.Margin = new Thickness(rec.Width, 0, 0, 0);
                    }
                old_point.X = new_point.X;
            }
        }

        private void Grid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            go = false;
            ((Ellipse)((Grid)sender).Children[1]).Opacity = 1;
            ((Ellipse)((Grid)sender).Children[2]).Opacity = 1;
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            TreeViewItem n = new TreeViewItem();
            n.Header = "Go";

            TreeViewItem n1 = new TreeViewItem();
            Rectangle r1 = new Rectangle();
            r1.Fill = Brushes.Black;
            r1.Height = r1.Width = 50;
            Rectangle r2 = new Rectangle();
            r2.Height = r2.Width = 30;
            r2.Fill = Brushes.Bisque;
            Grid m1 = new Grid();
            m1.Children.Add(r1);
            m1.Children.Add(r2);
            n1.Header = m1;

            Rectangle r21 = new Rectangle();
            Rectangle r22 = new Rectangle();
            r21.Width = r21.Height = 30;
            r22.Width = r22.Height = 30;
            r21.Fill = Brushes.SteelBlue;
            r22.Fill = Brushes.Tomato;
            n1.Items.Add(r21);
            n1.Items.Add(r22);

            TreeViewItem n2 = new TreeViewItem();
            Ellipse e1 = new Ellipse();
            e1.Fill = Brushes.Red;
            e1.Height = e1.Width = 30;
            Ellipse e2 = new Ellipse();
            e2.Height = e2.Width = 20;
            e2.Fill = Brushes.Bisque;
            Grid m2 = new Grid();
            m2.Children.Add(e1);
            m2.Children.Add(e2);
            n2.Header = m2;

            n.Items.Add(n1);
            n.Items.Add(n2);
            l.Items.Add(n);
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            OpenFileDialog j = new OpenFileDialog();
            j.ShowDialog();
            if (j.FileName != null)
                compresser(j.FileName);
        }

        [DllImport("kernel32.dll")]
        public static extern int DeviceIoControl(IntPtr hDevice, int dwIoControlCode, ref short lpInBuffer, int nInBufferSize, IntPtr lpOutBuffer, int nOutBufferSize, ref int lpBytesReturned, IntPtr lpOverlapped);

        private void compresser(string fileName)
        {
            int lpBytesReturned = 0;
            int FSCTL_SET_COMPRESSION = 0x9C040;
            short COMPRESSION_FORMAT_DEFAULT = 1;
            FileStream f = File.Open(fileName, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
            int result = DeviceIoControl(f.Handle, FSCTL_SET_COMPRESSION, ref COMPRESSION_FORMAT_DEFAULT, 2 /*sizeof(short)*/, IntPtr.Zero, 0, ref lpBytesReturned, IntPtr.Zero);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBox")]
        public static extern int MyNewMessageBoxMethod(IntPtr hWnd, String text, String caption, uint type);
        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            MyNewMessageBoxMethod(new IntPtr(0), "Hello World!", "Hello Dialog", 7);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog gg=new FolderBrowserDialog();
            if (gg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DirectoryInfo folder = new DirectoryInfo(gg.SelectedPath);
                long folderSize = 0;
                FileInfo[] files = folder.GetFiles("*", SearchOption.AllDirectories);
                foreach (FileInfo file in files) folderSize += file.Length;
                System.Windows.MessageBox.Show(folderSize.ToString());
            }
        }
    }
}
