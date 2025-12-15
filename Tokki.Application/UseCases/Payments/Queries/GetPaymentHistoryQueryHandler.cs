using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Payments.Queries.GetPaymentHistory
{
    public class GetPaymentHistoryQueryHandler : IRequestHandler<GetPaymentHistoryQuery, OperationResult<List<PaymentHistoryDto>>>
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IAccountRepository _accountRepository; 

        public GetPaymentHistoryQueryHandler(
            IPaymentRepository paymentRepository,
            IAccountRepository accountRepository)
        {
            _paymentRepository = paymentRepository;
            _accountRepository = accountRepository;
        }

        public async Task<OperationResult<List<PaymentHistoryDto>>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
        {
            var user = await _accountRepository.GetByIdAsync(request.UserId);

            DateTimeOffset? currentExpirationDate = user?.VipExpirationDate;
            var payments = await _paymentRepository.GetByUserIdAsync(request.UserId);

            var resultDtos = payments.Select(p => new PaymentHistoryDto
            {
                PaymentId = p.Id,
                Amount = p.Amount,
                Description = p.Description,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt,
                VipPackageId = p.VipPackageId,

                CurrentVipExpirationDate = currentExpirationDate
            }).ToList();

            return OperationResult<List<PaymentHistoryDto>>.Success(resultDtos, 200, OperationMessages.GetSuccess("Lịch sử giao dịch"));
        }
    }
}