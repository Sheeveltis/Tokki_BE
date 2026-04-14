using System;
using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class GameMatchSession
    {
        [Key]
        public string GameMatchSessionId { get; set; } = null!;

        public string UserId { get; set; } = null!;

        // Game nào (Matching card, Solitaire, ...)
        public GameType GameType { get; set; }

        // Topic nào (null nếu là game không cần topic, ví dụ Solitaire)
        public string? TopicId { get; set; }
        public DateTime CreatedAt { get; set; }


        // Điểm cao nhất user từng đạt với Game + Topic này
        public int BestScore { get; set; }

        // Điểm của lần chơi gần nhất (current)
        public int LatestScore { get; set; }

        // Thời gian chơi (giây/phút tùy quy ước, decimal để có phần lẻ)
        public GameDifficulty GameDifficulty { get; set; }

        // Navigation
        public Topic? Topic { get; set; }
        public Account User { get; set; } = null!;
    }
}
