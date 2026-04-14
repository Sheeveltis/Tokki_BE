using MediatR;
using Microsoft.AspNetCore.Http;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Categories.Commands.ImportCategories
{
    public class ImportCategoriesCommand : IRequest<OperationResult<bool>>
    {
        public IFormFile File { get; set; } = default!;
    }
}
