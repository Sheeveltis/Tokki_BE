using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.Alphabet.DTOs;

namespace Tokki.Application.UseCases.Alphabet.Queries
{
    public class GetAlphabetPaginatedQuery : IRequest<OperationResult<PagedResult<AlphabetDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public AlphabetType? Type { get; set; }
        public bool? IsActive { get; set; }
    }
}
