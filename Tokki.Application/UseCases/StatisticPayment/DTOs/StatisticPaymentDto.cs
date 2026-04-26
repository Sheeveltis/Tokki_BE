using System;

namespace Tokki.Application.UseCases.StatisticPayment.DTOs
{
    public class StatisticPaymentDto
    {
        public string PaymentId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AccountBankNumber { get; set; } = string.Empty; // Token từ Transaction
        public string BankName { get; set; } = string.Empty; // Gateway từ Transaction
        public string PackageName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
