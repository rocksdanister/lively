using System;
using System.Windows;
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
            this.wallpaperData = wp;
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
            else if (wp.LivelyInfo.Type == WallpaperType.gif)
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
                    }));
                }
                else
                {
                    Logger.Error("Wallpaper Preview: Failed to launch wallpaper: " + e.Msg + "\n" + e.Error.ToString());
                    MessageBox.Show(e.Error.Message, Properties.Resources.TitleAppName);
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
