using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class ExamTemplate
    {
        [Key]
        [MaxLength(50)]
        public string ExamTemplateId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        public ExamType Type { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ExamTemplateStatus Status { get; set; } = ExamTemplateStatus.Draft;
        [MaxLength(500)]
        public string? RejectReason { get; set; }

        public virtual ICollection<TemplatePart> TemplateParts { get; set; } = new List<TemplatePart>();
        public virtual ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}