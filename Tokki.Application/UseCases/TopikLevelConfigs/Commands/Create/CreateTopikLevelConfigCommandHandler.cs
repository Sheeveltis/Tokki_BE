using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Commands.Create
{
    public class CreateTopikLevelConfigCommandHandler : IRequestHandler<CreateTopikLevelConfigCommand, OperationResult<int>>
    {
        private readonly ITopikLevelConfigRepository _repository;

        public CreateTopikLevelConfigCommandHandler(ITopikLevelConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<int>> Handle(CreateTopikLevelConfigCommand request, CancellationToken cancellationToken)
        {
            // Check unique TargetAimLevel or ConfigKey
            var existingLevel = await _repository.FirstOrDefaultAsync(x => x.TargetAimLevel == request.TargetAimLevel, cancellationToken);
            if (existingLevel != null) return OperationResult<int>.Failure("Cấp độ mục tiêu này đã tồn tại.", 400);

            var existingKey = await _repository.FirstOrDefaultAsync(x => x.ConfigKey == request.ConfigKey, cancellationToken);
            if (existingKey != null) return OperationResult<int>.Failure("Config Key này đã tồn tại.", 400);

            var entity = new TopikLevelConfig
            {
                TargetAimLevel = request.TargetAimLevel,
                DisplayName = request.DisplayName,
                PassScore = request.PassScore,
                TotalScore = request.TotalScore,
                ExamGroup = request.ExamGroup,
                ConfigKey = request.ConfigKey,
                ListeningMaxQuestions = request.ListeningMaxQuestions,
                ListeningMaxScore = request.ListeningMaxScore,
                ReadingMaxQuestions = request.ReadingMaxQuestions,
                ReadingMaxScore = request.ReadingMaxScore,
                WritingMaxQuestions = request.WritingMaxQuestions,
                WritingMaxScore = request.WritingMaxScore,
                TargetListeningQuestions = request.TargetListeningQuestions,
                TargetListeningScore = request.TargetListeningScore,
                TargetReadingQuestions = request.TargetReadingQuestions,
                TargetReadingScore = request.TargetReadingScore,
                TargetWritingQuestions = request.TargetWritingQuestions,
                TargetWritingScore = request.TargetWritingScore,
                Strategy = request.Strategy,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder
            };

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync(cancellationToken);

            return OperationResult<int>.Success(entity.TopikLevelConfigID, 201);
        }
    }
}
