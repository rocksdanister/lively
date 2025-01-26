using Lively.Common;
using Lively.Common.Helpers.Storage;
using Lively.Common.Services;
using Lively.Core.Display;
using Lively.Helpers;
using Lively.Models;
using Lively.Models.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Lively.Services
{
    public class UserSettingsService : IUserSettingsService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string settingsPath = Constants.CommonPaths.UserSettingsPath;
        private readonly string appRulesPath = Constants.CommonPaths.AppRulesPath;
        private readonly string wallpaperLayoutPath = Constants.CommonPaths.WallpaperLayoutPath;
        //private readonly string weatherPath = Constants.CommonPaths.WeatherSettingsPath;

        public UserSettingsService(IDisplayManager displayManager, ITransparentTbService ttbService)
        {
            Load<SettingsModel>();
            //Load<IWeatherModel>();
            Load<List<ApplicationRulesModel>>();
            Load<List<WallpaperLayoutModel>>();

            Settings.SelectedDisplay = Settings.SelectedDisplay != null ?
                displayManager.DisplayMonitors.FirstOrDefault(x => x.Equals(Settings.SelectedDisplay)) ?? displayManager.PrimaryDisplayMonitor :
                displayManager.PrimaryDisplayMonitor;

            // Previous installed version is different from current instance.  
            if (!Settings.AppVersion.Equals(Assembly.GetExecutingAssembly().GetName().Version.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                Settings.AppPreviousVersion = Settings.AppVersion;
                Settings.AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Settings.IsUpdated = true;
                // This flag is set to false once UI program closes after notifying user.
                Settings.IsUpdatedNotify = true;
                // Save the new AppVersion.
                Save<SettingsModel>();
            }
            else if (Settings.IsUpdated)
            {
                // IsUpdated is set only once after each update.
                Settings.IsUpdated = false;
                Save<SettingsModel>();
            }

            // Reject unsupported language.
            Settings.Language = Languages.SupportedLanguages.FirstOrDefault(x => x.Code == Settings.Language)?.Code ?? string.Empty;

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

        public SettingsModel Settings { get; private set; }
        //public IWeatherModel WeatherSettings { get; private set; }
        public List<ApplicationRulesModel> AppRules { get; private set; }
        public List<WallpaperLayoutModel> WallpaperLayout { get; private set; }

        public void Save<T>()
        {
            if (typeof(T) == typeof(SettingsModel))
            {
                JsonStorage<SettingsModel>.StoreData(settingsPath, Settings);
            }
            else if (typeof(T) == typeof(List<ApplicationRulesModel>))
            {
                JsonStorage<List<ApplicationRulesModel>>.StoreData(appRulesPath, AppRules);
            }
            else if (typeof(T) == typeof(List<WallpaperLayoutModel>))
            {
                JsonStorage<List<WallpaperLayoutModel>>.StoreData(wallpaperLayoutPath, WallpaperLayout);
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
            if (typeof(T) == typeof(SettingsModel))
            {
                try
                {
                    Settings = JsonStorage<SettingsModel>.LoadData(settingsPath);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Settings = new SettingsModel();
                    Save<SettingsModel>();
                }

            }
            else if (typeof(T) == typeof(List<ApplicationRulesModel>))
            {
                try
                {
                    AppRules = new List<ApplicationRulesModel>(JsonStorage<List<ApplicationRulesModel>>.LoadData(appRulesPath));
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    AppRules = new List<ApplicationRulesModel>
                    {
                        //defaults.
                        new ApplicationRulesModel("Discord", Models.Enums.AppRules.ignore)
                    };
                    Save<List<ApplicationRulesModel>>();
                }
            }
            else if (typeof(T) == typeof(List<WallpaperLayoutModel>))
            {
                try
                {
                    WallpaperLayout = new List<WallpaperLayoutModel>(JsonStorage<List<WallpaperLayoutModel>>.LoadData(wallpaperLayoutPath));
                }
                catch (Exception e)
                {
                    Logger.Error(e.ToString());
                    WallpaperLayout = new List<WallpaperLayoutModel>();
                    Save<List<WallpaperLayoutModel>>();
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
    }
}
