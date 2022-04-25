using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Linq;
using System.Windows.Threading;
using livelywpf.Helpers.IPC;
using livelywpf.Helpers.Pinvoke;
using livelywpf.Core;
using livelywpf.Views;
using livelywpf.Views.Dialogues;
using Microsoft.Extensions.DependencyInjection;
using livelywpf.Services;
using livelywpf.Models;
using livelywpf.Views.SetupWizard;
using livelywpf.Cmd;

namespace livelywpf
{
    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Mutex mutex = new Mutex(false, Constants.SingleInstance.UniqueAppName);
        //Loaded from Settings.json (User configurable.)
        public static string WallpaperDir { get; set; }

        #region app entry

        [System.STAThreadAttribute()]
        public static void Main()
        {
            try
            {
                //wait a few seconds in case livelywpf instance is just shutting down..
                if (!mutex.WaitOne(TimeSpan.FromSeconds(1), false))
                {
                    try
                    {
                        //skipping first element (application path.)
                        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
                        PipeClient.SendMessage(Constants.SingleInstance.PipeServerName, args.Length != 0 ? args : new string[] { "--showApp", "true" });
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show($"Failed to communicate with ipc server: {e.Message}", "Lively Wallpaper");
                    }
                    return;
                }
            }
            catch (AbandonedMutexException e)
            {
                //unexpected app termination.
                Debug.WriteLine(e.Message);
            }

            try
            {
                var server = new PipeServer(Constants.SingleInstance.PipeServerName);
                server.MessageReceived += (s, e) => App.Services.GetRequiredService<ICommandHandler>().ParseArgs(e);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Failed to create ipc server: {e.Message}", "Lively Wallpaper");
            }

            try
            {
                //XAML Islands, uwp entry app.
                //See App.xaml.cs for wpf app startup override fn.
                using (var uwp = new rootuwp.App())
                {
                    //uwp.RequestedTheme = Windows.UI.Xaml.ApplicationTheme.Light;
                    livelywpf.App app = new livelywpf.App();
                    app.InitializeComponent();
                    app.Startup += App_Startup;
                    app.SessionEnding += App_SessionEnding;
                    app.Run();
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }


        private static Views.SetupWizard.SetupView setupWizard = null;
        private static void App_Startup(object sender, StartupEventArgs e)
        {
            var userSettings = App.Services.GetRequiredService<IUserSettingsService>();
            var appUpdater = App.Services.GetRequiredService<IAppUpdaterService>();
            var sysTray = App.Services.GetRequiredService<ISystray>();

            appUpdater.UpdateChecked += AppUpdateChecked;
            _ = appUpdater.CheckUpdate();
            appUpdater.Start();

            if (userSettings.Settings.IsFirstRun)
            {
                setupWizard = App.Services.GetRequiredService<SetupView>();
                setupWizard.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                setupWizard.Show();
            }

            //first element is not application path, unlike Environment.GetCommandLineArgs().
            if (e.Args.Length > 0)
            {
                App.Services.GetRequiredService<ICommandHandler>().ParseArgs(e.Args);
            }
        }

        public static void ApplicationThemeChange(AppTheme theme)
        {
            throw new NotImplementedException("xaml island theme/auto incomplete.");
            //switch (theme)
            //{
            //    case AppTheme.Auto:
            //        break;
            //    case AppTheme.Light:
            //        ModernWpf.ThemeManager.Current.ApplicationTheme = ModernWpf.ApplicationTheme.Light;
            //        break;
            //    case AppTheme.Dark:
            //        ModernWpf.ThemeManager.Current.ApplicationTheme = ModernWpf.ApplicationTheme.Dark;
            //        break;
            //    default:
            //        break;
            //}
        }

        #endregion //app entry

        #region app updater

        //number of times to notify user about update.
        private static int updateNotifyAmt = 1;
        private static bool updateNotify = false;

        private static void AppUpdateChecked(object sender, AppUpdaterEventArgs e)
        {
            var sysTray = App.Services.GetRequiredService<ISystray>();
            _ = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
            {
                if (e.UpdateStatus == AppUpdateStatus.available)
                {
                    if (updateNotifyAmt > 0)
                    {
                        updateNotifyAmt--;
                        updateNotify = true;
                        sysTray?.ShowBalloonNotification(4000,
                            Properties.Resources.TitleAppName,
                            Properties.Resources.DescriptionUpdateAvailable);
                    }
                }
                Logger.Info($"AppUpdate status: {e.UpdateStatus}");
            }));
        }

        private static AppUpdaterView updateWindow = null;
        public static void AppUpdateDialog(Uri uri, string changelog)
        {
            updateNotify = false;
            if (updateWindow == null)
            {
                var appWindow = App.Services.GetRequiredService<MainWindow>();
                updateWindow = new AppUpdaterView(uri, changelog);
                if (appWindow.IsVisible)
                {
                    updateWindow.Owner = appWindow;
                    updateWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    updateWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                updateWindow.Closed += (s, e) => { updateWindow = null; };
                updateWindow.Show();
            }
        }

        #endregion //app updater.

        #region app sessons

        private static void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if (e.ReasonSessionEnding == ReasonSessionEnding.Shutdown || e.ReasonSessionEnding == ReasonSessionEnding.Logoff)
            {
                //delay shutdown till lively close properly.
                e.Cancel = true;
                ExitApplication();
            }
        }

        public static void ShowMainWindow()
        {
            //Exit firstrun setupwizard.
            if (setupWizard != null)
            {
                var userSettings = App.Services.GetRequiredService<IUserSettingsService>();
                userSettings.Settings.IsFirstRun = false;
                userSettings.Save<ISettingsModel>();
                setupWizard.ExitWindow();
                setupWizard = null;
            }

            var appWindow = App.Services.GetRequiredService<MainWindow>();
            appWindow?.Show();
            appWindow.WindowState = appWindow?.WindowState != WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

            if (updateNotify)
            {
                var appUpdater = App.Services.GetRequiredService<IAppUpdaterService>();
                AppUpdateDialog(appUpdater.LastCheckUri, appUpdater.LastCheckChangelog);
            }
        }

        [Obsolete("Not working!")]
        public static void RestartApplication()
        {
            var appPath = Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".exe");
            Logger.Info("Restarting application:" + appPath);
            Process.Start(appPath);
            ExitApplication();
        }

        public static void ExitApplication()
        {
            MainWindow.IsExit = true;
            //Singleton dispose() not calling otherwise?
            ((ServiceProvider)App.Services)?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        #endregion //app sessions
    }
}