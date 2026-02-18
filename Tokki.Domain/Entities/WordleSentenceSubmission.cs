using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class WordleSentenceSubmission
    {
        [Key]
        [MaxLength(20)]
        public string SubmissionId { get; set; } = string.Empty;

        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual Account User { get; set; } = null!;

        [MaxLength(20)]
        public string DailyWordleId { get; set; } = string.Empty;

        [ForeignKey("DailyWordleId")]
        public virtual DailyWordle DailyWordle { get; set; } = null!;

        [MaxLength(100)] 
        public string SentenceContent { get; set; } = string.Empty;

        public int AiScore { get; set; } 

        public string? AiFeedbackJson { get; set; } 

        public bool IsPublic { get; set; } = true;
        public bool IsAnonymous { get; set; } = false;
        public int LikeCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
