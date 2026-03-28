using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.DeletePronunciationRule
{
    public class DeletePronunciationRuleCommandHandler : IRequestHandler<DeletePronunciationRuleCommand, OperationResult<bool>>
    {
        private readonly IPronunciationRuleRepository _repository;

        public DeletePronunciationRuleCommandHandler(IPronunciationRuleRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(DeletePronunciationRuleCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.PronunciationRuleId);
            if (entity == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy quy tắc phát âm.", 404);
            }

            await _repository.DeleteAsync(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true, 200, OperationMessages.DeleteSuccess("quy tắc phát âm"));
        }
    }
}
