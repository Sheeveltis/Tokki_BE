using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Mappings;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class BlogRepository : IBlogRepository
    {
        private readonly TokkiDbContext _context;
        private readonly IIdGeneratorService _idGeneratorService;

        public BlogRepository(TokkiDbContext context, IIdGeneratorService idGeneratorService)
        {
            _context = context;
            _idGeneratorService = idGeneratorService;
        }

        public async Task AddAsync(Blog blog)
        {
            await _context.Blogs.AddAsync(blog);
        }

        public async Task<Blog?> GetByIdAsync(string id)
        {
            return await _context.Blogs
                .Include(b => b.Category) 
                .Include(b => b.Tags)     
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<PagedResult<Blog>> GetPagedAsync(int pageNumber, int pageSize, string? categoryId,
            string? tag, string? keyword, BlogStatus? status, bool? isOfficial, string? authorId, CancellationToken cancellationToken)
        {
            var query = _context.Blogs
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Tags)
                .OrderByDescending(b => b.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(authorId))
            {
                query = query.Where(b => b.AuthorId == authorId);
            }

            if (isOfficial.HasValue)
            {
                query = query.Where(b => b.IsOfficial == isOfficial.Value);
            }

            if (!string.IsNullOrWhiteSpace(categoryId))
            {
                query = query.Where(b => b.CategoryId == categoryId);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                query = query.Where(b => b.Tags.Any(t => t.Name.ToLower() == tag.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string lowerKeyword = keyword.ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(lowerKeyword) || 
                                         b.ShortDescription.ToLower().Contains(lowerKeyword));
            }

            if (status.HasValue)
            {
                query = query.Where(b => b.Status == status.Value);
            }
            else
            {
                query = query.Where(b => b.Status != BlogStatus.Hidden);
            }

            return await query.ToPagedListAsync(pageNumber, pageSize);
        }

        public Task UpdateAsync(Blog blog)
        {
            _context.Blogs.Update(blog);
            return Task.CompletedTask; 
        }

        public Task DeleteAsync(Blog blog)
        {
            blog.Status = BlogStatus.Hidden;
            blog.UpdatedAt = DateTimeOffset.UtcNow;
            _context.Blogs.Update(blog);
            return Task.CompletedTask;
        }

        public async Task<bool> CategoryExistsAsync(string categoryId)
        {
            return await _context.Categories.AnyAsync(c => c.Id == categoryId);
        }

        public async Task<ICollection<Tag>> GetOrCreateTagsAsync(List<string> tagNames, bool isVerified = false)
        {
            if (tagNames == null || !tagNames.Any())
            {
                return new List<Tag>();
            }

            var cleanTagNames = tagNames
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!cleanTagNames.Any()) return new List<Tag>();

            var existingTags = await _context.Tags
                .Where(t => cleanTagNames.Contains(t.Name))
                .ToListAsync();

            var existingTagNames = existingTags.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newTagsToCreate = new List<Tag>();
            foreach (var name in cleanTagNames)
            {
                if (!existingTagNames.Contains(name))
                {
                    var newTag = new Tag
                    {
                        Id = _idGeneratorService.GenerateCustom(10), 
                        Name = name,
                        IsVerified = isVerified
                    };
                    newTagsToCreate.Add(newTag);
                }
            }

            if (newTagsToCreate.Any())
            {
                await _context.Tags.AddRangeAsync(newTagsToCreate);
            }

            var finalTags = new List<Tag>();
            finalTags.AddRange(existingTags);
            finalTags.AddRange(newTagsToCreate);

            return finalTags;
        }

        public Task DeleteTagAsync(Tag tag)
        {
            _context.Tags.Remove(tag);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(string blogId)
        {
            return await _context.Blogs
                                 .AsNoTracking() 
                                 .AnyAsync(b => b.Id == blogId && b.Status != BlogStatus.Hidden);
        }

        public async Task<bool> IncreaseViewCountAsync(string blogId)
        {
            var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE Blogs SET ViewCount = ViewCount + 1 WHERE Id = {blogId}"
            );
            return rowsAffected > 0;
        }

        public async Task AddRangeAsync(IEnumerable<Blog> blogs, CancellationToken cancellationToken = default)
        {
            await _context.Blogs.AddRangeAsync(blogs, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task<IEnumerable<Blog>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Blogs
                .AsNoTracking()
                .Include(b => b.Category)
                .Include(b => b.Tags)
                .Where(b => b.Status != BlogStatus.Hidden)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
