using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        // ===== NEW: CreateBy / ApprovedBy / ApprovedDate =====

        [MaxLength(15)]
        public string? CreateBy { get; set; }

        [MaxLength(15)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        // ===== Navigation =====

        [ForeignKey(nameof(PassageId))]
        public virtual Passage? Passage { get; set; }

        [ForeignKey(nameof(QuestionTypeId))]
        public virtual QuestionType? QuestionType { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();



        [ForeignKey(nameof(CreateBy))]
        public virtual Account? CreatedByAccount { get; set; }

        [ForeignKey(nameof(ApprovedBy))]
        public virtual Account? ApprovedByAccount { get; set; }
    }
}
