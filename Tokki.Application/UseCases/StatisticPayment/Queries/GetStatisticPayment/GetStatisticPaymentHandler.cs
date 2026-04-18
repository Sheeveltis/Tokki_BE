using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticPayment.DTOs;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetStatisticPayment
{
    public class GetStatisticPaymentHandler : IRequestHandler<GetStatisticPaymentQuery, OperationResult<PagedResult<StatisticPaymentDto>>>
    {
        private readonly IStatisticPaymentRepository _statisticPaymentRepository;

        public GetStatisticPaymentHandler(IStatisticPaymentRepository statisticPaymentRepository)
        {
            _statisticPaymentRepository = statisticPaymentRepository;
        }

        public async Task<OperationResult<PagedResult<StatisticPaymentDto>>> Handle(GetStatisticPaymentQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _statisticPaymentRepository.GetStatisticPaymentsAsync(
                request.SearchTerm,
                request.Status,
                request.HasTransaction,
                request.VipPackageId,
                request.FromDate,
                request.ToDate,
                request.PageNumber,
                request.PageSize);

            var result = PagedResult<StatisticPaymentDto>.Create(items, totalCount, request.PageNumber, request.PageSize);

            return OperationResult<PagedResult<StatisticPaymentDto>>.Success(result);
        }
    }
}
