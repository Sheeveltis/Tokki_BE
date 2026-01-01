using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class GameMatchSession
    {
       [Key]
        public string GameMatchSessionId { get; set; }

        public string  UserId { get; set; }

        public string GameId { get; set; }

        public string GameMatchId { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime EndedAt { get; set; }

        public int Score { get; set; }

        public int ExpGained { get; set; }

        public int PlayedDuration { get; set; }

        // "COMPLETED", "ABORTED"...
        public GameResultStatus GameResultStatus { get; set; } 

        // Navigation
        public Game Game { get; set; } = null!;

        public GameMatch GameMatch { get; set; } = null!;

        public Account User { get; set; } = null!;
    }

}
