using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.DTOs
{
    public class BlogDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string ShortDescription { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public BlogStatus Status { get; set; }
        public bool IsOfficial { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public BlogAuthorDTO Author { get; set; } = new();

        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
    }

    public class BlogAuthorDTO
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}
