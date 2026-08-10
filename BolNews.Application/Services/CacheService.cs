using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Application.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            int minutes = 10)
        {
            if (_cache.TryGetValue(key, out T value))
                return value;

            var keyLock = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await keyLock.WaitAsync();
            try
            {
                // someone else may have already rebuilt this exact key while we waited
                if (_cache.TryGetValue(key, out value))
                    return value;

                value = await factory();

                _cache.Set(key, value, TimeSpan.FromMinutes(minutes));

                return value;
            }
            finally
            {
                keyLock.Release();
            }
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }
    }
}