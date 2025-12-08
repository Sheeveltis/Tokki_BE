using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class EmailJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JobId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty; // Nội dung HTML (Không giới hạn độ dài)

        [Required]
        public UserTargetGroup TargetGroup { get; set; } = UserTargetGroup.All;

        [Required]
        public DateTime ScheduledTime { get; set; } // Thời gian muốn gửi

        // Lưu trạng thái dạng chuỗi để dễ đọc trong DB (giống OtpStatus)
        [Column(TypeName = "nvarchar(50)")]
        public EmailJobStatus Status { get; set; } = EmailJobStatus.Pending;

        public DateTime? SentAt { get; set; } // Thời gian thực tế gửi xong

        public string? ErrorMessage { get; set; } // Lưu lỗi nếu gửi thất bại

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    }
}