using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using Microsoft.Extensions.Options;

namespace BolNews.Application.Services
{
    public class GoldRateService : IGoldRateService
    {
        private readonly HttpClient _http;
        private readonly ICacheService _cache;
        private readonly GoldApiOptions _options;

        public GoldRateService(ICacheService cache, IOptions<GoldApiOptions> options, HttpClient http)
        {
            _cache = cache;
            _http = http;
            _options = options.Value;
        }

        public async Task<GoldDto> GetGoldRateAsync()
        {

            var apiKey = _options.Key;
            _options.Host = "gold-prices-pakistan.p.rapidapi.com"
            return await _cache.GetOrCreateAsync("gold_rate", async () =>
            {


                // 🔥 Replace later with real API
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri("https://gold-prices-pakistan.p.rapidapi.com/live")
                };

                request.Headers.Add("x-rapidapi-key", _options.Key);
                request.Headers.Add("x-rapidapi-host", _options.Host);

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return new GoldDto { Price24K = 0 };

                var data = await response.Content.ReadFromJsonAsync<GoldApiResponse>();

                if (data?.Tola == null || data.Tola.Count < 2)
                    return new GoldDto { Price24K = 0 };

                return new GoldDto
                {
                    Price24K = data.Tola[0],   // 24K
                    Price22K = data.Tola[1],   // 22K
                    PricePerGram = data.Gram1[0]
                };

            }, (60)); // gold doesn’t change fast
        }
    }
}
