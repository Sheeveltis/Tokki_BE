namespace Tokki.Application.UseCases.StatisticPayment.DTOs
{
    public class StatisticPaymentOverviewDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public decimal AverageAmount { get; set; }
        public decimal GrowthRate { get; set; } // So với kỳ trước
    }
}
