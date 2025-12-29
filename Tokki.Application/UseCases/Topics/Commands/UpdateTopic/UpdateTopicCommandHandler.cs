using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.UpdateTopic
{
    public class UpdateTopicCommandHandler : IRequestHandler<UpdateTopicCommand, OperationResult<bool>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly ILogger<UpdateTopicCommandHandler> _logger;

        public UpdateTopicCommandHandler(
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            ILogger<UpdateTopicCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(UpdateTopicCommand request, CancellationToken cancellationToken)
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

                // Nếu topic đã Deleted thì không cho update/khôi phục (theo nghiệp vụ bạn đang áp dụng với vocab)
                if (existingTopic.Status == TopicStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicAlreadyDeleted },
                        409,
                        AppErrors.TopicAlreadyDeleted.Description
                    );
                }

                // TopicName: chỉ update khi KHÔNG rỗng
                if (!string.IsNullOrWhiteSpace(request.TopicName))
                {
                    var newName = request.TopicName.Trim();

                    if (!string.Equals(newName, existingTopic.TopicName, StringComparison.Ordinal))
                    {
                        bool topicNameExists = await _topicRepository.IsTopicNameExistsAsync(newName, request.TopicId);
                        if (topicNameExists)
                        {
                            return OperationResult<bool>.Failure(
                                new List<Error> { AppErrors.TopicNameDuplicated },
                                409,
                                AppErrors.TopicNameDuplicated.Description
                            );
                        }

                        existingTopic.TopicName = newName;
                    }
                }

                // Description: truyền rỗng => bỏ qua (không cập nhật)
                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    existingTopic.Description = request.Description.Trim();
                }

                // ImgUrl: truyền rỗng => bỏ qua
                if (!string.IsNullOrWhiteSpace(request.ImgUrl))
                {
                    existingTopic.ImgUrl = request.ImgUrl.Trim();
                }

                // Level: chỉ update khi có gửi
                if (request.Level.HasValue)
                {
                    existingTopic.Level = request.Level.Value;
                }

                // Status: chỉ update khi có gửi
                if (request.Status.HasValue)
                {
                    var newStatus = request.Status.Value;

                    if (newStatus != existingTopic.Status)
                    {
                        existingTopic.Status = newStatus;

                        // Cascade VocabularyTopic theo status của Topic
                        var mappings = await _vocabularyTopicRepository.GetByTopicIdAsync(existingTopic.TopicId);

                        if (newStatus == TopicStatus.Deleted)
                        {
                            // Deleted: ép tất cả mapping -> Deleted
                            foreach (var vt in mappings)
                            {
                                vt.Status = VocabularyTopicStatus.Deleted;
                                vt.UpdateBy = request.UpdatedBy;
                                vt.UpdateDate = DateTime.UtcNow.AddHours(7);
                                await _vocabularyTopicRepository.UpdateAsync(vt);
                            }
                        }
                        else if (newStatus == TopicStatus.Draft)
                        {
                            // Draft: chỉ đổi những cái chưa Deleted
                            foreach (var vt in mappings)
                            {
                                if (vt.Status == VocabularyTopicStatus.Deleted) continue;

                                vt.Status = VocabularyTopicStatus.Draft;
                                vt.UpdateBy = request.UpdatedBy;
                                vt.UpdateDate = DateTime.UtcNow.AddHours(7);
                                await _vocabularyTopicRepository.UpdateAsync(vt);
                            }
                        }
                        else if (newStatus == TopicStatus.Active)
                        {
                            // Active: KHÔNG cascade (mapping giữ nguyên) theo nghiệp vụ bạn chốt
                        }

                        await _vocabularyTopicRepository.SaveChangesAsync(cancellationToken);
                    }
                }

                existingTopic.UpdateBy = request.UpdatedBy;
                existingTopic.UpdateDate = DateTime.UtcNow.AddHours(7);

                await _topicRepository.UpdateAsync(existingTopic);
                await _topicRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(true, 200, "Cập nhật chủ đề thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateTopic failed. TopicId={TopicId}", request.TopicId);

                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
