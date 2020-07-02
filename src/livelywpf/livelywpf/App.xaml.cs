using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MainWindow AppWindow { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            AppWindow = new MainWindow();
            //uwp root app needs this it seems.. is it possible to skip?
            AppWindow.Show();
            AppWindow.Hide();

            base.OnStartup(e);
        }
    }
}
