using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;
using Tokki.Application.UseCases.Games.DTOs;
namespace Tokki.Application.UseCases.Games.Queries.GetAllGamesForUser
{
    public class GetAllGamesForUserQuery : IRequest<OperationResult<PagedResult<GameForUserDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }

        // Nếu muốn lọc theo loại game (MatchingCard, TypingPractice...)
        public GameType? GameType { get; set; }
    }
}
