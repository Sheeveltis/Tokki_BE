using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.UseCases.StatisticBlog.DTOs;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data; 

namespace Tokki.Infrastructure.Repositories
{
    public class StatisticBlogRepository : IStatisticBlogRepository
    {
        private readonly TokkiDbContext _context;

        public StatisticBlogRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardStatDTO> GetDashboardStatsAsync(CancellationToken cancellationToken)
        {
            var totalBlogs = await _context.Blogs.CountAsync(cancellationToken);

            var totalViews = await _context.Blogs.SumAsync(b => (long)b.ViewCount, cancellationToken);

            var totalPublished = await _context.Blogs.CountAsync(b => b.Status == BlogStatus.Published, cancellationToken);

            return new DashboardStatDTO
            {
                TotalBlogs = totalBlogs,
                TotalViews = totalViews,
                TotalPublished = totalPublished
            };
        }

        public async Task<List<TopBlogDTO>> GetTopViewedBlogsAsync(int count, CancellationToken cancellationToken)
        {
            return await _context.Blogs
                .AsNoTracking() 
                .Where(b => b.Status == BlogStatus.Published) 
                .OrderByDescending(b => b.ViewCount) 
                .Take(count) 
                .Select(b => new TopBlogDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    Slug = b.Slug,
                    ViewCount = b.ViewCount,
                    AuthorId = b.AuthorId,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TopAuthorDTO>> GetTopAuthorsAsync(int count, CancellationToken cancellationToken)
        {
            return await _context.Blogs
                .AsNoTracking()
                .Where(b => b.Status == BlogStatus.Published)
                .GroupBy(b => b.AuthorId) 
                .Select(g => new TopAuthorDTO
                {
                    AuthorId = g.Key,
                    BlogCount = g.Count(), 
                    TotalViews = g.Sum(b => (long)b.ViewCount) 
                })
                .OrderByDescending(x => x.TotalViews) 
                .Take(count) 
                .ToListAsync(cancellationToken);
        }
    }
}