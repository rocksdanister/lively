using GrpcDotNetNamedPipes;
using Lively.Common;
using Lively.Core;
using Lively.Core.Display;
using Lively.Core.Suspend;
using Lively.Core.Watchdog;
using Lively.Factories;
using Lively.Grpc.Common.Proto.Desktop;
using Lively.RPC;
using Lively.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Lively.Grpc.Common.Proto.Settings;
using System.Threading.Tasks;
using Lively.Grpc.Common.Proto.Display;
using Lively.Grpc.Common.Proto.Commands;
using System.Linq;
using Lively.Automation;
using Lively.Views.WindowMsg;
using System.Windows.Threading;
using Lively.Views;
using Lively.Grpc.Common.Proto.Update;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Archive;
using Lively.Models;
using Lively.Common.Helpers;
using Microsoft.Win32;
using System.Reflection;
using Lively.Common.Models;
using Lively.Common.Services.Update;
using Lively.Helpers;
using Lively.Common.Services.Downloader;
using Lively.ViewModels;
using System.Collections.Generic;
using Lively.Common.Extensions;
using CommandLine;
using static Lively.Common.AutomationArgs;

namespace Lively
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly Mutex mutex = new Mutex(false, Constants.SingleInstance.UniqueAppName);
        private readonly NamedPipeServer grpcServer;
        private int updateNotifyAmt = 1;

        private readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance for the current application instance.
        /// </summary>
        public static IServiceProvider Services
        {
            get
            {
                IServiceProvider serviceProvider = ((App)Current)._serviceProvider;
                return serviceProvider ?? throw new InvalidOperationException("The service provider is not initialized");
            }
        }
        public bool IsExclusiveScreensaverMode { get; }

        public App()
        {
            // Commandline args, first element is application path.
            var commandArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
            try
            {
                // Wait a few seconds in case application instance is just shutting down..
                // Note: This wait is longer required since Core is never restarted after 2.0 rewrite.
                if (!mutex.WaitOne(0, false))
                {
                    try
                    {
                        // If another instance is running, communicate with it and then exit.
                        var client = new CommandsService.CommandsServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));
                        var request = new AutomationCommandRequest();
                        // If no argument assume user opened via icon and show interface.
                        request.Args.AddRange(commandArgs.Length != 0 ? commandArgs : ["--showApp", "true"]);
                        _ = client.AutomationCommandAsync(request);
                    }
                    catch (Exception e)
                    {
                        _ = MessageBox.Show($"Failed to communicate with Core:\n{e.Message}", "Lively Wallpaper");
                    }
                    ShutDown();
                    return;
                }
            }
            catch (AbandonedMutexException e)
            {
                //unexpected app termination.
                Debug.WriteLine(e.Message);
            }

            SetupUnhandledExceptionLogging();
            Logger.Info(LogUtil.GetHardwareInfo());

            //App() -> OnStartup() -> App.Startup event.
            _serviceProvider = ConfigureServices();
            grpcServer = ConfigureGrpcServer();

            try
            {
                //clear temp files from previous run if any..
                FileUtil.EmptyDirectory(Constants.CommonPaths.TempDir);
                FileUtil.EmptyDirectory(Constants.CommonPaths.ThemeCacheDir);
                FileUtil.EmptyDirectory(Constants.CommonPaths.CefRootCacheDir);
            }
            catch { /* TODO */ }

            try
            {
                //create directories if not exist, eg: C:\Users\<User>\AppData\Local
                Directory.CreateDirectory(Constants.CommonPaths.AppDataDir);
                Directory.CreateDirectory(Constants.CommonPaths.LogDir);
                Directory.CreateDirectory(Constants.CommonPaths.ThemeDir);
                Directory.CreateDirectory(Constants.CommonPaths.TempDir);
                Directory.CreateDirectory(Constants.CommonPaths.TempCefDir);
                Directory.CreateDirectory(Constants.CommonPaths.TempVideoDir);
                Directory.CreateDirectory(Constants.CommonPaths.ThemeCacheDir);
            }
            catch (Exception ex)
            {
                //nothing much can be done here..
                MessageBox.Show(ex.Message, "AppData directory creation failed, exiting Lively..", MessageBoxButton.OK, MessageBoxImage.Error);
                ShutDown();
                return;
            }

            try
            {
                //default livelyproperty for media files..
                var mediaProperty = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "api", "LivelyProperties.json");
                if (File.Exists(mediaProperty))
                {
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "api", "LivelyProperties.json"),
                        Path.Combine(Constants.CommonPaths.TempVideoDir, "LivelyProperties.json"), true);
                }
            }
            catch { /* TODO */ }

            var userSettings = Services.GetRequiredService<IUserSettingsService>();
            try
            {
                CreateWallpaperDir(userSettings.Settings.WallpaperDir);
            }
            catch (Exception ex)
            {
                Logger.Error($"Wallpaper directory setup failed: {ex.Message}, falling back to default.");
                userSettings.Settings.WallpaperDir = Path.Combine(Constants.CommonPaths.AppDataDir, "Library");
                CreateWallpaperDir(userSettings.Settings.WallpaperDir);
                userSettings.Save<SettingsModel>();
            }

            Services.GetRequiredService<WndProcMsgWindow>().Show();
            Services.GetRequiredService<RawInputMsgWindow>().Show();
            Services.GetRequiredService<IPlayback>().Start();
            Services.GetRequiredService<ISystray>();

            //Install any new asset collection if present, do this before restoring wallpaper incase wallpaper is updated.
            if (userSettings.Settings.IsUpdated || userSettings.Settings.IsFirstRun)
            {
                SplashWindow spl = new(0, 500); 
                spl.Show();

                // Install default wallpapers or updates.
                var maxWallpaper = ZipExtract.ExtractAssetBundle(userSettings.Settings.WallpaperBundleVersion,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundle", "wallpapers"),
                    Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir));
                var maxTheme = ZipExtract.ExtractAssetBundle(userSettings.Settings.ThemeBundleVersion,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundle", "themes"),
                    Path.Combine(Constants.CommonPaths.ThemeDir));
                if (maxTheme != userSettings.Settings.ThemeBundleVersion || maxWallpaper != userSettings.Settings.WallpaperBundleVersion)
                {
                    userSettings.Settings.WallpaperBundleVersion = maxWallpaper;
                    userSettings.Settings.ThemeBundleVersion = maxTheme;
                    userSettings.Save<SettingsModel>();
                }

                // Mpv property file changed in v2.1, delete user data.
                if (userSettings.Settings.IsUpdated
                    && !string.IsNullOrWhiteSpace(userSettings.Settings.AppPreviousVersion)
                    && new Version(userSettings.Settings.AppPreviousVersion) < new Version(2, 1, 0, 0))
                {
                    var wallpaperLibraryFactory = Services.GetRequiredService<IWallpaperLibraryFactory>();
                    var dir = new List<string>();
                    string[] folderPaths = {
                        Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir),
                        Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallTempDir)
                    };
                    for (int i = 0; i < folderPaths.Count(); i++)
                    {
                        try
                        {
                            dir.AddRange(Directory.GetDirectories(folderPaths[i], "*", SearchOption.TopDirectoryOnly));
                        }
                        catch { /* TODO */ }
                    }

                    for (int i = 0; i < dir.Count; i++)
                    {
                        try
                        {
                            var metadata = wallpaperLibraryFactory.GetMetadata(dir[i]);
                            if (metadata.Type.IsMediaWallpaper())
                            {
                                var dataFolder = Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperSettingsDir);
                                var wallpaperDataFolder = Path.Combine(dataFolder, new DirectoryInfo(dir[i]).Name);
                                if (Directory.Exists(wallpaperDataFolder))
                                    Directory.Delete(wallpaperDataFolder, true);
                            }
                        }
                        catch { }
                    }
                }

                spl.Close();
            }

            if (commandArgs.Length != 0)
            {
                var opts = new ScreenSaverOptions();
                Parser.Default.ParseArguments<ScreenSaverOptions>(commandArgs)
                    .WithParsed((x) => opts = x)
                    .WithNotParsed((x) => Debug.WriteLine(x));

                if (opts.ShowExclusive != null)
                    IsExclusiveScreensaverMode = opts.ShowExclusive == true;
            }

            if (IsExclusiveScreensaverMode)
            {
                Logger.Info("Starting in exclusive screensaver mode, skipping wallpaper restore..");
                // TODO: Call screensaver.
            }
            else
            {
                // Restore wallpaper(s) from previous run.
                Services.GetRequiredService<IDesktopCore>().RestoreWallpaper();
            }

            // First run setup wizard show.
            if (userSettings.Settings.IsFirstRun)
                Services.GetRequiredService<IRunnerService>().ShowUI();

            // Need to load theme later stage of startup to update.
            this.Startup += (s, e) => {
                ChangeTheme(userSettings.Settings.ApplicationTheme);
            };

            //Ref: https://github.com/Kinnara/ModernWpf/blob/master/ModernWpf/Helpers/ColorsHelper.cs
            SystemEvents.UserPreferenceChanged += (s, e) => {
                if (e.Category == UserPreferenceCategory.General)
                {
                    if (userSettings.Settings.ApplicationTheme == Common.AppTheme.Auto)
                    {
                        ChangeTheme(Common.AppTheme.Auto);
                    }
                }
            };

            this.SessionEnding += (s, e) => {
                if (e.ReasonSessionEnding == ReasonSessionEnding.Shutdown || e.ReasonSessionEnding == ReasonSessionEnding.Logoff)
                {
                    e.Cancel = true;
                    ShutDown();
                }
            };

