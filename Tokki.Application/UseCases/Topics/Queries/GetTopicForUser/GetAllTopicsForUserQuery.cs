using MediatR;
using System.Text.Json.Serialization;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Topics.DTOs;
using Tokki.Domain.Enums;

namespace Tokki.Application.UseCases.Topics.Queries
{
    public class GetAllTopicsForUserQuery : IRequest<OperationResult<PagedResult<UserTopicDto>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
        public int? Level { get; set; }

        [JsonIgnore]
        public string UserId { get; set; } = null!;
    }
}
