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
            var config = await _repository.GetByKeyAsync(request.Key);
            if (config == null)
            {
                return OperationResult<string>.Failure(new List<Error> { AppErrors.ConfigNotFound });
            }

            config.Value = request.Value;
            config.Description = request.Description;
            config.IsActive = request.IsActive;
            config.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(config.Key, 200, "Cập nhật thành công");
        }
    }
}