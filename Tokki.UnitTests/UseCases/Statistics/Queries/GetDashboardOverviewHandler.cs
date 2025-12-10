using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.Common.Models.Statistics;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Statistics.Queries;

public class GetDashboardOverviewHandler : IRequestHandler<GetDashboardOverviewQuery, OperationResult<DashboardOverviewDto>>
{
    private readonly IStatisticsRepository _repo;
    public GetDashboardOverviewHandler(IStatisticsRepository repo) => _repo = repo;

    public async Task<OperationResult<DashboardOverviewDto>> Handle(GetDashboardOverviewQuery request, CancellationToken cancellationToken)
    {
        var data = await _repo.GetOverviewAsync(request.StartDate, request.EndDate);
        return OperationResult<DashboardOverviewDto>.Success(data);
    }
}
