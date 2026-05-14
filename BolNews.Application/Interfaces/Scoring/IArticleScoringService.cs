using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces.Scoring
{
    public interface IArticleScoringService
    {
        Task CalculateScoresAsync(Article article);

        Task RecalculateAllScoresAsync();
    }
}
