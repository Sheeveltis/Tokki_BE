using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        [Required]
        public int QuestionFrom { get; set; }

        [Required]
        public int QuestionTo { get; set; }

        [MaxLength(255)]
        public string? PartTitle { get; set; }

        public string? Instruction { get; set; }

        public ExampleType ExampleType { get; set; } = ExampleType.None;

        public string? ExampleData { get; set; }

        // Navigation
        [ForeignKey(nameof(ExamTemplateId))]
        public virtual ExamTemplate ExamTemplate { get; set; } = null!;
    }
}
