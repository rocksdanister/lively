using Lively.Common;
using Lively.Common.API;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Core;
using Lively.Factories;
using Lively.Helpers;
using Lively.Models;
using Lively.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Interop;

namespace Lively.Views
{
    /// <summary>
    /// Interaction logic for WallpaperPreview.xaml
    /// </summary>
    public partial class WallpaperPreview : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        //wallpaper loader.
        private readonly ILibraryModel wallpaperData;
        private IWallpaper wallpaper;
        private bool _initializedWallpaper = false;

        private readonly IWallpaperFactory wallpaperFactory;
        private readonly IUserSettingsService userSettings;
        //private readonly IScreenRecorder recorder;

        public WallpaperPreview(ILibraryModel model)
        {
            userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            //recorder = App.Services.GetRequiredService<IScreenRecorder>();
            wallpaperFactory = App.Services.GetRequiredService<IWallpaperFactory>();
            this.wallpaperData = model;
            this.Title = model.Title;

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWallpaper(wallpaperData);
        }

        private void LoadWallpaper(ILibraryModel model)
        {
            try
            {
                IWallpaper instance = wallpaperFactory.CreateWallpaper(model, userSettings.Settings.SelectedDisplay, userSettings, true);
                model.ItemStartup = true;
                instance.WindowInitialized += WallpaperInitialized;
                instance.Show();
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                _initializedWallpaper = true;
                //TODO: show error message
            }
        }

        private void WallpaperInitialized(object sender, WindowInitializedArgs e)
        {
            try
            {
                _initializedWallpaper = true;
                wallpaper = (IWallpaper)sender;
                wallpaper.WindowInitialized -= WallpaperInitialized;
                _ = this.Dispatcher.BeginInvoke(new Action(() => {
                    //wallpaper.Model.ItemStartup = false;
                    ProgressIndicator.IsIndeterminate = false;
                }));

                if (e.Success)
                {
                    _ = this.Dispatcher.BeginInvoke(new Action(() => {
                        //attach wp hwnd to border ui element.
                        WpfUtil.SetProgramToFramework(this, wallpaper.Handle, PreviewBorder);
                        //fix for wallpaper overlapping window bordere in high dpi screens.
                        this.Width += 1;
                    }));
                }
                else
                {
                    Logger.Error("Failed launching wallpaper: " + e.Msg + "\n" + e.Error?.ToString());
                    _ = this.Dispatcher.BeginInvoke(new Action(() => {
                        this.Close();
                    }));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed processing wallpaper: " + ex);
                if (wallpaper != null)
                {
                    _ = this.Dispatcher.BeginInvoke(new Action(() => {
                        this.Close();
                    }));
                }
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (wallpaper != null)
            {
                var item = WpfUtil.GetAbsolutePlacement(PreviewBorder, true);
                NativeMethods.POINT pts = new NativeMethods.POINT() { X = (int)item.Left, Y = (int)item.Top };
                if (NativeMethods.ScreenToClient(new WindowInteropHelper(this).Handle, ref pts))
                {
                    NativeMethods.SetWindowPos(wallpaper.Handle, 1, pts.X, pts.Y, (int)item.Width, (int)item.Height, 0 | 0x0010);
                }
                this.Title = $"{(int)item.Width}x{(int)item.Height} - {wallpaperData.Title}";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_initializedWallpaper)
            {
                e.Cancel = true;
                return;
            }

            if (wallpaper != null)
            {
                //detach wallpaper window from this dialogue.
                WindowOperations.SetParentSafe(wallpaper.Handle, IntPtr.Zero);
                try
                {
                    var proc = wallpaper.Proc;
                    if (wallpaper.Category == WallpaperType.url && proc != null)
                    {
                        wallpaper.SendMessage(new LivelyCloseCmd());
                        proc.Refresh();
                        if (!proc.WaitForExit(4000))
                        {
                            wallpaper.Terminate();
                        }
                    }
                    else
                    {
                        wallpaper.Terminate();
                    }
                }
                catch
                {
                    wallpaper.Terminate();
                }
            }
        }
    }
}
