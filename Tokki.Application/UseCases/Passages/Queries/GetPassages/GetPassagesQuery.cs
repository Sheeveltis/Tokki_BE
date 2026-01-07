using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Passages.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Passages.Queries.GetPassages
{
    public class GetPassagesQuery : IRequest<OperationResult<PagedResult<PassageDto>>>
    {
        public string? SearchTerm { get; set; }
        public PassageMediaType? MediaType { get; set; }

        // null => lấy tất cả status
        public PassageStatus? Status { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
