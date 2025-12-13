using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class ExamQuestion
    {
        [Key]
        [MaxLength(10)]
        public string ExamQuestionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string ExamId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string QuestionBankId { get; set; } = string.Empty;

        [Required]
        public int QuestionNo { get; set; }

        public int Score { get; set; } = 2;

        // Navigation
        [ForeignKey(nameof(ExamId))]
        public virtual Exam Exam { get; set; } = null!;

        [ForeignKey(nameof(QuestionBankId))]
        public virtual QuestionBank QuestionBank { get; set; } = null!;
    }
}
