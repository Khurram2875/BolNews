using BolNews.Application.DTOs;
using BolNews.Web.Models;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class DashboardVM
    {
        public int TotalArticles { get; set; }
        public int ArticlesToday { get; set; }
        public List<PublicArticleVM> TopArticles { get; set; }
        public List<PublicArticleVM> RecentArticles { get; set; }

        public List<TrendingTopicResult> TrendingTopics { get; set; }

        public List<PublicArticleVM> LowPerformingArticles { get; set; }
        public List<string> Dates { get; set; }

        public List<int> ArticlesPerDay { get; set; }
        public List<string> TopArticleTitles { get; set; }
        public List<int> TopArticleViews { get; set; }
        public List<CategoryPerformanceDto> CategoryPerformance { get; set; }
        public List<string> CategoryNames { get; set; }
        public List<int> CategoryViews { get; set; }
        public List<double> CategoryAvgViews { get; set; }
        public List<EditorPerformanceDto> EditorPerformance { get; set; }

        public List<string> EditorNames { get; set; }
        public List<int> EditorViews { get; set; }
        public List<double> EditorAvgViews { get; set; }

    }
}
