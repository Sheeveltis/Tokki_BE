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
            var totalPending = await _context.Blogs.CountAsync(b => b.Status == BlogStatus.PendingApproval, cancellationToken);
 
            // Internal blogs: Created by Admin, Staff, or Moderator (Only Published)
            var internalRoles = new[] { AccountRole.Admin, AccountRole.Staff, AccountRole.Moderator };
 
            var blogStats = await (from b in _context.Blogs
                                   join a in _context.Accounts on b.AuthorId equals a.UserId
                                   where b.Status == BlogStatus.Published
                                   select new { a.Role })
                                   .ToListAsync(cancellationToken);
 
            int internalCount = blogStats.Count(x => internalRoles.Contains(x.Role));
            int communityCount = totalPublished - internalCount;
 
            return new DashboardStatDTO
            {
                TotalBlogs = totalBlogs,
                TotalViews = totalViews,
                TotalPublished = totalPublished,
                TotalInternalBlogs = internalCount,
                TotalCommunityBlogs = communityCount,
                TotalPendingBlogs = totalPending
            };
        }
 
        public async Task<List<TopBlogDTO>> GetTopViewedBlogsAsync(int count, CancellationToken cancellationToken)
        {
            return await (from b in _context.Blogs
                          join a in _context.Accounts on b.AuthorId equals a.UserId
                          where b.Status == BlogStatus.Published
                          orderby b.ViewCount descending
                          select new TopBlogDTO
                          {
                              Id = b.Id,
                              Title = b.Title,
                              Slug = b.Slug,
                              ViewCount = b.ViewCount,
                              AuthorId = b.AuthorId,
                              AuthorName = a.FullName,
                              AuthorAvatarUrl = a.AvatarUrl,
                              CreatedAt = b.CreatedAt
                          })
                          .Take(count)
                          .AsNoTracking()
                          .ToListAsync(cancellationToken);
        }
 
        public async Task<List<TopAuthorDTO>> GetTopAuthorsAsync(int count, AuthorSource source, CancellationToken cancellationToken)
        {
            var internalRoles = new[] { AccountRole.Admin, AccountRole.Staff, AccountRole.Moderator };
 
            var query = _context.Blogs
                .AsNoTracking()
                .Where(b => b.Status == BlogStatus.Published);
 
            // Join with Accounts to filter by source if needed
            if (source != AuthorSource.All)
            {
                if (source == AuthorSource.Internal)
                {
                    query = from b in query
                            join a in _context.Accounts on b.AuthorId equals a.UserId
                            where internalRoles.Contains(a.Role)
                            select b;
                }
                else // Community
                {
                    query = from b in query
                            join a in _context.Accounts on b.AuthorId equals a.UserId
                            where !internalRoles.Contains(a.Role)
                            select b;
                }
            }
 
            var topAuthorsData = await query
                .GroupBy(b => b.AuthorId)
                .Select(g => new
                {
                    AuthorId = g.Key,
                    BlogCount = g.Count(),
                    TotalViews = g.Sum(b => (long)b.ViewCount)
                })
                .OrderByDescending(x => x.TotalViews)
                .Take(count)
                .ToListAsync(cancellationToken);
 
            var authorIds = topAuthorsData.Select(x => x.AuthorId).ToList();
            var accounts = await _context.Accounts
                .Where(a => authorIds.Contains(a.UserId))
                .ToDictionaryAsync(a => a.UserId, a => a, cancellationToken);
 
            return topAuthorsData.Select(x => new TopAuthorDTO
            {
                AuthorId = x.AuthorId,
                FullName = accounts.ContainsKey(x.AuthorId) ? accounts[x.AuthorId].FullName : "Deleted User",
                AvatarUrl = accounts.ContainsKey(x.AuthorId) ? accounts[x.AuthorId].AvatarUrl : null,
                BlogCount = x.BlogCount,
                TotalViews = x.TotalViews
            }).ToList();
        }
    }
}