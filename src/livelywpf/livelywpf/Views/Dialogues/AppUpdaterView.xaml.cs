using ModernWpf.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for AppUpdaterView.xaml
    /// </summary>
    public partial class AppUpdaterView : Window
    {
        private DownloadHelper download;
        private readonly Uri fileUrl;
        private bool _forceClose = false;
        private bool downloadComplete = false;
        private string fileName;
        string savePath = "";

        public AppUpdaterView(Uri fileUrl, string changelogText)
        {
            InitializeComponent();
            if(fileUrl != null)
            {
                this.fileName = fileUrl.Segments.Last();
                this.fileUrl = fileUrl;
                changelog.Document.Blocks.Add(new Paragraph(new Run(changelogText)));
            }
            else
            {
                downloadBtn.IsEnabled = false;
                this.Title = Properties.Resources.TextupdateCheckFail;
                changelog.Document.Blocks.Add(new Paragraph(new Run(Properties.Resources.LivelyExceptionAppUpdateFail)));
            }
        }

        private void UpdateDownload_DownloadProgressChanged(object sender, DownloadEventArgs e)
        {
            progressBar.Value = e.Percentage;
            sizeTxt.Text = e.DownloadedSize + "/" + e.TotalSize + " MB";
        }

        private void UpdateDownload_DownloadFileCompleted(object sender, bool success)
        {
            if(success)
            {
                //success
                downloadComplete = true;
                downloadBtn.IsEnabled = true;
                downloadBtn.Content = Properties.Resources.TextInstall;
            }
            else
            {
                MessageBox.Show(Properties.Resources.LivelyExceptionAppUpdateFail, Properties.Resources.TextError);
                _forceClose = true;
                this.Close();
            }
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
                catch(Exception ex)
                {
                    MessageBox.Show(Properties.Resources.LivelyExceptionAppUpdateFail, Properties.Resources.TextError);
                }
            }
            else
            {
                var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = "Select location to save the file",
                    Filter = "Executable|*.exe",
                    FileName = fileName,
                };
                if (saveFileDialog1.ShowDialog() == true)
                {
                    savePath = saveFileDialog1.FileName;
                }
                if (String.IsNullOrEmpty(savePath))
                {
                    return;
                }

                try
                {
                    download = new DownloadHelper();
                    download.DownloadFile(fileUrl, savePath);
                    download.DownloadFileCompleted += UpdateDownload_DownloadFileCompleted;
                    download.DownloadProgressChanged += UpdateDownload_DownloadProgressChanged;
                }
                catch
                {
                    MessageBox.Show(Properties.Resources.LivelyExceptionAppUpdateFail, Properties.Resources.TextError);
                    _forceClose = true;
                    this.Close();
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
                    download.Dispose();
                }
            }
        }
    }
}
