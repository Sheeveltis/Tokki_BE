using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Exam.Commands.CreateExam
{
    public class CreateExamCommandHandler : IRequestHandler<CreateExamCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<CreateExamCommandHandler> _logger;

        public CreateExamCommandHandler(
            IExamRepository examRepository,
            IExamTemplateRepository examTemplateRepository,
            ITemplatePartRepository templatePartRepository,
            IQuestionBankRepository questionBankRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<CreateExamCommandHandler> logger)
        {
            _examRepository = examRepository;
            _examTemplateRepository = examTemplateRepository;
            _templatePartRepository = templatePartRepository;
            _questionBankRepository = questionBankRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateExamCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Bắt đầu tạo Exam từ TemplateId: {ExamTemplateId}, Title: {Title}",
                request.ExamTemplateId, request.Title);

            try
            {
                bool isDuplicate = await _examRepository.IsTitleExistsAsync(request.Title, null, cancellationToken);
                if (isDuplicate)
                    return OperationResult<string>.Failure($"Tên đề thi '{request.Title}' đã tồn tại.", 400);

                var template = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId);
                if (template == null) return OperationResult<string>.Failure(AppErrors.ExamTemplateNotFound, 404);

                if (template.Status != ExamTemplateStatus.Published)
                    return OperationResult<string>.Failure(AppErrors.ExamTemplateInactive, 400);

                var parts = await _templatePartRepository.GetByExamTemplateIdAsync(template.ExamTemplateId, cancellationToken);
                if (parts == null || !parts.Any())
                    return OperationResult<string>.Failure(AppErrors.ExamTemplateEmptyParts, 400);

                // --- LOGIC SKILL DURATIONS ---
                // Lấy danh sách Skill thực tế có trong các Part của Template
                var skillsInTemplate = parts.Select(p => p.Skill.ToString()).Distinct().ToList();
                var finalSkillDurations = new Dictionary<string, int>();

                // Để FE truyền key không phân biệt hoa thường (vd: "reading" vs "Reading")
                var inputDurations = new Dictionary<string, int>(request.SkillDurations, StringComparer.OrdinalIgnoreCase);

                foreach (var skillName in skillsInTemplate)
                {
                    // Kiểm tra nếu FE không gửi thời gian hoặc gửi <= 0 cho skill bắt buộc
                    if (!inputDurations.TryGetValue(skillName, out int time) || time <= 0)
                    {
                        return OperationResult<string>.Failure($"Vui lòng nhập thời gian làm bài hợp lệ cho phần '{skillName}'.", 400);
                    }
                    finalSkillDurations[skillName] = time;
                }

                // Luôn tự động tính tổng thời gian từ các skill
                int totalDuration = finalSkillDurations.Values.Sum();

                var examId = _idGeneratorService.GenerateCustom(10);
                var exam = new Domain.Entities.Exam
                {
                    ExamId = examId,
                    ExamTemplateId = template.ExamTemplateId,
                    Title = request.Title,
                    Type = template.Type,
                    Status = ExamStatus.Draft,
                    Duration = totalDuration, 
                    SkillDurations = System.Text.Json.JsonSerializer.Serialize(finalSkillDurations),
                    CreatedBy = request.CreatedBy,
                    ExamQuestions = new List<ExamQuestion>()
                };

                var allSelectedQuestionIds = new List<string>();

                foreach (var part in parts)
                {
                    int quantityNeeded = part.QuestionTo - part.QuestionFrom + 1;
                    if (quantityNeeded <= 0) continue;

                    // FIX: Loại bỏ DifficultyLevel, bốc random thuần túy theo Type
                    var randomQuestions = await _questionBankRepository.GetRandomQuestionsByTypeAsync(
                        part.QuestionTypeId,
                        quantityNeeded,
                        allSelectedQuestionIds,
                        cancellationToken // Đã bỏ actualDifficulty ở đây
                    );

                    if (randomQuestions.Count < quantityNeeded)
                    {
                        return OperationResult<string>.Failure(
                            AppErrors.ExamNotEnoughQuestions(part.QuestionTypeId, quantityNeeded, randomQuestions.Count), 400);
                    }

                    int currentQuestionNo = part.QuestionFrom;
                    foreach (var q in randomQuestions)
                    {
                        allSelectedQuestionIds.Add(q.QuestionBankId);
                        exam.ExamQuestions.Add(new ExamQuestion
                        {
                            ExamQuestionId = _idGeneratorService.GenerateCustom(10),
                            ExamId = examId,
                            QuestionBankId = q.QuestionBankId,
                            QuestionNo = currentQuestionNo,
                            Score = part.Mark
                        });
                        currentQuestionNo++;
                    }
                }

                await _examRepository.AddAsync(exam);
                await _examRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(exam.ExamId, 201, OperationMessages.CreateSuccess("đề thi"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo Exam");
                return OperationResult<string>.Failure("Lỗi hệ thống khi tạo đề thi.", 500);
            }
        }
    }
}