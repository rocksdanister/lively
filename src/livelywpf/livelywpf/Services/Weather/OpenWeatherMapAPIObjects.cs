using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf.Services.Weather
{
    //Auto Generated from json data.. TODO: cleanup.
    public class OpenWeatherMapAPIObjects
    {
        //ref: https://openweathermap.org/current
        public class Current
        {
            public class Root
            {
                public Coord Coord { get; set; }
                public List<Weather> Weather { get; set; }
                public string Base { get; set; }
                public Main Main { get; set; }
                public int Visibility { get; set; }
                public Wind Wind { get; set; }
                public Clouds Clouds { get; set; }
                public int Dt { get; set; }
                public Sys Sys { get; set; }
                public int Timezone { get; set; }
                public int Id { get; set; }
                public string Name { get; set; }
                public int Cod { get; set; }
            }

            public class Coord
            {
                public double Lon { get; set; }
                public double Lat { get; set; }
            }

            public class Weather
            {
                public int Id { get; set; }
                public string Main { get; set; }
                public string Description { get; set; }
                public string Icon { get; set; }
            }

            public class Main
            {
                public double Temp { get; set; }
                public double FeelsLike { get; set; }
                public double TempMin { get; set; }
                public int TempMax { get; set; }
                public int Pressure { get; set; }
                public int Humidity { get; set; }
            }

            public class Wind
            {
                public double Speed { get; set; }
                public int Deg { get; set; }
                public double Gust { get; set; }
            }

            public class Clouds
            {
                public int All { get; set; }
            }

            public class Sys
            {
                public int Type { get; set; }
                public int Id { get; set; }
                public string Country { get; set; }
                public int Sunrise { get; set; }
                public int Sunset { get; set; }
            }
        }

        //ref: https://openweathermap.org/forecast5
        public class Forecast
        {
            public class Root
            {
                public string Cod { get; set; }
                public int Message { get; set; }
                public int Cnt { get; set; }
                public List<List> List { get; set; }
                public City City { get; set; }
            }

            public class Main
            {
                public double Temp { get; set; }
                public double FeelsLike { get; set; }
                public double TempMin { get; set; }
                public double TempMax { get; set; }
                public int Pressure { get; set; }
                public int SeaLevel { get; set; }
                public int GrndLevel { get; set; }
                public int Humidity { get; set; }
                public double TempKf { get; set; }
            }

            public class Weather
            {
                public int Id { get; set; }
                public string Main { get; set; }
                public string Description { get; set; }
                public string Icon { get; set; }
            }

            public class Clouds
            {
                public int All { get; set; }
            }

            public class Wind
            {
                public double Speed { get; set; }
                public int Deg { get; set; }
                public double Gust { get; set; }
            }

            public class Sys
            {
                public string Pod { get; set; }
            }

            public class Rain
            {
                public double _3h { get; set; }
            }

            public class List
            {
                public int Dt { get; set; }
                public Main Main { get; set; }
                public List<Weather> Weather { get; set; }
                public Clouds Clouds { get; set; }
                public Wind Wind { get; set; }
                public int Visibility { get; set; }
                public double Pop { get; set; }
                public Sys Sys { get; set; }
                public string DtTxt { get; set; }
                public Rain Rain { get; set; }
            }

            public class Coord
            {
                public double Lat { get; set; }
                public double Lon { get; set; }
            }

            public class City
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public Coord Coord { get; set; }
                public string Country { get; set; }
                public int Population { get; set; }
                public int Timezone { get; set; }
                public int Sunrise { get; set; }
                public int Sunset { get; set; }
            }
        }
    }
}
