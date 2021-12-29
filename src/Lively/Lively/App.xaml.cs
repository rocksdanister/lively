using GrpcDotNetNamedPipes;
using Lively.Common;
using Lively.Common.Helpers.Storage;
using Lively.Core;
using Lively.Core.Display;
using Lively.Core.Suspend;
using Lively.Core.Watchdog;
using Lively.Factories;
using Lively.IPC;
using Lively.Models;
using Lively.Services;
using Lively.WndMsg;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Windows;

namespace Lively
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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
                Application.Current.Shutdown();
            }

            Services.GetRequiredService<WndProcMsgWindow>().Show();
            Services.GetRequiredService<RawInputMsgWindow>().Show();
            Services.GetRequiredService<IPlayback>().Start();

            //TEST
            ConfigureGrpcServer();
        }

        private void ConfigureGrpcServer()
        {
            var server = new NamedPipeServer(Constants.SingleInstance.GrpcPipeServerName);
            Desktop.DesktopService.BindService(server.ServiceBinder, Services.GetRequiredService<DesktopService>());
            server.Start();
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
                //.AddSingleton<ISystray, Systray>()
                //.AddSingleton<LibraryViewModel>() //loaded wallpapers etc..
                .AddSingleton<RawInputMsgWindow>()
                .AddSingleton<WndProcMsgWindow>()
                .AddSingleton<DesktopService>()
                //transient
                //.AddTransient<IApplicationsRulesFactory, ApplicationsRulesFactory>()
                .AddTransient<IWallpaperFactory, WallpaperFactory>()
                .AddTransient<ILivelyPropertyFactory, LivelyPropertyFactory>()
                //.AddTransient<IScreenRecorder, ScreenRecorderlibScreen>()
                //.AddTransient<ICommandHandler, CommandHandler>()
                //.AddTransient<IDownloadHelper, MultiDownloadHelper>()
                //.AddTransient<SetupView>()
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
    }
}
