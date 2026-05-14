using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services.Scoring
{
    public class FreshnessScoreProvider : IScoreProvider
    {
        public string Name => "Freshness";

        public Task<decimal> CalculateScoreAsync(Article article)
        {
            if (!article.IsPublished || !article.PublishedAt.HasValue)
                return Task.FromResult(0m);

            var age = DateTime.UtcNow - article.PublishedAt.Value;

            decimal score = age.TotalHours switch
            {
                <= 1 => 100m,
                <= 6 => 90m,
                <= 24 => 75m,
                <= 72 => 60m,
                <= 168 => 40m,
                <= 720 => 20m,
                _ => 5m
            };

            return Task.FromResult(score);
        }
    }
}
