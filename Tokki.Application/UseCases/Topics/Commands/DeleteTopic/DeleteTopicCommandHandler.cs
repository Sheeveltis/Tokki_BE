using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Domain.Enums;

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
                // 1. Kiểm tra xem topic có tồn tại không
                var existingTopic = await _topicRepository.GetByIdAsync(request.TopicId);

                if (existingTopic == null)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicNotFound },
                        404,
                        AppErrors.TopicNotFound.Description
                    );
                }

                // 2. Kiểm tra xem topic đã bị xóa mềm trước đó chưa
                if (existingTopic.Status == TopicStatus.Deleted)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicAlreadyDeleted },
                        400,
                        AppErrors.TopicAlreadyDeleted.Description
                    );
                }

                // 3. Kiểm tra xem topic có đang chứa từ vựng không (Business Rule)
                int numOfWord = await _topicRepository.CountVocabulariesInTopicAsync(request.TopicId);
                if (numOfWord > 0)
                {
                    return OperationResult<bool>.Failure(
                        new List<Error> { AppErrors.TopicHasVocabularies },
                        400,
                        AppErrors.TopicHasVocabularies.Description
                    );
                }

                // 4. Thực hiện xóa mềm (Soft Delete)
                existingTopic.Status = TopicStatus.Deleted;

                // 5. Cập nhật vào database
                await _topicRepository.UpdateAsync(existingTopic);
                await _topicRepository.SaveChangesAsync(cancellationToken);


                return OperationResult<bool>.Success(
                    true,
                    200,
                    "Xóa chủ đề thành công"
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