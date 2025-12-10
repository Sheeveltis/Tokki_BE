using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.Common.Models.Statistics;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Statistics.Queries;

namespace Tokki.UnitTests.UseCases.Statistics.Queries.GetRevenueByPackage
{
    public class GetRevenueByPackageHandler : IRequestHandler<GetRevenueByPackageQuery, OperationResult<List<RevenueByPackageDto>>>
    {
        private readonly IStatisticsRepository _repo;
        public GetRevenueByPackageHandler(IStatisticsRepository repo) => _repo = repo;

        public async Task<OperationResult<List<RevenueByPackageDto>>> Handle(GetRevenueByPackageQuery request, CancellationToken cancellationToken)
        {
            var data = await _repo.GetRevenueByPackageAsync(request.StartDate, request.EndDate);
            return OperationResult<List<RevenueByPackageDto>>.Success(data);
        }
    }
}
