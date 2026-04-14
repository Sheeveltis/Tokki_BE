using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Categories.Queries.ExportCategories
{
    public class ExportCategoriesQuery : IRequest<OperationResult<byte[]>>
    {
    }
}
