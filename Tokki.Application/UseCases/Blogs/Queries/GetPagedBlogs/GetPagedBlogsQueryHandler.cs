using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Blogs.DTOs;

namespace Tokki.Application.UseCases.Blogs.Queries.GetPagedBlogs
{
    public class GetPagedBlogsQueryHandler : IRequestHandler<GetPagedBlogsQuery, OperationResult<PagedResult<BlogDTO>>>
    {
        private readonly IBlogRepository _blogRepository;

        public GetPagedBlogsQueryHandler(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<OperationResult<PagedResult<BlogDTO>>> Handle(GetPagedBlogsQuery request, CancellationToken cancellationToken)
        {
            var pagedEntities = await _blogRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.CategoryId,
                request.Status,
                cancellationToken);

            var dtoItems = pagedEntities.Items.Select(blog => new BlogDTO
            {
                Id = blog.Id,
                Title = blog.Title,
                Slug = blog.Slug,
                ThumbnailUrl = blog.ThumbnailUrl,
                ShortDescription = blog.ShortDescription,
                ViewCount = blog.ViewCount,
                Status = blog.Status.GetDescription(),
                CreatedAt = blog.CreatedAt,
                AuthorId = blog.AuthorId,
                CategoryId = blog.CategoryId,
                CategoryName = blog.Category?.Name ?? "Không xác định", 
                Tags = blog.Tags.Select(t => t.Name).ToList()
            }).ToList();

            var pagedResultDto = new PagedResult<BlogDTO>(
                dtoItems,
                pagedEntities.TotalCount,
                pagedEntities.PageNumber,
                pagedEntities.PageSize
            );

            return OperationResult<PagedResult<BlogDTO>>.Success(
                pagedResultDto,
                200,
                OperationMessages.GetSuccess("Danh sách bài viết")
            );
        }
    }
}
