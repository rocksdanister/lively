using System;
using System.Drawing;
using System.Runtime.InteropServices;
using static Lively.Common.Helpers.Pinvoke.NativeMethods;

namespace Lively.Common.Helpers.Pinvoke
{
    public class DwmThumbnailWrapper : IDisposable
    {
        private bool disposedValue;
        private readonly IntPtr src, dest;
        private IntPtr thumbnail;

        public DwmThumbnailWrapper(IntPtr src, IntPtr dest)
        {
            this.src = src;
            this.dest = dest;
        }

        public void Show()
        {
            if (thumbnail != IntPtr.Zero)
                throw new InvalidOperationException("Thumbnail already registered!");

            var result = DwmRegisterThumbnail(dest, src, out thumbnail);
            if (result != 0)
                throw new InvalidOperationException($"Thumbnail failed: {result}");

            if (thumbnail == IntPtr.Zero)
                throw new InvalidOperationException("Thumbnail returned null.");
        }

        public bool TryShow()
        {
            try
            {
                Show();
            }
            catch
            {
                return false;
            }
            return true;
        }

        public void Update(Rectangle frameSrc, Rectangle frameDest)
        {
            var props = new DWM_THUMBNAIL_PROPERTIES
            {
                fVisible = true,
                dwFlags = DWM_TNP_VISIBLE | DWM_TNP_OPACITY | DWM_TNP_RECTSOURCE | DWM_TNP_RECTDESTINATION,
                opacity = 255,
                rcDestination = new RECT() { Left = frameDest.Left, Top = frameDest.Top, Bottom = frameDest.Bottom, Right = frameDest.Right },
                rcSource = new RECT() { Left = frameSrc.Left, Top = frameSrc.Top, Bottom = frameSrc.Bottom, Right = frameSrc.Right },
            };
            DwmUpdateThumbnailProperties(thumbnail, ref props);
        }

        private void Destroy()
        {
            DwmUnregisterThumbnail(thumbnail);
            thumbnail = IntPtr.Zero;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Destroy();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DwmThumbnailWrapper()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region native

        [StructLayout(LayoutKind.Sequential)]
        internal struct DWM_THUMBNAIL_PROPERTIES
        {
            public int dwFlags;
            public RECT rcDestination;
            public RECT rcSource;
            public byte opacity;
            public bool fVisible;
            public bool fSourceClientAreaOnly;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi.dll")]
        private static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport("dwmapi.dll")]
        private static extern int DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        [DllImport("dwmapi.dll")]
        private static extern int DwmQueryThumbnailSourceSize(IntPtr thumb, out POINT size);

        private static readonly int DWM_TNP_VISIBLE = 0x8;
        private static readonly int DWM_TNP_OPACITY = 0x4;
        private static readonly int DWM_TNP_RECTDESTINATION = 0x1;
        private static readonly int DWM_TNP_RECTSOURCE = 0x2;

        #endregion //native
    }
}
