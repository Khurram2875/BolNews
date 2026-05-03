namespace BolNews.Application.Models
{
    /// <summary>Typed deserialization model for the WeatherAPI.com JSON response.</summary>
    internal sealed class WeatherApiResponse
    {
        public WeatherLocation Location { get; set; } = new();
        public WeatherCurrent Current { get; set; } = new();
    }

    internal sealed class WeatherLocation
    {
        public string Name { get; set; } = string.Empty;
    }

    internal sealed class WeatherCurrent
    {
        public double Temp_C { get; set; }
        public WeatherCondition Condition { get; set; } = new();
    }

    internal sealed class WeatherCondition
    {
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
