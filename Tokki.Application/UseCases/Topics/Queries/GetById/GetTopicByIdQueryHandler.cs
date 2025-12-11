using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;

namespace Tokki.Application.UseCases.Topics.Queries.GetById
{
    public class GetTopicByIdQueryHandler : IRequestHandler<GetTopicByIdQuery, OperationResult<TopicDto>>
    {
        private readonly ITopicRepository _repository;

        public GetTopicByIdQueryHandler(ITopicRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<TopicDto>> Handle(GetTopicByIdQuery request, CancellationToken cancellationToken)
        {
            var topic = await _repository.GetByIdAsync(request.TopicId);

            if (topic == null)
            {
                return OperationResult<TopicDto>.Failure(
                    new List<Error> { AppErrors.TopicNotFound },
                    404,
                    AppErrors.TopicNotFound.Description
                );
            }

            var dto = new TopicDto
            {
                TopicId = topic.TopicId,
                TopicName = topic.TopicName,
                Description = topic.Description,
                CreateBy = topic.CreateBy,
                CreateDate = topic.CreateDate,
                UpdateBy = topic.UpdateBy,
                UpdateDate = topic.UpdateDate,
                Status = topic.Status,
                VocabularyCount = topic.Vocabularies?.Count ?? 0
            };

            return OperationResult<TopicDto>.Success(
                dto,
                200,
                "Lấy thông tin chủ đề thành công"
            );
        }
    }
}