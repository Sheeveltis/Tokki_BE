using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Categories.DTOs;

namespace Tokki.Application.UseCases.Categories.Queries.GetPagedCategories
{
    public class GetPagedCategoriesQueryHandler : IRequestHandler<GetPagedCategoriesQuery, OperationResult<PagedResult<CategoryDTO>>>
    {
        private readonly ICategoryRepository _repo;

        public GetPagedCategoriesQueryHandler(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<PagedResult<CategoryDTO>>> Handle(GetPagedCategoriesQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _repo.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                cancellationToken);

            var dtos = items.Select(c => new CategoryDTO
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                CreatedAt = c.CreatedAt
            }).ToList();

            var pagedResult = new PagedResult<CategoryDTO>(dtos, totalCount, request.PageNumber, request.PageSize);
            return OperationResult<PagedResult<CategoryDTO>>.Success(pagedResult, 200, OperationMessages.GetSuccess("Danh sách danh mục"));
        }
    }
}
