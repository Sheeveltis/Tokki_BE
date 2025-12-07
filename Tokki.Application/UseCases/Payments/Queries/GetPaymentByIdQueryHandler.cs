using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Payments.Queries.GetPaymentById
{
    public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, OperationResult<Payment>>
    {
        private readonly IPaymentRepository _paymentRepository;

        public GetPaymentByIdQueryHandler(IPaymentRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<OperationResult<Payment>> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByIdAsync(request.Id);

            if (payment == null)
            {
                return OperationResult<Payment>.Failure("Không tìm thấy giao dịch.", 404);
            }

            return OperationResult<Payment>.Success(payment);
        }
    }
}