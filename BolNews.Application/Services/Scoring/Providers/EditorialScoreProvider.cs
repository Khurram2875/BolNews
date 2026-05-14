using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services.Scoring.Providers
{
    public class EditorialScoreProvider : IScoreProvider
    {
        public string Name => "Editorial";

        public Task<decimal> CalculateScoreAsync(Article article)
        {
            decimal score = 0;

            if (article.IsEditorsPick)
                score += 40m;

            score += article.EditorialPriority switch
            {
                3 => 60m,
                2 => 40m,
                1 => 20m,
                _ => 0m
            };

            return Task.FromResult(Math.Min(score, 100m));
        }
    }
}
