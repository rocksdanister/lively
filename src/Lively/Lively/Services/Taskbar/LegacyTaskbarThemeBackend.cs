using Lively.Common.Helpers.Pinvoke;
using Lively.Models.Enums;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Lively.Services.Taskbar
{
    internal sealed class LegacyTaskbarThemeBackend : ITaskbarThemeBackend
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly System.Timers.Timer timer = new();
        private AccentPolicy accentPolicyRegular = new();
        private TaskbarThemeState currentState = new(TaskbarTheme.none, System.Drawing.Color.Black);
        private bool disposedValue;

        public LegacyTaskbarThemeBackend()
        {
            timer.Interval = 500;
            timer.Elapsed += (_, _) => SetTaskbarTransparent();
        }

        public bool IsRunning { get; private set; }

        public string Name => nameof(LegacyTaskbarThemeBackend);

        public void Start(TaskbarThemeState state)
        {
            if (state.Theme == TaskbarTheme.none)
            {
                Stop();
                return;
            }

            currentState = state;
            timer.Stop();
            SetTheme(state);
            ResetTaskbar();
            SetTaskbarTransparent();
            timer.Start();
            IsRunning = true;
        }

        public void Refresh()
        {
            if (!IsRunning)
                return;

            ResetTaskbar();
            SetTaskbarTransparent();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            timer.Stop();
            ResetTaskbar();
            IsRunning = false;
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                Stop();
                timer.Dispose();
                disposedValue = true;
            }
        }

        private void SetTheme(TaskbarThemeState state)
        {
            accentPolicyRegular.GradientColor = TaskbarThemeColor.ToLegacyGradientColor(state);
            accentPolicyRegular.AccentState = state.Theme switch
            {
                TaskbarTheme.clear => AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT,
                TaskbarTheme.blur => AccentState.ACCENT_ENABLE_BLURBEHIND,
                TaskbarTheme.fluent => AccentState.ACCENT_ENABLE_FLUENT,
                TaskbarTheme.color => AccentState.ACCENT_ENABLE_GRADIENT,
                TaskbarTheme.wallpaper => AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT,
                TaskbarTheme.wallpaperFluent => AccentState.ACCENT_ENABLE_FLUENT,
                _ => AccentState.ACCENT_DISABLED,
            };
        }

        private void SetTaskbarTransparent()
        {
            if (currentState.Theme == TaskbarTheme.none)
                return;

            var taskbars = GetTaskbars();
            if (taskbars.Count == 0)
                return;

            var accentPtr = IntPtr.Zero;
            try
            {
                var accentStructSize = Marshal.SizeOf(accentPolicyRegular);
                accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accentPolicyRegular, accentPtr, false);
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                foreach (var taskbar in taskbars)
                {
                    SetWindowCompositionAttribute(taskbar, ref data);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to apply legacy taskbar theme.");
                Stop();
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        private static List<IntPtr> GetTaskbars()
        {
            IntPtr taskbar;
            var taskbars = new List<IntPtr>(2);
            if ((taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null)) != IntPtr.Zero)
            {
                taskbars.Add(taskbar);
            }

            if ((taskbar = NativeMethods.FindWindow("Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
            {
                taskbars.Add(taskbar);
                while ((taskbar = NativeMethods.FindWindowEx(IntPtr.Zero, taskbar, "Shell_SecondaryTrayWnd", IntPtr.Zero)) != IntPtr.Zero)
                {
                    taskbars.Add(taskbar);
                }
            }
            return taskbars;
        }

        private static void ResetTaskbar()
        {
            foreach (var taskbar in GetTaskbars())
            {
                NativeMethods.SendMessage(taskbar, (int)NativeMethods.WM.DWMCOMPOSITIONCHANGED, IntPtr.Zero, IntPtr.Zero);
            }
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_FLUENT = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public uint GradientColor;
            public int AnimationId;
        }
    }
}
