using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BolNews.Application.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

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

            value = await factory();

            _cache.Set(key, value, TimeSpan.FromMinutes(minutes));

            return value;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }
    }
