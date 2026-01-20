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
                  request.PageNumber, request.PageSize, request.SearchTerm, request.Level
              );
            var dtos = new List<UserTopicDto>();
            foreach (var topic in items)
            {
                var totalVocab = await _topicRepository.CountVocabulariesInTopicAsync(topic.TopicId);

                int progressPercent = 0;

                if (!string.IsNullOrEmpty(request.UserId) && totalVocab > 0)
                {
                    var learnedCount = await _topicRepository.CountLearnedVocabulariesAsync(request.UserId, topic.TopicId);

                    progressPercent = (int)((double)learnedCount / totalVocab * 100);

                    if (progressPercent > 100) progressPercent = 100;
                }
                bool isLearned = (progressPercent == 100);

                dtos.Add(new UserTopicDto
                {
                    TopicId = topic.TopicId,
                    TopicName = topic.TopicName,
                    Description = topic.Description,
                    Level = topic.Level,
                    ImgUrl = topic.ImgUrl,
                    VocabularyCount = totalVocab,
                    Status = topic.Status,
                    Progress = progressPercent,
                    IsLearned = isLearned
                });
            }

            var pagedResult = PagedResult<UserTopicDto>.Create(
                dtos, totalCount, request.PageNumber, request.PageSize
            );

            return OperationResult<PagedResult<UserTopicDto>>.Success(
                pagedResult, 200, "Lấy danh sách chủ đề thành công"
            );
        }
    }
}
