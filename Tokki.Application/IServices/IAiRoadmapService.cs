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
            List<GrammarMenuItem> grammarMenu,           
            List<QuestionTypeMenuItem> questionTypeMenu); 

        Task<AiRoadmapResponse?> GenerateNextWeekPlanAsync(
            TargetAimLevel target,
            CurrentTopikLevel currentLevel,
            int nextWeekIndex,
            int examScorePercent,
            List<string> reviewTypes,
            List<string> persistentFailTypes,
            List<string> originalWeaknesses,
            List<QuestionTypeMenuItem> weakTypeInfos,    
            List<GrammarMenuItem> grammarMenu,          
            List<QuestionTypeMenuItem> questionTypeMenu); 
    }
}