using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace livelywpf.Core.Watchdog
{
    public class WatchdogProcess : IWatchdogService
    {
        private Process livelySubProcess;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public WatchdogProcess()
        {
            //crickets..
        }

        public void Start()
        {
            if (livelySubProcess != null)
                return;

            Logger.Info("Starting watchdog service...");
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

            try
            {
                livelySubProcess.Start();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to start watchdog service: " + e.Message);
            }
        }

        public void Add(int pid)
        {
            Logger.Info("Adding program to watchdog: " + pid);
            SendMessage("lively:add-pgm " + pid);
        }

        public void Remove(int pid)
        {
            Logger.Info("Removing program to watchdog: " + pid);
            SendMessage("lively:rmv-pgm " + pid);
        }

        public void Clear()
        {
            Logger.Info("Cleared watchdog program(s)..");
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
                Logger.Error("Failed to communicate with watchdog service: " + e.Message);
            }
        }
    }
}
