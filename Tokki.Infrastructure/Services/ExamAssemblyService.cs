using MediatR;
using Microsoft.EntityFrameworkCore;
using Tokki.Application.Common.Models;
using Tokki.Application.IServices;
using Tokki.Application.UseCases.Exam.Commands.CreateExam;
using Tokki.Application.UseCases.ExamTemplates.Commands.AddTemplateParts;
using Tokki.Application.UseCases.ExamTemplates.Commands.CreateExamTemplate;
using Tokki.Application.UseCases.ExamTemplates.DTOs;
using Tokki.Domain.Enums;
using Tokki.Infrastructure.Data;

namespace Tokki.Infrastructure.Services
{
    public class ExamAssemblyService : IExamAssemblyService
    {
        private readonly IMediator _mediator;
        private readonly TokkiDbContext _context;
        private const string AI_SYSTEM_ID = "AI_SYSTEM_ACCOUNT";
        private const int QUESTIONS_PER_PART = 10; 

        public ExamAssemblyService(IMediator mediator, TokkiDbContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        public async Task<OperationResult<string>> GenerateWeeklyExamAsync(
            string templateId,
            string userId,
            int weekIndex,
            List<string> weakQuestionTypeIds,
            DifficultyLevel targetLevel,
            CancellationToken cancellationToken = default)
        {
            var createExamCmd = new CreateExamCommand
            {
                Title = $"Weekly Exam - Week {weekIndex} ({DateTime.UtcNow:dd/MM})",
                Duration = 60, 
                ExamTemplateId = templateId,
                CreatedBy = userId 
            };
            var result = await _mediator.Send(createExamCmd, cancellationToken);

            return result;
        }
        public async Task<OperationResult<string>> GenerateWeeklyExamFromScopeAsync(
            string userId,
            int weekIndex,
            List<string> weeklyQuestionTypeIds,
            CancellationToken cancellationToken)
        {
            var targetTypes = weeklyQuestionTypeIds.Distinct().ToList();
            if (!targetTypes.Any()) return OperationResult<string>.Failure("Không có kiến thức nào trong tuần để kiểm tra.", 400);

            var existingExamId = await FindMatchingExamAsync(targetTypes, cancellationToken);
            if (!string.IsNullOrEmpty(existingExamId))
            {
                return OperationResult<string>.Success(existingExamId);
            }

            string templateName = $"AI Generated Template - Week {weekIndex} - {Guid.NewGuid().ToString().Substring(0, 8)}";

            var createTemplateCmd = new CreateExamTemplateCommand
            {
                Name = templateName,
                Description = "Cấu trúc đề thi được AI sinh tự động dựa trên lộ trình học.",
                Type = ExamType.WeeklyAssessment, 
            };

            var templateResult = await _mediator.Send(createTemplateCmd, cancellationToken);
            if (!templateResult.IsSuccess) return OperationResult<string>.Failure($"Lỗi tạo Template: {templateResult.Message}");

            string newTemplateId = templateResult.Data;

            var partsDto = new List<CreateTemplatePartDto>();
            int currentQuestionIndex = 1;

            foreach (var typeId in targetTypes)
            {
                var qType = await _context.QuestionTypes.FindAsync(typeId);
                QuestionSkill skill = qType?.Skill ?? QuestionSkill.Reading;
                partsDto.Add(new CreateTemplatePartDto
                {
                    PartTitle = $"Part for {typeId}",
                    QuestionTypeId = typeId,
                    Skill = skill,
                    QuestionFrom = currentQuestionIndex,
                    QuestionTo = currentQuestionIndex + QUESTIONS_PER_PART - 1,
                    Mark = 1,
                    Instruction = "Chọn đáp án đúng nhất."
                });

                currentQuestionIndex += QUESTIONS_PER_PART;
            }

            var addPartsCmd = new AddTemplatePartsCommand
            {
                ExamTemplateId = newTemplateId,
                Parts = partsDto
            };

            var partsResult = await _mediator.Send(addPartsCmd, cancellationToken);
            if (!partsResult.IsSuccess) return OperationResult<string>.Failure($"Lỗi tạo Parts: {partsResult.Message}");

            var templateEntity = await _context.ExamTemplates.FindAsync(newTemplateId);
            if (templateEntity != null)
            {
                templateEntity.Status = ExamTemplateStatus.Published;
                await _context.SaveChangesAsync(cancellationToken);
            }

            var createExamCmd = new CreateExamCommand
            {
                Title = $"Weekly Exam - Week {weekIndex} ({DateTime.UtcNow:dd/MM})",
                Duration = targetTypes.Count * 10, 
                ExamTemplateId = newTemplateId,
                CreatedBy = AI_SYSTEM_ID
            };

            var examResult = await _mediator.Send(createExamCmd, cancellationToken);

            if (!examResult.IsSuccess) return OperationResult<string>.Failure($"Lỗi tạo Exam: {examResult.Message}");

            return OperationResult<string>.Success(examResult.Data);
        }
        private async Task<string?> FindMatchingExamAsync(List<string> targetTypes, CancellationToken cancellationToken)
        {
            var candidateExams = await _context.Exams
                .Where(e => e.Status == ExamStatus.Published)
                .Select(e => new
                {
                    e.ExamId,
                    Types = e.ExamQuestions.Select(eq => eq.QuestionBank.QuestionTypeId).Distinct().ToList(),
                    QuestionCount = e.ExamQuestions.Count
                })
                .ToListAsync(cancellationToken);

            foreach (var exam in candidateExams)
            {
                bool coversAllNeeded = !targetTypes.Except(exam.Types).Any();

                bool noExtraTypes = !exam.Types.Except(targetTypes).Any();

                if (coversAllNeeded && noExtraTypes && exam.QuestionCount >= 5)
                {
                    return exam.ExamId;
                }
            }
            return null;
        }
    }
}