using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace livelywpf.Core
{
    public sealed class WatchdogProcess
    {
        private Process livelySubProcess;
        private static readonly WatchdogProcess instance = new WatchdogProcess();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static WatchdogProcess Instance
        {
            get
            {
                return instance;
            }
        }

        private WatchdogProcess()
        {
            //crickets..
        }

        public void Start()
        {
            if (livelySubProcess != null)
                return;

            try
            {
                Logger.Info("Starting watchdog service..");
                ProcessStartInfo start = new ProcessStartInfo()
                {
                    Arguments = Process.GetCurrentProcess().Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "subproc", "livelySubProcess.exe"),
                    RedirectStandardInput = true,
                    //RedirectStandardOutput = true,
                    UseShellExecute = false,
                };

                livelySubProcess = new Process
                {
                    StartInfo = start,
                };
                livelySubProcess.Start();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to start watchdog service:" + e.Message);
            }
        }

        public void Add(int pid)
        {
            Logger.Info("Watchdog: Adding program=>" + pid);
            SendMessage("lively:add-pgm " + pid);
        }

        public void Remove(int pid)
        {
            Logger.Info("Watchdog: Removing program=>" + pid);
            SendMessage("lively:rmv-pgm " + pid);
        }

        public void Clear()
        {
            Logger.Info("Watchdog: Cleared program(s)..");
            SendMessage("lively:clear");
        }

        private void SendMessage(string text)
        {
            try
            {
                livelySubProcess.StandardInput.WriteLine(text);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to communicate with watchdog service:" + e.Message);
            }
        }
    }
}
