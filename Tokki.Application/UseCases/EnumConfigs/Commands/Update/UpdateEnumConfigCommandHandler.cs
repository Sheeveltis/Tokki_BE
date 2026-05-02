using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.EnumConfigs.Commands.Update
{
    public class UpdateEnumConfigCommandHandler : IRequestHandler<UpdateEnumConfigCommand, OperationResult<bool>>
    {
        private readonly IEnumConfigRepository _enumConfigRepository;

        public UpdateEnumConfigCommandHandler(IEnumConfigRepository enumConfigRepository)
        {
            _enumConfigRepository = enumConfigRepository;
        }

        public async Task<OperationResult<bool>> Handle(UpdateEnumConfigCommand request, CancellationToken cancellationToken)
        {
            var config = await _enumConfigRepository.FirstOrDefaultAsync(x => x.Id == request.Id);
            if (config == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy cấu hình enum.", 404);
            }

            // Kiểm tra trùng Key hoặc Value trong cùng Group (trừ chính nó)
            var duplicateKey = await _enumConfigRepository.FirstOrDefaultAsync(x => x.GroupCode == config.GroupCode && x.Key == request.Key && x.Id != request.Id);
            if (duplicateKey != null)
            {
                return OperationResult<bool>.Failure("Key đã tồn tại trong nhóm này.", 400);
            }

            var duplicateValue = await _enumConfigRepository.FirstOrDefaultAsync(x => x.GroupCode == config.GroupCode && x.Value == request.Value && x.Id != request.Id);
            if (duplicateValue != null)
            {
                return OperationResult<bool>.Failure("Value đã tồn tại trong nhóm này.", 400);
            }

            config.Key = request.Key;
            config.Value = request.Value;
            config.Label = request.Label;
            config.Description = request.Description;
            config.SortOrder = request.SortOrder;
            config.IsActive = request.IsActive;
            config.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _enumConfigRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
