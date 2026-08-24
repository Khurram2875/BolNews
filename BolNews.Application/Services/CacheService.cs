using BolNews.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BolNews.Application.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private static readonly ConcurrentDictionary<string, CacheKeyLock> _keyLocks = new();
        private readonly ILogger<CacheService> _logger;

        private sealed class CacheKeyLock
        {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);
            public int ReferenceCount { get; set; }
        }

        public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        //updated code GetOrCreateAsync with logging
        public async Task<T> GetOrCreateAsync<T>(
    string key,
    Func<Task<T>> factory,
    int minutes = 10)
        {
            if (_cache.TryGetValue(key, out object? cachedValue))
            {
                _logger.LogInformation(
                    "CACHE HIT key={Key}",
                    key);

                return (T)cachedValue!;
            }

            _logger.LogInformation(
                "CACHE MISS key={Key}",
                key);

            var keyLock = RentLock(key);

            await keyLock.Semaphore.WaitAsync();

            try
            {
                if (_cache.TryGetValue(key, out cachedValue))
                {
                    _logger.LogInformation(
                        "CACHE HIT_AFTER_LOCK key={Key}",
                        key);

                    return (T)cachedValue!;
                }

                _logger.LogInformation(
                    "CACHE BUILD key={Key} minutes={Minutes}",
                    key,
                    minutes);

                var value = await factory();

                _cache.Set(
                    key,
                    value,
                    TimeSpan.FromMinutes(minutes));

                _logger.LogInformation(
                    "CACHE STORE key={Key} minutes={Minutes}",
                    key,
                    minutes);

                return value;
            }
            finally
            {
                keyLock.Semaphore.Release();
                ReturnLock(key, keyLock);
            }
        }
        //original code GetOrCreateAsync 
        //public async Task<T> GetOrCreateAsync<T>(
        //    string key,
        //    Func<Task<T>> factory,
        //    int minutes = 10)
        //{
        //    if (_cache.TryGetValue(key, out object? cachedValue))
        //        return (T)cachedValue!;

        //    var keyLock = RentLock(key);
        //    await keyLock.Semaphore.WaitAsync();
        //    try
        //    {
        //        // someone else may have already rebuilt this exact key while we waited
        //        if (_cache.TryGetValue(key, out cachedValue))
        //            return (T)cachedValue!;

        //        var value = await factory();

        //        _cache.Set(key, value, TimeSpan.FromMinutes(minutes));

        //        return value;
        //    }
        //    finally
        //    {
        //        keyLock.Semaphore.Release();
        //        ReturnLock(key, keyLock);
        //    }
        //}

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        private static CacheKeyLock RentLock(string key)
        {
            while (true)
            {
                var keyLock = _keyLocks.GetOrAdd(key, _ => new CacheKeyLock());

                lock (keyLock)
                {
                    if (_keyLocks.TryGetValue(key, out var currentLock) &&
                        ReferenceEquals(currentLock, keyLock))
                    {
                        keyLock.ReferenceCount++;
                        return keyLock;
                    }
                }
            }
        }

        private static void ReturnLock(string key, CacheKeyLock keyLock)
        {
            lock (keyLock)
            {
                keyLock.ReferenceCount--;

                if (keyLock.ReferenceCount == 0)
                {
                    _keyLocks.TryRemove(new KeyValuePair<string, CacheKeyLock>(key, keyLock));
                }
            }
        }
    }
}
