using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries
{
    /// <summary>
    /// Query để lấy tất cả vocabularies với cùng text (các nghĩa khác nhau)
    /// Ví dụ: text="은행" sẽ trả về cả "ngân hàng" và "quả ngân hạnh"
    /// </summary>
    public class GetVocabularyByTextQuery : IRequest<OperationResult<PagedResult<VocabularyDto>>>
    {
        public string Text { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? TopicId { get; set; }
        public VocabularyStatus? Status { get; set; }
    }
}
