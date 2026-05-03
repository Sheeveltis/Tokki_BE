using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface ISystemConfigRepository
    {
        Task<SystemConfig?> GetByKeyAsync(string key);
        Task<List<SystemConfig>> GetAllAsync();
        Task AddAsync(SystemConfig config);
        Task AddRangeAsync(IEnumerable<SystemConfig> configs);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<(List<SystemConfig> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, Tokki.Domain.Enums.SystemConfigType? configType = null, string? searchTerm = null, bool? isActive = null);
        Task<string?> GetValueByKeyAsync(string key);
        Task<SystemConfig?> FirstOrDefaultAsync(Expression<Func<SystemConfig, bool>> predicate, CancellationToken cancellationToken = default);
    }
}