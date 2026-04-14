using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Blogs.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.Queries.GetMyBlogs
{
    public class GetMyBlogsQuery : IRequest<OperationResult<PagedResult<BlogDTO>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public BlogStatus? Status { get; set; }
        public string? Keyword { get; set; }

        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
