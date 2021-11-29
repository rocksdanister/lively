using livelywpf.Helpers.Hardware;
using livelywpf.Models;
using livelywpf.Services;
using livelywpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls.Primitives;
using ModernWpf.Media.Animation;
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
            //new PageUI(),
            //new PageWeather(),
            new PageTaskbar(),
            new PageFinal()
        };
        private readonly IUserSettingsService userSettings;

        public SetupView(IUserSettingsService userSettings)
        {
            this.userSettings = userSettings;

            InitializeComponent();
            this.DataContext = App.Services.GetRequiredService<SettingsViewModel>();
            SetupDefaultWallpapers();
        }

        private async void SetupDefaultWallpapers()
        {
            //extraction of default wallpaper.
            userSettings.Settings.WallpaperBundleVersion = await Task.Run(() =>
                App.ExtractWallpaperBundle(userSettings.Settings.WallpaperBundleVersion));
            userSettings.Save<ISettingsModel>();
            App.Services.GetRequiredService<LibraryViewModel>().WallpaperDirectoryUpdate();

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
            if (Constants.ApplicationType.IsMSIX)
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
