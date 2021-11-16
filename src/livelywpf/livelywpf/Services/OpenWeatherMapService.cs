using livelywpf.Helpers;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace livelywpf.Services
{
    public class OpenWeatherMapService
    {
        //in milliseconds
        private readonly int fetchDelayError = 10 * 60 * 1000; //10min
        private readonly int fetchDelayRepeat = 1 * 60 * 60 * 1000; //1hr

        //public
        public DateTime LastCheckTime { get; private set; } = DateTime.MinValue;
        private readonly Timer retryTimer = new Timer();
        private const string apiKey = "xyz";

        public OpenWeatherMapService()
        {
            retryTimer.Elapsed += RetryTimer_Elapsed;
            //giving the retry delay is not reliable since it will reset if system sleeps/suspends.
            retryTimer.Interval = 5 * 60 * 1000;
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
            throw new NotImplementedException();
        }

        private async Task<bool> QueryWeather(int fetchDelay = 45 * 1000)
        {
            throw new NotImplementedException();
        }
    }
}
