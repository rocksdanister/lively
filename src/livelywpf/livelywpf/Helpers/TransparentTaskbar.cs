using ImageMagick;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace livelywpf.Helpers
{
    //ref:
    //https://gist.github.com/riverar/fd6525579d6bbafc6e48
    public sealed class TransparentTaskbar
    {
        public bool IsRunning { get; private set; } = false;
        private Color accentColor = Color.FromArgb(0, 0, 0);
        private TaskbarTheme taskbarTheme = TaskbarTheme.none;
        private AccentPolicy accentPolicyRegular = new AccentPolicy();
        //private AccentPolicy accentPolicyMaximised = new AccentPolicy();
        private readonly System.Timers.Timer _timer = new System.Timers.Timer();
        private static readonly TransparentTaskbar instance = new TransparentTaskbar();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly static IDictionary<string, string> incompatiblePrograms = new Dictionary<string, string>() { 
            {"TranslucentTB", "344635E9-9AE4-4E60-B128-D53E25AB70A7"},
            {"TaskbarX", null}, //don't have mutex
        };

        public static TransparentTaskbar Instance
        {
            get
            {
                return instance;
            }
        }

        private TransparentTaskbar()
        {
            _timer.Interval = 500;
            _timer.Elapsed += Timer_Elapsed;
        }

        public void Start(TaskbarTheme theme)
        {
            if (theme == TaskbarTheme.none)
            {
                Stop();
            }
            else
            {
                _timer.Stop();
                SetTheme(theme);
                ResetTaskbar();
                _timer.Start();
                IsRunning = true;
                Logger.Info("Taskbar theme service started.");
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                _timer.Stop();
                ResetTaskbar();
                IsRunning = false;
                Logger.Info("Taskbar theme service stopped.");
            }
        }

        private void SetTheme(TaskbarTheme theme)
        {
            taskbarTheme = theme;
            Logger.Info("Taskbar theme: {0}", theme);
            switch (taskbarTheme)
            {
                case TaskbarTheme.none:
                    //accent.AccentState = AccentState.ACCENT_DISABLED;
                    break;
                case TaskbarTheme.clear:
                    accentPolicyRegular.GradientColor = 16777215; //00FFFFFF
                    accentPolicyRegular.AccentState = AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT;
                    break;
                case TaskbarTheme.blur:
                    accentPolicyRegular.GradientColor = 0;
                    accentPolicyRegular.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                    break;
                case TaskbarTheme.color:
                    //todo
                    break;
                case TaskbarTheme.fluent:
                    accentPolicyRegular.GradientColor = 167772160; //A000000
                    accentPolicyRegular.AccentState = AccentState.ACCENT_ENABLE_FLUENT;
                    break;
                case TaskbarTheme.wallpaper:
                    accentPolicyRegular.GradientColor = Convert.ToUInt32(string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", 200, accentColor.B, accentColor.G, accentColor.R), 16);
                    accentPolicyRegular.AccentState = AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT;
                    break;
                case TaskbarTheme.wallpaperFluent:
                    accentPolicyRegular.GradientColor = Convert.ToUInt32(string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", 125, accentColor.B, accentColor.G, accentColor.R), 16);
                    accentPolicyRegular.AccentState = AccentState.ACCENT_ENABLE_FLUENT;
                    break;
            }
        }

        public void SetAccentColor(Color color)
        {
            accentColor = color;
            Start(taskbarTheme);
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SetTaskbarTransparent(taskbarTheme);
        }

        private void SetTaskbarTransparent(TaskbarTheme theme)
        {
            if (theme == TaskbarTheme.none)
            {
                return;
            }

            var taskbars = GetTaskbars();
            if (taskbars.Count != 0)
            {
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
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    Stop();
                }
                finally
                {
                    //not required for this structure..
                    //Marshal.DestroyStructure(accentPtr, typeof(AccentPolicy));
                    Marshal.FreeHGlobal(accentPtr);
                }
            }
        }

        #region helpers

        private List<IntPtr> GetTaskbars()
        {
            IntPtr taskbar;
            var taskbars = new List<IntPtr>(2);
            //main taskbar..
            if ((taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null)) != IntPtr.Zero)
            {
                taskbars.Add(taskbar);
            }
            //secondary taskbar(s)..
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

        private void ResetTaskbar()
        {
            foreach (var taskbar in GetTaskbars())
            {
                NativeMethods.SendMessage(taskbar, (int)NativeMethods.WM.DWMCOMPOSITIONCHANGED, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static string CheckIncompatiblePrograms()
        {
            foreach (var item in incompatiblePrograms)
            {
                if (item.Value != null)
                {
                    try
                    {
                        Mutex mutex = null;
                        try
                        {
                            if (Mutex.TryOpenExisting(item.Value, out mutex))
                            {
                                return item.Key;
                            }
                        }
                        finally
                        {
                            mutex?.Dispose();
                        }
                    }
                    catch { } //skipping
                }
                else
                {
                    try
                    {
                        var proc = Process.GetProcessesByName(item.Key);
                        if (proc.Count() != 0)
                        {
                            return item.Key;
                        }
                    }
                    catch { } //skipping
                }
            }
            return null;
        }

        /// <summary>
        /// Quickly computes the average color of image file.
        /// </summary>
        /// <param name="imgPath">Image file path.</param>
        /// <returns></returns>
        public static Color GetAverageColor(string imgPath)
        {
            //avg of colors by resizing to 1x1..
            using var image = new MagickImage(imgPath);
            //same as resize with box filter, Sample(1,1) was unreliable although faster..
            image.Scale(1, 1);

            //take the new pixel..
            using var pixels = image.GetPixels();
            var color = pixels.GetPixel(0, 0).ToColor();

            //ImageMagick color range is 0 - 65535.
            return Color.FromArgb(255 * color.R / 65535, 255 * color.G / 65535, 255 * color.B / 65535);
        }

        #endregion //helpers

        #region pinvoke {undocumented}

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_FLUENT = 4 //don't like alpha = 0
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public uint GradientColor; //AABBGGRR
            public int AnimationId;
        }

        #endregion //pinvoke {undocumented}
    }
}
