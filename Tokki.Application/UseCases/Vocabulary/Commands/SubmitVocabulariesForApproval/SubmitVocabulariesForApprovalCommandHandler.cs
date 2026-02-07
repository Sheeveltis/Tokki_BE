using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.SubmitVocabulariesForApproval
{
    public class SubmitVocabulariesForApprovalCommandHandler
        : IRequestHandler<SubmitVocabulariesForApprovalCommand, OperationResult<bool>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyExampleRepository _vocabularyExampleRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SubmitVocabulariesForApprovalCommandHandler> _logger;

        public SubmitVocabulariesForApprovalCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyExampleRepository vocabularyExampleRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SubmitVocabulariesForApprovalCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _vocabularyExampleRepository = vocabularyExampleRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            SubmitVocabulariesForApprovalCommand request,
            CancellationToken cancellationToken)
        {
            var userId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            if (request.VocabularyIds == null || !request.VocabularyIds.Any())
            {
                return OperationResult<bool>.Failure(
                    new List<Error>
                    {
                        new Error("VOCABULARY_EMPTY", "Danh sách vocabulary rỗng.")
                    },
                    400,
                    "Không có vocabulary nào để gửi duyệt."
                );
            }

            // ===== TRANSACTION (GIỐNG BULK CREATE) =====
            await using var transaction =
                await _vocabularyExampleRepository.BeginTransactionAsync(cancellationToken);

            try
            {
                int submittedCount = 0;

                foreach (var vocabId in request.VocabularyIds.Distinct())
                {
                    var vocabulary = await _vocabularyRepository.GetByIdAsync(vocabId);

                    if (vocabulary == null)
                    {
                        throw new Exception($"Vocabulary không tồn tại: {vocabId}");
                    }

                    if (vocabulary.Status != VocabularyStatus.Draft)
                    {
                        throw new Exception(
                            $"Vocabulary '{vocabulary.Text}' không ở trạng thái Draft."
                        );
                    }

                    vocabulary.Status = VocabularyStatus.PendingApproval;
                    vocabulary.UpdateDate = DateTime.UtcNow.AddHours(7);

                    await _vocabularyRepository.UpdateAsync(vocabulary);
                    submittedCount++;
                }

                await _vocabularyRepository.SaveChangesAsync(cancellationToken);
                await _vocabularyExampleRepository.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Submitted {Count} vocabularies for approval by {UserId}",
                    submittedCount,
                    userId
                );

                return OperationResult<bool>.Success(
                    true,
                    200,
                    $"Đã gửi duyệt {submittedCount} từ vựng."
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "Lỗi khi submit vocabulary for approval");

                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    ex.Message
                );
            }
        }
    }
}
