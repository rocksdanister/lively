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
using System.Windows.Shapes;
using Windows.Media.Core;

namespace livelywpf
{
    /// <summary>
    /// Interaction logic for MediaElementUWP.xaml
    /// </summary>
    public partial class MediaElementUWP : Window
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public MediaElementUWP(string path, int playSpeed)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //ShowInTaskbar = false :- causing issue with windows10 Taskview.
            //SetupDesktop.RemoveWindowFromTaskbar(new WindowInteropHelper(this).Handle);
        }
    }
}
