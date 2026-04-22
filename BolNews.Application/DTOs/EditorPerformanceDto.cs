using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class EditorPerformanceDto
    {
        public string AuthorName { get; set; }
        public int ArticleCount { get; set; }
        public int TotalViews { get; set; }
        public double AvgViewsPerArticle { get; set; }
    }
}
