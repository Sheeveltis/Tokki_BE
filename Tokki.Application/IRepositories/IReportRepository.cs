using Tokki.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tokki.Application.IRepositories
{
    public interface IReportRepository
    {
        Task<Report> AddAsync(Report report);
        Task<Report?> GetByIdAsync(Guid id);
        Task<List<Report>> GetByUserIdAsync(string userId);
        Task<List<Report>> GetUnreadResolvedReportsAsync(string userId); 
        Task UpdateAsync(Report report);
    }
}