using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;
using Tokki.Domain.Entities;

namespace Tokki.Domain.Entities
{
    public class TemplatePart
    {
        [Key]
        [MaxLength(50)]
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
        public int Mark { get; set; } 

        [MaxLength(10)]
        public string? QuestionTypeId { get; set; } = string.Empty;

        public string? ExampleUrl { get; set; }

        [ForeignKey(nameof(ExamTemplateId))]
        public virtual ExamTemplate ExamTemplate { get; set; } = null!;

        [ForeignKey(nameof(QuestionTypeId))]
        public virtual QuestionType QuestionType { get; set; } = null!;
    }
}