using Lively.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Common.Services
{
    public interface IUserSettingsService
    {
        SettingsModel Settings { get; }
        //IWeatherModel WeatherSettings { get; }
        List<ApplicationRulesModel> AppRules { get; }
        List<WallpaperLayoutModel> WallpaperLayout { get; }
        void Save<T>();
        void Load<T>();
    }
}
