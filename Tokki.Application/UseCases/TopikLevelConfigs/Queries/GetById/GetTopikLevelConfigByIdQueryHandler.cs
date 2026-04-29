using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.TopikLevelConfigs.DTOs;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Queries.GetById
{
    public class GetTopikLevelConfigByIdQueryHandler : IRequestHandler<GetTopikLevelConfigByIdQuery, OperationResult<TopikLevelConfigDto>>
    {
        private readonly ITopikLevelConfigRepository _repository;

        public GetTopikLevelConfigByIdQueryHandler(ITopikLevelConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<TopikLevelConfigDto>> Handle(GetTopikLevelConfigByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity == null)
            {
                return OperationResult<TopikLevelConfigDto>.Failure("Không tìm thấy cấu hình.", 404);
            }

            var dto = new TopikLevelConfigDto
            {
                TopikLevelConfigID = entity.TopikLevelConfigID,
                TargetAimLevel = entity.TargetAimLevel,
                DisplayName = entity.DisplayName,
                PassScore = entity.PassScore,
                TotalScore = entity.TotalScore,
                ExamGroup = entity.ExamGroup,
                ConfigKey = entity.ConfigKey,
                ListeningMaxQuestions = entity.ListeningMaxQuestions,
                ListeningMaxScore = entity.ListeningMaxScore,
                ReadingMaxQuestions = entity.ReadingMaxQuestions,
                ReadingMaxScore = entity.ReadingMaxScore,
                WritingMaxQuestions = entity.WritingMaxQuestions,
                WritingMaxScore = entity.WritingMaxScore,
                TargetListeningQuestions = entity.TargetListeningQuestions,
                TargetListeningScore = entity.TargetListeningScore,
                TargetReadingQuestions = entity.TargetReadingQuestions,
                TargetReadingScore = entity.TargetReadingScore,
                TargetWritingQuestions = entity.TargetWritingQuestions,
                TargetWritingScore = entity.TargetWritingScore,
                Strategy = entity.Strategy,
                IsActive = entity.IsActive,
                SortOrder = entity.SortOrder,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };

            return OperationResult<TopikLevelConfigDto>.Success(dto);
        }
    }
}
