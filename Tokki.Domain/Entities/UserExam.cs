using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class UserExam
    {
        [Key]
        [MaxLength(15)]
        public string UserExamId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string ExamId { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }

        public DateTime? SubmitTime { get; set; }

        public int Score { get; set; }

        public int Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual Account Account { get; set; } = null!;

        [ForeignKey(nameof(ExamId))]
        public virtual Exam Exam { get; set; } = null!;
    }
}