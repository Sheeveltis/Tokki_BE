using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;

namespace Tokki.Application.UseCases.Topics.Queries.GetTopicForUser
{
    public class GetAllTopicsForUserQueryHandler : IRequestHandler<GetAllTopicsForUserQuery, OperationResult<PagedResult<UserTopicDto>>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IUserTopicProgressRepository _progressRepository; 

        public GetAllTopicsForUserQueryHandler(
            ITopicRepository topicRepository,
            IUserTopicProgressRepository progressRepository)
        {
            _topicRepository = topicRepository;
            _progressRepository = progressRepository;
        }

        public async Task<OperationResult<PagedResult<UserTopicDto>>> Handle(GetAllTopicsForUserQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _topicRepository.GetPagedForUserAsync(
                  request.PageNumber,
                  request.PageSize,
                  request.SearchTerm,
                  request.Level
              );

            var topicIds = items.Select(x => x.TopicId).ToList();

            var progressList = await _progressRepository.GetByUserIdAndTopicIdsAsync(request.UserId, topicIds);

            var dtos = new List<UserTopicDto>();

            foreach (var topic in items)
            {
                var vocabularyCount = await _topicRepository.CountVocabulariesInTopicAsync(topic.TopicId);

                var userProgress = progressList.FirstOrDefault(p => p.TopicId == topic.TopicId);
                var isLearned = userProgress != null && userProgress.IsLearned;

                dtos.Add(new UserTopicDto
                {
                    TopicId = topic.TopicId,
                    TopicName = topic.TopicName,
                    Description = topic.Description,
                    Level = topic.Level,
                    ImgUrl = topic.ImgUrl,
                    VocabularyCount = vocabularyCount,
                    Status = topic.Status,
                    IsLearned = isLearned
                });
            }

            var pagedResult = PagedResult<UserTopicDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<UserTopicDto>>.Success(
                pagedResult,
                200,
                "Lấy danh sách chủ đề thành công"
            );
        }
    }
}
