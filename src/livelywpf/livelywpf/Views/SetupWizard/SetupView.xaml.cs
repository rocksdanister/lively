using ModernWpf;
using ModernWpf.Controls.Primitives;
using ModernWpf.Media.Animation;
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

namespace livelywpf.Views.SetupWizard
{
    /// <summary>
    /// Interaction logic for SetupView.xaml
    /// </summary>
    public partial class SetupView : Window
    {
        int index = 0;
        bool _isClosable = false;
        readonly List<object> pages = new List<object>() { 
            new PageWelcome(),
            new PageStartup(),
            new PageDirectory(),
            new PageUI(),
            new PageFinal() 
        };

        public SetupView()
        {
            InitializeComponent();
            ContentFrame.Navigate(pages[index], new DrillInNavigationTransitionInfo());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            index++;
            if ((index + 1) == pages.Count)
            {
                NextBtn.IsEnabled = false;
                NextBtn.Visibility = Visibility.Collapsed;
                //_isClosable = true;
            }
            ContentFrame.Navigate(pages[index], new DrillInNavigationTransitionInfo());
        }

        public void ExitWindow()
        {
            _isClosable = true;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isClosable)
            {
                e.Cancel = true;
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)NextBtn);
            }
        }
    }
}
