using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace livelyUpdater
{
    /// <summary>
    /// Incomplete
    /// work in progress, download & update's portable version of lively.
    /// </summary>
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DownloadFile("https://github.com/rocksdanister/lively/releases/download/v0.3.4.0/lively_lite_x86.zip", "update.zip");
        }

        private void DownloadFile(string url, string fileName)
        {
            WebClient client = new WebClient();
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Client_DownloadProgressChanged);
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(Client_DownloadFileCompleted);
            try
            {
                client.DownloadFileAsync(new Uri(url), fileName);
                //Task.Run(() => client.DownloadFileAsync(new Uri(url), "update.zip"));
            }
            catch (WebException e1)
            {
                MessageBox.Show("Failed to download update: " + e1.Message, "Error");
                Application.Exit();
            }
            catch(InvalidOperationException e2)
            {
                MessageBox.Show("Failed to download update: " + e2.Message, "Error");
                Application.Exit();
            }
            catch(ArgumentException e3)
            {
                MessageBox.Show("Failed to download update: " + e3.Message, "Error");
                Application.Exit();
            }
        }

        void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                double percentage = bytesIn / totalBytes * 100;
                progressBar1.Value = int.Parse(Math.Truncate(percentage).ToString());
            });
        }

        void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            System.Diagnostics.Process.Start("update.zip");
        }

    }
}
