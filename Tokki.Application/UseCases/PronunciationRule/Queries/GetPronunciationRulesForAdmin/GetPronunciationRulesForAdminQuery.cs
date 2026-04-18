using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRulesForAdmin
{
    public class GetPronunciationRulesForAdminQuery : IRequest<OperationResult<PagedResult<PronunciationRuleAdminDTO>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
    }
}
