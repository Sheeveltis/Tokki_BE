using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Games.DTOs;

namespace Tokki.Application.UseCases.Games.Queries.GetGameResultsForAllUsers
{
    public class GetGameResultsForAllUsersQuery : IRequest<OperationResult<PagedResult<GameResultDto>>>
    {
        public string GameId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
