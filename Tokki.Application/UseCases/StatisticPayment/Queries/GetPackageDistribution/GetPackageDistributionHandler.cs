using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.StatisticPayment.DTOs;

namespace Tokki.Application.UseCases.StatisticPayment.Queries.GetPackageDistribution
{
    public class GetPackageDistributionHandler : IRequestHandler<GetPackageDistributionQuery, OperationResult<List<PackageDistributionDto>>>
    {
        private readonly IStatisticPaymentRepository _repository;

        public GetPackageDistributionHandler(IStatisticPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<List<PackageDistributionDto>>> Handle(GetPackageDistributionQuery request, CancellationToken cancellationToken)
        {
            var result = await _repository.GetPackageDistributionAsync(request.StartDate, request.EndDate);
            return OperationResult<List<PackageDistributionDto>>.Success(result);
        }
    }
}
