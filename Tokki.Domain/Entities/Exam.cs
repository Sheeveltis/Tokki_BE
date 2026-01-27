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
    public class Exam
    {
        [Key]
        [MaxLength(10)]
        public string ExamId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string ExamTemplateId { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public int Duration { get; set; }

        [Required]
        public ExamType Type { get; set; }

        public ExamStatus Status { get; set; } = ExamStatus.Draft;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        // Navigation
        [ForeignKey(nameof(ExamTemplateId))]
        public virtual ExamTemplate ExamTemplate { get; set; } = null!;
        public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();
    }

}
