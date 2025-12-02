using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.Commands.UpdateBlog
{
    public class UpdateBlogCommand : IRequest<OperationResult<bool>>
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; } 
        public string? ThumbnailUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public BlogStatus Status { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
    }
}
