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
        private readonly IPronunciationExampleRepository _exampleRepo;
        private readonly IIdGeneratorService _idGen;

        public PronunciationProgressService(
            IUserPronunciationExampleProgressRepository exampleProgressRepo,
            IPronunciationExampleRepository exampleRepo,
            IIdGeneratorService idGen)
        {
            _exampleProgressRepo = exampleProgressRepo;
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
                if (exampleProgress.IsPracticed)
                {
                    exampleProgress.LastActivityAt = DateTime.UtcNow;
                    _exampleProgressRepo.Update(exampleProgress);
                    await _exampleProgressRepo.SaveChangesAsync(cancellationToken);
                    return;
                }

                exampleProgress.IsPracticed = true;
                exampleProgress.LastActivityAt = DateTime.UtcNow;
                _exampleProgressRepo.Update(exampleProgress);
            }

            await _exampleProgressRepo.SaveChangesAsync(cancellationToken);
        }
    }
}
