using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace livelywpf.Views.SetupWizard
{
    /// <summary>
    /// Interaction logic for PageStartup.xaml
    /// </summary>
    public partial class PageStartup : Page
    {
        public PageStartup()
        {
            InitializeComponent();
            //this.DataContext = App.Services.GetRequiredService<SettingsViewModel>();
        }
    }
}
