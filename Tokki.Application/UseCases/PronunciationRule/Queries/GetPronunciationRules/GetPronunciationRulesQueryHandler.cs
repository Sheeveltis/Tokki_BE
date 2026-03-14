using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRules
{
    public class GetPronunciationRulesQueryHandler : IRequestHandler<GetPronunciationRulesQuery, OperationResult<List<PronunciationRuleDTO>>>
    {
        private readonly IPronunciationRuleRepository _ruleRepository;

        public GetPronunciationRulesQueryHandler(IPronunciationRuleRepository ruleRepository)
        {
            _ruleRepository = ruleRepository;
        }

        public async Task<OperationResult<List<PronunciationRuleDTO>>> Handle(GetPronunciationRulesQuery request, CancellationToken cancellationToken)
        {
            var rules = await _ruleRepository.GetAllActiveRulesAsync(cancellationToken);

            var dtoList = rules.Select(r => new PronunciationRuleDTO
            {
                PronunciationRuleId = r.PronunciationRuleId,
                RuleName = r.RuleName,
                Description = r.Description,
                Content = r.Content,
                SortOrder = r.SortOrder
            }).ToList();

            return OperationResult<List<PronunciationRuleDTO>>.Success(dtoList, 200, "Lấy danh sách quy tắc thành công.");
        }
    }
}
