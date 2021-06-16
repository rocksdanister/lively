using livelywpf.Core.API;
using Newtonsoft.Json;
using System;
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
                    string tmp = null;
                    if (TryParseShadertoy(htmlPath, ref tmp))
                    {
                        webView.CoreWebView2.NavigateToString(tmp);
                    }
                    else if ((tmp = Helpers.StreamHelper.GetYouTubeVideoIdFromUrl(htmlPath)) != "")
                    {
                        //open fullscreen embed player with loop enabled.
                        webView.CoreWebView2.Navigate("https://www.youtube.com/embed/" + tmp + 
                            "?version=3&rel=0&autoplay=1&loop=1&controls=0&playlist="+tmp);
                    }
                    else
                    {
                        webView.CoreWebView2.Navigate(htmlPath);
                    }
                }
                else
                {
                    webView.CoreWebView2.Navigate(htmlPath);
                }
            }
            catch(Exception e)
            {
                Logger.Error("Webview2 Init fail: {0}" + e.ToString());
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

        private void webView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            RestoreLivelyProperties();
        }

        private void CoreWebView2_ProcessFailed(object sender, Microsoft.Web.WebView2.Core.CoreWebView2ProcessFailedEventArgs e)
        {
            Logger.Error("Webview2 Fail: {0}", e.ToString());
        }

        public void MessageProcess(string msg)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<IpcMessage>(msg, new JsonSerializerSettings() { Converters = { new IpcMessageConverter() } });
                switch (obj.Type)
                {
                    case MessageType.cmd_reload:
                        webView.Reload();
                        break;
                    case MessageType.lp_slider:
                        var sl = (LivelySlider)obj;
                        _ = ExecuteScriptFunctionAsync("livelyPropertyListener", sl.Name, sl.Value);
                        break;
                    case MessageType.lp_textbox:
                        var tb = (LivelyTextBox)obj;
                        _ = ExecuteScriptFunctionAsync("livelyPropertyListener", tb.Name, tb.Value);
                        break;
                    case MessageType.lp_dropdown:
                        var dd = (LivelyDropdown)obj;
                        _ = ExecuteScriptFunctionAsync("livelyPropertyListener", dd.Name, dd.Value);
                        break;
                    case MessageType.lp_cpicker:
                        var cp = (LivelyColorPicker)obj;
                        _ = ExecuteScriptFunctionAsync("livelyPropertyListener", cp.Name, cp.Value);
                        break;
                    case MessageType.lp_chekbox:
                        var cb = (LivelyCheckbox)obj;
                        _ = ExecuteScriptFunctionAsync("livelyPropertyListener", cb.Name, cb.Value);
                        break;
                    case MessageType.lp_fdropdown:
                        var fd = (LivelyFolderDropdown)obj;
                        var filePath = Path.Combine(Path.GetDirectoryName(htmlPath), fd.Value);
                        if (File.Exists(filePath))
                        {
                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener",
                            fd.Name,
                            fd.Value);
                        }
                        else
                        {
                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener",
                            fd.Name,
                            null); //or custom msg
                        }
                        break;
                    case MessageType.lp_button:
                        var btn = (LivelyButton)obj;
                        if (btn.IsDefault)
                        {
                            RestoreLivelyProperties();
                        }
                        else
                        {
                            _ = ExecuteScriptFunctionAsync("livelyPropertyListener", btn.Name, true);
                        }
                        break;
                    case MessageType.lsp_perfcntr:
                        break;
                    case MessageType.lsp_nowplaying:
                        break;
                    case MessageType.cmd_close:
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error processing msg: {0}", ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                webView?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error("Dispose Fail: {0}", ex.Message);
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

        private void RestoreLivelyProperties()
        {
            try
            {
                if (livelyPropertyPath == null)
                    return;

                foreach (var item in Cef.LivelyPropertiesJSON.LoadLivelyProperties(livelyPropertyPath))
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
                Logger.Error("Property restore error: {0}", e.Message);
            }
        }


        private bool TryParseShadertoy(string url, ref string html)
        {
            if (!url.Contains("shadertoy.com/view"))
            {
                return false;
            }

            try
            {
                _ = Helpers.LinkHandler.SanitizeUrl(url);
            }
            catch
            {
                return false;
            }

            url = url.Replace("view/", "embed/");
            html = @"<!DOCTYPE html><html lang=""en"" dir=""ltr""> <head> <meta charset=""utf - 8""> 
                    <title>Digital Brain</title> <style media=""screen""> iframe { position: fixed; width: 100%; height: 100%; top: 0; right: 0; bottom: 0;
                    left: 0; z-index; -1; pointer-events: none;  } </style> </head> <body> <iframe width=""640"" height=""360"" frameborder=""0"" 
                    src=" + url + @"?gui=false&t=10&paused=false&muted=true""></iframe> </body></html>";
            return true;
        }

        #endregion //helpers
    }
}
