using Microsoft.EntityFrameworkCore;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Payments.DTOs; 
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Repositories
{
    public class StatisticsRepository : IStatisticsRepository
    {
        private readonly TokkiDbContext _context;

        public StatisticsRepository(TokkiDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardOverviewDto> GetOverviewAsync(DateTime startDate, DateTime endDate)
        {
            var query = _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid &&
                            p.PaidAt >= startDate &&
                            p.PaidAt <= endDate);

            var totalRevenue = await query.SumAsync(p => p.Amount);
            var totalOrders = await query.CountAsync();

            var daysDiff = (endDate - startDate).TotalDays;
            var previousStartDate = startDate.AddDays(-daysDiff);

            var previousRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid &&
                            p.PaidAt >= previousStartDate &&
                            p.PaidAt < startDate)
                .SumAsync(p => p.Amount);

            double growth = 0;
            if (previousRevenue > 0)
                growth = (double)((totalRevenue - previousRevenue) / previousRevenue) * 100;

            return new DashboardOverviewDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageRevenue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
            };
        }

        public async Task<List<RevenueChartDto>> GetRevenueChartAsync(int year)
        {
            var data = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Paid &&
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

            var result = new List<RevenueChartDto>();

            for (int i = 1; i <= 12; i++)
            {
                var monthData = data.FirstOrDefault(x => x.Month == i);

                result.Add(new RevenueChartDto
                {
                    Month = $"Tháng {i}",
                    Revenue = monthData?.Revenue ?? 0,
                    TotalOrders = monthData?.Count ?? 0 
                });
            }

            return result;
        }

        public async Task<List<RevenueByPackageDto>> GetRevenueByPackageAsync(DateTime startDate, DateTime endDate)
        {
            var query = from p in _context.Payments
                        join v in _context.VipPackages on p.VipPackageId equals v.Id
                        where p.Status == PaymentStatus.Paid &&
                              p.PaidAt >= startDate &&
                              p.PaidAt <= endDate
                        select new { p.Amount, v.Name, v.DurationDays };

            var groupedData = await query
                .GroupBy(x => new { x.Name, x.DurationDays })
                .Select(g => new
                {
                    PackageName = g.Key.Name,
                    Duration = g.Key.DurationDays,
                    Revenue = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .ToListAsync();

            var totalRevenue = groupedData.Sum(x => x.Revenue);

            return groupedData.Select(x => new RevenueByPackageDto
            {
                PackageName = x.PackageName,
                DurationDays = x.Duration,
                Revenue = x.Revenue,
                SalesCount = x.Count,
                Percentage = totalRevenue > 0 ? (double)(x.Revenue / totalRevenue * 100) : 0
            }).OrderByDescending(x => x.Revenue).ToList();
        }

        public async Task<(List<TransactionReportDto> Items, int TotalCount)> GetTransactionsAsync(
            string? search, string? status, DateTime? fromDate, DateTime? toDate, int pageIndex, int pageSize)
        {
            var query = _context.Payments.AsQueryable();

            if (fromDate.HasValue) query = query.Where(p => p.CreatedAt >= fromDate);
            if (toDate.HasValue) query = query.Where(p => p.CreatedAt <= toDate);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PaymentStatus>(status, true, out var statusEnum))
            {
                query = query.Where(p => p.Status == statusEnum);
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
                                  Gateway = trans != null ? trans.Gateway : "Unknown"
                              };

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                joinedQuery = joinedQuery.Where(x =>
                    x.Payment.Id.Contains(search) ||
                    x.User.Email.Contains(search) ||
                    x.User.FullName.Contains(search));
            }

            var totalCount = await joinedQuery.CountAsync();

            var items = await joinedQuery
                .OrderByDescending(x => x.Payment.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new TransactionReportDto
                {
                    TransactionId = x.Payment.Id,
                    UserEmail = x.User.Email,
                    FullName = x.User.FullName,
                    UserAvatar = x.User.AvatarUrl ?? "",
                    PackageName = x.PackageName,
                    Amount = x.Payment.Amount,
                    PaymentMethod = x.Gateway,
                    Status = x.Payment.Status.ToString(),
                    PaymentDate = x.Payment.CreatedAt.DateTime
                })
                .ToListAsync();

            return (items, totalCount);
        }
    }
}