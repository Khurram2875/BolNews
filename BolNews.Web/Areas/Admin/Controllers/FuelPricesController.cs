using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =
     Roles.Admin + "," +
     Roles.Editor + "," + Roles.SubEditor)]
    public class FuelPricesController : Controller
        {
            private readonly IFuelPriceService _fuelPriceService;

            public FuelPricesController(
                IFuelPriceService fuelPriceService)
            {
                _fuelPriceService = fuelPriceService;
            }

            [HttpGet]
            public async Task<IActionResult> Index(
                CancellationToken cancellationToken)
            {
                var settings =
                    await _fuelPriceService.GetFuelPricesAsync(
                        cancellationToken);

                var model = new FuelPriceSettingsVM
                {
                    EffectiveDate = settings.EffectiveDate,

                    PetrolPrice = settings.Petrol.Price,
                    PetrolChange = settings.Petrol.Change,

                    DieselPrice = settings.Diesel.Price,
                    DieselChange = settings.Diesel.Change
                };

                return View(model);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Index(
                FuelPriceSettingsVM model,
                CancellationToken cancellationToken)
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var settings = new FuelPriceSettingsDto
                {
                    EffectiveDate = model.EffectiveDate,

                    Petrol = new FuelPriceItemDto
                    {
                        Price = model.PetrolPrice,
                        Change = model.PetrolChange
                    },

                    Diesel = new FuelPriceItemDto
                    {
                        Price = model.DieselPrice,
                        Change = model.DieselChange
                    }
                };

                await _fuelPriceService.SaveFuelPricesAsync(
                    settings,
                    cancellationToken);

                TempData["SuccessMessage"] =
                    "Fuel prices updated successfully.";

                return RedirectToAction(nameof(Index));
            }
        }
    
}
