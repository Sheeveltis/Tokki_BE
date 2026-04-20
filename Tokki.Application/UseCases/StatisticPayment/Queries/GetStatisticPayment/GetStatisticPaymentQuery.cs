using MediatR;
using System;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.StatisticPayment.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetStatisticPayment
{
    public class GetStatisticPaymentQuery : IRequest<OperationResult<PagedResult<StatisticPaymentDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public PaymentStatus? Status { get; set; }
        public bool? HasTransaction { get; set; }
        public string? VipPackageId { get; set; } // Filter theo gói VIP
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
