using System.Text.Json;
using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using Microsoft.Extensions.Configuration;

namespace BolNews.Application.Services
{
    public class GoogleTrendsService : IGoogleTrendsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GoogleTrendsService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["RapidApi:Key"]
                ?? throw new InvalidOperationException("RapidApi:Key is not configured.");
        }

        public async Task<List<string>> GetTrendingTopicsAsync(string geo = "PK")
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(
                    $"https://google-trends8.p.rapidapi.com/trendings?region_code={geo}&date={today}&hl=en-US"),
                Headers =
                {
                    { "x-rapidapi-key",  _apiKey },
                    { "x-rapidapi-host", "google-trends8.p.rapidapi.com" },
                },
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<GoogleTrendsResponse>(json);

            return data?.Trends?.Select(t => t.Title).ToList() ?? new List<string>();
        }
    }
}
