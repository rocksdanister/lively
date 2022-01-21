using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using System.Collections.Generic;
using Lively.Models;
using Lively.Common;

namespace Lively.Services
{
    //TODO: cache the results to disk reduce api calls
    //TODO: user input for "city" and "units", possibly during app setup and during weather wallpaper apply if not setup yet.
    public class OpenWeatherMapService : IWeatherService
    {
        //in milliseconds
        private readonly int fetchDelayError = 2 * 60 * 1000; //2min
        private readonly int fetchDelayRepeat = 1 * 60 * 60 * 1000; //1hr

        private readonly Timer retryTimer = new Timer();
        private readonly string key = Constants.Weather.OpenWeatherMapAPIKey;
        private string units = string.Empty; 
        private string city = string.Empty; 
        private bool checkSuccess = false;

        //public
        public DateTime LastCheckTime { get; private set; } = DateTime.MinValue;
        public WeatherData Weather { get; private set; } = new WeatherData();
        public event EventHandler<WeatherData> WeatherFetched;

        public OpenWeatherMapService(IUserSettingsService settings)
        {
            //units = settings.WeatherSettings.Units == Models.WeatherUnit.metric ? "metric" : "imperial";
            //city = settings.WeatherSettings.Location;

            retryTimer.Elapsed += RetryTimer_Elapsed;
            //giving the retry delay is not reliable since it will reset if system sleeps/suspends.
            retryTimer.Interval = 1 * 60 * 1000;
            _ = QueryWeather();
        }

        /// <summary>
        /// Check for updates periodically.
        /// </summary>
        public void Start()
        {
            retryTimer.Start();
        }

        /// <summary>
        /// Stops periodic updates check.
        /// </summary>
        public void Stop()
        {
            if (retryTimer.Enabled)
            {
                retryTimer.Stop();
            }
        }

        private void RetryTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((DateTime.Now - LastCheckTime).TotalMilliseconds > (checkSuccess ? fetchDelayRepeat : fetchDelayError))
            {
                _ = QueryWeather();
            }
        }

        public async Task QueryWeather()
        {
            try
            {
                var current = await QueryCurrent();
                //var forecast = await QueryForecast();

                Weather.Units = units;
                Weather.Current = new Data(current.Main.Temp,
                                           current.Main.TempMin,
                                           current.Main.TempMax,
                                           current.Main.FeelsLike,
                                           current.Main.Humidity,
                                           current.Weather[0].Description);

                checkSuccess = true;
            }
            catch (Exception)
            {
                checkSuccess = false;
            }
            LastCheckTime = DateTime.Now;
            WeatherFetched?.Invoke(this, Weather);
        }

        private async Task<OpenWeatherMapObjects.Current.Root> QueryCurrent() =>
            await Query<OpenWeatherMapObjects.Current.Root>($"https://api.openweathermap.org/data/2.5/weather?q={city}&units={units}&APPID={key}");

        private async Task<OpenWeatherMapObjects.Forecast.Root> QueryForecast() =>
            await Query<OpenWeatherMapObjects.Forecast.Root>($"https://api.openweathermap.org/data/2.5/forecast?q={city}&units={units}&APPID={key}");

        private async Task<T> Query<T>(string request)
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = await client.GetAsync(request);
            using HttpContent content = response.Content;
            var json = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
