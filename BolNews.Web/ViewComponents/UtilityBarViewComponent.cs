using System.Runtime;
using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents
{
    public class UtilityBarViewComponent : ViewComponent
    {
        private readonly IWeatherService _weather;
        private readonly IForexService _forex;
        private readonly IGoldRateService _gold;

        public UtilityBarViewComponent(
            IWeatherService weather,
             IForexService forex, IGoldRateService gold
           )
        {
            _weather = weather;
            _forex = forex;
            _gold = gold;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            string currentCity = await GetUserCityAsync();
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
        public async Task<string> GetUserCityAsync()
        {
            using HttpClient client = new HttpClient();

            try
            {
                // Calling the geolocation API
                var response = await client.GetFromJsonAsync<IpInfo>("http://ip-api.com/json/");

                // Check if the API returned a success status and a valid city
                if (response != null && response.Status == "success" && !string.IsNullOrEmpty(response.City))
                {
                    return response.City;
                }
            }
            catch (Exception ex)
            {
                // Log the error if necessary
                Console.WriteLine($"Location lookup failed: {ex.Message}");
            }

            // Return a fallback city if something goes wrong
            return "Karachi";
        }
    }
}
