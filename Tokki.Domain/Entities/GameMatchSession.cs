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
        public string GameId { get; set; } = null!;

        // Topic nào
        public string TopicId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }


        // Điểm cao nhất user từng đạt với Game + Topic này
        public int BestScore { get; set; }

        // Điểm của lần chơi gần nhất (current)
        public int LatestScore { get; set; }

        // Thời gian chơi (giây/phút tùy quy ước, decimal để có phần lẻ)

        // Nếu bạn vẫn muốn lưu exp thì giữ lại, còn không thì xóa luôn property này

        // Navigation
        public Game Game { get; set; } = null!;
        public Topic Topic { get; set; } = null!;
        public Account User { get; set; } = null!;
    }
}
