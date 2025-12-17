using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetVocabulariesByTopic
{
    public class GetVocabulariesByTopicQueryHandler : IRequestHandler<GetVocabulariesByTopicQuery, OperationResult<PagedResult<VocabularyDto>>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;

        public GetVocabulariesByTopicQueryHandler(
            IVocabularyRepository vocabularyRepository,
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository)
        {
            _vocabularyRepository = vocabularyRepository;
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
        }

        public async Task<OperationResult<PagedResult<VocabularyDto>>> Handle(
            GetVocabulariesByTopicQuery request,
            CancellationToken cancellationToken)
        {
            // Validate topic tồn tại
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<PagedResult<VocabularyDto>>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { new Error("TOPIC_NOT_FOUND", "Không tìm thấy topic.") },
                    404
                );
            }

            // Lấy vocabularies theo topic với phân trang
            var (vocabularies, totalCount) = await _vocabularyRepository.GetPagedVocabulariesByTopicAsync(
                request.TopicId,
                request.PageNumber,
                request.PageSize,
                request.Status,
                request.SearchText
            );

            // Build response
            var vocabularyDtos = new List<VocabularyDto>();

            foreach (var vocab in vocabularies)
            {
                // Lấy tất cả topics của vocabulary này
                var vocabTopics = await _vocabularyTopicRepository.GetByVocabularyIdAsync(vocab.VocabularyId);
                var activeTopics = vocabTopics.Where(vt => vt.Status == VocabularyTopicStatus.Active).ToList();

                var topics = new List<TopicInfoDto>();
                foreach (var vt in activeTopics)
                {
                    var t = await _topicRepository.GetByIdAsync(vt.TopicId);
                    if (t != null)
                    {
                        topics.Add(new TopicInfoDto
                        {
                            TopicId = t.TopicId,
                            TopicName = t.TopicName
                        });
                    }
                }

                vocabularyDtos.Add(new VocabularyDto
                {
                    VocabularyId = vocab.VocabularyId,
                    Text = vocab.Text,
                    Pronunciation = vocab.Pronunciation,
                    Definition = vocab.Definition,
                 //   ExampleSentence = vocab.ExampleSentence,
                    ImgURL = vocab.ImgURL,
                    Topics = topics,
                    CreateDate = vocab.CreateDate,
                    CreateBy = vocab.CreateBy,
                    UpdateDate = vocab.UpdateDate,
                    UpdateBy = vocab.UpdateBy,
                    Status = vocab.Status
                });
            }

            var pagedResult = PagedResult<VocabularyDto>.Create(
                vocabularyDtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            var message = totalCount > 0
                ? $"Tìm thấy {totalCount} vocabulary trong topic '{topic.TopicName}'."
                : $"Topic '{topic.TopicName}' chưa có vocabulary nào.";

            return OperationResult<PagedResult<VocabularyDto>>.Success(
                pagedResult,
                200,
                message
            );
        }
    }
}