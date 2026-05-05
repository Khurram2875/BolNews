using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Models
{
    public class IpInfo
    {
        public string Status { get; set; }
        public string Query { get; set; } // This will be your Public IP
        public string City { get; set; }
        public string Country { get; set; }
        public string RegionName { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}
