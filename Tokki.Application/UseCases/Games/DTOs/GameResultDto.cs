using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Games.DTOs
{
    public class GameResultDto
    {
        public string GameMatchSessionId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;

        public int BestScore { get; set; }
        public int LatestScore { get; set; }
        public GameDifficulty GameDifficulty { get; set; }


        public DateTime CreatedAt { get; set; }
    }
}
