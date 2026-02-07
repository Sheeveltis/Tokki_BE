using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopicStatus
{
    public class UpdateTopicStatusCommandHandler : IRequestHandler<UpdateTopicStatusCommand, OperationResult<bool>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly ILogger<UpdateTopicStatusCommandHandler> _logger;

        public UpdateTopicStatusCommandHandler(
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            ILogger<UpdateTopicStatusCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(UpdateTopicStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingTopic = await _topicRepository.GetByIdAsync(request.TopicId);

                if (existingTopic == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicNotFound },
                        404,
                        AppErrors.TopicNotFound.Description
                    );
                }

                // Nếu topic đã Deleted thì không cho update/khôi phục
                if (existingTopic.Status == TopicStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicAlreadyDeleted },
                        409,
                        AppErrors.TopicAlreadyDeleted.Description
                    );
                }

                var newStatus = request.Status;

                // Nếu status không đổi -> vẫn coi như OK (hoặc bạn có thể return luôn)
                if (newStatus != existingTopic.Status)
                {
                    existingTopic.Status = newStatus;

                    // Chỉ cascade VocabularyTopic khi Topic bị Deleted
                    if (newStatus == TopicStatus.Deleted)
                    {
                        var mappings = await _vocabularyTopicRepository.GetByTopicIdAsync(existingTopic.TopicId);

                        foreach (var vt in mappings)
                        {
                            vt.Status = VocabularyTopicStatus.Deleted;
                            vt.UpdateBy = request.UpdatedBy;
                            vt.UpdateDate = DateTime.UtcNow.AddHours(7);
                            await _vocabularyTopicRepository.UpdateAsync(vt);
                        }

                        await _vocabularyTopicRepository.SaveChangesAsync(cancellationToken);
                    }
                }

                existingTopic.UpdateBy = request.UpdatedBy;
                existingTopic.UpdateDate = DateTime.UtcNow.AddHours(7);

                await _topicRepository.UpdateAsync(existingTopic);
                await _topicRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, "Cập nhật trạng thái chủ đề thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateTopicStatus failed. TopicId={TopicId}", request.TopicId);

                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
