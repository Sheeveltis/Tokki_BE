using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    [Index(nameof(Letter), IsUnique = true)]
    public class AlphabetData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Letter { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Meaning { get; set; }

        [MaxLength(50)]
        public string? Pronunciation { get; set; }

        public int TotalStrokes { get; set; }

        [Required]
        public AlphabetType Type { get; set; }

        [MaxLength(2048)]
        public string? AudioUrl { get; set; }

        public string? DisplayDataJson { get; set; }

        public string? ValidationDataJson { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    }
}
