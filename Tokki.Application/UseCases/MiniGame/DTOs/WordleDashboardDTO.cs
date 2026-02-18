using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class WordleDashboardDTO
    {
        public DateOnly Date { get; set; }
        public List<WordleLevelStatus> Levels { get; set; } = new();
    }

    public class WordleLevelStatus
    {
        public string DailyWordleId { get; set; }
        public WordleLevel Level { get; set; } 
        public int WordLength { get; set; } 
        public bool IsWon { get; set; }
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 6;
        public List<string> Guesses { get; set; } = new(); 
    }
}
