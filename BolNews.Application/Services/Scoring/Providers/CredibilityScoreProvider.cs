using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services.Scoring.Providers
{
    public class CredibilityScoreProvider : IScoreProvider
    {
        public string Name => "Credibility";

        public Task<decimal> CalculateScoreAsync(Article article)
        {
            decimal score = 0;

            if (article.IsPublished)
                score += 30m;

            if (article.IsFactChecked)
                score += 50m;

            if (article.AuthorId > 0)
                score += 20m;

            return Task.FromResult(Math.Min(score, 100m));
        }
    }
}
