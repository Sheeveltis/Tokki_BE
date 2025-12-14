using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetVocabulariesByTopic
{
    /// <summary>
    /// Query để lấy danh sách vocabularies theo topic với phân trang
    /// </summary>
    public class GetVocabulariesByTopicQuery : IRequest<OperationResult<PagedResult<VocabularyDto>>>
    {
        public string TopicId { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public VocabularyStatus? Status { get; set; }
        public string? SearchText { get; set; }
    }
}
