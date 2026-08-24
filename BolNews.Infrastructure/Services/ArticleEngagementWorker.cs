using BolNews.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BolNews.Infrastructure.Services
{
    public sealed class ArticleEngagementWorker : BackgroundService
    {
        private readonly ArticleEngagementQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ArticleEngagementWorker> _logger;

        public ArticleEngagementWorker(
            ArticleEngagementQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<ArticleEngagementWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) //temprary code
        {
            await foreach (var item in
                _queue.ReadAllAsync(stoppingToken))
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var analyticsService =
                        scope.ServiceProvider
                            .GetRequiredService<IAnalyticsService>();

                    await analyticsService.TrackImpressionAsync(
                        item.ArticleId);

                    if (item.IncrementViewCount)
                    {
                        var articleService =
                            scope.ServiceProvider
                                .GetRequiredService<IArticleService>();

                        await articleService.IncrementViewCountAsync(
                            item.ArticleId);
                    }

                    sw.Stop();

                    _logger.LogInformation(
                        "ARTICLE_ENGAGEMENT_DONE ArticleId={ArticleId} ViewCount={IncrementViewCount} ElapsedMs={ElapsedMs}",
                        item.ArticleId,
                        item.IncrementViewCount,
                        sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();

                    _logger.LogError(
                        ex,
                        "Failed to process article engagement for ArticleId {ArticleId} after {ElapsedMs}ms",
                        item.ArticleId,
                        sw.ElapsedMilliseconds);
                }
            }
        }
        //original code commented out temporarily
        //protected override async Task ExecuteAsync( CancellationToken stoppingToken)
        //{
        //    await foreach (var item in
        //        _queue.ReadAllAsync(stoppingToken))
        //    {
        //        try
        //        {
        //            using var scope = _scopeFactory.CreateScope();

        //            var analyticsService =
        //                scope.ServiceProvider
        //                    .GetRequiredService<IAnalyticsService>();

        //            await analyticsService.TrackImpressionAsync(
        //                item.ArticleId);

        //            if (item.IncrementViewCount)
        //            {
        //                var articleService =
        //                    scope.ServiceProvider
        //                        .GetRequiredService<IArticleService>();

        //                await articleService.IncrementViewCountAsync(
        //                    item.ArticleId);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(
        //                ex,
        //                "Failed to process article engagement for ArticleId {ArticleId}",
        //                item.ArticleId);
        //        }
        //    }
        //}
    }
}