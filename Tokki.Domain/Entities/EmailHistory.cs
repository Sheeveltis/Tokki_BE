using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tokki.Domain.Entities
{
    [Table("EmailHistories")]
    [Index(nameof(UserId), nameof(TemplateId), IsUnique = true)] // chống gửi trùng theo template
    public class EmailHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [MaxLength(15)]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(15)]
        public string TemplateId { get; set; } = string.Empty; // thay TemplateKey

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow.AddHours(7);

        // Navigation
        [ForeignKey(nameof(UserId))]
        public virtual Account? Account { get; set; }

        [ForeignKey(nameof(TemplateId))]
        public virtual EmailTemplate? EmailTemplate { get; set; }
    }
}
