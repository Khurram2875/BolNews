using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Domain.Entities
{
    public class ArticleAnalytics
    {
        public int Id { get; set; }

        public int ArticleId { get; set; }
        public Article? Article { get; set; }

        public int Impressions { get; set; }
        public int Clicks { get; set; }

        public double CTR => Impressions == 0 ? 0 : (double)Clicks / Impressions * 100;

        public DateTime Date { get; set; } // daily tracking
    }
}
