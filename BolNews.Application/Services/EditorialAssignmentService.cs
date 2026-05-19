using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class EditorialAssignmentService : IEditorialAssignmentService
    {
        private readonly INotificationService _notificationService;

        public EditorialAssignmentService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task AssignReviewerAsync(
            Article article,
            string reviewerUserId,
            string currentUserId,
            IList<string> roles)
        {
            if (!(roles.Contains(Roles.Admin) || roles.Contains(Roles.Editor) || roles.Contains(Roles.SubEditor)))
                throw new UnauthorizedAccessException("Only editorial staff can assign reviewers.");

            article.ReviewerUserId = reviewerUserId;
            article.UpdatedAt = DateTime.UtcNow;
            article.UpdatedBy = currentUserId;

            if (!string.IsNullOrWhiteSpace(reviewerUserId))
            {
                await _notificationService.NotifyAsync(
                    reviewerUserId,
                    "Reviewer Assignment",
                    $"You have been assigned to review '{article.Title}'.",
                    $"/Admin/Articles/Edit/{article.Id}");
            }
        }

        public async Task AssignFactCheckerAsync(
            Article article,
            string factCheckerUserId,
            string currentUserId,
            IList<string> roles)
        {
            if (!(roles.Contains(Roles.Admin) || roles.Contains(Roles.Editor) || roles.Contains(Roles.SubEditor)))
                throw new UnauthorizedAccessException("Only editorial staff can assign fact checkers.");

            article.FactCheckerUserId = factCheckerUserId;
            article.UpdatedAt = DateTime.UtcNow;
            article.UpdatedBy = currentUserId;

            if (!string.IsNullOrWhiteSpace(factCheckerUserId))
            {
                await _notificationService.NotifyAsync(
                    factCheckerUserId,
                    "Fact Checker Assignment",
                    $"You have been assigned to fact check '{article.Title}'.",
                    $"/Admin/Articles/Edit/{article.Id}");
            }
        }
    }
}
