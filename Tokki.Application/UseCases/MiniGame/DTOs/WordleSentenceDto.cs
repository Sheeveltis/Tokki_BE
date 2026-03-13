using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Application.UseCases.MiniGame.DTOs
{
    public class WordleSentenceDto
    {
        public string SubmissionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string? UserId { get; set; } 
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public string SentenceContent { get; set; } = string.Empty;
        public int AiScore { get; set; }

        public bool IsPublic { get; set; }
        public bool IsAnonymous { get; set; }

        public int LikeCount { get; set; }
        public bool IsLiked { get; set; } 
        public string? TitleName { get; set; }
        public string? TitleColorHex { get; set; }
        public string? TitleIconUrl { get; set; }
    }
}
