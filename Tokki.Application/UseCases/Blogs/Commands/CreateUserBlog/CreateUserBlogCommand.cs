using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Blogs.Commands.CreateUserBlog
{
    public class CreateUserBlogCommand : IRequest<OperationResult<string>>
    {
        public string Title { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        [JsonIgnore]
        public string CreatedBy { get; set; } = string.Empty;
    }
}
