using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.TopikLevelConfigs.DTOs;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Queries.GetAll
{
    public class GetAllTopikLevelConfigsQueryHandler : IRequestHandler<GetAllTopikLevelConfigsQuery, OperationResult<PagedResult<TopikLevelConfigDto>>>
    {
        private readonly ITopikLevelConfigRepository _repository;

        public GetAllTopikLevelConfigsQueryHandler(ITopikLevelConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<TopikLevelConfigDto>>> Handle(GetAllTopikLevelConfigsQuery request, CancellationToken cancellationToken)
        {
            var (entities, totalCount) = await _repository.GetPagedAsync(
                request.PageNumber, 
                request.PageSize, 
                request.SearchText, 
                request.ExamGroup, 
                request.IsActive);
            
            var dtos = entities.Select(e => new TopikLevelConfigDto
            {
                TopikLevelConfigID = e.TopikLevelConfigID,
                TargetAimLevel = e.TargetAimLevel,
                DisplayName = e.DisplayName,
                PassScore = e.PassScore,
                TotalScore = e.TotalScore,
                ExamGroup = e.ExamGroup,
                ConfigKey = e.ConfigKey,
                ListeningMaxQuestions = e.ListeningMaxQuestions,
                ListeningMaxScore = e.ListeningMaxScore,
                ReadingMaxQuestions = e.ReadingMaxQuestions,
                ReadingMaxScore = e.ReadingMaxScore,
                WritingMaxQuestions = e.WritingMaxQuestions,
                WritingMaxScore = e.WritingMaxScore,
                TargetListeningQuestions = e.TargetListeningQuestions,
                TargetListeningScore = e.TargetListeningScore,
                TargetReadingQuestions = e.TargetReadingQuestions,
                TargetReadingScore = e.TargetReadingScore,
                TargetWritingQuestions = e.TargetWritingQuestions,
                TargetWritingScore = e.TargetWritingScore,
                Strategy = e.Strategy,
                IsActive = e.IsActive,
                SortOrder = e.SortOrder,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt
            }).ToList();

            var pagedResult = PagedResult<TopikLevelConfigDto>.Create(dtos, totalCount, request.PageNumber, request.PageSize);
            return OperationResult<PagedResult<TopikLevelConfigDto>>.Success(pagedResult);
        }
    }
}
