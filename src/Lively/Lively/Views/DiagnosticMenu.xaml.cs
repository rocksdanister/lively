using Lively.Common;
using Lively.Common.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace Lively.Views
{
    /// <summary>
    /// Interaction logic for DiagnosticMenu.xaml
    /// </summary>
    public partial class DiagnosticMenu : Window
    {
        private DebugLog debugLogWindow;

        public DiagnosticMenu()
        {
            InitializeComponent();
        }

        private void Generate_Report_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                DefaultExt = ".zip",
                Filter = "Compressed archive (.zip)|*.zip",
                FileName = "lively_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    LogUtil.ExtractLogFiles(saveDlg.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to generate log report:\n{ex.Message}", "Lively Wallpaper");
                }
            }
        }

        private void Open_Debug_View_Click(object sender, RoutedEventArgs e)
        {
            if (debugLogWindow is null)
            {
                debugLogWindow = new DebugLog();
                debugLogWindow.Closed += (s, e) => debugLogWindow = null;
                debugLogWindow.Show();
            }
        }

        private void Get_Help_Click(object sender, RoutedEventArgs e) =>
            LinkHandler.OpenBrowser("https://github.com/rocksdanister/lively/wiki/Common-Problems");
    }
}
