using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Payments.DTOs;

namespace Tokki.Application.UseCases.Statistics.Queries
{
    public class GetRevenueChartQuery : IRequest<OperationResult<List<RevenueChartDto>>>
    {
        public int Year { get; set; }
    }
}