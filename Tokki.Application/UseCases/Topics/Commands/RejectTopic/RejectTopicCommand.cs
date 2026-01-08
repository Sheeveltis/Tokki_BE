using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Topics.Commands.RejectTopic
{
    public class RejectTopicCommand : IRequest<OperationResult<bool>>
    {
        public string TopicId { get; set; } = string.Empty;
        public string RejectReason { get; set; } = string.Empty;
    }
}
