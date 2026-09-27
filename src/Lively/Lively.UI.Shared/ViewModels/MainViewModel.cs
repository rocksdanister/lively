using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lively.Common;
using Lively.Common.Exceptions;
using Lively.Common.Helpers.Files;
using Lively.Common.Services;
using Lively.Gallery.Client;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.Models.Enums;
using Lively.Models.Services;
using Lively.Models.UserControls;
using Lively.UI.Shared.Factories;
using Lively.UI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lively.UI.Shared.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IUserSettingsClient userSettings;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IDisplayManagerClient displayManager;
        private readonly IDispatcherService dispatcher;
        private readonly IAppThemeFactory themeFactory;
        private readonly IAppUpdaterClient appUpdater;
        private readonly GalleryClient galleryClient;
        private readonly IDialogService dialogService;
        private readonly LibraryViewModel libraryVm;
        private readonly IFileService fileService;
        private readonly IResourceService i18n;
        private readonly IMainNavigator navigator;
        private readonly IDownloadService downloader;
        private readonly ICommandsClient commandsClient;

        private CancellationTokenSource wallpaperImportCts;

        public MainViewModel(IUserSettingsClient userSettings,
                             IDesktopCoreClient desktopCore,
                             IDispatcherService dispatcher,
                             IDisplayManagerClient displayManager,
                             ICommandsClient commandsClient,
                             IAppThemeFactory themeFactory,
                             GalleryClient galleryClient,
                             IDownloadService downloader,
                             IDialogService dialogService,
                             IAppUpdaterClient appUpdater,
                             IFileService fileService,
                             LibraryViewModel libraryVm,
                             IResourceService i18n,
                             IMainNavigator navigator)
        {
            this.commandsClient = commandsClient;
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;
            this.galleryClient = galleryClient;
            this.dialogService = dialogService;
            this.themeFactory = themeFactory;
            this.dispatcher = dispatcher;
            this.fileService = fileService;
            this.appUpdater = appUpdater;
            this.downloader = downloader;
            this.libraryVm = libraryVm;
            this.navigator = navigator;
            this.i18n = i18n;

            desktopCore.WallpaperChanged += DesktopCore_WallpaperChanged;
            desktopCore.WallpaperError += DesktopCore_WallpaperError;
            navigator.ContentPageChanged += Navigator_ContentPageChanged;
            appUpdater.UpdateChecked += AppUpdater_UpdateChecked;
            galleryClient.LoggedOut += GalleryClient_LoggedOut;
            galleryClient.LoggedIn += GalleryClient_LoggedIn;
            i18n.CultureChanged += I18n_CultureChanged;

            // For SelectedItem: OpenHomeCommand() in NavigationView.Loaded since INavigator.Frame needs to be initialized.
            MenuItems = new(GetPages());
            WallpaperCount = desktopCore.Wallpapers.Count;
            IsUpdatedNotify = userSettings.Settings.IsUpdatedNotify;
            IsUpdateAvailable = appUpdater.Status == AppUpdateStatus.available;
            SearchPlaceholderText = GetSearchPlaceholderText();

            i18n.SetCulture(userSettings.Settings.Language);
            _ = SetAppTheme(userSettings.Settings.ApplicationThemeBackground);
            _ = GalleryInit();

            if (!desktopCore.IsCoreInitialized)
                ShowError(new WorkerWException(i18n.GetString("LivelyExceptionWorkerWSetupFail")));
        }

        [ObservableProperty]
        private ObservableCollection<MainNavigationItem> menuItems;

        private MainNavigationItem selectedMenuItem;
        public MainNavigationItem SelectedMenuItem
        {
            get => selectedMenuItem;
            set
            {
                SetProperty(ref selectedMenuItem, value);

                if (selectedMenuItem != null)
                    navigator.NavigateTo(selectedMenuItem.PageType);
            }
        }

        [ObservableProperty]
        private string searchPlaceholderText;

        [ObservableProperty]
        private bool isWebView2InstallNotify;

        [ObservableProperty]
        private bool isWebView2Installing;

        [ObservableProperty]
        private bool isSettingsPage;

        [ObservableProperty]
        private bool isLoggedIn;

        [ObservableProperty]
        private string displayUserName;

        [ObservableProperty]
        private Uri displayAvatar;

        [ObservableProperty]
        private bool isUpdatedNotify;

        [ObservableProperty]
        private bool isUpdateAvailable;

        [ObservableProperty]
        private string appThemeBackground = null;

        [ObservableProperty]
        private bool isControlPanelTeachingTipOpen;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WallpaperCountMessage))]
        [NotifyPropertyChangedFor(nameof(WallpaperCountGlyph))]
        private int wallpaperCount;

        public string WallpaperCountMessage => $"{WallpaperCount} {i18n.GetString("ActiveWallpapers/Label")}";

        public string WallpaperCountGlyph => monitorGlyphs[WallpaperCount >= monitorGlyphs.Length ? monitorGlyphs.Length - 1 : WallpaperCount];

        [ObservableProperty]
        private InAppNotificationModel errorNotification = new();

        [ObservableProperty]
        private InAppNotificationModel importNotification = new();

        [RelayCommand]
        private async Task InstallWebView2()
        {
            try
            {
                IsWebView2Installing = true;

                if (await WebViewUtil.InstallWebView2(downloader))
                    _ = commandsClient.RestartUI("--appUpdate true");
                else
                    LinkUtil.OpenBrowser(WebViewUtil.DownloadUrl);
            }
            finally
            {
                IsWebView2Installing = false;
            }
        }

        [RelayCommand]
        private void OpenSettings()
        {
            navigator.NavigateTo(ContentPageType.settingsGeneral);
        }

        [RelayCommand]
        private void OpenHome()
        {
            navigator.NavigateTo(ContentPageType.library);
        }

        private void I18n_CultureChanged(object sender, string e)
        {
            SearchPlaceholderText = GetSearchPlaceholderText();
            foreach (var item in MenuItems)
                item.Name = GetPageName(item.PageType);

            navigator.Reload();
        }

        private void AppUpdater_UpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            dispatcher.TryEnqueue(() =>
            {
                IsUpdateAvailable = e.UpdateStatus == AppUpdateStatus.available;
                if (IsSettingsPage)
                    return;

                MenuItems.First(x => x.PageType == ContentPageType.appupdate).IsAlert = IsUpdateAvailable;
            });
        }

        private void DesktopCore_WallpaperError(object sender, Exception ex)
        {
            _ = dispatcher.TryEnqueue(() =>
            {
                if (ex is WallpaperWebView2NotFoundException)
                    IsWebView2InstallNotify = true;
                else
                    ShowError(ex);
            });
        }

        private void ShowError(Exception ex)
        {
            ErrorNotification ??= new();
            ErrorNotification.Title = i18n.GetString("TextError");
            ErrorNotification.Message = $"{ex.Message}\n\nException:\n{ex.GetType().Name}";
            ErrorNotification.IsOpen = true;
        }

        private void DesktopCore_WallpaperChanged(object sender, System.EventArgs e)
        {
            _ = dispatcher.TryEnqueue(() =>
            {
                // Update active wallpaper status
                WallpaperCount = desktopCore.Wallpapers.Count;

                // Update theme if require
                if (userSettings.Settings.ApplicationThemeBackground == Models.Enums.AppThemeBackground.dynamic)
                    _ = SetAppTheme(userSettings.Settings.ApplicationThemeBackground);

                // Show TeachingTip once
                if (!userSettings.Settings.ControlPanelOpened)
                {
                    IsControlPanelTeachingTipOpen = true;
                    userSettings.Settings.ControlPanelOpened = true;
                    userSettings.Save<SettingsModel>();
                }
            });
        }

        private void Navigator_ContentPageChanged(object sender, Models.Enums.ContentPageType e)
        {
            // Update state
            IsSettingsPage = e switch
            {
                ContentPageType.library => false,
                ContentPageType.gallery => false,
                ContentPageType.appupdate => false,
                ContentPageType.settingsGeneral => true,
                ContentPageType.settingsPerformance => true,
                ContentPageType.settingsWallpaper => true,
                ContentPageType.settingsSystem => true,
                ContentPageType.settingsScreensaver => true,
                _ => throw new NotImplementedException(),
            };
            // Update selection UI if navigation is called by code.
            if (SelectedMenuItem == null || SelectedMenuItem.PageType != e)
                SelectedMenuItem = MenuItems.FirstOrDefault(x => x.PageType == e);
        }

        [RelayCommand]
        private async Task OpenControlPanel()
        {
            await dialogService.ShowControlPanelDialogAsync();
        }

        [RelayCommand]
        private void OpenUpdate()
        {
            navigator.NavigateTo(ContentPageType.appupdate);
        }

        [RelayCommand]
        private async Task OpenAppTheme()
        {
            await dialogService.ShowThemeDialogAsync();
        }

        [RelayCommand]
        private async Task OpenHelp()
        {
            await dialogService.ShowHelpDialogAsync();
        }

        [RelayCommand]
        private async Task OpenAbout()
        {
            await dialogService.ShowAboutDialogAsync();
        }

        [RelayCommand]
        private async Task OpenAddWallpaper()
        {
            var result = await dialogService.ShowAddWallpaperDialogAsync();
            switch (result.wallpaperType)
            {
                case WallpaperAddType.url:
                    {
                        var model = await libraryVm.AddWallpaperLink(result.wallpapers.FirstOrDefault(), true);
                        if (model != null)
                            navigator.NavigateTo(ContentPageType.library);
                    }
                    break;
                case WallpaperAddType.files:
                    {
                        if (result.wallpapers.Count == 1)
                        {
                            navigator.NavigateTo(ContentPageType.library);
                            await CreateWallpaper(result.wallpapers[0]);
                        }
                        else if (result.wallpapers.Count > 1)
                        {
                            navigator.NavigateTo(ContentPageType.library);
                            await AddWallpapers(result.wallpapers);
                        }
                    }
                    break;
                case WallpaperAddType.create:
                    {
                        await CreateWallpaper(null);
                    }
                    break;
                case WallpaperAddType.none:
                default:
                    break;
            }
        }

        public async Task CreateWallpaper(string filePath)
        {
            var creationType = await dialogService.ShowWallpaperCreateDialogAsync(filePath);
            if (creationType is null)
                return;

            switch (creationType)
            {
                case WallpaperCreateType.none:
                    {
                        await AddWallpaper(filePath);
                    }
                    break;
                case WallpaperCreateType.depthmap:
                    {
                        filePath ??= (await fileService.PickFileAsync(WallpaperType.picture)).FirstOrDefault();
                        if (filePath is not null)
                        {
                            var result = await dialogService.ShowDepthWallpaperDialogAsync(filePath);
                            if (result is not null)
                                await desktopCore.SetWallpaper(result, userSettings.Settings.SelectedDisplay);
                        }
                    }
                    break;
            }
        }

        public async Task<LibraryModel> AddWallpaper(string filePath)
        {
            LibraryModel result = null;
            try
            {
                if (Path.GetExtension(filePath) == ".zip" && FileUtil.IsFileGreater(filePath, 10485760))
                {
                    ImportNotification.IsOpen = true;
                    ImportNotification.Message = Path.GetFileName(filePath);
                    ImportNotification.IsProgressIndeterminate = true;
                }
                result = await libraryVm.AddWallpaperFile(filePath, true);
            }
            catch (Exception ex)
            {
                await dialogService.ShowDialogAsync(ex.Message,
                    i18n.GetString("TextError"),
                    i18n.GetString("TextOk"));
            }
            finally
            {
                ImportNotification.IsOpen = false;
                ImportNotification.Message = null;
                ImportNotification.IsProgressIndeterminate = false;
            }
            return result;
        }

        public async Task AddWallpapers(List<string> files)
        {
            try
            {
                ImportNotification.IsOpen = true;
                ImportNotification.Message = "0%";
                ImportNotification.Title = i18n.GetString("TextProcessingWallpaper");

                CanCancelWallpaperImport = true;
                AddWallpapersCancelCommand.NotifyCanExecuteChanged();

                wallpaperImportCts = new CancellationTokenSource();
                await libraryVm.AddWallpapers(files, wallpaperImportCts.Token, new Progress<int>(percent =>
                {
                    ImportNotification.Message = $"{percent}%";
                    ImportNotification.Progress = percent;
                }));
            }
            finally
            {
                ImportNotification.IsOpen = false;
                ImportNotification.Progress = 0;
                wallpaperImportCts?.Dispose();
                wallpaperImportCts = null;

                CanCancelWallpaperImport = false;
                AddWallpapersCancelCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanCancelWallpaperImport))]
        private void AddWallpapersCancel()
        {
            if (wallpaperImportCts is null)
                return;

            CanCancelWallpaperImport = false;
            AddWallpapersCancelCommand.NotifyCanExecuteChanged();

            ImportNotification.Title = i18n.GetString("PleaseWait/Text");
            ImportNotification.Message = "100%";
            wallpaperImportCts.Cancel();
        }

        private bool CanCancelWallpaperImport { get; set; } = false;

        public async Task SetAppTheme(AppThemeBackground appTheme)
        {
            switch (appTheme)
            {
                case Models.Enums.AppThemeBackground.default_mica:
                case Models.Enums.AppThemeBackground.default_acrylic:
                    {
                        AppThemeBackground = null;
                    }
                    break;
                case Models.Enums.AppThemeBackground.dynamic:
                    {
                        if (desktopCore.Wallpapers.Any())
                        {
                            var wallpaper = desktopCore.Wallpapers.FirstOrDefault(x => x.Display.Equals(displayManager.PrimaryMonitor));
                            if (wallpaper is null)
                            {
                                AppThemeBackground = null;
                            }
                            else
                            {
                                var userThemeDir = Path.Combine(wallpaper.LivelyInfoFolderPath, "lively_theme");
                                if (Directory.Exists(userThemeDir))
                                {
                                    string themeFile = null;
                                    try
                                    {
                                        themeFile = themeFactory.CreateFromDirectory(userThemeDir).File;
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
                            AppThemeBackground = null;
                        }
                    }
                    break;
                case Models.Enums.AppThemeBackground.custom:
                    {
                        if (!string.IsNullOrWhiteSpace(userSettings.Settings.ApplicationThemeBackgroundPath))
                        {
                            string themeFile = null;
                            try
                            {
                                var theme = themeFactory.CreateFromDirectory(userSettings.Settings.ApplicationThemeBackgroundPath);
                                themeFile = theme.Type == ThemeType.picture ? theme.File : null;
                            }
                            catch { }
                            AppThemeBackground = themeFile;
                        }
                    }
                    break;
                default:
                    {
                        AppThemeBackground = null;
                    }
                    break;
            }
        }

        private async Task GalleryInit()
        {
            try
            {
                await galleryClient.InitializeAsync();
            }
            catch (UnauthorizedAccessException ex1)
            {
                Logger.Info($"Skipping login: {ex1?.Message}");
            }
            catch (Exception ex2)
            {
                Logger.Error($"Failed to login: {ex2}");
            }
        }

        private void GalleryClient_LoggedOut(object sender, object e)
        {
            dispatcher.TryEnqueue(() =>
            {
                IsLoggedIn = false;
                // Close authorized flyout.
            });
        }

        private void GalleryClient_LoggedIn(object sender, object e)
        {
            dispatcher.TryEnqueue(() =>
            {
                IsLoggedIn = true;
                navigator.NavigateTo(ContentPageType.library);
                DisplayUserName = galleryClient.CurrentUser.DisplayName;
                if (LinkUtil.TrySanitizeUrl(galleryClient.CurrentUser.AvatarUrl, out Uri uri))
                    DisplayAvatar = uri;
            });
        }

        [RelayCommand]
        private void GalleryAuth()
        {
            navigator.NavigateTo(ContentPageType.gallery);
            // Close unauthorized flyout.
        }

        [RelayCommand]
        private async Task GalleryLogout()
        {
            await galleryClient.LogoutAsync();
        }

        [RelayCommand]
        private async Task GalleryEditProfile()
        {
            // Close authorized flyout.
            await dialogService.ShowGalleryEditProfileDialogAsync();
        }

        private MainNavigationItem[] GetPages()
        {
            return [
                new() { Name = GetPageName(ContentPageType.library), Glyph = "\uE8A9", PageType = ContentPageType.library},
                new() { Name = GetPageName(ContentPageType.gallery), Glyph = "\uE719", PageType = ContentPageType.gallery },
                new()
                {
                    Name = GetPageName(ContentPageType.appupdate),
                    Glyph = "\uE777",
                    PageType = ContentPageType.appupdate,
                    IsAlert = appUpdater.Status == AppUpdateStatus.available,
                    Alert = 1
                },
                new() { Name = GetPageName(ContentPageType.settingsGeneral), PageType = ContentPageType.settingsGeneral },
                new() { Name = GetPageName(ContentPageType.settingsPerformance), PageType = ContentPageType.settingsPerformance },
                new() { Name = GetPageName(ContentPageType.settingsWallpaper), PageType = ContentPageType.settingsWallpaper },
                new() { Name = GetPageName(ContentPageType.settingsScreensaver), PageType = ContentPageType.settingsScreensaver },
                new() { Name = GetPageName(ContentPageType.settingsSystem), PageType = ContentPageType.settingsSystem }
            ];
        }

        private string GetSearchPlaceholderText() => i18n.GetString("SearchBox/PlaceholderText");

        private string GetPageName(ContentPageType pageType)
        {
            return pageType switch
            {
                ContentPageType.library => i18n.GetString("TitleLibrary"),
                ContentPageType.gallery => i18n.GetString("TitleGallery"),
                ContentPageType.appupdate => i18n.GetString("TitleUpdates"),
                ContentPageType.settingsGeneral => i18n.GetString("TitleGeneral"),
                ContentPageType.settingsPerformance => i18n.GetString("TitlePerformance"),
                ContentPageType.settingsWallpaper => i18n.GetString("TitleWallpaper/Content"),
                ContentPageType.settingsScreensaver => i18n.GetString("TitleScreensaver/Content"),
                ContentPageType.settingsSystem => i18n.GetString("System/Text"),
                _ => throw new NotImplementedException(),
            };
        }

        private readonly string[] monitorGlyphs = [
            "\uE900",
            "\uE901",
            "\uE902",
            "\uE903",
            "\uE904",
            "\uE905",
        ];
    }
}
