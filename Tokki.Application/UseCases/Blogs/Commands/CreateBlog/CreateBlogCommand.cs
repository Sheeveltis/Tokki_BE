using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.Commands.CreateBlog
{
    public class CreateBlogCommand : IRequest<OperationResult<string>>
    {
        public string Title { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public BlogStatus Status { get; set; } = BlogStatus.Draft;
        public string CategoryId { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        [JsonIgnore]
        public string CreatedBy { get; set; } = string.Empty;
    }
}
