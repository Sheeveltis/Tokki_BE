using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Commands.Delete
{
    public class DeleteTopikLevelConfigCommandHandler : IRequestHandler<DeleteTopikLevelConfigCommand, OperationResult<bool>>
    {
        private readonly ITopikLevelConfigRepository _repository;

        public DeleteTopikLevelConfigCommandHandler(ITopikLevelConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(DeleteTopikLevelConfigCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null) return OperationResult<bool>.Failure("Không tìm thấy cấu hình.", 404);

            _repository.Delete(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
