using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Exam.Commands.UpdateExamInfo
{
    public class UpdateExamInfoCommandHandler : IRequestHandler<UpdateExamInfoCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly ILogger<UpdateExamInfoCommandHandler> _logger;

        public UpdateExamInfoCommandHandler(
            IExamRepository examRepository, 
            ITemplatePartRepository templatePartRepository,
            ILogger<UpdateExamInfoCommandHandler> logger)
        {
            _examRepository = examRepository;
            _templatePartRepository = templatePartRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(UpdateExamInfoCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var exam = await _examRepository.GetByIdAsync(request.ExamId);
                if (exam == null)
                {
                    return OperationResult<string>.Failure(AppErrors.ExamNotFound, 404);
                }

                bool isDuplicate = await _examRepository.IsTitleExistsAsync(request.Title, request.ExamId, cancellationToken);

                if (isDuplicate)
                {
                    return OperationResult<string>.Failure($"Tên đề thi '{request.Title}' đã được sử dụng. Vui lòng chọn tên khác.", 400);
                }

                // --- LOGIC SKILL DURATIONS ---
                var parts = await _templatePartRepository.GetByExamTemplateIdAsync(exam.ExamTemplateId, cancellationToken);
                var skillsInTemplate = parts.Select(p => p.Skill.ToString()).Distinct().ToList();
                var finalSkillDurations = new Dictionary<string, int>();
                var inputDurations = new Dictionary<string, int>(request.SkillDurations, StringComparer.OrdinalIgnoreCase);

                foreach (var skillName in skillsInTemplate)
                {
                    if (!inputDurations.TryGetValue(skillName, out int time) || time <= 0)
                    {
                        return OperationResult<string>.Failure($"Vui lòng nhập thời gian làm bài hợp lệ cho phần '{skillName}'.", 400);
                    }
                    finalSkillDurations[skillName] = time;
                }

                exam.Title = request.Title;
                exam.Duration = finalSkillDurations.Values.Sum();
                exam.SkillDurations = System.Text.Json.JsonSerializer.Serialize(finalSkillDurations);
                await _examRepository.UpdateAsync(exam);
                await _examRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(OperationMessages.UpdateSuccess("đề thi"));
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure(AppErrors.ServerError, 500);
            }
        }
    }
}
