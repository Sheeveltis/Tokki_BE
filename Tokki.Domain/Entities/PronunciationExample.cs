using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokki.Domain.Entities
{
    public class PronunciationExample
    {
        [Key]
        [MaxLength(10)]
        public string ExampleId { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string PronunciationRuleId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string TargetScript { get; set; } = string.Empty; 

        [Required]
        [MaxLength(500)]
        public string RawScript { get; set; } = string.Empty;   

        [Required]
        [MaxLength(500)]
        public string PhoneticScript { get; set; } = string.Empty; 

        [MaxLength(500)]
        public string? Meaning { get; set; }

        public string? AudioUrl { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsDeleted { get; set; } = false;

        [MaxLength(15)]
        public string? CreateBy { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        [MaxLength(15)]
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }

        [ForeignKey(nameof(PronunciationRuleId))]
        public virtual PronunciationRule? PronunciationRule { get; set; }
    }
}
