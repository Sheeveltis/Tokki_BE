using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Solitaire.DTOs
{
    public class SolitaireResultDto
    {
        public string GameMatchSessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? TitleName { get; set; }
        public string? TitleColorHex { get; set; }
        public string? TitleIconUrl { get; set; }
        public string GameId { get; set; } = string.Empty;

        public int BestScore { get; set; }
        public int LatestScore { get; set; }
        public GameDifficulty GameDifficulty { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
