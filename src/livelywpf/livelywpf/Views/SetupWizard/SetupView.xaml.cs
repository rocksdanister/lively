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
        private int index = 0;
        private bool _isClosable = false;
        private readonly List<object> pages = new List<object>() { 
            new PageWelcome(),
            new PageStartup(),
            //new PageDirectory(),
            new PageUI(),
            new PageTaskbar(),
            new PageFinal() 
        };

        public SetupView()
        {
            InitializeComponent();
            if (SystemInfo.CheckWindowsNorKN())
            {
                pages.Insert(pages.Count - 1, new PageWindowsN());
            }

            var pageWait = new PagePleaseWait();
            pageWait.ProcessCompleted += (s, e) =>
            {
                NavigateNext();
                NextBtn.Visibility = Visibility.Visible;
            };
            ContentFrame.Navigate(pageWait, new SuppressNavigationTransitionInfo());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigateNext();
        }

        private void NavigateNext()
        {
            if (Program.IsMSIX)
            {
                //Finish button is visible for Store app.
                if ((index + 1) == pages.Count)
                {
                    NextBtn.Content = Properties.Resources.TextOK;
                }

                if ((index) == pages.Count)
                {
                    //final page.
                    Program.ShowMainWindow();
                    //ExitWindow(); //ShowMainWindow() calls it its visible.
                }
                else
                {
                    ContentFrame.Navigate(pages[index], new EntranceNavigationTransitionInfo());
                }
            }
            else
            {
                if ((index + 1) == pages.Count)
                {
                    NextBtn.Visibility = Visibility.Collapsed;
                    //_isClosable = true;
                }
                ContentFrame.Navigate(pages[index], new EntranceNavigationTransitionInfo());
            }
            index++;
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
