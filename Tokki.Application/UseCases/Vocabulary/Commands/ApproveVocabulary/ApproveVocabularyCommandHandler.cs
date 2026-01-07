using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.ApproveVocabulary
{
    public class ApproveVocabularyCommandHandler
        : IRequestHandler<ApproveVocabularyCommand, OperationResult<bool>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApproveVocabularyCommandHandler> _logger;

        public ApproveVocabularyCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ApproveVocabularyCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            ApproveVocabularyCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            var vocabulary = await _vocabularyRepository.GetByIdAsync(request.VocabularyId);

            if (vocabulary == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error>
                    {
                        new Error("VOCABULARY_NOT_FOUND", "Không tìm thấy vocabulary.")
                    },
                    404,
                    "Vocabulary không tồn tại."
                );
            }

            if (vocabulary.Status != VocabularyStatus.PendingApproval)
            {
                return OperationResult<bool>.Failure(
                    new List<Error>
                    {
                        new Error(
                            "VOCABULARY_INVALID_STATUS",
                            $"Không thể duyệt vocabulary ở trạng thái {vocabulary.Status}."
                        )
                    },
                    400,
                    "Vocabulary không ở trạng thái chờ phê duyệt."
                );
            }

            vocabulary.Status = VocabularyStatus.Active;

            await _vocabularyRepository.UpdateAsync(vocabulary);
            await _vocabularyRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Vocabulary {VocabularyId} đã được duyệt bởi {UserId}",
                vocabulary.VocabularyId,
                currentUserId
            );

            return OperationResult<bool>.Success(
                true,
                200,
                "Duyệt vocabulary thành công."
            );
        }
    }
}
