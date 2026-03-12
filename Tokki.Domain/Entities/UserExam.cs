using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class UserExam
    {
        [Key]
        [StringLength(15)]
        public string UserExamId { get; set; }

        [Required]
        [StringLength(15)]
        public string UserId { get; set; } = null!;

        [Required]
        [StringLength(10)]
        public string ExamId { get; set; } = null!;

        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? SubmitTime { get; set; } 

        public int Score { get; set; } = 0;

        public UserExamStatus Status { get; set; } = UserExamStatus.InProgress;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual Account User { get; set; } = null!; 

        [ForeignKey("ExamId")]
        public virtual Exam Exam { get; set; } = null!;

        public virtual ICollection<UserExamAnswer> UserExamAnswers { get; set; } = new List<UserExamAnswer>();
        public virtual ICollection<UserExamWritingAnswer> UserExamWritingAnswers { get; set; } = new List<UserExamWritingAnswer>();
    }
}
