using Lively.Common;
using Lively.Common.Helpers.Archive;
using Lively.Grpc.Client;
using Lively.Helpers.Hardware;
using Lively.Models;
using Lively.UI.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls.Primitives;
using ModernWpf.Media.Animation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Lively.UI.Wpf.Views.SetupWizard
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
            //new PageTaskbar(),
            new PageFinal()
        };

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IUserSettingsClient userSettings;
        private readonly ICommandsClient commands;
        private readonly IDesktopCoreClient desktopCore;
        private readonly SettingsViewModel settingsVm;

        public SetupView(IUserSettingsClient userSettings, ICommandsClient commands, IDesktopCoreClient desktopCore ,SettingsViewModel settingsVm)
        {
            this.userSettings = userSettings;
            this.commands = commands;
            this.settingsVm = settingsVm;
            this.desktopCore = desktopCore;

            InitializeComponent();
            this.DataContext = settingsVm;
            //windows codec install page.
            if (SystemInfo.CheckWindowsNorKN())
            {
                pages.Insert(pages.Count - 1, new PageWindowsN());
            }

            SetupDefaultWallpapers();
        }

        private async void SetupDefaultWallpapers()
        {
            //extraction of default wallpaper.
            userSettings.Settings.WallpaperBundleVersion = await Task.Run(() => ExtractWallpaperBundle(userSettings.Settings.WallpaperBundleVersion));
            await userSettings.SaveAsync<ISettingsModel>();
            //re-scan libraryVm items..
            await this.Dispatcher.Invoke(async () =>
            {
                await settingsVm.WallpaperDirectoryChange(userSettings.Settings.WallpaperDir);
                //setup pages..
                pleaseWaitPanel.Visibility = Visibility.Collapsed;
                nextBtn.Visibility = Visibility.Visible;
                NavigateNext();
            });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigateNext();
        }

        private void NavigateNext()
        {
            if ((index + 1) == pages.Count)
            {
                nextBtn.Content = Properties.Resources.TextOK;
            }

            if (index == pages.Count)
            {
                _ = commands.ShowUI();
                ExitWindow();
            }
            else
            {
                _ = contentFrame.Navigate(pages[index], new EntranceNavigationTransitionInfo());
            }
            index++;
        }

        private void ExitWindow()
        {
            userSettings.Settings.IsFirstRun = false;
            _ = userSettings.SaveAsync<ISettingsModel>();
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

        /// <summary>
        /// Extract default wallpapers and incremental if any.
        /// </summary>
        private int ExtractWallpaperBundle(int currentBundleVer)
        {
            //Lively stores the last extracted bundle filename, extraction proceeds from next file onwards.
            int maxExtracted = currentBundleVer;
            try
            {
                //wallpaper bundles filenames are 0.zip, 1.zip ...
                var sortedBundles = Directory.GetFiles(
                    Path.Combine(desktopCore.BaseDirectory, "bundle"))
                    .OrderBy(x => x);

                foreach (var item in sortedBundles)
                {
                    if (int.TryParse(Path.GetFileNameWithoutExtension(item), out int val))
                    {
                        if (val > maxExtracted)
                        {
                            //Sharpzip library will overwrite files if exists during extraction.
                            ZipExtract.ZipExtractFile(item, Path.Combine(userSettings.Settings.WallpaperDir, Constants.CommonPartialPaths.WallpaperInstallDir), false);
                            maxExtracted = val;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
            return maxExtracted;
        }
    }
}
