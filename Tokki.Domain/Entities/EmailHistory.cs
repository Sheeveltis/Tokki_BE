// Tokki.Domain/Entities/EmailHistory.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tokki.Domain.Entities
{
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
        [MaxLength(50)]
        public string TemplateKey { get; set; } = string.Empty;

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow.AddHours(7);

        // Navigation Property
        public virtual Account? Account { get; set; }
    }
}