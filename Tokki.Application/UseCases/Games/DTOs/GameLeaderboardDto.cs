using System;

using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.DTOs
{
    public class GameLeaderboardDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? TitleName { get; set; }
        public string? TitleColorHex { get; set; }
        public string? TitleIconUrl { get; set; }
        public GameType GameType { get; set; }
        public GameDifficulty GameDifficulty { get; set; }
        public string? TopicId { get; set; }
        public int BestScore { get; set; }
    }
}
