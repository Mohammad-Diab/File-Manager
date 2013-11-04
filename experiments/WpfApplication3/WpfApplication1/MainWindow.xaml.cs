using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ContextMenu directory_context_menu, file_context_menu;
        BackgroundWorker b1, b2;
        public static List<FileElement> to_copy_files;
        static public bool go_to_path = false;
        public static string to_go_path = "";
        bool hiddenfiles = false, show_image = true, fake = false;
        public static bool is_paste = false;
        Stack<string> back, front;
        string directory_address = "Computer", to_select_file = null;
        public static string StandardImageSource = "pack://application:,,,/File Explorer;component/Images/Icons/", systemDrive = "";
        static string computer_Name, operating_System, CPU = "!", Memory_Size;
        Thickness name_thickness = new Thickness(80, 5, 0, 0), size_thickness = new Thickness(80, 25, 0, 0), type_thickness = new Thickness(80, 45, 0, 0), folder_thickness = new Thickness(80, 25, 0, 0);

        string path
        {
            get { return directory_address; }
            set
            {
                Up.IsEnabled = !(value == "Computer");
                directory_address = value;
            }
        }

        public MainWindow()
        {
            Initializer();
        }

        public MainWindow(string PrimePath)
        {
            Initializer();
            path = PrimePath;
        }

        public MainWindow(string prim_path, string to_select_file)
        {
            Initializer();
            path = prim_path;
            this.to_select_file = to_select_file;
        }

        private void Initializer()
        {
            InitializeComponent();
            if (Process.GetCurrentProcess().StartInfo.Verb == "runas")
                Title = Title + " (Administrator)";
            back = new Stack<string>();
            front = new Stack<string>();
            if (FileInformation.file_bitmap.Count == 0)
                foreach (var i in FileInformation.file_image)
                    FileInformation.file_bitmap.Add(new BitmapImage(new Uri(StandardImageSource + i)));
        }

        private void Close(object sender, RoutedEventArgs e)
        {

        }

        private void InverseSelection_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ODS_Click(object sender, RoutedEventArgs e)
        {

        }

        private void XLSX_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ODP_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PPTX_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ODT_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DOCX_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Bitmap_Click(object sender, RoutedEventArgs e)
        {

        }

        private void HTML_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PlanText_Click(object sender, RoutedEventArgs e)
        {

        }

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EmptyDirectoriesRemover_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BigFileFinder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DublicateFileRemover_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SuperFilesRenamer_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SystemMonitor_Click(object sender, RoutedEventArgs e)
        {

        }

        private void FileCompresser_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ShowHiddenFiles_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Refresh(object sender, RoutedEventArgs e)
        {

        }

        private void HH(object sender, RoutedEventArgs e)
        {

        }

        private void ShowImage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Quit(object sender, RoutedEventArgs e)
        {

        }

        private void NewWindowAdmin_Click(object sender, RoutedEventArgs e)
        {

        }

        private void New_Window(object sender, RoutedEventArgs e)
        {

        }

        private void Address_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void Explorer_PreviewKeyUp(object sender, KeyEventArgs e)
        {

        }

        private void Address_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Explorer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Explorer_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void Go_UP(object sender, RoutedEventArgs e)
        {

        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {

        }

        private void GoFront_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Surfing_Back(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
