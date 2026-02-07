using MediatR;
using Tokki.Application.Common.Models;

namespace Tokki.Application.UseCases.Topics.Commands.ApproveTopic
{
    public class ApproveTopicCommand : IRequest<OperationResult<bool>>
    {
        public string TopicId { get; set; } = string.Empty;
    }
}
