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
using System.Threading.Tasks;
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
        private readonly LibraryModel wallpaperData;
        private IWallpaper wallpaper;
        private bool isInitialized = false;

        private readonly IWallpaperPluginFactory wallpaperFactory;
        private readonly IUserSettingsService userSettings;

        public WallpaperPreview(LibraryModel model)
        {
            userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            wallpaperFactory = App.Services.GetRequiredService<IWallpaperPluginFactory>();
            this.wallpaperData = model;
            this.Title = model.Title;

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ = LoadWallpaper(wallpaperData);
        }

        private async Task LoadWallpaper(LibraryModel model)
        {
            try
            {
                wallpaper = wallpaperFactory.CreateWallpaper(model, userSettings.Settings.SelectedDisplay, userSettings, true);
                model.ItemStartup = true;
                await wallpaper.ShowAsync();

                isInitialized = true;
                ProgressIndicator.IsIndeterminate = false;
                //Attach wp hwnd to border ui element.
                WpfUtil.SetProgramToFramework(this, wallpaper.Handle, PreviewBorder);
                //Fix for wallpaper overlapping window bordere in high dpi screens.
                this.Width += 1;
            }
            catch (Exception e)
            {
                // TODO: Show user error message
                Logger.Error(e.ToString());
                // Allow dialog close
                isInitialized = true;
                // Close dialog and exit wallpaper
                this.Close();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (wallpaper is null)
                return;

            var item = WpfUtil.GetAbsolutePlacement(PreviewBorder, true);
            NativeMethods.POINT pts = new NativeMethods.POINT() { X = (int)item.Left, Y = (int)item.Top };
            if (NativeMethods.ScreenToClient(new WindowInteropHelper(this).Handle, ref pts))
            {
                NativeMethods.SetWindowPos(wallpaper.Handle, 1, pts.X, pts.Y, (int)item.Width, (int)item.Height, 0 | 0x0010);
            }
            this.Title = $"{(int)item.Width}x{(int)item.Height} - {wallpaperData.Title}";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!isInitialized)
            {
                e.Cancel = true;
                return;
            }

            if (wallpaper is null)
                return;

            //Detach wallpaper window from this dialogue.
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
