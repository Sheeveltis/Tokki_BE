namespace Tokki.Application.UseCases.StatisticPayment.DTOs
{
    public class PackageDistributionDto
    {
        public string PackageName { get; set; } = string.Empty;
        public int Count { get; set; } // Số lượng bán ra
        public decimal Revenue { get; set; } // Doanh thu từ gói này
        public double Percentage { get; set; } // Tỉ lệ % trên tổng số
    }
}
