using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopicOrderIndex
{
    public class UpdateTopicOrderIndexCommandHandler
        : IRequestHandler<UpdateTopicOrderIndexCommand, OperationResult<bool>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly ILogger<UpdateTopicOrderIndexCommandHandler> _logger;

        public UpdateTopicOrderIndexCommandHandler(
            ITopicRepository topicRepository,
            ILogger<UpdateTopicOrderIndexCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(
            UpdateTopicOrderIndexCommand request,
            CancellationToken cancellationToken)
        {
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

                if (topic.Status == TopicStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicAlreadyDeleted },
                        409,
                        AppErrors.TopicAlreadyDeleted.Description
                    );
                }

                int newIndex = request.OrderIndex;
                int? oldIndex = topic.OrderIndex;

                if (oldIndex == newIndex)
                {
                    return OperationResult<bool>.Success(true, 200, "OrderIndex không thay đổi.");
                }

                var now = DateTime.UtcNow.AddHours(7);

                if (!oldIndex.HasValue)
                {
                    // ✅ Chưa có index → chèn vào vị trí mới, đẩy các topic >= newIndex lên 1
                    await _topicRepository.ShiftOrderIndexUpFromAsync(
                        fromIndex: newIndex,
                        topicType: topic.TopicType,
                        excludeTopicId: topic.TopicId,
                        updatedBy: request.UpdatedBy,
                        updatedDate: now);
                }
                else
                {
                    // ✅ Đã có index → dịch chuyển các topic nằm giữa oldIndex và newIndex
                    await _topicRepository.ShiftOrderIndexBetweenAsync(
                        fromIndex: oldIndex.Value,
                        toIndex: newIndex,
                        topicType: topic.TopicType,
                        excludeTopicId: topic.TopicId,
                        updatedBy: request.UpdatedBy,
                        updatedDate: now);
                }

                topic.OrderIndex = newIndex;
                topic.UpdateBy = request.UpdatedBy;
                topic.UpdateDate = now;

                await _topicRepository.UpdateAsync(topic);
                await _topicRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, "Cập nhật thứ tự topic thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateTopicOrderIndex failed. TopicId={TopicId}", request.TopicId);
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}