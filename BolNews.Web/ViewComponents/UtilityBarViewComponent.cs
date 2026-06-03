using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Required for HttpContext header handling

namespace BolNews.Web.ViewComponents
{
    public class UtilityBarViewComponent : ViewComponent
    {
        private readonly IWeatherService _weather;
        private readonly IForexService _forex;
        private readonly IGoldRateService _gold;

        public UtilityBarViewComponent(
            IWeatherService weather,
            IForexService forex,
            IGoldRateService gold)
        {
            _weather = weather;
            _forex = forex;
            _gold = gold;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Access the component's internal HttpContext and pass it down
            string currentCity = await GetUserCityAsync(HttpContext);

            var weather = await _weather.GetWeatherAsync(currentCity);
            var forex = await _forex.GetRatesAsync();
            var gold = await _gold.GetGoldRateAsync();

            var vm = new UtilityBarVM
            {
                Weather = weather,
                Forex = forex,
                Gold = gold
            };

            return View(vm);
        }

        public async Task<string> GetUserCityAsync(HttpContext httpContext)
        {
            // 1. Grab the connection remote IP address
            string userIp = httpContext.Connection.RemoteIpAddress?.ToString();

            // 2. Check for Proxy / CDN Headers (Cloudflare or standard Load Balancers)
            // If the app is behind a reverse proxy, RemoteIpAddress will show the server proxy IP.
            // The true client IP shifts into these headers instead.
            if (httpContext.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp))
            {
                userIp = cfIp.ToString();
            }
            else if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                // X-Forwarded-For can return a comma-separated chain (client, proxy1, proxy2). 
                // The first IP string in that array is always the origin client.
                userIp = forwardedFor.ToString().Split(',')[0].Trim();
            }

            // 3. Localhost Verification Guard
            // If running locally, IP returns as ::1 or 127.0.0.1. 
            // The public geolocation API will fail on private IPs, so bypass and return your default fallback.
            if (string.IsNullOrEmpty(userIp) || userIp == "::1" || userIp.StartsWith("127.0.0."))
            {
                return "Karachi";
            }

            using HttpClient client = new HttpClient();

            try
            {
                // 4. Append the specific user's public IP explicitly to the API lookup URL
                var response = await client.GetFromJsonAsync<IpInfo>($"http://ip-api.com/json/{userIp}");

                if (response != null && response.Status == "success" && !string.IsNullOrEmpty(response.City))
                {
                    return response.City;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Location lookup failed for IP {userIp}: {ex.Message}");
            }

            return "Karachi";
        }
    }
}