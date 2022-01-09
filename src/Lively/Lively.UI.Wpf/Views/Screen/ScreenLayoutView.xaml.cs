using Lively.Grpc.Client;
using Lively.UI.Wpf;
using Lively.UI.Wpf.ViewModels;
using Lively.UI.Wpf.Views.Screen;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Lively.UI.Wpf.Views.Screen
{
    /// <summary>
    /// Interaction logic for ScreenLayoutView.xaml
    /// </summary>
    public partial class ScreenLayoutView : Window
    {
        private readonly List<ScreenLabelView> screenLabels = new List<ScreenLabelView>();
        private readonly IDisplayManagerClient displayManager;

        public ScreenLayoutView()
        {
            this.displayManager = App.Services.GetRequiredService<IDisplayManagerClient>();

            InitializeComponent();
            var vm = App.Services.GetRequiredService<ScreenLayoutViewModel>();
            this.DataContext = vm;
            this.Closing += vm.OnWindowClosing;
            CreateLabelWindows();

            displayManager.DisplayChanged += DisplayManager_DisplayChanged;
        }

        private void DisplayManager_DisplayChanged(object sender, EventArgs e)
        {
            //Windows will move the label window if property change.
            //This is a lazy fix if display disconnect/reconnect.
            this.Dispatcher.BeginInvoke(new Action(() => {
                CloseLabelWindows();
                CreateLabelWindows();
            }));
        }

        private void CreateLabelWindows()
        {
            var screens = displayManager.DisplayMonitors.ToList();
            if (screens.Count > 1)
            {
                screens.ForEach(screen =>
                {
                    var labelWindow = new ScreenLabelView(screen.Index.ToString(), screen.Bounds.Left + 10, screen.Bounds.Top + 10);
                    labelWindow.Show();
                    screenLabels.Add(labelWindow);
                });
            }
        }

        private void CloseLabelWindows()
        {
            screenLabels.ForEach(x => x.Close());
            screenLabels.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            displayManager.DisplayChanged -= DisplayManager_DisplayChanged;
            CloseLabelWindows();
        }
    }
}
