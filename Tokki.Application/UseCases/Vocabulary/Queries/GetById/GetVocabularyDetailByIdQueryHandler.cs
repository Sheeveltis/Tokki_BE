using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetById
{
    public class GetVocabularyDetailByIdQueryHandler
        : IRequestHandler<GetVocabularyDetailByIdQuery, OperationResult<VocabularyDetailDto>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;

        public GetVocabularyDetailByIdQueryHandler(
            IVocabularyRepository vocabularyRepository)
        {
            _vocabularyRepository = vocabularyRepository;
        }

        public async Task<OperationResult<VocabularyDetailDto>> Handle(
            GetVocabularyDetailByIdQuery request,
            CancellationToken cancellationToken)
        {
            var vocabulary = await _vocabularyRepository.GetByIdAsync(request.VocabularyId);

            if (vocabulary == null || vocabulary.Status == VocabularyStatus.Deleted)
            {
                return OperationResult<VocabularyDetailDto>.Failure(
                    new List<Error> { AppErrors.VocabularyNotFound },
                    404,
                    AppErrors.VocabularyNotFound.Description
                );
            }

            var dto = new VocabularyDetailDto
            {
                VocabularyId = vocabulary.VocabularyId,
                Text = vocabulary.Text,
                Definition = vocabulary.Definition,
                Pronunciation = vocabulary.Pronunciation,
                ImgURL = vocabulary.ImgURL,
                AudioURL = vocabulary.AudioURL,
                Status = vocabulary.Status,

                Topics = vocabulary.VocabularyTopics
                    .Where(vt => vt.Status == VocabularyTopicStatus.Active)
                    .Select(vt => new VocabularyTopicDto
                    {
                        TopicId = vt.TopicId,
                        TopicName = vt.Topic.TopicName
                    })
                    .ToList()
            };

            return OperationResult<VocabularyDetailDto>.Success(
                dto,
                200,
                "Lấy chi tiết từ vựng thành công."
            );
        }
    }
}
