using Lively.Common.API;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Lively.PlayerWebView2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            this.SessionEnding += (s, a) =>
            {
                if (a.ReasonSessionEnding == ReasonSessionEnding.Shutdown || a.ReasonSessionEnding == ReasonSessionEnding.Logoff)
                {
                    //Wallpaper core will handle the shutdown.
                    a.Cancel = true;
                }
            };
            SetupUnhandledExceptionLogging();
            MainWindow wnd = new MainWindow(e.Args);
            wnd.Show();
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
            WriteToParent(new LivelyMessageConsole()
            {
                Category = ConsoleMessageType.error,
                Message = $"Unhandled error: {exception.Message}",
            });
        }

        public static void WriteToParent(IpcMessage obj)
        {
#if DEBUG != true
            Console.WriteLine(JsonConvert.SerializeObject(obj));
#endif
            Debug.WriteLine(JsonConvert.SerializeObject(obj));
        }
    }
}
