using System.Text.Json;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Roadmap.Constants;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Infrastructure.Services.Roadmap
{
    public class TopikLevelConfigService : ITopikLevelConfigService
    {
        private readonly ISystemConfigRepository _repo;
        private const string KeyPrefix = "TOPIK_LEVEL_";

        public TopikLevelConfigService(ISystemConfigRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<TopikLevelConfigDto>> GetAllAsync(CancellationToken ct = default)
        {
            var all = await _repo.GetAllAsync();

            return all
                .Where(x => x.IsActive
                         && x.Key.StartsWith(KeyPrefix)
                         && x.ConfigType == SystemConfigType.Learning
                         && !string.IsNullOrEmpty(x.Value))
                .Select(x => ParseSafe(x.Value!))
                .Where(dto => dto != null)
                .Cast<TopikLevelConfigDto>()
                .OrderBy(x => x.TargetAimLevel)
                .ToList();
        }

        public async Task<TopikLevelConfigDto?> GetByLevelAsync(TargetAimLevel level, CancellationToken ct = default)
        {
            var key = KeyPrefix + (int)level;
            var config = await _repo.FirstOrDefaultAsync(
                x => x.Key == key && x.IsActive && x.ConfigType == SystemConfigType.Learning, ct);

            if (config?.Value == null) return null;
            return ParseSafe(config.Value);
        }

        private static TopikLevelConfigDto? ParseSafe(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<TopikLevelConfigDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }
    }
}