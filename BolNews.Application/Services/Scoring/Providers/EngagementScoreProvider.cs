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
    public class EngagementScoreProvider : IScoreProvider
    {
        private readonly IAnalyticsService _analyticsService;

        public EngagementScoreProvider(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public string Name => "Engagement";

        public async Task<decimal> CalculateScoreAsync(Article article)
        {
            if (article.Id <= 0)
                return 0m;

            var ctr = await _analyticsService.GetCTRAsync(article.Id);

            return ctr switch
            {
                >= 15 => 100m,
                >= 10 => 85m,
                >= 7 => 70m,
                >= 5 => 55m,
                >= 3 => 40m,
                >= 1 => 20m,
                _ => 5m
            };
        }
    }
}
