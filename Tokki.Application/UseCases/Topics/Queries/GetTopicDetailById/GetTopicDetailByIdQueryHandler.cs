using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Queries.GetById
{
    public class GetTopicDetailByIdQueryHandler : IRequestHandler<GetTopicDetailByIdQuery, OperationResult<TopicDetailDto>>
    {
        private readonly ITopicRepository _repository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;

        public GetTopicDetailByIdQueryHandler(
            ITopicRepository repository,
            IVocabularyTopicRepository vocabularyTopicRepository)
        {
            _repository = repository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
        }

        public async Task<OperationResult<TopicDetailDto>> Handle(GetTopicDetailByIdQuery request, CancellationToken cancellationToken)
        {
            var topic = await _repository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<TopicDetailDto>.Failure(
                    new List<Tokki.Application.Common.Models.Error> { AppErrors.TopicNotFound },
                    404,
                    AppErrors.TopicNotFound.Description
                );
            }

            // Lấy danh sách vocabularies thuộc topic
            var vocabularyTopics = await _vocabularyTopicRepository.GetByTopicIdAsync(topic.TopicId);

            // Chỉ lấy các vocabulary có status Active
            var activeVocabularies = vocabularyTopics
                .Where(vt => vt.Status == VocabularyTopicStatus.Active &&
                            vt.Vocabulary.Status == VocabularyStatus.Active)
                .Select(vt => new VocabularyDto
                {
                    VocabularyId = vt.Vocabulary.VocabularyId,
                    Text = vt.Vocabulary.Text,
                    Pronunciation = vt.Vocabulary.Pronunciation,
                    Definition = vt.Vocabulary.Definition,
<<<<<<< HEAD
=======
                   // ExampleSentence = vt.Vocabulary.ExampleSentence,
>>>>>>> 519bc38f4c1de86d626062dd3e0674f2cf6e5803
                    ImgURL = vt.Vocabulary.ImgURL,
                    Status = vt.Vocabulary.Status
                })
                .ToList();

            var dto = new TopicDetailDto
            {
                TopicId = topic.TopicId,
                TopicName = topic.TopicName,
                Description = topic.Description,
                ImgUrl = topic.ImgUrl,
                Level = topic.Level,
                CreateBy = topic.CreateBy,
                CreateDate = topic.CreateDate,
                UpdateBy = topic.UpdateBy,
                UpdateDate = topic.UpdateDate,
                Status = topic.Status,
                VocabularyCount = activeVocabularies.Count,
                Vocabularies = activeVocabularies
            };

            return OperationResult<TopicDetailDto>.Success(
                dto,
                200,
                "Lấy thông tin chủ đề thành công"
            );
        }
    }
}