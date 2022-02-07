using Lively.Common;
using Lively.Common.Helpers.Network;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace Lively.Views
{
    /// <summary>
    /// Interaction logic for AppUpdater.xaml
    /// </summary>
    public partial class AppUpdater : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IDownloadHelper download;
        private readonly Uri fileUrl;
        private bool _forceClose = false;
        private bool downloadComplete = false;
        private readonly string suggestedFileName;
        private string savePath = string.Empty;


        public AppUpdater(Uri fileUri, string changelogText)
        {
            InitializeComponent();
            if (fileUri != null)
            {
                this.suggestedFileName = fileUri.Segments.Last();
                this.fileUrl = fileUri;
                changelog.Document.Blocks.Add(new Paragraph(new Run(changelogText)));
            }
            else
            {
                downloadBtn.IsEnabled = false;
                this.Title = "Fail";
                changelog.Document.Blocks.Add(new Paragraph(new Run("Error, Download from website instead: https://github.com/rocksdanister/lively/releases")));
            }
        }

        private void Download_DownloadStarted(object sender, DownloadEventArgs e)
        {
            _ = this.Dispatcher.BeginInvoke(new Action(() => {
                totalSizeTxt.Text = "/" + e.TotalSize + " MB";
            }));
        }

        private void UpdateDownload_DownloadProgressChanged(object sender, DownloadProgressEventArgs e)
        {
            _ = this.Dispatcher.BeginInvoke(new Action(() => {
                progressBar.Value = e.Percentage;
                taskbarItemInfo.ProgressValue = e.Percentage / 100f;
                sizeTxt.Text = e.DownloadedSize.ToString();
            }));
        }

        private void UpdateDownload_DownloadFileCompleted(object sender, bool success)
        {
            _ = this.Dispatcher.BeginInvoke(new Action(() => {
                if (success)
                {
                    downloadComplete = true;
                    downloadBtn.IsEnabled = true;
                    downloadBtn.Content = "Install";
                    taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                }
                else
                {
                    taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                    changelog.Document.Blocks.Clear();
                    changelog.Document.Blocks.Add(new Paragraph(new Run("Error, Download from website instead: https://github.com/rocksdanister/lively/releases")));
                    _forceClose = true;
                }
            }));
        }

        private void Download_Button_Click(object sender, RoutedEventArgs e)
        {
            downloadBtn.IsEnabled = false;
            if (downloadComplete)
            {
                try
                {
                    _forceClose = true;
                    //run setup in silent mode.
                    Process.Start(savePath, "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS");
                    //inno installer will auto retry, waiting for application exit.
                    App.ShutDown();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show("Error, Download from website instead: https://github.com/rocksdanister/lively/releases", "Error");
                }
            }
            else
            {
                /*
                var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = "Select location to save the file",
                    Filter = "Executable|*.exe",
                    FileName = suggestedFileName,
                    InitialDirectory = Path.Combine(Program.AppDataDir, "temp"),
                };
                if (saveFileDialog1.ShowDialog() == true)
                {
                    savePath = saveFileDialog1.FileName;
                }
                if (String.IsNullOrEmpty(savePath))
                {
                    return;
                }
                */
                try
                {
                    download = App.Services.GetRequiredService<IDownloadHelper>();
                    savePath = Path.Combine(Constants.CommonPaths.TempDir, suggestedFileName);
                    download.DownloadFile(fileUrl, savePath);
                    download.DownloadFileCompleted += UpdateDownload_DownloadFileCompleted;
                    download.DownloadProgressChanged += UpdateDownload_DownloadProgressChanged;
                    download.DownloadStarted += Download_DownloadStarted;
                    taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                    changelog.Document.Blocks.Clear();
                    changelog.Document.Blocks.Add(new Paragraph(new Run("Error, Download from website instead: https://github.com/rocksdanister/lively/releases")));
                    _forceClose = true;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_forceClose != true && download != null)
            {
                if (MessageBox.Show("Cancel update?", "Please wait", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _forceClose = true;
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                if (download != null)
                {
                    download.DownloadFileCompleted -= UpdateDownload_DownloadFileCompleted;
                    download.DownloadProgressChanged -= UpdateDownload_DownloadProgressChanged;
                    download.Cancel();
                }
            }
        }
    }
}
