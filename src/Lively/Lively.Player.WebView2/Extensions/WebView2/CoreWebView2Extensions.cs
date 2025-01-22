using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WebView = Microsoft.Web.WebView2.WinForms.WebView2;

namespace Lively.Player.WebView2.Extensions.WebView2
{
    public static class CoreWebView2Extensions
    {
        public static void NavigateToLocalPath(this WebView webView, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            var fileName = Path.GetFileName(filePath);
            // Use unique hostname to avoid webview cache issues.
            var hostName = new DirectoryInfo(filePath).Parent.Name;
            var directoryPath = Path.GetDirectoryName(filePath);
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                hostName,
                directoryPath,
                CoreWebView2HostResourceAccessKind.Allow);

            webView.CoreWebView2.Navigate($"https://{hostName}/{fileName}");
        }

        // Ref: https://stackoverflow.com/questions/62835549/equivalent-of-webbrowser-invokescriptstring-object-in-webview2
        public static async Task<string> ExecuteScriptFunctionAsync(this WebView webView, string functionName, params object[] parameters)
        {
            var script = new StringBuilder();
            script.Append(functionName);
            script.Append("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                script.Append(JsonConvert.SerializeObject(parameters[i]));
                if (i < parameters.Length - 1)
                {
                    script.Append(", ");
                }
            }
            script.Append(");");
            return await webView?.ExecuteScriptAsync(script.ToString());
        }
    }
}
