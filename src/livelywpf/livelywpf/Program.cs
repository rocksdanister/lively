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
        public static readonly string LivelyDir =  Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Lively Wallpaper");

        public static SettingsViewModel SettingsVM = new SettingsViewModel();
        public static ApplicationRulesViewModel AppRulesVM = new ApplicationRulesViewModel();
        public static LibraryViewModel LibraryVM = new LibraryViewModel();

        [System.STAThreadAttribute()]
        public static void Main()
        {
            try
            {
                // wait a few seconds in case livelywpf instance is just shutting down..
                if (!mutex.WaitOne(TimeSpan.FromSeconds(5), false))
                {
                    //this is ignoring the config-file saved language, only checking system language.
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(CultureInfo.CurrentCulture.Name);
                    MessageBox.Show("Already running!");
                    return;
                }
            }
            catch (AbandonedMutexException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            try
            {
                using (new rootuwp.App())
                {
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
            sysTray = new Systray();
            //AppUpdater();
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
                App.AppWindow.WindowState = WindowState.Normal;
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
