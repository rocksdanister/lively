// Copyright (c) 2020 Shankar
// The Shankar licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/ModernFlyouts-Community/ModernFlyouts

using livelywpf.Helpers.Pinvoke;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using observableObject = Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject;

namespace livelywpf.Core
{
    public class DisplayManager : observableObject //,IWndProcHookHandler
    {
        private const int PRIMARY_MONITOR = unchecked((int)0xBAADF00D);

        private const int MONITORINFOF_PRIMARY = 0x00000001;
        private const int MONITOR_DEFAULTTONEAREST = 0x00000002;

        private static bool multiMonitorSupport;
        private const string defaultDisplayDeviceName = "DISPLAY";

        public static DisplayManager Instance { get; private set; }

        public event EventHandler DisplayUpdated;

        public ObservableCollection<DisplayMonitor> DisplayMonitors { get; } = new ObservableCollection<DisplayMonitor>();

        private Rect virtualScreenBounds = Rect.Empty;

        public Rect VirtualScreenBounds
        {
            get => virtualScreenBounds;
            private set => SetProperty(ref virtualScreenBounds, value);
        }

        public DisplayMonitor PrimaryDisplayMonitor => DisplayMonitors
            .FirstOrDefault(x => x.IsPrimary);

        private DisplayManager()
        {
            RefreshDisplayMonitorList();
        }

        public static void Initialize()
        {
            Instance = new DisplayManager();
        }

        public uint OnHwndCreated(IntPtr hWnd, out bool register)
        {
            register = false;
            return 0;
        }

