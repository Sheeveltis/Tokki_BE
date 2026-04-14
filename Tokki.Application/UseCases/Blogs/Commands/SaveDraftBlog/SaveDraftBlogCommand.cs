using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Blogs.Commands.SaveDraftBlog
{
    public class SaveDraftBlogCommand : IRequest<OperationResult<string>>
    {
        public string? Id { get; set; } // Nếu có Id là update nháp, nếu không là tạo mới
        public string Title { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();

        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
