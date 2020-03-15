using livelywpf.Lively.Helpers;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using Path = System.IO.Path;
using Octokit;

namespace livelywpf.Dialogues
{
    /// <summary>
    /// Interaction logic for AppUpdate.xaml
    /// </summary>
    public partial class AppUpdate : MetroWindow
    {
        //todo: rewrite with data binding.
        private Release release;
        private string filePath, url;
        private bool _downloadStarted = false;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public AppUpdate(Release release, string fileUrl)//string version, string url, string bodyText)
        {
            this.release = release;
            this.url = fileUrl;
            this.filePath = Path.Combine(App.PathData, "tmpdata", "update.exe");
            InitializeComponent();
            
            try
            {
                if(release == null)
                {
                    this.Title = Properties.Resources.txtContextMenuUpdate5;
                    richTxtBoxGitBody.Document.Blocks.Add(new Paragraph(new Run(Properties.Resources.txtLivelyErrorMsgTitle
                                                                       + "\n\n" + Properties.Resources.txtUpdateDownloadErrorMsg + "\nwww.github.com/rocksdanister/lively")));
                    return;
                }

                if (String.IsNullOrWhiteSpace(SaveData.config.IgnoreUpdateTag))
                {
                    chkMarkIgnore.IsChecked = false;
                }
                else if (!release.TagName.Equals(SaveData.config.IgnoreUpdateTag, StringComparison.Ordinal))
                {
                    chkMarkIgnore.IsChecked = false;
                }
                else
                {
                    chkMarkIgnore.IsChecked = true;
                }
                chkMarkIgnore.Checked += ChkMarkIgnore_Checked;
                chkMarkIgnore.Unchecked += ChkMarkIgnore_Checked;

                int result = UpdaterGit.CompareAssemblyVersion(release);

                if (result > 0) //github ver greater, update available!
                {
                    btnInstall.IsEnabled = true;
                    chkMarkIgnore.IsEnabled = true;
                    StringBuilder sb = new StringBuilder(release.Body);
                    //todo: rewrite this in a efficient manner.
                    //formatting git text.
                    sb.Replace("#", "").Replace("\t", "  ");

                    if (App.isPortableBuild)
                    {
                        //custom msg maybe?
                    }
                    else
                    {

                    }
                    richTxtBoxGitBody.Document.Blocks.Add(new Paragraph(new Run(sb.ToString())));
                }
                else if (result < 0) //this is early access software.
                {
                    this.Title = Properties.Resources.txtContextMenuUpdate3;
                    richTxtBoxGitBody.Document.Blocks.Add(new Paragraph(new Run(Properties.Resources.txtContextMenuUpdate3)));
                }
                else //up-to-date
                {
                    this.Title = Properties.Resources.txtContextMenuUpdate4;
                    richTxtBoxGitBody.Document.Blocks.Add(new Paragraph(new Run(Properties.Resources.txtContextMenuUpdate4)));
                }
                chkMarkIgnore.Content = Properties.Resources.txtIgnore +" " + release.TagName;
            }
            catch(Exception e)
            {
                Logger.Error(e.ToString());
                richTxtBoxGitBody.Document.Blocks.Add(new Paragraph(new Run(Properties.Resources.txtLivelyErrorMsgTitle + " " + e.Message
                                                                          + "\n\n" + Properties.Resources.txtUpdateDownloadErrorMsg + "\nwww.github.com/rocksdanister/lively")));
                btnInstall.IsEnabled = false;
                chkMarkIgnore.IsEnabled = false;
                this.Title = Properties.Resources.txtContextMenuUpdate5;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //crash otherwise, if this.close() is called before window is shown.
            //DownloadFile(url, filePath);
        }

        WebClient client = new WebClient();
        private void DownloadFile(string url, string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch {
                //downloader replaces eitherways.
            }

            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            try
            {
                client.DownloadFileAsync(new Uri(url), filePath);
                _downloadStarted = true;
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                MessageBox.Show(Properties.Resources.txtLivelyErrorMsgTitle + " " + e.Message
                                + "\n\n"+ Properties.Resources.txtUpdateDownloadErrorMsg + "\nwww.github.com/rocksdanister/lively", 
                                Properties.Resources.txtLivelyErrorMsgTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = e.BytesReceived;
            double totalBytes = e.TotalBytesToReceive;
            double percentage = bytesIn / totalBytes * 100;
            progressBar1.Value = Math.Truncate(percentage);
            txtDownloadProgress.Text = Math.Truncate(ByteToMegabyte(bytesIn)) + "/" + Math.Truncate(ByteToMegabyte(totalBytes)) + "MB";
        }

        private void btnInstall_Click(object sender, RoutedEventArgs e)
        {
            if(App.isPortableBuild)
            {
                MessageBox.Show(Properties.Resources.txtMsgUpdaterPortableUnsupported,
                    Properties.Resources.txtLivelyErrorMsgTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                try
                {
                    Process.Start("https://github.com/rocksdanister/lively/releases");
                }
                catch { }
                return;
            }
            //btn is disabled, if its enabled again then user clicked again to install.
            if (_downloadStarted)
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        Process.Start(filePath, "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS");
                        //inno installer will auto retry, waiting for application exit.
                        App.W.ExitApplication();
                    }
                    catch(Exception ex)
                    {
                        Logger.Error(ex.ToString());
                        MessageBox.Show(Properties.Resources.txtLivelyErrorMsgTitle + " " + ex.Message
                               + "\n\n" + Properties.Resources.txtUpdateDownloadErrorMsg + "\nwww.github.com/rocksdanister/lively",
                               Properties.Resources.txtLivelyErrorMsgTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                DownloadFile(url, filePath);
                //txtDownload.Text = "Downloading..";
                btnInstall.IsEnabled = false;
                chkMarkIgnore.IsEnabled = false;
            }
        }

        void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                //txtDownload.Text = "Ready To Install";
                btnInstall.Content = Properties.Resources.txtInstall;
                btnInstall.IsEnabled = true;
            }
            else
            {
                try
                {
                    Task.Run(() => File.Delete(filePath));
                }
                catch { }

                Logger.Error("Update Download failed:" + e.Error);
                MessageBox.Show(Properties.Resources.txtLivelyErrorMsgTitle + " " + e.Error
                               + "\n\n" + Properties.Resources.txtUpdateDownloadErrorMsg + "\nwww.github.com/rocksdanister/lively",
                               Properties.Resources.txtLivelyErrorMsgTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        static double ByteToMegabyte(double bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                client.DownloadFileCompleted -= Client_DownloadFileCompleted;
                client.DownloadProgressChanged -= Client_DownloadProgressChanged;
                client.CancelAsync();
                client.Dispose();
            }
            catch (Exception e1)
            {
                Logger.Error("Disposing webclient:-" + e1.ToString());
            }
        }
        
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (MessageBox.Show(Properties.Resources.msgLoadExternalLink + "\n" + e.Uri.ToString(), Properties.Resources.msgLoadExternalLinkTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            }
            else
            {
                return;
            }
        }
       
        private void ChkMarkIgnore_Checked(object sender, RoutedEventArgs e)
        {
            if (release.TagName != null)
            {
                if (chkMarkIgnore.IsChecked == true)
                {
                    SaveData.config.IgnoreUpdateTag = release.TagName;
                }
                else
                {
                    SaveData.config.IgnoreUpdateTag = null;
                }
                SaveData.SaveConfig();
            }
        }

        private void progressBar1_Loaded(object sender, RoutedEventArgs e)
        {

        }

    }
}
