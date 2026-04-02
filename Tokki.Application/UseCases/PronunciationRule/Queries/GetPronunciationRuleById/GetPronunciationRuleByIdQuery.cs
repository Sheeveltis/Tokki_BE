using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRuleById
{
    public class GetPronunciationRuleByIdQuery : IRequest<OperationResult<PronunciationRuleDTO>>
    {
        public string PronunciationRuleId { get; set; }

        public GetPronunciationRuleByIdQuery(string id)
        {
            PronunciationRuleId = id;
        }
    }
}
