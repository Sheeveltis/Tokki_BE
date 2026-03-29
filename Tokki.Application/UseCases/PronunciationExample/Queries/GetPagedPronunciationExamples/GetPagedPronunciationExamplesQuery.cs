using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.PronunciationExample.DTOs;

namespace Tokki.Application.UseCases.PronunciationExample.Queries.GetPagedPronunciationExamples
{
    public class GetPagedPronunciationExamplesQuery : IRequest<OperationResult<PagedResult<PronunciationExampleDTO>>>
    {
        public string PronunciationRuleId { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }

    public class PronunciationExampleDTO
    {
        public string ExampleId { get; set; } = string.Empty;
        public string PronunciationRuleId { get; set; } = string.Empty;
        public string TargetScript { get; set; } = string.Empty;
        public string RawScript { get; set; } = string.Empty;
        public string PhoneticScript { get; set; } = string.Empty;
        public string? Meaning { get; set; }
        public string? AudioUrl { get; set; }
        public int SortOrder { get; set; }
    }
}
