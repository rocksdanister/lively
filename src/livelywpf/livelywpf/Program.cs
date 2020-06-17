using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Windows;

namespace livelywpf
{
    public class Program
    {
        //private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static readonly Mutex mutex = new Mutex(false, "LIVELY:DESKTOPWALLPAPERSYSTEM");

        [System.STAThreadAttribute()]
        public static void Main()
        {
            try
            {
                //if (!mutex.WaitOne()) //indefinite wait.
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
                //Note to self:- logger backup(in the even of previous lively crash) is at App() contructor func, DO NOT start writing loghere to avoid overwriting crashlog.
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            try
            {
                using (new rootuwp.App())
                {
                    livelywpf.App app = new livelywpf.App();
                    app.InitializeComponent();
                    app.Run();
                }
            }
            finally 
            { 
                mutex.ReleaseMutex(); 
            }
        }
    }
}
