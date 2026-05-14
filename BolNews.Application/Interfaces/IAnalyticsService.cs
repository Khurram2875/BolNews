using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IAnalyticsService
    {
        Task TrackImpressionAsync(int articleId);
        Task TrackClickAsync(int articleId);
        Task<double> GetCTRAsync(int articleId);
        Task<List<Article>> GetLowCTRArticlesAsync();
        Task<DashboardDto> GetDashboardAsync();
        Task<int> GetTotalImpressionsAsync(int articleId);
        Task<int> GetTotalClicksAsync(int articleId);
    }
}
