using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;

namespace Tokki.Application.UseCases.UserPronunciation.Commands.CompletePronunciationRule
{
    public class CompletePronunciationRuleCommandHandler : IRequestHandler<CompletePronunciationRuleCommand, OperationResult<bool>>
    {
        private readonly IUserPronunciationProgressRepository _progressRepo;
        private readonly IPronunciationRuleRepository _ruleRepo;
        private readonly IIdGeneratorService _idGen;

        public CompletePronunciationRuleCommandHandler(
            IUserPronunciationProgressRepository progressRepo,
            IPronunciationRuleRepository ruleRepo,
            IIdGeneratorService idGen)
        {
            _progressRepo = progressRepo;
            _ruleRepo = ruleRepo;
            _idGen = idGen;
        }

        public async Task<OperationResult<bool>> Handle(CompletePronunciationRuleCommand request, CancellationToken cancellationToken)
        {
            var rule = await _ruleRepo.GetByIdAsync(request.PronunciationRuleId);

            if (rule == null)
            {
                return OperationResult<bool>.Failure(
                    AppErrors.PronunciationRuleNotFound,
                    404
                );
            }

            var progress = await _progressRepo.GetByUserIdAndRuleIdAsync(request.UserId, request.PronunciationRuleId);
            
            if (progress == null)
            {
                progress = new Domain.Entities.UserPronunciationProgress
                {
                    UserPronunciationProgressId = _idGen.GenerateCustom(15),
                    UserId = request.UserId,
                    PronunciationRuleId = request.PronunciationRuleId,
                    IsLearned = true,
                    CompletedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow
                };

                await _progressRepo.AddAsync(progress);
            }
            else
            {
                progress.IsLearned = true;
                progress.CompletedAt = DateTime.UtcNow;
                progress.LastActivityAt = DateTime.UtcNow;
                _progressRepo.Update(progress);
            }

            try
            {
                await _progressRepo.SaveChangesAsync(cancellationToken);
                return OperationResult<bool>.Success(true, 200, OperationMessages.UpdateSuccess("tiến độ học phát âm"));
            }
            catch (Exception ex)
            {
                return OperationResult<bool>.Failure(
                    new Error("Database.SaveError", ex.Message),
                    500
                );
            }
        }
    }
}
