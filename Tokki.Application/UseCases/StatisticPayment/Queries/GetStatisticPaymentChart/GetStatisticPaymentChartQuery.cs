using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.StatisticPayment.DTOs;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetStatisticPaymentChart
{
    public class GetStatisticPaymentChartQuery : IRequest<OperationResult<List<StatisticPaymentChartDto>>>
    {
        public int Year { get; set; }
    }
}
