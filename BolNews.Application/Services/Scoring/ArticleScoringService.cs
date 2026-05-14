using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services.Scoring
{
    public class ArticleScoringService : IArticleScoringService
    {
        private readonly IEnumerable<IScoreProvider> _scoreProviders;
        private readonly IArticleRepository _articleRepository;
        public ArticleScoringService(IEnumerable<IScoreProvider> scoreProviders, IArticleRepository articleRepository)
        {
            _scoreProviders = scoreProviders;
            _articleRepository = articleRepository;
        }

        public async Task CalculateScoresAsync(Article article)
        {
            decimal seoScore = 0m;
            decimal freshnessScore = 0m;
            decimal engagementScore = 0m;
            decimal popularityScore = 0m;
            decimal editorialScore = 0m;
            decimal credibilityScore = 0m;

            foreach (var provider in _scoreProviders)
            {
                var score = await provider.CalculateScoreAsync(article);

                switch (provider.Name)
                {
                    case "SEO":
                        seoScore = score;
                        break;

                    case "Freshness":
                        freshnessScore = score;
                        break;

                    case "Engagement":
                        engagementScore = score;
                        break;

                    case "Popularity":
                        popularityScore = score;
                        break;

                    case "Editorial":
                        editorialScore = score;
                        break;

                    case "Credibility":
                        credibilityScore = score;
                        break;
                }
            }

            article.SeoScore = seoScore;
            article.FreshnessScore = freshnessScore;
            article.EngagementScore = engagementScore;
            article.PopularityScore = popularityScore;
            article.EditorialScore = editorialScore;
            article.CredibilityScore = credibilityScore;

            article.OverallScore =
                (seoScore * 0.20m) +
                (freshnessScore * 0.15m) +
                (engagementScore * 0.20m) +
                (popularityScore * 0.20m) +
                (editorialScore * 0.15m) +
                (credibilityScore * 0.10m);

            article.LastScoreCalculatedAt = DateTime.UtcNow;
        }

        public async Task RecalculateAllScoresAsync()
        {
            var articles = await _articleRepository.GetAllAsync();

            foreach (var article in articles)
            {
                await CalculateScoresAsync(article);
            }

            await _articleRepository.BulkUpdateAsync(articles);
        }
    }
}
