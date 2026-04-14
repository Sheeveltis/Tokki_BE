using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Blogs.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Blogs.Queries.GetMyBlogs
{
    public class GetMyBlogsQueryHandler : IRequestHandler<GetMyBlogsQuery, OperationResult<PagedResult<BlogDTO>>>
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IAccountRepository _accountRepository;

        public GetMyBlogsQueryHandler(IBlogRepository blogRepository, IAccountRepository accountRepository)
        {
            _blogRepository = blogRepository;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<PagedResult<BlogDTO>>> Handle(GetMyBlogsQuery request, CancellationToken cancellationToken)
        {
            // Lấy danh sách blog của User
            var pagedEntities = await _blogRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                null, // categoryId
                null, // tag
                request.Keyword,
                request.Status,
                null, // isOfficial
                request.UserId,
                cancellationToken);

            // Vì là bài viết của chính họ nên ta có thể lấy info author nhanh hơn hoặc map thẳng
            var author = await _accountRepository.GetBasicInfoAsync(request.UserId);

            var dtoItems = pagedEntities.Items.Select(blog => new BlogDTO
            {
                Id = blog.Id,
                Title = blog.Title,
                Slug = blog.Slug,
                ThumbnailUrl = blog.ThumbnailUrl,
                ShortDescription = blog.ShortDescription,
                ViewCount = blog.ViewCount,
                Status = blog.Status,
                IsOfficial = blog.IsOfficial,
                CreatedAt = blog.CreatedAt,
                CategoryId = blog.CategoryId,
                CategoryName = blog.Category?.Name ?? "Không xác định",
                Tags = blog.Tags.Select(t => t.Name).ToList(),
                Author = new BlogAuthorDTO
                {
                    Id = request.UserId,
                    FullName = author?.FullName ?? "Người dùng",
                    AvatarUrl = author?.AvatarUrl
                }
            }).ToList();

            var pagedResultDto = new PagedResult<BlogDTO>(
                dtoItems,
                pagedEntities.TotalCount,
                pagedEntities.PageNumber,
                pagedEntities.PageSize
            );

            return OperationResult<PagedResult<BlogDTO>>.Success(pagedResultDto, 200);
        }
    }
}
