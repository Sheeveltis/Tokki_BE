using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.Common.Models.Statistics;

namespace Tokki.Application.UseCases.Statistics.Queries
{
    public class GetDashboardOverviewQuery : IRequest<OperationResult<DashboardOverviewDto>>
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}