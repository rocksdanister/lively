using Lively.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Services
{
    public interface IUserSettingsService
    {
        ISettingsModel Settings { get; }
        //IWeatherModel WeatherSettings { get; }
        List<IApplicationRulesModel> AppRules { get; }
        List<IWallpaperLayoutModel> WallpaperLayout { get; }
        void Save<T>();
        void Load<T>();
    }
}
