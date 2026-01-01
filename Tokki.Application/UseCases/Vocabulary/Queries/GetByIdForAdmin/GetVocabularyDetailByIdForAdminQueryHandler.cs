using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetByIdForUser
{
    public class GetVocabularyDetailByIdForAdminQueryHandler
        : IRequestHandler<GetVocabularyDetailByIdForAdminQuery, OperationResult<VocabularyDetailForAdminDto>>
    {
        private readonly IVocabularyRepository _vocabularyRepository;

        public GetVocabularyDetailByIdForAdminQueryHandler(IVocabularyRepository vocabularyRepository)
        {
            _vocabularyRepository = vocabularyRepository;
        }

        public async Task<OperationResult<VocabularyDetailForAdminDto>> Handle(
            GetVocabularyDetailByIdForAdminQuery request,
            CancellationToken cancellationToken)
        {
            var vocabulary = await _vocabularyRepository.GetByIdAsync(request.VocabularyId);

            if (vocabulary == null )
            {
                return OperationResult<VocabularyDetailForAdminDto>.Failure(
                    new List<Error> { AppErrors.VocabularyNotFound },
                    404,
                    AppErrors.VocabularyNotFound.Description
                );
            }

            var dto = new VocabularyDetailForAdminDto
            {
                VocabularyId = vocabulary.VocabularyId,
                Text = vocabulary.Text,
                Definition = vocabulary.Definition,
                Pronunciation = vocabulary.Pronunciation,
                ImgURL = vocabulary.ImgURL,
                AudioURL = vocabulary.AudioURL,
                Status = vocabulary.Status,
                CreateBy = vocabulary.CreateBy,
                CreateDate = vocabulary.CreateDate,
                UpdateBy = vocabulary.UpdateBy,
                UpdateDate = vocabulary.UpdateDate,
                Topics = vocabulary.VocabularyTopics
                    .Where(vt => vt.Status == VocabularyTopicStatus.Active
                                 && vt.Topic != null
                                 && vt.Topic.Status == TopicStatus.Active)
                    .Select(vt => new VocabularyTopicDto
                    {
                        TopicId = vt.TopicId,
                        TopicName = vt.Topic.TopicName
                    })
                    .ToList(),

                Examples = vocabulary.VocabularyExamples
                    .Where(e => e.Status == VocabularyExampleStatus.Active)
                    .OrderBy(e => e.CreateAt)
                    .Select(e => new VocabularyExampleDetailDto
                    {
                        ExampleId = e.ExampleId,
                        Sentence = e.Sentence,
                        Translation = e.Translation,
                        Status = e.Status,
                        CreateAt = e.CreateAt,
                        CreateBy = e.CreateBy
                    })
                    .ToList()
            };

            return OperationResult<VocabularyDetailForAdminDto>.Success(
                dto,
                200,
                "Lấy chi tiết từ vựng thành công."
            );
        }
    }
}
