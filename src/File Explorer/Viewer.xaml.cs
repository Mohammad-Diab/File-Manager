using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
//using System.Windows.Forms;
using GemBox.Document;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;
using System.IO.Compression;

namespace File_Manager
{
    /// <summary>
    /// Interaction logic for Viewer.xaml
    /// </summary>

    public partial class Viewer : Window
    {
        PhotoOrientation m_userRotation = PhotoOrientation.Normal;
        FileInfo file;
        DirectoryInfo directory;
        DriveInfo drive;
        bool? ftype;
        int sub_dir_count, file_count;
        bool isOn;
        string text_, imagesource = "pack://application:,,,/File Manager;component/Images/Icons/";
        string[] blue = { "abstract", "as", "base", "bool", "break", "byte", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while" };
        string[] cyan = { "Console", "Comparer", "DateTime", "Delegate", "Dictionary", "Dictionary.Enumerator", "Dictionary.KeyCollection", "Dictionary.KeyCollection.Enumerator", "Dictionary.ValueCollection", "Dictionary<,>.ValueCollection.Enumerator", "EqualityComparer", "Exception", "FileStream", "ICollection", "IComparer", "IDictionary", "IEnumerable", "IEnumerator", "IEqualityComparer", "IList", "Int16", "Int32", "Int64", "IntPtr", "KeyNotFoundException", "KeyValuePair", "LinkedList<>", "LinkedList.Enumerator", "LinkedListNode", "List", "List<>.Enumerator", "MessageBox", "Queue", "Queue<>.Enumerator", "SortedDictionary", "SortedDictionary.Enumerator", "SortedDictionary.KeyCollection", "SortedDictionary<,>.KeyCollection.Enumerator", "SortedDictionary.ValueCollection", "SortedDictionary.ValueCollection.Enumerator", "SortedList", "Stack", "Stack.Enumerator", "StreamReader", "StreamWriter", "StringBuilder" };
        public Viewer(string path, bool? type, int sub_dirs, int sub_files)
        {
            this.ftype = type;
            InitializeComponent();
            if (type == null)
                drive = new DriveInfo(path);
            else if (type == true)
                file = new FileInfo(path);
            else if (type == false)
                directory = new DirectoryInfo(path);
            sub_dir_count = sub_dirs;
            file_count = sub_files;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (ftype == true)
            {
                FileName.Text = file.FullName;
                if (MainWindow.IsImage(file.Extension))
                {
                    ImageElement.Visibility = Visibility.Visible;
                    ImageViewer.Source = new BitmapImage(new Uri(file.FullName));
                }
                else if (MainWindow.IsVedio(file.Extension) || MainWindow.IsAudio(file.Extension))
                {
                    MediaElement.Visibility = Visibility.Visible;
                    MediaViewer.Source = new Uri(file.FullName);
                    MediaViewer.Play();
                    isOn = true;
                    if (MediaViewer.NaturalDuration.HasTimeSpan)
                        MediaSlider.Maximum = MediaViewer.NaturalDuration.TimeSpan.Ticks / 1000000;
                    //Thread t1 = new Thread(SlidTraker);
                    ThreadStart bb = new ThreadStart(SlidTraker);
                    //t1.Start(MediaSlider);
                    //MediaViewer.Position = new TimeSpan((long)MediaSlider.Value);
                }
                else if (MainWindow.IsHtml(file.Extension))
                {
                    WebPageViewer.Visibility = Visibility.Visible;
                    WebPageViewer.Source = new Uri(file.FullName);
                    Grid1.Background = null;
                }
                else if (MainWindow.IsText(file.Extension))
                {
                    Text.Visibility = Visibility.Visible;
                    StreamReader SR = new StreamReader(file.FullName, Encoding.Default);
                    TextViewer.Document = Document(SR.ReadToEnd(), file.Extension);
                    SR.Close();
                    if (file.IsReadOnly)
                        EditText.IsEnabled = false;
                }
                else if (MainWindow.IsArchive(file.Extension))
                {
                    string unit;
                    Archive.Visibility = Visibility.Visible;
                    ArchiveImage.Source = new BitmapImage(new Uri(MainWindow.GetImageSource(imagesource, file.Extension, true)));
                    ArchiveName.Text = "Name: " + file.Name;
                    ArchiveSize.Text = "Size: " + MainWindow.File_Size(file.Length, out unit) + " " + unit;
                    ArchiveType.Text = "Type: " + Archive_Type(file.Extension);
                }
                else if (file.Extension.ToLower().Equals(".docx") || file.Extension.ToLower().Equals(".xps")||file.Extension.ToLower().Equals(".odt"))
                {
                    /*Docx Viewer
                    XpsDocument Doc;
                    ComponentInfo.SetLicense("FREE-LIMITED-KEY");
                    DocumentModel DM = new DocumentModel();
                    var section = new GemBox.Document.Section(DM);
                    DM.Sections.Add(section);
                    var paragraph = new GemBox.Document.Paragraph(DM);
                    section.Blocks.Add(paragraph);
                    var run = new GemBox.Document.Run(DM,"Hello Word");
                    paragraph.Inlines.Add(run);
                    Doc = DM.ConvertToXpsDocument(SaveOptions.XpsDefault);
                    XPSViewer.Document = Doc.GetFixedDocumentSequence();
                    */

                    //BinaryReader n=new BinaryReader(new FileStream(file.FullName,FileMode.Open));
                    //DocumentModel DM = new DocumentModel();
                    //DM.ConvertToXpsDocument(SaveOptions.XpsDefault);
                    XpsDocument XPS = new XpsDocument(file.FullName, FileAccess.Read);
                    XPSViewer.Document = XPS.GetFixedDocumentSequence();
                    XPSViewer.Visibility = Visibility.Visible;
                    //StreamReader SR = new StreamReader(file.FullName, Encoding.ASCII);
                    ////TextViewer.Document = ((Block)SR.ReadToEnd());
                    //if (file.IsReadOnly)
                    //    EditText.IsEnabled = false;
                }
                else if (false)
                {
                    //System.Windows.Xps;
                }
                else
                {
                    if (file.Extension.ToLower().Equals(".msi"))
                        OpenUnknoun.Content = "Install Package";
                    else if (file.Extension.ToLower().Equals(".exe"))
                        OpenUnknoun.Content = "Run Application";
                    string unit;
                    Unknown.Visibility = Visibility.Visible;
                    ItemImage.Source = new BitmapImage(new Uri(MainWindow.GetImageSource(imagesource, file.Extension, true)));
                    name.Text = "Name: " + file.Name;
                    size.Text = "Size: " + MainWindow.File_Size(file.Length, out unit) + " " + unit;
                    type.Text = "Type: " + file.Extension + " (file)";
                    created.Text = "Data Created: " + file.CreationTime.ToString();
                    modified.Text = "Data Modified: " + file.LastWriteTime.ToString();
                    Accessed.Text = "Last Access: " + file.LastAccessTime.ToString();
                }
            }
            else if (ftype == false)
            {
                OpenUnknoun.Content = "Open";
                FileName.Text = directory.FullName;
                Unknown.Visibility = Visibility.Visible;
                ItemImage.Source = new BitmapImage(new Uri(MainWindow.GetImageSource(imagesource, "Folder", true)));
                name.Text = "Name: " + directory.Name;
                size.Text = "Contains " + (sub_dir_count + file_count) + " Items (" + sub_dir_count + " Directories & " + file_count + " Files)";
                type.Text = "Type: Directory";
                created.Text = "Data Created: " + directory.CreationTime.ToString();
                modified.Text = "Data Modified: " + directory.LastWriteTime.ToString();
                Accessed.Text = "Last Access: " + directory.LastAccessTime.ToString();
            }
            else
            {
                string unit;
                FileName.Text = drive.Name;
                Unknown.Visibility = Visibility.Visible;
                ItemImage.Source = new BitmapImage(new Uri(MainWindow.GetImageSource(imagesource, drive.DriveType.ToString(), drive.IsReady)));
                size.Text = "Contains " + (sub_dir_count + file_count) + " Items (" + sub_dir_count + " Directories & " + file_count + " Files)";
                if (drive.IsReady)
                {
                    name.Text = "Name: " + drive.VolumeLabel + " (" + drive.Name + ")";
                    type.Text = "File System: " + drive.DriveFormat.ToString();
                    created.Text = "Total Size: " + MainWindow.File_Size(drive.TotalSize, out unit) + " " + unit;
                    modified.Text = "Free Space: " + MainWindow.File_Size(drive.AvailableFreeSpace, out unit) + " " + unit;
                    driveusage.Visibility = Visibility.Visible;
                    driveusage.Value = 100 - (drive.AvailableFreeSpace * 100 / drive.TotalSize);
                    OpenUnknoun.Content = "Open";
                }
                else
                {
                    name.Text = "Name: " + drive.Name;
                    type.Text = "Type: " + drive.DriveType;
                    created.Text = "Drive is not Ready";
                    modified.Text = "";
                    OpenUnknoun.Visibility = Visibility.Collapsed;
                }
                Accessed.Visibility = Visibility.Collapsed;
            }
        }

        private void Zoom_In(object sender, RoutedEventArgs e)
        {
            //ImageViewer.
        }

        private void ComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {
            Encoding encode;
            Text_Encoding(out encode);
            StreamReader SR = new StreamReader(file.FullName, encode);
            TextViewer.Document = Document(SR.ReadToEnd(), file.Extension);
            SR.Close();
        }

        private void Text_Encoding(out Encoding encode)
        {
            if (TextEncoding.SelectedIndex == 0)
                encode = Encoding.ASCII;
            else if (TextEncoding.SelectedIndex == 1)
                encode = Encoding.BigEndianUnicode;
            else if (TextEncoding.SelectedIndex == 2)
                encode = Encoding.Unicode;
            else if (TextEncoding.SelectedIndex == 3)
                encode = Encoding.UTF32;
            else if (TextEncoding.SelectedIndex == 4)
                encode = Encoding.UTF7;
            else
                encode = Encoding.UTF8;
        }

        private void Save_Text(object sender, RoutedEventArgs e)
        {
            Encoding encode;
            Text_Encoding(out encode);
            StreamWriter SW = new StreamWriter(file.FullName, false, encode);
            SW.Write(TextViewer.Document);
            SW.Close();
        }

        private void Text_Editing(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("Tools\\notepad++.exe", file.FullName)); ;
        }

