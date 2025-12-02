using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.DTOs
{
    public class BlogDetailDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public int ViewCount { get; set; }

        public string Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public string AuthorId { get; set; } = string.Empty;

        public string CategoryId { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new List<string>();
    }
}
