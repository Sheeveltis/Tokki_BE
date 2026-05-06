using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.MiniGame.Queries.Wordle
{
    public class GetSuitableVocabsQuery : IRequest<OperationResult<PagedResult<VocabularyDto>>>
    {
        public WordleLevel Level { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }
}
