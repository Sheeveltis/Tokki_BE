using MediatR;
using Tokki.Application.Common.Models;
using Tokki.Application.UseCases.Topics.DTOs;

namespace Tokki.Application.UseCases.Topics.Queries.GetById
{
    public class GetTopicDetailByIdQuery : IRequest<OperationResult<TopicDetailDto>>
    {
        public string TopicId { get; set; } = string.Empty;
    }
}
