using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.SubmitQuestionBankForApproval
{
    public class SubmitQuestionBankForApprovalCommandHandler
        : IRequestHandler<SubmitQuestionBankForApprovalCommand, OperationResult<List<string>>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;

        public SubmitQuestionBankForApprovalCommandHandler(IQuestionBankRepository questionBankRepository)
        {
            _questionBankRepository = questionBankRepository;
        }

        public async Task<OperationResult<List<string>>> Handle(
            SubmitQuestionBankForApprovalCommand request,
            CancellationToken cancellationToken)
        {
            var ids = (request.QuestionBankIds ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return OperationResult<List<string>>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Danh sách QuestionBankIds rỗng hoặc không hợp lệ."
                );
            }

            var updatedIds = new List<string>();

            // Nếu bạn muốn all-or-nothing, thì fail ngay khi gặp 1 ID lỗi.
            // Ở đây mình làm chặt: gặp lỗi thì return Failure luôn để staff biết sửa.
            foreach (var qbId in ids)
            {
                var qb = await _questionBankRepository.GetByIdAsync(qbId, cancellationToken);
                if (qb == null)
                {
                    return OperationResult<List<string>>.Failure(
                        new List<Error> { AppErrors.QuestionBankNotFound },
                        404,
                        $"Không tìm thấy QuestionBankId: {qbId}"
                    );
                }

                if (qb.Status != QuestionBankStatus.Draft &&
                    qb.Status != QuestionBankStatus.Rejected)
                {
                    return OperationResult<List<string>>.Failure(
                        new List<Error> { AppErrors.ValidationFailed },
                        400,
                        $"QuestionBankId {qbId} không ở trạng thái Draft hoặc Rejected."
                    );
                }

                qb.Status = QuestionBankStatus.PendingApproval;

                // reset thông tin duyệt cũ (nếu có)
                qb.ApprovedBy = null;
                qb.ApprovedDate = null;

                await _questionBankRepository.UpdateAsync(qb);
                updatedIds.Add(qb.QuestionBankId);
            }

            await _questionBankRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<List<string>>.Success(
                updatedIds,
                200,
                "Gửi duyệt thành công."
            );
        }
    }
}
