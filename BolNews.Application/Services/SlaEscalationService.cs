using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;

namespace BolNews.Application.Services
{
    public class SlaEscalationService : ISlaEscalationService
    {
        private readonly IArticleRepository _articleRepository;
        private readonly ISlaService _slaService;
        private readonly INotificationService _notificationService;

        public SlaEscalationService(
            IArticleRepository articleRepository,
            ISlaService slaService,
            INotificationService notificationService)
        {
            _articleRepository = articleRepository;
            _slaService = slaService;
            _notificationService = notificationService;
        }

        public async Task ProcessAsync()
        {
            var articles =
                await _articleRepository.GetActiveWorkflowArticlesAsync();

            foreach (var article in articles)
            {
                var sla = _slaService.Evaluate(article);

                if (!sla.IsTracked || !sla.IsOverdue)
                    continue;

                await ProcessArticleAsync(article);
            }
            await _articleRepository.SaveChangesAsync();
        }

        private async Task ProcessArticleAsync(Article article)
        {
            switch (article.WorkflowStatus)
            {
                case ArticleWorkflowStatus.Submitted:
                    await HandleSubmittedAsync(article);
                    break;

                case ArticleWorkflowStatus.UnderReview:
                    await HandleUnderReviewAsync(article);
                    break;

                case ArticleWorkflowStatus.FactCheckPending:
                    await HandleFactCheckAsync(article);
                    break;

                case ArticleWorkflowStatus.Approved:
                    await HandleApprovedAsync(article);
                    break;
            }
        }

        private async Task HandleSubmittedAsync(Article article)
        {
            if (article.ReviewEscalatedAt.HasValue)
                return;

            if (!string.IsNullOrWhiteSpace(article.ReviewerUserId))
            {
                await _notificationService.NotifyAsync(
                    article.ReviewerUserId,
                    "Review SLA Breached",
                    $"Article '{article.Title}' is overdue for review.",
                    $"/Admin/Articles/Edit/{article.Id}");
            }

            article.ReviewEscalatedAt = DateTime.UtcNow;
        }

        private async Task HandleUnderReviewAsync(Article article)
        {
            if (article.FactCheckEscalatedAt.HasValue)
                return;

            if (!string.IsNullOrWhiteSpace(article.ReviewerUserId))
            {
                await _notificationService.NotifyAsync(
                    article.ReviewerUserId,
                    "Editorial Review Overdue",
                    $"Article '{article.Title}' review is overdue.",
                    $"/Admin/Articles/Edit/{article.Id}");
            }

            article.FactCheckEscalatedAt = DateTime.UtcNow;
        }

        private async Task HandleFactCheckAsync(Article article)
        {
            if (article.FactCheckEscalatedAt.HasValue)
                return;

            if (!string.IsNullOrWhiteSpace(article.FactCheckerUserId))
            {
                await _notificationService.NotifyAsync(
                    article.FactCheckerUserId,
                    "Fact Check SLA Breached",
                    $"Article '{article.Title}' is overdue for fact checking.",
                    $"/Admin/Articles/Edit/{article.Id}");
            }

            article.FactCheckEscalatedAt = DateTime.UtcNow;
        }

        private async Task HandleApprovedAsync(Article article)
        {
            if (article.PublishEscalatedAt.HasValue)
                return;

            if (!string.IsNullOrWhiteSpace(article.Author?.UserId))
            {
                await _notificationService.NotifyAsync(
                    article.Author.UserId,
                    "Publishing Delay",
                    $"Approved article '{article.Title}' has not been published yet.",
                    $"/Admin/Articles/Edit/{article.Id}");
            }

            article.PublishEscalatedAt = DateTime.UtcNow;
        }
    }
}
