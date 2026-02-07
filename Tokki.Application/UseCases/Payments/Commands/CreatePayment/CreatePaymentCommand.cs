using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Payments.Commands.CreatePayment
{
    public record CreatePaymentResult(string PaymentId, string PaymentUrl);

    public class CreatePaymentCommand : IRequest<OperationResult<CreatePaymentResult>>
    {
        public string UserId { get; set; } = string.Empty;
        public string VipPackageId { get; set; }
    }
}