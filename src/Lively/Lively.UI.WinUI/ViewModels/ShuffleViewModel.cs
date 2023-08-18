using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Lively.Common.Helpers.MVVM;
using Lively.Grpc.Client;
using Lively.Models;
using Lively.UI.WinUI.Views.Pages;
using Microsoft.UI.Dispatching;
using NLog;
using Windows.Devices.Printers;
using Windows.System.Profile;

namespace Lively.UI.WinUI.ViewModels
{
    public class ShuffleViewModel : ObservableObject
    {

        public event EventHandler OnRequestClose;
        private readonly DispatcherQueue dispatcherQueue;

        private readonly IDisplayManagerClient displayManager;
        private readonly IDesktopCoreClient desktopCore;
        private readonly IUserSettingsClient userSettings;

        public ShuffleViewModel(IDesktopCoreClient desktopCore, IDisplayManagerClient displayManager, IUserSettingsClient userSettings)
        {
            this.userSettings = userSettings;
            this.desktopCore = desktopCore;
            //DebugBoxText = userSettings.Settings.DoRandomWallpaper.ToString();
            EnableShuffle = userSettings.Settings.DoRandomWallpaper;
            TimeToChangeWallpaper = userSettings.Settings.TimeToChangeWallpaper.ToString();
            

            /*try
            {
                this.EnableShuffle = userSettings.Settings.DoRandomWallpaper;
            }
            catch(Exception e)
            {
                this.EnableShuffle = true;
                DebugBoxText = e.ToString();
            }*/



            dispatcherQueue = DispatcherQueue.GetForCurrentThread() ?? DispatcherQueueController.CreateOnCurrentThread().DispatcherQueue;
        }

        private bool _enableShuffle;
        public bool EnableShuffle
        {
            get { return _enableShuffle; }
            set
            {
                _enableShuffle = value;
                if (userSettings.Settings.DoRandomWallpaper != _enableShuffle)
                {
                    userSettings.Settings.DoRandomWallpaper = _enableShuffle;
                    UpdateSettingsConfigFile();
                }
                OnPropertyChanged();
            }
        }

        private string _timeToChangeWallpaper;
        public string TimeToChangeWallpaper
        {
            get { return _timeToChangeWallpaper; }
            set
            {
                _timeToChangeWallpaper = value;
                if (userSettings.Settings.TimeToChangeWallpaper.ToString() != _timeToChangeWallpaper)
                {
                    try
                    {
                        userSettings.Settings.TimeToChangeWallpaper = Int32.Parse(_timeToChangeWallpaper);
                        UpdateSettingsConfigFile();
                    }
                    catch { }
                }
                OnPropertyChanged();
            }
        }

        private string _debugBoxText;
        public string DebugBoxText
        {
            get { return _debugBoxText; }
            set
            {
                _debugBoxText = value;
                OnPropertyChanged();
            }
        }

        public void UpdateSettingsConfigFile()
        {
            _ = dispatcherQueue.TryEnqueue(() =>
            {
                userSettings.Save<ISettingsModel>();
            });
        }

        public void PageUnloaded()
        {
            desktopCore.SetWallpaperLoop(userSettings.Settings.DoRandomWallpaper, userSettings.Settings.TimeToChangeWallpaper);
        }

    }
}
