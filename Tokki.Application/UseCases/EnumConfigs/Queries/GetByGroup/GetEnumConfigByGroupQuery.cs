using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.EnumConfigs.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EnumConfigs.Queries.GetByGroup
{
    public class GetEnumConfigByGroupQuery : IRequest<OperationResult<PagedResult<EnumConfigDto>>>
    {
        public EnumGroup GroupCode { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public GetEnumConfigByGroupQuery(EnumGroup groupCode, int pageNumber = 1, int pageSize = 10)
        {
            GroupCode = groupCode;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
