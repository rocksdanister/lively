using Lively.Models.Message;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using WebView = Microsoft.Web.WebView2.WinForms.WebView2;

namespace Lively.Player.WebView2.Extensions.WebView2
{
    public static class DevToolsExtensions
    {
        // Ref: https://github.com/MicrosoftEdge/WebView2Feedback/issues/529
        public static async Task CaptureScreenshot(this WebView webView, ScreenshotFormat format, string filePath)
        {
            var base64String = await webView.CaptureScreenshot(format);
            var imageBytes = Convert.FromBase64String(base64String);
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
                        using MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
                        using Image image = Image.FromStream(ms, true);
                        image.Save(filePath, ImageFormat.Bmp);
                    }
                    break;
            }
        }

        public static async Task<string> CaptureScreenshot(this WebView webView, ScreenshotFormat format)
        {
            var param = format switch
            {
                ScreenshotFormat.jpeg => "{\"format\":\"jpeg\"}",
                ScreenshotFormat.webp => "{\"format\":\"webp\"}",
                ScreenshotFormat.png => "{}", // Default
                ScreenshotFormat.bmp => "{}", // Not supported by cef
                _ => "{}",
            };
            string r3 = await webView.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", param);
            JObject o3 = JObject.Parse(r3);
            JToken data = o3["data"];
            return data.ToString();
        }

        //private static Image Base64ToImage(string base64String)
        //{
        //    // Convert base 64 string to byte[]
        //    byte[] imageBytes = Convert.FromBase64String(base64String);
        //    // Convert byte[] to Image
        //    using MemoryStream ms = new MemoryStream(imageBytes, 0, imageBytes.Length);
        //    Image image = Image.FromStream(ms, true);
        //    return image;
        //}
    }
}
