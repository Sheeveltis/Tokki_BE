using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.RejectVocabulary
{
    public class RejectVocabulariesCommandHandler
        : IRequestHandler<RejectVocabulariesCommand, OperationResult<bool>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyExampleRepository _vocabularyExampleRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;
        private readonly ILogger<RejectVocabulariesCommandHandler> _logger;

        public RejectVocabulariesCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyExampleRepository vocabularyExampleRepository,
            IAccountRepository accountRepository,
            IHttpContextAccessor httpContextAccessor,
            IEmailService emailService,
            ILogger<RejectVocabulariesCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _vocabularyExampleRepository = vocabularyExampleRepository;
            _accountRepository = accountRepository;
            _httpContextAccessor = httpContextAccessor;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            RejectVocabulariesCommand request,
            CancellationToken cancellationToken)
        {
            var reviewerId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(reviewerId))
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
                    "Không có vocabulary nào để từ chối."
                );
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return OperationResult<bool>.Failure(
                    new List<Error>
                    {
                        new Error("REJECT_REASON_REQUIRED", "Vui lòng nhập lý do từ chối.")
                    },
                    400,
                    "Thiếu lý do từ chối."
                );
            }

            await using var transaction =
                await _vocabularyExampleRepository.BeginTransactionAsync(cancellationToken);

            try
            {
                // Gom theo người tạo để gửi mail
                var rejectedByCreator = new Dictionary<string, List<(string Text, string Definition)>>();

                foreach (var vocabId in request.VocabularyIds.Distinct())
                {
                    var vocabulary = await _vocabularyRepository.GetByIdAsync(vocabId);

                    if (vocabulary == null)
                        throw new Exception($"Vocabulary không tồn tại: {vocabId}");

                    if (vocabulary.Status != VocabularyStatus.PendingApproval)
                    {
                        throw new Exception(
                            $"Vocabulary '{vocabulary.Text}' không ở trạng thái PendingApproval."
                        );
                    }

                    vocabulary.Status = VocabularyStatus.Rejected;
                    vocabulary.UpdateBy = reviewerId;
                    vocabulary.UpdateDate = DateTime.UtcNow.AddHours(7);

                    await _vocabularyRepository.UpdateAsync(vocabulary);

                    if (!string.IsNullOrWhiteSpace(vocabulary.CreateBy))
                    {
                        if (!rejectedByCreator.ContainsKey(vocabulary.CreateBy))
                            rejectedByCreator[vocabulary.CreateBy] = new();

                        rejectedByCreator[vocabulary.CreateBy]
                            .Add((vocabulary.Text, vocabulary.Definition));
                    }
                }

                await _vocabularyRepository.SaveChangesAsync(cancellationToken);
                await _vocabularyExampleRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // ===== SEND EMAIL CHO NGƯỜI TẠO (GIỐNG TOPIC) =====
                foreach (var item in rejectedByCreator)
                {
                    var creatorId = item.Key;
                    var vocabList = item.Value;

                    var creator = await _accountRepository.GetByIdAsync(creatorId);
                    if (creator == null || string.IsNullOrWhiteSpace(creator.Email))
                    {
                        _logger.LogWarning("Không tìm thấy email người tạo vocab. UserId={UserId}", creatorId);
                        continue;
                    }

                    await SendRejectEmailAsync(
                        creator.Email,
                        creator.FullName,
                        vocabList,
                        request.Reason
                    );
                }

                _logger.LogInformation(
                    "Rejected {Count} vocabularies by {UserId}. Reason: {Reason}",
                    request.VocabularyIds.Distinct().Count(),
                    reviewerId,
                    request.Reason
                );

                return OperationResult<bool>.Success(
                    true,
                    200,
                    $"Đã từ chối {request.VocabularyIds.Distinct().Count()} từ vựng."
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "Lỗi khi reject hàng loạt vocabulary");

                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    ex.Message
                );
            }
        }

        private async Task SendRejectEmailAsync(
            string toEmail,
            string fullName,
            List<(string Text, string Definition)> vocabularies,
            string reason)
        {
            var subject = "[Tokki] Từ vựng của bạn bị từ chối phê duyệt";

            var safeName = string.IsNullOrWhiteSpace(fullName)
                ? toEmail
                : fullName;

            var listHtml = string.Join("", vocabularies.Select(v =>
                $"<li><strong>{System.Net.WebUtility.HtmlEncode(v.Text)}</strong> – {System.Net.WebUtility.HtmlEncode(v.Definition)}</li>"
            ));

            var body = $@"
<div style='font-family: Arial, sans-serif; padding: 20px;'>
    <h2>Xin chào {System.Net.WebUtility.HtmlEncode(safeName)},</h2>

    <p>
        Các từ vựng bạn tạo đã bị <strong>từ chối phê duyệt</strong>.
    </p>

    <p><strong>Số lượng:</strong> {vocabularies.Count}</p>

    <ul>
        {listHtml}
    </ul>

    <p><strong>Lý do từ chối:</strong></p>
    <p>{System.Net.WebUtility.HtmlEncode(reason)}</p>

    <p>Bạn có thể chỉnh sửa và gửi lại để được duyệt.</p>

    <hr />
    <p style='font-size: 12px; color: gray;'>
        Đây là email tự động từ hệ thống Tokki, vui lòng không trả lời email này.
    </p>
</div>";

            await _emailService.SendEmailAsync(toEmail, subject, body);
        }
    }
}
