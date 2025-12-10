using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.Common.Models.Statistics;

namespace Tokki.Application.UseCases.Statistics.Queries
{
    public class GetRevenueChartQuery : IRequest<OperationResult<RevenueChartDto>>
    {
        public int Year { get; set; }
    }   
}