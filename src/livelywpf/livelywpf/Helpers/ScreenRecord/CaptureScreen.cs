using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace livelywpf
{
    public static class CaptureScreen
    {
        /// <summary>
        /// Captures screen foreground image.
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="fileName"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public static void CopyScreen(string savePath, int x, int y, int width, int height)
        {
            using (var screenBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (var bmpGraphics = Graphics.FromImage(screenBmp))
                {
                    bmpGraphics.CopyFromScreen(x, y, 0, 0, screenBmp.Size);
                    screenBmp.Save(savePath, ImageFormat.Jpeg);
                }
            }
        }

        public static Bitmap CopyScreen(int x, int y, int width, int height)
        {
            var screenBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var bmpGraphics = Graphics.FromImage(screenBmp))
            {
                bmpGraphics.CopyFromScreen(x, y, 0, 0, screenBmp.Size);
                return screenBmp;
            }
        }
    }
}
