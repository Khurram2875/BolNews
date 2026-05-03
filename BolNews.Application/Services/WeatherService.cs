using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;


namespace BolNews.Application.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _http;
        private readonly ICacheService _cache;

        public WeatherService(HttpClient http, ICacheService cache)
        {
            _http = http;
            _cache = cache;
        }

        public async Task<WeatherDto> GetWeatherAsync(string city)
        {
            return await _cache.GetOrCreateAsync(
                $"weather_{city}",
                async () =>
                {
                    var response = await _http.GetFromJsonAsync<dynamic>(
    $"https://api.weatherapi.com/v1/current.json?key=4413c03ccb994158aaf180644262904&q={city}&aqi=no");

                    //if (response == null)
                    //    return new WeatherDto { City = city };

                    return new WeatherDto
                    {
                        City = response.GetProperty("location").GetProperty("name").GetString(),

                        // Note: temp_c is 36.1 in your JSON, so we convert double to int
                        Temperature = (int)response.GetProperty("current").GetProperty("temp_c").GetDouble(),

                        Condition = response.GetProperty("current")
                                .GetProperty("condition")
                                .GetProperty("text").GetString(),

                        Icon = response.GetProperty("current")
                           .GetProperty("condition")
                           .GetProperty("icon").GetString()
                    };
                },
               20
            );
        }
    }

    
}
