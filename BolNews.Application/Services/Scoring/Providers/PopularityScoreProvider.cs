using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services.Scoring.Providers
{
    public class PopularityScoreProvider : IScoreProvider
    {
        private readonly IAnalyticsService _analyticsService;

        public PopularityScoreProvider(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public string Name => "Popularity";

        public async Task<decimal> CalculateScoreAsync(Article article)
        {
            if (article.Id <= 0)
                return 0m;

            var impressions =
                await _analyticsService.GetTotalImpressionsAsync(article.Id);

            return impressions switch
            {
                >= 10000 => 100m,
                >= 5000 => 85m,
                >= 2000 => 70m,
                >= 1000 => 55m,
                >= 500 => 40m,
                >= 100 => 20m,
                _ => 5m
            };
        }
    }
}
