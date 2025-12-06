using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Tokki.Domain.Entities
{
    // Ràng buộc 1: 1 tài khoản Google cụ thể chỉ thuộc về 1 User duy nhất (Chống hack/trùng lặp)
    [Index(nameof(Provider), nameof(ProviderUserId), IsUnique = true)]

    // Ràng buộc 2: User chỉ được link 1 Google, 1 Facebook (Giúp User A login nhiều cách nhưng quản lý gọn)
    [Index(nameof(UserId), nameof(Provider), IsUnique = true)]
    public class SocialLogin
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(15)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Provider { get; set; } = string.Empty; // google, facebook

        [Required]
        [MaxLength(255)]
        public string ProviderUserId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? EmailFromProvider { get; set; }

        [MaxLength(255)]
        public string? NameFromProvider { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public virtual Account? Account { get; set; }
    }
}