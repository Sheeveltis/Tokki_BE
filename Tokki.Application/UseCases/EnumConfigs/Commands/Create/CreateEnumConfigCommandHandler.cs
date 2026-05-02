using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.EnumConfigs.Commands.Create
{
    public class CreateEnumConfigCommandHandler : IRequestHandler<CreateEnumConfigCommand, OperationResult<int>>
    {
        private readonly IEnumConfigRepository _enumConfigRepository;

        public CreateEnumConfigCommandHandler(IEnumConfigRepository enumConfigRepository)
        {
            _enumConfigRepository = enumConfigRepository;
        }

        public async Task<OperationResult<int>> Handle(CreateEnumConfigCommand request, CancellationToken cancellationToken)
        {
            // Kiểm tra trùng Key hoặc Value trong cùng Group
            var existingKey = await _enumConfigRepository.FirstOrDefaultAsync(x => x.GroupCode == request.GroupCode && x.Key == request.Key);
            if (existingKey != null)
            {
                return OperationResult<int>.Failure("Key đã tồn tại trong nhóm này.", 400);
            }

            var existingValue = await _enumConfigRepository.FirstOrDefaultAsync(x => x.GroupCode == request.GroupCode && x.Value == request.Value);
            if (existingValue != null)
            {
                return OperationResult<int>.Failure("Value đã tồn tại trong nhóm này.", 400);
            }

            var enumConfig = new EnumConfig
            {
                GroupCode = request.GroupCode,
                Key = request.Key,
                Value = request.Value,
                Label = request.Label,
                Description = request.Description,
                SortOrder = request.SortOrder,
                IsActive = true
            };

            await _enumConfigRepository.AddAsync(enumConfig);
            await _enumConfigRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<int>.Success(enumConfig.Id, 201);
        }
    }
}
