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
        private readonly IVipPackageRepository _vipPackageRepository;
        private readonly ISePayService _sePayService;
        private readonly IIdGeneratorService _idGeneratorService;

        public CreatePaymentCommandHandler(
            IPaymentRepository paymentRepository,
            IVipPackageRepository vipPackageRepository,
            ISePayService sePayService,
            IIdGeneratorService idGeneratorService)
        {
            _paymentRepository = paymentRepository;
            _vipPackageRepository = vipPackageRepository;
            _sePayService = sePayService;
            _idGeneratorService = idGeneratorService;
        }

        public async Task<OperationResult<CreatePaymentResult>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
        {
            var vipPackage = await _vipPackageRepository.GetByIdAsync(request.VipPackageId);

            if (vipPackage == null)
            {
                return OperationResult<CreatePaymentResult>.Failure(AppErrors.VipPackageNotFound, 404);
            }
            if (!vipPackage.IsActive)
            {
                return OperationResult<CreatePaymentResult>.Failure(AppErrors.VipPackageInactive, 400);
            }

            var paymentId = _idGeneratorService.GenerateCustom(10);
            var description = $"Thanh toán {paymentId}"; 

            var qrUrl = _sePayService.GenerateQrUrl(paymentId, vipPackage.Price, description);
            var vnTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
            var expiresAt = vnTime.AddMinutes(10);

            var payment = new Payment
            {
                Id = paymentId,
                UserId = request.UserId,
                Amount = vipPackage.Price,
                Description = description,
                VipPackageId = vipPackage.Id,
                Status = PaymentStatus.Pending,
                CreatedAt = vnTime,
                ExpiresAt = expiresAt
            };

            await _paymentRepository.AddAsync(payment);

            var resultData = new CreatePaymentResult(payment.Id, qrUrl, expiresAt);

            return OperationResult<CreatePaymentResult>.Success(
                resultData,
                201,
                OperationMessages.CreateSuccess("Giao dịch thanh toán")
            );
        }
    }
}