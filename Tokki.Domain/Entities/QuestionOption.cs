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
    public class QuestionOption
    {
        [Key]
        [MaxLength(10)]
        public string OptionId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string QuestionBankId { get; set; } = string.Empty;

        [Required]
        [MaxLength(1)]
        public string KeyOption { get; set; } = string.Empty; // '1', '2', '3', '4'

        public string? Content { get; set; }

        [MaxLength(255)]
        public string? ImageUrl { get; set; }

        public bool IsCorrect { get; set; } = false;

        // Navigation
        [ForeignKey(nameof(QuestionBankId))]
        public virtual QuestionBank QuestionBank { get; set; } = null!;
    }
}
