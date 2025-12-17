using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class TemplatePart
    {
        [Key]
        [MaxLength(10)]
        public string TemplatePartId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string ExamTemplateId { get; set; } = string.Empty;

        [Required]
        public QuestionSkill Skill { get; set; } 

        public int QuestionFrom { get; set; }
        public int QuestionTo { get; set; }

        [Required]
        [MaxLength(150)]
        public string PartTitle { get; set; } = string.Empty;

        public string? Instruction { get; set; }

        [Required]
        public DifficultyLevel DifficultyLevel { get; set; } 

        [Required]
        [MaxLength(10)]
        public string QuestionTypeId { get; set; } = string.Empty;

        public ExampleType ExampleType { get; set; } = ExampleType.None;
        public string? ExampleData { get; set; }

        [ForeignKey(nameof(ExamTemplateId))]
        public virtual ExamTemplate ExamTemplate { get; set; } = null!;

        [ForeignKey(nameof(QuestionTypeId))]
        public virtual QuestionType QuestionType { get; set; } = null!;
    }
}