using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Titles.Queries.ExportTitles
{
    public class ExportTitlesQuery : IRequest<OperationResult<byte[]>>
    {
    }
}
