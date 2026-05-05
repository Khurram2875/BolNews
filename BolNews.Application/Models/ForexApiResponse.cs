using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BolNews.Application.Models
{
    public class ForexApiResponse
    {
        [JsonPropertyName("conversion_rates")]
        public Dictionary<string, decimal> Rates { get; set; }

        // Optional: Add this if you want to capture the status
        [JsonPropertyName("result")]
        public string Result { get; set; }
    }
}
