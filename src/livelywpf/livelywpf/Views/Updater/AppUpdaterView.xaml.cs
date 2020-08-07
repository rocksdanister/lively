using ModernWpf.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Documents;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for AppUpdaterView.xaml
    /// </summary>
    public partial class AppUpdaterView : Window
    {
        private readonly DownloadHelper download = new DownloadHelper();
        private readonly Uri fileUrl;
        private bool _forceClose = false;
        private bool downloadComplete = false;
        string savePath = "";

        public AppUpdaterView(Uri fileUrl, string changelogText)
        {
            InitializeComponent();
            this.fileUrl = fileUrl;
            changelog.Document.Blocks.Add(new Paragraph(new Run(changelogText)));
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
                //run setup
            }
            else
            {
                var saveFileDialog1 = new Microsoft.Win32.SaveFileDialog()
                {
                    Title = "Select location to save the file",
                    Filter = "Executable|*.exe",
                    FileName = "update.exe",
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
            if (_forceClose != true)
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
                download.DownloadFileCompleted -= UpdateDownload_DownloadFileCompleted;
                download.DownloadProgressChanged -= UpdateDownload_DownloadProgressChanged;
                download.Dispose();
            }
        }
    }
}
