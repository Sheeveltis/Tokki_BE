using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Payments.DTOs;

namespace Tokki.Application.UseCases.Statistics.Queries
{
    public class GetDashboardOverviewQuery : IRequest<OperationResult<DashboardOverviewDto>>
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}