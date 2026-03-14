using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.DeleteTopic
{
    public class DeleteTopicCommandHandler : IRequestHandler<DeleteTopicCommand, OperationResult<bool>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DeleteTopicCommandHandler> _logger;

        public DeleteTopicCommandHandler(
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DeleteTopicCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
        {
            try
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

                // 1) Check topic tồn tại
                var existingTopic = await _topicRepository.GetByIdAsync(request.TopicId);
                if (existingTopic == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicNotFound },
                        404,
                        AppErrors.TopicNotFound.Description
                    );
                }

                // 2) Nếu topic đã Deleted thì báo
                if (existingTopic.Status == TopicStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicAlreadyDeleted },
                        400,
                        AppErrors.TopicAlreadyDeleted.Description
                    );
                }

                var now = DateTime.UtcNow.AddHours(7);

                // 3) Soft delete topic
                int? deletedOrderIndex = existingTopic.OrderIndex; var deletedType = existingTopic.TopicType;

                existingTopic.Status = TopicStatus.Deleted;
                existingTopic.UpdateBy = currentUserId;
                existingTopic.UpdateDate = now;

                await _topicRepository.UpdateAsync(existingTopic);

                // ✅ Lùi OrderIndex các topic phía sau 1 bậc
                if (deletedOrderIndex.HasValue && deletedOrderIndex.Value > 0)
                {
                    await _topicRepository.DecrementOrderIndexAfterAsync(
                        deletedOrderIndex.Value,
                        deletedType,
                        currentUserId,
                        now);
                }


                // 4) Soft delete toàn bộ mapping VocabularyTopic của topic này
                var mappings = await _vocabularyTopicRepository.GetByTopicIdAsync(request.TopicId);

                if (mappings != null && mappings.Count > 0)
                {
                    foreach (var vt in mappings)
                    {
                        // Soft delete mapping
                        vt.Status = VocabularyTopicStatus.Deleted;
                        vt.UpdateBy = currentUserId;
                        vt.UpdateDate = DateTime.UtcNow.AddHours(7);

                        await _vocabularyTopicRepository.UpdateAsync(vt);
                    }
                }

                // 5) Save changes
                // Nếu các repo dùng chung DbContext thì chỉ cần 1 lần SaveChanges,
                // nhưng gọi cả 2 cũng không sao (lần 2 thường return 0).
                await _topicRepository.SaveChangesAsync(cancellationToken);
                await _vocabularyTopicRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Soft deleted topic {TopicId} and {Count} mappings",
                    request.TopicId, mappings?.Count ?? 0);

                return OperationResult<bool>.Success(true, 200, "Xóa chủ đề thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa topic {TopicId}", request.TopicId);
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
