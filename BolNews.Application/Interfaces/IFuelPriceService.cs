using BolNews.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IFuelPriceService
    {
        Task<FuelPriceSettingsDto> GetFuelPricesAsync(
            CancellationToken cancellationToken = default);

        Task SaveFuelPricesAsync(
            FuelPriceSettingsDto settings,
            CancellationToken cancellationToken = default);
    }
}
