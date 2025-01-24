using CefSharp;
using Lively.Models.Message;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.Player.CefSharp.Extensions.CefSharp.DevTools
{
    public static partial class DevToolsExtensions
    {

        private static int LastMessageId = 600000;
        /// <summary>
        /// Calls Page.captureScreenshot without any optional params
        /// (Results in PNG image of default viewport)
        /// https://chromedevtools.github.io/devtools-protocol/tot/Page/#method-captureScreenshot
        /// </summary>
        /// <param name="browser">the ChromiumWebBrowser</param>
        /// <returns>png encoded image as byte[]</returns>
        public static async Task<byte[]> CaptureScreenshot(this IWebBrowser chromiumWebBrowser, ScreenshotFormat format)
        {
            if (chromiumWebBrowser is null)
                return null;

            //if (!browser.HasDocument)
            //{
            //    throw new System.Exception("Page hasn't loaded");
            //}

            var host = chromiumWebBrowser.GetBrowserHost();

            if (host == null || host.IsDisposed)
            {
                throw new Exception("BrowserHost is Null or Disposed");
            }

            //var param = new Dictionary<string, object>
            //{
            //    { "format", "png" },
            //}

            var msgId = Interlocked.Increment(ref LastMessageId);

            var observer = new TaskMethodDevToolsMessageObserver(msgId);

            //Make sure to dispose of our observer registration when done
            //TODO: Create a single observer that maps tasks to Id's
            //Or at least create one for each type, events and method
            using (var observerRegistration = host.AddDevToolsMessageObserver(observer))
            {
                //Page.captureScreenshot defaults to PNG, all params are optional
                //for this DevTools method
                int id = 0;
                const string methodName = "Page.captureScreenshot";
                Dictionary<string, object> param = null;
                switch (format)
                {
                    case ScreenshotFormat.jpeg:
                        param = new Dictionary<string, object> { { "format", "jpeg" } };
                        break;
                    case ScreenshotFormat.png:
                        param = null; // Default
                        break;
                    case ScreenshotFormat.webp:
                        param = new Dictionary<string, object> { { "format", "webp" } };
                        break;
                    case ScreenshotFormat.bmp:
                        // CEF unsupported
                        param = null; // Default
                        break;
                }

                //TODO: Simplify this, we can use an Func to reduce code duplication
                if (Cef.CurrentlyOnThread(CefThreadIds.TID_UI))
                {
                    id = host.ExecuteDevToolsMethod(msgId, methodName, param);
                }
                else
                {
                    id = await Cef.UIThreadTaskFactory.StartNew(() =>
                    {
                        return host.ExecuteDevToolsMethod(msgId, methodName, param);
                    });
                }

                if (id != msgId)
                {
                    throw new Exception("Message Id doesn't match the provided Id");
                }

                var result = await observer.Task;

                var success = result.Item1;

                dynamic response = JsonConvert.DeserializeObject<dynamic>(Encoding.UTF8.GetString(result.Item2));

                //Success
                if (success)
                {
                    return Convert.FromBase64String((string)response.data);
                }

                var code = (string)response.code;
                var message = (string)response.message;

                throw new Exception(code + ":" + message);
            }
        }

        public static async Task CaptureScreenshot(this IWebBrowser chromeBrowser, ScreenshotFormat format, string filePath)
        {
            byte[] imageBytes = await CaptureScreenshot(chromeBrowser, format);
            if (imageBytes == null)
                return;

            switch (format)
            {
                case ScreenshotFormat.jpeg:
                case ScreenshotFormat.png:
                case ScreenshotFormat.webp:
                    {
                        // Write to disk
                        File.WriteAllBytes(filePath, imageBytes);
                    }
                    break;
                case ScreenshotFormat.bmp:
                    {
                        // Convert byte[] to Image
                        using var ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                        using var image = Image.FromStream(ms, true);
                        image.Save(filePath, ImageFormat.Bmp);
                    }
                    break;
            }
        }
    }
}
