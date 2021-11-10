using livelywpf.Helpers.NetWork;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace livelywpf.Views.Dialogues
{
    /// <summary>
    /// Interaction logic for AppUpdaterView.xaml
    /// </summary>
    public partial class AppUpdaterView : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private IDownloadHelper download;
        private readonly Uri fileUrl;
        private bool _forceClose = false;
        private bool downloadComplete = false;
        private readonly string suggestedFileName;
        private string savePath = string.Empty;

        public AppUpdaterView(Uri fileUri, string changelogText)
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
                this.Title = Properties.Resources.TextupdateCheckFail;
                changelog.Document.Blocks.Add(new Paragraph(new Run(Properties.Resources.LivelyExceptionAppUpdateFail)));
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
                    downloadBtn.Content = Properties.Resources.TextInstall;
                    taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
                }
                else
                {
                    taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                    changelog.Document.Blocks.Clear();
                    changelog.Document.Blocks.Add(new Paragraph(new Run(Properties.Resources.LivelyExceptionAppUpdateFail)));
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
                    //run setup in silent mode.
                    Process.Start(savePath, "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS");
                    //inno installer will auto retry, waiting for application exit.
                    Program.ExitApplication();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.ToString());
                    MessageBox.Show(Properties.Resources.LivelyExceptionAppUpdateFail, Properties.Resources.TextError);
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
                    Logger.Error(ex.ToString());
                    taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                    changelog.Document.Blocks.Clear();
                    changelog.Document.Blocks.Add(new Paragraph(new Run(Properties.Resources.LivelyExceptionAppUpdateFail)));
                    _forceClose = true;
                }
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_forceClose != true && download != null)
            {
                e.Cancel = true;
                ContentDialog cancelDownload = new ContentDialog
                {
                    Title = Properties.Resources.TitlePleaseWait,
                    Content = Properties.Resources.DescriptionCancelQuestion,
                    PrimaryButtonText = Properties.Resources.TextYes,
                    SecondaryButtonText = Properties.Resources.TextNo,
                };
                ContentDialogResult result = await cancelDownload.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    _forceClose = true;
                    this.Close();
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