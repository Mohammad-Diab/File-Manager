using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    /// Interaction logic for AboutTool.xaml
    /// </summary>
    public partial class AboutTool : Window
    {
        public AboutTool()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ToolAbout info = ToolAbout.Find(tool_name.Text);
            if (string.IsNullOrWhiteSpace(tool_date.Text))
                tool_date.Visibility = Visibility.Collapsed;
            if (info == null)
            {
                Title = "About " + tool_name.Text;
                return;
            }
            tool_name.Text = info.Name;
            Title = "About " + info.Name;
            tool_title.Text = "About " + info.Name;
            if (string.IsNullOrWhiteSpace(tool_vertion.Text) || tool_vertion.Text == "Version")
                tool_vertion.Text = "Version: " + info.Version;
            BitmapSource image = ToolIcons.Load(info.Icon);
            if (image != null)
            {
                tool_image.Source = image;
                SafetyTools.SetWindowIcon(this, info.Icon);
            }
            else if (tool_image.Source != null)
                Icon = tool_image.Source;
            tool_description.Text = info.Description;
            tool_description.Visibility = Visibility.Visible;
            List<AboutLine> steps = new List<AboutLine>();
            for (int i = 0; i < info.HowTo.Length; i++)
                steps.Add(new AboutLine(i + 1 + ".", info.HowTo[i], false));
            how_to_list.ItemsSource = steps;
            how_to_header.Visibility = how_to_list.Visibility = steps.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            List<AboutLine> news = new List<AboutLine>();
            foreach (string i in info.WhatsNew)
                if (i.StartsWith("#"))
                    news.Add(new AboutLine("", i.Substring(1), true));
                else
                    news.Add(new AboutLine("•", i, false));
            whats_new_list.ItemsSource = news;
            whats_new_header.Visibility = whats_new_list.Visibility = news.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            tool_notes.Text = info.Notes;
            notes_header.Visibility = tool_notes.Visibility = string.IsNullOrEmpty(info.Notes) ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public class AboutLine
    {
        public AboutLine(string mark, string text, bool header)
        {
            Mark = mark;
            Text = text;
            Weight = header ? FontWeights.SemiBold : FontWeights.Normal;
            Color = header ? Brushes.CadetBlue : Brushes.Black;
            MarkWidth = new GridLength(header ? 0 : 18);
        }

        public string Mark { get; set; }
        public string Text { get; set; }
        public FontWeight Weight { get; set; }
        public Brush Color { get; set; }
        public GridLength MarkWidth { get; set; }
    }

    class ToolAbout
    {
        public string[] Keys;
        public string Name, Version, Description, Notes, Icon;
        public string[] HowTo, WhatsNew;

        static List<ToolAbout> tools;

        internal static ToolAbout Find(string name)
        {
            if (tools == null)
                tools = CreateTable();
            string key = (name ?? "").ToLower().Replace(" ", "");
            foreach (var i in tools)
                if (i.Keys.Any(x => key.Contains(x)))
                    return i;
            return null;
        }

        static List<ToolAbout> CreateTable()
        {
            List<ToolAbout> list = new List<ToolAbout>();
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "analy", "bigfile" },
                Name = "Directories Analyzer",
                Version = "3.2",
                Icon = "DirectoriesAnalyzer",
                Description = "Finds what fills a drive or a folder. It measures every folder and file below the folders you choose, shows folders and files sorted by size, lists all files in one list sorted by size, and adds up the space used by each kind of file (images, music, video, documents, applications, archives, fonts and other files).",
                HowTo = new string[] {
                    "Click the folder button and choose a folder or a drive, or open the tool from File Manager ('view details' in the details panel) for the selected folders. Add more folders the same way: each one becomes a root of the list.",
                    "BROWSING: double-click or Enter opens a folder (a file opens with its program), Backspace or the Up button goes back, Space shows a preview.",
                    "ALL FILES: every file of the analysed folders, the biggest first; double-click or Enter opens a file. STATISTICAL: size, share and a bar for each file type; click a type to list only its files in ALL FILES.",
                    "Right-click an item to open it, open it (or its folder) in File Manager, preview it, copy its path or delete it (Recycle Bin; hold Shift to delete permanently).",
                    "The save button exports the analysed folders, the statistics and all files as a CSV file (Excel opens it).",
                    "The settings button chooses whether hidden and system files are counted in the next analysis." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "Several folders can be analysed in one window (File Manager opens one window for all selected folders); analysing a folder again replaces the old result, a folder inside an analysed one is not counted twice.",
                    "Statistics are rebuilt from the analysed folders every time, so a type (for example video) is never shown as 0 by mistake, and they follow deletes. More video, image, audio and archive types are recognised.",
                    "Space shows a preview and Enter opens the item in both lists; folders open in File Manager.",
                    "Right-click menu in both lists: open, open in File Manager, preview, copy path, delete.",
                    "Delete uses the Recycle Bin (Shift = permanent) and updates the sizes, the lists and the statistics.",
                    "Bar for each type in STATISTICAL, biggest file and average size; click a type to list its files; export to CSV.",
                    "Folder links (junctions, symbolic links) are not followed: no double counting and no endless loops. Folders that could not be opened are counted and listed in an information bar, with 'Restart as administrator'.",
                    "Settings: count hidden and system files or not.",
                    "#3.2", "Statistics by file type.", "Fixed: some files of a folder were missing.",
                    "#3.1", "All files list sorted by size.", "More than one folder can be analysed at the same time.",
                    "#3", "New design: explore the folders like an explorer instead of a tree.", "Faster calculation.",
                    "#2 - 2.2", "Directory analyzer: folder sizes in a tree view, colours for each use, faster, bugs fixed.",
                    "#1 - 1.1", "Finds the 100 biggest files, then the 100 biggest folders, in a directory." },
                Notes = "Folders you are not allowed to open are skipped and not counted (the information bar says how many). Sizes are the file sizes, not the space used on the disk."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "extension", "extention", "renamer" },
                Name = "Super Extension Changer",
                Version = "2.1",
                Icon = "ExtensionChanger",
                Description = "Changes the extension of many files at once, and finds the right extension for files whose extension is missing or wrong by reading what is inside them.",
                HowTo = new string[] {
                    "EXT. CHANGER: add files or a whole folder (or drop them on the list) and type the new extension; the 'New name' column shows the result before anything is renamed.",
                    "Tick 'Remove old extension' to replace the extension; untick it to add the new extension after the old one.",
                    "Click Rename and confirm. The undo button gives the files of the last rename their old names back.",
                    "SUGGEST EXT.: add files and click 'Suggest file extension'. Every file shows the detected type, how sure the tool is (Sure, Likely, Unsure) and the suggested extension; pick another one from the list if you want.",
                    "Rename applies the suggestions. Files that already have a fitting extension keep their names; files the tool is not sure about are only renamed after you confirm them.",
                    "The settings button: add the files of sub-folders, add hidden files, rename only sure suggestions." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "Smarter detection: file signatures plus a look inside the file: ZIP based files (docx, xlsx, pptx, odt, ods, odp, epub, jar, apk, xpi, nupkg, msix...), Office 97-2003 files (doc, xls, ppt, msg, msi), RIFF (wav, avi, webp), the MP4 family (mp4, m4a, mov, heic, avif, 3gp), Matroska / WebM, tar inside gzip, programs (exe, dll, sys) and text files (txt, csv, json, xml, html, svg, srt, reg...).",
                    "Many more file types: webp, heic, avif, ico, tiff, psd, flac, ogg, opus, m4a, aac, mkv, webm, mov, 7z, rar, xz, zst, gz, bz2, tar, cab, iso, vhd, vhdx, qcow2, vdi, vmdk, pdf, rtf, epub, exe, dll, msi, lnk, ttf, otf, woff, woff2, sqlite and others.",
                    "The list shows the detected type and how sure it is; unsure files are never renamed without asking, and a fitting extension (for example .jpeg for a JPEG) is kept.",
                    "Preview of the new names before renaming, and undo of the last rename.",
                    "Settings work: sub-folders, hidden files, only sure suggestions.",
                    "Fixed: names like 'photo.jpg.jpg', files left locked after reading, the dot warning on every key.",
                    "Folder links are not followed when adding a folder.",
                    "#2.1", "Drag and drop files.",
                    "#2", "New interface.", "Faster.",
                    "#1.2", "Extension suggester.",
                    "#1.1", "Faster, bugs fixed.",
                    "#1", "Changes the extension of many files." },
                Notes = "Changing an extension does not convert the file. Files in protected places (Windows, Program Files, a user folder...) need a second confirmation."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "monitor" },
                Name = "System Monitor",
                Version = "1.0",
                Icon = "SystemMonitor",
                Description = "Shows the running processes with their icon, memory, CPU use and threads, and live graphs of CPU, memory, disk, network and GPU use for the last 60 seconds.",
                HowTo = new string[] {
                    "PROCESSES: click a column header to sort (again to reverse), drag a header edge to resize. The list refreshes every few seconds, or at once with Refresh; hover a name for the program path.",
                    "Select a process and click 'Ask to close' to close its window the normal way (the program can still ask you to save).",
                    "'End process' stops it at once after a warning; 'End process tree' also stops the processes it started.",
                    "PERFORMANCE: choose CPU, Memory, a disk, a network adapter or a GPU on the left to see its graph; 'Show each core' adds a small graph for every CPU core." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "The process list fills and refreshes without jumping to the top.",
                    "CPU use of every process from its real use since the last refresh (0 % when idle, '-' when Windows does not allow reading it).",
                    "The real icon of every program, sortable and resizable columns, tooltips with the path.",
                    "End process tree; clear warnings before ending a process.",
                    "New PERFORMANCE tab: CPU (total and per core), memory, every disk (read, write, active time), every network adapter (send, receive) and every GPU (busiest engine, dedicated memory).",
                    "Only one System Monitor window; updating stops while the window is minimised or the other tab is shown, and when it is closed." },
                Notes = "Some system processes do not allow reading their CPU time, path or icon. GPU graphs need Windows 10 (1709) or later."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "virtualmachine" },
                Name = "Virtual Machine",
                Version = "1.0",
                Icon = "VirtualMachine",
                Description = "Starts a virtual PC with QEMU from a hard disk image, a CD/DVD image (ISO) and/or a floppy image, to try an operating system or a boot disk without touching this PC.",
                HowTo = new string[] {
                    "Install QEMU for Windows (the tool shows the official download page when it is missing, then click 'Check again').",
                    "Choose a hard disk image, an ISO and/or a floppy image with '...', or create a new empty hard disk or floppy with the button next to it.",
                    "Choose the CPU, the cores, the memory (type it or use the slider), the device to boot from, the video card and the other options.",
                    "Click RUN. Press Ctrl+Alt+G in the QEMU window to free the mouse. STOP turns the virtual machine off at once." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "QEMU is found where it is installed; when it is missing RUN is disabled with the reason and the download page is offered.",
                    "The window no longer freezes while the virtual machine runs; STOP, and the QEMU error message when it fails.",
                    "CPU architecture from the installed QEMU emulators, cores, memory as text and slider (64 MB steps), hardware acceleration (WHPX) with software fallback.",
                    "Hard disk, CD/DVD and floppy at the same time, with the device to boot from; create a new hard disk or a blank 1.44 MB floppy.",
                    "Video card std / virtio / vmware / cirrus, full screen, network and sound on or off.",
                    "The last settings are remembered." },
                Notes = "STOP is like pulling the power cable: shut the system inside the virtual machine down first. Hardware acceleration needs the Windows feature 'Windows Hypervisor Platform'. Only PC machines (x86_64 and i386) are offered; other QEMU emulators need options this tool does not have."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "virtualhard", "vhd" },
                Name = "Create Virtual Hard Disk",
                Version = "1.0",
                Icon = "CreateVirtualHardDisk",
                Description = "Creates an empty virtual hard disk file (VHD, VHDX, QCOW2, VDI, VMDK or raw image) with QEMU's qemu-img, and attaches a VHD or VHDX file to Windows as a drive.",
                HowTo = new string[] {
                    "Choose where to save the disk with '...'.",
                    "Choose the format, the size and dynamically expanding or fixed size.",
                    "Click CREATE.",
                    "Attach shows a VHD/VHDX file as a drive in Windows; Detach removes it again (Windows asks for administrator rights)." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "Creating a disk really works (with qemu-img, no administrator rights) and the window stays responsive.",
                    "Formats VHD, VHDX, QCOW2, VDI, VMDK and raw image, dynamic or fixed size.",
                    "The disk is created under a temporary name and only then replaces an existing file; closing the window can stop it.",
                    "Attach and Detach work through Windows' diskpart." },
                Notes = "A new disk is empty: create a partition and format it inside the virtual machine, or in Disk Management after Attach."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "encrypt", "encrept" },
                Name = "Files Encrypter",
                Version = "3",
                Icon = "FilesEncrypter",
                Description = "Encrypts files with a password (AES-256) into .enf files, or many files into one .enp package, and decrypts them again.",
                HowTo = new string[] {
                    "ENCRYPT: add files, choose the options (one package, encrypt the names, what to do with the originals) and the output folder.",
                    "Type the password twice and click Encrypt.",
                    "DECRYPT: add .enf / .enp files, type the password and click Decrypt.",
                    "VIEW PACK CONTENT: open a package, type the password, see the files inside and extract only the ones you want." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "New format v4: AES-256 with an HMAC-SHA256 check, keys from the password with PBKDF2 (200,000 rounds).",
                    "Originals are only deleted or overwritten after every encrypted file was written and checked by decrypting it again; 'Securely delete the originals' hands exactly those files to File Destroyer.",
                    "Decrypting never overwrites a file, and a wrong password is reported instead of writing garbage.",
                    "Files of v0.9.0 and .fme files of v0.8.1 still open.",
                    "View package content works; right-click menus; big folders are added quickly.",
                    "#3", "New format with a password check value (the password is not stored in the file).",
                    "#2", "New interface.", "New encryption method.",
                    "#1.1", "Faster, better interface.",
                    "#1", "First release." },
                Notes = "Nobody can open the files without the password: there is no way to recover a forgotten password."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "destroy", "destoy" },
                Name = "File Destroyer",
                Version = "2.1",
                Icon = "FileDestroyer",
                Description = "Overwrites files before deleting them so they can not be recovered, and wipes the free space of a drive (Destroy Disk) so files deleted earlier can not be recovered either.",
                HowTo = new string[] {
                    "Add the files and folders to destroy.",
                    "Choose the method: Quick (zeros), Quick (ones), Quick (random) or Secure (3 passes, the last one checked), and how many repetitions.",
                    "Click Destroy and confirm: the list shows how many files, the method and the first paths.",
                    "DESTROY DISK: choose a drive and a method and start; File Manager fills the free space with overwrite files and deletes them at the end." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "One shared shredder for every method: the whole file and the rest of its last cluster are overwritten, with write-through; alternate data streams too; Secure reads its last pass back and checks it.",
                    "Every drive (the system drive too) asks first and shows a clear warning; the confirmation lists the number of files, the method and the first paths; the result lists failures.",
                    "Stop and Pause work inside a big file; a file that was not fully overwritten keeps its name.",
                    "Folder links are never followed, read-only, hidden and system files are handled.",
                    "Destroy Disk writes only into <drive>\\FileManager-Wipe, keeps 2 GB free on the system drive, always removes its files at the end (also when the window is closed) and cleans leftovers at the next start.",
                    "#2.1", "Fast (random) method.", "Bug fixed.",
                    "#2", "Better interface, faster, can pause.", "Fast (zeros and ones) method.",
                    "#1.2", "New interface.", "Destroyer for already deleted files.",
                    "#1.1", "Secure method.",
                    "#1", "Basic files destroyer." },
                Notes = "On SSDs, USB sticks and memory cards the drive itself may keep old copies of the data (wear levelling), and cloud folders keep server copies, so overwriting can not guarantee that everything is gone there; use the drive's secure erase or full-disk encryption (BitLocker) for those."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "duplicat", "dublicat", "identical" },
                Name = "Duplicate Files Remover",
                Version = "2.4",
                Icon = "IdenticalFileRemover",
                Description = "Finds files with exactly the same content (compared with SHA-256) in the folders you choose and helps you delete the extra copies.",
                HowTo = new string[] {
                    "Add the folders to search.",
                    "Start the search and wait for the groups of identical files (Stop keeps the groups found so far).",
                    "In every group the oldest copy stays unticked; tick the copies you want to delete.",
                    "Click Delete (Recycle Bin; hold Shift to delete permanently).",
                    "'Is similar to' compares two files." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "The same physical file reached through two paths (links, hard links, overlapping folders) is never offered as a duplicate.",
                    "The oldest copy is kept, then the shortest path; the last copy of a group can never be ticked; ticking, sizes and sorting work.",
                    "Exact comparison with SHA-256; files are closed after reading; unreadable files are counted; every copy is checked again before it is deleted.",
                    "Delete uses the Recycle Bin (Shift = permanent); right-click menus; 'Restart as administrator' when folders were skipped.",
                    "#2.4", "Bug fixed.",
                    "#2.3", "'Is similar to' tool.",
                    "#2.2", "Faster.",
                    "#2.1", "Resizable lists.",
                    "#2", "New design and interface.",
                    "#1.1", "Faster, bug fixed.",
                    "#1", "Finds duplicate files." },
                Notes = "Files you are not allowed to read are skipped and counted in the result. Empty (0 byte) files are not compared."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "empty" },
                Name = "Empty Folders Remover",
                Version = "2",
                Icon = "EmptyFolderRemover",
                Description = "Finds empty folders (also folders that contain only empty folders) and zero-byte files in one or more folders, and deletes the ones you choose.",
                HowTo = new string[] {
                    "Add one or more folders.",
                    "Start the search (Stop keeps the folders searched completely).",
                    "Check the results: zero-byte files are listed separately and are not ticked by 'Select all'.",
                    "Click Delete (Recycle Bin; hold Shift to delete permanently)." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "A folder that can not be opened is never called empty; an information bar says how many were skipped, with 'Restart as administrator'.",
                    "Zero-byte files are listed separately.",
                    "Folder links are skipped; several folders at once.",
                    "Every folder is checked again just before it is deleted; Recycle Bin delete; right-click menu.",
                    "#2", "Search in several folders and files at once.",
                    "#1.21", "Bugs fixed.",
                    "#1.2", "Faster, new interface.",
                    "#1.1", "Finds empty folders, folders with only empty folders, and empty files." },
                Notes = "Some folders (in Windows, Program Files or other users' folders) can only be read as administrator; without that they are skipped, never reported as empty."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "compress" },
                Name = "Compressed Files Manager",
                Version = "1.1.2.128",
                Icon = "CompressedFilesManager",
                Description = "Opens, creates, changes and extracts archives (7z, zip, tar, gzip, bzip2, xz and every format 7-Zip can read) with the 7-Zip engine.",
                HowTo = new string[] {
                    "Open an archive, browse it like a folder (double-click, Up, Root or type a path) and double-click a file to open it.",
                    "Select files and click Extract (nothing selected = everything); Test checks the archive.",
                    "Add files or folders (or drop them) into an open 7z, zip or tar archive; Rename (F2) and Remove (Del) work inside it.",
                    "To create an archive add files and folders to a project, then Compress: choose the format, the level, the password, split volumes and the other options." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "7z, zip, tar, gzip, bzip2 and xz work, with password (7z and zip AES-256), encrypted file names (7z) and split volumes.",
                    "Password-protected archives open; double-click extracts to a temporary folder and opens the file; search; properties; test.",
                    "Extracting asks what to do with existing files, refuses unsafe paths and can be stopped; damaged files and wrong passwords are reported.",
                    "Archives are written to a temporary file and only then replace the target; dates of files and folders are kept.",
                    "Projects are saved as text and re-read from the disk; keyboard shortcuts for every command.",
                    "#1.1.2.128", "Faster archive loading.", "Smaller and faster File Manager archive structure.", "Progress bar while loading an archive.", "Bugs fixed." },
                Notes = "Uses 7z.dll of 7-Zip, shipped with File Manager."
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "desktop" },
                Name = "Desktop Environment",
                Version = "1.0",
                Icon = "DesktopEnvironment",
                Description = "A full-screen start screen that File Manager shows when Windows Explorer is not running: drives, your folder and desktop as tiles, apps, games, documents and shortcuts to your libraries.",
                HowTo = new string[] {
                    "Use the bar on the left to switch between Home, Apps, Games and Documents; Pictures, Music and Videos open those folders in File Manager; the last button opens the Control Panel.",
                    "Double-click a tile to open it (folders and drives open in File Manager).",
                    "Type in the filter box to find an app, a game or a document; tick document types and choose the sort order on the right.",
                    "Add an app or a game (a shortcut is added to your Start menu), add all programs of a folder, or add a document location." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "Office and OpenDocument thumbnails without 7-Zip paths of another computer; files are no longer locked by the tiles.",
                    "Every launcher button works or is hidden; the add buttons, the document type filter and the sort order work.",
                    "Folders, drives and the FILE MANAGER tile open in File Manager; documents open with a double-click; documents show their size, type, date and folder." },
                Notes = ""
            });
            list.Add(new ToolAbout()
            {
                Keys = new string[] { "subtitle" },
                Name = "Subtitle Options",
                Version = "2.0",
                Icon = null,
                Description = "Changes how subtitles look in File Manager's video preview: font, size, colour, style, background, position, timing and text encoding.",
                HowTo = new string[] {
                    "FONT STYLE: choose the font, its size and colour, bold and italic, and the background colour and opacity.",
                    "SUBTITLE SETTING: move the text, shift the timing to match the video and choose the text encoding of the subtitle file." },
                WhatsNew = new string[] {
                    "#File Manager v1.0.0",
                    "The 'Default' button that did nothing is hidden; this About page works.",
                    "#2.0", "New dialog, faster.", "Background colour and opacity.", "Font and colour filters.", "The dialog can be resized.",
                    "#1.1", "Synchronise the subtitle.", "Text position.", "Subtitle encoding.",
                    "#1", "Subtitle font, colour and size, bold and italic." },
                Notes = ""
            });
            return list;
        }
    }

    static class ToolIcons
    {
        internal static BitmapSource Load(string name)
        {
            try
            {
                BitmapDecoder decoder = BitmapDecoder.Create(new Uri("pack://application:,,,/File Manager;component/Images/Tools Icon/Window/" + name + ".ico"), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                BitmapFrame best = null;
                foreach (var i in decoder.Frames)
                    if (best == null || i.PixelWidth > best.PixelWidth)
                        best = i;
                return best;
            }
            catch (Exception) { }
            return null;
        }
    }
}