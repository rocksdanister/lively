using ImageMagick;
using Lively.Common.Helpers.Pinvoke;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Lively.Helpers
{
    public static class CaptureScreen
    {
        /// <summary>
        /// Captures screen foreground image.
        /// </summary>
        /// <param name="savePath"></param>
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

        /// <summary>
        /// Screen capture and create animated gif.
        /// </summary>
        /// <param name="savePath">File location</param>
        /// <param name="x">capture x offset</param>
        /// <param name="y">capture y offset</param>
        /// <param name="width">capture width</param>
        /// <param name="height">capture height</param>
        /// <param name="captureDelay">delay between capture frames</param>
        /// <param name="animeDelay">delay between saved frames</param>
        /// <param name="totalFrames">total number of frames to capture</param>
        /// <param name="progress">current progress</param>
        /// <returns></returns>
        public static async Task CaptureGif(string savePath, int x, int y, int width, int height,
            int captureDelay, int animeDelay, int totalFrames, IProgress<int> progress)
        {
            await Task.Run(async () =>
            {
                var miArray = new MagickImage[totalFrames];
                try
                {
                    for (int i = 0; i < totalFrames; i++)
                    {
                        using (var bmp = CopyScreen(x, y, width, height))
                        {
                            miArray[i] = ToMagickImage(bmp);
                        }
                        await Task.Delay(captureDelay);
                        progress.Report((i + 1) * 100 / totalFrames);
                    }

                    using (MagickImageCollection collection = new MagickImageCollection())
                    {
                        for (int i = 0; i < totalFrames; i++)
                        {
                            collection.Add(miArray[i]);
                            collection[i].AnimationDelay = animeDelay;
                        }

                        // Optionally reduce colors
                        QuantizeSettings settings = new QuantizeSettings
                        {
                            Colors = 256,
                        };
                        collection.Quantize(settings);

                        // Optionally optimize the images (images should have the same size).
                        collection.Optimize();
                        // Save image to disk.
                        collection.Write(savePath);
                    }
                }
                finally
                {
                    for (int i = 0; i < totalFrames; i++)
                    {
                        miArray[i]?.Dispose();
                    }
                }
            });
        }

        #region helpers

        //ref: https://github.com/dlemstra/Magick.NET/issues/543
        public static MagickImage ToMagickImage(Bitmap bmp)
        {
            MagickImage mi = new MagickImage();
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;
                mi.Read(ms);
            }
            return mi;
        }

        public static void ResizeAnimatedGif(string srcFile, string destFile, int width, int height)
        {
            // Read from file
            using (var collection = new MagickImageCollection(srcFile))
            {
                // This will remove the optimization and change the image to how it looks at that point
                // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                collection.Coalesce();

                // Resize each image in the collection to a width of 200. When zero is specified for the height
                // the height will be calculated with the aspect ratio.
                foreach (var image in collection)
                {
                    image.Resize(width, height);
                }

                // Save the result
                collection.Write(destFile);
            }
        }

        #endregion //helpers
    }
}
