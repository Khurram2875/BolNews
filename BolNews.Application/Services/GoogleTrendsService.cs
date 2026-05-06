using System.Text.Json;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

namespace BolNews.Application.Services
{
    public class GoogleTrendsService : IGoogleTrendsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ICacheService _cache;

        public GoogleTrendsService(HttpClient httpClient, IConfiguration config, ICacheService cache)
        {
            _httpClient = httpClient;
            _apiKey = config["RapidApi:Key"]
                ?? throw new InvalidOperationException("RapidApi:Key is not configured.");
            _cache = cache;
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

        public async Task<List<TrendingTopicDto>> GetTrendingTopicsAsync()
        {
            return await _cache.GetOrCreateAsync("google_trends_pk", async () =>
            {
                var url = "https://trends.google.com/trending/rss?geo=PK";

                var xmlString = await _httpClient.GetStringAsync(url);

                var doc = XDocument.Parse(xmlString);

                XNamespace ht = "https://trends.google.com/trending/rss";

                var items = doc.Descendants("item")
                    .Take(10)
                    .Select(x => new TrendingTopicDto
                    {
                        Title = x.Element("title")?.Value,
                        Traffic = x.Element(ht + "approx_traffic")?.Value,
                        Url = x.Element("link")?.Value,
                        Image = x.Element(ht + "picture")?.Value,
                        Source = x.Element(ht + "picture_source")?.Value
                    })
                    .ToList();

                return items;

            }, (15));
        }
    }
}

