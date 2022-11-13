using CommunityToolkit.Mvvm.ComponentModel;
using Lively.Grpc.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Lively.Models;
using Lively.UI.WinUI.Factories;
using Lively.Common;
using Microsoft.UI.Xaml.Controls;

namespace Lively.UI.WinUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DispatcherQueue dispatcherQueue;
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDisplayManagerClient displayManager;
        private readonly IThemeFactory themeFactory;

        public MainViewModel(IUserSettingsClient userSettings, IDesktopCoreClient desktopCore, IDisplayManagerClient displayManager, IThemeFactory themeFactory)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.themeFactory = themeFactory;
            //MainWindow dispatcher may not be ready yet, creating our own instead..
            dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueueController.CreateOnCurrentThread().DispatcherQueue;
            _ = UpdateTheme();

            desktopCore.WallpaperChanged += (_, _) =>
            {
                if (userSettings.Settings.ApplicationThemeBackground == Common.AppThemeBackground.dynamic)
                {
                    _ = dispatcherQueue.TryEnqueue(() =>
                    {
                        _ = UpdateTheme();
                    });
                }
            };

            IsUpdated = userSettings.Settings.IsUpdated;
        }

        [ObservableProperty]
        private bool isUpdated;

        [ObservableProperty]
        private string appThemeBackground = string.Empty;

        public async Task UpdateTheme()
        {
            switch (userSettings.Settings.ApplicationThemeBackground)
            {
                case Common.AppThemeBackground.default_mica:
                case Common.AppThemeBackground.default_acrylic:
                    {
                        AppThemeBackground = string.Empty;
                    }
                    break;
                case Common.AppThemeBackground.dynamic:
                    {
                        if (desktopCore.Wallpapers.Any())
                        {
                            var wallpaper = desktopCore.Wallpapers.FirstOrDefault(x => x.Display.Equals(displayManager.PrimaryMonitor));
                            if (wallpaper is null)
                            {
                                AppThemeBackground = string.Empty;
                            }
                            else
                            {
                                var userThemeDir = Path.Combine(wallpaper.LivelyInfoFolderPath, "lively_theme");
                                if (Directory.Exists(userThemeDir))
                                {
                                    var themeFile = string.Empty;
                                    try
                                    {
                                        themeFile = themeFactory.CreateTheme(userThemeDir).File;
                                    }
                                    catch { }
                                    AppThemeBackground = themeFile;
                                }
                                else
                                {
                                    var fileName = new DirectoryInfo(wallpaper.LivelyInfoFolderPath).Name + ".jpg";
                                    var filePath = Path.Combine(Constants.CommonPaths.ThemeCacheDir, fileName);
                                    if (!File.Exists(filePath))
                                    {
                                        await desktopCore.TakeScreenshot(desktopCore.Wallpapers[0].Display.DeviceId, filePath);
                                    }
                                    AppThemeBackground = filePath;
                                }
                            }
                        }
                        else
                        {
                            AppThemeBackground = string.Empty;
                        }
                    }
                    break;
                case Common.AppThemeBackground.custom:
                    {
                        if (!string.IsNullOrWhiteSpace(userSettings.Settings.ApplicationThemeBackgroundPath))
                        {
                            var themeFile = string.Empty;
                            try
                            {
                                var theme = themeFactory.CreateTheme(userSettings.Settings.ApplicationThemeBackgroundPath);
                                themeFile = theme.Type == ThemeType.picture ? theme.File : string.Empty;
                            }
                            catch { }
                            AppThemeBackground = themeFile;
                        }
                    }
                    break;
                default:
                    {
                        AppThemeBackground = string.Empty;
                    }
                    break;
            }
        }
    }
}
