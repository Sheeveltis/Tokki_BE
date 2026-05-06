using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetWordleLeaderboardQuery : IRequest<OperationResult<PagedResult<WordleSentenceDto>>>
    {
        public string DailyWordleId { get; set; } = string.Empty;
        public bool IncludePrivate { get; set; } = true;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
