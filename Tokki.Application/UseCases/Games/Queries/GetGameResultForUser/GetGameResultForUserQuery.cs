using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Games.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.Queries.GetGameResultForUser
{
    public class GetGameResultForUserQuery : IRequest<OperationResult<GameResultDto?>>
    {
        public string GameId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public GameDifficulty GameDifficulty { get; set; }

    }
}
