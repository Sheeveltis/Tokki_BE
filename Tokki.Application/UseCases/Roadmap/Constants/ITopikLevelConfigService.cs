using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Roadmap.Constants
{
    public interface ITopikLevelConfigService
    {
        Task<List<TopikLevelConfigDto>> GetAllAsync(CancellationToken ct = default);
        Task<TopikLevelConfigDto?> GetByLevelAsync(TargetAimLevel level, CancellationToken ct = default);
    }
}