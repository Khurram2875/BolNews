using System.Text.Json;
using BolNews.Web.Interfaces;
using BolNews.Web.Models;

namespace BolNews.Web.Services
{
    public class GoogleTrendsService : IGoogleTrendsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GoogleTrendsService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<List<string>> GetTrendingTopicsAsync(string geo = "PK")
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,

                RequestUri = new Uri("https://google-trends8.p.rapidapi.com/trendings?region_code=US&date=2026-04-20&hl=en-US"),
                Headers =
                    {
                        { "x-rapidapi-key", "564d6df482mshf2fa90efacbe1f2p1c27e5jsn54e614af9309" },
                        { "x-rapidapi-host", "google-trends8.p.rapidapi.com" },
                    },
            };

            var response = await _httpClient.SendAsync(request);
            //response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<GoogleTrendsResponse>(json);

            return data?.Trends?.Select(t => t.Title).ToList() ?? new List<string>();
        }
    }
}
