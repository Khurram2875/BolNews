using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BolNews.Application.Services
{
    public class ArticleLockService : IArticleLockService
    {
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(30);
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ArticleLockService> _logger;

        private readonly IArticleRepository _articleRepository;

        public ArticleLockService(IArticleRepository articleRepository, UserManager<ApplicationUser> userManager, ILogger<ArticleLockService> logger )
        {
            _articleRepository = articleRepository;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<bool> TryAcquireLockAsync(Article article,string currentUserId)
        {
            _logger.LogInformation("Article {ArticleId} locked by {UserId}",article.Id,currentUserId);

            if (!article.LockedAt.HasValue ||
                string.IsNullOrWhiteSpace(article.LockedByUserId))
            {
                article.LockedByUserId = currentUserId;
                article.LockedAt = DateTime.UtcNow;

                await _articleRepository.SaveChangesAsync();

                return true;
            }

            if (article.LockedByUserId == currentUserId)
            {
                article.LockedAt = DateTime.UtcNow;

                await _articleRepository.SaveChangesAsync();

                return true;
            }

            var expired =
                DateTime.UtcNow - article.LockedAt.Value > LockTimeout;

            if (expired)
            {
                article.LockedByUserId = currentUserId;
                article.LockedAt = DateTime.UtcNow;

                await _articleRepository.SaveChangesAsync();

                return true;
            }

            return false;
        }

        public bool IsLockedByAnotherUser(Article article,string currentUserId)
        {
            if (!article.LockedAt.HasValue)
                return false;

            if (article.LockedByUserId == currentUserId)
                return false;

            var expired =
                DateTime.UtcNow - article.LockedAt.Value > LockTimeout;

            _logger.LogInformation("Expired lock on article {ArticleId} reassigned to {UserId}",article.Id,currentUserId);

            return !expired;
        }

        public async Task ReleaseLockAsync(Article article,string currentUserId)
        {
            _logger.LogInformation("Article {ArticleId} lock released by {UserId}",article.Id,currentUserId);

            if (article.LockedByUserId != currentUserId)
                return;

            article.LockedByUserId = null;
            article.LockedAt = null;

            await _articleRepository.SaveChangesAsync();
        }
        public async Task<string?> GetLockOwnerNameAsync(Article article, string currentUserId)
        {
            if (!IsLockedByAnotherUser(article, currentUserId))
                return null;

            if (string.IsNullOrWhiteSpace(article.LockedByUserId))
                return "another newsroom user";

            var user = await _userManager.FindByIdAsync(article.LockedByUserId);

            return user?.FullName ?? "another newsroom user";
        }
        public async Task RefreshLockAsync(Article article, string currentUserId)
        {
            if (article.LockedByUserId != currentUserId)
                return;

            article.LockedAt = DateTime.UtcNow;

            await _articleRepository.SaveChangesAsync();
        }
    }
}
