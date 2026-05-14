using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs.Scoring
{
    public class ArticleScoreResultDto
    {
        public decimal SeoScore { get; set; }

        public decimal EditorialScore { get; set; }

        public decimal EngagementScore { get; set; }

        public decimal FreshnessScore { get; set; }

        public decimal PopularityScore { get; set; }

        public decimal CredibilityScore { get; set; }

        public decimal OverallScore { get; set; }
    }
}
