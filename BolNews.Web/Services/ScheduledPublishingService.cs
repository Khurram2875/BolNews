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
            ILogger<ScheduledPublishingService> logger)
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
                                $"/news/{article.Category.Slug}/{article.Slug}");
                        }

                        _logger.LogInformation( "Auto publishing article {Id} - {Title}", article.Id, article.Title);
                    }

                    if (dueArticles.Any())
                    {
                        await articleRepository.SaveChangesAsync();
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