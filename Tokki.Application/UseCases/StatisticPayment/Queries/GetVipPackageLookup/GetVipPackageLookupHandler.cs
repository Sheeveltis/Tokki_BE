using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticPayment.DTOs;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetVipPackageLookup
{
    public class GetVipPackageLookupHandler : IRequestHandler<GetVipPackageLookupQuery, OperationResult<List<VipPackageLookupDto>>>
    {
        private readonly IStatisticPaymentRepository _repository;

        public GetVipPackageLookupHandler(IStatisticPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<List<VipPackageLookupDto>>> Handle(GetVipPackageLookupQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetVipPackagesLookupAsync();
            return OperationResult<List<VipPackageLookupDto>>.Success(result);
        }
    }
}
