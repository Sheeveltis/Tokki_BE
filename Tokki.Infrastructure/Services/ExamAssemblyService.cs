using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Infrastructure.Services
{
    public class ExamAssemblyService : IExamAssemblyService
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamTemplateRepository _examTemplateRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<ExamAssemblyService> _logger;

        public ExamAssemblyService(
            IExamRepository examRepository,
            IExamTemplateRepository examTemplateRepository,
            ITemplatePartRepository templatePartRepository,
            IQuestionBankRepository questionBankRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<ExamAssemblyService> logger)
        {
            _examRepository = examRepository;
            _examTemplateRepository = examTemplateRepository;
            _templatePartRepository = templatePartRepository;
            _questionBankRepository = questionBankRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> GenerateWeeklyExamAsync(
            string templateId,
            string userId,
            int weekIndex,
            List<string> weakQuestionTypeIds,
            CancellationToken cancellationToken)
        {
            try
            {
                var template = await _examTemplateRepository.GetByIdAsync(templateId);
                if (template == null || template.Status != ExamTemplateStatus.Published)
                {
                    return OperationResult<string>.Failure("Cấu trúc đề thi không tồn tại hoặc chưa được kích hoạt.", 404);
                }

                var parts = await _templatePartRepository.GetByExamTemplateIdAsync(templateId, cancellationToken);
                if (parts == null || !parts.Any())
                {
                    return OperationResult<string>.Failure("Cấu trúc đề thi bị trống.", 400);
                }

                var examId = _idGeneratorService.GenerateCustom(10);
                var examTitle = $"Weekly Test W{weekIndex} - {DateTime.UtcNow:yyyyMMdd}-{userId.Substring(0, 4)}"; // Tự sinh Title unique

                var exam = new Exam
                {
                    ExamId = examId,
                    ExamTemplateId = templateId,
                    Title = examTitle,
                    Type = ExamType.WeeklyAssessment, 
                    Status = ExamStatus.Published, 
                    Duration = 60, 
                    CreatedBy = "AI_SYSTEM",
                    CreatedAt = DateTime.UtcNow,
                    ExamQuestions = new List<ExamQuestion>()
                };

                var allSelectedQuestionIds = new List<string>();

                foreach (var part in parts)
                {
                    int quantityNeeded = part.QuestionTo - part.QuestionFrom + 1;
                    if (quantityNeeded <= 0) continue;

                    List<QuestionBank> selectedQuestions;

                    if (weakQuestionTypeIds.Contains(part.QuestionTypeId))
                    {
                        selectedQuestions = await _questionBankRepository.GetRandomQuestionsByTypeAsync(
                            part.QuestionTypeId,
                            quantityNeeded,
                            allSelectedQuestionIds,
                            cancellationToken
                        );
                    }
                    else
                    {
                        selectedQuestions = await _questionBankRepository.GetRandomQuestionsByTypeAsync(
                            part.QuestionTypeId,
                            quantityNeeded,
                            allSelectedQuestionIds,
                            cancellationToken
                        );
                    }

                    if (selectedQuestions.Count < quantityNeeded)
                    {
                        _logger.LogWarning($"Không đủ câu hỏi cho Type {part.QuestionTypeId}. Cần {quantityNeeded}, có {selectedQuestions.Count}");
                    }

                    int currentQuestionNo = part.QuestionFrom;
                    foreach (var q in selectedQuestions)
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

                return OperationResult<string>.Success(exam.ExamId, 201, "Đã tạo đề kiểm tra tuần thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi AI tạo Exam tuần");
                return OperationResult<string>.Failure("Lỗi hệ thống khi tạo đề thi.", 500);
            }
        }
    }
}