using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Views.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        public LivelyPropertiesTray(ILibraryModel model)
        {
            this.InitializeComponent();
            this.Title = model.Title;
            this.SetTitleBarBackgroundColors(((SolidColorBrush)App.Current.Resources["SystemControlBackgroundChromeMediumLowBrush"]).Color);

            //Position primary screen bottom-right
            var display = App.Services.GetRequiredService<IDisplayManagerClient>().DisplayMonitors.FirstOrDefault(x => x.IsPrimary);
            double width = this.Width, height = this.Height;
            if (display is not null)
            {
                var left = display.WorkingArea.Right - width * 1.5f;
                var top = display.WorkingArea.Bottom - height * 1.5f;
                this.MoveAndResize(left, top, width, height);
            }

            contentFrame.Navigate(typeof(LivelyPropertiesView), model);
        }
    }
}
