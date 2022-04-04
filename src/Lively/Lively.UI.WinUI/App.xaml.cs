using Lively.Common.Helpers;
using Lively.Common.Helpers.Archive;
using Lively.Common.Helpers.Pinvoke;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Factories;
using Lively.UI.WinUI.Helpers;
using Lively.UI.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources.Core;
using Windows.Globalization;
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

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            if (!SingleInstanceUtil.IsAppMutexRunning(SingleInstance.UniqueAppName))
            {
                _ = NativeMethods.MessageBox(IntPtr.Zero, "Wallpaper core is not running, exiting..", "Lively UI", 16);
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
            //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/3368   
            //Environment.GetCommandLineArgs()[1]
            var m_window = Services.GetRequiredService<MainWindow>();
            m_window.Activate();
            m_window.SetWindowSizeEx(875, 875);
        }

        private IServiceProvider ConfigureServices()
        {
            //TODO: make nlogger write only to console.
            var provider = new ServiceCollection()
                //singleton
                .AddSingleton<IDesktopCoreClient, WinDesktopCoreClient>()
                .AddSingleton<IUserSettingsClient, UserSettingsClient>()
                .AddSingleton<IDisplayManagerClient, DisplayManagerClient>()
                .AddSingleton<ICommandsClient, CommandsClient>()
                .AddSingleton<IAppUpdaterClient, AppUpdaterClient>()
                .AddSingleton<MainWindow>()
                .AddSingleton<LibraryViewModel>() //Library items are stored..
                .AddSingleton<SettingsViewModel>() //Some events..
                .AddSingleton<LibraryUtil>() //Used frequently..
                //transient
                //.AddTransient<HelpViewModel>()
                .AddTransient<AboutViewModel>()
                .AddTransient<AddWallpaperViewModel>()
                .AddTransient<ScreenLayoutViewModel>()
                .AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
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
