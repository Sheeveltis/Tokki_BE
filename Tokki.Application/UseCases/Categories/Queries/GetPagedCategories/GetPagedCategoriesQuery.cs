using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Categories.DTOs;

namespace Tokki.Application.UseCases.Categories.Queries.GetPagedCategories
{
    public class GetPagedCategoriesQuery : IRequest<OperationResult<PagedResult<CategoryDTO>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }
}
