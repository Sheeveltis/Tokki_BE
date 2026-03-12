using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class UserExamWritingAnswer
    {
        [Key]
        [MaxLength(20)]
        public string UserExamWritingAnswerId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string UserExamId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string QuestionId { get; set; } = string.Empty;

        public string AnswerContent { get; set; } = string.Empty;
        public int WordCount { get; set; }
        [Required]
        public int OrderIndex { get; set; }

        public int? Score { get; set; }
        public string? AiAnalysisJson { get; set; }

        public DateTime? GradedAt { get; set; }

        [ForeignKey(nameof(UserExamId))]
        public virtual UserExam UserExam { get; set; } = null!;

        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionBank Question { get; set; } = null!;
    }
}
