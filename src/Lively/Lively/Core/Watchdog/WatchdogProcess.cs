using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lively.Core.Watchdog
{
    public class WatchdogProcess : IWatchdogService
    {
        private Process subProcess;
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public WatchdogProcess()
        {
            //crickets..
        }

        public void Start()
        {
            if (subProcess != null)
                return;

            Logger.Info("Starting watchdog service...");
            ProcessStartInfo start = new ProcessStartInfo()
            {
                Arguments = Process.GetCurrentProcess().Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Watchdog", "Lively.Watchdog"),
                RedirectStandardInput = true,
                //RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            subProcess = new Process
            {
                StartInfo = start,
            };

            try
            {
                subProcess.Start();
            }
            catch (Exception e)
            {
                Logger.Error("Failed to start watchdog service: " + e.Message);
            }
        }

        public void Add(int pid)
        {
            Logger.Info("Adding program to watchdog: " + pid);
            SendMessage("ADD " + pid);
        }

        public void Remove(int pid)
        {
            Logger.Info("Removing program to watchdog: " + pid);
            SendMessage("RMV " + pid);
        }

        public void Clear()
        {
            Logger.Info("Cleared watchdog program(s)..");
            SendMessage("CLR");
        }

        private void SendMessage(string text)
        {
            try
            {
                subProcess.StandardInput.WriteLine(text);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to communicate with watchdog service: " + e.Message);
            }
        }
    }
}
