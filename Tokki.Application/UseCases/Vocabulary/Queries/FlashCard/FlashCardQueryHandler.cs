using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Application.UseCases.Vocabulary.Queries.GetVocabulariesByTopic;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.FlashCard
{
    public class FlashCardQueryHandler: IRequestHandler<FlashCardQuery, OperationResult<PagedResult<FlashCardDto>>>
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


        public async Task<OperationResult<PagedResult<FlashCardDto>>> Handle(FlashCardQuery request, CancellationToken cancellationToken)
        {
            // Validate topic tồn tại
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<PagedResult<FlashCardDto>>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.TopicNotFound },
                    404, "Lấy từ vựng thất bại."
                );
            }

            // Lấy vocabularies theo topic với phân trang
            var (vocabularies, totalCount) = await _vocabularyRepository.GetPagedVocabulariesByTopicAsync(
                request.TopicId,
                request.PageNumber,
                request.PageSize
            );

            // Build response
            var flashCardDto = new List<FlashCardDto>();

            foreach (var vocab in vocabularies)
            {
                flashCardDto.Add(new FlashCardDto
                {
                    Text = vocab.Text,
                    Definition = vocab.Definition,
                    ImgURL = vocab.ImgURL
                });
            }

            var pagedResult = PagedResult<FlashCardDto>.Create(
                flashCardDto,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<FlashCardDto>>.Success(
                pagedResult,
                200,
                "Lấy từ vựng thành công."  // ✅ Sửa từ ' thành "
            );
        }

    }
}
