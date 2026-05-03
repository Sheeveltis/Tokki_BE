using MediatR;
using System.Collections.Generic;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.EnumConfigs.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.EnumConfigs.Queries.GetAll
{
    public class GetAllEnumConfigsQuery : IRequest<OperationResult<List<EnumConfigAdminDto>>>
    {
        public EnumGroup? GroupCode { get; set; }
    }

    public class EnumConfigAdminDto : EnumConfigDto
    {
        public int Id { get; set; }
        public EnumGroup GroupCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
