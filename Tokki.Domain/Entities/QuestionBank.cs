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

        public string? PassageId { get; set; }

        public string? QuestionTypeId { get; set; }

        public string? Content { get; set; }

        [MaxLength(255)]
        public string? MediaUrl { get; set; }

        public string? Explanation { get; set; }


        public QuestionBankStatus Status { get; set; }

        // Navigation
        [ForeignKey(nameof(PassageId))]
        public virtual Passage? Passage { get; set; }

        [ForeignKey(nameof(QuestionTypeId))]
        public virtual QuestionType? QuestionType { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();
    }
}
