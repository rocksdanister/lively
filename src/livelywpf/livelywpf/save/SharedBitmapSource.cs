using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace livelywpf
{
    //credit: https://stackoverflow.com/questions/94456/load-a-wpf-bitmapimage-from-a-system-drawing-bitmap/32841840#32841840
    public class SharedBitmapSource : BitmapSource, IDisposable
    {
        #region Public Properties

        /// <summary>
        /// I made it public so u can reuse it and get the best our of both namespaces
        /// </summary>
        public Bitmap Bitmap { get; private set; }

        public override double DpiX { get { return Bitmap.HorizontalResolution; } }

        public override double DpiY { get { return Bitmap.VerticalResolution; } }

        public override int PixelHeight { get { return Bitmap.Height; } }

        public override int PixelWidth { get { return Bitmap.Width; } }

        public override System.Windows.Media.PixelFormat Format { get { return ConvertPixelFormat(Bitmap.PixelFormat); } }

        public override BitmapPalette Palette { get { return null; } }

        #endregion

        #region Constructor/Destructor

        public SharedBitmapSource(int width, int height, System.Drawing.Imaging.PixelFormat sourceFormat)
            : this(new Bitmap(width, height, sourceFormat)) { }

        public SharedBitmapSource(Bitmap bitmap)
        {
            Bitmap = bitmap;
        }

        // Use C# destructor syntax for finalization code.
        ~SharedBitmapSource()
        {
            // Simply call Dispose(false).
            Dispose(false);
        }

        #endregion

        #region Overrides

        public override void CopyPixels(Int32Rect sourceRect, Array pixels, int stride, int offset)
        {
            BitmapData sourceData = Bitmap.LockBits(
            new Rectangle(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height),
            ImageLockMode.ReadOnly,
            Bitmap.PixelFormat);

            var length = sourceData.Stride * sourceData.Height;

            if (pixels is byte[])
            {
                var bytes = pixels as byte[];
                Marshal.Copy(sourceData.Scan0, bytes, 0, length);
            }

            Bitmap.UnlockBits(sourceData);
        }

        protected override Freezable CreateInstanceCore()
        {
            return (Freezable)Activator.CreateInstance(GetType());
        }

        #endregion

        #region Public Methods

        public BitmapSource Resize(int newWidth, int newHeight)
        {
            Image newImage = new Bitmap(newWidth, newHeight);
            using (Graphics graphicsHandle = Graphics.FromImage(newImage))
            {
                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsHandle.DrawImage(Bitmap, 0, 0, newWidth, newHeight);
            }
            return new SharedBitmapSource(newImage as Bitmap);
        }

        public new BitmapSource Clone()
        {
            return new SharedBitmapSource(new Bitmap(Bitmap));
        }

        //Implement IDisposable.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected/Private Methods

        private static System.Windows.Media.PixelFormat ConvertPixelFormat(System.Drawing.Imaging.PixelFormat sourceFormat)
        {
            switch (sourceFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    return PixelFormats.Bgr24;

                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    return PixelFormats.Pbgra32;

                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    return PixelFormats.Bgr32;

            }
            return new System.Windows.Media.PixelFormat();
        }

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                }
                // Free your own state (unmanaged objects).
                // Set large fields to null.
                _disposed = true;
            }
        }

        #endregion
    }
}
