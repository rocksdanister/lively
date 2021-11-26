using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Models
{
    [Serializable]
    public class WeatherModel : IWeatherModel
    {
        public string Location { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public WeatherUnit Units { get; set; }
    }
}
