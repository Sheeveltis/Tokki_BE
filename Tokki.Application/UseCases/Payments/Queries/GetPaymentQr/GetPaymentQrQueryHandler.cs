using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.Payments.Queries.GetPaymentQr
{
    public class GetPaymentQrQueryHandler : IRequestHandler<GetPaymentQrQuery, OperationResult<string>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ISePayService _sePayService;

        public GetPaymentQrQueryHandler(IPaymentRepository paymentRepository, ISePayService sePayService)
        {
            _paymentRepository = paymentRepository;
            _sePayService = sePayService;
        }

        public async Task<OperationResult<string>> Handle(GetPaymentQrQuery request, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByIdAsync(request.PaymentId);

            if (payment == null)
            {
                return OperationResult<string>.Failure("Không tìm thấy giao dịch.", 404);
            }

          
            var qrUrl = _sePayService.GenerateQrUrl(payment.Id, payment.Amount, payment.Description);

            return OperationResult<string>.Success(qrUrl);
        }
    }
}