using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationRule.DTOs;

namespace Tokki.Application.UseCases.PronunciationRule.Queries.GetPronunciationRulesForUser
{
    public class GetPronunciationRulesForUserQueryHandler : IRequestHandler<GetPronunciationRulesForUserQuery, OperationResult<PagedResult<PronunciationRuleDTO>>>
    {
        private readonly IPronunciationRuleRepository _ruleRepository;
        private readonly IPronunciationExampleRepository _exampleRepository;
        private readonly IUserPronunciationExampleProgressRepository _exampleProgressRepository;

        public GetPronunciationRulesForUserQueryHandler(
            IPronunciationRuleRepository ruleRepository,
            IPronunciationExampleRepository exampleRepository,
            IUserPronunciationExampleProgressRepository exampleProgressRepository)
        {
            _ruleRepository = ruleRepository;
            _exampleRepository = exampleRepository;
            _exampleProgressRepository = exampleProgressRepository;
        }

        public async Task<OperationResult<PagedResult<PronunciationRuleDTO>>> Handle(GetPronunciationRulesForUserQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _ruleRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                cancellationToken
            );

            var dtoList = new List<PronunciationRuleDTO>();

            foreach (var r in items)
            {
                var examples = await _exampleRepository.GetExamplesByRuleIdAsync(r.PronunciationRuleId, cancellationToken);
                var totalExamples = examples.Count;
                var practicedCount = await _exampleProgressRepository.CountPracticedByUserIdAndRuleIdAsync(request.UserId, r.PronunciationRuleId);

                var progressPercent = totalExamples > 0 ? (int)((double)practicedCount / totalExamples * 100) : 0;

                dtoList.Add(new PronunciationRuleDTO
                {
                    PronunciationRuleId = r.PronunciationRuleId,
                    RuleName = r.RuleName,
                    Description = r.Description ?? "",
                    Content = r.Content ?? "",
                    SortOrder = r.SortOrder,
                    TotalExamples = totalExamples,
                    PracticedCount = practicedCount,
                    ProgressPercent = progressPercent,
                    IsLearned = progressPercent == 100 && totalExamples > 0
                });
            }

            var pagedResult = PagedResult<PronunciationRuleDTO>.Create(
                dtoList,
                totalCount,
                request.PageNumber,
                request.PageSize
            );

            return OperationResult<PagedResult<PronunciationRuleDTO>>.Success(pagedResult, 200, "Lấy danh sách quy tắc thành công (User).");
        }
    }
}
