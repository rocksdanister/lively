using Lively.Common;
using Lively.UI.WinUI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NLog;
using System;

namespace Lively.UI.WinUI.Views.Pages
{
    public sealed partial class ShuffleView : Page
    {
        private ShuffleViewModel _viewModel;
        public ShuffleView()
        {
            this.InitializeComponent();
            this.Unloaded += ShuffleView_Unloaded;

            _viewModel = App.Services.GetRequiredService<ShuffleViewModel>();
            this.DataContext = _viewModel;
        }

        private void ShuffleView_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel?.PageUnloaded();
        }
    }
}