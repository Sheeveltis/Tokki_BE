using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tokki.Domain.Entities
{
    [Index(nameof(TemplateKey), IsUnique = true)]
    public class EmailTemplate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] 
        [MaxLength(15)] 
        public string TemplateId { get; set; } = string.Empty;

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