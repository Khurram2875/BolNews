using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class DashboardDto
    {
        public List<ArticlePerformanceDto> TopArticles { get; set; }
        public List<ArticlePerformanceDto> WorstArticles { get; set; }
    }
    public class ArticlePerformanceDto
    {
        public int ArticleId { get; set; }
        public string Title { get; set; }

        public int Clicks { get; set; }
        public int Impressions { get; set; }

        public double CTR { get; set; }
    }
}
