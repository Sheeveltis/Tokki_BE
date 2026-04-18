namespace Tokki.Application.UseCases.StatisticPayment.DTOs
{
    public class StatisticPaymentChartDto
    {
        public string label { get; set; } = string.Empty; // Tháng/Ngày
        public decimal value { get; set; } // Doanh thu
        public int count { get; set; } // Số giao dịch
    }
}
