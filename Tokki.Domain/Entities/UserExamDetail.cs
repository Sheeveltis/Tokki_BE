using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
    public class UserExamDetail
    {
        [Key]
        [MaxLength(15)]
        public string DetailId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string UserExamId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string QuestionId { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? SelectedOptionId { get; set; } 

        public bool IsCorrect { get; set; }

        [MaxLength(10)]
        public string QuestionTypeId { get; set; } = string.Empty; 

        [ForeignKey(nameof(UserExamId))]
        public virtual UserExam UserExam { get; set; } = null!;

        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionBank Question { get; set; } = null!;

        [ForeignKey(nameof(QuestionTypeId))]
        public virtual QuestionType QuestionType { get; set; } = null!;
    }
}