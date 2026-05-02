using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetAllForManager
{
    public class GetAllForManagerQuery : IRequest<OperationResult<PagedResult<VocabularyForGetAll>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Filter chính
        public VocabularyStatus? Status { get; set; }

        public string? VocabId { get; set; }


        // Search chung (tìm trong Text, Definition, Pronunciation)
        public string? SearchText { get; set; }
        public int? LevelTopic { get; set; }
    }
}