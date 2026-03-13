using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.SystemConfigs.DTOs;

namespace Tokki.Application.UseCases.SystemConfigs.Queries.GetSystemConfigByKey
{
    public class GetSystemConfigByKeyQueryHandler : IRequestHandler<GetSystemConfigByKeyQuery, OperationResult<SystemConfigDto>>
    {
        private readonly ISystemConfigRepository _repository;

        public GetSystemConfigByKeyQueryHandler(ISystemConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<OperationResult<SystemConfigDto>> Handle(GetSystemConfigByKeyQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var config = await _repository.FirstOrDefaultAsync(x => x.Key == request.Key.Trim());

                if (config == null)
                {
                    return OperationResult<SystemConfigDto>.Failure(new Error("CONFIG_NOT_FOUND", "Không tìm thấy cấu hình với Key này."));
                }

                var dto = new SystemConfigDto
                {
                    Key = config.Key,
                    Value = config.Value,
                    Description = config.Description,
                    DataType = config.DataType,
                    IsActive = config.IsActive
                };

                return OperationResult<SystemConfigDto>.Success(dto, 200, "Lấy chi tiết cấu hình thành công.");
            }
            catch (Exception ex)
            {
                return OperationResult<SystemConfigDto>.Failure(new Error("GET_BY_KEY_ERROR", $"Lỗi hệ thống: {ex.Message}"));
            }
        }
    }
}
