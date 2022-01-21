using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lively.Services
{
    public interface IWeatherService
    {
        DateTime LastCheckTime { get; }
        WeatherData Weather { get; }

        void Start();
        void Stop();
        Task QueryWeather();

        event EventHandler<WeatherData> WeatherFetched;
    }

    public class Data
    {

        public Data(double temp, double tempMin, double tempMax, double feelsLike, int humidity, string description)
        {
            Temp = temp;
            TempMin = tempMin;
            TempMax = tempMax;
            FeelsLike = feelsLike;
            Humidity = humidity;
            Description = description;
        }

        public double Temp { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
        public string Description { get; set; }
    }

    public class WeatherData
    {
        public string Units { get; set; }
        public Data Current { get; set; }
        //public List<Data> Forecast { get; set; }

    }
}