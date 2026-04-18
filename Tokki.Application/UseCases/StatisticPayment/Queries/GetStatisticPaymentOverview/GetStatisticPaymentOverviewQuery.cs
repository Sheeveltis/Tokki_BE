using MediatR;
using System;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.StatisticPayment.DTOs;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetStatisticPaymentOverview
{
    public class GetStatisticPaymentOverviewQuery : IRequest<OperationResult<StatisticPaymentOverviewDto>>
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
