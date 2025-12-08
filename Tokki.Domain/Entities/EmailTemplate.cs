using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore; // Cần cái này cho [Index]

namespace Tokki.Domain.Entities
{
    // Đảm bảo Key không bị trùng
    [Index(nameof(TemplateKey), IsUnique = true)]
    public class EmailTemplate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TemplateId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TemplateKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    }
}