using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class FuelPriceSettingsDto
    {
        public DateTime EffectiveDate { get; set; }

        public FuelPriceItemDto Petrol { get; set; } = new();

        public FuelPriceItemDto Diesel { get; set; } = new();
    }

    public class FuelPriceItemDto
    {
        public decimal Price { get; set; }

        public decimal Change { get; set; }
    }
}
