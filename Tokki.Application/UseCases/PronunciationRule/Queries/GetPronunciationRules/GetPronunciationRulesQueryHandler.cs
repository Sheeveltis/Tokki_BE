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
        private readonly IPronunciationExampleRepository _exampleRepository;
        private readonly IUserPronunciationProgressRepository _progressRepository;
        private readonly IUserPronunciationExampleProgressRepository _exampleProgressRepository;

        public GetPronunciationRulesQueryHandler(
            IPronunciationRuleRepository ruleRepository,
            IPronunciationExampleRepository exampleRepository,
            IUserPronunciationProgressRepository progressRepository,
            IUserPronunciationExampleProgressRepository exampleProgressRepository)
        {
            _ruleRepository = ruleRepository;
            _exampleRepository = exampleRepository;
            _progressRepository = progressRepository;
            _exampleProgressRepository = exampleProgressRepository;
        }

        public async Task<OperationResult<PagedResult<PronunciationRuleDTO>>> Handle(GetPronunciationRulesQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _ruleRepository.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                cancellationToken
            );

            var ruleIds = items.Select(r => r.PronunciationRuleId).ToList();
            var learnedRules = new List<string>();
            var dtoList = new List<PronunciationRuleDTO>();

            if (!string.IsNullOrEmpty(request.UserId))
            {
                var progressList = await _progressRepository.GetByUserIdAndRuleIdsAsync(request.UserId, ruleIds);
                learnedRules = progressList.Where(p => p.IsLearned).Select(p => p.PronunciationRuleId).ToList();
            }

            foreach (var r in items)
            {
                var examples = await _exampleRepository.GetExamplesByRuleIdAsync(r.PronunciationRuleId, cancellationToken);
                var totalExamples = examples.Count;
                var practicedCount = 0;

                if (!string.IsNullOrEmpty(request.UserId) && totalExamples > 0)
                {
                    practicedCount = await _exampleProgressRepository.CountPracticedByUserIdAndRuleIdAsync(request.UserId, r.PronunciationRuleId);
                }

                var progressPercent = totalExamples > 0 ? (int)((double)practicedCount / totalExamples * 100) : 0;

                dtoList.Add(new PronunciationRuleDTO
                {
                    PronunciationRuleId = r.PronunciationRuleId,
                    RuleName = r.RuleName,
                    Description = r.Description ?? "",
                    Content = r.Content ?? "",
                    SortOrder = r.SortOrder,
                    IsLearned = learnedRules.Contains(r.PronunciationRuleId) || (progressPercent == 100 && totalExamples > 0),
                    TotalExamples = totalExamples,
                    PracticedCount = practicedCount,
                    ProgressPercent = progressPercent
                });
            }

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
