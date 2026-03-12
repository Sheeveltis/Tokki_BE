using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.ApproveTopic
{
    public class ApproveTopicCommandHandler
        : IRequestHandler<ApproveTopicCommand, OperationResult<bool>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ApproveTopicCommandHandler> _logger;

        public ApproveTopicCommandHandler(
            ITopicRepository topicRepository,
            IAccountRepository accountRepository,
            IEmailService emailService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ApproveTopicCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            ApproveTopicCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            try
            {
                var topic = await _topicRepository.GetByIdAsync(request.TopicId);
                if (topic == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicNotFound },
                        404,
                        AppErrors.TopicNotFound.Description
                    );
                }

                // Không cho duyệt nếu đã Deleted
                if (topic.Status == TopicStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicAlreadyDeleted },
                        400,
                        AppErrors.TopicAlreadyDeleted.Description
                    );
                }

                // Idempotent: đã Active thì coi như đã duyệt
                if (topic.Status == TopicStatus.Active)
                {
                    return OperationResult<bool>.Success(true, 200, "Topic đã được duyệt và đang hoạt động.");
                }

                // Chỉ cho phép PendingApproval -> Active
                if (topic.Status != TopicStatus.PendingApproval)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error>
                        {
                            new Error("TOPIC_INVALID_STATUS", "Topic không ở trạng thái chờ phê duyệt.")
                        },
                        400,
                        "Không thể duyệt topic."
                    );
                }

                var now = DateTime.UtcNow.AddHours(7);

                int maxOrderIndex = await _topicRepository.GetMaxOrderIndexForVocabAsync();
                topic.OrderIndex = maxOrderIndex + 1;

                // (optional nhưng nên có) ép type đúng luôn:
                topic.TopicType = TopicType.VocabStudy;

                // Duyệt
                topic.Status = TopicStatus.Active;

                // Audit update
                topic.UpdateBy = currentUserId;
                topic.UpdateDate = now;

                // Audit approve (người duyệt + thời gian duyệt)
                topic.ApprovedBy = currentUserId;   // NVARCHAR(15) FK -> Accounts.UserId
                topic.ApprovedDate = now;

                await _topicRepository.UpdateAsync(topic);
                await _topicRepository.SaveChangesAsync(cancellationToken);

                // Gửi email thông báo cho người tạo topic
                if (!string.IsNullOrWhiteSpace(topic.CreateBy))
                {
                    var creator = await _accountRepository.GetByIdAsync(topic.CreateBy);
                    if (creator != null && !string.IsNullOrWhiteSpace(creator.Email))
                    {
                        await SendApproveEmailAsync(
                            creator.Email,
                            creator.FullName,
                            topic.TopicName
                        );
                    }
                }

                return OperationResult<bool>.Success(true, 200, "Duyệt topic thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ApproveTopic failed. TopicId={TopicId}", request.TopicId);

                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }

        private async Task SendApproveEmailAsync(
            string toEmail,
            string fullName,
            string topicTitle)
        {
            var subject = "[Tokki] Topic của bạn đã được phê duyệt";

            var safeName = string.IsNullOrWhiteSpace(fullName)
                ? toEmail
                : fullName;

            var body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Xin chào {safeName},</h2>

                    <p>
                        Chúc mừng bạn! Topic <strong>{topicTitle}</strong> của bạn
                        đã được <strong>phê duyệt</strong> và hiện đang hoạt động trên hệ thống Tokki.
                    </p>

                    <p>
                        Bạn có thể truy cập hệ thống để xem và tiếp tục quản lý nội dung của mình.
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
