using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.PronunciationExample.Commands.ChangeSortOrder
{
    public class ChangePronunciationExampleSortOrderCommandHandler : IRequestHandler<ChangePronunciationExampleSortOrderCommand, OperationResult<Unit>>
    {
        private readonly IPronunciationExampleRepository _exampleRepo;

        public ChangePronunciationExampleSortOrderCommandHandler(IPronunciationExampleRepository exampleRepo)
        {
            _exampleRepo = exampleRepo;
        }

        public async Task<OperationResult<Unit>> Handle(ChangePronunciationExampleSortOrderCommand request, CancellationToken cancellationToken)
        {
            var targetExample = await _exampleRepo.GetByIdAsync(request.ExampleId);
            if (targetExample == null)
            {
                return OperationResult<Unit>.Failure(new Error("Example.NotFound", "Ví dụ không tồn tại."), 404);
            }

            var ruleId = targetExample.PronunciationRuleId;
            int oldOrder = targetExample.SortOrder;
            int newOrder = request.NewSortOrder;

            if (newOrder == oldOrder)
            {
                return OperationResult<Unit>.Success(Unit.Value);
            }

            // Lấy tất cả ví dụ cùng Rule để sắp xếp lại
            var allExamples = await _exampleRepo.GetExamplesByRuleIdAsync(ruleId, cancellationToken);
            
            // Đảm bảo newOrder không vượt quá giới hạn thực tế
            int maxOrder = allExamples.Count > 0 ? allExamples.Max(x => x.SortOrder) : 1;
            if (newOrder > maxOrder) newOrder = maxOrder;
            if (newOrder < 1) newOrder = 1;

            if (newOrder < oldOrder)
            {
                // Di chuyển lên (vd: từ 9 về 2): Các item từ 2 đến 8 sẽ tăng lên 1 đơn vị
                var affectedItems = allExamples.Where(x => x.SortOrder >= newOrder && x.SortOrder < oldOrder && x.ExampleId != targetExample.ExampleId);
                foreach (var item in affectedItems)
                {
                    item.SortOrder++;
                    await _exampleRepo.UpdateAsync(item);
                }
            }
            else
            {
                // Di chuyển xuống (vd: từ 2 lên 9): Các item từ 3 đến 9 sẽ giảm đi 1 đơn vị
                var affectedItems = allExamples.Where(x => x.SortOrder > oldOrder && x.SortOrder <= newOrder && x.ExampleId != targetExample.ExampleId);
                foreach (var item in affectedItems)
                {
                    item.SortOrder--;
                    await _exampleRepo.UpdateAsync(item);
                }
            }

            targetExample.SortOrder = newOrder;
            targetExample.UpdateDate = DateTime.UtcNow;
            await _exampleRepo.UpdateAsync(targetExample);

            await _exampleRepo.SaveChangesAsync(cancellationToken);

            return OperationResult<Unit>.Success(Unit.Value);
        }
    }
}
