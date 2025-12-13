using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

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
                // Check if topic exists
                var existingTopic = await _topicRepository.GetByIdAsync(request.TopicId);
                if (existingTopic == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicNotFound },
                        404,
                        AppErrors.TopicNotFound.Description
                    );
                }

                // Check if new topic name already exists (excluding current topic)
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

                // Update topic properties
                existingTopic.TopicName = request.TopicName;
                existingTopic.Description = request.Description;
                existingTopic.Status = request.Status;
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
                _logger.LogError(ex, "Lỗi khi cập nhật chủ đề: {TopicId}", request.TopicId);
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
