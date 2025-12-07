using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Payments.Queries.GetPaymentQr
{
    public class GetPaymentQrQuery : IRequest<OperationResult<string>>
    {
        public string PaymentId { get; set; }

        public GetPaymentQrQuery(string paymentId)
        {
            PaymentId = paymentId;
        }
    }
}