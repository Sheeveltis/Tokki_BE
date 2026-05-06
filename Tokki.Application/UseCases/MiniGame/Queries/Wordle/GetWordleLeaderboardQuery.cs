using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetWordleLeaderboardQuery : IRequest<OperationResult<List<WordleSentenceDto>>>
    {
        public string DailyWordleId { get; set; } = string.Empty;
        public bool IncludePrivate { get; set; } = true;
    }
}
