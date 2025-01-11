using Lively.Common.Helpers.Pinvoke;
using Lively.Grpc.Client;
using Lively.UI.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Media;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.LivelyProperty
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LivelyPropertiesTray : WindowEx
    {
        public LivelyPropertiesTray(CustomiseWallpaperViewModel viewModel)
        {
            this.InitializeComponent();
            this.SystemBackdrop = new MicaBackdrop();
            this.SetTitleBarBackgroundColors(((SolidColorBrush)App.Current.Resources["SystemControlBackgroundChromeMediumLowBrush"]).Color);

            //Position primary screen bottom-right
            var display = App.Services.GetRequiredService<IDisplayManagerClient>().PrimaryMonitor;
            if (display is not null)
            {
                var dpi = NativeMethods.GetDpiForWindow(this.GetWindowHandle());
                var scalingFactor = (float)dpi / 96;
                var width = (int)(375 * scalingFactor);
                var height = (int)(900 * scalingFactor);
                var left = display.WorkingArea.Right - width - 5;
                var top = display.WorkingArea.Bottom - height - 10;
                NativeMethods.SetWindowPos(this.GetWindowHandle(), -2, left, top, width, height, (int)NativeMethods.SetWindowPosFlags.SWP_SHOWWINDOW);
            }

            contentFrame.Navigate(typeof(LivelyPropertiesView), viewModel);
        }
    }
}
