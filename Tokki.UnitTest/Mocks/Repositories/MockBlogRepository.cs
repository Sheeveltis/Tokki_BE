using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.UnitTest.Mocks.Repositories
{
    public static class MockBlogRepository
    {
        public static Mock<IBlogRepository> GetMock(List<Blog>? predefinedBlogs = null)
        {
            var mockRepo = new Mock<IBlogRepository>();
            var blogs = predefinedBlogs ?? new List<Blog>();

            mockRepo.Setup(x => x.GetPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<BlogStatus?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int pageNumber, int pageSize, string? categoryId, BlogStatus? status, CancellationToken token) =>
                {
                    var query = blogs.AsQueryable();

                    if (!string.IsNullOrEmpty(categoryId))
                        query = query.Where(b => b.CategoryId == categoryId);
                    
                    if (status.HasValue)
                        query = query.Where(b => b.Status == status.Value);

                    var totalCount = query.Count();
                    var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                    return PagedResult<Blog>.Create(items, totalCount, pageNumber, pageSize);
                });

            mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => blogs.FirstOrDefault(b => b.Id == id));

            mockRepo.Setup(x => x.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => blogs.Any(b => b.Id == id));

            mockRepo.Setup(x => x.CategoryExistsAsync(It.IsAny<string>()))
                .ReturnsAsync((string categoryId) => categoryId == "VALID-CAT");

            mockRepo.Setup(x => x.GetOrCreateTagsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync((List<string> tagNames) => 
                {
                    return tagNames.Select(t => new Tag { Id = Guid.NewGuid().ToString(), Name = t }).ToList();
                });

            mockRepo.Setup(x => x.AddAsync(It.IsAny<Blog>()))
                .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.UpdateAsync(It.IsAny<Blog>()))
                .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.DeleteAsync(It.IsAny<Blog>()))
                .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            mockRepo.Setup(x => x.IncreaseViewCountAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => 
                {
                    var blog = blogs.FirstOrDefault(b => b.Id == id);
                    if (blog != null)
                    {
                        blog.ViewCount++;
                        return true;
                    }
                    return false;
                });

            return mockRepo;
        }

        public static Blog GetSampleBlog(string id = "BLOG-001", BlogStatus status = BlogStatus.Draft) => new()
        {
            Id = id,
            Title = "Sample Blog",
            Slug = "sample-blog",
            Content = "This is a sample blog content",
            ShortDescription = "Short desc",
            CategoryId = "VALID-CAT",
            Status = status,
            AuthorId = "USER-001",
            ViewCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
