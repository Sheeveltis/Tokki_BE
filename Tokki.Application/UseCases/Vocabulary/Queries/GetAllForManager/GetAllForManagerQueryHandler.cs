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
    public class GetAllForManagerQueryHandler : IRequestHandler<GetAllForManagerQuery, OperationResult<PagedResult<VocabularyDto>>>
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

        public async Task<OperationResult<PagedResult<VocabularyDto>>> Handle(
            GetAllForManagerQuery request,
            CancellationToken cancellationToken)
        {
            

            var (vocabularies, totalCount) = await _vocabularyRepository.GetPagedVocabulariesForManagerAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.TopicId,
                    request.Status,
                    request.SearchText
                );

            // --- PHẦN MAP DTO GIỮ NGUYÊN ---
            var vocabularyDtos = new List<VocabularyDto>();

            foreach (var vocab in vocabularies)
            {
                // Lấy danh sách Topics cho từng Vocabulary để hiển thị ra
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
                    // ExampleSentence = vocab.ExampleSentence,
                    ImgURL = vocab.ImgURL,
                    AudioURL = vocab.AudioURL,
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

            return OperationResult<PagedResult<VocabularyDto>>.Success(
                pagedResult,
                200,
                "Lấy danh sách từ vựng thành công"
            );
        }
    }
}