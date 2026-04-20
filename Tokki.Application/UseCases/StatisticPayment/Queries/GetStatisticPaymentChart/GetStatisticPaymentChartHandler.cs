using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticPayment.DTOs;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetStatisticPaymentChart
{
    public class GetStatisticPaymentChartHandler : IRequestHandler<GetStatisticPaymentChartQuery, OperationResult<List<StatisticPaymentChartDto>>>
    {
        private readonly IStatisticPaymentRepository _repository;

        public GetStatisticPaymentChartHandler(IStatisticPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<List<StatisticPaymentChartDto>>> Handle(GetStatisticPaymentChartQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetRevenueChartAsync(request.Year);
            return OperationResult<List<StatisticPaymentChartDto>>.Success(result);
        }
    }
}
