using livelywpf.Helpers;
using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Windows;

namespace livelywpf.Views.Screen
{
    /// <summary>
    /// Interaction logic for ScreenLayoutView.xaml
    /// </summary>
    public partial class ScreenLayoutView : Window
    {
        private readonly List<ScreenLabelView> screenLabels = new List<ScreenLabelView>();

        public ScreenLayoutView()
        {
            InitializeComponent();
            var vm = App.Services.GetRequiredService<ScreenLayoutViewModel>();
            this.DataContext = vm;
            this.Closing += vm.OnWindowClosing;
            CreateLabelWindows();

            ScreenHelper.DisplayUpdated += ScreenHelper_DisplayUpdated;
        }

        private void ScreenHelper_DisplayUpdated(object sender, EventArgs e)
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
            var screens = ScreenHelper.GetScreen();
            if (screens.Count > 1)
            {
                screens.ForEach(screen =>
                {
                    var labelWindow = new ScreenLabelView(screen.DeviceNumber, screen.Bounds.Left + 10, screen.Bounds.Top + 10);
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
            ScreenHelper.DisplayUpdated -= ScreenHelper_DisplayUpdated;
            CloseLabelWindows();
        }
    }
}
