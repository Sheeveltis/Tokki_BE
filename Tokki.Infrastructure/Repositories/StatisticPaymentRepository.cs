using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticPayment.DTOs;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class StatisticPaymentRepository : IStatisticPaymentRepository
    {
        private readonly TokkiDbContext _context;

        public StatisticPaymentRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<(List<StatisticPaymentDto> Items, int TotalCount)> GetStatisticPaymentsAsync(
            string? searchTerm, PaymentStatus? status, bool? hasTransaction, string? vipPackageId, DateTime? fromDate, DateTime? toDate, int pageNumber, int pageSize)
        {
            var query = _context.Payments.AsQueryable();

            if (fromDate.HasValue) query = query.Where(p => p.CreatedAt >= fromDate);
            if (toDate.HasValue) query = query.Where(p => p.CreatedAt <= toDate);

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (hasTransaction.HasValue)
            {
                if (hasTransaction.Value)
                    query = query.Where(p => p.TransactionId != null);
                else
                    query = query.Where(p => p.TransactionId == null);
            }

            if (!string.IsNullOrEmpty(vipPackageId))
            {
                query = query.Where(p => p.VipPackageId == vipPackageId);
            }

            var joinedQuery = from p in query
                              join u in _context.Accounts on p.UserId equals u.UserId
                              join v in _context.VipPackages on p.VipPackageId equals v.Id
                              join t in _context.Transactions on p.TransactionId equals t.Id into tGroup
                              from trans in tGroup.DefaultIfEmpty()
                              select new
                              {
                                  Payment = p,
                                  User = u,
                                  PackageName = v.Name,
                                  Transaction = trans
                              };

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                joinedQuery = joinedQuery.Where(x =>
                    x.Payment.Id.Contains(searchTerm) ||
                    x.User.Email.Contains(searchTerm) ||
                    x.User.FullName.Contains(searchTerm) ||
                    (x.Transaction != null && x.Transaction.AccountNumber.Contains(searchTerm)));
            }

            var totalCount = await joinedQuery.CountAsync();

            var items = await joinedQuery
                .OrderByDescending(x => x.Payment.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new StatisticPaymentDto
                {
                    PaymentId = x.Payment.Id,
                    UserEmail = x.User.Email,
                    FullName = x.User.FullName,
                    AccountBankNumber = x.Transaction != null ? x.Transaction.AccountNumber : "N/A",
                    BankName = x.Transaction != null ? x.Transaction.Gateway : "Unknown",
                    PackageName = x.PackageName,
                    Amount = x.Payment.Amount,
                    Status = x.Payment.Status.ToString(),
                    CreatedAt = x.Payment.CreatedAt.DateTime
                })
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<StatisticPaymentOverviewDto> GetOverviewAsync(DateTime startDate, DateTime endDate)
        {
            var query = _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid && 
                            p.TransactionId != null && // Bắt buộc phải có giao dịch thật
                            p.PaidAt >= startDate &&
                            p.PaidAt <= endDate);

            var totalRevenue = await query.SumAsync(p => p.Amount);
            var totalTransactions = await query.CountAsync();

            var daysDiff = (endDate - startDate).TotalDays;
            var previousStartDate = startDate.AddDays(-daysDiff);

            var previousRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid &&
                            p.TransactionId != null && // Lọc cho cả kỳ trước
                            p.PaidAt >= previousStartDate &&
                            p.PaidAt < startDate)
                .SumAsync(p => p.Amount);

            decimal growth = 0;
            if (previousRevenue > 0)
                growth = (totalRevenue - previousRevenue) / previousRevenue * 100;

            return new StatisticPaymentOverviewDto
            {
                TotalRevenue = totalRevenue,
                TotalTransactions = totalTransactions,
                AverageAmount = totalTransactions > 0 ? totalRevenue / totalTransactions : 0,
                GrowthRate = growth
            };
        }

        public async Task<List<StatisticPaymentChartDto>> GetRevenueChartAsync(int year)
        {
            var data = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid &&
                            p.TransactionId != null && // Bắt buộc phải có giao dịch thật
                            p.PaidAt.HasValue &&
                            p.PaidAt.Value.Year == year)
                .GroupBy(p => p.PaidAt.Value.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Revenue = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .ToListAsync();

            var result = new List<StatisticPaymentChartDto>();

            for (int i = 1; i <= 12; i++)
            {
                var monthData = data.FirstOrDefault(x => x.Month == i);

                result.Add(new StatisticPaymentChartDto
                {
                    label = $"Tháng {i}",
                    value = monthData?.Revenue ?? 0,
                    count = monthData?.Count ?? 0
                });
            }

            return result;
        }

        public async Task<List<VipPackageLookupDto>> GetVipPackagesLookupAsync()
        {
            return await _context.VipPackages
                .Select(v => new VipPackageLookupDto
                {
                    Id = v.Id,
                    Name = v.Name
                })
                .ToListAsync();
        }

        public async Task<List<PackageDistributionDto>> GetPackageDistributionAsync(DateTime startDate, DateTime endDate)
        {
            var query = from p in _context.Payments
                        join v in _context.VipPackages on p.VipPackageId equals v.Id
                        where p.Status == PaymentStatus.Paid &&
                              p.TransactionId != null &&
                              p.PaidAt >= startDate &&
                              p.PaidAt <= endDate
                        select new { p.Amount, v.Name };

            var data = await query.ToListAsync();

            var totalCount = data.Count;

            return data.GroupBy(x => x.Name)
                       .Select(g => new PackageDistributionDto
                       {
                           PackageName = g.Key,
                           Count = g.Count(),
                           Revenue = g.Sum(x => x.Amount),
                           Percentage = totalCount > 0 ? (double)g.Count() / totalCount * 100 : 0
                       })
                       .OrderByDescending(x => x.Count)
                       .ToList();
        }
    }
}
