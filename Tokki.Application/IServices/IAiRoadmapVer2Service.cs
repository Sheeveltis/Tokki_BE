using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.IServices
{
    public interface IAiRoadmapVer2Service
    {
        Task<List<string>> SequenceWeaknessesAsync(
            List<string> questionTypeIds,
            CurrentTopikLevel currentLevel,
            TargetAimLevel targetLevel,
            List<QuestionTypeMenuDto> typeMenu,
            CancellationToken token = default);

        Task<AiRoadmapResponse?> GenerateStudyPlanAsync(
            TargetAimLevel targetAim,
            CurrentTopikLevel currentLevel,
            int weekIndex,
            int totalWeeks,
            List<string> focusTypeIds,
            List<string> deferredTypeIds,
            List<QuestionTypeMenuDto> weakTypeInfos,
            List<QuestionTypeMenuDto> fullMenu,
            CancellationToken token = default);
    }
}
