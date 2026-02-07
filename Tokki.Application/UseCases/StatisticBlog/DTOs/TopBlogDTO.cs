using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.StatisticBlog.DTOs
{
    public class TopBlogDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }

}
