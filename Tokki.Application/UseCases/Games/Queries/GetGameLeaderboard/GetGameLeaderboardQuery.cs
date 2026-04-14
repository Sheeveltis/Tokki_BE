using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Games.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.Queries.GetGameLeaderboard
{
    public class GetGameLeaderboardQuery : IRequest<OperationResult<PagedResult<GameLeaderboardDto>>>
    {
        public GameType? GameType { get; set; }
        public GameDifficulty? GameDifficulty { get; set; }
        public string? TopicId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
