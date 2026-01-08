using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Payments.DTOs;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Statistics.Queries;

namespace Tokki.UnitTests.UseCases.Statistics.Queries.GetRevenueChart
{
    public class GetRevenueChartHandler : IRequestHandler<GetRevenueChartQuery, OperationResult<List<RevenueChartDto>>>
    {
        private readonly IStatisticsRepository _repo;
        public GetRevenueChartHandler(IStatisticsRepository repo) => _repo = repo;

        public async Task<OperationResult<List<RevenueChartDto>>> Handle(GetRevenueChartQuery request, CancellationToken cancellationToken)
        {
            var data = await _repo.GetRevenueChartAsync(request.Year);
            return OperationResult<List<RevenueChartDto>>.Success(data);
        }
    }
}