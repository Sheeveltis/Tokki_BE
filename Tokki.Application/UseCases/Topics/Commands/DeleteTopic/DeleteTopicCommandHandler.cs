using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.Topics.Commands.DeleteTopic
{
    public class DeleteTopicCommandHandler : IRequestHandler<DeleteTopicCommand, OperationResult<bool>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly ILogger<DeleteTopicCommandHandler> _logger;

        public DeleteTopicCommandHandler(
            ITopicRepository topicRepository,
            ILogger<DeleteTopicCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _logger = logger;
        }

        public async Task<OperationResult<bool>> Handle(DeleteTopicCommand request, CancellationToken cancellationToken)
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

                // Check if topic has associated meanings
                bool hasMeanings = await _topicRepository.HasMeaningsAsync(request.TopicId);
                if (hasMeanings)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { new Error("Topic.HasMeanings", "Không thể xóa chủ đề đang có từ vựng") },
                        400,
                        "Không thể xóa chủ đề đang có từ vựng"
                    );
                }

                await _topicRepository.DeleteAsync(existingTopic);
                await _topicRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<bool>.Success(
                    true,
                    200,
                    "Xóa chủ đề thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa chủ đề: {TopicId}", request.TopicId);
                return OperationResult<bool>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}
