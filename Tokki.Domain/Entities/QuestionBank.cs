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
    public class QuestionBank
    {
        [Key]
        [MaxLength(10)]
        public string QuestionBankId { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? PassageId { get; set; }

        [MaxLength(10)]
        public string? QuestionTypeId { get; set; }

        [Required]
        public QuestionSkill Skill { get; set; }

        public string? Content { get; set; }

        [MaxLength(255)]
        public string? MediaUrl { get; set; }

        public string? Explanation { get; set; }

        public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Medium;

        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey(nameof(PassageId))]
        public virtual Passage? Passage { get; set; }

        [ForeignKey(nameof(QuestionTypeId))]
        public virtual QuestionType? QuestionType { get; set; }

        public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();
    }
}
