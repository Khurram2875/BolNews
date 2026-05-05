using System.Net.Http.Json;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using Microsoft.Extensions.Options;

namespace BolNews.Application.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _http;
        private readonly ICacheService _cache;
        private readonly string _apiKey;

        public WeatherService(HttpClient http, ICacheService cache, IOptions<WeatherApiOptions> options)
        {
            _http = http;
            _cache = cache;
            _apiKey = options.Value.Key;
        }

        public async Task<WeatherDto> GetWeatherAsync(string city)
        {
            return await _cache.GetOrCreateAsync(
                $"weather_{city}",
                async () =>
                {
                    var url = $"https://api.weatherapi.com/v1/current.json?key={_apiKey}&q={Uri.EscapeDataString(city)}&aqi=no";

                    var response = await _http.GetFromJsonAsync<WeatherApiResponse>(url);

                    if (response == null)
                        return new WeatherDto { City = city };

                    return new WeatherDto
                    {
                        City = response.Location?.Name ?? city,
                        Temperature = (int)response.Current.Temp_C,
                        FeelsLike = (int)response.Current.Feelslike_C,
                        Humidity = (int)response.Current.Humidity,
                        Condition = response.Current.Condition?.Text ?? string.Empty,
                        WindMph = (double)response.Current.Wind_MPH,
                        Icon = response.Current.Condition?.Icon ?? string.Empty
                    };
                },
                20
            );
        }
    }
}