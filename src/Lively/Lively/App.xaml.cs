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
using Lively.Common.Helpers.Network;
using System.Windows.Threading;
using Lively.Views;
using Lively.Grpc.Common.Proto.Update;
using Lively.Common.Services;
using Lively.Common.Helpers.Files;
using Lively.Common.Helpers.Archive;
using Lively.Models;
using Lively.Common.Helpers;
using Lively.Helpers.Theme;
using Microsoft.Win32;

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

        public App()
        {
            try
            {
                //wait a few seconds in case application instance is just shutting down..
                if (!mutex.WaitOne(TimeSpan.FromSeconds(1), false))
                {
                    try
                    {
                        //skipping first element (application path.)
                        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
                        var client = new CommandsService.CommandsServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName));
                        var request = new AutomationCommandRequest();
                        request.Args.AddRange(args.Length != 0 ? args : new string[] { "--showApp", "true" });
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
                //create directories if not exist, eg: C:\Users\<User>\AppData\Local
                Directory.CreateDirectory(Constants.CommonPaths.AppDataDir);
                Directory.CreateDirectory(Constants.CommonPaths.LogDir);
                Directory.CreateDirectory(Constants.CommonPaths.TempDir);
                Directory.CreateDirectory(Constants.CommonPaths.TempCefDir);
                Directory.CreateDirectory(Constants.CommonPaths.TempVideoDir);
            }
            catch (Exception ex)
            {
                //nothing much can be done here..
                MessageBox.Show(ex.Message, "AppData Directory Initialize Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                //clear temp files from previous run if any..
                FileOperations.EmptyDirectory(Constants.CommonPaths.TempDir);
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
                userSettings.Save<ISettingsModel>();
            }

            Services.GetRequiredService<WndProcMsgWindow>().Show();
            Services.GetRequiredService<RawInputMsgWindow>().Show();
            Services.GetRequiredService<IPlayback>().Start();
            Services.GetRequiredService<ISystray>();

            if (userSettings.Settings.IsUpdated)
            {
                //install any new wallpaper collection if present, do this before restoring wallpaper incase installed wallpaper is updated.
                //first run default wallpapers are installed by UI to avoid slow startup times and better user experience.
                var maxVersion = ExtractWallpaperBundle(userSettings.Settings.WallpaperBundleVersion,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundle"),
                    userSettings.Settings.WallpaperDir);
                if (maxVersion != userSettings.Settings.WallpaperBundleVersion)
                {
                    userSettings.Settings.WallpaperBundleVersion = maxVersion;
                    userSettings.Save<ISettingsModel>();
                }
            }

            //restore wallpaper(s) from previous run.
            Services.GetRequiredService<IDesktopCore>().RestoreWallpaper();

            //first run Setup-Wizard show..
            if (userSettings.Settings.IsFirstRun)
            {
                Services.GetRequiredService<IRunnerService>().ShowUI();
            }

            //need to load theme later stage of startu to update..
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

#if DEBUG != true
            var appUpdater = Services.GetRequiredService<IAppUpdaterService>();
            appUpdater.UpdateChecked += AppUpdateChecked;
            _ = appUpdater.CheckUpdate();
            appUpdater.Start();
#endif
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
                //transient
                //.AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
                .AddTransient<IWallpaperFactory, WallpaperFactory>()
                .AddTransient<ILivelyPropertyFactory, LivelyPropertyFactory>()
                //.AddTransient<IScreenRecorder, ScreenRecorderlibScreen>()
                .AddTransient<ICommandHandler, CommandHandler>()
                .AddTransient<IDownloadHelper, MultiDownloadHelper>()
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
            server.Start();

            return server;
        }

        /// <summary>
        /// Actual apptheme, no Auto allowed.
        /// </summary>
        private static Common.AppTheme _currentTheme = Common.AppTheme.Dark;
        public static void ChangeTheme(Common.AppTheme theme)
        {
            theme = theme == Common.AppTheme.Auto ? ThemeUtil.GetWindowsTheme() : theme;
            if (_currentTheme == theme)
                return;

            _currentTheme = theme;
            Uri uri = theme switch
            {
                Common.AppTheme.Light => new Uri("Themes/Light.xaml", UriKind.Relative),
                Common.AppTheme.Dark => new Uri("Themes/Dark.xaml", UriKind.Relative),
                _ => new Uri("Themes/Dark.xaml", UriKind.Relative)
            };

            try
            {
                ResourceDictionary resourceDict = Application.LoadComponent(uri) as ResourceDictionary;
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(resourceDict);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            Logger.Info($"Theme changed: {theme}");
        }

        //number of times to notify user about update.
        private static int updateNotifyAmt = 1;
        private static bool updateNotify = false;
        private void AppUpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            var sysTray = Services.GetRequiredService<ISystray>();
            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                if (e.UpdateStatus == AppUpdateStatus.available)
                {
                    if (updateNotifyAmt > 0)
                    {
                        updateNotifyAmt--;
                        updateNotify = true;
                        sysTray?.ShowBalloonNotification(4000,
                            "Lively Wallpaper",
                            Lively.Properties.Resources.TextUpdateAvailable);
                    }

                    //If UI program already running then notification is displayed withing the it.
                    if (!Services.GetRequiredService<IRunnerService>().IsVisibleUI && updateNotify)
                    {
                        AppUpdateDialog(e.UpdateUri, e.ChangeLog);
                    }
                }
                Logger.Info($"AppUpdate status: {e.UpdateStatus}");
            }));
        }

        private static AppUpdater updateWindow;
        public static void AppUpdateDialog(Uri uri, string changelog)
        {
            updateNotify = false;
            if (updateWindow == null)
            {
                updateWindow = new AppUpdater(uri, changelog)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                updateWindow.Closed += (s, e) => { updateWindow = null; };
                updateWindow.Show();
            }
        }

        private void CreateWallpaperDir(string baseDirectory)
        {
            Directory.CreateDirectory(Path.Combine(baseDirectory, Constants.CommonPartialPaths.WallpaperInstallDir));
            Directory.CreateDirectory(Path.Combine(baseDirectory, Constants.CommonPartialPaths.WallpaperInstallTempDir));
            Directory.CreateDirectory(Path.Combine(baseDirectory, Constants.CommonPartialPaths.WallpaperSettingsDir));
        }

        /// <summary>
        /// Extract default wallpapers if any
        /// </summary>
        public static int ExtractWallpaperBundle(int currentBundleVer, string currentBundleDir, string currentWallpaperDir)
        {
            int maxExtracted = currentBundleVer;
            try
            {
                //wallpaper bundles filenames are 0.zip, 1.zip ...
                var sortedBundles = Directory.GetFiles(currentBundleDir).OrderBy(x => x);

                foreach (var item in sortedBundles)
                {
                    if (int.TryParse(Path.GetFileNameWithoutExtension(item), out int val))
                    {
                        if (val > maxExtracted)
                        {
                            //will overwrite files if exists during extraction.
                            ZipExtract.ZipExtractFile(item, Path.Combine(currentWallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir), false);
                            maxExtracted = val;
                        }
                    }
                }
            }
            catch { /* TODO */ }
            return maxExtracted;
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
