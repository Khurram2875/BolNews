using BolNews.Application.Interfaces;
using BolNews.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.ViewComponents
{
    public class FuelPriceViewComponent : ViewComponent
    {
        private readonly IFuelPriceService _fuelPriceService;

        public FuelPriceViewComponent(
            IFuelPriceService fuelPriceService)
        {
            _fuelPriceService = fuelPriceService;
        }

        public async Task<IViewComponentResult> InvokeAsync(
    CancellationToken cancellationToken)
        {
            var settings =
                await _fuelPriceService.GetFuelPricesAsync(
                    cancellationToken);

            var model = new FuelPriceWidgetVM
            {
                EffectiveDate = settings.EffectiveDate,

                PetrolPrice = settings.Petrol.Price,
                PetrolChange = settings.Petrol.Change,

                DieselPrice = settings.Diesel.Price,
                DieselChange = settings.Diesel.Change
            };

            return View(model);
        }
    }
}
