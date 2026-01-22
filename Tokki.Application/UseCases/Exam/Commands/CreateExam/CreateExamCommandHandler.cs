using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            // 1. Log bắt đầu request
            _logger.LogInformation("Bắt đầu tạo Exam từ TemplateId: {ExamTemplateId}, Title: {Title}", request.ExamTemplateId, request.Title);

            try
            {
                var template = await _examTemplateRepository.GetByIdAsync(request.ExamTemplateId);
                if (template == null)
                {
                    _logger.LogWarning("Thất bại: Không tìm thấy ExamTemplate với Id: {ExamTemplateId}", request.ExamTemplateId);
                    return OperationResult<string>.Failure(AppErrors.ExamTemplateNotFound, 404);
                }

                if (template.Status != ExamTemplateStatus.Published)
                {
                    _logger.LogWarning("Thất bại: Template {ExamTemplateId} chưa được Publish (Status: {Status})", request.ExamTemplateId, template.Status);
                    return OperationResult<string>.Failure(AppErrors.ExamTemplateInactive, 400);
                }

                var parts = await _templatePartRepository.GetByExamTemplateIdAsync(template.ExamTemplateId, cancellationToken);
                if (parts == null || !parts.Any())
                {
                    _logger.LogWarning("Thất bại: Template {ExamTemplateId} không có phần (Part) nào.", request.ExamTemplateId);
                    return OperationResult<string>.Failure(AppErrors.ExamTemplateEmptyParts, 400);
                }

                var examId = _idGeneratorService.GenerateCustom(10);
                var exam = new Domain.Entities.Exam
                {
                    ExamId = examId,
                    ExamTemplateId = template.ExamTemplateId,
                    Title = request.Title,
                    Type = template.Type,
                    Status = (int)ExamStatus.Draft,
                    Duration = request.Duration,
                    ExamQuestions = new List<ExamQuestion>()
                };

                var allSelectedQuestionIds = new List<string>();

                foreach (var part in parts)
                {
                    int quantityNeeded = part.QuestionTo - part.QuestionFrom + 1;

                    // Log nhẹ để biết đang xử lý Part nào
                    _logger.LogDebug("Đang xử lý Part: {TemplatePartId}, Cần lấy: {Quantity} câu hỏi loại {TypeId}", part.TemplatePartId, quantityNeeded, part.QuestionTypeId);

                    if (quantityNeeded <= 0) continue;

                    var randomQuestions = await _questionBankRepository.GetRandomQuestionsByTypeAsync(
                        part.QuestionTypeId,
                        quantityNeeded,
                        allSelectedQuestionIds,
                        cancellationToken
                    );

                    // LOGIC QUAN TRỌNG: Kiểm tra đủ câu hỏi không
                    if (randomQuestions.Count < quantityNeeded)
                    {
                        _logger.LogError("LỖI NGHIỆP VỤ: Không đủ câu hỏi trong ngân hàng. TypeId: {TypeId}. Cần: {Needed}, Có sẵn: {Found}. Đã bỏ qua các ID: {ExcludedCount}",
                            part.QuestionTypeId, quantityNeeded, randomQuestions.Count, allSelectedQuestionIds.Count);

                        return OperationResult<string>.Failure(
                            AppErrors.ExamNotEnoughQuestions(
                                part.QuestionTypeId,
                                quantityNeeded,
                                randomQuestions.Count
                            ),
                            400
                        );
                    }

                    int currentQuestionNo = part.QuestionFrom;
                    foreach (var q in randomQuestions)
                    {
                        allSelectedQuestionIds.Add(q.QuestionBankId);
                        var examQuestion = new ExamQuestion
                        {
                            ExamQuestionId = _idGeneratorService.GenerateCustom(10),
                            ExamId = examId,
                            QuestionBankId = q.QuestionBankId,
                            QuestionNo = currentQuestionNo,
                            Score = part.Mark
                        };
                        exam.ExamQuestions.Add(examQuestion);
                        currentQuestionNo++;
                    }
                }

                await _examRepository.AddAsync(exam);
                await _examRepository.SaveChangesAsync(cancellationToken);

                // Log thành công
                _logger.LogInformation("TẠO ĐỀ THI THÀNH CÔNG. ExamId: {ExamId}, Tổng số câu: {TotalQuestions}", exam.ExamId, exam.ExamQuestions.Count);

                return OperationResult<string>.Success(exam.ExamId, 201, OperationMessages.CreateSuccess("đề thi"));
            }
            catch (Exception ex)
            {
                // 2. Log lỗi Crash (Exception)
                // Quan trọng: Truyền 'ex' vào tham số đầu tiên để lưu Stack Trace
                _logger.LogError(ex, "LỖI HỆ THỐNG (CRASH) khi tạo Exam. TemplateId: {ExamTemplateId}. Error: {Message}", request.ExamTemplateId, ex.Message);

                return OperationResult<string>.Failure("Đã xảy ra lỗi hệ thống khi tạo đề thi. Vui lòng thử lại sau.", 500);
            }
        }
    }
}