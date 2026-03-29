using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Tokki.Application.IServices;

namespace Tokki.Infrastructure.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _db;

        public RedisService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task SetAsync(string key, string value, TimeSpan ttl)
        {
            await _db.StringSetAsync(key, value, ttl);
        }

        public async Task<string?> GetAsync(string key)
        {
            return (string?)(await _db.StringGetAsync(key));
        }

        public async Task DeleteAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }

        public async Task<TimeSpan?> GetTtlAsync(string key)
        {
            return await _db.KeyTimeToLiveAsync(key);
        }
    }
}
