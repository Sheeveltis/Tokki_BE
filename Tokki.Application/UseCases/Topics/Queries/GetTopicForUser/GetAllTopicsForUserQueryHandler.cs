using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Queries.GetTopicForUser
{
    public class GetAllTopicsForUserQueryHandler : IRequestHandler<GetAllTopicsForUserQuery, OperationResult<PagedResult<UserTopicDto>>>
    {
        private readonly ITopicRepository _topicRepository;
        private readonly IUserTopicProgressRepository _progressRepository;
        private readonly IEnumConfigRepository _enumConfigRepository;

        public GetAllTopicsForUserQueryHandler(
            ITopicRepository topicRepository,
            IUserTopicProgressRepository progressRepository,
            IEnumConfigRepository enumConfigRepository)
        {
            _topicRepository = topicRepository;
            _progressRepository = progressRepository;
            _enumConfigRepository = enumConfigRepository;
        }

        public async Task<OperationResult<PagedResult<UserTopicDto>>> Handle(GetAllTopicsForUserQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _topicRepository.GetVocabTopicsPagedForUserAsync(
                request.PageNumber, request.PageSize, request.SearchTerm, request.Level);

            // Lấy danh sách config cho TopicLevel từ DB
            var levelConfigs = await _enumConfigRepository.GetByGroupAsync(EnumGroup.TopicLevel);

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

                // Tìm nhãn tương ứng từ config
                var levelInfo = levelConfigs.FirstOrDefault(x => x.Value == topic.Level);

                dtos.Add(new UserTopicDto
                {
                    TopicId = topic.TopicId,
                    TopicName = topic.TopicName,
                    Description = topic.Description,
                    Level = topic.Level,
                    LevelLabel = levelInfo?.Label,
                    LevelKey = levelInfo?.Key,
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
