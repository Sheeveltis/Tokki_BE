using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Payments.Commands.CreatePayment
{
    public class CancelPaymentCommand : IRequest<OperationResult<string>>
    {
        public string PaymentId { get; set; }
        public string UserId { get; set; }
    }
}
