using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Models
{
    public class GoldApiResponse
    {
        public List<decimal> Tola { get; set; }
        public List<decimal> Gram10 { get; set; }
        public List<decimal> Gram1 { get; set; }
    }
}
