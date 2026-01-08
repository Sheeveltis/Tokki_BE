using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Commands.ApproveVocabulary
{
    public class ApproveVocabulariesCommandHandler
        : IRequestHandler<ApproveVocabulariesCommand, OperationResult<bool>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly IVocabularyExampleRepository _vocabularyExampleRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApproveVocabulariesCommandHandler> _logger;

        public ApproveVocabulariesCommandHandler(
            IVocabularyRepository vocabularyRepository,
            IVocabularyExampleRepository vocabularyExampleRepository,
            IAccountRepository accountRepository,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ApproveVocabulariesCommandHandler> logger)
        {
            _vocabularyRepository = vocabularyRepository;
            _vocabularyExampleRepository = vocabularyExampleRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            ApproveVocabulariesCommand request,
            CancellationToken cancellationToken)
        {
            var approverId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(approverId))
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
                    "Không có vocabulary nào để duyệt."
                );
            }

            // Transaction giống BulkCreate
            await using var transaction =
                await _vocabularyExampleRepository.BeginTransactionAsync(cancellationToken);

            try
            {
                // Gom vocab theo CreateBy để gửi mail cho từng người tạo
                var approvedByCreator = new Dictionary<string, List<(string Text, string Definition)>>();

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

                    vocabulary.Status = VocabularyStatus.Active;
                    vocabulary.UpdateBy = approverId;
                    vocabulary.UpdateDate = DateTime.UtcNow.AddHours(7);

                    await _vocabularyRepository.UpdateAsync(vocabulary);

                    if (!string.IsNullOrWhiteSpace(vocabulary.CreateBy))
                    {
                        if (!approvedByCreator.ContainsKey(vocabulary.CreateBy))
                            approvedByCreator[vocabulary.CreateBy] = new();

                        approvedByCreator[vocabulary.CreateBy].Add((vocabulary.Text, vocabulary.Definition));
                    }
                }

                await _vocabularyRepository.SaveChangesAsync(cancellationToken);
                await _vocabularyExampleRepository.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                // Gửi email cho người tạo (giống cách Topic handler làm)
                foreach (var item in approvedByCreator)
                {
                    var creatorId = item.Key;
                    var vocabList = item.Value;

                    var creator = await _accountRepository.GetByIdAsync(creatorId);
                    if (creator == null || string.IsNullOrWhiteSpace(creator.Email))
                    {
                        _logger.LogWarning("Không tìm thấy email người tạo vocab. UserId={UserId}", creatorId);
                        continue;
                    }

                    await SendApproveEmailAsync(
                        creator.Email,
                        creator.FullName,
                        vocabList
                    );
                }

                _logger.LogInformation(
                    "Approved {Count} vocabularies by {UserId}",
                    request.VocabularyIds.Distinct().Count(),
                    approverId
                );

                return OperationResult<bool>.Success(
                    true,
                    200,
                    $"Đã duyệt thành công {request.VocabularyIds.Distinct().Count()} từ vựng."
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "Lỗi khi duyệt hàng loạt vocabulary");

                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    ex.Message
                );
            }
        }

        private async Task SendApproveEmailAsync(
            string toEmail,
            string fullName,
            List<(string Text, string Definition)> vocabularies)
        {
            var subject = "[Tokki] Từ vựng của bạn đã được phê duyệt";

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
        Các từ vựng bạn tạo đã được <strong>phê duyệt</strong>.
    </p>

    <p><strong>Số lượng:</strong> {vocabularies.Count}</p>

    <ul>
        {listHtml}
    </ul>

    <hr />
    <p style='font-size: 12px; color: gray;'>
        Đây là email tự động từ hệ thống Tokki, vui lòng không trả lời email này.
    </p>
</div>";

            await _emailService.SendEmailAsync(toEmail, subject, body);
        }
    }
}
