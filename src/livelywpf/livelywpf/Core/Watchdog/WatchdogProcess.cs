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
        public bool IsRunning { get; private set; } = false;
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
            if (IsRunning)
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
                IsRunning = true;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to start watchdog process:" + e.Message);
            }
        }

        public void Add(int pid)
        {
            Logger.Info("Adding pgm:" + pid);
            SendMessage("lively:add-pgm " + pid);
        }

        public void Remove(int pid)
        {
            Logger.Info("Removing pgm:" + pid);
            SendMessage("lively:rmv-pgm " + pid);
        }

        public void Clear()
        {
            Logger.Info("Cleared pgm..");
            SendMessage("lively:clear");
        }

        private void SendMessage(string text)
        {
            if (IsRunning)
            {
                try
                {
                    livelySubProcess.StandardInput.WriteLine(text);
                }
                catch { }
            }
        }
    }
}
