using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class CategoryPerformanceDto
    {
        public string CategoryName { get; set; }
        public int ArticleCount { get; set; }
        public int TotalViews { get; set; }
        public double AvgViewsPerArticle { get; set; }
    }
}
