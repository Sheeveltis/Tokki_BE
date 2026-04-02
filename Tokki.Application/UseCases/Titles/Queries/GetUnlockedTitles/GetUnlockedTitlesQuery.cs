using MediatR;
using Tokki.Application.Common.Models;
using System.Collections.Generic;

namespace Tokki.Application.UseCases.Titles.Queries.GetUnlockedTitles
{
    public class GetUnlockedTitlesQuery : IRequest<OperationResult<PagedResult<MyTitleResponse>>>
    {
        public string UserId { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
