using System;
using System.ComponentModel;
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
        public static void CopyScreen(string savePath, string fileName, int x, int y, int width, int height)
        {
            using (var screenBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
            {
                using (var bmpGraphics = Graphics.FromImage(screenBmp))
                {
                    bmpGraphics.CopyFromScreen(x, y, 0, 0, screenBmp.Size);
                    screenBmp.Save(Path.Combine(savePath, fileName), ImageFormat.Jpeg);
                    /*
                    IntPtr hBitmap = screenBmp.GetHbitmap();
                    try
                    {
                        var bmSrc = Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                        using (var fileStream = new FileStream(Path.Combine(savePath, fileName), FileMode.Create))
                        {
                            BitmapEncoder encoder = new JpegBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bmSrc));
                            encoder.Save(fileStream);
                        }
                    }
                    finally
                    {
                        NativeMethods.DeleteObject(hBitmap);
                    }
                    */
                }
            }
        }

        /// <summary>
        /// Capture window, can work if not foreground.
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <returns></returns>
        public static Bitmap CaptureWindow(IntPtr hWnd)
        {
            NativeMethods.GetWindowRect(hWnd, out NativeMethods.RECT rect);
            var region = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);

            IntPtr winDc;
            IntPtr memoryDc;
            IntPtr bitmap;
            IntPtr oldBitmap;
            bool success;
            Bitmap result;

            winDc = NativeMethods.GetWindowDC(hWnd);
            memoryDc = NativeMethods.CreateCompatibleDC(winDc);
            bitmap = NativeMethods.CreateCompatibleBitmap(winDc, region.Width, region.Height);
            oldBitmap = NativeMethods.SelectObject(memoryDc, bitmap);

            success = NativeMethods.BitBlt(memoryDc, 0, 0, region.Width, region.Height, winDc, region.Left, region.Top, 
                NativeMethods.TernaryRasterOperations.SRCCOPY | NativeMethods.TernaryRasterOperations.CAPTUREBLT);

            try
            {
                if (!success)
                {
                    throw new Win32Exception();
                }

                result = Image.FromHbitmap(bitmap);
            }
            finally
            {
                NativeMethods.SelectObject(memoryDc, oldBitmap);
                NativeMethods.DeleteObject(bitmap);
                NativeMethods.DeleteDC(memoryDc);
                NativeMethods.ReleaseDC(hWnd, winDc);
            }
            return result;
        }
    }
}
