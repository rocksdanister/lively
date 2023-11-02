using System;
using System.Collections.Generic;
using System.Text;

namespace Lively.Models
{
    [Serializable]
    public class WeatherModel
    {
        public string Location { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public WeatherUnit Units { get; set; }
    }

    public enum WeatherUnit
    {
        metric,
        imperial
    }
}
