using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.Common.Models.Statistics;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Statistics.Queries;

namespace Tokki.UnitTests.UseCases.Statistics.Queries.GetTransactionsReport
{
    public class GetTransactionsReportHandler : IRequestHandler<GetTransactionsReportQuery, OperationResult<PagedResult<TransactionReportDto>>>
    {
        private readonly IStatisticsRepository _repo;
        public GetTransactionsReportHandler(IStatisticsRepository repo) => _repo = repo;

        public async Task<OperationResult<PagedResult<TransactionReportDto>>> Handle(GetTransactionsReportQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _repo.GetTransactionsAsync(
                request.Search, request.Status, request.FromDate, request.ToDate, request.Page, request.PageSize);

            var result = new PagedResult<TransactionReportDto>(items, totalCount, request.Page, request.PageSize);
            return OperationResult<PagedResult<TransactionReportDto>>.Success(result);
        }
    }
}