        public IntPtr OnWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (uint)NativeMethods.WM.DISPLAYCHANGE)
                //|| (msg == (uint)NativeMethods.WM.SETTINGCHANGE && wParam == ((IntPtr)NativeMethods.SPI.SPI_SETWORKAREA)))
            {
                RefreshDisplayMonitorList();
            }
            return IntPtr.Zero;
        }

        public DisplayMonitor GetDisplayMonitorFromHWnd(IntPtr hWnd)
        {
            IntPtr hMonitor = multiMonitorSupport
                ? NativeMethods.MonitorFromWindow(new HandleRef(null, hWnd), MONITOR_DEFAULTTONEAREST)
                : (IntPtr)PRIMARY_MONITOR;

            return GetDisplayMonitorFromHMonitor(hMonitor);
        }

        public DisplayMonitor GetDisplayMonitorFromPoint(Point point)
        {
            IntPtr hMonitor;
            if (multiMonitorSupport)
            {
                var pt = new NativeMethods.POINT(  //POINTSTRUCT
                    (int)Math.Round(point.X),
                    (int)Math.Round(point.Y));
                hMonitor = NativeMethods.MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
            }
            else
                hMonitor = (IntPtr)PRIMARY_MONITOR;

            return GetDisplayMonitorFromHMonitor(hMonitor);
        }

        private void RefreshDisplayMonitorList()
        {
            multiMonitorSupport = NativeMethods.GetSystemMetrics((int)NativeMethods.SystemMetric.SM_CMONITORS) != 0;

            var hMonitors = GetHMonitors();

            foreach (var displayMonitor in DisplayMonitors)
            {
                displayMonitor.isStale = true;
            }

            for (int i = 0; i < hMonitors.Count; i++)
            {
                var displayMonitor = GetDisplayMonitorFromHMonitor(hMonitors[i]);
                displayMonitor.Index = i + 1;
            }

            var staleDisplayMonitors = DisplayMonitors
                .Where(x => x.isStale).ToList();
            foreach (var displayMonitor in staleDisplayMonitors)
            {
                DisplayMonitors.Remove(displayMonitor);
            }

            staleDisplayMonitors.Clear();
            staleDisplayMonitors = null;

            VirtualScreenBounds = GetVirtualScreenBounds();

            DisplayUpdated?.Invoke(this, EventArgs.Empty);
        }

        private DisplayMonitor GetDisplayMonitorFromHMonitor(IntPtr hMonitor)
        {
            DisplayMonitor displayMonitor = null;

            if (!multiMonitorSupport || hMonitor == (IntPtr)PRIMARY_MONITOR)
            {
                displayMonitor = GetDisplayMonitorByDeviceName(defaultDisplayDeviceName);

                if (displayMonitor == null)
                {
                    displayMonitor = new DisplayMonitor(defaultDisplayDeviceName);
                    DisplayMonitors.Add(displayMonitor);
                }

                displayMonitor.Bounds = GetVirtualScreenBounds();
                displayMonitor.DeviceId = GetDefaultDisplayDeviceId();
                displayMonitor.DisplayName = "Display";
                displayMonitor.HMonitor = hMonitor;
                displayMonitor.IsPrimary = true;
                displayMonitor.WorkingArea = GetWorkingArea();

                displayMonitor.isStale = false;
            }
            else
            {
                var info = new NativeMethods.MONITORINFOEX();// MONITORINFOEX();
                NativeMethods.GetMonitorInfo(new HandleRef(null, hMonitor), info);

                string deviceName = new string(info.szDevice).TrimEnd((char)0);

                displayMonitor = GetDisplayMonitorByDeviceName(deviceName);

                displayMonitor ??= CreateDisplayMonitorFromMonitorInfo(deviceName);

                displayMonitor.HMonitor = hMonitor;

                UpdateDisplayMonitor(displayMonitor, info);
            }

            return displayMonitor;
        }

        private DisplayMonitor GetDisplayMonitorByDeviceName(string deviceName)
        {
            return DisplayMonitors.FirstOrDefault(x => x.DeviceName == deviceName);
        }

        private DisplayMonitor CreateDisplayMonitorFromMonitorInfo(string deviceName)
        {
            var displayMonitor = new DisplayMonitor(deviceName);

            var displayDevice = GetDisplayDevice(deviceName);
            displayMonitor.DeviceId = displayDevice.DeviceID;
            displayMonitor.DisplayName = displayDevice.DeviceString;

            DisplayMonitors.Add(displayMonitor);

            return displayMonitor;
        }

        private void UpdateDisplayMonitor(DisplayMonitor displayMonitor, NativeMethods.MONITORINFOEX info)
        {
            displayMonitor.Bounds = new Rect(
                info.rcMonitor.Left, info.rcMonitor.Top,
                info.rcMonitor.Right - info.rcMonitor.Left,
                info.rcMonitor.Bottom - info.rcMonitor.Top);

            displayMonitor.IsPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;

            displayMonitor.WorkingArea = new Rect(
                info.rcWork.Left, info.rcWork.Top,
                info.rcWork.Right - info.rcWork.Left,
                info.rcWork.Bottom - info.rcWork.Top);

            displayMonitor.isStale = false;
        }

        private IList<IntPtr> GetHMonitors()
        {
            if (multiMonitorSupport)
            {
                var hMonitors = new List<IntPtr>();

                bool callback(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam)
                {
                    hMonitors.Add(monitor);
                    return true;
                }

                NativeMethods.EnumDisplayMonitors(new HandleRef(null, IntPtr.Zero), null, callback, IntPtr.Zero);

                return hMonitors;
            }

            return new[] { (IntPtr)PRIMARY_MONITOR };
        }

        private static NativeMethods.DISPLAY_DEVICE GetDisplayDevice(string deviceName)
        {
            var result = new NativeMethods.DISPLAY_DEVICE();

            var displayDevice = new NativeMethods.DISPLAY_DEVICE();
            displayDevice.cb = Marshal.SizeOf(displayDevice);
            try
            {
                for (uint id = 0; NativeMethods.EnumDisplayDevices(deviceName, id, ref displayDevice, NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME); id++)
                {
                    if (displayDevice.StateFlags.HasFlag(NativeMethods.DisplayDeviceStateFlags.AttachedToDesktop)
                        && !displayDevice.StateFlags.HasFlag(NativeMethods.DisplayDeviceStateFlags.MirroringDriver))
                    {
                        result = displayDevice;
                        break;
                    }

                    displayDevice.cb = Marshal.SizeOf(displayDevice);
                }
            }
            catch { }

            if (string.IsNullOrEmpty(result.DeviceID)
                || string.IsNullOrWhiteSpace(result.DeviceID))
            {
                result.DeviceID = GetDefaultDisplayDeviceId();
            }

            return result;
        }

        private static string GetDefaultDisplayDeviceId() => NativeMethods.GetSystemMetrics((int)NativeMethods.SystemMetric.SM_REMOTESESSION) != 0 ?
                    "\\\\?\\DISPLAY#REMOTEDISPLAY#" : "\\\\?\\DISPLAY#LOCALDISPLAY#";

        private static Rect GetVirtualScreenBounds()
        {
            var location = new Point(NativeMethods.GetSystemMetrics(
                (int)NativeMethods.SystemMetric.SM_XVIRTUALSCREEN), NativeMethods.GetSystemMetrics((int)NativeMethods.SystemMetric.SM_YVIRTUALSCREEN));
            var size = new Size(NativeMethods.GetSystemMetrics(
                (int)NativeMethods.SystemMetric.SM_CXVIRTUALSCREEN), NativeMethods.GetSystemMetrics((int)NativeMethods.SystemMetric.SM_CYVIRTUALSCREEN));
            return new Rect(location, size);
        }

        private static Rect GetWorkingArea()
        {
            var rc = new NativeMethods.RECT();
            NativeMethods.SystemParametersInfo((int)NativeMethods.SPI.SPI_GETWORKAREA, 0, ref rc, 0);
            return new Rect(rc.Left, rc.Top,
                rc.Right - rc.Left, rc.Bottom - rc.Top);
        }
    }
}
