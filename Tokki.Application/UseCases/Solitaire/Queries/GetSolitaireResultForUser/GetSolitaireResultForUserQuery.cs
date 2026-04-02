using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Solitaire.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Solitaire.Queries.GetSolitaireResultForUser
{
    public class GetSolitaireResultForUserQuery : IRequest<OperationResult<SolitaireResultDto?>>
    {
        public string GameId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public GameDifficulty GameDifficulty { get; set; }
    }
}
