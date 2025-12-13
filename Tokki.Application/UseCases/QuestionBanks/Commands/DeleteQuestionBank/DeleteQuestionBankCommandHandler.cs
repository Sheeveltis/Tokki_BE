

using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.DeleteQuestionBank
{
    public class DeleteQuestionBankCommandHandler : IRequestHandler<DeleteQuestionBankCommand, OperationResult<bool>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly ILogger<DeleteQuestionBankCommandHandler> _logger;

        public DeleteQuestionBankCommandHandler(
            IQuestionBankRepository questionBankRepository,
            ILogger<DeleteQuestionBankCommandHandler> logger)
        {
            _questionBankRepository = questionBankRepository;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(DeleteQuestionBankCommand request, CancellationToken cancellationToken)
        {
            var questionBank = await _questionBankRepository.GetByIdAsync(request.QuestionBankId, cancellationToken);
            if (questionBank == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.QuestionBankNotFound },
                    404,
                    AppErrors.QuestionBankNotFound.Description
                );
            }

            try
            {
                await _questionBankRepository.DeleteAsync(questionBank);
                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(
                    true,
                    200,
                    "Xóa câu hỏi thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa câu hỏi: {QuestionBankId}", request.QuestionBankId);
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
