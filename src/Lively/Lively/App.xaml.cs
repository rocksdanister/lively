using CommandLine;
using GrpcDotNetNamedPipes;
using Lively.Commandline;
using Lively.Common;
using Lively.Common.Factories;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Common.Services;
using Lively.Core;
using Lively.Core.Display;
using Lively.Core.Suspend;
using Lively.Core.Watchdog;
using Lively.Factories;
using Lively.Grpc.Common.Proto.Commands;
using Lively.Grpc.Common.Proto.Desktop;
using Lively.Grpc.Common.Proto.Display;
using Lively.Grpc.Common.Proto.Update;
using Lively.Helpers;
using Lively.Models.Enums;
using Lively.Models.Services;
using Lively.RPC;
using Lively.Services;
using Lively.ViewModels;
using Lively.Views.WindowMsg;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static Lively.Common.CommandlineArgs;

namespace Lively
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly NamedPipeServer grpcServer;
        private int updateNotifyAmt = 1;
        private static Mutex mutex;

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
        public static bool IsExclusiveScreensaverMode { get; private set; }

        public App()
        {
            // Commandline args, first element is application path.
            var commandArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
            try
            {
                if (!AcquireMutex())
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
                    QuitApp();
                    return;
                }
            }
            catch (AbandonedMutexException e)
            {
                // If a thread terminates while owning a mutex, the mutex is said to be abandoned.
                // The state of the mutex is set to signaled, and the next waiting thread gets ownership.
                // Ref: https://learn.microsoft.com/en-us/dotnet/api/system.threading.mutex?view=net-8.0
                Debug.WriteLine(e.Message);
            }
            // Call release on same thread.
            this.Exit += (_, _) => ReleaseMutex();
            // Parse commands (if any) before configuring services
            if (commandArgs.Length != 0)
            {
                var opts = new ScreenSaverOptions();
                Parser.Default.ParseArguments<ScreenSaverOptions>(commandArgs)
                    .WithParsed((x) => opts = x)
                    .WithNotParsed((x) => Debug.WriteLine(x));

                if (opts.ShowExclusive != null)
                    IsExclusiveScreensaverMode = opts.ShowExclusive == true && !PackageUtil.IsRunningAsPackaged;
            }

            SetupUnhandledExceptionLogging();
            Logger.Info(LogUtil.GetHardwareInfo());

            //App() -> OnStartup() -> App.Startup event.
            _serviceProvider = ConfigureServices();
            var userSettings = Services.GetRequiredService<IUserSettingsService>();
            grpcServer = ConfigureGrpcServer();

            try
            {
                // Run startup tasks.
                Services.GetRequiredService<AppInitializer>().Run();
                // Set application language.
                Services.GetRequiredService<IResourceService>().SetCulture(userSettings.Settings.Language);
                Services.GetRequiredService<WndProcMsgWindow>().Show();
                Services.GetRequiredService<RawInputMsgWindow>().Show();
                Services.GetRequiredService<IPlayback>().Start();
                Services.GetRequiredService<ISystray>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageBox.Show(ex.ToString(), ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
                QuitApp();
                return;
            }

            Services.GetRequiredService<WndProcMsgWindow>().WindowMessageReceived += (s, args) =>
            {
                if (args.Message == (uint)NativeMethods.WM.ENDSESSION)
                {
                    QuitApp();
                }
                else if (args.Message == (uint)NativeMethods.WM.QUERYENDSESSION)
                {
                    // ENDSESSION_CLOSEAPP
                    if (args.LParam != IntPtr.Zero && args.LParam == (IntPtr)0x00000001)
                    {
                        ReleaseMutex();
                        //The app is being queried if it can close for an update.
                        _ = NativeMethods.RegisterApplicationRestart(
                            null,
                            (int)NativeMethods.RestartFlags.RESTART_NO_CRASH |
                            (int)NativeMethods.RestartFlags.RESTART_NO_HANG |
                            (int)NativeMethods.RestartFlags.RESTART_NO_REBOOT);

                        args.Result = (IntPtr)1;
                    }
                }
            };

            // System notification.
            Services.GetRequiredService<IDesktopCore>().WallpaperError += (s, e) =>
            {
                if (!Services.GetRequiredService<IRunnerService>().IsVisibleUI)
                    Services.GetRequiredService<ISystray>().ShowBalloonNotification(4000, Lively.Properties.Resources.TextError, e.Message);
            };

            if (IsExclusiveScreensaverMode)
            {
                Logger.Info("Starting in exclusive screensaver mode, skipping wallpaper restore..");
                var screenSaverService = Services.GetRequiredService<IScreensaverService>();
                screenSaverService.Stopped += (_, _) => {
                    App.QuitApp();
                };
                // Custom theme resources are not this early, make sure not to call any window or control using it.
                _ = screenSaverService.StartAsync(false);
            }
            else
            {
                // Restore wallpaper(s) from previous run.
                Services.GetRequiredService<IDesktopCore>().RestoreWallpaper();
            }

            // First run setup wizard show.
            if (userSettings.Settings.IsFirstRun)
                Services.GetRequiredService<IRunnerService>().ShowUI();

            if (userSettings.Settings.SystemTaskbarTheme != TaskbarTheme.none)
                Services.GetRequiredService<ITransparentTbService>().Start(userSettings.Settings.SystemTaskbarTheme);

            _ = WindowsStartup.TrySetStartup(userSettings.Settings.Startup);

            // Need to load theme later stage of startup to update.
            this.Startup += (s, e) => {
                ChangeTheme(userSettings.Settings.ApplicationTheme);
            };

            //Ref: https://github.com/Kinnara/ModernWpf/blob/master/ModernWpf/Helpers/ColorsHelper.cs
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) => {
                if (e.Category == Microsoft.Win32.UserPreferenceCategory.General)
                {
                    if (userSettings.Settings.ApplicationTheme == Models.Enums.AppTheme.Auto)
                    {
                        ChangeTheme(Models.Enums.AppTheme.Auto);
                    }
                }
            };

            this.SessionEnding += (s, e) => {
                if (e.ReasonSessionEnding == ReasonSessionEnding.Shutdown || e.ReasonSessionEnding == ReasonSessionEnding.Logoff)
                {
                    e.Cancel = true;
                    QuitApp();
                }
            };

            var appUpdater = Services.GetRequiredService<IAppUpdaterService>();
            appUpdater.UpdateChecked += AppUpdateChecked;
            _ = appUpdater.CheckUpdate(30 * 1000);
            appUpdater.Start();
        }

        private IServiceProvider ConfigureServices()
        {
            //TODO: Logger abstraction.
            var provider = new ServiceCollection()
                // Singleton
                .AddSingleton<IUserSettingsService, UserSettingsService>()
                .AddSingleton<IDesktopCore, WinDesktopCore>()
                .AddSingleton<IWatchdogService, WatchdogProcess>()
                .AddSingleton<IDisplayManager, DisplayManager>()
                .AddSingleton<IScreensaverService, ScreensaverService>()
                .AddSingleton<IPlayback, Playback>()
                .AddSingleton<IRunnerService, RunnerService>()
                .AddSingleton<ISystray, Systray>()
                .AddSingleton<IAppUpdaterService, GithubUpdaterService>()
                .AddSingleton<ITransparentTbService, TranslucentTBService>()
                .AddSingleton<IVirtualDesktopService, VirtualDesktopService>()
                .AddSingleton<RawInputMsgWindow>()
                .AddSingleton<WndProcMsgWindow>()
                .AddSingleton<WinDesktopCoreServer>()
                .AddSingleton<DisplayManagerServer>()
                .AddSingleton<UserSettingsServer>()
                .AddSingleton<CommandsServer>()
                .AddSingleton<AppUpdateServer>()
                .AddSingleton<IResourceService, ResourceService>()
                .AddSingleton<IWindowService, WindowService>()
                // Transient
                .AddTransient<AppInitializer>()
                .AddTransient<LibraryPreviewViewModel>()
                .AddTransient<DiagnosticViewModel>()
                .AddTransient<IWallpaperLibraryFactory, WallpaperLibraryFactory>()
                .AddTransient<IWallpaperPluginFactory, WallpaperPluginFactory>()
                .AddTransient<ILivelyPropertyFactory, LivelyPropertyFactory>()
                .AddTransient<IWebView2UserDataFactory, WebView2UserDataFactory>()
                //.AddTransient<IScreenRecorder, ScreenRecorderlibScreen>()
                .AddTransient<ICommandHandler, CommandHandler>()
                .AddTransient<IDownloadService, HttpDownloadService>()
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
            Grpc.Common.Proto.Settings.SettingsService.BindService(server.ServiceBinder, Services.GetRequiredService<UserSettingsServer>());
            DisplayService.BindService(server.ServiceBinder, Services.GetRequiredService<DisplayManagerServer>());
            CommandsService.BindService(server.ServiceBinder, Services.GetRequiredService<CommandsServer>());
            UpdateService.BindService(server.ServiceBinder, Services.GetRequiredService<AppUpdateServer>());
            server.Start();

            return server;
        }

        /// <summary>
        /// Actual apptheme, no Auto allowed.
        /// </summary>
        private static Models.Enums.AppTheme currentTheme = Models.Enums.AppTheme.Dark;
        public static void ChangeTheme(Models.Enums.AppTheme theme)
        {
            theme = theme == Models.Enums.AppTheme.Auto ? ThemeUtil.GetWindowsTheme() : theme;
            if (currentTheme == theme)
                return;

            Uri uri = theme switch
            {
                Models.Enums.AppTheme.Light => new Uri("Themes/Light.xaml", UriKind.Relative),
                Models.Enums.AppTheme.Dark => new Uri("Themes/Dark.xaml", UriKind.Relative),
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

        public static bool AcquireMutex()
        {
            mutex = new Mutex(true, Constants.SingleInstance.UniqueAppName, out bool mutexCreated);
            if (!mutexCreated)
            {
                mutex = null;
                return false;
            }
            return true;
        }

        public static void ReleaseMutex()
        {
            mutex?.ReleaseMutex();
            mutex?.Close();
            mutex = null;
        }

        public static void QuitApp()
        {
            try
            {
                ((ServiceProvider)App.Services)?.Dispose();
            }
            catch (InvalidOperationException) { /* not initialised */ }
            ((App)Current).grpcServer?.Dispose();
            // Shutdown needs to be called from dispatcher.
            Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
        }
    }
}
