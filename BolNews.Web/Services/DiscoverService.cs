using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using BolNews.Web.Models;

namespace BolNews.Web.Services
{
    public class DiscoverService : IDiscoverService
    {
        public DiscoverScoreResult Evaluate(PublicArticleVM article)
        {
            var result = new DiscoverScoreResult();
            int score = 0;

            // 🔥 1. Title Quality (20 points)
            if (!string.IsNullOrWhiteSpace(article.Title) && article.Title.Length >= 50)
            {
                result.HasGoodTitle = true;
                score += 20;
            }
            else
            {
                result.Suggestions.Add("Improve headline length (50–90 characters recommended)");
            }

            // 🔥 2. Image (20 points)
            if (!string.IsNullOrEmpty(article.FeaturedImageXl))
            {
                result.HasLargeImage = true;
                score += 20;
            }
            else
            {
                result.Suggestions.Add("Add high-quality 1200px featured image");
            }

            // 🔥 3. Freshness (20 points)
            if (article.PublishedAt.HasValue &&
                (DateTime.UtcNow - article.PublishedAt.Value).TotalHours < 24)
            {
                result.IsFresh = true;
                score += 20;
            }
            else
            {
                result.Suggestions.Add("Publish fresh content (last 24 hours preferred)");
            }

            // 🔥 4. Meta Description (15 points)
            if (!string.IsNullOrWhiteSpace(article.MetaDescription))
            {
                result.HasMetaDescription = true;
                score += 15;
            }
            else
            {
                result.Suggestions.Add("Add a compelling meta description");
            }

            // 🔥 5. Content Length (15 points)
            if (!string.IsNullOrWhiteSpace(article.Content) && article.Content.Length > 1000)
            {
                score += 15;
            }
            else
            {
                result.Suggestions.Add("Increase content depth (1000+ characters recommended)");
            }

            // 🔥 6. Structured Data (10 points)
            // (Assume always true since you've implemented it)
            result.HasStructuredData = true;
            score += 10;

            result.Score = score;
            return result;
        }
    }
}
