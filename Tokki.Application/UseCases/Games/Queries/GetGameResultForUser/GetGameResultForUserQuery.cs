using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Games.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.Queries.GetGameResultForUser
{
    public class GetGameResultForUserQuery : IRequest<OperationResult<PagedResult<GameResultDto>>>
    {
        public GameType? GameType { get; set; }
        public string? TopicId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public GameDifficulty? GameDifficulty { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

    }
}
