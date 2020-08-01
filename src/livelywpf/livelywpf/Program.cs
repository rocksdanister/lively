using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace livelywpf
{
    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Mutex mutex = new Mutex(false, "LIVELY:DESKTOPWALLPAPERSYSTEM");
        public static string WallpaperDir;// =  Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper");
        public static readonly string AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lively Wallpaper");

        public static SettingsViewModel SettingsVM;
        public static ApplicationRulesViewModel AppRulesVM;
        public static LibraryViewModel LibraryVM;

        [System.STAThreadAttribute()]
        public static void Main()
        {
            try
            {
                // wait a few seconds in case livelywpf instance is just shutting down..
                if (!mutex.WaitOne(TimeSpan.FromSeconds(1), false))
                {
                    //ref: https://stackoverflow.com/questions/19147/what-is-the-correct-way-to-create-a-single-instance-wpf-application
                    // send our Win32 message to make the currently running instance
                    // jump on top of all the other windows
                    // todo: ditch this once ipc server is ready?
                    NativeMethods.PostMessage(
                        (IntPtr)NativeMethods.HWND_BROADCAST,
                        NativeMethods.WM_SHOWLIVELY,
                        IntPtr.Zero,
                        IntPtr.Zero);
                    return;
                }
            }
            catch (AbandonedMutexException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            try
            {
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

        private static Systray sysTray;
        private static void App_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                //create directories if not exist, eg: C:\Users\<User>\AppData\Roaming
                Directory.CreateDirectory(AppDataDir);
                Directory.CreateDirectory(Path.Combine(AppDataDir, "logs"));
                Directory.CreateDirectory(Path.Combine(AppDataDir, "temp"));
                Directory.CreateDirectory(Path.Combine(WallpaperDir, "wallpapers"));
                Directory.CreateDirectory(Path.Combine(WallpaperDir, "SaveData", "wptmp"));
                Directory.CreateDirectory(Path.Combine(WallpaperDir, "SaveData", "wpdata"));
            }
            catch (Exception ex)
            {
                //not logging here, something must be seriously wrong.. just display & terminate.
                MessageBox.Show(ex.Message, "Error: Failed to create data folder", MessageBoxButton.OK, MessageBoxImage.Error);
                ExitApplication();
                //Environment.Exit(1);
            }

            sysTray = new Systray();
            //AppUpdater();
            //LibraryVM.RestoreWallpaper();
        }

        private static async void AppUpdater()
        {
            try
            {
                var gitRelease = await UpdaterGithub.GetLatestRelease("lively", "rocksdanister", 10000);
                int result = UpdaterGithub.CompareAssemblyVersion(gitRelease);
                if (result > 0)
                {
                    try
                    {
                        //download asset format: lively_setup_x86_full_vXXXX.exe, XXXX - 4 digit version no.
                        var gitUrl = await UpdaterGithub.GetAssetUrl("lively_setup_x86_full", gitRelease, "lively", "rocksdanister");

                        //changelog text
                        StringBuilder sb = new StringBuilder(gitRelease.Body);
                        //formatting git text.
                        sb.Replace("#", "").Replace("\t", "  ");
                        Views.AppUpdaterView updateWindow = new Views.AppUpdaterView(new Uri(gitUrl), sb.ToString())
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };
                        updateWindow.Show();
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error retriving asseturl for update: " + e.Message);
                    }
                }
                else if (result < 0)
                {
                    //beta release.
                }
                else
                {
                    //up-to-date
                }
            }
            catch (Exception e)
            {
                Logger.Error("Git update check fail:" + e.Message);
            }
        }

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
            if (App.AppWindow != null)
            {
                App.AppWindow.Show();
                App.AppWindow.WindowState = App.AppWindow.WindowState != WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        public static void ExitApplication()
        {
            MainWindow._isExit = true;
            SetupDesktop.CloseAllWallpapers();
            if (sysTray != null)
            {
                sysTray.Dispose();
            }
            SetupDesktop.RefreshDesktop();

            System.Windows.Application.Current.Shutdown();
        }
    }
}
