using MediatR;
using System.Collections.Generic;
using System.Linq;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.EnumConfigs.DTOs;

namespace Tokki.Application.UseCases.EnumConfigs.Queries.GetByGroup
{
    public class GetEnumConfigByGroupQueryHandler : IRequestHandler<GetEnumConfigByGroupQuery, OperationResult<List<EnumConfigDto>>>
    {
        private readonly IEnumConfigRepository _enumConfigRepository;

        public GetEnumConfigByGroupQueryHandler(IEnumConfigRepository enumConfigRepository)
        {
            _enumConfigRepository = enumConfigRepository;
        }

        public async Task<OperationResult<List<EnumConfigDto>>> Handle(GetEnumConfigByGroupQuery request, CancellationToken cancellationToken)
        {
            var configs = await _enumConfigRepository.GetByGroupAsync(request.GroupCode);

            var data = configs.Select(c => new EnumConfigDto
            {
                Key = c.Key,
                Value = c.Value,
                Label = c.Label,
                Description = c.Description,
                SortOrder = c.SortOrder
            }).ToList();

            return OperationResult<List<EnumConfigDto>>.Success(data);
        }
    }
}
