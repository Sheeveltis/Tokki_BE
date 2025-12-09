using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums; 
namespace Tokki.Domain.Entities
{
    [Table("Subscriptions")]
    public class Subscription
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string VipPackageId { get; set; }
        public string? PaymentId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public SubscriptionStatus Status { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Property (Khóa ngoại) ---
        [ForeignKey(nameof(UserId))]
        public virtual Account? Account { get; set; }
    }
}