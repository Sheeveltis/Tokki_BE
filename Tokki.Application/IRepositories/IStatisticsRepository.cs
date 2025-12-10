using Tokki.Application.UseCases.Payments.DTOs; 
namespace Tokki.Application.IRepositories
{
    public interface IStatisticsRepository
    {
        Task<DashboardOverviewDto> GetOverviewAsync(DateTime startDate, DateTime endDate);

        Task<RevenueChartDto> GetRevenueChartAsync(int year);

        Task<List<RevenueByPackageDto>> GetRevenueByPackageAsync(DateTime startDate, DateTime endDate);

        Task<(List<TransactionReportDto> Items, int TotalCount)> GetTransactionsAsync(
            string? search,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            int pageIndex,
            int pageSize);
    }
}