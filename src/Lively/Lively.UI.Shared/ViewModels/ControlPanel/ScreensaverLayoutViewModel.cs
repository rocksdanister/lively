using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Factories;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class ScreensaverLayoutViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDisplayManagerClient displayManager;
        private readonly IDispatcherService dispatcher;
        private readonly IWallpaperLibraryFactory wallpaperLibraryFactory;
        private readonly LibraryViewModel libraryVm;

        private readonly List<ScreenSaverLayoutModel> screenSaverLayout;

        public ScreensaverLayoutViewModel(IUserSettingsClient userSettings,
            IDesktopCoreClient desktopCore,
            IDisplayManagerClient displayManager,
            IWallpaperLibraryFactory wallpaperLibraryFactory,
            IDispatcherService dispatcher,
            LibraryViewModel libraryVm)
        {
            this.wallpaperLibraryFactory = wallpaperLibraryFactory;
            this.displayManager = displayManager;
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.libraryVm = libraryVm;
            this.dispatcher = dispatcher;

            IsScreensaverLockOnResume = userSettings.Settings.ScreensaverLockOnResume;
            SelectedScreensaverArrangementIndex = (int)userSettings.Settings.ScreensaverArragement;
            SelectedScreensaverTypeIndex = (int)userSettings.Settings.ScreensaverType;
            SelectedDisplay = userSettings.Settings.SelectedDisplay;
            screenSaverLayout = GetScreensaverConfigFile();
            IsScreensaverPluginNotify = !IsScreensaverPluginExists() && userSettings.Settings.IsScreensaverPluginNotify;
            UpdateLayout();

            // This event is also fired when monitor configuration changed.
            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
        }

        [ObservableProperty]
        private bool isHideDialog;

        /// <summary>
        /// We are only saving the selection temporarily during the runtime of the transient class.
        /// </summary>
        [ObservableProperty]
        private DisplayMonitor selectedDisplay;

        [ObservableProperty]
        private ObservableCollection<ScreenLayoutModel> screenItems = [];

        private ScreenLayoutModel _selectedItem;
        public ScreenLayoutModel SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value is null)
                    return;

                SetProperty(ref _selectedItem, value);
                RemoveScreensaverCommand.NotifyCanExecuteChanged();

                if (!SelectedDisplay.Equals(value.Screen))
                    SelectedDisplay = value.Screen;
            }
        }

        [ObservableProperty]
        private WallpaperArrangement selectedScreensaverArrangement;

        private int _selectedScreensaverArrangementIndex;
        public int SelectedScreensaverArrangementIndex
        {
            get => _selectedScreensaverArrangementIndex;
            set
            {
                if (userSettings.Settings.ScreensaverArragement != (WallpaperArrangement)value && value != -1)
                {
                    userSettings.Settings.ScreensaverArragement = (WallpaperArrangement)value;
                    UpdateSettingsConfigFile();

                    RemoveScreensaverCommand.NotifyCanExecuteChanged();
                    UpdateLayout();
                }
                SetProperty(ref _selectedScreensaverArrangementIndex, value);
                SelectedScreensaverArrangement = userSettings.Settings.ScreensaverType != ScreensaverType.wallpaper ? 
                    (WallpaperArrangement)value : SelectedScreensaverArrangement;
            }
        }

        private int _selectedScreensaverTypeIndex;
        public int SelectedScreensaverTypeIndex
        {
            get => _selectedScreensaverTypeIndex;
            set
            {
                if (userSettings.Settings.ScreensaverType != (ScreensaverType)value && value != -1)
                {
                    userSettings.Settings.ScreensaverType = (ScreensaverType)value;
                    UpdateSettingsConfigFile();

                    SelectedScreensaverArrangement = userSettings.Settings.ScreensaverType != ScreensaverType.wallpaper ?
                        userSettings.Settings.ScreensaverArragement : userSettings.Settings.WallpaperArrangement;

                    RemoveScreensaverCommand.NotifyCanExecuteChanged();
                    UpdateLayout();
                }
                SetProperty(ref _selectedScreensaverTypeIndex, value);
                IsScreensaverLayout = (ScreensaverType)value != ScreensaverType.wallpaper;
            }
        }

        [RelayCommand(CanExecute = nameof(IsScreensaverLayout))]
        private async Task AddScreensaver()
        {
            try
            {
                IsHideDialog = true;
                var model = await libraryVm.SelectItem();
                if (model is null)
                    return;

                var layout = userSettings.Settings.ScreensaverArragement switch
                {
                    WallpaperArrangement.per => screenSaverLayout.Find(x => x.Layout == WallpaperArrangement.per)?.Wallpapers?.Find(x => x.Display.Equals(SelectedItem.Screen)),
                    WallpaperArrangement.span => screenSaverLayout.Find(x => x.Layout == WallpaperArrangement.span)?.Wallpapers.FirstOrDefault(),
                    WallpaperArrangement.duplicate => screenSaverLayout.Find(x => x.Layout == WallpaperArrangement.duplicate)?.Wallpapers.FirstOrDefault(),
                    _ => throw new NotImplementedException(),
                };
                // If already exists change path only and update layout.
                if (layout is not null)
                    layout.LivelyInfoPath = model.LivelyInfoFolderPath;
                else
                {
                    // Create new entry.
                    switch (userSettings.Settings.ScreensaverArragement)
                    {
                        case WallpaperArrangement.per:
                            {
                                // For this arrangement we check if the ScreenlayoutModel already exists and if so add to the wallpaper array directly.
                                var perLayout = screenSaverLayout.Find(x => x.Layout == WallpaperArrangement.per);
                                if (perLayout is null)
                                {
                                    screenSaverLayout.Add(new ScreenSaverLayoutModel()
                                    {
                                        Layout = userSettings.Settings.ScreensaverArragement,
                                        Wallpapers =
                                        [
                                            new(SelectedItem.Screen, model.LivelyInfoFolderPath)
                                        ]
                                    });
                                }
                                else
                                {
                                    perLayout.Wallpapers.Add(new(SelectedItem.Screen, model.LivelyInfoFolderPath));
                                }
                            }
                            break;
                        case WallpaperArrangement.span:
                        case WallpaperArrangement.duplicate:
                            {
                                screenSaverLayout.Add(new ScreenSaverLayoutModel()
                                {
                                    Layout = userSettings.Settings.ScreensaverArragement,
                                    Wallpapers =
                                    [
                                        new(displayManager.PrimaryMonitor, model.LivelyInfoFolderPath)
                                    ]
                                });
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            finally
            {
                IsHideDialog = false;
            }
            UpdateScreensaverConfigFile();
            RemoveScreensaverCommand.NotifyCanExecuteChanged();
            UpdateLayout();
        }

        [RelayCommand(CanExecute = nameof(CanRemoveScreensaver))]
        private void RemoveScreensaver()
        {
            switch (userSettings.Settings.ScreensaverArragement)
            {
                case WallpaperArrangement.per:
                    {
                        screenSaverLayout.Find(x => x.Layout == WallpaperArrangement.per)?.Wallpapers?.RemoveAll(x => x.Display.Equals(SelectedItem.Screen));
                        var item = ScreenItems.FirstOrDefault(x => x.Screen.Equals(SelectedItem.Screen));
                        if (item is not null)
                            item.ScreenImagePath = null;
                    }
                    break;
                case WallpaperArrangement.span:
                    {
                        screenSaverLayout.RemoveAll(x => x.Layout == WallpaperArrangement.span);
                        foreach (var item in ScreenItems)
                            item.ScreenImagePath = null;
                    }
                    break;
                case WallpaperArrangement.duplicate:
                    {
                        screenSaverLayout.RemoveAll(x => x.Layout == WallpaperArrangement.duplicate);
                        foreach (var item in ScreenItems)
                            item.ScreenImagePath = null;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            UpdateScreensaverConfigFile();
            RemoveScreensaverCommand.NotifyCanExecuteChanged();
        }

        private bool CanRemoveScreensaver()
        {
            if (userSettings.Settings.ScreensaverType != ScreensaverType.different)
                return false;

            return userSettings.Settings.ScreensaverArragement switch
            {
                WallpaperArrangement.per => screenSaverLayout.Exists(x => x.Layout == WallpaperArrangement.per && x.Wallpapers != null &&  x.Wallpapers.Exists(x => x.Display.Equals(SelectedItem.Screen))),
                WallpaperArrangement.span => screenSaverLayout.Exists(x => x.Layout == WallpaperArrangement.span),
                WallpaperArrangement.duplicate => screenSaverLayout.Exists(x => x.Layout == WallpaperArrangement.duplicate),
                _ => throw new NotImplementedException(),
            };
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddScreensaverCommand))]
        private bool isScreensaverLayout;

        private bool _isScreensaverLockOnResume;
        public bool IsScreensaverLockOnResume
        {
            get => _isScreensaverLockOnResume;
            set
            {
                if (userSettings.Settings.ScreensaverLockOnResume != value)
                {
                    userSettings.Settings.ScreensaverLockOnResume = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isScreensaverLockOnResume, value);
            }
        }

        [ObservableProperty]
        private bool isScreensaverPluginNotify;

        [RelayCommand]
        private void CloseScreensaverPluginNotify()
        {
            IsScreensaverPluginNotify = false;
            userSettings.Settings.IsScreensaverPluginNotify = false;
            UpdateSettingsConfigFile();
        }

        public void OnWindowClosing()
        {
            desktopCore.WallpaperChanged -= DesktopCore_WallpaperChanged;
        }

        private void DesktopCore_WallpaperChanged(object sender, EventArgs e)
        {
            dispatcher.TryEnqueue(UpdateLayout);
        }

        private void UpdateLayout()
        {
            ScreenItems.Clear();
            switch (userSettings.Settings.ScreensaverType)
            {
                case ScreensaverType.wallpaper:
                    {
                        foreach (var item in displayManager.DisplayMonitors)
                        {
                            // Same as running wallpaper based on the wallpaper arrangement.
                            var wallpaper = userSettings.Settings.WallpaperArrangement switch
                            {
                                WallpaperArrangement.per => desktopCore.Wallpapers.FirstOrDefault(x => item.Equals(x.Display)),
                                WallpaperArrangement.span => desktopCore.Wallpapers.FirstOrDefault(),
                                WallpaperArrangement.duplicate => desktopCore.Wallpapers.FirstOrDefault(),
                                _ => throw new NotImplementedException(),
                            };
                            ScreenItems.Add(new ScreenLayoutModel(item,
                                string.IsNullOrEmpty(wallpaper?.PreviewPath) ? wallpaper?.ThumbnailPath : wallpaper.PreviewPath,
                                null,
                            string.Empty));
                        }
                    }
                    break;
                case ScreensaverType.different:
                    {                       
                        foreach (var item in displayManager.DisplayMonitors)
                        {
                            var screensaver = screenSaverLayout is null ? null : userSettings.Settings.ScreensaverArragement switch
                            {
                                WallpaperArrangement.per => screenSaverLayout.Find(x => x.Layout == WallpaperArrangement.per)?.Wallpapers?.Find(x => item.Equals(x.Display)),
                                WallpaperArrangement.span => screenSaverLayout.Find(x => x.Layout == WallpaperArrangement.span)?.Wallpapers?.FirstOrDefault(),
                                WallpaperArrangement.duplicate => screenSaverLayout.Find(x => x.Layout == WallpaperArrangement.duplicate)?.Wallpapers?.FirstOrDefault(),
                                _ => throw new NotImplementedException(),
                            };

                            LibraryModel wallpaper = null;
                            try
                            {
                                if (screensaver is not null)
                                    wallpaper = wallpaperLibraryFactory.CreateFromDirectory(screensaver.LivelyInfoPath);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Screensaver {screensaver?.LivelyInfoPath} loading failed. | {ex}");
                                // Remove screensaver from collection but not saving the state to disk since its not required here (performance reasons.) 
                                switch (userSettings.Settings.ScreensaverArragement)
                                {
                                    case WallpaperArrangement.per:
                                        if (screensaver is not null)
                                            screenSaverLayout?.Find(x => x.Layout == WallpaperArrangement.per)?.Wallpapers?.Remove(screensaver);
                                        break;
                                    case WallpaperArrangement.span:
                                        screenSaverLayout?.RemoveAll(x => x.Layout == WallpaperArrangement.span);
                                        break;
                                    case WallpaperArrangement.duplicate:
                                        screenSaverLayout?.RemoveAll(x => x.Layout == WallpaperArrangement.duplicate);
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }

                            ScreenItems.Add(new ScreenLayoutModel(item,
                                string.IsNullOrEmpty(wallpaper?.PreviewClipPath) ? wallpaper?.ThumbnailPath : wallpaper.PreviewClipPath,
                                null,
                            string.Empty));
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            // Fallback to default selection in the event display configuration changed.
            SelectedItem = ScreenItems.FirstOrDefault(x => x.Screen.Equals(SelectedDisplay)) 
                ?? ScreenItems.FirstOrDefault(x => x.Screen.Equals(userSettings.Settings.SelectedDisplay))
                ?? ScreenItems.FirstOrDefault(x => x.Screen.IsPrimary);
        }

        private void UpdateScreensaverConfigFile()
        {
            JsonStorage<List<ScreenSaverLayoutModel>>.StoreData(Constants.CommonPaths.ScreenSaverLayoutPath, screenSaverLayout);
        }

        private static List<ScreenSaverLayoutModel> GetScreensaverConfigFile()
        {
            try
            {
                if (File.Exists(Constants.CommonPaths.ScreenSaverLayoutPath))
                    return JsonStorage<List<ScreenSaverLayoutModel>>.LoadData(Constants.CommonPaths.ScreenSaverLayoutPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return [];
        }

        private void UpdateSettingsConfigFile()
        {
            dispatcher.TryEnqueue(userSettings.Save<SettingsModel>);
        }

        private static bool IsScreensaverPluginExists()
        {
            try
            {
                return File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Lively.scr"));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return false;
        }
    }
}
