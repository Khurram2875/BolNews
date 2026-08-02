using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class ArticleDiscussionService : IArticleDiscussionService
    {
        private readonly IArticleRepository _articleRepository;
        private readonly INotificationService _notificationService;
        private readonly INotificationRealtimeService _realtimeService;
        private readonly IPresenceTracker _presenceTracker;

        public ArticleDiscussionService(IArticleRepository articleRepository, INotificationService notificationService, INotificationRealtimeService realtimeService, IPresenceTracker presenceTracker)
        {
            _articleRepository = articleRepository;
            _notificationService = notificationService;
            _realtimeService = realtimeService;
            _presenceTracker = presenceTracker;
        }

        public async Task<List<ArticleDiscussionCommentDto>> GetThreadAsync(int articleId, string currentUserId, IList<string> roles)
        {
            var article = await _articleRepository.FindByIdAsync(articleId);

            if (article == null)
                throw new InvalidOperationException("Article not found.");

            EnsureDiscussionAccess(article, currentUserId, roles);

            return article.DiscussionComments
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new ArticleDiscussionCommentDto
                {
                    Id = c.Id,
                    ArticleId = c.ArticleId,
                    UserId = c.UserId,
                    UserName = c.User.FullName,
                    Message = c.Message,
                    CreatedAt = c.CreatedAt,
                    IsCurrentUser = c.UserId == currentUserId
                })
                .ToList();
        }

        public async Task AddCommentAsync(int articleId, string message, string currentUserId, IList<string> roles)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new InvalidOperationException("Comment cannot be empty.");

            var article = await _articleRepository.FindByIdAsync(articleId);

            if (article == null)
                throw new InvalidOperationException("Article not found.");

            EnsureDiscussionAccess(article, currentUserId, roles);

            var comment = new ArticleDiscussionComment
            {
                ArticleId = articleId,
                UserId = currentUserId,
                Message = message.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            article.DiscussionComments.Add(comment);
            //article.DiscussionComments.Add(
            //    new ArticleDiscussionComment
            //    {
            //        ArticleId = articleId,
            //        UserId = currentUserId,
            //        Message = message.Trim(),
            //        CreatedAt = DateTime.UtcNow,
            //        IsDeleted = false
            //    });

            await _articleRepository.SaveChangesAsync();
            await NotifyDiscussionParticipantsAsync(article, currentUserId, comment.Message);
            await SendRealtimeDiscussionUpdateAsync(article, currentUserId, comment);
        }

        private static void EnsureDiscussionAccess(Article article, string currentUserId, IList<string> roles)
        {
            if (roles.Contains(Roles.Admin) ||
                roles.Contains(Roles.Editor) ||
                roles.Contains(Roles.SubEditor))
            {
                return;
            }

            if (roles.Contains(Roles.Author))
            {
                var ownsArticle =
                    article.CreatedBy == currentUserId;

                if (!ownsArticle)
                {
                    throw new UnauthorizedAccessException(
                        "Authors can only access discussions for their own articles.");
                }

                return;
            }

            if (roles.Contains(Roles.Factchecker))
            {
                if (article.FactCheckerUserId == currentUserId)
                    return;
            }

            throw new UnauthorizedAccessException(
                "Access denied.");
        }
        //Notification Helper
        private async Task NotifyDiscussionParticipantsAsync(Article article, string currentUserId, string message)
        {
            var recipients = await ResolveDiscussionRecipientsAsync(article, currentUserId);

            if (!string.IsNullOrWhiteSpace(article.Author?.UserId))
                recipients.Add(article.Author.UserId);

            if (!string.IsNullOrWhiteSpace(article.ReviewerUserId))
                recipients.Add(article.ReviewerUserId);

            if (!string.IsNullOrWhiteSpace(article.FactCheckerUserId))
                recipients.Add(article.FactCheckerUserId);

            // Don't notify commenter
            recipients.Remove(currentUserId);

            foreach (var recipient in recipients)
            {
                if (_presenceTracker.IsUserInDiscussion(
                    article.Id.ToString(),
                    recipient))
                {
                    continue;
                }

                await _notificationService.NotifyAsync(
                    recipient,
                    "Editorial Discussion Update",
                    $"New discussion comment on '{article.Title}'.",
                    $"/Admin/Articles/Discussion/{article.Id}");
            }
        }
        //Reltime Notification Helper
        private async Task SendRealtimeDiscussionUpdateAsync(Article article, string currentUserId, ArticleDiscussionComment comment)
        {
            var recipients = await ResolveDiscussionRecipientsAsync(article, currentUserId);

            if (!string.IsNullOrWhiteSpace(article.Author?.UserId))
                recipients.Add(article.Author.UserId);

            if (!string.IsNullOrWhiteSpace(article.ReviewerUserId))
                recipients.Add(article.ReviewerUserId);

            if (!string.IsNullOrWhiteSpace(article.FactCheckerUserId))
                recipients.Add(article.FactCheckerUserId);

            recipients.Remove(currentUserId);
            Console.WriteLine("=== DISCUSSION REALTIME FIRING ===");
            Console.WriteLine($"Article: {article.Id}");
            Console.WriteLine($"Recipients: {string.Join(",", recipients)}");
            Console.WriteLine($"Message: {comment.Message}");
            await _realtimeService.SendDiscussionUpdateAsync(recipients, 
                new
                {
                    ArticleId = article.Id,
                    UserName = article.DiscussionComments
                        .FirstOrDefault(x => x.UserId == currentUserId)
                        ?.User?.FullName ?? "Newsroom User",
                    Message = comment.Message,
                    CreatedAt = comment.CreatedAt.ToString("g")


                });
           
        }
        private async Task<HashSet<string>> ResolveDiscussionRecipientsAsync(Article article, string currentUserId)
        {
            var recipients = new HashSet<string>();

            if (!string.IsNullOrWhiteSpace(article.Author?.UserId))
                recipients.Add(article.Author.UserId);

            if (!string.IsNullOrWhiteSpace(article.ReviewerUserId))
                recipients.Add(article.ReviewerUserId);

            if (!string.IsNullOrWhiteSpace(article.FactCheckerUserId))
                recipients.Add(article.FactCheckerUserId);

            // Anyone who has previously commented on this thread should also be notified
            foreach (var commenterId in article.DiscussionComments
                         .Where(c => !c.IsDeleted)
                         .Select(c => c.UserId)
                         .Distinct())
            {
                if (!string.IsNullOrWhiteSpace(commenterId))
                    recipients.Add(commenterId);
            }

            recipients.Remove(currentUserId);

            return recipients;
        }

    }
}
