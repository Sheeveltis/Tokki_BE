using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Solitaire.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Solitaire.Queries.GetSolitaireResultsForAllUsers
{
    public class GetSolitaireResultsForAllUsersQuery : IRequest<OperationResult<PagedResult<SolitaireResultDto>>>
    {
        public string GameId { get; set; } = string.Empty;
        public GameDifficulty GameDifficulty { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
