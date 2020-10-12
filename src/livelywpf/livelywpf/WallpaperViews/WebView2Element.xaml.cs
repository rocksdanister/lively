using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for WebView2Element.xaml
    /// </summary>
    public partial class WebView2Element : Window
    {
        //ref: https://docs.microsoft.com/en-us/microsoft-edge/webview2/gettingstarted/wpf
        private readonly string htmlPath;
        private readonly string livelyPropertyPath;
        public WebView2Element(string path, string livelyPropertyPath)
        {
            this.htmlPath = path;
            this.livelyPropertyPath = livelyPropertyPath;
            InitializeComponent();
            webView.EnsureCoreWebView2Async();
            this.Loaded += WebView2Element_Loaded;
        }

        private void WebView2Element_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            WindowOperations.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;
        }
        private void webView_CoreWebView2Ready(object sender, EventArgs e)
        {
            //todo: 
            //link checking
            //cross-origin request fix for disk files.
            webView.CoreWebView2.Navigate(htmlPath);
        }

        private void webView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            RestoreLivelyProperties(livelyPropertyPath);
        }

        public void SendMessage(string msg)
        {
            if (String.Equals(msg, "lively:reload", StringComparison.OrdinalIgnoreCase))
            {
                webView.Reload();
            }
            else if (msg.Contains("lively:customise", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    LivelyPropertiesMsg(msg);
                }
                catch
                {
                    //todo: logging.
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (webView != null)
            {
                try
                {
                    webView.Dispose();
                }
                catch { }
            }
        }

        #region helpers

        //credit: https://stackoverflow.com/questions/62835549/equivalent-of-webbrowser-invokescriptstring-object-in-webview2
        private async Task<string> ExecuteScriptFunctionAsync(string functionName, params object[] parameters)
        {
            string script = functionName + "(";
            for (int i = 0; i < parameters.Length; i++)
            {
                script += JsonConvert.SerializeObject(parameters[i]);
                if (i < parameters.Length - 1)
                {
                    script += ", ";
                }
            }
            script += ");";
            return await webView.ExecuteScriptAsync(script);
        }

        private async void RestoreLivelyProperties(string propertyPath)
        {
            try
            {
                if (propertyPath == null)
                    return;

                foreach (var item in Cef.LivelyPropertiesJSON.LoadLivelyProperties(propertyPath))
                {
                    string uiElementType = item.Value["type"].ToString();
                    if (!uiElementType.Equals("button", StringComparison.OrdinalIgnoreCase) && !uiElementType.Equals("label", StringComparison.OrdinalIgnoreCase))
                    {
                        if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase) ||
                            uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
                        {
                            await ExecuteScriptFunctionAsync("livelyPropertyListener", item.Key, (int)item.Value["value"]);
                        }
                        else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                        {
                            var filePath = Path.Combine(Path.GetDirectoryName(htmlPath), item.Value["folder"].ToString(), item.Value["value"].ToString());
                            if (File.Exists(filePath))
                            {
                                await ExecuteScriptFunctionAsync("livelyPropertyListener",
                                item.Key,
                                Path.Combine(item.Value["folder"].ToString(), item.Value["value"].ToString()));
                            }
                            else
                            {
                                await ExecuteScriptFunctionAsync("livelyPropertyListener",
                                item.Key,
                                null); //or custom msg
                            }
                        }
                        else if (uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                        {
                            await ExecuteScriptFunctionAsync("livelyPropertyListener", item.Key, (bool)item.Value["value"]);
                        }
                        else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase) || uiElementType.Equals("textbox", StringComparison.OrdinalIgnoreCase))
                        {
                            await ExecuteScriptFunctionAsync("livelyPropertyListener", item.Key, (string)item.Value["value"]);
                        }
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Re-using cefsharp ipc msg code.
        /// not Working? why...
        /// ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-IV-:-Interaction
        /// </summary>
        /// <param name="val"></param>
        private async void LivelyPropertiesMsg(string val)
        {
            var msg = val.Split(' ');
            if (msg.Length < 4)
                return;

            string uiElementType = msg[1];
            if (uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(msg[3], out int value))
                {
                    await ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], value);
                }
            }
            else if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase))
            {
                //MessageBox.Show(msg[3] + " " + double.TryParse(msg[3], out double test));
                if (double.TryParse(msg[3], out double value))
                {
                    await ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], value);
                }
            }
            else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
            {
                var sIndex = val.IndexOf("\"") + 1;
                var lIndex = val.LastIndexOf("\"") - 1;
                var filePath = Path.Combine(Path.GetDirectoryName(htmlPath), val.Substring(sIndex, lIndex - sIndex + 1));
                if (File.Exists(filePath))
                {
                    await ExecuteScriptFunctionAsync("livelyPropertyListener",
                    msg[2],
                    val.Substring(sIndex, lIndex - sIndex + 1));
                }
                else
                {
                    await ExecuteScriptFunctionAsync("livelyPropertyListener",
                    msg[2],
                    null); //or custom msg
                }
            }
            else if (uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(msg[3], out bool value))
                {
                    await ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], value);
                }
            }
            else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], msg[3]);
            }
            else if (uiElementType.Equals("textbox", StringComparison.OrdinalIgnoreCase))
            {
                var sIndex = val.IndexOf("\"") + 1;
                var lIndex = val.LastIndexOf("\"") - 1;
                await ExecuteScriptFunctionAsync("livelyPropertyListener",
                    msg[2],
                    val.Substring(sIndex, lIndex - sIndex + 1));
            }
            else if (uiElementType.Equals("button", StringComparison.OrdinalIgnoreCase))
            {
                if (msg[2].Equals("lively_default_settings_reload", StringComparison.OrdinalIgnoreCase))
                {
                    RestoreLivelyProperties(livelyPropertyPath);
                }
                else
                {
                    await ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], true);
                }
            }
        }

        #endregion //helpers
    }
}
