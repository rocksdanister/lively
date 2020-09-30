using System;
using System.Windows;
using System.Windows.Interop;
using livelywpf.Core;

namespace livelywpf.Views
{
    /// <summary>
    /// Interaction logic for WallpaperPreviewWindow.xaml
    /// </summary>
    public partial class WallpaperPreviewWindow : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly LibraryModel wallpaperData;
        private IWallpaper wallpaper = null;
        private bool _initializedWallpaper = false;
        public WallpaperPreviewWindow(LibraryModel wp)
        {
            InitializeComponent();
            this.SizeChanged += WallpaperPreviewWindow_SizeChanged;
            this.wallpaperData = wp;
        }

        private void WallpaperPreviewWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(wallpaper != null)
            {
                var item = WindowOperations.GetAbsolutePlacement(PreviewBorder, true);
                NativeMethods.POINT pts = new NativeMethods.POINT() { X = (int)item.Left, Y = (int)item.Top };
                if (NativeMethods.ScreenToClient(new WindowInteropHelper(this).Handle, ref pts))
                {
                    NativeMethods.SetWindowPos(wallpaper.GetHWND(), 1, pts.X, pts.Y, (int)item.Width, (int)item.Height, 0 | 0x0010);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWallpaper(wallpaperData);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!_initializedWallpaper)
            {
                e.Cancel = true;
                return;
            }

            if(wallpaper != null)
            {
                //detach wallpaper window from this dialogue.
                WindowOperations.SetParentSafe(wallpaper.GetHWND(), IntPtr.Zero);
                try
                {
                    //temporary..till webprocess async close is ready.
                    if(wallpaper.GetWallpaperType() == WallpaperType.url)
                    {
                        var Proc = wallpaper.GetProcess();
                        Proc.Refresh();
                        Proc.StandardInput.WriteLine("lively:terminate");
                        if (!Proc.WaitForExit(4000))
                        {
                            wallpaper.Terminate();
                        }
                    }
                    else
                    {
                        wallpaper.Close();
                    }
                }
                catch
                {
                    wallpaper.Terminate();
                }
            }
        }

        private void LoadWallpaper(LibraryModel wp)
        {
            var targetDisplay = Program.SettingsVM.Settings.SelectedDisplay;
            if (wp.LivelyInfo.Type == WallpaperType.web
                || wp.LivelyInfo.Type == WallpaperType.webaudio
                || wp.LivelyInfo.Type == WallpaperType.url)
            {
                wp.ItemStartup = true;
                var item = new WebProcess(wp.FilePath, wp, targetDisplay);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            else if (wp.LivelyInfo.Type == WallpaperType.app
                || wp.LivelyInfo.Type == WallpaperType.godot
                || wp.LivelyInfo.Type == WallpaperType.unity)
            {
                if (Program.IsMSIX)
                {
                    Logger.Info("WallpaperPreview: Skipping program wallpaper on MSIX package.");
                    return;
                }

                wp.ItemStartup = true;
                var item = new ExtPrograms(wp.FilePath, wp, targetDisplay, 
                    Program.SettingsVM.Settings.WallpaperWaitTime);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            else if (wp.LivelyInfo.Type == WallpaperType.video)
            {
                wp.ItemStartup = true;
                var item = new VideoPlayerMPVExt(wp.FilePath, wp, targetDisplay,
                    Program.SettingsVM.Settings.WallpaperScaling);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            else if (wp.LivelyInfo.Type == WallpaperType.videostream)
            {
                wp.ItemStartup = true;
                var item = new VideoPlayerVLC(wp.FilePath, wp, targetDisplay);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
            else if (wp.LivelyInfo.Type == WallpaperType.gif || wp.LivelyInfo.Type == WallpaperType.picture)
            {
                var item = new GIFPlayerUWP(wp.FilePath, wp,
                    targetDisplay, Program.SettingsVM.Settings.WallpaperScaling);
                item.WindowInitialized += SetupDesktop_WallpaperInitialized;
                item.Show();
            }
        }

        private void SetupDesktop_WallpaperInitialized(object sender, WindowInitializedArgs e)
        {
            try
            {
                _initializedWallpaper = true;
                wallpaper = (IWallpaper)sender;
                wallpaper.WindowInitialized -= SetupDesktop_WallpaperInitialized;
                _ = this.Dispatcher.BeginInvoke(new Action(() => {
                    wallpaper.GetWallpaperData().ItemStartup = false;
                    ProgressRing.IsActive = false;
                }));

                if (e.Success)
                {
                    _ = this.Dispatcher.BeginInvoke(new Action(() => {       
                        //attach wp hwnd to border ui element.
                        WindowOperations.SetProgramToFramework(this, wallpaper.GetHWND(), PreviewBorder);
                        //fix for wallpaper overlapping window bordere in high dpi screens.
                        this.Width += 1;
                    }));
                }
                else
                {
                    if (e.Error != null)
                    {
                        Logger.Error("Wallpaper Preview: Failed to launch wallpaper=>" + e.Msg + "\n" + e.Error);
                    }
                    else
                    {
                        Logger.Error("Wallpaper Preview: Failed to launch wallpaper=> (No Exception thrown)" + e.Msg);
                    }
                    MessageBox.Show(Properties.Resources.LivelyExceptionGeneral, Properties.Resources.TextError);

                    _ = this.Dispatcher.BeginInvoke(new Action(() => {
                        this.Close();
                    }));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Wallpaper Preview: Failed processing wallpaper: " + ex.ToString());
                if (wallpaper != null)
                {
                    _ = this.Dispatcher.BeginInvoke(new Action(() => {
                        this.Close();
                    }));
                }
            }
        }
    }
}
