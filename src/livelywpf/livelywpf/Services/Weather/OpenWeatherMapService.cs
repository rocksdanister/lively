using livelywpf.Helpers;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using livelywpf.Services.Weather;
using Timer = System.Timers.Timer;

namespace livelywpf.Services.Weather
{
    public class OpenWeatherMapService : IWeatherService
    {
        //in milliseconds
        private readonly int fetchDelayError = 2 * 60 * 1000; //2min
        private readonly int fetchDelayRepeat = 1 * 60 * 60 * 1000; //1hr

        private readonly Timer retryTimer = new Timer();
        private const string key = "abc"; //TODO: compile time flag.
        private string units = "metric"; // TODO: user input
        private string city = "New York"; // TODO: user input
        private bool checkSuccess = false;

        //public
        public DateTime LastCheckTime { get; private set; } = DateTime.MinValue;
        public WeatherData Weather { get; private set; } = new WeatherData();
        public event EventHandler<WeatherData> WeatherFetched;


        public OpenWeatherMapService()
        {
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
                var forecast = await QueryForecast();
                Weather.Temp = current.Main.Temp;
                Weather.Humidity = current.Main.Humidity;
                Weather.FeelsLike = current.Main.FeelsLike;
                checkSuccess = true;
            }
            catch (Exception)
            {
                checkSuccess = false;
            }
            LastCheckTime = DateTime.Now;
            WeatherFetched?.Invoke(this, Weather);
        }

        private async Task<OpenWeatherMapAPIObjects.Current.Root> QueryCurrent() =>
            await Query<OpenWeatherMapAPIObjects.Current.Root>($"https://api.openweathermap.org/data/2.5/weather?q={city}&units={units}&APPID={key}");

        private async Task<OpenWeatherMapAPIObjects.Forecast.Root> QueryForecast() =>
            await Query<OpenWeatherMapAPIObjects.Forecast.Root>($"https://api.openweathermap.org/data/2.5/forecast?q={city}&units={units}&APPID={key}");

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
