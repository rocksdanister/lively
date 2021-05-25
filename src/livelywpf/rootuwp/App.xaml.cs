using Microsoft.Toolkit.Win32.UI.XamlHost;

namespace rootuwp
{
    public sealed partial class App : XamlApplication
    {
        public App()
        {
            this.Initialize();

            // Hide the Xaml Island window
            var coreWindow = Windows.UI.Core.CoreWindow.GetForCurrentThread();
            var coreWindowInterop = Interop.GetInterop(coreWindow);
            NativeMethods.ShowWindow(coreWindowInterop.WindowHandle, Interop.SW_HIDE);
        }
    }
}
