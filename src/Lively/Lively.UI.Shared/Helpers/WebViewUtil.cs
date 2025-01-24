using Lively.Common;
using Lively.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.UI.Shared.Helpers
{
    public static class WebViewUtil
    {
        public static string DownloadUrl => "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

        public static bool IsWebView2Available()
        {
            try
            {
                return !string.IsNullOrEmpty(CoreWebView2Environment.GetAvailableBrowserVersionString());
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<bool> InstallWebView2(IDownloadService downloader)
        {
            if (Constants.ApplicationType.IsMSIX)
                return false;

            try
            {
                var filePath = Path.Combine(Constants.CommonPaths.TempDir, "MicrosoftEdgeWebview2Setup.exe");
                await downloader.DownloadFile(new Uri(DownloadUrl), filePath);
                await Process.Start(filePath, "/silent /install").WaitForExitAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
