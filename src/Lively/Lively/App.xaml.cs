using GrpcDotNetNamedPipes;
using Lively.Common;
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace Lively
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly NamedPipeServer grpcServer;
        private readonly Mutex mutex = new Mutex(false, Constants.SingleInstance.UniqueAppName);

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
                        _ = new Desktop.DesktopService.DesktopServiceClient(new NamedPipeChannel(".", Constants.SingleInstance.GrpcPipeServerName)).
                            ShowUIAsync(new Google.Protobuf.WellKnownTypes.Empty());

                        //skipping first element (application path.)
                        //var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
                        //PipeClient.SendMessage(Constants.SingleInstance.PipeServerName, args.Length != 0 ? args : new string[] { "--showApp", "true" });
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "AppData Directory Initialize Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ShutDown();
            }

            Services.GetRequiredService<WndProcMsgWindow>().Show();
            Services.GetRequiredService<RawInputMsgWindow>().Show();
            Services.GetRequiredService<IPlayback>().Start();
            Services.GetRequiredService<ISystray>();
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
                //.AddSingleton<LibraryViewModel>() //loaded wallpapers etc..
                .AddSingleton<RawInputMsgWindow>()
                .AddSingleton<WndProcMsgWindow>()
                .AddSingleton<WinDesktopCoreServer>()
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
            Desktop.DesktopService.BindService(server.ServiceBinder, Services.GetRequiredService<WinDesktopCoreServer>());
            server.Start();

            return server;
        }

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
