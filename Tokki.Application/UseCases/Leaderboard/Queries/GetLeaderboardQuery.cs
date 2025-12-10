using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Leaderboard.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Leaderboard.Queries
{
    public class GetLeaderboardQuery : IRequest<OperationResult<List<LeaderboardItemDto>>>
    {
        public LeaderboardTimeFrame TimeFrame { get; set; } = LeaderboardTimeFrame.AllTime;
        public int Top { get; set; } = 20;
    }
}