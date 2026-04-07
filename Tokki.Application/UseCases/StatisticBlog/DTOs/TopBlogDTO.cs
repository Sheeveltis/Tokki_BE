using System;
 
namespace Tokki.Application.UseCases.StatisticBlog.DTOs
{
    public class TopBlogDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorAvatarUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
