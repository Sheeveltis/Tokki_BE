using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface ITopikLevelConfigRepository
    {
        Task<List<TopikLevelConfig>> GetAllAsync();
        Task<(List<TopikLevelConfig> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchText = null, int? examGroup = null, bool? isActive = null);
        Task<TopikLevelConfig?> GetByIdAsync(int id);
        Task<TopikLevelConfig?> FirstOrDefaultAsync(Expression<Func<TopikLevelConfig, bool>> predicate, CancellationToken cancellationToken = default);
        Task AddAsync(TopikLevelConfig config);
        void Update(TopikLevelConfig config);
        void Delete(TopikLevelConfig config);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
