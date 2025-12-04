using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Payments.Commands.CreatePayment
{
    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, OperationResult<CreatePaymentResult>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ISePayService _sePayService;
        private readonly IIdGeneratorService _idGeneratorService; 

        public CreatePaymentCommandHandler(
            IPaymentRepository paymentRepository,
            ISePayService sePayService,
            IIdGeneratorService idGeneratorService)
        {
            _paymentRepository = paymentRepository;
            _sePayService = sePayService;
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<CreatePaymentResult>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            if (request.Amount < 1000) 
            {
                return OperationResult<CreatePaymentResult>.Failure("Số tiền thanh toán phải lớn hơn 1,000 VNĐ.");
            }

            var paymentId = _idGeneratorService.GenerateCustom(10); 

            var qrUrl = _sePayService.GenerateQrUrl(paymentId, request.Amount, request.Description);

            var payment = new Payment
            {
                Id = paymentId,
                UserId = request.UserId,
                Amount = request.Amount,
                Description = request.Description,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            await _paymentRepository.AddAsync(payment);

            var resultData = new CreatePaymentResult(payment.Id, qrUrl);

            return OperationResult<CreatePaymentResult>.Success(resultData, 201, "Tạo giao dịch thành công. Vui lòng quét mã QR.");
        }
    }
}