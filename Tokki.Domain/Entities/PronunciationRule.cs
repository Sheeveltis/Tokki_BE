using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class PronunciationRule
    {
        [Key]
        [MaxLength(10)]
        public string PronunciationRuleId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string RuleName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? Content { get; set; } 

        public bool IsDeleted { get; set; } = false;

        [MaxLength(15)]
        public string? CreateBy { get; set; }

        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        [MaxLength(15)]
        public string? UpdateBy { get; set; }

        public DateTime? UpdateDate { get; set; }

        public virtual ICollection<PronunciationExample> Examples { get; set; } = new List<PronunciationExample>();

        [ForeignKey(nameof(CreateBy))]
        public virtual Account? Creator { get; set; }

        [ForeignKey(nameof(UpdateBy))]
        public virtual Account? Updater { get; set; }
        public int SortOrder { get; set; } = 0;
    }
}
