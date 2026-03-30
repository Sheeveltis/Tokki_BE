using MediatR;
using System.Text.Json;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Exam.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Queries.GetExamDetailStats
{
    public class GetExamDetailStatsQueryHandler : IRequestHandler<GetExamDetailStatsQuery, OperationResult<AdminExamStatsDTO>>
    {
        private readonly IExamRepository _examRepository;

        public GetExamDetailStatsQueryHandler(IExamRepository examRepository)
        {
            _examRepository = examRepository;
        }

        public async Task<OperationResult<AdminExamStatsDTO>> Handle(GetExamDetailStatsQuery request, CancellationToken cancellationToken)
        {
            var e = await _examRepository.GetExamStatsByIdAsync(request.ExamId, cancellationToken);

            if (e == null)
            {
                return OperationResult<AdminExamStatsDTO>.Failure("Exam not found or has no stats.");
            }

            // Parse durations (in-memory)
            var durations = string.IsNullOrEmpty(e.SkillDurations)
                ? new Dictionary<string, int>()
                : JsonSerializer.Deserialize<Dictionary<string, int>>(e.SkillDurations) ?? new();

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

            var dto = new AdminExamStatsDTO
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
                MaxScore = e.MaxScore,
                TotalParticipants = e.TotalParticipants,
                AverageScore = Math.Round(e.AverageScore, 2),
                TopScore = e.TopScore,
                PdfDownloadCount = e.PdfDownloadCount,
                AverageDurationMinutes = Math.Round(e.AverageDurationMinutes, 1),
                InProgressCount = e.InProgressCount,
                CompletedCount = e.CompletedCount,
                SkillQuestionCounts = skillCounts
            };

            return OperationResult<AdminExamStatsDTO>.Success(dto);
        }
    }
}
