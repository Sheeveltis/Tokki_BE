using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Queries
{
    public class GetAllTopicsQuery : IRequest<OperationResult<PagedResult<TopicDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public TopicStatus? Status { get; set; }
        public int? Level { get; set; }
    }
}
