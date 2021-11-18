using System;
using System.Threading.Tasks;

namespace livelywpf.Services.Weather
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

    public class WeatherData
    {
        public double Temp { get; set; }
        public double FeelsLike { get; set; }
        public int Humidity { get; set; }
    }
}