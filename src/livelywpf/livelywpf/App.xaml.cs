using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static MainWindow AppWindow { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            //wpf app startup
            NLogger.SetupNLog();
            SetupUnhandledExceptionLogging();
            NLogger.LogHardwareInfo();

            try
            {
                //create directories if not exist, eg: C:\Users\<User>\AppData\Local
                Directory.CreateDirectory(Program.AppDataDir);
                Directory.CreateDirectory(Path.Combine(Program.AppDataDir, "logs"));
                Directory.CreateDirectory(Path.Combine(Program.AppDataDir, "temp"));
            }
            catch (Exception ex)
            {
                Logger.Error("Temp Directory creation fail:" + ex.ToString());
                MessageBox.Show(ex.Message, "Error: Failed to create data folder", MessageBoxButton.OK, MessageBoxImage.Error);
                Program.ExitApplication();
            }

            #region vm init

            Program.SettingsVM = new SettingsViewModel();
            Program.WallpaperDir = Program.SettingsVM.Settings.WallpaperDir;
            try
            {
                Directory.CreateDirectory(Path.Combine(Program.WallpaperDir, "wallpapers"));
                Directory.CreateDirectory(Path.Combine(Program.WallpaperDir, "SaveData", "wptmp"));
                Directory.CreateDirectory(Path.Combine(Program.WallpaperDir, "SaveData", "wpdata"));
            }
            catch (Exception ex)
            {
                Logger.Error("Wallpaper Directory creation fail:" + ex.ToString());
                MessageBox.Show(ex.Message, "Error: Failed to create wallpaper folder", MessageBoxButton.OK, MessageBoxImage.Error);
                Program.ExitApplication();
            }
            if (Program.SettingsVM.Settings.IsFirstRun)
            {
                ExtractWallpaperBundle();
            }

            try
            {
                //"Wallpaper Type" string of libraryitems are localized, so set locale before library vm init.
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Program.SettingsVM.Settings.Language);
            }
            catch (CultureNotFoundException)
            {
                Logger.Error("Localisation:Culture not found:" + Program.SettingsVM.Settings.Language);
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en");
            }
            Program.AppRulesVM = new ApplicationRulesViewModel();
            Program.LibraryVM = new LibraryViewModel();

            #endregion //vm init

            AppWindow = new MainWindow();
            //uwp root app needs window to show.. is it possible to skip?
            AppWindow.Show();
            if (Program.SettingsVM.Settings.IsRestart)
            {
                Program.SettingsVM.Settings.IsRestart = false;
                Program.SettingsVM.UpdateConfigFile();
            }
            else
            {
                AppWindow.Hide();
            }
            base.OnStartup(e);
        }

        /// <summary>
        /// Extract default wallpapers.
        /// </summary>
        private void ExtractWallpaperBundle()
        {
            //todo: Check appversion, if newer version then Settings.json saved version - extract version specific bundles if exists.
            //todo: Add a "please wait" page in SetupWizard to indicate extraction in progress.
            try
            {
                //Note: Sharpzip library will overwrite files if exists during extraction.
                ZipExtract.ZipExtractFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wallpaper bundle", "base.zip"), 
                    Path.Combine(Program.WallpaperDir, "wallpapers"), false);
            }
            catch(Exception e)
            {
                Logger.Error("Base Wallpaper Extract Fail:" + e.ToString());
            }
        }

        private void SetupUnhandledExceptionLogging()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            Dispatcher.UnhandledException += (s, e) =>
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                Logger.Error(message + "\n" + exception.ToString());
            }
        }
    }
}
