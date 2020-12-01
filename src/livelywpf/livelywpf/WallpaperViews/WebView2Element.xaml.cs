using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
//using Microsoft.Web.WebView2.Wpf;

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
        private readonly WallpaperType wallpaperType;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public WebView2Element(string path, WallpaperType type, string livelyPropertyPath)
        {
            InitializeComponent();
            this.htmlPath = path;
            this.livelyPropertyPath = livelyPropertyPath;
            this.wallpaperType = type;
            InitWebView();
        }

        //TODO:
        //link checking
        //cross-origin request fix for disk files.
        //custom cache path.
        private async void InitWebView()
        {
            try
            {
                await webView.EnsureCoreWebView2Async();
                //only after await, null otherwise.
                webView.CoreWebView2.ProcessFailed += CoreWebView2_ProcessFailed;

                if (wallpaperType == WallpaperType.url)
                {
                    string ytVideoId = "test";
                    if (htmlPath.Contains("shadertoy.com/view"))
                    {
                        webView.CoreWebView2.NavigateToString(ShadertoyURLtoEmbedLink(htmlPath));
                    }
                    else if ((ytVideoId = Helpers.libMPVStreams.GetYouTubeVideoIdFromUrl(htmlPath)) != "")
                    {
                        //open fullscreen embed player with loop enabled.
                        webView.CoreWebView2.Navigate("https://www.youtube.com/embed/" + ytVideoId +
                            "?version=3&rel=0&autoplay=1&loop=1&controls=0&playlist=" + ytVideoId);
                    }
                    else
                    {
                        webView.CoreWebView2.Navigate(htmlPath);
                    }
                    Logger.Debug("YTVIDID:" + ytVideoId);
                }
                else
                {
                    webView.CoreWebView2.Navigate(htmlPath);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Webview2: fail=>" + e.ToString());
                //To avoid blinding white color.
                webView.Visibility = Visibility.Collapsed;
            }
        }

        private void WebView2Element_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            WindowOperations.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            this.ShowInTaskbar = false;
            this.ShowInTaskbar = true;
        }

        private void webView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            RestoreLivelyProperties(livelyPropertyPath);
        }

        private void CoreWebView2_ProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            Logger.Error("Webview2: fail=>" + e.ToString());
        }

        public void SendMessage(string msg)
        {
            if (msg.Equals("lively:reload", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    webView.Reload();
                }
                catch (Exception e)
                {
                    Logger.Error("Webview2: reload error=>" + e.Message);
                }
            }
            else if (msg.Contains("lively:customise", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    LivelyPropertiesMsg(msg);
                }
                catch (Exception e)
                {
                    Logger.Error("Webview2: lively property error=>" + e.Message);
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
                catch (Exception ex)
                {
                    Logger.Error("Webview2: Dispose error=>" + ex.Message);
                }
            }
        }

        #region helpers

        //credit: https://stackoverflow.com/questions/62835549/equivalent-of-webbrowser-invokescriptstring-object-in-webview2
        private async Task<string> ExecuteScriptFunctionAsync(string functionName, params object[] parameters)
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
            return await webView.ExecuteScriptAsync(script.ToString());
        }

        private void RestoreLivelyProperties(string propertyPath)
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
                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener", item.Key, (int)item.Value["value"]);
                        }
                        else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
                        {
                            var filePath = Path.Combine(Path.GetDirectoryName(htmlPath), item.Value["folder"].ToString(), item.Value["value"].ToString());
                            if (File.Exists(filePath))
                            {
                                _ = ExecuteScriptFunctionAsync("livelyPropertyListener",
                                item.Key,
                                Path.Combine(item.Value["folder"].ToString(), item.Value["value"].ToString()));
                            }
                            else
                            {
                                _ = ExecuteScriptFunctionAsync("livelyPropertyListener",
                                item.Key,
                                null); //or custom msg
                            }
                        }
                        else if (uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener", item.Key, (bool)item.Value["value"]);
                        }
                        else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase) || uiElementType.Equals("textbox", StringComparison.OrdinalIgnoreCase))
                        {
                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener", item.Key, (string)item.Value["value"]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Webview2: lively property restore error=>" + e.Message);
            }
        }

        /// <summary>
        /// Re-using cefsharp ipc msg code.
        /// ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-IV-:-Interaction
        /// </summary>
        /// <param name="val"></param>
        private void LivelyPropertiesMsg(string val)
        {
            var msg = val.Split(' ');
            if (msg.Length < 4)
                return;

            string uiElementType = msg[1];
            if (uiElementType.Equals("dropdown", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(msg[3], out int value))
                {
                    _ = ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], value);
                }
            }
            else if (uiElementType.Equals("slider", StringComparison.OrdinalIgnoreCase))
            {
                //MessageBox.Show(msg[3] + " " + double.TryParse(msg[3], out double test));
                if (double.TryParse(msg[3], out double value))
                {
                    _ = ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], value);
                }
            }
            else if (uiElementType.Equals("folderDropdown", StringComparison.OrdinalIgnoreCase))
            {
                var sIndex = val.IndexOf("\"") + 1;
                var lIndex = val.LastIndexOf("\"") - 1;
                var filePath = Path.Combine(Path.GetDirectoryName(htmlPath), val.Substring(sIndex, lIndex - sIndex + 1));
                if (File.Exists(filePath))
                {
                    _ = ExecuteScriptFunctionAsync("livelyPropertyListener",
                    msg[2],
                    val.Substring(sIndex, lIndex - sIndex + 1));
                }
                else
                {
                    _ = ExecuteScriptFunctionAsync("livelyPropertyListener",
                    msg[2],
                    null); //or custom msg
                }
            }
            else if (uiElementType.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(msg[3], out bool value))
                {
                    _ = ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], value);
                }
            }
            else if (uiElementType.Equals("color", StringComparison.OrdinalIgnoreCase))
            {
                _ = ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], msg[3]);
            }
            else if (uiElementType.Equals("textbox", StringComparison.OrdinalIgnoreCase))
            {
                var sIndex = val.IndexOf("\"") + 1;
                var lIndex = val.LastIndexOf("\"") - 1;
                _ = ExecuteScriptFunctionAsync("livelyPropertyListener",
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
                    _ = ExecuteScriptFunctionAsync("livelyPropertyListener", msg[2], true);
                }
            }
        }

        /// <summary>
        /// Converts shadertoy.com url to embed link: fullscreen, muted audio.
        /// </summary>
        /// <param name="shadertoylink"></param>
        /// <returns>shadertoy embed url</returns>
        private string ShadertoyURLtoEmbedLink(string shadertoylink)
        {
            Uri uri;
            try
            {
                uri = new Uri(shadertoylink);
            }
            catch (UriFormatException)
            {
                try
                {
                    //if user did not input https/http assume https connection.
                    uri = new UriBuilder(shadertoylink)
                    {
                        Scheme = "https",
                        Port = -1,
                    }.Uri;
                    shadertoylink = uri.ToString();
                }
                catch { }
            }

            shadertoylink = shadertoylink.Replace("view/", "embed/");

            string text = @"<!DOCTYPE html><html lang=""en"" dir=""ltr""> <head> <meta charset=""utf - 8""> 
                    <title>Digital Brain</title> <style media=""screen""> iframe { position: fixed; width: 100%; height: 100%; top: 0; right: 0; bottom: 0;
                    left: 0; z-index; -1; pointer-events: none;  } </style> </head> <body> <iframe width=""640"" height=""360"" frameborder=""0"" 
                    src=" + shadertoylink + @"?gui=false&t=10&paused=false&muted=true""></iframe> </body></html>";
            return text;
        }

        #endregion //helpers
    }
}