using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Games.DTOs;

namespace Tokki.Application.UseCases.Games.Queries.GetGameResultForUser
{
    public class GetGameResultForUserQuery : IRequest<OperationResult<GameResultDto?>>
    {
        public string GameId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }
}
