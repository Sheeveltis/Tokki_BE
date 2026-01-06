using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Passages.Commands.DeletePassage
{
    public class DeletePassageCommandHandler : IRequestHandler<DeletePassageCommand, OperationResult<bool>>
    {
        private readonly IPassageRepository _passageRepository;
        private readonly IQuestionBankRepository _questionBankRepository;

        public DeletePassageCommandHandler(
            IPassageRepository passageRepository,
            IQuestionBankRepository questionBankRepository)
        {
            _passageRepository = passageRepository;
            _questionBankRepository = questionBankRepository;
        }

        public async Task<OperationResult<bool>> Handle(DeletePassageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var id = request.PassageId.Trim();

                var passage = await _passageRepository.GetByIdAsync(id, cancellationToken);
                if (passage == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.PassageNotFound },
                        404,
                        AppErrors.PassageNotFound.Description
                    );
                }

                // ✅ Không được ẩn nếu đang có QuestionBank dùng Passage này
                var inUse = await _questionBankRepository.AnyUsingPassageAsync(id, cancellationToken);
                if (inUse)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.PassageInUse },
                        409,
                        AppErrors.PassageInUse.Description
                    );
                }

                if (passage.Status == PassageStatus.Hidden)
                {
                    return OperationResult<bool>.Success(true, 200, "Đoạn văn đã bị ẩn trước đó.");
                }

                passage.Status = PassageStatus.Hidden;

                await _passageRepository.UpdateAsync(passage);
                await _passageRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, "Xóa đoạn văn thành công.");
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
