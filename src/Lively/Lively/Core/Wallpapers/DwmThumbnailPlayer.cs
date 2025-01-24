using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Helpers.Shell;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Message;
using Lively.Views;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Lively.Core.Wallpapers
{
    public class DwmThumbnailPlayer : IWallpaper
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly TaskCompletionSource<bool> windowLoadedCompletionSource = new();
        private readonly IntPtr thumbnailSrc;
        private readonly Rectangle targetRect;
        private DwmThumbnailWrapper dwmThumbnail;
        private BlankWindow window;

        public DwmThumbnailPlayer(IntPtr thumbnailSrc, LibraryModel model, DisplayMonitor display, Rectangle targetRect)
        {
            this.Model = model;
            this.Screen = display;
            this.targetRect = targetRect;
            this.thumbnailSrc = thumbnailSrc;
        }

        public bool IsExited { get; private set; }

        public bool IsLoaded { get; private set; }

        public WallpaperType Category => WallpaperType.video;

        public LibraryModel Model { get; }

        public IntPtr Handle { get; private set;}

        public IntPtr InputHandle => IntPtr.Zero;

        public Process Proc => null;

        public DisplayMonitor Screen { get; set; }

        public string LivelyPropertyCopyPath => null;

        public async Task ShowAsync()
        {
            await Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ThreadStart(async delegate
            {
                try
                {
                    window = new BlankWindow()
                    {
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ResizeMode = ResizeMode.NoResize,
                        WindowStyle = WindowStyle.None,
                        ShowActivated = false,
                        Left = -9999,
                    };
                    window.Loaded += Window_Loaded;
                    window.Closed += Window_Closed;
                    window.Show();
                    await windowLoadedCompletionSource.Task;

                    if (Handle == IntPtr.Zero)
                        throw new InvalidOperationException("Window handle null");

                    dwmThumbnail = new DwmThumbnailWrapper(thumbnailSrc, Handle);
                    dwmThumbnail.Show();
                    dwmThumbnail.Update(new Rectangle(targetRect.Left, targetRect.Top, targetRect.Width, targetRect.Height),
                        new Rectangle(0, 0, targetRect.Width, targetRect.Height));
                }
                catch
                {
                    Terminate();

                    throw;
                }
            }));
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Handle = new WindowInteropHelper(window).Handle;
            //ShowInTaskbar = false : causing issue with windows10 Taskview.
            WindowUtil.RemoveWindowFromTaskbar(Handle);
            //this hides the window from taskbar and also fixes crash when win10 taskview is launched. 
            window.ShowInTaskbar = false;
            window.ShowInTaskbar = true;

            windowLoadedCompletionSource.TrySetResult(true);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            dwmThumbnail?.Dispose();
            dwmThumbnail = null;
        }

        public void Play()
        {
            //nothing
        }

        public void Pause()
        {
            //nothing
        }

        public void Stop()
        {
            //nothing
        }

        public void Close()
        {
            _ = Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(delegate
            {
                window.Close();
            }));
            DesktopUtil.RefreshDesktop();
        }

        public void Terminate()
        {
            Close();
        }

        public Task ScreenCapture(string filePath)
        {
            throw new NotImplementedException();
        }

        public void SendMessage(IpcMessage obj)
        {
            //nothing
        }

        public void SetMute(bool mute)
        {
            //nothing
        }

        public void SetPlaybackPos(float pos, PlaybackPosType type)
        {
            //nothing
        }

        public void SetVolume(int volume)
        {
            //nothing
        }
    }
}
