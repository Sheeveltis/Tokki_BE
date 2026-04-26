using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRulesForAdmin
{
    public class GetPronunciationRulesForAdminQueryHandler : IRequestHandler<GetPronunciationRulesForAdminQuery, OperationResult<PagedResult<PronunciationRuleAdminDTO>>>
    {
        private readonly IPronunciationRuleRepository _ruleRepository;
        private readonly IPronunciationExampleRepository _exampleRepository;

        public GetPronunciationRulesForAdminQueryHandler(
            IPronunciationRuleRepository ruleRepository,
            IPronunciationExampleRepository exampleRepository)
        {
            _ruleRepository = ruleRepository;
            _exampleRepository = exampleRepository;
        }

        public async Task<OperationResult<PagedResult<PronunciationRuleAdminDTO>>> Handle(GetPronunciationRulesForAdminQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _ruleRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                cancellationToken
            );

            var dtoList = new List<PronunciationRuleAdminDTO>();

            foreach (var r in items)
            {
                var examples = await _exampleRepository.GetExamplesByRuleIdAsync(r.PronunciationRuleId, cancellationToken);
                
                dtoList.Add(new PronunciationRuleAdminDTO
                {
                    PronunciationRuleId = r.PronunciationRuleId,
                    RuleName = r.RuleName,
                    Description = r.Description ?? "",
                    Content = r.Content ?? "",
                    SortOrder = r.SortOrder,
                    TotalExamples = examples.Count,
                    CreateDate = r.CreateDate
                });
            }

            var pagedResult = PagedResult<PronunciationRuleAdminDTO>.Create(
                dtoList,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<PronunciationRuleAdminDTO>>.Success(pagedResult, 200, "Lấy danh sách quy tắc thành công (Admin).");
        }
    }
}