        private void Mute_Media(object sender, RoutedEventArgs e)
        {
            MediaViewer.IsMuted = !MediaViewer.IsMuted;
        }

        private void Video_Scroll(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaViewer.Position = new TimeSpan((long)(MediaSlider.Value / 1000000));
        }

        private void Pause_Media(object sender, RoutedEventArgs e)
        {
            if (isOn)
                MediaViewer.Pause();
            else
                MediaViewer.Play();
            isOn = !isOn;
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            if (ftype == true)
            {
                if (MainWindow.IsArchive(file.Extension))
                {
                    ProcessStartInfo PSI = new ProcessStartInfo("Tools\\7zFM.exe", "\"" + file.FullName + "\"");
                    System.Diagnostics.Process.Start(PSI);
                }
                else
                {
                    ProcessStartInfo PSI = new ProcessStartInfo(file.FullName);
                    System.Diagnostics.Process.Start(PSI);
                }
            }
            else if (ftype == false)
            {
                MainWindow.go_to_path = true;
                MainWindow.to_go_path = directory.FullName;
            }
            else if (ftype == null)
            {
                MainWindow.go_to_path = true;
                MainWindow.to_go_path = drive.Name;
            }
            this.Close();
        }

        private string Archive_Type(string Extintion)
        {
            if (file.Extension.Length > 0 && file.Extension[file.Extension.Length - 1] == '_')
                return "Cabinet Archive";
            switch (Extintion.ToLower())
            {
                case ".rar":
                    return "WinRar Archive";
                case ".7z":
                    return "7zip Archive";
                case ".ace":
                    return "WinAce Archive";
                case ".mou":
                    return "WinMount Archive";
                case ".cab":
                    return "Cabinet Archive";
                default:
                    return "(" + Extintion.ToUpper() + ") Archive";
            }
        }

