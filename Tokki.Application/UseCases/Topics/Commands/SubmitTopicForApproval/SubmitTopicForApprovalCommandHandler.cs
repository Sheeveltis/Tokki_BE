using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.SubmitTopicForApproval
{
    public class SubmitTopicForApprovalCommandHandler
        : IRequestHandler<SubmitTopicForApprovalCommand, OperationResult<bool>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SubmitTopicForApprovalCommandHandler> _logger;

        public SubmitTopicForApprovalCommandHandler(
            ITopicRepository topicRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SubmitTopicForApprovalCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            SubmitTopicForApprovalCommand request,
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

            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.TopicNotFound },
                    404,
                    AppErrors.TopicNotFound.Description
                );
            }

            if (topic.Status != TopicStatus.Draft)
            {
                return OperationResult<bool>.Failure(
                    new List<Error>
                    {
                        new Error(
                            "TOPIC_INVALID_STATUS",
                            "Topic không ở trạng thái soạn thảo."
                        )
                    },
                    400,
                    "Không thể gửi duyệt topic."
                );
            }

            topic.Status = TopicStatus.PendingApproval;

            await _topicRepository.UpdateAsync(topic);
            await _topicRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<bool>.Success(
                true,
                200,
                "Gửi topic chờ phê duyệt thành công."
            );
        }
    }
}
