using Lively.Common.Helpers.Pinvoke;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace Lively.Helpers;

public static class SystemIdleUtil
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Fails after 50 days (uint limit.)
    /// </summary>
    public static uint GetLastInputTime()
    {
        NativeMethods.LASTINPUTINFO lastInputInfo = new NativeMethods.LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
        lastInputInfo.dwTime = 0;

        uint envTicks = (uint)Environment.TickCount;

        if (NativeMethods.GetLastInputInfo(ref lastInputInfo))
        {
            uint lastInputTick = lastInputInfo.dwTime;

            return (envTicks - lastInputTick);
        }
        else {
            throw new Win32Exception("GetLastInputTime fail.");
        }
    }

    public static bool IsExclusiveFullScreenAppRunning()
    {
        if (NativeMethods.SHQueryUserNotificationState(out NativeMethods.QUERY_USER_NOTIFICATION_STATE state) == 0)
        {
            return state switch
            {
                NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_NOT_PRESENT => false,
                NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_BUSY => false,
                NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE => false,
                NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_ACCEPTS_NOTIFICATIONS => false,
                NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_QUIET_TIME => false,
                NativeMethods.QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN => true,
                _ => false,
            };
        }
        else
        {
            throw new Win32Exception("SHQueryUserNotificationState fail.");
        }
    }

    public static async Task<bool> IsSmtcPlayingAsync()
    {
        try
        {
            // GlobalSystemMediaTransportControlsSessionManager.RequestAsync() can hang due to system instability.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(cts.Token);

            foreach (var session in manager.GetSessions())
            {
                var status = session.GetPlaybackInfo()?.PlaybackStatus;
                if (status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                    return true;
            }
        }
        catch (OperationCanceledException ce) {
            Logger.Warn($"GSMTC not responding, {ce}");
        }
        catch (Exception ex) {
            Logger.Error(ex);
        }

        return false;
    }

    public static List<uint> GetActiveAudioSessionPids()
    {
        var pids = new List<uint>();

        try
        {
            using var enumerator = new MMDeviceEnumerator();

            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
            {
                try
                {
                    var sessions = device.AudioSessionManager.Sessions;

                    for (int i = 0; i < sessions.Count; i++)
                    {
                        try
                        {
                            var session = sessions[i];

                            if (session.State == AudioSessionState.AudioSessionStateActive && 
                                session.AudioMeterInformation.MasterPeakValue > 0.001f)
                                pids.Add(session.GetProcessID);
                        }
                        catch { }
                    }
                }
                catch { }
                finally
                {
                    device.Dispose();
                }
            }
        }
        catch { }

        return pids;
    }
}
