using livelywpf.Helpers.Storage;
using livelywpf.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Services
{
    class UserSettingsService : IUserSettingsService
    {
        private readonly string settingsPath = Constants.CommonPaths.UserSettingsPath;
        private readonly string appRulesPath = Constants.CommonPaths.AppRulesPath;
        private readonly string wallpaperLayoutPath = Constants.CommonPaths.WallpaperLayoutPath;

        public UserSettingsService()
        {
            Load<ISettingsModel>();
            Load<List<IApplicationRulesModel>>();
            Load<List<IWallpaperLayoutModel>>();
        }

        private ISettingsModel _settings;
        public ISettingsModel Settings => _settings;

        private List<IApplicationRulesModel> _appRules;
        public List<IApplicationRulesModel> AppRules => _appRules;

        private List<IWallpaperLayoutModel> _wallpaperLayout;
        public List<IWallpaperLayoutModel> WallpaperLayout => _wallpaperLayout;

        public void Save<T>()
        {
            // ugh...
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
            else
            {
                throw new InvalidCastException("Type not found");
            }
        }

        public void Load<T>()
        {
            // ugh...
            if (typeof(T) == typeof(ISettingsModel))
            {
                try
                {
                    _settings = JsonStorage<SettingsModel>.LoadData(settingsPath);
                }
                catch (Exception e)
                {
                    //Logger.Error(e.ToString());
                    _settings = new SettingsModel();
                    Save<ISettingsModel>();
                }

            }
            else if (typeof(T) == typeof(List<IApplicationRulesModel>))
            {
                try
                {
                    _appRules = new List<IApplicationRulesModel>(JsonStorage<List<ApplicationRulesModel>>.LoadData(appRulesPath));
                }
                catch (Exception e)
                {
                    //Logger.Error(e.ToString());
                    _appRules = new List<IApplicationRulesModel>
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
                    _wallpaperLayout = new List<IWallpaperLayoutModel>(JsonStorage<List<WallpaperLayoutModel>>.LoadData(wallpaperLayoutPath));
                }
                catch (Exception e)
                {
                    //Logger.Error(e.ToString());
                    _wallpaperLayout = new List<IWallpaperLayoutModel>();
                    Save<List<IWallpaperLayoutModel>>();
                }
            }
            else
            {
                throw new InvalidCastException("Type not found");
            }
        }
    }
}
