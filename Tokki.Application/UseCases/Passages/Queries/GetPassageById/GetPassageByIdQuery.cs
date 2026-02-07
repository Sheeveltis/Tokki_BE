using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Passages.DTOs;

namespace Tokki.Application.UseCases.Passages.Queries.GetPassageById
{
    public class GetPassageByIdQuery : IRequest<OperationResult<PassageDto>>
    {
        public string PassageId { get; set; } = string.Empty;
    }
}
