using MediatR;
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
                request.Status
            );

            var dtos = items.Select(t => new TopicDto
            {
                TopicId = t.TopicId,
                TopicName = t.TopicName,
                Description = t.Description,
                CreateBy = t.CreateBy,
                CreateDate = t.CreateDate,
                UpdateBy = t.UpdateBy,
                UpdateDate = t.UpdateDate,
                Status = t.Status,
            }).ToList();

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