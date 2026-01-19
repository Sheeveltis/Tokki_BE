using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;
using System.Text;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.ApproveQuestionBank
{
    public class ApproveQuestionBanksCommandHandler
        : IRequestHandler<ApproveQuestionBanksCommand, OperationResult<List<string>>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApproveQuestionBanksCommandHandler> _logger;

        public ApproveQuestionBanksCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IAccountRepository accountRepository,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ApproveQuestionBanksCommandHandler> logger)
        {
            _questionBankRepository = questionBankRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<List<string>>> Handle(
            ApproveQuestionBanksCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return OperationResult<List<string>>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            var ids = (request.QuestionBankIds ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (ids.Count == 0)
            {
                return OperationResult<List<string>>.Failure(
                    new List<Error> { AppErrors.ValidationFailed },
                    400,
                    "Danh sách QuestionBankIds rỗng hoặc không hợp lệ."
                );
            }

            var now = DateTime.UtcNow.AddHours(7);
            var approvedIds = new List<string>();

            // Danh sách QB thực sự được approve trong lần gọi này (để gom mail)
            var approvedThisBatchForMail = new List<QuestionBank>();

            try
            {
                // NEW: lấy full details 1 lần
                var qbs = await _questionBankRepository.GetByIdsWithDetailsAsync(ids, cancellationToken);
                var qbMap = qbs.ToDictionary(x => x.QuestionBankId, StringComparer.OrdinalIgnoreCase);

                // Check missing
                var missingIds = ids.Where(id => !qbMap.ContainsKey(id)).ToList();
                if (missingIds.Count > 0)
                {
                    return OperationResult<List<string>>.Failure(
                        new List<Error> { AppErrors.QuestionBankNotFound },
                        404,
                        $"Không tìm thấy QuestionBankId: {string.Join(", ", missingIds)}"
                    );
                }

                var toUpdate = new List<QuestionBank>();

                foreach (var qbId in ids)
                {
                    var qb = qbMap[qbId];

                    if (qb.Status == QuestionBankStatus.Deleted)
                    {
                        return OperationResult<List<string>>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            $"QuestionBankId {qbId} đã bị xóa, không thể duyệt."
                        );
                    }

                    // Idempotent: Active coi như ok, không gửi mail lại
                    if (qb.Status == QuestionBankStatus.Active)
                    {
                        approvedIds.Add(qb.QuestionBankId);
                        continue;
                    }

                    // Chỉ cho phép PendingApproval -> Active
                    if (qb.Status != QuestionBankStatus.PendingApproval)
                    {
                        return OperationResult<List<string>>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            $"QuestionBankId {qbId} không ở trạng thái PendingApproval."
                        );
                    }

                    qb.Status = QuestionBankStatus.Active;
                    qb.ApprovedBy = currentUserId.Trim();
                    qb.ApprovedDate = now;

                    toUpdate.Add(qb);
                    approvedIds.Add(qb.QuestionBankId);

                    // Gom mail theo CreateBy (chỉ những item approve thực sự)
                    if (!string.IsNullOrWhiteSpace(qb.CreateBy))
                    {
                        approvedThisBatchForMail.Add(qb);
                    }
                }

                if (toUpdate.Count > 0)
                {
                    await _questionBankRepository.UpdateRangeAsync(toUpdate);
                }

                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                // Gửi mail sau khi DB đã commit
                await SendBatchApproveEmailsAsync(approvedThisBatchForMail, cancellationToken);

                return OperationResult<List<string>>.Success(
                    approvedIds,
                    200,
                    "Duyệt câu hỏi thành công."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch approve QuestionBank failed. Count={Count}", ids.Count);

                return OperationResult<List<string>>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }

        private async Task SendBatchApproveEmailsAsync(
            List<QuestionBank> approvedQbs,
            CancellationToken cancellationToken)
        {
            if (approvedQbs == null || approvedQbs.Count == 0) return;

            var groups = approvedQbs
                .Where(q => !string.IsNullOrWhiteSpace(q.CreateBy))
                .GroupBy(q => q.CreateBy!.Trim(), StringComparer.OrdinalIgnoreCase);

            foreach (var g in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var creatorId = g.Key;

                try
                {
                    var creator = await _accountRepository.GetByIdAsync(creatorId);
                    if (creator == null || string.IsNullOrWhiteSpace(creator.Email))
                        continue;

                    var subject = "[Tokki] Danh sách câu hỏi đã được phê duyệt";
                    var body = BuildBatchApproveEmailBody(
                        fullName: creator.FullName,
                        email: creator.Email,
                        qbs: g.ToList()
                    );

                    await _emailService.SendEmailAsync(creator.Email, subject, body);
                }
                catch (Exception ex)
                {
                    // Không fail request vì DB đã commit; chỉ log để theo dõi
                    _logger.LogError(ex, "Send batch approve email failed. CreateBy={CreateBy} Count={Count}", creatorId, g.Count());
                }
            }
        }

        private string BuildBatchApproveEmailBody(string fullName, string email, List<QuestionBank> qbs)
        {
            var safeName = string.IsNullOrWhiteSpace(fullName) ? email : fullName;

            var sb = new StringBuilder();
            sb.Append($@"
<div style='font-family: Arial, sans-serif; padding: 20px;'>
  <h2>Xin chào {WebUtility.HtmlEncode(safeName)},</h2>
  <p>Các câu hỏi dưới đây của bạn đã được <strong>phê duyệt</strong> và chuyển sang <strong>Active</strong>:</p>
  <hr />
  <ol>
");

            foreach (var qb in qbs
                         .OrderByDescending(x => x.ApprovedDate ?? DateTime.MinValue)
                         .ThenByDescending(x => x.QuestionBankId))
            {
                var questionTypeName = qb.QuestionType?.Name ?? qb.QuestionTypeId ?? "(Không xác định)";
                var passageTitle = qb.Passage?.Title ?? qb.PassageId ?? "(Không có passage)";

                sb.Append("<li style='margin-bottom: 16px;'>");
                sb.Append($"<div><strong>ID:</strong> {WebUtility.HtmlEncode(qb.QuestionBankId)}</div>");
                sb.Append($"<div><strong>QuestionType:</strong> {WebUtility.HtmlEncode(questionTypeName)}</div>");
                sb.Append($"<div><strong>Passage:</strong> {WebUtility.HtmlEncode(passageTitle)}</div>");

                if (!string.IsNullOrWhiteSpace(qb.Content))
                    sb.Append($"<div><strong>Content:</strong> {WebUtility.HtmlEncode(qb.Content)}</div>");

                if (!string.IsNullOrWhiteSpace(qb.MediaUrl))
                    sb.Append($"<div><strong>MediaUrl:</strong> {WebUtility.HtmlEncode(qb.MediaUrl)}</div>");

                if (!string.IsNullOrWhiteSpace(qb.Explanation))
                    sb.Append($"<div><strong>Explanation:</strong> {WebUtility.HtmlEncode(qb.Explanation)}</div>");

                sb.Append(BuildOptionsHtml(qb));
                sb.Append("</li>");
            }

            sb.Append(@"
  </ol>
  <hr />
  <p style='font-size: 12px; color: gray;'>
    Đây là email tự động từ hệ thống Tokki, vui lòng không trả lời email này.
  </p>
</div>
");

            return sb.ToString();
        }

        private static string BuildOptionsHtml(QuestionBank qb)
        {
            if (qb.QuestionOptions == null || qb.QuestionOptions.Count == 0)
            {
                return "<div><strong>Đáp án:</strong> (Không có)</div>";
            }

            var sb = new StringBuilder();
            sb.Append("<div style='margin-top:8px;'><strong>Đáp án</strong><ol>");

            foreach (var o in qb.QuestionOptions.OrderBy(x => x.KeyOption))
            {
                var content = WebUtility.HtmlEncode(o.Content ?? string.Empty);

                var imgPart = string.IsNullOrWhiteSpace(o.ImageUrl)
                    ? string.Empty
                    : $"<div><em>ImageUrl:</em> {WebUtility.HtmlEncode(o.ImageUrl)}</div>";

                var correctTag = o.IsCorrect ? " <strong>(Đáp án đúng)</strong>" : string.Empty;

                sb.Append($"<li><strong>{WebUtility.HtmlEncode(o.KeyOption)}</strong>: {content}{correctTag}{imgPart}</li>");
            }

            sb.Append("</ol></div>");
            return sb.ToString();
        }
    }
}
