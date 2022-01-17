using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Linearstar.Windows.RawInput;
using Lively.Common;
using Lively.Common.Helpers.Pinvoke;
using Lively.Core;
using Lively.Core.Display;
using Lively.Models;
using Lively.Services;
using Point = System.Drawing.Point;

namespace Lively.Views.WindowMsg
{
    public enum RawInputMouseBtn
    {
        left,
        right
    }

    public class MouseRawArgs : EventArgs
    {
        public int X { get; }
        public int Y { get; }
        public MouseRawArgs(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class MouseClickRawArgs : MouseRawArgs
    {
        public RawInputMouseBtn Button { get; }
        public MouseClickRawArgs(int x, int y, RawInputMouseBtn btn) : base(x, y)
        {
            Button = btn;
        }
    }

    public class KeyboardClickRawArgs : EventArgs
    {
        //todo
    }

    /// <summary>
    /// Mouseinput retrival and forwarding to wallpaper using DirectX RawInput.
    /// ref: https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input
    /// </summary>
    public partial class RawInputMsgWindow : Window
    {
        #region setup

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        IntPtr progman, workerWOrig;
        public InputForwardMode InputMode { get; private set; }
        //public events
        public event EventHandler<MouseRawArgs> MouseMoveRaw;
        public event EventHandler<MouseClickRawArgs> MouseDownRaw;
        public event EventHandler<MouseClickRawArgs> MouseUpRaw;
        public event EventHandler<KeyboardClickRawArgs> KeyboardClickRaw;
        //public event EventHandler<KeyboardClickRawArgs> KeyboardUpRaw;

        private readonly IUserSettingsService userSettings;
        private readonly IDesktopCore desktopCore;
        private readonly IDisplayManager displayManager;

        public RawInputMsgWindow(IUserSettingsService userSettings, IDesktopCore desktopCore, IDisplayManager displayManager)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            this.displayManager = displayManager;

            InitializeComponent();
            this.InputMode = InputForwardMode.mousekeyboard;
            desktopCore.WallpaperReset += (s, e) => FindDesktopHandles();
        }

        private void FindDesktopHandles()
        {
            //resetting
            workerWOrig = IntPtr.Zero;
            progman = IntPtr.Zero;

            progman = NativeMethods.FindWindow("Progman", null);
            var folderView = NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (folderView == IntPtr.Zero)
            {
                //If the desktop isn't under Progman, cycle through the WorkerW handles and find the correct one
                do
                {
                    workerWOrig = NativeMethods.FindWindowEx(NativeMethods.GetDesktopWindow(), workerWOrig, "WorkerW", null);
                    folderView = NativeMethods.FindWindowEx(workerWOrig, IntPtr.Zero, "SHELLDLL_DefView", null);
                } while (folderView == IntPtr.Zero && workerWOrig != IntPtr.Zero);
            }
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(this);
            var hwnd = windowInteropHelper.Handle;

            switch (InputMode)
            {
                case InputForwardMode.off:
                    this.Close();
                    break;
                case InputForwardMode.mouse:
                    //ExInputSink flag makes it work even when not in foreground and async..
                    RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse,
                        RawInputDeviceFlags.ExInputSink, hwnd);
                    break;
                case InputForwardMode.mousekeyboard:
                    RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse,
                        RawInputDeviceFlags.ExInputSink, hwnd);
                    RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard,
                        RawInputDeviceFlags.ExInputSink, hwnd);
                    break;
            }

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(Hook);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            switch (InputMode)
            {
                case InputForwardMode.off:
                    break;
                case InputForwardMode.mouse:
                    RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
                    break;
                case InputForwardMode.mousekeyboard:
                    RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
                    RawInputDevice.UnregisterDevice(HidUsageAndPage.Keyboard);
                    break;
            }
        }

        #endregion //setup

        #region input forward

        protected IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            // You can read inputs by processing the WM_INPUT message.
            if (msg == (int)NativeMethods.WM.INPUT)
            {
                // Create an RawInputData from the handle stored in lParam.
                var data = RawInputData.FromHandle(lparam);

                //You can identify the source device using Header.DeviceHandle or just Device.
                //var sourceDeviceHandle = data.Header.DeviceHandle;
                //var sourceDevice = data.Device;

                // The data will be an instance of either RawInputMouseData, RawInputKeyboardData, or RawInputHidData.
                // They contain the raw input data in their properties.
                switch (data)
                {
                    case RawInputMouseData mouse:
                        //RawInput only gives relative mouse movement value.. 
                        if (!NativeMethods.GetCursorPos(out NativeMethods.POINT P))
                        {
                            break;
                        }

                        switch (mouse.Mouse.Buttons)
                        {
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.LeftButtonDown:
                                ForwardMessageMouse(P.X, P.Y, (int)NativeMethods.WM.LBUTTONDOWN, (IntPtr)0x0001);
                                MouseDownRaw?.Invoke(this, new MouseClickRawArgs(P.X, P.Y, RawInputMouseBtn.left));
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.LeftButtonUp:
                                ForwardMessageMouse(P.X, P.Y, (int)NativeMethods.WM.LBUTTONUP, (IntPtr)0x0001);
                                MouseUpRaw?.Invoke(this, new MouseClickRawArgs(P.X, P.Y, RawInputMouseBtn.left));
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.RightButtonDown:
                                //issue: click being skipped; desktop already has its own rightclick contextmenu.
                                //ForwardMessage(M.X, M.Y, (int)NativeMethods.WM.RBUTTONDOWN, (IntPtr)0x0002);
                                MouseDownRaw?.Invoke(this, new MouseClickRawArgs(P.X, P.Y, RawInputMouseBtn.right));
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.RightButtonUp:
                                //issue: click being skipped; desktop already has its own rightclick contextmenu.
                                //ForwardMessage(M.X, M.Y, (int)NativeMethods.WM.RBUTTONUP, (IntPtr)0x0002);
                                MouseUpRaw?.Invoke(this, new MouseClickRawArgs(P.X, P.Y, RawInputMouseBtn.right));
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.None:
                                ForwardMessageMouse(P.X, P.Y, (int)NativeMethods.WM.MOUSEMOVE, (IntPtr)0x0020);
                                MouseMoveRaw?.Invoke(this, new MouseRawArgs(P.X, P.Y));
                                break;
                            case Linearstar.Windows.RawInput.Native.RawMouseButtonFlags.MouseWheel:
                                //Disabled, not tested yet.
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
                    case RawInputKeyboardData keyboard:
                        ForwardMessageKeyboard((int)keyboard.Keyboard.WindowMessage, 
                            (IntPtr)keyboard.Keyboard.VirutalKey, keyboard.Keyboard.ScanCode,
                            (keyboard.Keyboard.Flags != Linearstar.Windows.RawInput.Native.RawKeyboardFlags.Up));
                        KeyboardClickRaw?.Invoke(this, new KeyboardClickRawArgs());
                        break;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Forwards the keyboard message to the required wallpaper window based on given cursor location.<br/>
        /// Skips if desktop is not focused.
        /// </summary>
        /// <param name="msg">key press msg.</param>
        /// <param name="wParam">Virtual-Key code.</param>
        /// <param name="scanCode">OEM code of the key.</param>
        /// <param name="isPressed">Key is pressed.</param>
        private void ForwardMessageKeyboard(int msg, IntPtr wParam, int scanCode, bool isPressed)
        {
            try
            {
                //Don't forward when not on desktop.
                if (userSettings.Settings.InputForward == InputForwardMode.mousekeyboard && IsDesktop())
                {
                    //Detect active wp based on cursor pos, better way to do this?
                    if (!NativeMethods.GetCursorPos(out NativeMethods.POINT P))
                        return;

                    var display = displayManager.GetDisplayMonitorFromPoint(new Point(P.X, P.Y));
                    foreach (var wallpaper in desktopCore.Wallpapers)
                    {
                        if (IsInputAllowed(wallpaper.Category))
                        {
                            if (display.Equals(wallpaper.Screen) || userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span)
                            {
                                //ref:
                                //https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-keydown
                                //https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-keyup
                                uint lParam = 1u; //press
                                lParam |= (uint)scanCode << 16; //oem code
                                lParam |= 1u << 24; //extended key
                                lParam |= 0u << 29; //context code; Note: Alt key combos wont't work
                                /* Same as:
                                 * lParam = isPressed ? (lParam |= 0u << 30) : (lParam |= 1u << 30); //prev key state
                                 * lParam = isPressed ? (lParam |= 0u << 31) : (lParam |= 1u << 31); //transition state
                                 */
                                lParam = isPressed ? lParam : (lParam |= 3u << 30);
                                NativeMethods.PostMessageW(wallpaper.InputHandle, msg, wParam, (UIntPtr)lParam);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Keyboard Forwarding Error:" + e.Message);
            }
        }

        /// <summary>
        /// Forwards the mouse message to the required wallpaper window based on given cursor location.<br/>
        /// Skips if apps are in foreground.
        /// </summary>
        /// <param name="x">Cursor pos x</param>
        /// <param name="y">Cursor pos y</param>
        /// <param name="msg">mouse message</param>
        /// <param name="wParam">additional msg parameter</param>
        private void ForwardMessageMouse(int x, int y, int msg, IntPtr wParam)
        {
            if (userSettings.Settings.InputForward == InputForwardMode.off)
            {
                return;
            }
            else if (!IsDesktop()) //Don't forward when not on desktop.
            {
                if (msg != (int)NativeMethods.WM.MOUSEMOVE || !userSettings.Settings.MouseInputMovAlways)
                {
                    return;
                }
            }

            try
            {
                var display = displayManager.GetDisplayMonitorFromPoint(new System.Drawing.Point(x, y));
                var mouse = CalculateMousePos(x, y, display, userSettings.Settings.WallpaperArrangement);
                foreach (var wallpaper in desktopCore.Wallpapers)
                {
                    if (IsInputAllowed(wallpaper.Category))
                    {
                        if (wallpaper.Screen.Equals(display) || userSettings.Settings.WallpaperArrangement == WallpaperArrangement.span)
                        {
                            //The low-order word specifies the x-coordinate of the cursor, the high-order word specifies the y-coordinate of the cursor.
                            //ref: https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-mousemove
                            uint lParam = Convert.ToUInt32(mouse.Y);
                            lParam <<= 16;
                            lParam |= Convert.ToUInt32(mouse.X);
                            NativeMethods.PostMessageW(wallpaper.InputHandle, msg, wParam, (UIntPtr)lParam);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Mouse Forwarding Error:" + e.Message);
            }
        }

        #endregion //input forward

        #region helpers

        /// <summary>
        /// Converts global mouse cursor position value to per display localised value.
        /// </summary>
        /// <param name="x">Cursor pos x</param>
        /// <param name="y">Cursor pos y</param>
        /// <param name="display">Target display device</param>
        /// <returns>Localised cursor value</returns>
        private Point CalculateMousePos(int x, int y, IDisplayMonitor display, WallpaperArrangement arrangement)
        {
            if (displayManager.IsMultiScreen())
            {
                if (arrangement == WallpaperArrangement.span)
                {
                    var screenArea = displayManager.VirtualScreenBounds;
                    x -= screenArea.Location.X;
                    y -= screenArea.Location.Y;
                }
                else //per-display or duplicate mode.
                {
                    x += -1 * display.Bounds.X;
                    y += -1 * display.Bounds.Y;
                }
            }
            return new Point(x, y);
        }

        private static bool IsInputAllowed(WallpaperType category)
        {
            return category switch
            {
                WallpaperType.app => true,
                WallpaperType.web => true,
                WallpaperType.webaudio => true,
                WallpaperType.url => true,
                WallpaperType.bizhawk => true,
                WallpaperType.unity => true,
                WallpaperType.godot => true,
                WallpaperType.video => false,
                WallpaperType.gif => false,
                WallpaperType.unityaudio => true,
                WallpaperType.videostream => false,
                WallpaperType.picture => false,
                _ => false,
            };
        }

        /// <summary>
        /// Is foreground live-wallpaper desktop.
        /// </summary>
        /// <returns></returns>
        private bool IsDesktop()
        {
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            return (IntPtr.Equals(hWnd, workerWOrig) || IntPtr.Equals(hWnd, progman));
        }

        #endregion //helpers
    }
}
