using Lively.Common;
using Lively.Common.Helpers.Storage;
using Lively.Core.Display;
using Lively.Helpers;
using Lively.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Lively.Services
{
    public class JsonUserSettingsService : IUserSettingsService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string settingsPath = Constants.CommonPaths.UserSettingsPath;
        private readonly string appRulesPath = Constants.CommonPaths.AppRulesPath;
        private readonly string wallpaperLayoutPath = Constants.CommonPaths.WallpaperLayoutPath;
        //private readonly string weatherPath = Constants.CommonPaths.WeatherSettingsPath;

        public JsonUserSettingsService(IDisplayManager displayManager, ITransparentTbService ttbService)
        {
            Load<ISettingsModel>();
            //Load<IWeatherModel>();
            Load<List<IApplicationRulesModel>>();
            Load<List<IWallpaperLayoutModel>>();

            Settings.SelectedDisplay = Settings.SelectedDisplay != null ?
                displayManager.DisplayMonitors.FirstOrDefault(x => x.Equals(Settings.SelectedDisplay)) ?? displayManager.PrimaryDisplayMonitor :
                displayManager.PrimaryDisplayMonitor;

            Settings.VideoPlayer = IsVideoPlayerAvailable(Settings.VideoPlayer) ? Settings.VideoPlayer : LivelyMediaPlayer.mpv;
            Settings.GifPlayer = IsGifPlayerAvailable(Settings.GifPlayer) ? Settings.GifPlayer : LivelyGifPlayer.mpv;
            Settings.WebBrowser = IsWebPlayerAvailable(Settings.WebBrowser) ? Settings.WebBrowser : LivelyWebBrowser.cef;

            //previous installed appversion is different from current instance..    
            if (!Settings.AppVersion.Equals(Assembly.GetExecutingAssembly().GetName().Version.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Settings.AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Settings.IsUpdated = true;
            }

            //Ensure if the locale is supported..
            var lang = SupportedLanguages.GetLanguage(Settings.Language);
            if (lang.Codes.FirstOrDefault(x => x == Settings.Language) == null)
            {
                Settings.Language = lang.Codes[0];
            }

            //Restrictions on msix..
            //Settings.DesktopAutoWallpaper = Settings.DesktopAutoWallpaper && !Common.Constants.ApplicationType.IsMSIX;

            try
            {
                _ = WindowsStartup.SetStartup(Settings.Startup);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }

            if (Settings.SystemTaskbarTheme != TaskbarTheme.none)
            {
                ttbService.Start(Settings.SystemTaskbarTheme);
            }
        }

        public ISettingsModel Settings { get; private set; }
        //public IWeatherModel WeatherSettings { get; private set; }
        public List<IApplicationRulesModel> AppRules { get; private set; }
        public List<IWallpaperLayoutModel> WallpaperLayout { get; private set; }

        public void Save<T>()
        {
            if (typeof(T) == typeof(ISettingsModel))
            {
                JsonStorage<ISettingsModel>.StoreData(settingsPath, Settings);
            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
            {
                JsonStorage<List<IApplicationRulesModel>>.StoreData(appRulesPath, AppRules);
            }
            else if (typeof(T) == typeof(List<IWallpaperLayoutModel>))
            {
                JsonStorage<List<IWallpaperLayoutModel>>.StoreData(wallpaperLayoutPath, WallpaperLayout);
            }
            /*
            else if (typeof(T) == typeof(IWeatherModel))
            {
                JsonStorage<IWeatherModel>.StoreData(weatherPath, WeatherSettings);
            }
            */
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }

        public void Load<T>()
        {
            if (typeof(T) == typeof(ISettingsModel))
            {
                try
                {
                    Settings = JsonStorage<SettingsModel>.LoadData(settingsPath);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Settings = new SettingsModel();
                    Save<ISettingsModel>();
                }

            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
            {
                try
                {
                    AppRules = new List<IApplicationRulesModel>(JsonStorage<List<ApplicationRulesModel>>.LoadData(appRulesPath));
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    AppRules = new List<IApplicationRulesModel>
                    {
                        //defaults.
                        new ApplicationRulesModel("Discord", AppRulesEnum.ignore)
                    };
                    Save<List<IApplicationRulesModel>>();
                }
            }
            else if (typeof(T) == typeof(List<IWallpaperLayoutModel>))
            {
                try
                {
                    WallpaperLayout = new List<IWallpaperLayoutModel>(JsonStorage<List<WallpaperLayoutModel>>.LoadData(wallpaperLayoutPath));
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    WallpaperLayout = new List<IWallpaperLayoutModel>();
                    Save<List<IWallpaperLayoutModel>>();
                }
            }
            /*
            else if (typeof(T) == typeof(IWeatherModel))
            {
                try
                {
                    WeatherSettings = JsonStorage<WeatherModel>.LoadData(weatherPath);
                }
                catch (Exception e)
                {
                    WeatherSettings = new WeatherModel();
                    Save<IWeatherModel>();
                }

            }
            */
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }

        #region helpers

        private bool IsVideoPlayerAvailable(LivelyMediaPlayer mp)
        {
            return mp switch
            {
                LivelyMediaPlayer.libvlc => false, //depreciated
                LivelyMediaPlayer.libmpv => false, //depreciated
                LivelyMediaPlayer.wmf => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "wmf", "Lively.PlayerWmf.exe")),
                LivelyMediaPlayer.libvlcExt => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libVLCPlayer", "libVLCPlayer.exe")),
                LivelyMediaPlayer.libmpvExt => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer", "libMPVPlayer.exe")),
                LivelyMediaPlayer.mpv => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "mpv.exe")),
                LivelyMediaPlayer.vlc => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "vlc", "vlc.exe")),
                _ => false,
            };
        }

        private bool IsGifPlayerAvailable(LivelyGifPlayer gp)
        {
            return gp switch
            {
                LivelyGifPlayer.win10Img => false, //xaml island
                LivelyGifPlayer.libmpvExt => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "libMPVPlayer", "libMPVPlayer.exe")),
                LivelyGifPlayer.mpv => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "mpv", "mpv.exe")),
                _ => false,
            };
        }

        private bool IsWebPlayerAvailable(LivelyWebBrowser wp)
        {
            return wp switch
            {
                LivelyWebBrowser.cef => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "Cef", "Lively.PlayerCefSharp.exe")),
                LivelyWebBrowser.webview2 => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "Wv2", "Lively.PlayerWebView2.exe")),
                _ => false,
            };
        }

        #endregion //helpers
    }
}
