using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services.Scoring
{
    public class SeoScoreProvider : IScoreProvider
    {
        public string Name => "SEO";

        public Task<decimal> CalculateScoreAsync(Article article)
        {
            decimal score = 0;

            if (!string.IsNullOrWhiteSpace(article.Title))
                score += 15;

            if (!string.IsNullOrWhiteSpace(article.MetaTitle))
                score += 15;

            if (!string.IsNullOrWhiteSpace(article.MetaDescription))
                score += 15;

            if (!string.IsNullOrWhiteSpace(article.Slug))
                score += 10;

            // canonical is implicitly supported via SEO routing
            score += 10;

            if (!string.IsNullOrWhiteSpace(article.FeaturedImageXl))
                score += 10;

            if (!string.IsNullOrWhiteSpace(article.Content) &&
                article.Content.Length >= 1200)
                score += 15;

            if (article.CategoryId > 0)
                score += 5;

            if (article.AuthorId > 0)
                score += 5;

            return Task.FromResult(Math.Min(score, 100));
        }
    }
}
