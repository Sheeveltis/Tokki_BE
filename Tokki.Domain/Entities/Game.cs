using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Tokki.Domain.Entities
{
    public class Game
    {
        [Key]
        public string  GameId { get; set; }

        public string GameName { get; set; } = null!;

        // Ví dụ: "MATCHING_CARD", "QUIZ", ...
        public GameType  GameType { get; set; } 

        public bool IsVip { get; set; }

        public string? CreatedBy { get; set; }

        public GameStatus Status { get; set; } 

        public DateTime CreatedAt { get; set; }
        public string ImgUrl { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation

        public ICollection<GameMatchSession> PlaySessions { get; set; } = new List<GameMatchSession>();
    }

}
