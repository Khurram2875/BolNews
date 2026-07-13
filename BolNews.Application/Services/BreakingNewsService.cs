using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Services
{
    public class BreakingNewsService : IBreakingNewsService
    {
        private readonly IBreakingNewsRepository _repository;
        private readonly IMemoryCache _cache;

        private const string CacheKey = "breaking_news";

        public BreakingNewsService(
            IBreakingNewsRepository repository,
            IMemoryCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<List<BreakingNews>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<List<BreakingNews>> GetActiveAsync()
        {
            var result = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);

                return await _repository.GetActiveAsync();
            }) ?? new List<BreakingNews>();

            return result;
        }

        public async Task<List<BreakingNews>> GetInactiveAsync()
        {
            return await _repository.GetInactiveAsync();
        }

        public async Task<BreakingNews?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateAsync(BreakingNews news, string currentUserId)
        {
            
            news.CreatedAt = DateTime.UtcNow;
            news.CreatedByUserId = currentUserId;

            await _repository.AddAsync(news);
            await _repository.SaveChangesAsync();

            ClearCache();
        }

        public async Task UpdateAsync(BreakingNews news, string currentUserId)
        {
            news.UpdatedAt = DateTime.UtcNow;
            news.UpdatedByUserId = currentUserId;

            await _repository.UpdateAsync(news);
            await _repository.SaveChangesAsync();

            ClearCache();
        }

        public async Task DeleteAsync(int id, string currentUserId)
        {
            var news = await _repository.GetByIdAsync(id);

            if (news == null)
                return;

            news.IsDeleted = true;
            news.UpdatedAt = DateTime.UtcNow;
            news.UpdatedByUserId = currentUserId;

            await _repository.UpdateAsync(news);
            await _repository.SaveChangesAsync();

            ClearCache();
        }

        public async Task ActivateAsync(int id, string currentUserId)
        {
            var news = await _repository.GetByIdAsync(id);

            if (news == null || news.IsActive)
                return;

            news.IsActive = true;
            news.UpdatedAt = DateTime.UtcNow;
            news.UpdatedByUserId = currentUserId;

            await _repository.UpdateAsync(news);
            await _repository.SaveChangesAsync();

            ClearCache();
        }

        public async Task DeactivateAsync(int id, string currentUserId)
        {
            var news = await _repository.GetByIdAsync(id);

            if (news == null || !news.IsActive)
                return;

            news.IsActive = false;
            news.UpdatedAt = DateTime.UtcNow;
            news.UpdatedByUserId = currentUserId;

            await _repository.UpdateAsync(news);
            await _repository.SaveChangesAsync();

            ClearCache();
        }

        private void ClearCache()
        {
            _cache.Remove(CacheKey);
        }
    }
}
