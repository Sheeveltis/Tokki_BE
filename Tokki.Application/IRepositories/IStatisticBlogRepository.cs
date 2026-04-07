using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.UseCases.StatisticBlog.DTOs;

namespace Tokki.Application.IRepositories
{
    public interface IStatisticBlogRepository
    {
        Task<DashboardStatDTO> GetDashboardStatsAsync(CancellationToken cancellationToken);
        Task<List<TopBlogDTO>> GetTopViewedBlogsAsync(int count, CancellationToken cancellationToken);
        Task<List<TopAuthorDTO>> GetTopAuthorsAsync(int count, Tokki.Domain.Enums.AuthorSource source, CancellationToken cancellationToken);
    }
}
