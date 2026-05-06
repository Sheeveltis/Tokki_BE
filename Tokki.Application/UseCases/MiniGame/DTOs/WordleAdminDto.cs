using System;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class WordleAdminDto
    {
        public string DailyWordleId { get; set; } = string.Empty;
        public DateOnly GameDate { get; set; }
        public WordleLevel Level { get; set; }
        public string Word { get; set; } = string.Empty;
        public string VocabularyId { get; set; } = string.Empty;
        public string? Definition { get; set; }
        public string? Pronunciation { get; set; }
        public bool IsLocked { get; set; }
    }
}
