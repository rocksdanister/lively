using H.Hooks;
using Lively.Common.Helpers.Pinvoke;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using WinUIEx;

namespace Lively.UI.WinUI.Views.LivelyProperty
{
    public sealed partial class EyeDropper : WindowEx
    {
        public Color? SelectedColor { get; private set; }

        private readonly LowLevelMouseHook hook;

        public EyeDropper()
        {
            this.InitializeComponent();
            this.SystemBackdrop = new DesktopAcrylicBackdrop();
            //White border issue: https://github.com/microsoft/WindowsAppSDK/issues/2772
            this.IsTitleBarVisible = false;

            hook = new LowLevelMouseHook()
            {
                GenerateMouseMoveEvents = true,
                Handling = true
            };
            hook.Move += (_, e) =>
            {
                SetPosAndForeground(e.Position.X + 25, e.Position.Y + 25);
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    SelectedColor = GetColorAt(e.Position.X, e.Position.Y);
                    SetPreviewColor((Color)SelectedColor);
                });
            };
            hook.Down += (_, e) =>
            {
                e.IsHandled = true; //Mouse click block
                if (e.Keys.Values.Contains(Key.MouseLeft))
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        SelectedColor = GetColorAt(e.Position.X, e.Position.Y);
                        this.Close();
                    });
                }
                else //discard
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        SelectedColor = null;
                        this.Close();
                    });
                }
            };
            hook.Start();
            
            this.Closed += (_, _) =>
            {
                hook?.Dispose();
            };

            this.Activated += (_, _) =>
            {
                //White border temp fix
                var styleCurrentWindowStandard = NativeMethods.GetWindowLongPtr(this.GetWindowHandle(), (int)NativeMethods.GWL.GWL_STYLE);
                var styleNewWindowStandard = styleCurrentWindowStandard.ToInt64() & ~((long)NativeMethods.WindowStyles.WS_THICKFRAME);
                if (NativeMethods.SetWindowLongPtr(new HandleRef(null, this.GetWindowHandle()), (int)NativeMethods.GWL.GWL_STYLE, (IntPtr)styleNewWindowStandard) == IntPtr.Zero)
                {
                    //fail
                }

                if (IsWindows11_OrGreater)
                {
                    //Bring back win11 rounded corner
                    var attribute = NativeMethods.DWMWINDOWATTRIBUTE.WindowCornerPreference;
                    var preference = NativeMethods.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                    NativeMethods.DwmSetWindowAttribute(this.GetWindowHandle(), attribute, ref preference, sizeof(uint));
                }

                if (NativeMethods.GetCursorPos(out NativeMethods.POINT P))
                {
                    //Force redraw
                    NativeMethods.SetWindowPos(this.GetWindowHandle(), -1, P.X + 25, P.Y + 25, 1, 1, (int)NativeMethods.SetWindowPosFlags.SWP_SHOWWINDOW);
                }
            };
        }

        private void SetPreviewColor(Color color)
        {
            cBorder.Background = new SolidColorBrush(Color.FromArgb(
                              255,
                              color.R,
                              color.G,
                              color.B
                            ));
            cText.Text = $"rgb({color.R}, {color.G}, {color.B})";
        }

        public void SetPosAndForeground(int x, int y)
        {
            NativeMethods.SetWindowPos(this.GetWindowHandle(), -1, x, y, 0, 0, (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE);
        }

        #region helpers

        public static Color GetColorAt(int x, int y)
        {
            IntPtr desk = NativeMethods.GetDesktopWindow();
            IntPtr dc = NativeMethods.GetWindowDC(desk);
            try
            {
                int a = (int)NativeMethods.GetPixel(dc, x, y);
                return Color.FromArgb(255, (byte)((a >> 0) & 0xff), (byte)((a >> 8) & 0xff), (byte)((a >> 16) & 0xff));
            }
            finally
            {
                NativeMethods.ReleaseDC(desk, dc);
            }
        }

        public static bool IsWindows11_OrGreater => Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;

        #endregion //helpers
    }
}
