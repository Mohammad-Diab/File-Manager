using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
    /// Interaction logic for Make_File_Structure.xaml
    /// </summary>
    public partial class Make_File_Structure : Window
    {
        List<BasicFileInfoCombonents> info;
        public Make_File_Structure()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            info = new List<BasicFileInfoCombonents>();
            DirectoryInfo dir = new DirectoryInfo(path.Text);
            FileInfo[] files = dir.GetFiles();
            StreamReader[] streams = new StreamReader[9];
            for (int i = 0; i < 9; i++)
                streams[i] = new StreamReader(files[i].Open(FileMode.Open));
            for (int i = 0; i < 319; i++)
            {
                string Cate = streams[0].ReadLine();
                string Desc = streams[1].ReadLine();
                string Ext = streams[2].ReadLine();
                string File = streams[3].ReadLine();
                string HasSub = streams[4].ReadLine();
                List<string> HxI = new List<string>();
                string tmp = streams[5].ReadLine();
                if (tmp[0] == '\"')
                {
                    tmp = tmp.Substring(1);
                    HxI.Add(tmp);
                    tmp = streams[5].ReadLine();
                    HxI.Add(tmp);
                }
                else
                    HxI.Add(tmp);
                string Maydan = streams[6].ReadLine();
                string Offset = streams[7].ReadLine();
                string Subtext = streams[8].ReadLine();
                FileCategory categ = FileCategory.Unknown;
                switch (Cate)
                {
                    case "FileCategory.Unknown":
                        categ = FileCategory.Unknown;
                        break;
                    case "FileCategory.Application":
                        categ = FileCategory.Application;
                        break;
                    case "FileCategory.Archive":
                        categ = FileCategory.Archive;
                        break;
                    case "FileCategory.Audio":
                        categ = FileCategory.Audio;
                        break;
                    case "FileCategory.CannotOpen":
                        categ = FileCategory.CannotOpen;
                        break;
                    case "FileCategory.Directory":
                        categ = FileCategory.Directory;
                        break;
                    case "FileCategory.DiskImage":
                        categ = FileCategory.DiskImage;
                        break;
                    case "FileCategory.Document":
                        categ = FileCategory.Document;
                        break;
                    case "FileCategory.Font":
                        categ = FileCategory.Font;
                        break;
                    case "FileCategory.GameFile":
                        categ = FileCategory.GameFile;
                        break;
                    case "FileCategory.Html":
                        categ = FileCategory.Html;
                        break;
                    case "FileCategory.Image":
                        categ = FileCategory.Image;
                        break;
                    case "FileCategory.Text":
                        categ = FileCategory.Text;
                        break;
                    case "FileCategory.TextCanRunAsApplication":
                        categ = FileCategory.TextCanRunAsApplication;
                        break;
                    case "FileCategory.Vedio":
                        categ = FileCategory.Vedio;
                        break;
                    default:
                        break;
                }
                BasicFileInfoCombonents FIC;
                if (HxI[0][0].Equals('-'))
                    FIC = new BasicFileInfoCombonents(Ext, File, Desc, HasSub[0] == 'T', Subtext, categ, Maydan[0] == 'T');
                else
                    FIC = new BasicFileInfoCombonents(Ext, File, Desc, HasSub[0] == 'T', Subtext, categ, Maydan[0] == 'T', HxI, Offset);
                list.Items.Add(Ext);
                info.Add(FIC);
            }
        }

        private void list_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list.SelectedIndex > -1)
            {
                ext.Text = info[list.SelectedIndex].Extention;
                des.Text = info[list.SelectedIndex].Description;
                cat.Text = info[list.SelectedIndex].Category.ToString();
                has_sub.Text = info[list.SelectedIndex].HasSubtext.ToString();
                sub.Text = info[list.SelectedIndex].Subtext;
                maydan.Text = info[list.SelectedIndex].MayDanger.ToString();
                offset.Text = info[list.SelectedIndex].Offset.ToString();
                hix.Text = String.Empty;
                foreach (var i in info[list.SelectedIndex].IDs)
                {
                    foreach (var j in i)
                        hix.Text += j + " ";
                    hix.Text += "\n";
                }
                //i256.Source = info[list.SelectedIndex].Bitmap256;
                //i64.Source = info[list.SelectedIndex].Bitmap64;
                //i24.Source = info[list.SelectedIndex].Bitmap24;
                //i16.Source = info[list.SelectedIndex].Bitmap16;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            BinaryFormatter BF = new BinaryFormatter();
            BF.Serialize(File.Open(TargetFile.Text, FileMode.OpenOrCreate), info);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            list.Items.Clear();
            info = new List<BasicFileInfoCombonents>();
            BinaryFormatter BF = new BinaryFormatter();
            info = (List<BasicFileInfoCombonents>)BF.Deserialize(File.Open(TargetFile.Text, FileMode.OpenOrCreate));
            foreach (var i in info)
                list.Items.Add(i.Extention);
        }
    }
}