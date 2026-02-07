using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Payments.DTOs;

namespace Tokki.Application.UseCases.Statistics.Queries
{
    public class GetTransactionsReportQuery : IRequest<OperationResult<PagedResult<TransactionReportDto>>>
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}