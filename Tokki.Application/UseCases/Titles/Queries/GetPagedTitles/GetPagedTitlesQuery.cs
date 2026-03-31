using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Titles.Queries.GetPagedTitles
{
    public class GetPagedTitlesQuery : IRequest<OperationResult<PagedResult<Title>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public TitleStatus? Status { get; set; }
    }
}
