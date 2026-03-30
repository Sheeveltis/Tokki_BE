using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Categories.DTOs;

namespace Tokki.Application.UseCases.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, OperationResult<CategoryDTO>>
    {
        private readonly ICategoryRepository _repo;

        public GetCategoryByIdQueryHandler(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<CategoryDTO>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            var category = await _repo.GetByIdAsync(request.Id, cancellationToken);
            if (category == null)
            {
                return OperationResult<CategoryDTO>.Failure(AppErrors.CategoryNotFound, 404, OperationMessages.GetFail("Danh mục"));
            }

            var dto = new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                CreatedAt = category.CreatedAt
            };

            return OperationResult<CategoryDTO>.Success(dto, 200, OperationMessages.GetSuccess("Danh mục"));
        }
    }
}
