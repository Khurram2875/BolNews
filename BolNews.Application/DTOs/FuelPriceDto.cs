using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class FuelPriceDto
    {
        [JsonPropertyName("fuel")]
        public string Fuel { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonPropertyName("price_pkr")]
        public decimal PricePkr { get; set; }

        [JsonPropertyName("change_pkr")]
        public decimal ChangePkr { get; set; }

        [JsonPropertyName("effective_date")]
        public DateTime EffectiveDate { get; set; }
    }
}
