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
    public class GetPronunciationRulesQueryHandler : IRequestHandler<GetPronunciationRulesQuery, OperationResult<PagedResult<PronunciationRuleDTO>>>
    {
        private readonly IPronunciationRuleRepository _ruleRepository;

        public GetPronunciationRulesQueryHandler(IPronunciationRuleRepository ruleRepository)
        {
            _ruleRepository = ruleRepository;
        }

        public async Task<OperationResult<PagedResult<PronunciationRuleDTO>>> Handle(GetPronunciationRulesQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _ruleRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                cancellationToken
            );

            var dtoList = items.Select(r => new PronunciationRuleDTO
            {
                PronunciationRuleId = r.PronunciationRuleId,
                RuleName = r.RuleName,
                Description = r.Description ?? "",
                Content = r.Content ?? "",
                SortOrder = r.SortOrder
            }).ToList();

            var pagedResult = PagedResult<PronunciationRuleDTO>.Create(
                dtoList,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<PronunciationRuleDTO>>.Success(pagedResult, 200, "Lấy danh sách quy tắc thành công.");
        }
    }
}
