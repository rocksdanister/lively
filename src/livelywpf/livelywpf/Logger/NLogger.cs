using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace livelywpf
{
    public class NLogger
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static bool _isInitialized = false;
        public static void SetupNLog()
        {
            _isInitialized = true;
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = Path.Combine(Program.LivelyDir, "logfile.txt"), DeleteOldFileOnStartup = true };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            // Apply config           
            NLog.LogManager.Configuration = config;
        }

        public static void LogHardwareInfo()
        {
            if(!_isInitialized)
            {
                SetupNLog();
            }

            Logger.Info("Lively v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " " + CultureInfo.CurrentCulture.Name + "  64Bit:" + Environment.Is64BitProcess);
            //Logger.Info("Portable build: " + App.isPortableBuild);
            Logger.Info(SystemInfo.GetOSInfo());
            Logger.Info(SystemInfo.GetCPUInfo());
            Logger.Info(SystemInfo.GetGPUInfo());
        }

        public static void LogWin32Error(string msg = null)
        {
            if (!_isInitialized)
            {
                SetupNLog();
            }

            //todo: throw equivalent win32 exception.
            int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            if (err != 0)
            {
                Logger.Error(msg + " HRESULT:" + err);
            }
        }
    }
}
