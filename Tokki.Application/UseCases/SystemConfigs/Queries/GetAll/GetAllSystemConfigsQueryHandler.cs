using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.SystemConfigs.DTOs;

namespace Tokki.Application.UseCases.SystemConfigs.Queries.GetAll
{
    public class GetAllSystemConfigsQueryHandler : IRequestHandler<GetAllSystemConfigsQuery, OperationResult<PagedResult<SystemConfigDto>>>
    {
        private readonly ISystemConfigRepository _repository;

        public GetAllSystemConfigsQueryHandler(ISystemConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<PagedResult<SystemConfigDto>>> Handle(GetAllSystemConfigsQuery request, CancellationToken cancellationToken)
        {
            // 1. Gọi Repository lấy dữ liệu phân trang
            var (items, totalCount) = await _repository.GetPagedAsync(request.PageNumber, request.PageSize, request.ConfigType);

            // 2. Map Entity sang DTO
            var dtos = items.Select(c => new SystemConfigDto
            {
                Key = c.Key,
                Value = c.Value,
                Description = c.Description,
                DataType = c.DataType,
                IsActive = c.IsActive,
                ConfigType = (int)c.ConfigType,
                ConfigTypeName = c.ConfigType.ToString()
            }).ToList();

            // 3. Tạo đối tượng PagedResult từ class có sẵn của bạn
            var pagedResult = PagedResult<SystemConfigDto>.Create(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            // 4. Trả về kết quả
            return OperationResult<PagedResult<SystemConfigDto>>.Success(
                pagedResult,
                200,
                "Lấy danh sách thành công"
            );
        }
    }
}