using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Commands.Update
{
    public class UpdateTopikLevelConfigCommandHandler : IRequestHandler<UpdateTopikLevelConfigCommand, OperationResult<bool>>
    {
        private readonly ITopikLevelConfigRepository _repository;

        public UpdateTopikLevelConfigCommandHandler(ITopikLevelConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<bool>> Handle(UpdateTopikLevelConfigCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.TopikLevelConfigID);
            if (entity == null) return OperationResult<bool>.Failure("Không tìm thấy cấu hình.", 404);

            // Check unique constraints for other records
            var existingLevel = await _repository.FirstOrDefaultAsync(x => x.TargetAimLevel == request.TargetAimLevel && x.TopikLevelConfigID != request.TopikLevelConfigID, cancellationToken);
            if (existingLevel != null) return OperationResult<bool>.Failure("Cấp độ mục tiêu này đã tồn tại.", 400);

            var existingKey = await _repository.FirstOrDefaultAsync(x => x.ConfigKey == request.ConfigKey && x.TopikLevelConfigID != request.TopikLevelConfigID, cancellationToken);
            if (existingKey != null) return OperationResult<bool>.Failure("Config Key này đã tồn tại.", 400);

            entity.TargetAimLevel = request.TargetAimLevel;
            entity.DisplayName = request.DisplayName;
            entity.PassScore = request.PassScore;
            entity.TotalScore = request.TotalScore;
            entity.ExamGroup = request.ExamGroup;
            entity.ConfigKey = request.ConfigKey;
            entity.ListeningMaxQuestions = request.ListeningMaxQuestions;
            entity.ListeningMaxScore = request.ListeningMaxScore;
            entity.ReadingMaxQuestions = request.ReadingMaxQuestions;
            entity.ReadingMaxScore = request.ReadingMaxScore;
            entity.WritingMaxQuestions = request.WritingMaxQuestions;
            entity.WritingMaxScore = request.WritingMaxScore;
            entity.TargetListeningQuestions = request.TargetListeningQuestions;
            entity.TargetListeningScore = request.TargetListeningScore;
            entity.TargetReadingQuestions = request.TargetReadingQuestions;
            entity.TargetReadingScore = request.TargetReadingScore;
            entity.TargetWritingQuestions = request.TargetWritingQuestions;
            entity.TargetWritingScore = request.TargetWritingScore;
            entity.Strategy = request.Strategy;
            entity.IsActive = request.IsActive;
            entity.SortOrder = request.SortOrder;
            entity.UpdatedAt = DateTime.UtcNow.AddHours(7);

            _repository.Update(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true);
        }
    }
}
