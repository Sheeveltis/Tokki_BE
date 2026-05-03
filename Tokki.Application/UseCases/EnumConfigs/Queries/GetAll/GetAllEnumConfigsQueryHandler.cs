using MediatR;
using System.Collections.Generic;
using System.Linq;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;

namespace Tokki.Application.UseCases.EnumConfigs.Queries.GetAll
{
    public class GetAllEnumConfigsQueryHandler : IRequestHandler<GetAllEnumConfigsQuery, OperationResult<List<EnumConfigAdminDto>>>
    {
        private readonly IEnumConfigRepository _enumConfigRepository;

        public GetAllEnumConfigsQueryHandler(IEnumConfigRepository enumConfigRepository)
        {
            _enumConfigRepository = enumConfigRepository;
        }

        public async Task<OperationResult<List<EnumConfigAdminDto>>> Handle(GetAllEnumConfigsQuery request, CancellationToken cancellationToken)
        {
            var configs = await _enumConfigRepository.GetFilteredAsync(request.GroupCode);

            var data = configs.Select(c => new EnumConfigAdminDto
            {
                Id = c.Id,
                GroupCode = c.GroupCode,
                Key = c.Key,
                Value = c.Value,
                Label = c.Label,
                Description = c.Description,
                SortOrder = c.SortOrder,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();

            return OperationResult<List<EnumConfigAdminDto>>.Success(data);
        }
    }
}
