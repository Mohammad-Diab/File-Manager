using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace File_Manager
{
    public partial class About_Panel : UserControl
    {
        public event EventHandler LinkClick;

        public About_Panel()
        {
            InitializeComponent();
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            app_version.Text = "Version " + version.ToString(3);
            UpdateLinks();
        }

        static string NoticesPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "THIRD-PARTY-NOTICES.txt"); }
        }

        static string LicensePath
        {
            get
            {
                foreach (string i in new string[] { "LICENSE.txt", "LICENSE" })
                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, i)))
                        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, i);
                return null;
            }
        }

        void UpdateLinks()
        {
            bool notices = File.Exists(NoticesPath);
            notices_link.IsEnabled = notices;
            notices_link.ToolTip = notices ? "Opens THIRD-PARTY-NOTICES.txt: the licences of the icons, sounds and libraries File Manager uses" : "THIRD-PARTY-NOTICES.txt was not found next to File Manager.exe";
            license_link.ToolTip = LicensePath != null ? "Opens the LICENSE file of File Manager (Apache License 2.0)" : "Opens the licence of File Manager (Apache License 2.0) on GitHub";
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
                UpdateLinks();
        }

        void OnLinkClick()
        {
            if (LinkClick != null)
                LinkClick(this, EventArgs.Empty);
        }

        private void Email_Click(object sender, RoutedEventArgs e)
        {
            OnLinkClick();
            Functions.OpenUrl("mailto:mohammad.diab93@outlook.com");
        }

        private void Repository_Click(object sender, RoutedEventArgs e)
        {
            OnLinkClick();
            Functions.OpenUrl(Functions.HelpLink);
        }

        private void License_Click(object sender, RoutedEventArgs e)
        {
            OnLinkClick();
            string license = LicensePath;
            if (license == null)
                Functions.OpenUrl(Functions.LicenseLink);
            else if (Path.GetExtension(license).ToLower() == ".txt")
                SafetyTools.PreviewItem(license);
            else
                try { Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "notepad.exe"), "\"" + license + "\"")); }
                catch (Exception) { Functions.OpenUrl(Functions.LicenseLink); }
        }

        private void Notices_Click(object sender, RoutedEventArgs e)
        {
            UpdateLinks();
            if (!notices_link.IsEnabled)
                return;
            OnLinkClick();
            SafetyTools.PreviewItem(NoticesPath);
        }

        private void ReleaseNotes_Click(object sender, RoutedEventArgs e)
        {
            OnLinkClick();
            Functions.OpenUrl(Functions.ReleasesLink);
        }
    }
}
