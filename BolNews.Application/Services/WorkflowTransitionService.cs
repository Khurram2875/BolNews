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

        public WorkflowTransitionService(
            IArticleRevisionService articleRevisionService,
            INotificationService notificationService,
            IArticleScoringService articleScoringService)
        {
            _articleRevisionService = articleRevisionService;
            _notificationService = notificationService;
            _articleScoringService = articleScoringService;
        }

        public async Task ExecuteTransitionAsync(
            Article article,
            ArticleWorkflowStatus targetStatus,
            string currentUserId,
            IList<string> roles,
            string? reason = null)
        {
            if (!(roles.Contains(Roles.Admin) || roles.Contains(Roles.Editor) || roles.Contains(Roles.SubEditor)))
                throw new UnauthorizedAccessException("You are not authorized for editorial workflow transitions.");

            ValidateWorkflowTransition(article.WorkflowStatus, targetStatus, roles);

            await _articleRevisionService.CreateSnapshotAsync(
                article,
                currentUserId,
                workflowState: $"{article.WorkflowStatus} -> {targetStatus}",
                changeReason: reason);

            article.WorkflowStatus = targetStatus;
            article.UpdatedAt = DateTime.UtcNow;
            article.UpdatedBy = currentUserId;
            article.WorkflowComment = null;

            if (targetStatus == ArticleWorkflowStatus.Rejected)
            {
                article.IsPublished = false;
                article.WorkflowComment = reason;
                if (!string.IsNullOrWhiteSpace(article.Author?.UserId))
                {
                    await _notificationService.NotifyAsync(article.Author.UserId, "Article Rejected",
                        $"Your article '{article.Title}' was rejected. Reason: {reason}",
                        $"/Admin/Articles/Edit/{article.Id}");
                }
            }

            if (targetStatus == ArticleWorkflowStatus.Approved && !string.IsNullOrWhiteSpace(article.Author?.UserId))
            {
                await _notificationService.NotifyAsync(article.Author.UserId, "Article Approved",
                    $"Your article '{article.Title}' has been approved.",
                    $"/Admin/Articles/Edit/{article.Id}");
            }

            if (targetStatus == ArticleWorkflowStatus.Published && !string.IsNullOrWhiteSpace(article.Author?.UserId))
            {
                await _notificationService.NotifyAsync(article.Author.UserId, "Article Published",
                    $"Your article '{article.Title}' is now live.",
                    $"/Articles/{article.Slug}");
            }

            article.UpdatedAt = DateTime.UtcNow;
            article.UpdatedBy = currentUserId;
            await _articleScoringService.CalculateScoresAsync(article);
        }

        private static void ValidateWorkflowTransition(ArticleWorkflowStatus current, ArticleWorkflowStatus target, IList<string> roles)
        {
            if (roles.Contains(Roles.Admin) || roles.Contains(Roles.Editor))
                return;

            if (roles.Contains(Roles.SubEditor))
            {
                var allowed = current switch
                {
                    ArticleWorkflowStatus.Submitted => target == ArticleWorkflowStatus.UnderReview || target == ArticleWorkflowStatus.Rejected,
                    ArticleWorkflowStatus.UnderReview => target == ArticleWorkflowStatus.FactCheckPending || target == ArticleWorkflowStatus.Rejected,
                    ArticleWorkflowStatus.FactCheckPending => target == ArticleWorkflowStatus.Approved || target == ArticleWorkflowStatus.Rejected,
                    ArticleWorkflowStatus.Approved => target == ArticleWorkflowStatus.Published,
                    _ => false
                };

                if (!allowed)
                    throw new UnauthorizedAccessException($"Transition from {current} to {target} is not allowed.");

                return;
            }

            throw new UnauthorizedAccessException("Workflow transition denied.");
        }
    }
}
