using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Exam.Commands.RemoveQuestionFromExam
{
    public class RemoveQuestionFromExamCommandHandler : IRequestHandler<RemoveQuestionFromExamCommand, OperationResult<bool>>
    {
        private readonly IExamRepository _examRepository;
        private readonly IExamQuestionRepository _examQuestionRepository;
        private readonly ILogger<RemoveQuestionFromExamCommandHandler> _logger;

        public RemoveQuestionFromExamCommandHandler(
            IExamRepository examRepository,
            IExamQuestionRepository examQuestionRepository,
            ILogger<RemoveQuestionFromExamCommandHandler> logger)
        {
            _examRepository = examRepository;
            _examQuestionRepository = examQuestionRepository;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(RemoveQuestionFromExamCommand request, CancellationToken cancellationToken)
        {
            var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken);
            if (exam == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.ExamNotFound },
                    404,
                    AppErrors.ExamNotFound.Description
                );
            }

            var examQuestion = await _examQuestionRepository.GetByExamAndQuestionNoAsync(
                request.ExamId,
                request.QuestionNo,
                cancellationToken
            );

            if (examQuestion == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { new Error("ExamQuestion.NotFound", $"Câu số {request.QuestionNo} không tồn tại trong bài test") },
                    404,
                    $"Câu số {request.QuestionNo} không tồn tại trong bài test"
                );
            }

            try
            {
                await _examQuestionRepository.DeleteAsync(examQuestion);
                await _examQuestionRepository.SaveChangesAsync(cancellationToken);

                // Nếu xóa câu hỏi và bài test đang Published, chuyển về Draft
                if (exam.Status == Domain.Enums.ExamStatus.Published)
                {
                    exam.Status = Domain.Enums.ExamStatus.Draft;
                    await _examRepository.UpdateAsync(exam);
                    await _examRepository.SaveChangesAsync(cancellationToken);
                }

                return OperationResult<bool>.Success(
                    true,
                    200,
                    $"Xóa câu số {request.QuestionNo} khỏi bài test thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa câu hỏi khỏi bài test: {ExamId}, QuestionNo: {QuestionNo}",
                    request.ExamId, request.QuestionNo);
                return OperationResult<bool>.Failure(
                   new List<Tokki.Application.Common.Models.Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
    }
