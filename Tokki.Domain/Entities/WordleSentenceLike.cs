using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class WordleSentenceLike
    {
        [Key]
        [MaxLength(20)]
        public string LikeId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string SubmissionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        [ForeignKey("SubmissionId")]
        public virtual WordleSentenceSubmission Submission { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual Account User { get; set; } = null!;
    }
}
