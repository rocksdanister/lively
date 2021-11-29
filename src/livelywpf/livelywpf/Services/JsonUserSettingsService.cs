using livelywpf.Helpers.Storage;
using livelywpf.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Services
{
    public class JsonUserSettingsService : IUserSettingsService
    {
        private readonly string settingsPath = Constants.CommonPaths.UserSettingsPath;
        private readonly string appRulesPath = Constants.CommonPaths.AppRulesPath;
        private readonly string wallpaperLayoutPath = Constants.CommonPaths.WallpaperLayoutPath;
        private readonly string weatherPath = Constants.CommonPaths.WeatherSettingsPath;

        public JsonUserSettingsService()
        {
            Load<ISettingsModel>();
            Load<IWeatherModel>();
            Load<List<IApplicationRulesModel>>();
            Load<List<IWallpaperLayoutModel>>();
        }

        public ISettingsModel Settings { get; private set; }
        public IWeatherModel WeatherSettings { get; private set; }
        public List<IApplicationRulesModel> AppRules { get; private set; }
        public List<IWallpaperLayoutModel> WallpaperLayout { get; private set; }

        public void Save<T>()
        {
            // ugh...
            if (typeof(T) == typeof(ISettingsModel))
            {
                JsonStorage<ISettingsModel>.StoreData(settingsPath, Settings);
            }
            else if (typeof(T) == typeof(IWeatherModel))
            {
                JsonStorage<IWeatherModel>.StoreData(weatherPath, WeatherSettings);
            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
            {
                JsonStorage<List<IApplicationRulesModel>>.StoreData(appRulesPath, AppRules);
            }
            else if (typeof(T) == typeof(List<IWallpaperLayoutModel>))
            {
                JsonStorage<List<IWallpaperLayoutModel>>.StoreData(wallpaperLayoutPath, WallpaperLayout);
            }
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }

        public void Load<T>()
        {
            // ugh...
            if (typeof(T) == typeof(ISettingsModel))
            {
                try
                {
                    Settings = JsonStorage<SettingsModel>.LoadData(settingsPath);
                }
                catch (Exception e)
                {
                    //Logger.Error(e.ToString());
                    Settings = new SettingsModel();
                    Save<ISettingsModel>();
                }

            }
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
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
            {
                try
                {
                    AppRules = new List<IApplicationRulesModel>(JsonStorage<List<ApplicationRulesModel>>.LoadData(appRulesPath));
                }
                catch (Exception e)
                {
                    //Logger.Error(e.ToString());
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
                    //Logger.Error(e.ToString());
                    WallpaperLayout = new List<IWallpaperLayoutModel>();
                    Save<List<IWallpaperLayoutModel>>();
                }
            }
            else
            {
                throw new InvalidCastException($"Type not found: {typeof(T)}");
            }
        }
    }
}
