using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IEnumConfigRepository
    {
        Task<List<EnumConfig>> GetByGroupAsync(EnumGroup groupCode);
        Task<EnumConfig?> GetByValueAsync(EnumGroup groupCode, int value);
        Task<EnumConfig?> GetByKeyAsync(EnumGroup groupCode, string key);
        Task<List<EnumConfig>> GetAllAsync();
        Task<List<EnumConfig>> GetFilteredAsync(EnumGroup? groupCode);
        Task<EnumConfig?> FirstOrDefaultAsync(Expression<Func<EnumConfig, bool>> predicate, CancellationToken cancellationToken = default);
        Task AddAsync(EnumConfig config);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
