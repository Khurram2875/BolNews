using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents
{
    public class UtilityBarViewComponent : ViewComponent
    {
        private readonly IWeatherService _weather;
        //private readonly IForexService _forex;
        //private readonly IGoldRateService _gold;

        public UtilityBarViewComponent(
            IWeatherService weather
            )
        {
            _weather = weather;
            //_forex = forex;
            //_gold = gold;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var weather = await _weather.GetWeatherAsync("Karachi");
            //var forex = await _forex.GetRatesAsync();
            //var gold = await _gold.GetGoldRateAsync();

            var vm = new UtilityBarVM
            {
                Weather = weather
                //Forex = forex,
                //Gold = gold
            };

            return View(vm);
        }
    }
}
