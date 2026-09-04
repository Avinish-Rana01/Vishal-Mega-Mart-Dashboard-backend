using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace VS_Mart_Backend.Features.Base
{
    public abstract class BaseDashboardService
    {
        protected readonly IConfiguration _configuration;
        protected readonly IMemoryCache _cache;
        protected readonly string _connectionString;

        private static bool? _cacheOverride = null;
        private readonly ConcurrentDictionary<string, bool> _refreshingKeys = new();

        private class CacheItem<T>
        {
            public T Data { get; set; } = default!;
            public DateTime CreatedAt { get; set; }
        }

        protected BaseDashboardService(IConfiguration configuration, IMemoryCache cache)
        {
            _configuration = configuration;
            _cache = cache;
            _connectionString = _configuration.GetConnectionString("POS") 
                ?? throw new InvalidOperationException("Connection string 'POS' was not found in configuration.");
        }

        public bool IsCacheEnabled()
        {
            if (_cacheOverride.HasValue) return _cacheOverride.Value;
            return _configuration.GetValue<bool>("EnableCache", true);
        }

        public void SetCacheEnabled(bool enabled)
        {
            _cacheOverride = enabled;
        }

        protected async Task<T> GetOrCreateWithSWRAsync<T>(string cacheKey, Func<Task<T>> databaseQuery)
        {
            if (!IsCacheEnabled()) return await databaseQuery();

            if (_cache.TryGetValue(cacheKey, out CacheItem<T>? cachedItem) && cachedItem != null)
            {
                if (DateTime.UtcNow - cachedItem.CreatedAt > TimeSpan.FromSeconds(20))
                {
                    if (_refreshingKeys.TryAdd(cacheKey, true))
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var freshData = await databaseQuery();
                                _cache.Set(cacheKey, new CacheItem<T> { Data = freshData, CreatedAt = DateTime.UtcNow }, TimeSpan.FromSeconds(90));
                            }
                            finally
                            {
                                _refreshingKeys.TryRemove(cacheKey, out _);
                            }
                        });
                    }
                }
                return cachedItem.Data;
            }

            var initialData = await databaseQuery();
            _cache.Set(cacheKey, new CacheItem<T> { Data = initialData, CreatedAt = DateTime.UtcNow }, TimeSpan.FromSeconds(90));
            return initialData;
        }
    }
}
