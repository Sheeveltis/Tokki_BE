using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.QuestionBanks.Commands.RejectQuestionBank
{
    public class RejectQuestionBanksCommandHandler
        : IRequestHandler<RejectQuestionBanksCommand, OperationResult<List<string>>>
    {
        private readonly IQuestionBankRepository _questionBankRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RejectQuestionBanksCommandHandler> _logger;

        public RejectQuestionBanksCommandHandler(
            IQuestionBankRepository questionBankRepository,
            IAccountRepository accountRepository,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<RejectQuestionBanksCommandHandler> logger)
        {
            _questionBankRepository = questionBankRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<List<string>>> Handle(
            RejectQuestionBanksCommand request,
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

            if (string.IsNullOrWhiteSpace(request.RejectReason))
            {
                return OperationResult<List<string>>.Failure(
                    new List<Error>
                    {
                        new Error("REJECT_REASON_REQUIRED", "Lý do từ chối là bắt buộc.")
                    },
                    400,
                    "Thiếu lý do từ chối."
                );
            }

            var now = DateTime.UtcNow.AddHours(7);
            var rejectedIds = new List<string>();

            try
            {
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

                    if (qb.Status == QuestionBankStatus.Deleted)
                    {
                        return OperationResult<List<string>>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            $"QuestionBankId {qbId} đã bị xóa, không thể từ chối duyệt."
                        );
                    }

                    // Idempotent
                    if (qb.Status == QuestionBankStatus.Rejected)
                    {
                        rejectedIds.Add(qb.QuestionBankId);
                        continue;
                    }

                    // Chỉ cho phép PendingApproval -> Rejected
                    if (qb.Status != QuestionBankStatus.PendingApproval)
                    {
                        return OperationResult<List<string>>.Failure(
                            new List<Error> { AppErrors.ValidationFailed },
                            400,
                            $"QuestionBankId {qbId} không ở trạng thái PendingApproval."
                        );
                    }

                    qb.Status = QuestionBankStatus.Rejected;
                    qb.ApprovedBy = currentUserId.Trim();
                    qb.ApprovedDate = now;

                    await _questionBankRepository.UpdateAsync(qb);
                    rejectedIds.Add(qb.QuestionBankId);

                    // Email cho người tạo
                    if (!string.IsNullOrWhiteSpace(qb.CreateBy))
                    {
                        var creator = await _accountRepository.GetByIdAsync(qb.CreateBy);
                        if (creator != null && !string.IsNullOrWhiteSpace(creator.Email))
                        {
                            await SendRejectEmailAsync(
                                creator.Email,
                                creator.FullName,
                                qb.QuestionBankId,
                                request.RejectReason
                            );
                        }
                    }
                }

                await _questionBankRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<List<string>>.Success(
                    rejectedIds,
                    200,
                    "Từ chối phê duyệt câu hỏi thành công."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch reject QuestionBank failed. Count={Count}", ids.Count);

                return OperationResult<List<string>>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }

        private async Task SendRejectEmailAsync(string toEmail, string fullName, string questionBankId, string rejectReason)
        {
            var subject = "[Tokki] Câu hỏi của bạn chưa được phê duyệt";

            var safeName = string.IsNullOrWhiteSpace(fullName) ? toEmail : fullName;
            var safeReason = WebUtility.HtmlEncode(rejectReason);

            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xin chào {safeName},</h2>
                    <p>
                        Câu hỏi <strong>{questionBankId}</strong> của bạn đã được xem xét nhưng
                        <strong>chưa được phê duyệt</strong>.
                    </p>

                    <div style='background-color: #f8d7da; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Lý do từ chối:</strong></p>
                        <p>{safeReason}</p>
                    </div>

                    <p>
                        Bạn có thể chỉnh sửa lại câu hỏi và gửi duyệt lại.
                    </p>

                    <hr />
                    <p style='font-size: 12px; color: gray;'>
                        Đây là email tự động từ hệ thống Tokki, vui lòng không trả lời email này.
                    </p>
                </div>
            ";

            await _emailService.SendEmailAsync(toEmail, subject, body);
        }
    }
}
