using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.FavoriteVocabulary.DTOs;

namespace Tokki.Application.UseCases.FavoriteVocabulary.Queries.GetFavoriteVocabularies
{
    
    public class GetFavoriteVocabulariesQuery : IRequest<OperationResult<PagedResult<FavoriteVocabularyDto>>>
    {
        public string? TopicId { get; set; } // null => lấy hết
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; } // optional
    }
}
