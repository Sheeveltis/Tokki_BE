using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticPayment.DTOs;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetStatisticPaymentOverview
{
    public class GetStatisticPaymentOverviewHandler : IRequestHandler<GetStatisticPaymentOverviewQuery, OperationResult<StatisticPaymentOverviewDto>>
    {
        private readonly IStatisticPaymentRepository _repository;

        public GetStatisticPaymentOverviewHandler(IStatisticPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<StatisticPaymentOverviewDto>> Handle(GetStatisticPaymentOverviewQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetOverviewAsync(request.StartDate, request.EndDate);
            return OperationResult<StatisticPaymentOverviewDto>.Success(result);
        }
    }
}
