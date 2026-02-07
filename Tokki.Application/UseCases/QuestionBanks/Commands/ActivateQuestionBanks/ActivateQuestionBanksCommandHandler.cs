using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.ActivateQuestionBanks
{
    public class ActivateQuestionBanksCommandHandler : IRequestHandler<ActivateQuestionBanksCommand, OperationResult<int>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly ILogger<ActivateQuestionBanksCommandHandler> _logger;

        public ActivateQuestionBanksCommandHandler(
            IQuestionBankRepository questionBankRepository,
            ILogger<ActivateQuestionBanksCommandHandler> logger)
        {
            _questionBankRepository = questionBankRepository;
            _logger = logger;
        }

        public async Task<OperationResult<int>> Handle(ActivateQuestionBanksCommand request, CancellationToken cancellationToken)
        {
            var ids = request.QuestionBankIds?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            if (ids.Count == 0)
            {
                return OperationResult<int>.Failure(
                    new List<Error> { AppErrors.BadRequest },
                    400,
                    "Danh sách QuestionBankIds không hợp lệ."
                );
            }

            var items = await _questionBankRepository.GetByIdsAsync(ids, cancellationToken);

            // 1) Check thiếu ID
            var foundIds = items.Select(x => x.QuestionBankId).ToHashSet();
            var notFound = ids.Where(id => !foundIds.Contains(id)).ToList();
            if (notFound.Count > 0)
            {
                return OperationResult<int>.Failure(
                    new List<Error> { AppErrors.QuestionBankNotFound },
                    404,
                    $"Không tìm thấy QuestionBankId: {string.Join(", ", notFound)}"
                );
            }

            // 2) Check chỉ cho Draft
            var notDraft = items
                .Where(x => x.Status != QuestionBankStatus.Draft)
                .Select(x => x.QuestionBankId)
                .ToList();

            if (notDraft.Count > 0)
            {
                return OperationResult<int>.Failure(
                    new List<Error> { AppErrors.Forbidden },
                    403,
                    $"Chỉ được kích hoạt các câu hỏi đang Draft. Không hợp lệ: {string.Join(", ", notDraft)}"
                );
            }

            try
            {
                // 3) Update Draft -> Active
                foreach (var qb in items)
                {
                    qb.Status = QuestionBankStatus.Active;
                }

                await _questionBankRepository.UpdateRangeAsync(items);
                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<int>.Success(
                    items.Count,
                    200,
                    $"Đã kích hoạt {items.Count} câu hỏi."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kích hoạt QuestionBankIds: {Ids}", string.Join(", ", ids));
                return OperationResult<int>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
