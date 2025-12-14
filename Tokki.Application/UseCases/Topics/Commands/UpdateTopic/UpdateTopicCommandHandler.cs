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
        private readonly ILogger<UpdateTopicCommandHandler> _logger;

        public UpdateTopicCommandHandler(
            ITopicRepository topicRepository,
            ILogger<UpdateTopicCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(UpdateTopicCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var existingTopic = await _topicRepository.GetByIdAsync(request.TopicId);

                if (existingTopic == null || existingTopic.Status == TopicStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicNotFound },
                        404,
                        AppErrors.TopicNotFound.Description
                    );
                }

                if (!string.IsNullOrEmpty(request.TopicName) && request.TopicName != existingTopic.TopicName)
                {
                    bool topicNameExists = await _topicRepository.IsTopicNameExistsAsync(
                        request.TopicName,
                        request.TopicId
                    );

                    if (topicNameExists)
                    {
                        return OperationResult<bool>.Failure(
                            new List<Error> { AppErrors.TopicNameDuplicated },
                            409,
                            AppErrors.TopicNameDuplicated.Description
                        );
                    }

                    existingTopic.TopicName = request.TopicName;
                }

             
                if (request.Description != null)
                {
                    existingTopic.Description = request.Description;
                }


                existingTopic.UpdateBy = request.UpdatedBy;

                existingTopic.UpdateDate = DateTime.UtcNow.AddHours(7);

                await _topicRepository.UpdateAsync(existingTopic);
                await _topicRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(
                    true,
                    200,
                    "Cập nhật chủ đề thành công"
                );
            }
            catch (Exception ex)
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