using MediatR;
using Microsoft.Extensions.Logging;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;

namespace Tokki.Application.UseCases.Topics.Commands.CreateTopic
{
    public class CreateTopicCommandHandler : IRequestHandler<CreateTopicCommand, OperationResult<string>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<CreateTopicCommandHandler> _logger;

        public CreateTopicCommandHandler(
            ITopicRepository topicRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<CreateTopicCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
        {
            bool topicNameExists = await _topicRepository.IsTopicNameExistsAsync(request.TopicName);
            if (topicNameExists)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.TopicNameDuplicated },
                    409,
                    AppErrors.TopicNameDuplicated.Description
                );
            }

            try
            {
                string newId = _idGeneratorService.GenerateCustom(15);

                var topic = new Topic
                {
                    TopicId = newId,
                    TopicName = request.TopicName,
                    Description = request.Description,
                    CreateBy = request.CreateBy,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    Status = 0
                };

                await _topicRepository.AddAsync(topic);
                await _topicRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    topic.TopicId,
                    201,
                    "Tạo chủ đề thành công"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo chủ đề: {TopicName}", request.TopicName);
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.ServerError },
                    500,
                    AppErrors.ServerError.Description
                );
            }
        }
    }
}