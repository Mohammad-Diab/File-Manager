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

namespace File_Operations
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
    }

    public class OperationTree
    {
        public OperationTree parent;
        public List<OperationTree> children;
        public FileSystemInfo item;
        public long completed;
        public long size, files_count, directory_count;
        public OperationOption user_option;
        public string error_message;
        public object short_path;
        public OperationType operation;
        public bool is_cut;

        bool done;
        public Boolean Done
        {
            get { return done; }
            set
            {
                if (value && parent != null)
                    parent.completed++;
                done = value;
            }
        }

        public OperationTree(FileInfo file, bool is_cut, object short_path, OperationType operation, OperationTree parent, string error)
        {
            this.parent = parent;
            //if (parent != null)
            //    parent.size += file.Length;
            item = file;
            completed = 0;
            done = false;
            if (file != null)
                if (file.Exists)
                    size = file.Length;
            this.operation = operation;
            this.short_path = short_path;
            this.is_cut = is_cut;
            files_count = 1;
            directory_count = 0;
            user_option = OperationOption.NoValue;
            error_message = error;
        }

        public OperationTree(DirectoryInfo directory, bool is_cut, object short_path, OperationType operation, OperationTree parent, bool get_files, string error)
        {
            this.operation = operation;
            this.parent = parent;
            item = directory;
            completed = files_count = 0;
            done = false;
            this.short_path = short_path;
            this.is_cut = is_cut;
            user_option = OperationOption.NoValue;
            children = new List<OperationTree>();
            directory_count = 1;
            if (get_files)
                GetSuns(directory, this);
            foreach (var i in children)
            {
                files_count += i.files_count;
                directory_count += i.directory_count;
            }
            error_message = error;
        }

        void GetSuns(DirectoryInfo directory, OperationTree operationTree)
        {
            try
            {
                var dirs = directory.GetDirectories();
                foreach (var i in dirs)
                {
                    OperationTree sun = new OperationTree(i, is_cut, short_path + "\\" + i.Name, operation, operationTree, true, "");
                    children.Add(sun);
                    size += sun.size;
                }
            }
            catch (Exception) { }
            try
            {
                var files = directory.GetFiles();
                foreach (var i in files)
                    try
                    { children.Add(new OperationTree(i, is_cut, short_path + "\\" + i.Name, operation, operationTree, "")); size += i.Length; }
                    catch (Exception) { }
            }
            catch (Exception) { }
        }

        internal bool Contains(OperationTree operationTree, out int index)
        {
            for (int i = 0; i < children.Count; i++)
                if (children[i].item == operationTree.item)
                {
                    index = i;
                    return true;
                }
            index = -1;
            return false;
        }
    }

    public enum OperationOption
    {
        NoValue = 0,
        Over = 1,
        Skip = 2,
        CopyWithNewName = 3,
        Retry = 4
    }

    public enum OperationType
    {
        Copy = 0,
        Attributes = 1,
        Delete = 2,
        Rename = 3,
        Destroy = 4,
        DiskDestroy = 5,
        Encrypt = 6,
        Decrypt = 7,
        Compress = 8,
        Extract = 9,
        CreateShortcut = 10,
        ConvertISZ_ToISO = 11,
        ChangeFileDate = 12
    }

}
