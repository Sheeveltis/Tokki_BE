using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Payments.Queries.GetPaymentHistory
{
    public class PaymentHistoryDto
    {
        public string PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public string? VipPackageId { get; set; }

        public DateTimeOffset? CurrentVipExpirationDate { get; set; }
        public int CurrentRemainingDays
        {
            get
            {
                if (!CurrentVipExpirationDate.HasValue) return 0;
                var remaining = (CurrentVipExpirationDate.Value - DateTimeOffset.UtcNow).TotalDays;
                return remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
            }
        }

        public string StatusDisplay
        {
            get
            {
                if (Status == PaymentStatus.Pending) return "Đang chờ thanh toán";
                if (Status == PaymentStatus.Paid) return "Thành công";
                return "Thất bại";
            }
        }
    }
}