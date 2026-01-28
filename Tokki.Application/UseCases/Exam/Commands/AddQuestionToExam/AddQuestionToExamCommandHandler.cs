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
        private readonly IExamQuestionRepository _examQuestionRepository;
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly ILogger<AddQuestionToExamCommandHandler> _logger;

        public AddQuestionToExamCommandHandler(
            IExamQuestionRepository examQuestionRepository,
            IQuestionBankRepository questionBankRepository,
            ILogger<AddQuestionToExamCommandHandler> logger)
        {
            _examQuestionRepository = examQuestionRepository;
            _questionBankRepository = questionBankRepository;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(AddQuestionToExamCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var newQuestion = await _questionBankRepository.GetByIdAsync(request.QuestionBankId, cancellationToken);
                if (newQuestion == null)
                {
                    return OperationResult<string>.Failure(AppErrors.QuestionBankNotFound, 404);
                }

                var examQuestionSlot = await _examQuestionRepository.GetByExamAndQuestionNoAsync(request.ExamId, request.QuestionNo, cancellationToken);

                if (examQuestionSlot == null)
                {
                    return OperationResult<string>.Failure($"Không tìm thấy câu hỏi số {request.QuestionNo} trong đề thi này để thay thế.", 404);
                }

                examQuestionSlot.QuestionBankId = request.QuestionBankId;

                await _examQuestionRepository.UpdateAsync(examQuestionSlot);
                await _examQuestionRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(OperationMessages.UpdateSuccess("câu hỏi mới"));
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Failure(AppErrors.ServerError, 500);
            }
        }
    }
}
