using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.DeleteQuestionBank
{
    public class DeleteQuestionBankCommandHandler : IRequestHandler<DeleteQuestionBankCommand, OperationResult<bool>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IQuestionOptionRepository _questionOptionRepository;

        public DeleteQuestionBankCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IQuestionOptionRepository questionOptionRepository)
        {
            _questionBankRepository = questionBankRepository;
            _questionOptionRepository = questionOptionRepository;
        }

        public async Task<OperationResult<bool>> Handle(
     DeleteQuestionBankCommand request,
     CancellationToken cancellationToken)
        {
            var questionBank = await _questionBankRepository
                .GetByIdWithDetailsAsync(request.QuestionBankId, cancellationToken);

            if (questionBank == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.QuestionBankNotFound },
                    404,
                    AppErrors.QuestionBankNotFound.Description
                );
            }

            if (questionBank.Status == QuestionBankStatus.Deleted)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.QuestionBankHasDeleted },
                    409,
                    AppErrors.QuestionBankHasDeleted.Description
                );
            }

            try
            {
                //  CHỈ xóa cứng options khi Draft hoặc Active
                if (questionBank.Status == QuestionBankStatus.Draft ||
                    questionBank.Status == QuestionBankStatus.Active)
                {
                    await _questionOptionRepository.DeleteByQuestionBankIdAsync(
                        questionBank.QuestionBankId,
                        cancellationToken
                    );
                }

                //  Assigned chỉ đổi status, KHÔNG xóa options
                questionBank.Status = QuestionBankStatus.Deleted;

                await _questionBankRepository.UpdateAsync(questionBank);
                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, "Xóa QuestionBank thành công");
            }
            catch (Exception)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }

    }
}
