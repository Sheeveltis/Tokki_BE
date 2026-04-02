using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Helpers;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Accounts.DTOs;
using Tokki.Application.UseCases.Blogs.DTOs;

namespace Tokki.Application.UseCases.Blogs.Queries.GetPagedBlogs
{
    public class GetPagedBlogsQueryHandler : IRequestHandler<GetPagedBlogsQuery, OperationResult<PagedResult<BlogDTO>>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IAccountRepository _accountRepository; 

        public GetPagedBlogsQueryHandler(
            IBlogRepository blogRepository,
            IAccountRepository accountRepository)
        {
            _blogRepository = blogRepository;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<PagedResult<BlogDTO>>> Handle(GetPagedBlogsQuery request, CancellationToken cancellationToken)
        {
            var pagedEntities = await _blogRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.CategoryId,
                request.Status,
                cancellationToken);

            var distinctAuthorIds = pagedEntities.Items
                                    .Select(b => b.AuthorId)
                                    .Where(id => !string.IsNullOrEmpty(id))
                                    .Distinct()
                                    .ToList();

            var authorMap = await _accountRepository.GetBasicInfosAsync(distinctAuthorIds);

            var dtoItems = pagedEntities.Items.Select(blog =>
            {
                var authorInfo = authorMap.ContainsKey(blog.AuthorId) ? authorMap[blog.AuthorId] : null;

                return new BlogDTO
                {
                    Id = blog.Id,
                    Title = blog.Title,
                    Slug = blog.Slug,
                    ThumbnailUrl = blog.ThumbnailUrl,
                    ShortDescription = blog.ShortDescription,
                    ViewCount = blog.ViewCount,
                    Status = blog.Status,
                    CreatedAt = blog.CreatedAt,

                    CategoryId = blog.CategoryId,
                    CategoryName = blog.Category?.Name ?? "Không xác định",
                    Tags = blog.Tags.Select(t => t.Name).ToList(),

                    Author = new BlogAuthorDTO
                    {
                        Id = blog.AuthorId,
                        FullName = authorInfo?.FullName ?? "Người dùng ẩn danh",
                        AvatarUrl = authorInfo?.AvatarUrl
                    }
                };
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
