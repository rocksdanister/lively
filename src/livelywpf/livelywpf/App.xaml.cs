using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using livelywpf.Helpers.Files;
using livelywpf.Helpers.Archive;
using livelywpf.Helpers;
using livelywpf.Core;
using livelywpf.Views;
using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using livelywpf.Services;
using livelywpf.Factories;
using livelywpf.Models;
using livelywpf.Views.SetupWizard;
using livelywpf.Helpers.ScreenRecord;
using livelywpf.Cmd;
using livelywpf.Core.InputForwarding;
using livelywpf.Core.Suspend;
using livelywpf.Core.Watchdog;
using livelywpf.Helpers.NetWork;
using livelywpf.Services.Weather;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

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
            //App() -> OnStartup() -> App.Startup event.
            _serviceProvider = ConfigureServices();
        }

        private IServiceProvider ConfigureServices()
        {
            //TODO: Logger abstraction.
            //TODO: Simplify startup order: App() -> OnStartup() -> App.Startup event.
            var provider = new ServiceCollection()
                //singleton
                .AddSingleton<MainWindow>()
                .AddSingleton<IUserSettingsService, JsonUserSettingsService>()
                .AddSingleton<IDesktopCore, WinDesktopCore>()
                .AddSingleton<IWatchdogService, WatchdogProcess>()
                .AddSingleton<IScreensaverService, ScreensaverService>()
                .AddSingleton<IPlayback, Playback>()
                .AddSingleton<IAppUpdaterService, GithubUpdaterService>()
                .AddSingleton<ISystray, Systray>()
                .AddSingleton<ITransparentTbService, TransparentTbService>()
                .AddSingleton<IHardwareUsageService, PerfCounterUsageService>() //single service for all wallpapers/widgets.
                .AddSingleton<IWeatherService, OpenWeatherMapService>() //single service for all wallpapers/widgets.
                .AddSingleton<SettingsViewModel>() //some init stuff like locale, startup etc happening.. TODO: remove!
                .AddSingleton<LibraryViewModel>() //loaded wallpapers etc..
                .AddSingleton<RawInputMsgWindow>()
                .AddSingleton<WndProcMsgWindow>()
                //transient
                .AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
                .AddTransient<IWallpaperFactory, WallpaperFactory>()
                .AddTransient<ILivelyPropertyFactory, LivelyPropertyFactory>()
                .AddTransient<IScreenRecorder, ScreenRecorderlibScreen>()
                .AddTransient<ICommandHandler, CommandHandler>()
                .AddTransient<IDownloadHelper, MultiDownloadHelper>()
                .AddTransient<SetupView>()
                .AddTransient<ApplicationRulesViewModel>()
                .AddTransient<AddWallpaperViewModel>()
                .AddTransient<AboutViewModel>()
                .AddTransient<HelpViewModel>()
                .AddTransient<ScreenLayoutViewModel>()
                /*
                .AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog("Nlog.config");
                })
                */
                .BuildServiceProvider();

            return provider;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                //create directories if not exist, eg: C:\Users\<User>\AppData\Local
                Directory.CreateDirectory(Constants.CommonPaths.AppDataDir);
                Directory.CreateDirectory(Constants.CommonPaths.LogDir);
                Directory.CreateDirectory(Constants.CommonPaths.TempDir);
                Directory.CreateDirectory(Constants.CommonPaths.TempCefDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "AppData Directory Initialize Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Program.ExitApplication();
            }

            //Initial logging.
            SetupUnhandledExceptionLogging();
            LogUtil.LogHardwareInfo();

            //clear temp files if any.
            FileOperations.EmptyDirectory(Constants.CommonPaths.TempDir);

            //Initialize before viewmodel and main window.
            ScreenHelper.Initialize();

            #region vm init

            var userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            Program.WallpaperDir = userSettings.Settings.WallpaperDir;
            try
            {
                CreateWallpaperDir();
            }
            catch (Exception ex)
            {
                Logger.Error("Wallpaper Directory creation fail, falling back to default directory:" + ex.ToString());
                userSettings.Settings.WallpaperDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lively Wallpaper", "Library");
                userSettings.Save<ISettingsModel>();
                try
                {
                    CreateWallpaperDir();
                }
                catch (Exception ie)
                {
                    Logger.Error("Wallpaper Directory creation failed, Exiting:" + ie.ToString());
                    MessageBox.Show(ie.Message, "Error: Failed to create wallpaper folder", MessageBoxButton.OK, MessageBoxImage.Error);
                    Program.ExitApplication();
                }
            }

            //previous installed appversion is different from current instance..    
            if (!userSettings.Settings.AppVersion.Equals(Assembly.GetExecutingAssembly().GetName().Version.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                //todo: show changelog window here..
                userSettings.Settings.WallpaperBundleVersion = ExtractWallpaperBundle(userSettings.Settings.WallpaperBundleVersion);
                userSettings.Settings.AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                userSettings.Save<ISettingsModel>();
            }

            #endregion //vm init

            var appWindow = App.Services.GetRequiredService<MainWindow>();
            Application.Current.MainWindow = appWindow;
            if (userSettings.Settings.IsRestart)
            {
                userSettings.Settings.IsRestart = false;
                userSettings.Save<ISettingsModel>();
                appWindow?.Show();
            }
            //Creates an empty xaml island control as a temp fix for closing issue; also receives window msg..
            //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/3482
            //Steps to reproduce: Start gif wallpaper using uwp control -> restart lively -> close restored gif wallpaper -> library gridview stops.
            App.Services.GetRequiredService<WndProcMsgWindow>().Show();
            //Package app otherwise bugging out when initialized in settings vm.
            App.Services.GetRequiredService<RawInputMsgWindow>().Show();
            App.Services.GetRequiredService<IDesktopCore>().RestoreWallpaper();
            App.Services.GetRequiredService<IPlayback>().Start();

            base.OnStartup(e);
        }

        /// <summary>
        /// Extract default wallpapers and incremental if any.
        /// </summary>
        public static int ExtractWallpaperBundle(int currentBundleVer)
        {
            //Lively stores the last extracted bundle filename, extraction proceeds from next file onwards.
            int maxExtracted = currentBundleVer;
            try
            {
                //wallpaper bundles filenames are 0.zip, 1.zip ...
                var sortedBundles = Directory.GetFiles(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bundle"))
                    .OrderBy(x => x);

                foreach (var item in sortedBundles)
                {
                    if(int.TryParse(Path.GetFileNameWithoutExtension(item), out int val))
                    {
                        if (val > maxExtracted)
                        {
                            //Sharpzip library will overwrite files if exists during extraction.
                            ZipExtract.ZipExtractFile(item, Path.Combine(Program.WallpaperDir, "wallpapers"), false);
                            maxExtracted = val;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Base Wallpaper Extract Fail:" + e.ToString());
            }
            return maxExtracted;
        }

        private void CreateWallpaperDir()
        {
            Directory.CreateDirectory(Path.Combine(Program.WallpaperDir, "wallpapers"));
            Directory.CreateDirectory(Path.Combine(Program.WallpaperDir, "SaveData", "wptmp"));
            Directory.CreateDirectory(Path.Combine(Program.WallpaperDir, "SaveData", "wpdata"));
        }

        private void SetupUnhandledExceptionLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            Dispatcher.UnhandledException += (s, e) =>
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                Logger.Error("{0}\n{1}", message, exception.ToString());
            }
        }
    }
}
