using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Models
{
    public class AdOptions
    {
        public bool Enabled { get; set; }

        public string HeaderAd { get; set; }
        public string SidebarAd { get; set; }
        public string InArticleAd { get; set; }
    }
}
