using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.VipPackages.Commands.CreateVipPackage
{
    public class CreateVipPackageHandler : IRequestHandler<CreateVipPackageCommand, OperationResult<string>>
    {
        private readonly IVipPackageRepository _repository;
        private readonly IIdGeneratorService _idGenerator; 

        public CreateVipPackageHandler(IVipPackageRepository repository, IIdGeneratorService idGenerator)
        {
            _repository = repository;
            _idGenerator = idGenerator;
        }

        public async Task<OperationResult<string>> Handle(CreateVipPackageCommand request, CancellationToken cancellationToken)
        {
            if (request.Price < 0) return OperationResult<string>.Failure("Giá không hợp lệ.");
            if (request.DurationDays <= 0) return OperationResult<string>.Failure("Thời hạn phải lớn hơn 0.");

            var package = new VipPackage
            {
                Id = _idGenerator.GenerateCustom(21), 
                Name = request.Name,
                Price = request.Price,
                DurationDays = request.DurationDays,
                Description = request.Description,
                IsActive = false, 
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(package);
            return OperationResult<string>.Success(package.Id);
        }
    }
}