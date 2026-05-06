using System;
using System.Collections.Generic;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class WordlePlayerProgressDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int AttemptCount { get; set; }
        public bool IsWon { get; set; }
        public List<string> Guesses { get; set; } = new List<string>();
        public DateTime LastActivity { get; set; }
    }
}
