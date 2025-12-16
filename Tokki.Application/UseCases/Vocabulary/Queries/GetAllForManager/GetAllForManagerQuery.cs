using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Vocabulary.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Vocabulary.Queries.GetAllForManager
{
    public class GetAllForManagerQuery : IRequest<OperationResult<PagedResult<VocabularyDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Filter chính
        public VocabularyStatus? Status { get; set; }

        // Filter theo Topic (Manager có thể muốn lọc từ vựng thuộc 1 topic cụ thể)
        public string? TopicId { get; set; }

        // Search chung (tìm trong Text, Definition, Pronunciation)
        public string? SearchText { get; set; }
    }
}