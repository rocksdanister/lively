namespace livelywpf.Models
{
    public interface IWeatherModel
    {
        /// <summary>
        /// city, country
        /// </summary>
        string Location { get; set; }
        string City { get; set; }
        string Country { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }
        WeatherUnit Units { get; set; }
    }

    public enum WeatherUnit
    {
        metric,
        imperial
    }
}