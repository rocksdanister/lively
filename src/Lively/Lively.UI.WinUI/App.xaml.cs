using Lively.Common.Helpers;
using Lively.Common.Helpers.Pinvoke;
using Lively.Grpc.Client;
using Lively.UI.WinUI.Factories;
using Lively.UI.WinUI.Helpers;
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
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using WinRT;
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
        private Window m_window;
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
            SetAppTheme(Services.GetRequiredService<IUserSettingsClient>().Settings.ApplicationTheme);
            //Services.GetRequiredService<SettingsViewModel>().AppThemeChanged += (s, e) => SetAppTheme(e);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = Services.GetRequiredService<MainWindow>();
            var windowNative = m_window.As<IWindowNative>();
            var m_windowHandle = windowNative.WindowHandle;
            m_window.Activate();

            //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/6353
            //IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(m_window);
            //var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            //var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            //appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 720));
            SetWindowSize(m_windowHandle, 875, 875);
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
                //.AddSingleton<IAppUpdaterService, GithubUpdaterService>()
                .AddSingleton<MainWindow>()
                .AddSingleton<LibraryViewModel>() //Library items are stored..
                .AddSingleton<SettingsViewModel>() //Some events..
                //transient
                //.AddTransient<HelpViewModel>()
                //.AddTransient<AboutViewModel>()
                .AddTransient<AddWallpaperViewModel>()
                .AddTransient<LibraryUtil>()
                .AddTransient<ScreenLayoutViewModel>()
                .AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
                .BuildServiceProvider();

            return provider;
        }

        //Cannot change runtime.
        //Issue: https://github.com/microsoft/microsoft-ui-xaml/issues/4474
        public void SetAppTheme(Common.AppTheme theme)
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

        #region helpers

        private void SetWindowSize(IntPtr hwnd, int width, int height)
        {
            var dpi = NativeMethods.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            NativeMethods.SetWindowPos(hwnd, 0, 0, 0, width, height, (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        #endregion //helpers
    }
}
