using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.UserPronunciation.Commands.PracticePronunciationExample
{
    public class PracticePronunciationExampleCommandHandler : IRequestHandler<PracticePronunciationExampleCommand, OperationResult<bool>>
    {
        private readonly IUserPronunciationExampleProgressRepository _exampleProgressRepo;
        private readonly IUserPronunciationProgressRepository _ruleProgressRepo;
        private readonly IPronunciationExampleRepository _exampleRepo;
        private readonly IIdGeneratorService _idGen;

        public PracticePronunciationExampleCommandHandler(
            IUserPronunciationExampleProgressRepository exampleProgressRepo,
            IUserPronunciationProgressRepository ruleProgressRepo,
            IPronunciationExampleRepository exampleRepo,
            IIdGeneratorService idGen)
        {
            _exampleProgressRepo = exampleProgressRepo;
            _ruleProgressRepo = ruleProgressRepo;
            _exampleRepo = exampleRepo;
            _idGen = idGen;
        }

        public async Task<OperationResult<bool>> Handle(PracticePronunciationExampleCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra ví dụ có tồn tại không
            var example = await _exampleRepo.GetByIdAsync(request.PronunciationExampleId);
            if (example == null) return OperationResult<bool>.Failure(AppErrors.PronunciationExampleNotFound, 404);

            // 2. Cập nhật tiến độ của Ví dụ
            var exampleProgress = await _exampleProgressRepo.GetByUserIdAndExampleIdAsync(request.UserId, request.PronunciationExampleId);
            if (exampleProgress == null)
            {
                exampleProgress = new Tokki.Domain.Entities.UserPronunciationExampleProgress
                {
                    UserExampleProgressId = _idGen.GenerateCustom(15),
                    UserId = request.UserId,
                    PronunciationExampleId = request.PronunciationExampleId,
                    IsPracticed = true,
                    LastActivityAt = DateTime.UtcNow
                };
                await _exampleProgressRepo.AddAsync(exampleProgress);
            }
            else
            {
                exampleProgress.IsPracticed = true;
                exampleProgress.LastActivityAt = DateTime.UtcNow;
                _exampleProgressRepo.Update(exampleProgress);
            }

            // 3. Tính toán lại tiến độ của Quy tắc (Rule)
            var ruleId = example.PronunciationRuleId;
            var allExamplesInRule = await _exampleRepo.GetExamplesByRuleIdAsync(ruleId, cancellationToken);
            var totalExamples = allExamplesInRule.Count;

            // Lưu ExampleProgress trước để Count chính xác (hoặc count + 1 nếu là bản ghi mới)
            await _exampleProgressRepo.SaveChangesAsync(cancellationToken);

            var practicedCount = await _exampleProgressRepo.CountPracticedByUserIdAndRuleIdAsync(request.UserId, ruleId);

            // 4. Nếu hoàn thành toàn bộ ví dụ -> Cập nhật Rule Progress
            if (practicedCount >= totalExamples)
            {
                var ruleProgress = await _ruleProgressRepo.GetByUserIdAndRuleIdAsync(request.UserId, ruleId);
                if (ruleProgress == null)
                {
                    ruleProgress = new Tokki.Domain.Entities.UserPronunciationProgress
                    {
                        UserPronunciationProgressId = _idGen.GenerateCustom(15),
                        UserId = request.UserId,
                        PronunciationRuleId = ruleId,
                        IsLearned = true,
                        CompletedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow
                    };
                    await _ruleProgressRepo.AddAsync(ruleProgress);
                }
                else
                {
                    ruleProgress.IsLearned = true;
                    ruleProgress.CompletedAt = DateTime.UtcNow;
                    ruleProgress.LastActivityAt = DateTime.UtcNow;
                    _ruleProgressRepo.Update(ruleProgress);
                }
                await _ruleProgressRepo.SaveChangesAsync(cancellationToken);
            }

            return OperationResult<bool>.Success(true, 200, "Cập nhật tiến độ luyện tập thành công.");
        }
    }
}
