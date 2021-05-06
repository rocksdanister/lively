using ImageMagick;
using System;
using System.Drawing;
using System.Drawing.Imaging;
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
        IntPtr mTaskbar, sTaskbar;
        public bool IsRunning { get; private set; } = false;
        private TaskbarTheme taskbarTheme = TaskbarTheme.none;
        private readonly System.Timers.Timer _timer = new System.Timers.Timer();
        private static readonly TransparentTaskbar instance = new TransparentTaskbar();
        private AccentPolicy accentPolicy = new AccentPolicy();
        private Color accentColor = Color.FromArgb(0, 0, 0);
        private readonly static IDictionary<string, string> incompatiblePrograms = new Dictionary<string, string>() { 
            {"TranslucentTB", "344635E9-9AE4-4E60-B128-D53E25AB70A7"},
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
            Initialize();
        }

        private void Initialize()
        {
            mTaskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
            sTaskbar = NativeMethods.FindWindow("Shell_SecondaryTrayWnd", null);

            _timer.Interval = 500;
            _timer.Elapsed += Timer_Elapsed;
        }

        public void Start()
        {
            if (Program.IsMSIX || IsRunning)
                return;

            _timer.Start();
            IsRunning = true;
        }

        public void Stop()
        {
            if (IsRunning)
            {
                _timer.Stop();
                ResetTaskbar();
                IsRunning = false;
                
            }
        }

        public void Reset()
        {
            _timer.Stop();
            mTaskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);
            sTaskbar = NativeMethods.FindWindow("Shell_SecondaryTrayWnd", null);
            if (IsRunning)
            {
                _timer.Start();
            }
        }

        public void SetTheme(TaskbarTheme theme)
        {
            _timer.Stop();
            switch (theme)
            {
                case TaskbarTheme.none:
                    //accent.AccentState = AccentState.ACCENT_DISABLED;
                    break;
                case TaskbarTheme.clear:
                    accentPolicy.GradientColor = 16777215; //00FFFFFF
                    accentPolicy.AccentState = AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT;
                    break;
                case TaskbarTheme.blur:
                    accentPolicy.GradientColor = 0;
                    accentPolicy.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                    break;
                case TaskbarTheme.color:
                    //todo
                    break;
                case TaskbarTheme.fluent:
                    accentPolicy.GradientColor = 167772160; //A000000
                    accentPolicy.AccentState = AccentState.ACCENT_ENABLE_FLUENT;
                    break;
                case TaskbarTheme.wallpaper:
                    accentPolicy.GradientColor = Convert.ToUInt32(string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", 200, accentColor.B, accentColor.G, accentColor.R), 16);
                    accentPolicy.AccentState = AccentState.ACCENT_ENABLE_TRANSPARENTGRADIENT;
                    break;
                case TaskbarTheme.wallpaperFluent:
                    accentPolicy.GradientColor = Convert.ToUInt32(string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", 125, accentColor.B, accentColor.G, accentColor.R), 16);
                    accentPolicy.AccentState = AccentState.ACCENT_ENABLE_FLUENT;
                    break;
            }
            taskbarTheme = theme;
            if (IsRunning)
            {
                ResetTaskbar();
                _timer.Start();
            }
        }

        public void SetAccentColor(Color color)
        {
            accentColor = color;
            SetTheme(taskbarTheme);
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SetTaskbarTransparent(taskbarTheme);
        }

        private void SetTaskbarTransparent(TaskbarTheme theme)
        {
            if (theme == TaskbarTheme.none)
                return;

            var accentPtr = IntPtr.Zero;
            try
            {
                var accentStructSize = Marshal.SizeOf(accentPolicy);
                accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accentPolicy, accentPtr, false);
                
                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    SizeOfData = accentStructSize,
                    Data = accentPtr
                };

                SetWindowCompositionAttribute(mTaskbar, ref data);
                if (!sTaskbar.Equals(IntPtr.Zero))
                {
                    SetWindowCompositionAttribute(sTaskbar, ref data);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        private void ResetTaskbar()
        {
            NativeMethods.SendMessage(mTaskbar, (int)NativeMethods.WM.DWMCOMPOSITIONCHANGED, IntPtr.Zero, IntPtr.Zero);
            NativeMethods.SendMessage(sTaskbar, (int)NativeMethods.WM.DWMCOMPOSITIONCHANGED, IntPtr.Zero, IntPtr.Zero);
        }

        #region helpers

        public static string CheckIncompatibleProgramsRunning()
        {
            foreach (var item in incompatiblePrograms)
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
