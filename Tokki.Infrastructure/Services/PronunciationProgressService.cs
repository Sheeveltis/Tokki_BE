using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Infrastructure.Services
{
    public class PronunciationProgressService : IPronunciationProgressService
    {
        private readonly IUserPronunciationExampleProgressRepository _exampleProgressRepo;
        private readonly IUserPronunciationProgressRepository _ruleProgressRepo;
        private readonly IPronunciationExampleRepository _exampleRepo;
        private readonly IIdGeneratorService _idGen;

        public PronunciationProgressService(
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

        public async Task UpdatePracticeProgressAsync(string userId, string exampleId, CancellationToken cancellationToken = default)
        {
            // 1. Kiểm tra ví dụ có tồn tại không
            var example = await _exampleRepo.GetByIdAsync(exampleId);
            if (example == null) return;

            // 2. Cập nhật tiến độ của Ví dụ
            var exampleProgress = await _exampleProgressRepo.GetByUserIdAndExampleIdAsync(userId, exampleId);
            if (exampleProgress == null)
            {
                exampleProgress = new UserPronunciationExampleProgress
                {
                    UserExampleProgressId = _idGen.GenerateCustom(15),
                    UserId = userId,
                    PronunciationExampleId = exampleId,
                    IsPracticed = true,
                    LastActivityAt = DateTime.UtcNow
                };
                await _exampleProgressRepo.AddAsync(exampleProgress);
            }
            else
            {
                // Nếu đã practiced rồi thì có thể return luôn hoặc cập nhật LastActivityAt
                if (exampleProgress.IsPracticed)
                {
                    exampleProgress.LastActivityAt = DateTime.UtcNow;
                    _exampleProgressRepo.Update(exampleProgress);
                    await _exampleProgressRepo.SaveChangesAsync(cancellationToken);
                    return; // Đã hoàn thành rồi thì không cần tính lại Rule Progress nữa (hoặc có thể tính lại cho chắc)
                }

                exampleProgress.IsPracticed = true;
                exampleProgress.LastActivityAt = DateTime.UtcNow;
                _exampleProgressRepo.Update(exampleProgress);
            }

            await _exampleProgressRepo.SaveChangesAsync(cancellationToken);

            // 3. Tính toán lại tiến độ của Quy tắc (Rule)
            var ruleId = example.PronunciationRuleId;
            var allExamplesInRule = await _exampleRepo.GetExamplesByRuleIdAsync(ruleId, cancellationToken);
            var totalExamples = allExamplesInRule.Count;

            var practicedCount = await _exampleProgressRepo.CountPracticedByUserIdAndRuleIdAsync(userId, ruleId);

            // 4. Nếu hoàn thành toàn bộ ví dụ -> Cập nhật Rule Progress
            if (practicedCount >= totalExamples)
            {
                var ruleProgress = await _ruleProgressRepo.GetByUserIdAndRuleIdAsync(userId, ruleId);
                if (ruleProgress == null)
                {
                    ruleProgress = new UserPronunciationProgress
                    {
                        UserPronunciationProgressId = _idGen.GenerateCustom(15),
                        UserId = userId,
                        PronunciationRuleId = ruleId,
                        IsLearned = true,
                        CompletedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow
                    };
                    await _ruleProgressRepo.AddAsync(ruleProgress);
                }
                else
                {
                    if (!ruleProgress.IsLearned)
                    {
                        ruleProgress.IsLearned = true;
                        ruleProgress.CompletedAt = DateTime.UtcNow;
                    }
                    ruleProgress.LastActivityAt = DateTime.UtcNow;
                    _ruleProgressRepo.Update(ruleProgress);
                }
                await _ruleProgressRepo.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
