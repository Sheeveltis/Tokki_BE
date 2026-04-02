using Microsoft.Extensions.Caching.Memory;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Roadmap.DTOs;

namespace Tokki.Infrastructure.Services
{
    public class RoadmapProgressService : IRoadmapProgressService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(10);

        public RoadmapProgressService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void Set(string jobId, RoadmapProgressState state)
            => _cache.Set(CacheKey(jobId), state, Expiry);

        public RoadmapProgressState? Get(string jobId)
        {
            _cache.TryGetValue(CacheKey(jobId), out RoadmapProgressState? state);
            return state;
        }

        public void Remove(string jobId)
            => _cache.Remove(CacheKey(jobId));

        private static string CacheKey(string jobId) => $"roadmap_progress:{jobId}";
    }
}