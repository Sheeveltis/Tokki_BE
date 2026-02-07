using Tokki.Domain.Entities;

namespace Tokki.Application.IRepositories
{
    public interface IVipPackageRepository
    {
        Task AddAsync(VipPackage package);
        Task UpdateAsync(VipPackage package);
        Task<VipPackage?> GetByIdAsync(string id);
        Task<List<VipPackage>> GetAllAsync(bool includeInactive = false); 
    }
}