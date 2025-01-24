using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Factories;
using Lively.Common.Helpers.Shell;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using Lively.UI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class SettingsWallpaperViewModel : ObservableObject
    {
        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDialogService dialogService;
        private readonly IDispatcherService dispatcher;
        private readonly IDownloadService downloader;
        private readonly IApplicationsRulesFactory appRuleFactory;
        private readonly ICommandsClient commandsClient;

        public SettingsWallpaperViewModel(IUserSettingsClient userSettings,
            IDesktopCoreClient desktopCore,
            IDialogService dialogService,
            IDispatcherService dispatcher,
            ICommandsClient commandsClient,
            IDownloadService downloader,
            IApplicationsRulesFactory appRuleFactory)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.dialogService = dialogService;
            this.appRuleFactory = appRuleFactory;
            this.commandsClient = commandsClient;
            this.dispatcher = dispatcher;
            this.downloader = downloader;

            AppMusicExclusionRules = new ObservableCollection<AppMusicExclusionRuleModel>(GetAppMusicExclusionRule());
            IsDesktopAutoWallpaper = userSettings.Settings.DesktopAutoWallpaper;
            //IsLockScreenAutoWallpaper = userSettings.Settings.LockScreenAutoWallpaper;
            SelectedWallpaperScalingIndex = (int)userSettings.Settings.WallpaperScaling;
            SelectedWallpaperInputMode = (int)userSettings.Settings.InputForward;
            MouseMoveOnDesktop = userSettings.Settings.MouseInputMovAlways;
            SelectedVideoPlayerIndex = (int)userSettings.Settings.VideoPlayer;
            VideoPlayerHWDecode = userSettings.Settings.VideoPlayerHwAccel;
            SelectedGifPlayerIndex = (int)userSettings.Settings.GifPlayer;
            SelectedWebBrowserIndex = (int)userSettings.Settings.WebBrowser;
            WebDebuggingPort = userSettings.Settings.WebDebugPort;
            CefDiskCache = userSettings.Settings.CefDiskCache;
            SelectedWallpaperStreamQualityIndex = (int)userSettings.Settings.StreamQuality;
            DetectStreamWallpaper = userSettings.Settings.AutoDetectOnlineStreams;
        }

        private bool _isDesktopAutoWallpaper;
        public bool IsDesktopAutoWallpaper
        {
            get => _isDesktopAutoWallpaper;
            set
            {
                if (userSettings.Settings.DesktopAutoWallpaper != value)
                {
                    userSettings.Settings.DesktopAutoWallpaper = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _isDesktopAutoWallpaper, value);
            }
        }

        //private bool _isLockScreenAutoWallpaper;
        //public bool IsLockScreenAutoWallpaper
        //{
        //    get => _isLockScreenAutoWallpaper;
        //    set
        //    {
        //        if (userSettings.Settings.LockScreenAutoWallpaper != value)
        //        {
        //            userSettings.Settings.LockScreenAutoWallpaper = value;
        //            UpdateSettingsConfigFile();
        //        }
        //        SetProperty(ref _isLockScreenAutoWallpaper, value);
        //    }
        //}

        private int _selectedWallpaperScalingIndex;
        public int SelectedWallpaperScalingIndex
        {
            get => _selectedWallpaperScalingIndex;
            set
            {
                if (userSettings.Settings.WallpaperScaling != (WallpaperScaler)value)
                {
                    userSettings.Settings.WallpaperScaling = (WallpaperScaler)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart([WallpaperType.video, WallpaperType.picture, WallpaperType.videostream, WallpaperType.gif]);
                }
                SetProperty(ref _selectedWallpaperScalingIndex, value);
            }
        }

        private int _selectedWallpaperInputMode;
        public int SelectedWallpaperInputMode
        {
            get => _selectedWallpaperInputMode;
            set
            {
                if (userSettings.Settings.InputForward != (InputForwardMode)value)
                {
                    var newSetting = (InputForwardMode)value;
                    var previousSetting = userSettings.Settings.InputForward;
                    userSettings.Settings.InputForward = newSetting;
                    UpdateSettingsConfigFile();

                    if (newSetting == InputForwardMode.mousekeyboard)
                        DesktopUtil.SetDesktopIconVisibility(false);
                    else if (previousSetting == InputForwardMode.mousekeyboard)
                        DesktopUtil.SetDesktopIconVisibility(true);
                }
                IsDesktopIconsHidden = userSettings.Settings.InputForward == InputForwardMode.mousekeyboard;
                SetProperty(ref _selectedWallpaperInputMode, value);
            }
        }

        [ObservableProperty]
        private bool isDesktopIconsHidden;

        private bool _mouseMoveOnDesktop;
        public bool MouseMoveOnDesktop
        {
            get => _mouseMoveOnDesktop;
            set
            {
                if (userSettings.Settings.MouseInputMovAlways != value)
                {
                    userSettings.Settings.MouseInputMovAlways = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _mouseMoveOnDesktop, value);
            }
        }

        [ObservableProperty]
        private bool isSelectedVideoPlayerAvailable;

        [ObservableProperty]
        private bool isBackwardCompatibilityWallpaperScaler;

        private int _selectedVideoPlayerIndex;
        public int SelectedVideoPlayerIndex
        {
            get => _selectedVideoPlayerIndex;
            set
            {
                IsSelectedVideoPlayerAvailable = IsVideoPlayerAvailable((LivelyMediaPlayer)value);
                // Only mpv supports scaler settings in customise menu, enable global scaler menu for older players.
                IsBackwardCompatibilityWallpaperScaler = IsSelectedVideoPlayerAvailable && (LivelyMediaPlayer)value != LivelyMediaPlayer.mpv;

                if (userSettings.Settings.VideoPlayer != (LivelyMediaPlayer)value && IsSelectedVideoPlayerAvailable)
                {
                    userSettings.Settings.VideoPlayer = (LivelyMediaPlayer)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart([WallpaperType.video, WallpaperType.picture, WallpaperType.videostream]);
                }
                SetProperty(ref _selectedVideoPlayerIndex, value);
            }
        }

        private bool _videoPlayerHWDecode;
        public bool VideoPlayerHWDecode
        {
            get => _videoPlayerHWDecode;
            set
            {
                if (userSettings.Settings.VideoPlayerHwAccel != value)
                {
                    userSettings.Settings.VideoPlayerHwAccel = value;
                    UpdateSettingsConfigFile();
                    //if mpv player is also set as gif player..
                    _ = WallpaperRestart([WallpaperType.video, WallpaperType.videostream, WallpaperType.gif]);
                }
                SetProperty(ref _videoPlayerHWDecode, value);
            }
        }

        [ObservableProperty]
        private bool isSelectedGifPlayerAvailable;

        private int _selectedGifPlayerIndex;
        public int SelectedGifPlayerIndex
        {
            get => _selectedGifPlayerIndex;
            set
            {
                IsSelectedGifPlayerAvailable = IsGifPlayerAvailable((LivelyGifPlayer)value);
                if (userSettings.Settings.GifPlayer != (LivelyGifPlayer)value && IsSelectedGifPlayerAvailable)
                {
                    userSettings.Settings.GifPlayer = (LivelyGifPlayer)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart([WallpaperType.gif, WallpaperType.picture]);
                }
                SetProperty(ref _selectedGifPlayerIndex, value);
            }
        }

        [ObservableProperty]
        private bool isSelectedWebBrowserAvailable;

        private int _selectedWebBrowserIndex;
        public int SelectedWebBrowserIndex
        {
            get => _selectedWebBrowserIndex;
            set
            {
                IsSelectedWebBrowserAvailable = IsWebPlayerAvailable((LivelyWebBrowser)value);
                IsWebView2Required = !WebViewUtil.IsWebView2Available() && IsSelectedWebBrowserAvailable && ((LivelyWebBrowser)value) == LivelyWebBrowser.webview2;
                if (userSettings.Settings.WebBrowser != (LivelyWebBrowser)value && IsSelectedWebBrowserAvailable)
                {
                    userSettings.Settings.WebBrowser = (LivelyWebBrowser)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart([WallpaperType.web, WallpaperType.webaudio, WallpaperType.url, WallpaperType.videostream]);
                }
                SetProperty(ref _selectedWebBrowserIndex, value);
            }
        }

        [ObservableProperty]
        private bool isWebView2Required;

        [ObservableProperty]
        private bool isWebView2Installing;

        [RelayCommand]
        private async Task InstallWebView2()
        {
            try
            {
                IsWebView2Installing = true;

                if (await WebViewUtil.InstallWebView2(downloader))
                {
                    // Restart wallpaper
                    _ = WallpaperRestart([WallpaperType.web, WallpaperType.webaudio, WallpaperType.url, WallpaperType.videostream]);
                    // Restart to reload WebView2 (Update changelog page.)
                    _ = commandsClient.RestartUI();
                }
                else
                {
                    LinkUtil.OpenBrowser(WebViewUtil.DownloadUrl);
                }
            }
            finally
            {
                IsWebView2Installing = false;
            }
        }

        private string _webDebuggingPort;
        public string WebDebuggingPort
        {
            get => _webDebuggingPort;
            set
            {
                if (userSettings.Settings.WebDebugPort != value)
                {
                    userSettings.Settings.WebDebugPort = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _webDebuggingPort, value);
            }
        }

        private bool _cefDiskCache;
        public bool CefDiskCache
        {
            get => _cefDiskCache;
            set
            {
                if (userSettings.Settings.CefDiskCache != value)
                {
                    userSettings.Settings.CefDiskCache = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _cefDiskCache, value);
            }
        }

        public bool IsStreamSupported
        {
            get
            {
                try
                {
                    return File.Exists(Path.Combine(desktopCore.BaseDirectory, "plugins", "mpv", "youtube-dl.exe"));
                }
                catch
                {
                    return false;
                }
            }
        }

        private int _selectedWallpaperStreamQualityIndex;
        public int SelectedWallpaperStreamQualityIndex
        {
            get => _selectedWallpaperStreamQualityIndex;
            set
            {
                if (userSettings.Settings.StreamQuality != (StreamQualitySuggestion)value)
                {
                    userSettings.Settings.StreamQuality = (StreamQualitySuggestion)value;
                    UpdateSettingsConfigFile();
                    _ = WallpaperRestart([WallpaperType.videostream]);
                }
                SetProperty(ref _selectedWallpaperStreamQualityIndex, value);
            }
        }

        private bool _detectStreamWallpaper;
        public bool DetectStreamWallpaper
        {
            get => _detectStreamWallpaper;
            set
            {
                if (userSettings.Settings.AutoDetectOnlineStreams != value)
                {
                    userSettings.Settings.AutoDetectOnlineStreams = value;
                    UpdateSettingsConfigFile();
                }
                SetProperty(ref _detectStreamWallpaper, value);
            }
        }

        #region apprules

        [ObservableProperty]
        private bool isAppMusicExclusionRuleChanged;

        [ObservableProperty]
        private ObservableCollection<AppMusicExclusionRuleModel> appMusicExclusionRules;

        private AppMusicExclusionRuleModel _selectedAppMusicExclusionRuleItem;
        public AppMusicExclusionRuleModel SelectedAppMusicExclusionRuleItem
        {
            get => _selectedAppMusicExclusionRuleItem;
            set
            {
                SetProperty(ref _selectedAppMusicExclusionRuleItem, value);
                RemoveAppMusicExclusionRuleCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand]
        private async Task AddAppMusicExclusionRule()
        {
            var result = await dialogService.ShowApplicationPickerDialogAsync();
            if (result is null)
                return;

            try
            {
                var rule = appRuleFactory.CreateAppMusicExclusionRule(result.AppPath);
                if (AppMusicExclusionRules.Any(x => x.AppName.Equals(rule.AppName, StringComparison.Ordinal)))
                    return;

                AppMusicExclusionRules.Add(rule);
                IsAppMusicExclusionRuleChanged = true;
                UpdateAppMusicExclusionRule();
            }
            catch { /* Failed to parse program information, ignore. */ }
        }

        [RelayCommand(CanExecute = nameof(CanRemoveAppRule))]
        private void RemoveAppMusicExclusionRule()
        {
            AppMusicExclusionRules.Remove(SelectedAppMusicExclusionRuleItem);
            IsAppMusicExclusionRuleChanged = true;
            UpdateAppMusicExclusionRule();
        }

        private bool CanRemoveAppRule => SelectedAppMusicExclusionRuleItem != null;

        [RelayCommand]
        private async Task RestartMusicWallpapers()
        {
            IsAppMusicExclusionRuleChanged = false;
            await WallpaperRestart([WallpaperType.web, WallpaperType.webaudio]);
        }

        private void UpdateAppMusicExclusionRule()
        {
            JsonStorage<List<AppMusicExclusionRuleModel>>.StoreData(Constants.CommonPaths.MusicAppExclusionRulesPath, AppMusicExclusionRules.ToList());
        }

        private static List<AppMusicExclusionRuleModel> GetAppMusicExclusionRule()
        {
            try
            {
                if (File.Exists(Constants.CommonPaths.MusicAppExclusionRulesPath))
                    return JsonStorage<List<AppMusicExclusionRuleModel>>.LoadData(Constants.CommonPaths.MusicAppExclusionRulesPath);
            }
            catch { /* Ignore */ }
            return [];
        }

        #endregion //apprules

        public void UpdateSettingsConfigFile()
        {
            _ = dispatcher.TryEnqueue(userSettings.Save<SettingsModel>);
        }

        private bool IsVideoPlayerAvailable(LivelyMediaPlayer mp)
        {
            return mp switch
            {
                LivelyMediaPlayer.libvlc => false, //depreciated
                LivelyMediaPlayer.libmpv => false, //depreciated
                LivelyMediaPlayer.wmf => File.Exists(Path.Combine(desktopCore.BaseDirectory, Constants.PlayerPartialPaths.WmfPath)),
                LivelyMediaPlayer.libvlcExt => false,
                LivelyMediaPlayer.libmpvExt => false,
                LivelyMediaPlayer.mpv => File.Exists(Path.Combine(desktopCore.BaseDirectory, Constants.PlayerPartialPaths.MpvPath)),
                LivelyMediaPlayer.vlc => File.Exists(Path.Combine(desktopCore.BaseDirectory, Constants.PlayerPartialPaths.VlcPath)),
                _ => false,
            };
        }

        private bool IsGifPlayerAvailable(LivelyGifPlayer gp)
        {
            return gp switch
            {
                LivelyGifPlayer.win10Img => false, //xaml island
                LivelyGifPlayer.libmpvExt => false,
                LivelyGifPlayer.mpv => File.Exists(Path.Combine(desktopCore.BaseDirectory, Constants.PlayerPartialPaths.MpvPath)),
                _ => false,
            };
        }

        private bool IsWebPlayerAvailable(LivelyWebBrowser wp)
        {
            return wp switch
            {
                LivelyWebBrowser.cef => File.Exists(Path.Combine(desktopCore.BaseDirectory, Constants.PlayerPartialPaths.CefSharpPath)),
                LivelyWebBrowser.webview2 => File.Exists(Path.Combine(desktopCore.BaseDirectory, Constants.PlayerPartialPaths.WebView2Path)),
                _ => false,
            };
        }

        private async Task WallpaperRestart(WallpaperType[] type)
        {
            var originalWallpapers = desktopCore.Wallpapers.Where(x => type.Any(y => y == x.Category)).ToList();
            if (originalWallpapers.Any())
            {
                foreach (var item in type)
                {
                    await desktopCore.CloseWallpaper(item, true);
                }

                foreach (var item in originalWallpapers)
                {
                    await desktopCore.SetWallpaper(item.LivelyInfoFolderPath, item.Display.DeviceId);
                    if (userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span
                        || userSettings.Settings.WallpaperArrangement == WallpaperArrangement.duplicate)
                    {
                        break;
                    }
                }
            }
        }
    }
}