#if !DEBUG
            var appUpdater = Services.GetRequiredService<IAppUpdaterService>();
            appUpdater.UpdateChecked += AppUpdateChecked;
            _ = appUpdater.CheckUpdate(30 * 1000);
            appUpdater.Start();
#endif
            Debug.WriteLine("App Update checking disabled in DEBUG mode.");
        }

        private IServiceProvider ConfigureServices()
        {
            //TODO: Logger abstraction.
            var provider = new ServiceCollection()
                //singleton
                .AddSingleton<IUserSettingsService, JsonUserSettingsService>()
                .AddSingleton<IDesktopCore, WinDesktopCore>()
                .AddSingleton<IWatchdogService, WatchdogProcess>()
                .AddSingleton<IDisplayManager, DisplayManager>()
                .AddSingleton<IScreensaverService, ScreensaverService>()
                .AddSingleton<IPlayback, Playback>()
                .AddSingleton<IRunnerService, RunnerService>()
                .AddSingleton<ISystray, Systray>()
                .AddSingleton<IAppUpdaterService, GithubUpdaterService>()
                .AddSingleton<ITransparentTbService, TranslucentTBService>()
                .AddSingleton<RawInputMsgWindow>()
                .AddSingleton<WndProcMsgWindow>()
                .AddSingleton<WinDesktopCoreServer>()
                .AddSingleton<DisplayManagerServer>()
                .AddSingleton<UserSettingsServer>()
                .AddSingleton<CommandsServer>()
                .AddSingleton<AppUpdateServer>()
                .AddSingleton<WallpaperPlaylistServer>()
                .AddSingleton<IDialogService, DialogService>()
                //transient
                //.AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
                .AddTransient<IWallpaperLibraryFactory, WallpaperLibraryFactory>()
                .AddTransient<IWallpaperPluginFactory, WallpaperPluginFactory>()
                .AddTransient<ILivelyPropertyFactory, LivelyPropertyFactory>()
                //.AddTransient<IScreenRecorder, ScreenRecorderlibScreen>()
                .AddTransient<ICommandHandler, CommandHandler>()
                .AddTransient<IDownloadService, SimpleDownloadService>()
                //https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
                .AddHttpClient()
                //.AddTransient<SetupView>()
                /*
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with
                NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog("Nlog.config");
                })
                */
                .BuildServiceProvider();

            return provider;
        }

        private NamedPipeServer ConfigureGrpcServer()
        {
            var server = new NamedPipeServer(Constants.SingleInstance.GrpcPipeServerName);
            DesktopService.BindService(server.ServiceBinder, Services.GetRequiredService<WinDesktopCoreServer>());
            SettingsService.BindService(server.ServiceBinder, Services.GetRequiredService<UserSettingsServer>());
            DisplayService.BindService(server.ServiceBinder, Services.GetRequiredService<DisplayManagerServer>());
            CommandsService.BindService(server.ServiceBinder, Services.GetRequiredService<CommandsServer>());
            UpdateService.BindService(server.ServiceBinder, Services.GetRequiredService<AppUpdateServer>());
            PlaylistService.BindService(server.ServiceBinder, Services.GetRequiredService<WallpaperPlaylistServer>());
            server.Start();

            return server;
        }

        /// <summary>
        /// Actual apptheme, no Auto allowed.
        /// </summary>
        private static Common.AppTheme currentTheme = Common.AppTheme.Dark;
        public static void ChangeTheme(Common.AppTheme theme)
        {
            theme = theme == Common.AppTheme.Auto ? ThemeUtil.GetWindowsTheme() : theme;
            if (currentTheme == theme)
                return;

            Uri uri = theme switch
            {
                Common.AppTheme.Light => new Uri("Themes/Light.xaml", UriKind.Relative),
                Common.AppTheme.Dark => new Uri("Themes/Dark.xaml", UriKind.Relative),
                _ => new Uri("Themes/Dark.xaml", UriKind.Relative)
            };

            try
            {
                // WPF theme
                ResourceDictionary resourceDict = Application.LoadComponent(uri) as ResourceDictionary;
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(resourceDict);
                // Tray theme
                Services.GetRequiredService<ISystray>().SetTheme(theme);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            Logger.Info($"Theme changed: {theme}");
            currentTheme = theme;
        }

        private void AppUpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                Logger.Info($"AppUpdate status: {e.UpdateStatus}");
                if (e.UpdateStatus != AppUpdateStatus.available || updateNotifyAmt <= 0)
                    return;

                updateNotifyAmt--;
                // If interface is visible then skip (shown in-app instead.)
                if (!Services.GetRequiredService<IRunnerService>().IsVisibleUI)
                {
                    Services.GetRequiredService<ISystray>().ShowBalloonNotification(4000,
                        "Lively Wallpaper",
                        Lively.Properties.Resources.TextUpdateAvailable);
                }
            }));
        }

        private void CreateWallpaperDir(string baseDirectory)
        {
            Directory.CreateDirectory(Path.Combine(baseDirectory, Constants.CommonPartialPaths.WallpaperInstallDir));
            Directory.CreateDirectory(Path.Combine(baseDirectory, Constants.CommonPartialPaths.WallpaperInstallTempDir));
            Directory.CreateDirectory(Path.Combine(baseDirectory, Constants.CommonPartialPaths.WallpaperSettingsDir));
        }

        private void SetupUnhandledExceptionLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            Dispatcher.UnhandledException += (s, e) =>
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

            //ref: https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler.unobservedtaskexception?redirectedfrom=MSDN&view=net-6.0
            TaskScheduler.UnobservedTaskException += (s, e) => {
                //LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
            };
        }

        private void LogUnhandledException(Exception exception, string source) => Logger.Error(exception);

        public static void ShutDown()
        {
            try
            {
                ((ServiceProvider)App.Services)?.Dispose();
            }
            catch (InvalidOperationException) { /* not initialised */ }
            ((App)Current).grpcServer?.Dispose();
            //Shutdown needs to be called from dispatcher..
            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
        }
    }
}
