using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.IRepositories;
using Tokki.Application.UseCases.PronunciationExample.DTOs;

namespace Tokki.Application.UseCases.PronunciationExample.Queries.GetPagedPronunciationExamples
{
    public class GetPagedPronunciationExamplesQueryHandler : IRequestHandler<GetPagedPronunciationExamplesQuery, OperationResult<PagedResult<PronunciationExampleDTO>>>
    {
        private readonly IPronunciationExampleRepository _exampleRepo;

        public GetPagedPronunciationExamplesQueryHandler(IPronunciationExampleRepository exampleRepo)
        {
            _exampleRepo = exampleRepo;
        }

        public async Task<OperationResult<PagedResult<PronunciationExampleDTO>>> Handle(GetPagedPronunciationExamplesQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.PronunciationRuleId))
            {
                return OperationResult<PagedResult<PronunciationExampleDTO>>.Failure(new Error("Rule.Required", "Mã quy tắc phát âm là bắt buộc."), 400);
            }

            var (items, totalCount) = await _exampleRepo.GetPagedAsync(
                request.PronunciationRuleId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                cancellationToken);

            var dtos = items.Select(e => new PronunciationExampleDTO
            {
                ExampleId = e.ExampleId,
                PronunciationRuleId = e.PronunciationRuleId,
                TargetScript = e.TargetScript,
                RawScript = e.RawScript,
                PhoneticScript = e.PhoneticScript,
                Meaning = e.Meaning,
                AudioUrl = e.AudioUrl,
                SortOrder = e.SortOrder
            }).ToList();

            var pagedResult = new PagedResult<PronunciationExampleDTO>(dtos, totalCount, request.PageNumber, request.PageSize);
            return OperationResult<PagedResult<PronunciationExampleDTO>>.Success(pagedResult);
        }
    }
}
