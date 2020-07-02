using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace livelywpf
{
    public class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Mutex mutex = new Mutex(false, "LIVELY:DESKTOPWALLPAPERSYSTEM");

        public static SettingsViewModel SettingsVM = new SettingsViewModel();
        public static ApplicationRulesViewModel AppRulesVM = new ApplicationRulesViewModel();

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

        private static void App_Startup(object sender, StartupEventArgs e)
        {
            Systray sys = new Systray();
        }

        private static void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if (e.ReasonSessionEnding == ReasonSessionEnding.Shutdown || e.ReasonSessionEnding == ReasonSessionEnding.Logoff)
            {
                //delay shutdown till lively close properly.
                e.Cancel = true;
                System.Windows.Application.Current.Shutdown();
            }
        }

    }
}
