using Application.DTOs;
using Application.Ports.Output;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Cache
{
    

    public class RedisProductCache : IProductCache
    {
        private readonly IDatabase _redis;
        private readonly IMemoryCache _memoryCache;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisProductCache(IConnectionMultiplexer redis, IMemoryCache memoryCache)
        {
            _redis = redis.GetDatabase();
            _memoryCache = memoryCache;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }
        public async Task<ProductResponseDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_memoryCache.TryGetValue(id, out ProductResponseDto? cached))
                return cached;

            var redisValue = await _redis.StringGetAsync($"product:{id}");
            if (redisValue.HasValue)
            {
                var product = JsonSerializer.Deserialize<ProductResponseDto>(redisValue!, _jsonOptions);

                if (product != null)
                {
                    _memoryCache.Set(id, product, TimeSpan.FromMinutes(1));
                }
                return product;
            }
            return null;
        }

        public async Task InvalidateSearchAsync(CancellationToken cancellationToken = default)
        {
            var server = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: "products:search:*").ToArray();
            if (keys.Any())
            {
                await _redis.KeyDeleteAsync(keys);
            }
        }

        public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _redis.KeyDeleteAsync($"product:{id}");
            _memoryCache.Remove(id);
        }

        public async Task SetAsync(Guid id, ProductResponseDto product, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            var serialized = JsonSerializer.Serialize(product, _jsonOptions);
            await _redis.StringSetAsync($"product:{id}", serialized, ttl);
            _memoryCache.Set(id, product, TimeSpan.FromMinutes(1));
        }
    }
}
