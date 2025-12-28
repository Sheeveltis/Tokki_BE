using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.Topics.DTOs;

namespace Tokki.Application.UseCases.Topics.Queries
{
    public class GetAllTopicsQueryHandler : IRequestHandler<GetAllTopicsQuery, OperationResult<PagedResult<TopicDto>>>
    {
        private readonly ITopicRepository _repository;

        public GetAllTopicsQueryHandler(ITopicRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<TopicDto>>> Handle(GetAllTopicsQuery request, CancellationToken cancellationToken)
        {
            // Lấy dữ liệu phân trang
            var (items, totalCount) = await _repository.GetPagedAsync(
                  request.PageNumber,
                  request.PageSize,
                  request.SearchTerm,
                  request.Status,
                  request.Level
              );

            var dtos = new List<TopicDto>();

            foreach (var topic in items)
            {
                // Đếm số vocabularies trong topic (ĐÃ CẬP NHẬT)
                var vocabularyCount = await _repository.CountVocabulariesInTopicAsync(topic.TopicId);

                dtos.Add(new TopicDto
                {
                    TopicId = topic.TopicId,
                    TopicName = topic.TopicName,
                    Description = topic.Description,                  
                    Level=topic.Level,
                    ImgUrl=topic.ImgUrl,
                    VocabularyCount = vocabularyCount,
                    Status = topic.Status

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
