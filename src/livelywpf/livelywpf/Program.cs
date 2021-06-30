using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Linq;

namespace livelywpf
{
    public class Program
    {
        #region init

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly string uniqueAppName = "LIVELY:DESKTOPWALLPAPERSYSTEM";
        private static readonly string pipeServerName = uniqueAppName + Environment.UserName;
        private static readonly Mutex mutex = new Mutex(false, uniqueAppName);
        //Loaded from Settings.json (User configurable.)
        public static string WallpaperDir { get; set; }
        public static string AppDataDir { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lively Wallpaper");
        public static bool IsMSIX { get; } = new DesktopBridge.Helpers().IsRunningAsUwp();
        public static bool IsTestBuild { get; } = false;

        //todo: use singleton or something instead?
        public static SettingsViewModel SettingsVM { get; set; }
        public static ApplicationRulesViewModel AppRulesVM { get; set; }
        public static LibraryViewModel LibraryVM { get; set; }

        #endregion //init

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
                        Helpers.PipeClient.SendMessage(pipeServerName, args.Length != 0 ? args : new string[] { "--showApp", "true" });
                    }
                    catch
                    {
                        //fallback, Send our registered Win32 message to make the currently running lively instance to bring to foreground.
                        //registration of WM_SHOWLIVELY done at NativeMethods.cs file.
                        //ref: https://stackoverflow.com/questions/19147/what-is-the-correct-way-to-create-a-single-instance-wpf-application
                        NativeMethods.PostMessage(
                            (IntPtr)NativeMethods.HWND_BROADCAST,
                            NativeMethods.WM_SHOWLIVELY,
                            IntPtr.Zero,
                            IntPtr.Zero);
                    }
                    return;
                }
            }
            catch (AbandonedMutexException e)
            {
                //unexpected app termination.
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            try
            {
                var server = new Helpers.PipeServer(pipeServerName);
                server.MessageReceived += Server_MessageReceived1;
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

        private static void Server_MessageReceived1(object sender, string[] msg)
        {
            Cmd.CommandHandler.ParseArgs(msg);
        }

        private static Systray sysTray;
        private static Views.SetupWizard.SetupView setupWizard = null;
        private static void App_Startup(object sender, StartupEventArgs e)
        {
            sysTray = new Systray(SettingsVM.IsSysTrayIconVisible);
            AppUpdater();

            if (Program.SettingsVM.Settings.IsFirstRun)
            {
                setupWizard = new Views.SetupWizard.SetupView()
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                setupWizard.Show();
            }

            _ = SetupDesktop.RestoreWallpaperFromLayout(Path.Combine(Program.AppDataDir, "WallpaperLayout.json"));

            //first element is not application path, unlike Environment.GetCommandLineArgs().
            if (e.Args.Length > 0)
            {
                Cmd.CommandHandler.ParseArgs(e.Args);
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

        private static Uri gitUpdateUri;
        private static string gitUpdatChangelog;
        private static bool showUpdateDialog = false;
        private static async void AppUpdater()
        {
            if (IsMSIX)
                return;

            try
            {
                var userName = "rocksdanister";
                var repositoryName = IsTestBuild ? "lively-beta" : "lively";
                var fetchDelay = IsTestBuild ? 30000 : 45000;

                var gitRelease = await UpdaterGithub.GetLatestRelease(repositoryName, userName, fetchDelay);
                var result = UpdaterGithub.CompareAssemblyVersion(gitRelease);
                if (result > 0)
                {
                    try
                    {
                        //download asset format: lively_setup_x86_full_vXXXX.exe, XXXX - 4 digit version no.
                        var gitUrl = await UpdaterGithub.GetAssetUrl("lively_setup_x86_full", 
                            gitRelease, repositoryName, userName);

                        //changelog text
                        StringBuilder sb = new StringBuilder(gitRelease.Body);
                        //formatting git text.
                        sb.Replace("#", "").Replace("\t", "  ");
                        gitUpdatChangelog = sb.ToString();
                        sb.Clear();
                        gitUpdateUri = new Uri(gitUrl);

                        if (App.AppWindow.IsVisible)
                        {
                            ShowUpdateDialog();
                        }
                        else
                        {
                            showUpdateDialog = true;
                            sysTray.ShowBalloonNotification(4000,
                                Properties.Resources.TitleAppName,
                                Properties.Resources.DescriptionUpdateAvailable);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error retriving asseturl for update: " + e.Message);
                    }
                    sysTray.UpdateTrayBtn.Text = Properties.Resources.TextUpdateAvailable;
                    sysTray.UpdateTrayBtn.Enabled = true;
                }
                else if (result < 0)
                {
                    //beta release.
                    sysTray.UpdateTrayBtn.Text = ">_<'";
                }
                else
                {
                    //up-to-date
                    sysTray.UpdateTrayBtn.Text = Properties.Resources.TextUpdateUptodate;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Git update check fail:" + e.Message);
                sysTray.UpdateTrayBtn.Text = Properties.Resources.TextupdateCheckFail;
                sysTray.UpdateTrayBtn.Enabled = true;
            }
        }

        private static Views.AppUpdaterView updateWindow = null;
        public static void ShowUpdateDialog()
        {
            showUpdateDialog = false;
            if (updateWindow == null)
            {
                updateWindow = new Views.AppUpdaterView(gitUpdateUri, gitUpdatChangelog);
                if (App.AppWindow.IsVisible)
                {
                    updateWindow.Owner = App.AppWindow;
                    updateWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    updateWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                updateWindow.Closed += UpdateWindow_Closed;
                updateWindow.Show();
            }
        }

        private static void UpdateWindow_Closed(object sender, EventArgs e)
        {
            updateWindow.Closed -= UpdateWindow_Closed;
            updateWindow = null;
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
                SettingsVM.Settings.IsFirstRun = false;
                SettingsVM.UpdateConfigFile();
                setupWizard.ExitWindow();
                setupWizard = null;
            }

            App.AppWindow?.Show();
            App.AppWindow.WindowState = App.AppWindow?.WindowState != WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

            if (showUpdateDialog)
            {
                ShowUpdateDialog();
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
            SetupDesktop.ShutDown();
            sysTray?.Dispose();
            Helpers.TransparentTaskbar.Instance.Stop();
            System.Windows.Application.Current.Shutdown();
        }

        #endregion //app sessions
    }
}