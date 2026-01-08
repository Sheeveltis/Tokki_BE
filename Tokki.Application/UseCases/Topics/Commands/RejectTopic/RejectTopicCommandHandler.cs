using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.RejectTopic
{
    public class RejectTopicCommandHandler
        : IRequestHandler<RejectTopicCommand, OperationResult<bool>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RejectTopicCommandHandler> _logger;

        public RejectTopicCommandHandler(
            ITopicRepository topicRepository,
            IAccountRepository accountRepository,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<RejectTopicCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            RejectTopicCommand request,
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

            if (string.IsNullOrWhiteSpace(request.RejectReason))
            {
                return OperationResult<bool>.Failure(
                    new List<Error>
                    {
                        new Error(
                            "REJECT_REASON_REQUIRED",
                            "Lý do từ chối là bắt buộc."
                        )
                    },
                    400,
                    "Thiếu lý do từ chối."
                );
            }

            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.TopicNotFound },
                    404,
                    AppErrors.TopicNotFound.Description
                );
            }

            if (topic.Status != TopicStatus.PendingApproval)
            {
                return OperationResult<bool>.Failure(
                    new List<Error>
                    {
                        new Error(
                            "TOPIC_INVALID_STATUS",
                            "Topic không ở trạng thái chờ phê duyệt."
                        )
                    },
                    400,
                    "Không thể từ chối phê duyệt topic."
                );
            }

            topic.Status = TopicStatus.Rejected;
            await _topicRepository.UpdateAsync(topic);
            await _topicRepository.SaveChangesAsync(cancellationToken);

            // Gửi email cho người tạo topic
            if (!string.IsNullOrEmpty(topic.CreateBy))
            {
                var creator = await _accountRepository.GetByIdAsync(topic.CreateBy);
                if (creator != null && !string.IsNullOrEmpty(creator.Email))
                {
                    await SendRejectEmailAsync(
                        creator.Email,
                        creator.FullName,
                        topic.TopicName,
                        request.RejectReason
                    );
                }
            }

            return OperationResult<bool>.Success(
                true,
                200,
                "Từ chối phê duyệt topic thành công."
            );
        }

        private async Task SendRejectEmailAsync(
            string toEmail,
            string fullName,
            string topicTitle,
            string rejectReason)
        {
            var subject = "[Tokki] Topic của bạn chưa được phê duyệt";

            var safeName = string.IsNullOrWhiteSpace(fullName)
                ? toEmail
                : fullName;

            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xin chào {safeName},</h2>

                    <p>
                        Topic <strong>{topicTitle}</strong> của bạn đã được xem xét nhưng
                        <strong>chưa được phê duyệt</strong>.
                    </p>

                    <div style='background-color: #f8d7da; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                        <p><strong>Lý do từ chối:</strong></p>
                        <p>{rejectReason}</p>
                    </div>

                    <p>
                        Bạn có thể chỉnh sửa lại nội dung topic theo góp ý và gửi lại để được phê duyệt.
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
