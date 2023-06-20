using CommandLine;
using Lively.Common.Helpers;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Network;
using Lively.Common.Helpers.Pinvoke;
using Lively.Gallery.Client;
using Lively.Grpc.Client;
using Lively.ML.DepthEstimate;
using Lively.Models;
using Lively.UI.WinUI.Factories;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.Services;
using Lively.UI.WinUI.ViewModels;
using Lively.UI.WinUI.Views.LivelyProperty;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
using WinUIEx;
using static Lively.Common.Constants;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
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

        public static StartArgs StartFlags { get; private set; } = new();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            if (!SingleInstanceUtil.IsAppMutexRunning(SingleInstance.UniqueAppName))
            {
                _ = NativeMethods.MessageBox(IntPtr.Zero, "Wallpaper core is not running, run Lively.exe first before opening UI.", "Lively Wallpaper", 16);
                //Sad dev noises.. this.Exit() does not work without Window: https://github.com/microsoft/microsoft-ui-xaml/issues/5931
                Process.GetCurrentProcess().Kill();
            }

            this.InitializeComponent();
            _serviceProvider = ConfigureServices();
            var userSettings = Services.GetRequiredService<IUserSettingsClient>();
            SetAppTheme(userSettings.Settings.ApplicationTheme);
            //SetAppLanguage(userSettings.Settings.Language);
            //Services.GetRequiredService<SettingsViewModel>().AppThemeChanged += (s, e) => SetAppTheme(e);

            SetupUnhandledExceptionLogging();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            //Workaround, LaunchActivatedEventArgs does not work: https://github.com/microsoft/microsoft-ui-xaml/issues/3368   
            var cmdArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
            if (cmdArgs.Any())
            {
                Parser.Default.ParseArguments<StartArgs>(cmdArgs)
                    .WithParsed((x) => StartFlags = x)
                    .WithNotParsed((x) => Logger.Error(x));
                if (StartFlags.TrayWidget)
                {
                    var desktopCore = Services.GetRequiredService<IDesktopCoreClient>();
                    var items = desktopCore.Wallpapers.Where(x => x.LivelyPropertyCopyPath != null);
                    if (items.Any())
                    {
                        var selection = items.FirstOrDefault(x => x.Display.IsPrimary) ?? items.First();
                        if (selection is not null)
                        {
                            var libraryVm = Services.GetRequiredService<LibraryViewModel>();
                            var model = libraryVm.LibraryItems.FirstOrDefault(x => selection.LivelyInfoFolderPath == x.LivelyInfoFolderPath);
                            if (model is not null)
                            {
                                var tray = new LivelyPropertiesTray(model);
                                tray.Closed += (s, e) =>
                                {
                                    App.ShutDown();
                                };
                                tray.Show();
                            }
                        }
                    }
                }
                else
                {
                    //TODO
                }
            }
            else
            {
                var m_window = Services.GetRequiredService<MainWindow>();
                m_window.Activate();
            }
        }

        private IServiceProvider ConfigureServices()
        {
            var provider = new ServiceCollection()
                //singleton
                .AddSingleton<IDesktopCoreClient, WinDesktopCoreClient>()
                .AddSingleton<IUserSettingsClient, UserSettingsClient>()
                .AddSingleton<IDisplayManagerClient, DisplayManagerClient>()
                .AddSingleton<ICommandsClient, CommandsClient>()
                .AddSingleton<IAppUpdaterClient, AppUpdaterClient>()
                .AddSingleton<MainWindow>()
                .AddSingleton<MainViewModel>()
                .AddSingleton<GalleryClient>((e) => new GalleryClient(e.GetRequiredService<IHttpClientFactory>(), "http://api.livelywallpaper.net/api/",
                    "https://accounts.google.com/o/oauth2/auth/oauthchooseaccount?client_id=923081992071-qg27j4uhasb3r4lasb9cb19nbhvgbb34.apps.googleusercontent.com&redirect_uri=http://127.0.0.1:43821/signin-oidc&scope=email%20openid%20profile&response_type=code&state=asdafwswdwefwsdg&flowName=GeneralOAuthFlow",
                    "https://github.com/login/oauth/authorize?client_id=bbfd46fbb54895ecee74&redirect_uri=http://127.0.0.1:43821/signin-oidc-github&scope=user:email",
                    new JsonTokenStore()))
                .AddSingleton<LibraryViewModel>() //Library items are stored..
                .AddSingleton<GalleryViewModel>()
                .AddSingleton<GallerySubscriptionViewModel>()
                .AddSingleton<SettingsViewModel>() //Some events..
                .AddSingleton<ICacheService, DiskCacheService>((e) => new DiskCacheService(e.GetRequiredService<IHttpClientFactory>(), Path.Combine(Path.GetTempPath(), "Lively Wallpaper", "gallery")))
                .AddSingleton<IDepthEstimate, MiDaS>()
                //transient
                //.AddTransient<HelpViewModel>()
                .AddTransient<AboutViewModel>()
                .AddTransient<AddWallpaperViewModel>()
                .AddTransient<ControlPanelViewModel>()
                .AddTransient<ChooseDisplayViewModel>()
                .AddTransient<FindMoreAppsViewModel>()
                .AddTransient<ThemeViewModel>()
                .AddTransient<GalleryLoginViewModel>()
                .AddTransient<ManageAccountViewModel>()
                .AddTransient<RestoreWallpaperViewModel>()
                .AddTransient<AddWallpaperCreateViewModel>()
                .AddTransient<DepthEstimateWallpaperViewModel>()
                .AddTransient<IDialogService, DialogService>()
                .AddTransient<IApplicationsFactory, ApplicationsFactory>()
                .AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
                .AddTransient<IThemeFactory, ThemeFactory>()
                .AddTransient<IDownloadHelper, SimpleDownloadHelper>()
                //https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
                .AddHttpClient()
                .BuildServiceProvider();

            return provider;
        }

        //Cannot change runtime.
        //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/4474
        private void SetAppTheme(Common.AppTheme theme)
        {
            switch (theme)
            {
                case Common.AppTheme.Auto:
                    //Nothing
                    break;
                case Common.AppTheme.Light:
                    this.RequestedTheme = ApplicationTheme.Light;
                    break;
                case Common.AppTheme.Dark:
                    this.RequestedTheme = ApplicationTheme.Dark;
                    break;
            }
        }

        //Cannot set custom language on unpackaged, issues:
        //https://github.com/microsoft/microsoft-ui-xaml/issues/5940
        //https://github.com/microsoft/WindowsAppSDK/issues/1687
        //https://github.com/microsoft/WindowsAppSDK-Samples/issues/138
        private void SetAppLanguage(string cult = "en-US")
        {
            ApplicationLanguages.PrimaryLanguageOverride = cult;
            CultureInfo culture = new CultureInfo(cult);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.CurrentCulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            //ResourceContext.GetForCurrentView().Reset();
            ResourceContext.GetForViewIndependentUse().Reset();
        }

        //Not working ugh..
        //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/5221
        private void SetupUnhandledExceptionLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject);

            TaskScheduler.UnobservedTaskException += (s, e) =>
                LogUnhandledException(e.Exception);

            this.UnhandledException += (s, e) =>
                LogUnhandledException(e.Exception);

            Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected += (s, e) =>
                LogUnhandledException(e.UnhandledError);
        }

        private void LogUnhandledException<T>(T exception) => Logger.Error(exception);

        public static void ShutDown()
        {
            try
            {
                ((ServiceProvider)App.Services)?.Dispose();
            }
            catch (InvalidOperationException) { /* not initialised */ }

            //Stackoverflow exception :L
            //Note: Exit() does not work without Window: https://github.com/microsoft/microsoft-ui-xaml/issues/5931
            //((App)Current).Exit();
        }
    }
}
