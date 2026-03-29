using System;
using System.Threading.Tasks;

namespace Tokki.Application.IServices
{
    public interface IRedisService
    {
        Task SetAsync(string key, string value, TimeSpan ttl);
        Task<string?> GetAsync(string key);
        Task DeleteAsync(string key);
        Task<TimeSpan?> GetTtlAsync(string key);
    }
}
