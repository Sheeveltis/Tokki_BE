using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Payments.Commands.CreatePayment;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Payments.Commands.CancelPayment
{
    public class CancelPaymentCommandHandler : IRequestHandler<CancelPaymentCommand, OperationResult<string>>
    {
        private readonly IPaymentRepository _paymentRepository;

        public CancelPaymentCommandHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<OperationResult<string>> Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);

            if (payment == null)
                return OperationResult<string>.Failure(AppErrors.PaymentNotFound, 404);

            if (payment.UserId != request.UserId)
                return OperationResult<string>.Failure(AppErrors.Forbidden, 403);

            if (payment.Status != PaymentStatus.Pending)
                return OperationResult<string>.Failure(AppErrors.PaymentAlreadyProcessed, 400);

            payment.Status = PaymentStatus.Cancelled;
            await _paymentRepository.UpdateAsync(payment);

            return OperationResult<string>.Success("Hủy giao dịch thành công.", 200);
        }
    }
}