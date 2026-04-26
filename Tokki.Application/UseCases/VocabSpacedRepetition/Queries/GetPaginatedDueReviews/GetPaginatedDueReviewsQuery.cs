using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.VocabSpacedRepetition.DTOs;

namespace Tokki.Application.UseCases.VocabSpacedRepetition.Queries.GetPaginatedDueReviews
{
    public class GetPaginatedDueReviewsQuery : IRequest<OperationResult<PagedResult<ReviewItemDTO>>>
    {
        [JsonIgnore]
        public string UserId { get; set; } = string.Empty;
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
