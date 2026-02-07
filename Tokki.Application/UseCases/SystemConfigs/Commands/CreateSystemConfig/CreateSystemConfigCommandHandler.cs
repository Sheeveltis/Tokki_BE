using FluentValidation;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.SystemConfigs.Commands.Create
{
    public class CreateSystemConfigCommandHandler : IRequestHandler<CreateSystemConfigCommand, OperationResult<string>>
    {
        private readonly ISystemConfigRepository _repository;
        private readonly IValidator<CreateSystemConfigCommand> _validator;

        public CreateSystemConfigCommandHandler(ISystemConfigRepository repository, IValidator<CreateSystemConfigCommand> validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<OperationResult<string>> Handle(CreateSystemConfigCommand request, CancellationToken cancellationToken)
        {
            // Check trùng Key
            var existingConfig = await _repository.GetByKeyAsync(request.Key);
            if (existingConfig != null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.ConfigKeyDuplicated });
            }

            var newConfig = new SystemConfig
            {
                Key = request.Key,
                Value = request.Value,
                Description = request.Description,
                DataType = request.DataType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };

            await _repository.AddAsync(newConfig);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(newConfig.Key, 201, "Tạo cấu hình thành công");
        }
    }
}