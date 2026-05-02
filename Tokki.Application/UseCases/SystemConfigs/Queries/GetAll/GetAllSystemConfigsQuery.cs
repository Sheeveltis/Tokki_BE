using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.SystemConfigs.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.SystemConfigs.Queries.GetAll
{
    public class GetAllSystemConfigsQuery : IRequest<OperationResult<PagedResult<SystemConfigDto>>>
    {
        public int PageNumber { get; set; } = 1;  
        public int PageSize { get; set; } = 10;
        public SystemConfigType? ConfigType { get; set; }
        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }
    }
}