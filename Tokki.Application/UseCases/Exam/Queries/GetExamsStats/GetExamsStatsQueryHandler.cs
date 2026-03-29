using MediatR;
using System.Text.Json;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Queries.GetExamsStats
{
    public class GetExamsStatsQueryHandler : IRequestHandler<GetExamsStatsQuery, OperationResult<PagedResult<AdminExamStatsDTO>>>
    {
        private readonly IExamRepository _examRepository;

        public GetExamsStatsQueryHandler(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<OperationResult<PagedResult<AdminExamStatsDTO>>> Handle(GetExamsStatsQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _examRepository.GetPagedWithStatsAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Type,
                request.Status,
                request.CreatorFilter,
                cancellationToken
            );

            var dtos = items.Select(e => {
                // Parse durations (in-memory)
                var durations = string.IsNullOrEmpty(e.SkillDurations)
                    ? new Dictionary<string, int>()
                    : JsonSerializer.Deserialize<Dictionary<string, int>>(e.SkillDurations) ?? new();

                // Group questions by skill (in-memory using the provided question numbers)
                var skillCounts = new Dictionary<string, int>();
                foreach (var part in e.TemplateParts)
                {
                    var count = e.QuestionNumbers.Count(qNo => qNo >= part.QuestionFrom && qNo <= part.QuestionTo);
                    if (count > 0)
                    {
                        var skillName = part.Skill.ToString();
                        if (skillCounts.ContainsKey(skillName)) 
                            skillCounts[skillName] += count;
                        else 
                            skillCounts[skillName] = count;
                    }
                }

                return new AdminExamStatsDTO
                {
                    ExamId = e.ExamId,
                    ExamTemplateId = e.ExamTemplateId,
                    ExamTemplateName = e.ExamTemplateName,
                    Title = e.Title,
                    Type = e.Type,
                    Status = e.Status,
                    Duration = e.Duration,
                    SkillDurations = durations,
                    CreatedAt = e.CreatedAt.AddHours(7),
                    TotalQuestions = e.TotalQuestions,
                    
                    // Stats (already calculated in SQL)
                    TotalParticipants = e.TotalParticipants,
                    AverageScore = Math.Round(e.AverageScore, 2),
                    TopScore = e.TopScore,
                    PdfDownloadCount = e.PdfDownloadCount,
                    AverageDurationMinutes = Math.Round(e.AverageDurationMinutes, 1),
                    InProgressCount = e.InProgressCount,
                    CompletedCount = e.CompletedCount,
                    SkillQuestionCounts = skillCounts
                };
            }).ToList();

            var pagedResult = PagedResult<AdminExamStatsDTO>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<AdminExamStatsDTO>>.Success(pagedResult);
        }
    }
}
