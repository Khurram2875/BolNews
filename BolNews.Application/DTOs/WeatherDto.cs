using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class WeatherDto
    {
        public string City { get; set; }
        public int Temperature { get; set; }
        public int FeelsLike { get; set; }
        public string Condition { get; set; }
        public string Icon { get; set; }
        public double WindMph { get; set; }
        public int Humidity { get; set; }

    }
}
