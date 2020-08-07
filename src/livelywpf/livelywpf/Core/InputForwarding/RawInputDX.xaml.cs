using System;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Linearstar.Windows.RawInput;

namespace livelywpf.Core
{
    /// <summary>
    /// Mouseinput retrival and forwarding to wallpaper using DirectX RawInput.
    /// ref: https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input
    /// </summary>
    public partial class RawInputDX : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public InputForwardMode InputMode { get; private set; }
        public RawInputDX(InputForwardMode inputMode)
        {
            InitializeComponent();
            //Starting a hidden window outside screen region.
            //todo: Other wrappers such as SharpDX:https://github.com/sharpdx/SharpDX does not require a window, could not get it to work properly globally.. investigate.
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = -99999;
            SourceInitialized += Window_SourceInitialized;
            this.InputMode = inputMode;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var hwnd = windowInteropHelper.Handle;

            if (InputMode == InputForwardMode.mouse)
            {
                //ExInputSink flag makes it work even when not in foreground, similar to global hook.. but asynchronous, no complications and no AV false detection!
                RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse,
                    RawInputDeviceFlags.ExInputSink, hwnd);
            }
            else if(InputMode == InputForwardMode.mousekeyboard)
            {
                throw new NotImplementedException();
            }

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(Hook);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (InputMode == InputForwardMode.mouse)
            {
                RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
            }
        }

        protected IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            const int WM_INPUT = 0x00FF;

            // You can read inputs by processing the WM_INPUT message.
            if (msg == WM_INPUT)
            {
                // Create an RawInputData from the handle stored in lParam.
                var data = RawInputData.FromHandle(lparam);

                // You can identify the source device using Header.DeviceHandle or just Device.
                //var sourceDeviceHandle = data.Header.DeviceHandle;
                //var sourceDevice = data.Device;

                // The data will be an instance of either RawInputMouseData, RawInputKeyboardData, or RawInputHidData.
                // They contain the raw input data in their properties.
                switch (data)
                {
                    case RawInputMouseData mouse:
                        //RawInput only gives relative mouse movement value.. cheating here with Winform library.
                        var M = System.Windows.Forms.Control.MousePosition;
                        switch (mouse.Mouse.Buttons)
                        {
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.LeftButtonDown:
                                MouseLBtnDownSimulate(M.X, M.Y);
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.LeftButtonUp:
                                MouseLBtnUpSimulate(M.X, M.Y);
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.RightButtonDown:
                                //issue: click being skipped.
                                //SetupDesktop.MouseRBtnDownSimulate(M.X, M.Y);
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.RightButtonUp:
                                //issue: click being skipped.
                                //SetupDesktop.MouseRBtnUpSimulate(M.X, M.Y);
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None:
                                MouseMoveSimulate(M.X, M.Y);
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.MouseWheel:
                                /*
                                https://github.com/ivarboms/game-engine/blob/master/Input/RawInput.cpp
                                Mouse wheel deltas are represented as multiples of 120.
                                MSDN: The delta was set to 120 to allow Microsoft or other vendors to build
                                finer-resolution wheels (a freely-rotating wheel with no notches) to send more
                                messages per rotation, but with a smaller value in each message.
                                Because of this, the value is converted to a float in case a mouse's wheel
                                reports a value other than 120, in which case dividing by 120 would produce
                                a very incorrect value.
                                More info: http://social.msdn.microsoft.com/forums/en-US/gametechnologiesgeneral/thread/1deb5f7e-95ee-40ac-84db-58d636f601c7/
                                */

                                //Disabled, not tested yet.
                                /*
                                // One wheel notch is represented as this delta (WHEEL_DELTA).
                                const float oneNotch = 120;

                                // Mouse wheel delta in multiples of WHEEL_DELTA (120).
                                float mouseWheelDelta = mouse.Mouse.RawButtons;

                                // Convert each notch from [-120, 120] to [-1, 1].
                                mouseWheelDelta = mouseWheelDelta / oneNotch;

                                MouseScrollSimulate(mouseWheelDelta);
                                */
                                break;
                        }
                        break;
                }
            }
            return IntPtr.Zero;
        }

        public static void MouseMoveSimulate(int x, int y)
        {
            ForwardMessage(x, y, (int)NativeMethods.WM.MOUSEMOVE, (IntPtr)0x0020);
        }

        public static void MouseLBtnDownSimulate(int x, int y)
        {
            ForwardMessage(x, y, (int)NativeMethods.WM.LBUTTONDOWN, (IntPtr)0x0001);
        }

        public static void MouseLBtnUpSimulate(int x, int y)
        {
            ForwardMessage(x, y, (int)NativeMethods.WM.LBUTTONUP, (IntPtr)0x0001);
        }

        public static void MouseRBtnDownSimulate(int x, int y)
        {
            ForwardMessage(x, y, (int)NativeMethods.WM.RBUTTONDOWN, (IntPtr)0x0002);
        }

        public static void MouseRBtnUpSimulate(int x, int y)
        {
            ForwardMessage(x, y, (int)NativeMethods.WM.RBUTTONUP, (IntPtr)0x0002);
        }

        /// <summary>
        /// Forwards the message to the required wallpaper window based on given cursor location.
        /// Skips if apps are in foreground.
        /// </summary>
        /// <param name="x">Cursor pos x</param>
        /// <param name="y">Cursor pos y</param>
        /// <param name="msg">window message</param>
        /// <param name="wParam">additional msg parameter</param>
        private static void ForwardMessage(int x, int y, int msg, IntPtr wParam)
        {
            //Don't forward when not on desktop.
            if (!Playback.IsDesktop())
            {
                if (msg != (int)NativeMethods.WM.MOUSEMOVE || !Program.SettingsVM.Settings.MouseInputMovAlways)
                {
                    return;
                }
            }

            try
            {
                var display = Screen.FromPoint(new System.Drawing.Point(x, y));
                var mouse = CalculateMousePos(x, y, display);

                SetupDesktop.Wallpapers.ForEach(x =>
                {
                    if (x.GetWallpaperType() == WallpaperType.web ||
                    x.GetWallpaperType() == WallpaperType.webaudio ||
                    x.GetWallpaperType() == WallpaperType.app ||
                    x.GetWallpaperType() == WallpaperType.url ||
                    x.GetWallpaperType() == WallpaperType.bizhawk ||
                    x.GetWallpaperType() == WallpaperType.unity ||
                    x.GetWallpaperType() == WallpaperType.godot)
                    {
                        //The low-order word specifies the x-coordinate of the cursor, the high-order word specifies the y-coordinate of the cursor.
                        //ref: https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousemove
                        UInt32 lParam = (uint)mouse.Y;
                        lParam <<= 16;
                        lParam |= (uint)mouse.X;
                        NativeMethods.PostMessageW(x.GetHWND(), msg, wParam, (IntPtr)lParam);
                    }
                });
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
            }
        }

        /// <summary>
        /// Converts global mouse cursor position value to per display localised value.
        /// </summary>
        /// <param name="x">Cursor pos x</param>
        /// <param name="y">Cursor pos y</param>
        /// <param name="display">Target display device</param>
        /// <returns>Localised cursor value</returns>
        private static Point CalculateMousePos(int x, int y, Screen display)
        {
            if (ScreenHelper.IsMultiScreen())
            {
                if (Program.SettingsVM.Settings.WallpaperArrangement == WallpaperArrangement.span)
                {
                    x -= SystemInformation.VirtualScreen.Location.X;
                    y -= SystemInformation.VirtualScreen.Location.Y;
                }
                else //per-display or duplicate mode.
                {
                    if (x < 0)
                    {
                        x = SystemInformation.VirtualScreen.Width + x - Screen.PrimaryScreen.Bounds.Width;
                    }
                    else
                    {
                        x -= Math.Abs(display.Bounds.X);
                    }

                    if (y < 0)
                    {
                        y = SystemInformation.VirtualScreen.Height + y - Screen.PrimaryScreen.Bounds.Height;
                    }
                    else
                    {
                        y -= Math.Abs(display.Bounds.Y);
                    }
                }
            }
            return new Point(x, y);
        }
    }
}
