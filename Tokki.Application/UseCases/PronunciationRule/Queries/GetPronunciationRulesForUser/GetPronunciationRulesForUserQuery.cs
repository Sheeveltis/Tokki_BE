using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRulesForUser
{
    public class GetPronunciationRulesForUserQuery : IRequest<OperationResult<PagedResult<PronunciationRuleDTO>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }

        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
    }
}
