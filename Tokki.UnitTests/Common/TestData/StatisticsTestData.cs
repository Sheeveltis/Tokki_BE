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
            };
        }
        public static List<RevenueChartDto> GetRevenueChart()
        {
            return new List<RevenueChartDto>
            {
                new RevenueChartDto
                {
                    Month = "Tháng 1",
                    Revenue = 500000,
                    TotalOrders = 50
                },
                new RevenueChartDto
                {
                    Month = "Tháng 2",
                    Revenue = 700000,
                    TotalOrders = 70
                }
            };
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