using System.Collections.Generic;
using System.Threading.Tasks;
using Tokki.Application.UseCases.Roadmap.DTOs;
using Tokki.Domain.Enums;
using Tokki.Application.IRepositories;

namespace Tokki.Application.IServices
{
    public interface IAiRoadmapService
    {
        Task<AiRoadmapResponse?> GenerateStudyPlanAsync(
            TargetAimLevel target,
            CurrentTopikLevel currentLevel,
            int durationDays,
            List<string> weaknesses,
            List<QuestionTypeMenuItem> weakTypeInfos,    
            List<QuestionTypeMenuItem> questionTypeMenu,
            int typesPerWeek,   
            int totalWeeks); 

        Task<AiRoadmapResponse?> GenerateNextWeekPlanAsync(
            TargetAimLevel target,
            CurrentTopikLevel currentLevel,
            int nextWeekIndex,
            int examScorePercent,
            List<string> reviewTypes,
            List<string> persistentFailTypes,
            List<string> originalWeaknesses,
            List<QuestionTypeMenuItem> weakTypeInfos,    
            List<QuestionTypeMenuItem> questionTypeMenu);
        Task<string?> GenerateEntranceFeedbackAsync(
            TargetAimLevel targetAim,
            int readingWeakCount,
            int listeningWeakCount,
            int writingWeakCount,
            List<string> readingNames,
            List<string> listeningNames,
            List<string> writingNames,
            int recommendedDays);
        Task<List<string>> SequenceWeaknessesAsync(
            List<string> questionTypeIds,
            CurrentTopikLevel currentLevel,
            TargetAimLevel targetLevel,
            List<QuestionTypeMenuItem> typeMenu,
            CancellationToken token = default);
    }
}