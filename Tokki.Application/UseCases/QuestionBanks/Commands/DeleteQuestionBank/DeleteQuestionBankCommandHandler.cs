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

        public async Task<OperationResult<bool>> Handle(DeleteQuestionBankCommand request, CancellationToken cancellationToken)
        {
            var questionBank = await _questionBankRepository.GetByIdWithDetailsAsync(
                request.QuestionBankId, cancellationToken);

            if (questionBank == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.QuestionBankNotFound },
                    404,
                    AppErrors.QuestionBankNotFound.Description
                );
            }

            // Nếu đã xóa trước đó -> trả lỗi theo yêu cầu
            if (questionBank.Status == QuestionBankStatus.Deleted)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.QuestionBankHasDeleted },
                    409, // khuyến nghị: Conflict (đã ở trạng thái không thể delete tiếp)
                    AppErrors.QuestionBankHasDeleted.Description
                );
            }

            try
            {
                // 1) XÓA CỨNG toàn bộ đáp án trắc nghiệm
                await _questionOptionRepository.DeleteByQuestionBankIdAsync(
                    questionBank.QuestionBankId, cancellationToken);

                // 2) XÓA MỀM câu hỏi
                questionBank.Status = QuestionBankStatus.Deleted;

                await _questionBankRepository.UpdateAsync(questionBank);
                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, "Xóa câu hỏi thành công");
            }
            catch
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
