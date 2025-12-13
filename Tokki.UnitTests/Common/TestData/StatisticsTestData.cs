using Tokki.Application.UseCases.Payments.DTOs;
using Tokki.Application.UseCases.Statistics.Queries;

namespace Tokki.UnitTests.Common.TestData
{
    public static class StatisticsTestData
    {
        public static DashboardOverviewDto GetDashboardOverview()
        {
            return new DashboardOverviewDto
            {
                TotalRevenue = 15000000,
                TotalOrders = 300,
                AverageRevenue = 50000,
                GrowthRate = 15.5
            };
        }
        public static RevenueChartDto GetRevenueChart()
        {
            var chart = new RevenueChartDto();
            chart.Labels = new List<string> { "Tháng 1", "Tháng 2" };
            chart.Data = new List<decimal> { 500000, 700000 };
            return chart;
        }
        public static List<RevenueByPackageDto> GetRevenueByPackages()
        {
            return new List<RevenueByPackageDto>
            {
                new RevenueByPackageDto { PackageName = "VIP 1 Tháng", Revenue = 5000000, SalesCount = 100, Percentage = 50 },
                new RevenueByPackageDto { PackageName = "VIP 1 Năm", Revenue = 5000000, SalesCount = 10, Percentage = 50 }
            };
        }
        public static List<TransactionReportDto> GetTransactions()
        {
            return new List<TransactionReportDto>
            {
                new TransactionReportDto
                {
                    TransactionId = "TRANS_01",
                    Amount = 50000,
                    FullName = "Nguyen Van A",
                    Status = "Paid"
                },
                new TransactionReportDto
                {
                    TransactionId = "TRANS_02",
                    Amount = 100000,
                    FullName = "Tran Van B",
                    Status = "Pending"
                }
            };
        }
    }
}