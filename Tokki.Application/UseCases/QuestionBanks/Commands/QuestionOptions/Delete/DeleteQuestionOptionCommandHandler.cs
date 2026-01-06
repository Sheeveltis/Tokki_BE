using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.QuestionOptions.Delete
{
    public class DeleteQuestionOptionCommandHandler : IRequestHandler<DeleteQuestionOptionCommand, OperationResult<bool>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IQuestionOptionRepository _questionOptionRepository;

        public DeleteQuestionOptionCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IQuestionOptionRepository questionOptionRepository)
        {
            _questionBankRepository = questionBankRepository;
            _questionOptionRepository = questionOptionRepository;
        }

        public async Task<OperationResult<bool>> Handle(DeleteQuestionOptionCommand request, CancellationToken cancellationToken)
        {
            var qb = await _questionBankRepository.GetByIdWithDetailsAsync(request.QuestionBankId, cancellationToken);
            if (qb == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.QuestionBankNotFound },
                    404,
                    AppErrors.QuestionBankNotFound.Description);
            }

            if (qb.Status != QuestionBankStatus.Draft)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.QuestionBankNeedToHaveDraftStatus },
                    403,
                    "Chỉ được phép xóa đáp án khi câu hỏi đang ở trạng thái Draft.");
            }

            var option = qb.QuestionOptions.FirstOrDefault(o => o.OptionId == request.OptionId);
            if (option == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.QuestionOptionNotFound },
                    404,
                    AppErrors.QuestionOptionNotFound.Description);
            }

            await _questionOptionRepository.DeleteAsync(option);
            await _questionOptionRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(true, 200, "Xóa đáp án thành công.");
        }
    }
}
