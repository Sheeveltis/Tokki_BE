using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Exam.Commands.AddQuestionToExam
{
    public class AddQuestionToExamCommandHandler : IRequestHandler<AddQuestionToExamCommand, OperationResult<string>>
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamQuestionRepository _examQuestionRepository;
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly ITemplatePartRepository _templatePartRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<AddQuestionToExamCommandHandler> _logger;

        public AddQuestionToExamCommandHandler(
            IExamRepository examRepository,
            IExamQuestionRepository examQuestionRepository,
            IQuestionBankRepository questionBankRepository,
            ITemplatePartRepository templatePartRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<AddQuestionToExamCommandHandler> logger)
        {
            _examRepository = examRepository;
            _examQuestionRepository = examQuestionRepository;
            _questionBankRepository = questionBankRepository;
            _templatePartRepository = templatePartRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(AddQuestionToExamCommand request, CancellationToken cancellationToken)
        {
            // 1. Validate Exam exists
            var exam = await _examRepository.GetByIdWithDetailsAsync(request.ExamId, cancellationToken);
            if (exam == null)
            {
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamNotFound },
                    404,
                    AppErrors.ExamNotFound.Description
                );
            }

            // 2. Validate QuestionBank exists
            var questionBank = await _questionBankRepository.GetByIdAsync(request.QuestionBankId, cancellationToken);
            if (questionBank == null)
            {
                return OperationResult<string>.Failure(
                     new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamQuestionBankNotFound },
                    404,
                    AppErrors.ExamQuestionBankNotFound.Description
                );
            }

            // 3. Validate QuestionNo chưa tồn tại trong exam
            bool questionNoExists = await _examQuestionRepository.IsQuestionNoExistsAsync(request.ExamId, request.QuestionNo);
            if (questionNoExists)
            {
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamQuestionNoDuplicated },
                    409,
                    $"Câu số {request.QuestionNo} đã tồn tại trong bài test"
                );
            }

            // 4. Validate QuestionNo nằm trong range của Template Part
            var templatePart = await _templatePartRepository.GetPartByQuestionNoAsync(
                exam.ExamTemplateId,
                request.QuestionNo,
                cancellationToken
            );

            if (templatePart == null)
            {
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamQuestionNotInPart },
                    400,
                    $"Câu số {request.QuestionNo} không nằm trong khoảng của bất kỳ phần nào trong mẫu đề"
                );
            }

            // 5. Validate Skill của câu hỏi khớp với Skill của Part
            if (questionBank.Skill != templatePart.Skill)
            {
                return OperationResult<string>.Failure(
                     new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamQuestionSkillMismatch },
                    400,
                    $"Kỹ năng của câu hỏi ({questionBank.Skill}) không khớp với kỹ năng của phần '{templatePart.PartTitle}' ({templatePart.Skill})"
                );
            }

            try
            {
                string examQuestionId = _idGeneratorService.GenerateCustom(10);

                var examQuestion = new ExamQuestion
                {
                    ExamQuestionId = examQuestionId,
                    ExamId = request.ExamId,
                    QuestionBankId = request.QuestionBankId,
                    QuestionNo = request.QuestionNo,
                    Score = request.Score
                };

                await _examQuestionRepository.AddAsync(examQuestion);
                await _examQuestionRepository.SaveChangesAsync(cancellationToken);

                // Check nếu đã thêm đủ câu hỏi theo template, tự động chuyển status sang Published
                var questionCount = await _examRepository.GetQuestionCountAsync(request.ExamId, cancellationToken);
                var totalQuestionsInTemplate = exam.ExamTemplate.TemplateParts.Any()
                    ? exam.ExamTemplate.TemplateParts.Max(tp => tp.QuestionTo)
                    : 0;

                if (questionCount >= totalQuestionsInTemplate && exam.Status == Domain.Enums.ExamStatus.Draft)
                {
                    exam.Status = Domain.Enums.ExamStatus.Published;
                    await _examRepository.UpdateAsync(exam);
                    await _examRepository.SaveChangesAsync(cancellationToken);
                }

                return OperationResult<string>.Success(
                    examQuestionId,
                    201,
                    $"Thêm câu hỏi số {request.QuestionNo} thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm câu hỏi vào bài test: {ExamId}", request.ExamId);
                return OperationResult<string>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
