using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using livelywpf.Core;
using ModernWpf.Media.Animation;

namespace livelywpf.Cef
{
    /// <summary>
    /// Interaction logic for LivelyPropertiesWindow.xaml
    /// </summary>
    public partial class LivelyPropertiesWindow : Window
    {
        public LivelyPropertiesWindow(LibraryModel model)
        {
            InitializeComponent();
            ContentFrame.Navigate(new Cef.LivelyPropertiesView(model), new SuppressNavigationTransitionInfo());
        }

        #region window move/resize lock

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
        }

        //prevent window resize and move during recording.
        //ref: https://stackoverflow.com/questions/3419909/how-do-i-lock-a-wpf-window-so-it-can-not-be-moved-resized-minimized-maximized
        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == (int)NativeMethods.WM.WINDOWPOSCHANGING)
            {
                var wp = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(lParam);
                wp.flags |= (int)NativeMethods.SetWindowPosFlags.SWP_NOMOVE | (int)NativeMethods.SetWindowPosFlags.SWP_NOSIZE;
                Marshal.StructureToPtr(wp, lParam, false);
            }
            return IntPtr.Zero;
        }

        #endregion //window move/resize lock
    }
}
