using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.MiniGame.DTOs;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetWordlePlayersQuery : IRequest<OperationResult<List<WordlePlayerProgressDto>>>
    {
        public string DailyWordleId { get; set; } = string.Empty;
    }
}
