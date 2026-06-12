using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;

namespace BolNews.Application.Services
{
    public class WorkflowTransitionService : IWorkflowTransitionService
    {
        private readonly IArticleRevisionService _articleRevisionService;
        private readonly INotificationService _notificationService;
        private readonly IArticleScoringService _articleScoringService;
        private readonly ICacheService _cacheService;


        public WorkflowTransitionService(
            IArticleRevisionService articleRevisionService,
            INotificationService notificationService,
            IArticleScoringService articleScoringService, ICacheService cacheService)
        {
            _articleRevisionService = articleRevisionService;
            _notificationService = notificationService;
            _articleScoringService = articleScoringService;
            _cacheService = cacheService;
        }

        public async Task ExecuteTransitionAsync(
            Article article,
            ArticleWorkflowStatus targetStatus,
            string currentUserId,
            IList<string> roles,
            string? reason = null)
        {
            if (!(roles.Contains(Roles.Admin) ||
                  roles.Contains(Roles.Editor) ||
                  roles.Contains(Roles.SubEditor)))
            {
                throw new UnauthorizedAccessException(
                    "You are not authorized for editorial workflow transitions.");
            }

            ValidateWorkflowTransition(
                article.WorkflowStatus,
                targetStatus,
                roles);

            await _articleRevisionService.CreateSnapshotAsync(
                article,
                currentUserId,
                workflowState: $"{article.WorkflowStatus} -> {targetStatus}",
                changeReason: reason);

            var now = DateTime.UtcNow;

            var rejectionReason = string.IsNullOrWhiteSpace(reason)
                ? "No reason provided."
                : reason;

            article.WorkflowStatus = targetStatus;
            article.UpdatedAt = now;
            article.UpdatedBy = currentUserId;

            // Clear old workflow comments by default
            article.WorkflowComment = null;

            // SLA TIMESTAMP TRACKING
            switch (targetStatus)
            {
                case ArticleWorkflowStatus.UnderReview:
                    article.ReviewStartedAt = now;
                    break;

                case ArticleWorkflowStatus.FactCheckPending:
                    article.FactCheckStartedAt = now;
                    break;

                case ArticleWorkflowStatus.Approved:
                    article.ApprovedAt = now;
                    break;
            }

            // REJECTION
            if (targetStatus == ArticleWorkflowStatus.Rejected)
            {
                article.IsPublished = false;
                article.WorkflowComment = rejectionReason;

                // clear scheduling if rejected
                article.ScheduledPublishAt = null;
                article.EmbargoUntil = null;

                if (!string.IsNullOrWhiteSpace(article.Author?.UserId))
                {
                    await _notificationService.NotifyAsync(
                        article.Author.UserId,
                        "Article Rejected",
                        $"Your article '{article.Title}' was rejected. Reason: {rejectionReason}",
                        $"/Admin/Articles/Edit/{article.Id}");
                }

                await _articleScoringService.CalculateScoresAsync(article);
                return;
            }

            // APPROVED
            if (targetStatus == ArticleWorkflowStatus.Approved &&
                !string.IsNullOrWhiteSpace(article.Author?.UserId))
            {
                await _notificationService.NotifyAsync(
                    article.Author.UserId,
                    "Article Approved",
                    $"Your article '{article.Title}' has been approved.",
                    $"/Admin/Articles/Edit/{article.Id}");
            }

            // PUBLISH / SCHEDULE / EMBARGO LOGIC
            if (targetStatus == ArticleWorkflowStatus.Published)
            {
                var effectivePublishTime =
                    article.ScheduledPublishAt ?? article.EmbargoUntil;

                // FUTURE publish requested
                if (effectivePublishTime.HasValue &&
                    effectivePublishTime.Value > now)
                {
                    article.IsPublished = false;

                    // keep article approved until scheduler publishes it
                    article.WorkflowStatus = ArticleWorkflowStatus.Approved;

                    article.UpdatedAt = now;
                    article.UpdatedBy = currentUserId;

                    await _articleScoringService.CalculateScoresAsync(article);
                    return;
                }

                // IMMEDIATE publish
                article.IsPublished = true;

                if (!article.PublishedAt.HasValue)
                    article.PublishedAt = now;
                

                if (!string.IsNullOrWhiteSpace(article.Author?.UserId))
                {
                    await _notificationService.NotifyAsync(
                        article.Author.UserId,
                        "Article Published",
                        $"Your article '{article.Title}' is now live.",
                        $"/news/{article.Category.Slug}/{article.Slug}");
                }
                InvalidatePublicCaches();
            }

            await _articleScoringService.CalculateScoresAsync(article);
        }

        private static void ValidateWorkflowTransition(
            ArticleWorkflowStatus current,
            ArticleWorkflowStatus target,
            IList<string> roles)
        {
            if (roles.Contains(Roles.Admin) || roles.Contains(Roles.Editor))
                return;

            if (roles.Contains(Roles.SubEditor))
            {
                var allowed = current switch
                {
                    ArticleWorkflowStatus.Submitted =>
                        target == ArticleWorkflowStatus.UnderReview ||
                        target == ArticleWorkflowStatus.Rejected,

                    ArticleWorkflowStatus.UnderReview =>
                        target == ArticleWorkflowStatus.FactCheckPending ||
                        target == ArticleWorkflowStatus.Rejected,

                    ArticleWorkflowStatus.FactCheckPending =>
                        target == ArticleWorkflowStatus.Approved ||
                        target == ArticleWorkflowStatus.Rejected,

                    ArticleWorkflowStatus.Approved =>
                        target == ArticleWorkflowStatus.Published,

                    _ => false
                };

                if (!allowed)
                {
                    throw new UnauthorizedAccessException(
                        $"Transition from {current} to {target} is not allowed.");
                }

                return;
            }

            throw new UnauthorizedAccessException("Workflow transition denied.");
        }
        private void InvalidatePublicCaches()
        {
            _cacheService.Remove(CacheKeys.HomePage);

            _cacheService.Remove(CacheKeys.BreakingNews);

            _cacheService.Remove(CacheKeys.Trending("today"));

            _cacheService.Remove(CacheKeys.Trending("week"));

            _cacheService.Remove(CacheKeys.Trending("month"));

            _cacheService.Remove(CacheKeys.NavbarCategories);

            _cacheService.Remove(CacheKeys.LatestNews(5));

            _cacheService.Remove(CacheKeys.LatestNews(10));

            _cacheService.Remove(CacheKeys.Sitemap);
        }
    }
}
