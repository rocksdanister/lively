using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Globalization;

namespace livelywpf
{
    /// <summary>
    /// Retrieve system information:- operating system version, cpu & gpu name.
    /// </summary>
    public static class SystemInfo
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static void LogHardwareInfo()
        {
            Logger.Info("Lively v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " " + CultureInfo.CurrentCulture.Name + "  64Bit:" + Environment.Is64BitProcess);
            GetOSInfo();
            GetCPUInfo();
            GetGPUInfo();
        }

        private static void GetGPUInfo()
        {
            try
            {
                using (ManagementObjectSearcher myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController"))
                {
                    foreach (ManagementObject obj in myVideoObject.Get())
                    {
                        Logger.Info("GPU: " + obj["Name"]);
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Info("GPU: " + e.Message);
            }
        }

        private static void GetCPUInfo()
        {
            try
            {
                using (ManagementObjectSearcher myProcessorObject = new ManagementObjectSearcher("select * from Win32_Processor"))
                {
                    foreach (ManagementObject obj in myProcessorObject.Get())
                    {
                        Logger.Info("CPU: " + obj["Name"]);
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Info("CPU: " + e.Message);
            }
        }

        private static void GetOSInfo()
        {
            try
            {
                using (ManagementObjectSearcher myOperativeSystemObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in myOperativeSystemObject.Get())
                    {
                        Logger.Info("OS: " + obj["Caption"] + " " + obj["Version"]);
                    }
                }
            }
            catch(Exception e)
            {
                Logger.Info("OS: " + e.Message);
            }
        }
    }
}
