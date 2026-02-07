using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.PublishTopic
{
    public class PublishTopicCommandHandler : IRequestHandler<PublishTopicCommand, OperationResult<bool>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidator<PublishTopicCommand> _validator;

        public PublishTopicCommandHandler(
            ITopicRepository topicRepository,
            IHttpContextAccessor httpContextAccessor,
            IValidator<PublishTopicCommand> validator)
        {
            _topicRepository = topicRepository;
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
        }

        public async Task<OperationResult<bool>> Handle(PublishTopicCommand request, CancellationToken cancellationToken)
        {
            // 1) VALIDATION
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new Error(e.ErrorCode, e.ErrorMessage))
                    .ToList();

                return OperationResult<bool>.Failure(errors, 400, AppErrors.ValidationFailed.Description);
            }

            // 2) USER
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
                // 3) LOAD TOPIC
                var topic = await _topicRepository.GetByIdAsync(request.TopicId);
                if (topic == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicNotFound },
                        404,
                        AppErrors.TopicNotFound.Description
                    );
                }

                // 4) RULES
                if (topic.Status == TopicStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicAlreadyDeleted },
                        400,
                        AppErrors.TopicAlreadyDeleted.Description
                    );
                }

                // Idempotent: đã Active thì coi như publish thành công
                if (topic.Status == TopicStatus.Active)
                {
                    return OperationResult<bool>.Success(true, 200, "Topic đã được kích hoạt.");
                }

                // Chỉ cho phép Draft -> Active
                if (topic.Status != TopicStatus.Draft)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicInvalidStatusTransition },
                        400,
                        AppErrors.TopicInvalidStatusTransition.Description
                    );
                }

                // 5) UPDATE
                topic.Status = TopicStatus.Active;

                // Update audit
                topic.UpdateBy = currentUserId;
                topic.UpdateDate = DateTime.UtcNow.AddHours(7);

                // Approve audit (người duyệt + thời gian duyệt)
                topic.ApprovedBy = currentUserId;                 // NVARCHAR(15) FK -> Accounts.UserId
                topic.ApprovedDate = DateTime.UtcNow.AddHours(7); // thời gian duyệt

                await _topicRepository.UpdateAsync(topic);
                await _topicRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, "Xuất bản chủ đề thành công.");
            }
            catch
            {
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
