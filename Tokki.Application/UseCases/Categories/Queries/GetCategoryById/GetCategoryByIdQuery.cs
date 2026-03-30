using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Categories.DTOs;

namespace Tokki.Application.UseCases.Categories.Queries.GetCategoryById
{
    public class GetCategoryByIdQuery : IRequest<OperationResult<CategoryDTO>>
    {
        public string Id { get; set; } = string.Empty;
        public GetCategoryByIdQuery(string id)
        {
            Id = id;
        }
    }
}
