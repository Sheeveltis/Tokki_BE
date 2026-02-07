using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    [Table("EmailTemplates")]
    [Index(nameof(TemplateName), IsUnique = true)]
    [Index(nameof(Type), nameof(Value), nameof(TargetGroup), IsUnique = true)]
    public class EmailTemplate
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [MaxLength(15)]
        public string TemplateId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        public EmailTemplateType Type { get; set; }

        [Required]
        public int Value { get; set; }

        [Required]
        public UserTargetGroup TargetGroup { get; set; } = UserTargetGroup.All;

        // NEW: trạng thái template
        [Required]
        public EmailTemplateStatus Status { get; set; } = EmailTemplateStatus.Draft;

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        [Required]
        public DateTime CreateAt { get; set; } = DateTime.UtcNow.AddHours(7);
    }
}
