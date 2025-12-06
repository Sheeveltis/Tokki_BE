using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.SystemConfigs.Commands.Update
{
    public class UpdateSystemConfigCommandHandler : IRequestHandler<UpdateSystemConfigCommand, OperationResult<string>>
    {
        private readonly ISystemConfigRepository _repository;

        public UpdateSystemConfigCommandHandler(ISystemConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<string>> Handle(UpdateSystemConfigCommand request, CancellationToken cancellationToken)
        {
            // 1. Tìm Config theo Key
            var config = await _repository.GetByKeyAsync(request.Key);
            if (config == null)
            {
                return OperationResult<string>.Failure($"Không tìm thấy cấu hình với Key '{request.Key}'", 404);
            }

            // 2. Cập nhật dữ liệu
            config.Value = request.Value;
            config.Description = request.Description;
            config.IsActive = request.IsActive;
            config.UpdatedAt = DateTime.UtcNow;

            // 3. Lưu (EF Core tự tracking sự thay đổi, chỉ cần SaveChanges)
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(config.Key, 200, "Cập nhật thành công");
        }
    }
}