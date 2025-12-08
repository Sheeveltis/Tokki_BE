using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tokki.Domain.Enums;

namespace Tokki.Domain.Entities
{
    public class Payment
    {
        public string Id { get; set; } = string.Empty; 
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty; 
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? PaidAt { get; set; }
        public int? TransactionId { get; set; }
        [ForeignKey("TransactionId")]
        public virtual Transaction? Transaction { get; set; }
        public VipPlanType PlanType { get; set; } = VipPlanType.OneMonth; 
        //public string? PaymentUrl { get; set; }
    }
}