using Lively.Grpc.Client;
using Lively.UI.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private readonly Window m_window;

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
            this.InitializeComponent();
            _serviceProvider = ConfigureServices();
            m_window = Services.GetRequiredService<MainWindow>();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            //m_window = new MainWindow();

            //m_window.ExtendsContentIntoTitleBar = true;
            //m_window.SetTitleBar(m_window.TitleBar);

            m_window.Activate();
        }

        private IServiceProvider ConfigureServices()
        {
            //TODO: make nlogger write only to console.
            var provider = new ServiceCollection()
                //singleton
                .AddSingleton<IDesktopCoreClient, WinDesktopCoreClient>()
                .AddSingleton<IUserSettingsClient, UserSettingsClient>()
                .AddSingleton<IDisplayManagerClient, DisplayManagerClient>()
                //.AddSingleton<IAppUpdaterService, GithubUpdaterService>()
                .AddSingleton<MainWindow>()
                .AddSingleton<LibraryViewModel>()
                //.AddSingleton<SettingsViewModel>() // can be made transient now?
                //transient
                //.AddTransient<HelpViewModel>()
                //.AddTransient<AboutViewModel>()
                //.AddTransient<LibraryUtil>()
                //.AddTransient<ScreenLayoutViewModel>()
                //.AddTransient<ApplicationRulesViewModel>()
                //.AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
                .BuildServiceProvider();

            return provider;
        }

        public static void ShutDown()
        {
            try
            {
                ((ServiceProvider)App.Services)?.Dispose();
            }
            catch (InvalidOperationException) { /* not initialised */ }
            //Shutdown needs to be called from dispatcher..
            //Dispatcher.Invoke(Application.Current.Shutdown);
            //var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            //dispatcherQueue.TryEnqueue(() => Debug.WriteLine("Dispatcher Queue"));
        }
    }
}
