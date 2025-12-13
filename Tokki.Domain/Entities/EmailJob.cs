// Tokki.Domain/Entities/EmailJob.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class EmailJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [MaxLength(15)]
        public string JobId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        public UserTargetGroup TargetGroup { get; set; } = UserTargetGroup.None;

        [Column(TypeName = "nvarchar(max)")]
        public string? SpecificEmails { get; set; } // JSON array: ["email1@test.com", "email2@test.com"]

        [Required]
        public DateTime ScheduledTime { get; set; }

        public EmailJobStatus Status { get; set; } = EmailJobStatus.Pending;

        public DateTime? SentAt { get; set; }
        public string? ErrorMessage { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    }
}