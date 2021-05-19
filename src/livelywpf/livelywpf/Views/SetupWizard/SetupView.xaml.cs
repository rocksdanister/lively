using ModernWpf.Controls.Primitives;
using ModernWpf.Media.Animation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

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
            Initialize();
        }

        private async void Initialize()
        {
            //extraction of default wallpaper.
            Program.SettingsVM.Settings.WallpaperBundleVersion = await Task.Run(() => 
                App.ExtractWallpaperBundle(Program.SettingsVM.Settings.WallpaperBundleVersion));
            Program.SettingsVM.UpdateConfigFile();
            Program.LibraryVM.WallpaperDirectoryUpdate();

            //windows codec install page.
            if (SystemInfo.CheckWindowsNorKN())
            {
                pages.Insert(pages.Count - 1, new PageWindowsN());
            }

            //setup pages..
            pleaseWaitPanel.Visibility = Visibility.Collapsed;
            nextBtn.Visibility = Visibility.Visible;
            NavigateNext();
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
                    nextBtn.Content = Properties.Resources.TextOK;
                }

                if ((index) == pages.Count)
                {
                    //final page.
                    Program.ShowMainWindow();
                    //ExitWindow(); //ShowMainWindow() calls it its visible.
                }
                else
                {
                    contentFrame.Navigate(pages[index], new EntranceNavigationTransitionInfo());
                }
            }
            else
            {
                if ((index + 1) == pages.Count)
                {
                    nextBtn.Visibility = Visibility.Collapsed;
                    //_isClosable = true;
                }
                contentFrame.Navigate(pages[index], new EntranceNavigationTransitionInfo());
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
                FlyoutBase.ShowAttachedFlyout((FrameworkElement)nextBtn);
            }
        }
    }
}
