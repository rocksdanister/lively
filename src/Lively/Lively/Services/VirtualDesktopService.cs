using Lively.Common.Services;
using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Lively.Services
{
    /// <summary>
    /// Tracks the active Windows virtual desktop by watching the Explorer registry state.
    /// Windows writes the active desktop id to
    /// HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops\CurrentVirtualDesktop
    /// (older builds use the SessionInfo\{n}\VirtualDesktops subkey) on every switch.
    /// RegNotifyChangeKeyValue gives event latency; a periodic timeout re-read covers builds
    /// that only update the SessionInfo variant.
    /// </summary>
    public class VirtualDesktopService : IVirtualDesktopService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private const string vdRegPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops";
        private const int pollFallbackMs = 2000;

        private Guid currentDesktopId = Guid.Empty;
        public Guid CurrentDesktopId => currentDesktopId;

        public event EventHandler<Guid> CurrentDesktopChanged;

        private Thread watcherThread;
        private ManualResetEvent stopEvent;
        private bool disposedValue;

        public void Start()
        {
            if (watcherThread != null)
                return;

            currentDesktopId = ReadCurrentDesktopId();
            stopEvent = new ManualResetEvent(false);
            watcherThread = new Thread(WatcherLoop)
            {
                IsBackground = true,
                Name = "VirtualDesktopWatcher"
            };
            watcherThread.Start();
            Logger.Info($"Virtual desktop watcher started, current: {currentDesktopId}");
        }

        public void Stop()
        {
            stopEvent?.Set();
            watcherThread?.Join(1000);
            watcherThread = null;
            stopEvent?.Dispose();
            stopEvent = null;
        }

        private void WatcherLoop()
        {
            using var notifyEvent = new AutoResetEvent(false);
            var waitHandles = new WaitHandle[] { stopEvent, notifyEvent };
            IntPtr hKey = IntPtr.Zero;
            var notifyArmed = false;

            try
            {
                while (!stopEvent.WaitOne(0))
                {
                    if (hKey == IntPtr.Zero && RegOpenKeyEx(HKEY_CURRENT_USER, vdRegPath, 0, KEY_NOTIFY, out hKey) != 0)
                        hKey = IntPtr.Zero;

                    // One registration per signal, re-armed only after it fires.
                    if (hKey != IntPtr.Zero && !notifyArmed)
                    {
                        notifyArmed = RegNotifyChangeKeyValue(hKey, false, REG_NOTIFY_CHANGE_LAST_SET,
                            notifyEvent.SafeWaitHandle.DangerousGetHandle(), true) == 0;
                    }

                    // Wake on registry change; timeout re-read covers missed/unsupported notifications.
                    var signaled = WaitHandle.WaitAny(waitHandles, notifyArmed ? pollFallbackMs * 5 : pollFallbackMs);
                    if (signaled == 0)
                        break;
                    if (signaled == 1)
                        notifyArmed = false;

                    var newId = ReadCurrentDesktopId();
                    if (newId != Guid.Empty && newId != currentDesktopId)
                    {
                        currentDesktopId = newId;
                        Logger.Info($"Virtual desktop changed: {newId}");
                        CurrentDesktopChanged?.Invoke(this, newId);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Virtual desktop watcher failed: {e}");
            }
            finally
            {
                if (hKey != IntPtr.Zero)
                    RegCloseKey(hKey);
            }
        }

        /// <summary>
        /// Reads the active desktop id, Guid.Empty when unavailable.
        /// </summary>
        public static Guid ReadCurrentDesktopId()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(vdRegPath, writable: false);
                if (key?.GetValue("CurrentVirtualDesktop") is byte[] raw && raw.Length == 16)
                    return new Guid(raw);

                // Older Windows builds keep it per session.
                var sessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                using var sessionKey = Registry.CurrentUser.OpenSubKey(
                    $@"Software\Microsoft\Windows\CurrentVersion\Explorer\SessionInfo\{sessionId}\VirtualDesktops", writable: false);
                if (sessionKey?.GetValue("CurrentVirtualDesktop") is byte[] sessionRaw && sessionRaw.Length == 16)
                    return new Guid(sessionRaw);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to read current virtual desktop: {e.Message}");
            }
            return Guid.Empty;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Stop();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #region pinvoke

        private static readonly IntPtr HKEY_CURRENT_USER = new(unchecked((int)0x80000001));
        private const int KEY_NOTIFY = 0x0010;
        private const int REG_NOTIFY_CHANGE_LAST_SET = 0x4;

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegOpenKeyEx(IntPtr hKey, string lpSubKey, int ulOptions, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll")]
        private static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, int dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

        [DllImport("advapi32.dll")]
        private static extern int RegCloseKey(IntPtr hKey);

        #endregion //pinvoke
    }
}
