using Lively.UI.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Lively.UI.WinUI.Views.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScreenLayoutView : Page
    {
        public ScreenLayoutView()
        {
            this.InitializeComponent();
            var vm = App.Services.GetRequiredService<ScreenLayoutViewModel>();
            this.DataContext = vm;
            //this.Unloaded += vm.OnWindowClosing;
            this.Unloaded += ScreenLayoutView_Unloaded;
        }

        private void ScreenLayoutView_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Unloaded screenlayoutview");
        }
    }
}
