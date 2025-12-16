using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Payments.Queries.GetPaymentHistory
{
    public class GetPaymentHistoryQuery : IRequest<OperationResult<List<PaymentHistoryDto>>>
    {
        public string UserId { get; set; }

        public GetPaymentHistoryQuery(string userId)
        {
            UserId = userId;
        }
    }
}