using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class ArticleLockService : IArticleLockService
    {
        private static readonly TimeSpan LockTimeout =
            TimeSpan.FromMinutes(30);

        private readonly IArticleRepository _articleRepository;

        public ArticleLockService(
            IArticleRepository articleRepository)
        {
            _articleRepository = articleRepository;
        }

        public async Task<bool> TryAcquireLockAsync(
            Article article,
            string currentUserId)
        {
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

        public bool IsLockedByAnotherUser(
            Article article,
            string currentUserId)
        {
            if (!article.LockedAt.HasValue)
                return false;

            if (article.LockedByUserId == currentUserId)
                return false;

            var expired =
                DateTime.UtcNow - article.LockedAt.Value > LockTimeout;

            return !expired;
        }

        public async Task ReleaseLockAsync(
            Article article,
            string currentUserId)
        {
            if (article.LockedByUserId != currentUserId)
                return;

            article.LockedByUserId = null;
            article.LockedAt = null;

            await _articleRepository.SaveChangesAsync();
        }
    }
}
