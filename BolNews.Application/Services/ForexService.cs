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
    public class ForexService : IForexService
    {
        private readonly HttpClient _http;
        private readonly ICacheService _cache;
        private readonly string _apiKey;

        public ForexService(HttpClient http, ICacheService cache, IOptions<ForexApiOptions> options)
        {
            _http = http;
            _cache = cache;
            _apiKey = options.Value.Key;
        }

        public async Task<ForexDto> GetRatesAsync()
        {
            return await _cache.GetOrCreateAsync("forex_rates", async () =>
            {
                var url = $"https://v6.exchangerate-api.com/v6/{_apiKey}/latest/PKR";
                var res = await _http.GetFromJsonAsync<ForexApiResponse>(
                   url);

                return new ForexDto
                {
                    USD = Math.Round(1 / res.Rates["USD"], 2),
                    EUR = Math.Round(1 / res.Rates["EUR"], 2),
                    GBP = Math.Round(1 / res.Rates["GBP"], 2),
                    AED = Math.Round(1 / res.Rates["AED"], 2),
                    SAR = Math.Round(1 / res.Rates["SAR"], 2),
                    KWD = Math.Round(1 / res.Rates["KWD"], 2)
                };

            }, 30);
        }
    }
}
