using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.UpdatePronunciationRule
{
    public class UpdatePronunciationRuleCommandHandler : IRequestHandler<UpdatePronunciationRuleCommand, OperationResult<bool>>
    {
        private readonly IPronunciationRuleRepository _repository;

        public UpdatePronunciationRuleCommandHandler(IPronunciationRuleRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(UpdatePronunciationRuleCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.PronunciationRuleId);
            if (entity == null)
            {
                return OperationResult<bool>.Failure("Không tìm thấy quy tắc phát âm.", 404);
            }

            if (await _repository.IsRuleNameExistsAsync(request.RuleName.Trim(), request.PronunciationRuleId))
            {
                return OperationResult<bool>.Failure($"Tên quy tắc '{request.RuleName}' đã tồn tại.", 400);
            }

            entity.RuleName = request.RuleName.Trim();
            entity.Description = request.Description?.Trim();
            entity.Content = request.Content;
            entity.SortOrder = request.SortOrder;
            entity.UpdateBy = request.UpdateBy;
            entity.UpdateDate = DateTime.UtcNow;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true, 200, OperationMessages.UpdateSuccess("quy tắc phát âm"));
        }
    }
}
