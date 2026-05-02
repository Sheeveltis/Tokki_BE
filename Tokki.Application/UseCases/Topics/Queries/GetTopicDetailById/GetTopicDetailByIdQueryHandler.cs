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
    public class GetTopicDetailByIdQueryHandler
        : IRequestHandler<GetTopicDetailByIdQuery, OperationResult<TopicDetailDto>>
    {
        private readonly ITopicRepository _repository;
        private readonly IVocabularyTopicRepository _vocabularyTopicRepository;
        private readonly IEnumConfigRepository _enumConfigRepository;

        public GetTopicDetailByIdQueryHandler(
            ITopicRepository repository,
            IVocabularyTopicRepository vocabularyTopicRepository,
            IEnumConfigRepository enumConfigRepository)
        {
            _repository = repository;
            _vocabularyTopicRepository = vocabularyTopicRepository;
            _enumConfigRepository = enumConfigRepository;
        }

        public async Task<OperationResult<TopicDetailDto>> Handle(
            GetTopicDetailByIdQuery request,
            CancellationToken cancellationToken)
        {
            var topic = await _repository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                return OperationResult<TopicDetailDto>.Failure(
                    new List<Error> { AppErrors.TopicNotFound },
                    404,
                    AppErrors.TopicNotFound.Description
                );
            }

            // Lấy thông tin Level từ DB
            var levelInfo = await _enumConfigRepository.GetByValueAsync(EnumGroup.TopicLevel, topic.Level);

            var vocabularyTopics = await _vocabularyTopicRepository.GetByTopicIdAsync(topic.TopicId);

            var activeVocabularies = vocabularyTopics
                .Where(vt => vt.Status == VocabularyTopicStatus.Active &&
                             vt.Vocabulary.Status == VocabularyStatus.Active)
                .Select(vt => new VocabularyDto
                {
                    VocabularyId = vt.Vocabulary.VocabularyId,
                    Text = vt.Vocabulary.Text,
                    Pronunciation = vt.Vocabulary.Pronunciation,
                    Definition = vt.Vocabulary.Definition,
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
                LevelLabel = levelInfo?.Label,
                LevelKey = levelInfo?.Key,

                CreateBy = topic.CreateBy,
                CreateDate = topic.CreateDate,

                UpdateBy = topic.UpdateBy,
                UpdateDate = topic.UpdateDate,

                ApprovedBy = topic.ApprovedBy,
                ApprovedDate = topic.ApprovedDate,

                Status = topic.Status,
                VocabularyCount = activeVocabularies.Count,
                Vocabularies = activeVocabularies,
                OrderIndex = topic.OrderIndex,
            };

            return OperationResult<TopicDetailDto>.Success(dto, 200, "Lấy thông tin chủ đề thành công");
        }
    }
}