        private void Extract(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog FBD = new System.Windows.Forms.FolderBrowserDialog();
            if (FBD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ProcessStartInfo PSI = new ProcessStartInfo("Tools\\7z.exe", " x \"" + file.FullName + "\" -o\"" + FBD.SelectedPath + "\"");
                System.Diagnostics.Process.Start(PSI);
            }
        }

        private void Window_KeyUp_1(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Escape))
            {
                MediaViewer.Source = null;
                WebPageViewer.Source = null;
                this.Close();
            }
        }

        private FlowDocument Document(string document, string extension)
        {
            //char[] separator = { ';' };
            FlowDocument doc = new FlowDocument();
            //if (extension.Equals(".cs"))
            //{
            //    var instructions = document.Split(';');
            //    for (int i = 0; i < instructions.Length; i++)
            //    {
            //        var words = instructions[i].Split(' ');
            //        for (int j = 0; j < words.Length; j++)
            //        {
            //            Run block = new Run(words[j]);
            //            if (Contains(words[j], blue))
            //                block.Foreground = Brushes.Blue;
            //            else if (Contains(words[j], cyan))
            //                block.Foreground = Brushes.Cyan;
            //            Paragraph par = new Paragraph(block);
            //            doc.Blocks.Add(par);
            //            //int i1 = words[i].IndexOf("\"");
            //            //int i2 = words[i].IndexOf("\"", words[i].IndexOf("\""));
            //        }
            //    }
            //}
            //else
            //{
            text_ = document;
            System.Windows.Documents.Run block = new System.Windows.Documents.Run(document);
            System.Windows.Documents.Paragraph par = new System.Windows.Documents.Paragraph(block);
            doc.Blocks.Add(par);
            //}
            return doc;
        }

        private bool Contains(string value, string[] In)
        {
            foreach (var i in In)
                if (i.Equals(value))
                    return true;
            return false;
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            if (text_ != null)
            {
                if (Search_Text.Text.Equals(""))
                    Search_Text.Background = Brushes.White;
                else if (text_.Contains(Search_Text.Text))
                {
                    Search_Text.Background = Brushes.White;
                    //while (text_.Contains(Search_Text.Text))
                    //{

                    //}
                }
                else
                {
                    Search_Text.Background = Brushes.Red;
                }
            }
        }

        private void SlidTraker()
        {
            while (isOn)
                Sett(MediaViewer.Position.Ticks);
        }

        private void Sett(long n)
        {
            MediaSlider.Value = n;
        }

        private void v(object sender, MouseButtonEventArgs e)
        {
            MediaViewer.Position = new TimeSpan((long)MediaSlider.Value * 1000000);
        }

        private void MediaViewer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Error.Text = "Can't open this media file, make sure that you install all codec required and the file is a correct media file.";
            MediaControl.Visibility = Visibility.Collapsed;
        }

        private void Rotate_Right(object sender, RoutedEventArgs e)
        {
            RotateTransform m_transform;
            m_userRotation = Add90DegreesCW(m_userRotation);
            UpdateImageRotation(m_userRotation, out m_transform);
            //ImageViewer.
        }

        private void UpdateImageRotation(PhotoOrientation rotation, out RotateTransform m_transform)
        {
            m_transform = new RotateTransform();
            switch (rotation)
            {
                case PhotoOrientation.Rotate270:
                    m_transform.Angle = 90;
                    break;
                case PhotoOrientation.Rotate180:
                    m_transform.Angle = 180;
                    break;
                case PhotoOrientation.Rotate90:
                    m_transform.Angle = 270;
                    break;
                case PhotoOrientation.Normal:
                default:
                    m_transform.Angle = 0;
                    break;
            }
        }

        public static PhotoOrientation Add90DegreesCW(PhotoOrientation input)
        {
            switch (input)
            {
                case PhotoOrientation.Normal:
                    return PhotoOrientation.Rotate270;
                case PhotoOrientation.Rotate90:
                    return PhotoOrientation.Normal;
                case PhotoOrientation.Rotate180:
                    return PhotoOrientation.Rotate90;
                case PhotoOrientation.Rotate270:
                    return PhotoOrientation.Rotate180;
                default:
                    return PhotoOrientation.Unspecified;
            }
        }
    }
}


public enum PhotoOrientation
{
    Unspecified = 0,
    Normal = 1,
    FlipHorizontal = 2,
    Rotate180 = 3,
    FlipVertical = 4,
    Transpose = 5,
    Rotate270 = 6,
    Transverse = 7,
    Rotate90 = 8,
}