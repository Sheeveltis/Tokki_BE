using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.EnumConfigs.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EnumConfigs.Queries.GetByGroup
{
    public class GetEnumConfigByGroupQuery : IRequest<OperationResult<List<EnumConfigDto>>>
    {
        public EnumGroup GroupCode { get; set; }

        public GetEnumConfigByGroupQuery(EnumGroup groupCode)
        {
            GroupCode = groupCode;
        }
    }
}
