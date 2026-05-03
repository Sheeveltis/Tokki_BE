using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Queries
{
    public class GetAllTopicsQueryHandler : IRequestHandler<GetAllTopicsQuery, OperationResult<PagedResult<TopicDto>>>
    {
        private readonly ITopicRepository _repository;
        private readonly IEnumConfigRepository _enumConfigRepository;

        public GetAllTopicsQueryHandler(ITopicRepository repository, IEnumConfigRepository enumConfigRepository)
        {
            _repository = repository;
            _enumConfigRepository = enumConfigRepository;
        }

        public async Task<OperationResult<PagedResult<TopicDto>>> Handle(GetAllTopicsQuery request, CancellationToken cancellationToken)
        {
            // Lấy dữ liệu phân trang
            var (items, totalCount) = await _repository.GetVocabTopicsPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status,
                request.Level
            );

            // Lấy danh sách config cho TopicLevel từ DB
            var levelConfigs = await _enumConfigRepository.GetByGroupAsync(EnumGroup.TopicLevel);

            var dtos = new List<TopicDto>();

            foreach (var topic in items)
            {
                var vocabularyCount = await _repository.CountVocabulariesInTopicAsync(topic.TopicId);

                // Tìm nhãn tương ứng từ config
                var levelInfo = levelConfigs.FirstOrDefault(x => x.Value == topic.Level);

                dtos.Add(new TopicDto
                {
                    TopicId = topic.TopicId,
                    TopicName = topic.TopicName,
                    Description = topic.Description,
                    Level = topic.Level,
                    LevelLabel = levelInfo?.Label,
                    LevelKey = levelInfo?.Key,
                    ImgUrl = topic.ImgUrl,
                    VocabularyCount = vocabularyCount,
                    Status = topic.Status,
                    OrderIndex = topic.OrderIndex
                });
            }

            var pagedResult = PagedResult<TopicDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<TopicDto>>.Success(
                pagedResult,
                200,
                "Lấy danh sách chủ đề thành công"
            );
        }
    }
}
