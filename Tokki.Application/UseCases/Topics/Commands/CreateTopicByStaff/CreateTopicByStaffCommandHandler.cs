using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.IServices;
using Tokki.Domain.Entities;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Commands.CreateTopicByStaff
{
    public class CreateTopicByStaffCommandHandler
        : IRequestHandler<CreateTopicByStaffCommand, OperationResult<string>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CreateTopicByStaffCommandHandler> _logger;

        public CreateTopicByStaffCommandHandler(
            ITopicRepository topicRepository,
            IIdGeneratorService idGenerator,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CreateTopicByStaffCommandHandler> logger)
        {
            _topicRepository = topicRepository;
            _idGenerator = idGenerator;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<OperationResult<string>> Handle(
            CreateTopicByStaffCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.UserUnauthorized },
                    401,
                    AppErrors.UserUnauthorized.Description
                );
            }

            bool exists = await _topicRepository.IsTopicNameExistsAsync(request.TopicName);
            if (exists)
            {
                return OperationResult<string>.Failure(
                    new List<Error> { AppErrors.TopicNameDuplicated },
                    409,
                    AppErrors.TopicNameDuplicated.Description
                );
            }

            var topic = new Topic
            {
                TopicId = _idGenerator.GenerateCustom(15),
                TopicName = request.TopicName,
                Description = request.Description,
                Level = request.Level,
                ImgUrl = request.ImgUrl,
                CreateBy = currentUserId,
                CreateDate = DateTime.UtcNow.AddHours(7),
                Status = TopicStatus.PendingApproval,
                TopicType =TopicType.VocabStudy
            };

            await _topicRepository.AddAsync(topic);
            await _topicRepository.SaveChangesAsync(cancellationToken);

            return OperationResult<string>.Success(
                topic.TopicId,
                201,
                "Tạo topic thành công. Đang chờ phê duyệt."
            );
        }
    }
}
