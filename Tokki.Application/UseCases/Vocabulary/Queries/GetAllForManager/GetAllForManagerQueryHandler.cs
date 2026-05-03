using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetAllForManager
{
    public class GetAllForManagerQueryHandler : IRequestHandler<GetAllForManagerQuery, OperationResult<PagedResult<VocabularyForGetAll>>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;

        public GetAllForManagerQueryHandler(
            IVocabularyRepository vocabularyRepository,
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository)
        {
            _vocabularyRepository = vocabularyRepository;
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
        }

        public async Task<OperationResult<PagedResult<VocabularyForGetAll>>> Handle(
            GetAllForManagerQuery request,
            CancellationToken cancellationToken)
        {
            // request.LevelTopic == null => repository phải hiểu là "không filter"
            var (vocabularies, totalCount) = await _vocabularyRepository.GetPagedVocabulariesForManagerAsync(
                request.PageNumber,
                request.PageSize,
                request.VocabId,
                request.Status,
                request.SearchText,
                request.LevelTopic
            );

            var vocabularyDtos = new List<VocabularyForGetAll>();

            var topicCache = new Dictionary<string, Tokki.Domain.Entities.Topic>();

            foreach (var vocab in vocabularies)
            {
                var vocabTopics = await _vocabularyTopicRepository.GetByVocabularyIdAsync(vocab.VocabularyId);

                var activeMappings = vocabTopics
                    .Where(vt => vt.Status == VocabularyTopicStatus.Active)
                    .ToList();

                var topics = new List<TopicInfoDto>();

                foreach (var vt in activeMappings)
                {
                    if (!topicCache.TryGetValue(vt.TopicId, out var topic))
                    {
                        topic = await _topicRepository.GetByIdAsync(vt.TopicId);
                        if (topic != null)
                        {
                            topicCache[vt.TopicId] = topic;
                        }
                    }

                    if (topic != null && topic.Status == TopicStatus.Active)
                    {
                        topics.Add(new TopicInfoDto
                        {
                            TopicId = topic.TopicId,
                            TopicName = topic.TopicName,
                            Level = topic.Level
                        });
                    }
                }

                var levels = topics
                    .Select(t => t.Level)
                    .Distinct()
                    .ToList();

                // An toàn: chỉ set khi đúng 1 level
                int? singleLevel = levels.Count == 1 ? levels[0] : (int?)null;

                vocabularyDtos.Add(new VocabularyForGetAll
                {
                    VocabularyId = vocab.VocabularyId,
                    Text = vocab.Text,
                    Pronunciation = vocab.Pronunciation,
                    Definition = vocab.Definition,
                    AudioURL = vocab.AudioURL,
                    Status = vocab.Status,

                    // nếu DTO bạn có field Topics thì mở dòng dưới
                    // Topics = topics,

                    LevelTopic = singleLevel
                });
            }

            var pagedResult = PagedResult<VocabularyForGetAll>.Create(
                vocabularyDtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<VocabularyForGetAll>>.Success(
                pagedResult,
                200,
                "Lấy danh sách từ vựng thành công"
            );
        }
    }
}
