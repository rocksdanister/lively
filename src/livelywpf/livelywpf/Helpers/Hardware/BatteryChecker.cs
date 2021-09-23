using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace livelywpf.Helpers.Hardware
{
    //ref:
    //https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-getsystempowerstatus
    //https://docs.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-system_power_status
    public class BatteryChecker
    {
        public BatteryChecker()
        {
            // Nothing
        }

        public static bool GetSystemPowerStatus(ref SystemPowerStatus sps)
        {
            sps = new SystemPowerStatus();
            return GetSystemPowerStatus(sps);
        }

        public static SystemStatusFlag GetBatterySaverStatus()
        {
            var sps = new SystemPowerStatus();
            return GetSystemPowerStatus(sps) ? sps._SystemStatusFlag : SystemStatusFlag.Off;
        }

        public static ACLineStatus GetACPowerStatus()
        {
            var sps = new SystemPowerStatus();
            return GetSystemPowerStatus(sps) ? sps._ACLineStatus : ACLineStatus.Online;
        }

        public static bool IsBatterySavingMode => GetBatterySaverStatus() == SystemStatusFlag.On;

        #region pinvoke

        [DllImport("Kernel32")]
        private static extern Boolean GetSystemPowerStatus(SystemPowerStatus sps);

        public enum ACLineStatus : byte
        {
            Offline = 0,
            Online = 1,
            Unknown = 255
        }

        public enum BatteryFlag : byte
        {
            High = 1,
            Low = 2,
            Critical = 4,
            Charging = 8,
            NoSystemBattery = 128,
            Unknown = 255
        }

        public enum SystemStatusFlag : byte
        {
            Off = 0, // Battery saver is off.
            On = 1 // Battery saver on. Save energy where possible.
        }

        // Fields must mirror their unmanaged counterparts, in order
        [StructLayout(LayoutKind.Sequential)]
        public class SystemPowerStatus
        {
            public ACLineStatus _ACLineStatus;
            public BatteryFlag _BatteryFlag;
            public Byte _BatteryLifePercent;
            public SystemStatusFlag _SystemStatusFlag;
            public Int32 _BatteryLifeTime;
            public Int32 _BatteryFullLifeTime;
        }

        #endregion //pinvoke
    }
}
