using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.TopikLevelConfigs.DTOs;

namespace Tokki.Application.UseCases.TopikLevelConfigs.Queries.GetAll
{
    public class GetAllTopikLevelConfigsQuery : IRequest<OperationResult<PagedResult<TopikLevelConfigDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchText { get; set; }
        public int? ExamGroup { get; set; }
        public bool? IsActive { get; set; }
    }
}
