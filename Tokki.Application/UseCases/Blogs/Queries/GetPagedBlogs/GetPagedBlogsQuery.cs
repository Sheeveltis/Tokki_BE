using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Blogs.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.Queries.GetPagedBlogs
{
    public class GetPagedBlogsQuery : IRequest<OperationResult<PagedResult<BlogDTO>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? CategoryId { get; set; }
        public string? Tag { get; set; }
        public string? Keyword { get; set; }
        public BlogStatus? Status { get; set; }
    }
}
