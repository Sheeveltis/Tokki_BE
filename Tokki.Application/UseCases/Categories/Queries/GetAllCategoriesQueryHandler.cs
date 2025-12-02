using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Categories.DTOs;

namespace Tokki.Application.UseCases.Categories.Queries
{
    public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, OperationResult<IEnumerable<CategoryDTO>>>
    {
        private readonly ICategoryRepository _repo;

        public GetAllCategoriesQueryHandler(ICategoryRepository repo)
        {
            _repo = repo;
        }

        public async Task<OperationResult<IEnumerable<CategoryDTO>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await _repo.GetAllAsync(cancellationToken);

            var response = categories.Select(c => new CategoryDTO
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                CreatedAt = c.CreatedAt
            });

            return OperationResult<IEnumerable<CategoryDTO>>.Success(response, 200, OperationMessages.GetSuccess("Danh sách danh mục"));
        }
    }
}
