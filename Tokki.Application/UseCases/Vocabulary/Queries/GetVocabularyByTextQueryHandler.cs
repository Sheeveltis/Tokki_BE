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

namespace Tokki.Application.UseCases.Vocabulary.Queries
{
    public class GetVocabularyByTextQueryHandler : IRequestHandler<GetVocabularyByTextQuery, OperationResult<PagedResult<VocabularyDto>>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;

        public GetVocabularyByTextQueryHandler(
            IVocabularyRepository vocabularyRepository,
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository)
        {
            _vocabularyRepository = vocabularyRepository;
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
        }

        public async Task<OperationResult<PagedResult<VocabularyDto>>> Handle(
            GetVocabularyByTextQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return OperationResult<PagedResult<VocabularyDto>>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { new Error("INVALID_INPUT", "Text không được để trống.") },
                    400
                );
            }

            // Lấy vocabularies theo text với phân trang
            var (vocabularies, totalCount) = await _vocabularyRepository.GetPagedVocabulariesByTextAsync(
                request.Text,
                request.PageNumber,
                request.PageSize,
                request.TopicId,
                request.Status
            );

            if (totalCount == 0)
            {
                return OperationResult<PagedResult<VocabularyDto>>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { new Error("VOCABULARY_NOT_FOUND", $"Không tìm thấy vocabulary với text '{request.Text}'.") },
                    404
                );
            }

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
                    var topic = await _topicRepository.GetByIdAsync(vt.TopicId);
                    if (topic != null)
                    {
                        topics.Add(new TopicInfoDto
                        {
                            TopicId = topic.TopicId,
                            TopicName = topic.TopicName
                        });
                    }
                }

                vocabularyDtos.Add(new VocabularyDto
                {
                    VocabularyId = vocab.VocabularyId,
                    Text = vocab.Text,
                    Pronunciation = vocab.Pronunciation,
                    AudioURL = vocab.AudioURL,
                    Definition = vocab.Definition,
                    ExampleSentence = vocab.ExampleSentence,
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
                ? $"Tìm thấy {totalCount} nghĩa khác nhau cho từ '{request.Text}'."
                : $"Từ '{request.Text}' chưa có nghĩa nào.";

            return OperationResult<PagedResult<VocabularyDto>>.Success(
                pagedResult,
                200,
                message
            );
        }
    }
}
