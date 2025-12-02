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

namespace Tokki.Application.UseCases.Blogs.Queries
{
    public class GetBlogByIdQueryHandler : IRequestHandler<GetBlogByIdQuery, OperationResult<BlogDetailDTO>>
    {
        private readonly IBlogRepository _blogRepository;

        public GetBlogByIdQueryHandler(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<OperationResult<BlogDetailDTO>> Handle(GetBlogByIdQuery request, CancellationToken cancellationToken)
        {
            var blog = await _blogRepository.GetByIdAsync(request.Id);

            if (blog == null)
            {
                return OperationResult<BlogDetailDTO>.Failure(AppErrors.BlogNotFound, 404, OperationMessages.NotFound("Bài viết"));
            }

            var response = new BlogDetailDTO
            {
                Id = blog.Id,
                Title = blog.Title,
                Slug = blog.Slug,
                ThumbnailUrl = blog.ThumbnailUrl ?? string.Empty,
                Content = blog.Content,
                ShortDescription = blog.ShortDescription,
                ViewCount = blog.ViewCount,
                Status = blog.Status.GetDescription(),
                CreatedAt = blog.CreatedAt,
                AuthorId = blog.AuthorId,
                CategoryId = blog.CategoryId,
                CategoryName = blog.Category?.Name ?? "N/A", // Null check an toàn
                Tags = blog.Tags.Select(t => t.Name).ToList()
            };

            return OperationResult<BlogDetailDTO>.Success(response, 200, OperationMessages.GetSuccess("Bài viết"));
        }
    }
}
