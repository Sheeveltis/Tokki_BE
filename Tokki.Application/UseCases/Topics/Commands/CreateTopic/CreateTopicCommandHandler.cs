using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.CreateTopic
{
    public class CreateTopicCommandHandler : IRequestHandler<CreateTopicCommand, OperationResult<string>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IIdGeneratorService _idGeneratorService;
        private readonly ILogger<CreateTopicCommandHandler> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateTopicCommandHandler(
            ITopicRepository topicRepository,
            IIdGeneratorService idGeneratorService,
            ILogger<CreateTopicCommandHandler> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _topicRepository = topicRepository;
            _idGeneratorService = idGeneratorService;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<OperationResult<string>> Handle(CreateTopicCommand request, CancellationToken cancellationToken)
        {
            // USER (CreateBy from claims)
            var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            try
            {
                // 1) Check duplicated topic name
                bool topicNameExists = await _topicRepository.IsTopicNameExistsAsync(request.TopicName);
                if (topicNameExists)
                {
                    return OperationResult<string>.Failure(
                        new List<Error> { AppErrors.TopicNameDuplicated },
                        409,
                        AppErrors.TopicNameDuplicated.Description
                    );
                }

                // 2) Create new topic (DEFAULT = Draft)
                string newId = _idGeneratorService.GenerateCustom(15);
                var topic = new Topic
                {
                    TopicId = newId,
                    TopicName = request.TopicName,
                    ImgUrl = request.ImgUrl,
                    Level = request.Level,
                    Description = request.Description,
                    CreateBy = currentUserId,
                    CreateDate = DateTime.UtcNow.AddHours(7),
                    Status = TopicStatus.Draft
                };

                await _topicRepository.AddAsync(topic);
                await _topicRepository.SaveChangesAsync(cancellationToken);

                return OperationResult<string>.Success(
                    topic.TopicId,
                    201,
                    "Tạo chủ đề (bản nháp) thành công"
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
