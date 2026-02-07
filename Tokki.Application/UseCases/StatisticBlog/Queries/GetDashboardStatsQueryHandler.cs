using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticBlog.DTOs;

public class GetDashboardStatsQuery : IRequest<OperationResult<DashboardStatDTO>> { }

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, OperationResult<DashboardStatDTO>>
{
    private readonly IStatisticBlogRepository _repo;
    public GetDashboardStatsQueryHandler(IStatisticBlogRepository repo) => _repo = repo;

    public async Task<OperationResult<DashboardStatDTO>> Handle(GetDashboardStatsQuery request, CancellationToken token)
    {
        var stats = await _repo.GetDashboardStatsAsync(token);
        return OperationResult<DashboardStatDTO>.Success(stats,200,OperationMessages.GetSuccess("Thống kê sơ bộ blog"));
    }
}