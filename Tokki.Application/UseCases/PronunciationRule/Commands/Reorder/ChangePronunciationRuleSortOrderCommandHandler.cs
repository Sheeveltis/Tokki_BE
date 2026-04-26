using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.PronunciationRule.Commands.Reorder
{
    public class ChangePronunciationRuleSortOrderCommandHandler : IRequestHandler<ChangePronunciationRuleSortOrderCommand, OperationResult<Unit>>
    {
        private readonly IPronunciationRuleRepository _repository;

        public ChangePronunciationRuleSortOrderCommandHandler(IPronunciationRuleRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<Unit>> Handle(ChangePronunciationRuleSortOrderCommand request, CancellationToken cancellationToken)
        {
            var targetRule = await _repository.GetByIdAsync(request.PronunciationRuleId);
            if (targetRule == null)
            {
                return OperationResult<Unit>.Failure(new Error("Rule.NotFound", "Quy tắc không tồn tại."), 404);
            }

            int oldOrder = targetRule.SortOrder;
            int newOrder = request.NewSortOrder;

            if (newOrder == oldOrder)
            {
                return OperationResult<Unit>.Success(Unit.Value);
            }

            // Lấy tất cả quy tắc để sắp xếp lại
            var allRules = await _repository.GetAllActiveRulesAsync(cancellationToken);
            
            // Đảm bảo newOrder không vượt quá giới hạn
            int maxOrder = allRules.Count > 0 ? allRules.Max(x => x.SortOrder) : 1;
            if (newOrder > maxOrder) newOrder = maxOrder;
            if (newOrder < 1) newOrder = 1;

            if (newOrder < oldOrder)
            {
                // Di chuyển lên: Các item từ newOrder đến oldOrder-1 sẽ tăng lên 1
                var affectedItems = allRules.Where(x => x.SortOrder >= newOrder && x.SortOrder < oldOrder && x.PronunciationRuleId != targetRule.PronunciationRuleId);
                foreach (var item in affectedItems)
                {
                    item.SortOrder++;
                    await _repository.UpdateAsync(item);
                }
            }
            else
            {
                // Di chuyển xuống: Các item từ oldOrder+1 đến newOrder sẽ giảm đi 1
                var affectedItems = allRules.Where(x => x.SortOrder > oldOrder && x.SortOrder <= newOrder && x.PronunciationRuleId != targetRule.PronunciationRuleId);
                foreach (var item in affectedItems)
                {
                    item.SortOrder--;
                    await _repository.UpdateAsync(item);
                }
            }

            targetRule.SortOrder = newOrder;
            targetRule.UpdateDate = DateTime.UtcNow;
            await _repository.UpdateAsync(targetRule);

            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<Unit>.Success(Unit.Value);
        }
    }
}
