using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.FlashCard
{
    public class FlashCardQueryHandler : IRequestHandler<FlashCardQuery, OperationResult<List<FlashCardDto>>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;
        private readonly ITopicRepository _topicRepository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;

        public FlashCardQueryHandler(
            IVocabularyRepository vocabularyRepository,
            ITopicRepository topicRepository,
            IVocabularyTopicRepository vocabularyTopicRepository)
        {
            _vocabularyRepository = vocabularyRepository;
            _topicRepository = topicRepository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
        }

        public async Task<OperationResult<List<FlashCardDto>>> Handle(
            FlashCardQuery request,
            CancellationToken cancellationToken)
        {
            // Validate topic tồn tại
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<List<FlashCardDto>>.Failure(
                    new List<Error> { AppErrors.TopicNotFound },
                    404,
                    "Topic không tồn tại."
                );
            }

            // Lấy tất cả VocabularyTopic relationships theo TopicId
            var vocabularyTopics = await _vocabularyTopicRepository.GetByTopicIdAsync(request.TopicId);

            // Lấy danh sách VocabularyId
            var vocabularyIds = vocabularyTopics
                .Where(vt => vt.Status == VocabularyTopicStatus.Active)
                .Select(vt => vt.VocabularyId)
                .ToList();

            if (!vocabularyIds.Any())
            {
                return OperationResult<List<FlashCardDto>>.Success(
                    new List<FlashCardDto>(),
                    200,
                    "Topic này chưa có từ vựng nào."
                );
            }

            // Lấy vocabularies theo danh sách IDs
            var vocabularies = await _vocabularyRepository.GetByIdsAsync(vocabularyIds);

            // Build response
            var flashCardDto = vocabularies
                .Where(v => v.Status == VocabularyStatus.Active)
                .Select(vocab => new FlashCardDto
                {
                    VocabularyId = vocab.VocabularyId,
                    Text = vocab.Text,
                    Definition = vocab.Definition,
                    ImgURL = vocab.ImgURL,
                    AudioUrl = vocab.AudioURL
                    
                })
                .ToList();

            return OperationResult<List<FlashCardDto>>.Success(
                flashCardDto,
                200,
                $"Lấy {flashCardDto.Count} từ vựng thành công."
            );
        }
    }
}