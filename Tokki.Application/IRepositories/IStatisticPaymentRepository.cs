using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.StatisticPayment.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.IRepositories
{
    public interface IStatisticPaymentRepository
    {
        Task<(List<StatisticPaymentDto> Items, int TotalCount)> GetStatisticPaymentsAsync(
            string? searchTerm,
            PaymentStatus? status,
            bool? hasTransaction,
            string? vipPackageId,
            DateTime? fromDate,
            DateTime? toDate,
            int pageNumber,
            int pageSize);

        Task<StatisticPaymentOverviewDto> GetOverviewAsync(DateTime startDate, DateTime endDate);
        Task<List<StatisticPaymentChartDto>> GetRevenueChartAsync(int year);
        Task<List<VipPackageLookupDto>> GetVipPackagesLookupAsync();
        Task<List<PackageDistributionDto>> GetPackageDistributionAsync(DateTime startDate, DateTime endDate);
    }
}
