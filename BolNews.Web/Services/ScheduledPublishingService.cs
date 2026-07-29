using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace BolNews.Web.Services
{
    public class ScheduledPublishingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledPublishingService> _logger;
       

        public ScheduledPublishingService(
            IServiceScopeFactory scopeFactory,
            ILogger<ScheduledPublishingService> logger
           )
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            //Console.WriteLine(
            //    "Scheduled publishing service started.");
            //_logger.LogInformation(
            //    "Scheduled publishing service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var articleRepository =
                        scope.ServiceProvider
                            .GetRequiredService<IArticleRepository>();

                    var notificationService =
                        scope.ServiceProvider
                            .GetRequiredService<INotificationService>();

                    var scoringService =
                        scope.ServiceProvider
                            .GetRequiredService<IArticleScoringService>();

                    var cacheService =
                    scope.ServiceProvider
                        .GetRequiredService<ICacheService>();
                    var now = DateTime.UtcNow;
                    //_logger.LogInformation("Scheduler heartbeat at {Time}",  now);

                    ////for testing on localhost
                    //Console.WriteLine($"Scheduler heartbeat at {DateTime.UtcNow}");

                    var dueArticles =
                        await articleRepository
                            .GetDueScheduledArticlesAsync(now);

                    //_logger.LogInformation("Due scheduled articles found: {Count}", dueArticles.Count);

                    foreach (var article in dueArticles)
                    {
                        article.IsPublished = true;
                        article.WorkflowStatus =
                            ArticleWorkflowStatus.Published;

                        if (!article.PublishedAt.HasValue)
                        {
                            article.PublishedAt = now;
                        }
                        article.UpdatedAt = now;

                        // clear scheduling fields
                        article.ScheduledPublishAt = null;
                        article.EmbargoUntil = null;

                        await scoringService
                            .CalculateScoresAsync(article);

                        if (!string.IsNullOrWhiteSpace(
                            article.Author?.UserId))
                        {
                            await notificationService.NotifyAsync(
                                article.Author.UserId,
                                "Article Published",
                                $"Your article '{article.Title}' is now live.",
                                $"/{article.Category.Slug}/{article.Slug}");
                        }

                        _logger.LogInformation( "Auto publishing article {Id} - {Title}", article.Id, article.Title);
                    }

                    if (dueArticles.Any())
                    {
                        await articleRepository.SaveChangesAsync();

                        cacheService.InvalidateHomePage();

                        cacheService.Remove(CacheKeys.BreakingNews);

                        cacheService.Remove(CacheKeys.Trending("today"));

                        cacheService.Remove(CacheKeys.Trending("week"));

                        cacheService.Remove(CacheKeys.Trending("month"));

                        cacheService.Remove(CacheKeys.NavbarCategories);

                        cacheService.Remove(CacheKeys.LatestNews(5));

                        cacheService.Remove(CacheKeys.LatestNews(10));

                        cacheService.Remove(CacheKeys.Sitemap);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Scheduled publishing failed.");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }
    }
}
